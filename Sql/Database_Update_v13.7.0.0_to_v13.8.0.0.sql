
INSERT INTO [dbo].[EventTypes]([event_type],[description],[sort_order]) VALUES('LPAE','Loan Penalty Accrual Event',730)
INSERT INTO [dbo].[EventTypes]([event_type],[description],[sort_order]) VALUES('AILE','Accrual Interest Loan Event',740)
GO

CREATE TABLE LoanPenaltyAccrualEvents
(
	id INT NOT NULL,
	penalty MONEY NOT NULL,
)
GO

CREATE TABLE AccrualInterestLoanEvents
(
	id INT NOT NULL,
	interest MONEY NOT NULL,
)
GO
---------------------Penalty-----------------------
DECLARE @eventId INT
DECLARE @date DATE
SET @date = GETDATE()
DECLARE @contract_id INT
IF OBJECT_ID('tempdb..#installments') IS NULL
BEGIN
	CREATE TABLE #installments
	(
		contract_id INT
		, number INT
		, expected_date DATETIME
		, principal MONEY
		, interest MONEY
		, paid_principal MONEY
		, paid_interest MONEY
		, paid_date DATETIME
	)
	CREATE INDEX IX_Contract_id ON #installments (contract_id) -- speed things up
END
TRUNCATE TABLE #installments
INSERT INTO #installments SELECT * FROM dbo.InstallmentSnapshot(@date)
DECLARE @late_principal_penalty_rate FLOAT
DECLARE @late_interest_penalty_rate FLOAT
DECLARE @late_amount_penalty_rate FLOAT
DECLARE @late_olb_penalty_rate FLOAT
DECLARE @grace_period INT
DECLARE @RepaymentEvents TABLE
(
	num INT
	, installment_number INT
	, interest MONEY
	, principal MONEY
	, penalty MONEY
	, event_date DATETIME
	, total MONEY
	, rt_interest MONEY
	, rt_principal MONEY
	, rt_total MONEY
)
DECLARE @max_late_days INT
DECLARE @retval MONEY
DECLARE @number INT
DECLARE @principal MONEY
DECLARE @interest MONEY
DECLARE @expected_date DATETIME
DECLARE @preceding_principal_due MONEY
DECLARE @preceding_interest_due MONEY
DECLARE @start_re INT
DECLARE @start_date DATETIME
DECLARE @late_days INT
DECLARE @end_date DATETIME
DECLARE @principal_late MONEY
DECLARE @interest_late MONEY
DECLARE @total_paid_principal MONEY
DECLARE @total_paid_interest MONEY
DECLARE @i_date DATETIME
DECLARE @re_date DATETIME
DECLARE @amount MONEY


DECLARE cur CURSOR FOR 
SELECT id FROM dbo.ActiveLoans(@date, 0)
OPEN cur
	FETCH NEXT FROM	cur
	INTO @contract_id
	WHILE 0 = @@FETCH_STATUS
	BEGIN
		
		SELECT @late_principal_penalty_rate = non_repayment_penalties_based_on_overdue_principal
		, @late_interest_penalty_rate = non_repayment_penalties_based_on_overdue_interest
		, @late_amount_penalty_rate = non_repayment_penalties_based_on_initial_amount
		, @late_olb_penalty_rate = non_repayment_penalties_based_on_olb
		, @grace_period = grace_period_of_latefees
		FROM dbo.Credit
		WHERE id = @contract_id
		-- Select repayment events with running totals
		DELETE @RepaymentEvents
		INSERT INTO @RepaymentEvents
		SELECT re1.num, re1.installment_number, re1.interests, re1.principal, re1.penalties, re1.event_date, re1.total 
		, SUM(re2.interests) AS rt_interest
		, SUM(re2.principal) AS rt_principal
		, SUM(re2.total) AS rt_total
		FROM
		(
			SELECT re.interests, re.principal, re.penalties, ce.event_date
			, ROW_NUMBER() OVER (ORDER BY event_date, re.id) AS num
			, re.interests + re.principal AS total
			, re.installment_number
			FROM dbo.RepaymentEvents AS re
			LEFT JOIN dbo.ContractEvents AS ce ON re.id = ce.id
			WHERE ce.contract_id = @contract_id AND is_deleted = 0 AND event_date <= @date
		) AS re1
		LEFT JOIN
		(
			SELECT re.interests, re.principal, re.penalties, ce.event_date
			, ROW_NUMBER() OVER (ORDER BY event_date, re.id) AS num
			, re.interests + re.principal AS total
			FROM dbo.RepaymentEvents AS re
			LEFT JOIN dbo.ContractEvents AS ce ON re.id = ce.id
			WHERE ce.contract_id = @contract_id AND is_deleted = 0 AND event_date <= @date
		) AS re2 ON re2.num <= re1.num
		GROUP BY re1.num, re1.installment_number, re1.event_date, re1.principal,re1.penalties, re1.interests, re1.total
		-- Get number of late days
		SELECT @max_late_days = [value]
		FROM dbo.GeneralParameters
		WHERE [key] = 'LATE_DAYS_AFTER_ACCRUAL_CEASES'
		SET @retval = 0
		
		IF @late_interest_penalty_rate > 0 OR @late_principal_penalty_rate > 0
		BEGIN
			-- Traverse through installments
			DECLARE i_cursor CURSOR FOR
			SELECT number, expected_date, principal, interest
			FROM #installments
			WHERE contract_id = @contract_id AND expected_date < @date
			
			OPEN i_cursor
			FETCH NEXT FROM i_cursor
			INTO @number, @expected_date, @principal, @interest
			WHILE 0 = @@FETCH_STATUS
			BEGIN
				SET @start_re = NULL
				SET @start_date = NULL
				-- Get due values for all the preceding installments
				SELECT @preceding_principal_due = ISNULL(SUM(principal), 0)
				, @preceding_interest_due = ISNULL(SUM(interest), 0)
				FROM #installments
				WHERE contract_id = @contract_id AND number < @number
				-- Get start_date, i.e. the date of the latest event when principal or interest
				-- (but *not* penalty) has been repaid
				SELECT @start_re = ISNULL(MAX(num), 0)
				FROM
				(
					SELECT * 
					FROM @RepaymentEvents
					WHERE num >= (SELECT MIN(num) FROM @RepaymentEvents WHERE rt_principal > @preceding_principal_due)
					AND num <= ISNULL((SELECT MAX(num) FROM @RepaymentEvents WHERE rt_principal < @preceding_principal_due + @principal), 0) + 1
					UNION ALL
					SELECT * 
					FROM @RepaymentEvents
					WHERE num >= (SELECT MIN(num) FROM @RepaymentEvents WHERE rt_interest > @preceding_interest_due)
					AND num <= ISNULL((SELECT MAX(num) FROM @RepaymentEvents WHERE rt_interest < @preceding_interest_due + @interest), 0) + 1
				) AS _re
				WHERE principal > 0 OR interest > 0
				-- Get the first repayment event
				SELECT @start_date = event_date
				FROM @RepaymentEvents
				WHERE num = @start_re
				SET @start_date = ISNULL(@start_date, @expected_date)
				-- Obviously, @start_date cannot come before @expected_date
				SET @start_date = CASE WHEN @start_date < @expected_date THEN @expected_date ELSE @start_date END
				SET @end_date = CASE
					WHEN NOT @max_late_days IS NULL THEN DATEADD(dd, @max_late_days, @expected_date)
					ELSE @date
				END
				SET @end_date = CASE WHEN @end_date > @date THEN @date ELSE @end_date END
				SET @late_days = DATEDIFF(dd, @start_date, @end_date)
				SET @late_days = CASE WHEN @late_days < 0 THEN 0 ELSE @late_days END
				SET @late_days = CASE WHEN @late_days <= @grace_period THEN 0 ELSE @late_days END
				-- Get late amounts
				SELECT @principal_late = @preceding_principal_due + @principal - ISNULL(SUM(principal), 0)
				, @interest_late = @preceding_interest_due + @interest - ISNULL(SUM(interest), 0)
				FROM @RepaymentEvents
				SET @principal_late = CASE
					WHEN @principal_late < 0 THEN 0
					WHEN @principal_late > @principal THEN @principal
					ELSE @principal_late
				END
				SET @interest_late = CASE
					WHEN @interest_late < 0 THEN 0
					WHEN @interest_late > @interest THEN @interest
					ELSE @interest_late
				END
				SET @retval = @retval + @principal_late * @late_principal_penalty_rate * @late_days
				SET @retval = @retval + @interest_late * @late_interest_penalty_rate * @late_days
				FETCH NEXT FROM i_cursor
				INTO @number, @expected_date, @principal, @interest
			END -- end of traversal
			CLOSE i_cursor
			DEALLOCATE i_cursor
		END
		IF @late_amount_penalty_rate > 0 OR @late_olb_penalty_rate > 0
		BEGIN
			-- Now calculate penalty on initial amount and / or OLB
			
			-- First, we have to get start date (which in turn will allow
			-- to calculate number of late days).
			-- To get the start date we have to figure out two things:
			-- a) expected date of the first unpaid installment, and
			-- b) date of the latest repayment event with principal > 0 or interest > 0
			-- Take the most recent one as the start date.
			-- If both are NULL then take @date.
			-- Get total paid amounts
			
			SELECT @total_paid_principal = ISNULL(SUM(principal), 0)
			, @total_paid_interest = ISNULL(SUM(interest), 0)
			FROM @RepaymentEvents
			-- Get expected date of the first non-repaid installment
			SELECT @i_date = MIN(expected_date)
			FROM
			(
				SELECT a.number, a.expected_date
				, SUM(b.principal) AS rt_principal
				, SUM(b.interest) AS rt_interest
				FROM #installments AS a
				LEFT JOIN #installments AS b ON a.contract_id = b.contract_id AND b.number <= a.number
				WHERE a.contract_id = @contract_id AND a.expected_date < @date
				GROUP BY a.number, a.expected_date
			) AS i WHERE rt_principal > @total_paid_principal + 0.05 OR rt_interest > @total_paid_interest + 0.05
			-- Get the latest repayment date
			SELECT @re_date = MAX(event_date)
			FROM @RepaymentEvents
			WHERE principal > 0 OR interest > 0
			-- Get start date
			IF @i_date IS NULL
			BEGIN
				SET @start_date = @re_date
			END
			ELSE
			BEGIN
				SET @start_date = CASE WHEN @i_date > @re_date OR @re_date IS NULL THEN @i_date ELSE @re_date END
			END
			SET @start_date = ISNULL(@start_date, @date)
			-- Get end date
			SET @end_date = CASE
				WHEN NOT @max_late_days IS NULL THEN DATEADD(dd, @max_late_days, @start_date)
				ELSE @date
			END
			SET @end_date = CASE WHEN @end_date > @date THEN @date ELSE @end_date END
			-- Get late days
			SET @late_days = DATEDIFF(dd, @start_date, @end_date)
			SET @late_days = CASE WHEN @late_days < 0 THEN 0 ELSE @late_days END
			SET @late_days = CASE WHEN @late_days <= @grace_period THEN 0 ELSE @late_days END
			-- Get amount
			SELECT @amount = SUM(principal)
			FROM #installments
			WHERE contract_id = @contract_id
			-- Get penalty on amount
			SET @retval = @retval + @amount * @late_days * @late_amount_penalty_rate
			
			-- Get penalty on OLB
			SET @retval = @retval + (@amount - @total_paid_principal) * @late_days * @late_olb_penalty_rate
		END
		-- Subtract paid penalty
		SET @start_re = NULL
		SELECT @start_re = MAX(num)
		FROM @RepaymentEvents
		WHERE principal > 0 OR interest > 0
		--SELECT @retval = @retval - ISNULL(SUM(penalty), 0)
		--FROM @RepaymentEvents
		--WHERE num > @start_re
		SET @retval = CASE WHEN @retval < 0 THEN 0 ELSE @retval END
		
		INSERT INTO [dbo].[ContractEvents] ([event_type],[contract_id],[event_date],[user_id],[is_deleted],[is_exported]) VALUES('LPAE',@contract_id,@date,1,0,0)
		SELECT @eventId = SCOPE_IDENTITY()
		INSERT INTO [dbo].[LoanPenaltyAccrualEvents] ([id],[penalty]) VALUES(@eventId, @retval)
				
		FETCH NEXT FROM cur
				INTO @contract_id
	END
CLOSE cur
DEALLOCATE cur
GO

---------------------Interest----------------------
DECLARE @date DATE
	, @contract_id INT
	, @previous_date DATE
	, @previous_installment_date DATE
	, @date_ink DATE
	, @interest FLOAT
	, @interest_per_day FLOAT
	, @eventId INT
SET @date = GETDATE()

DECLARE cur CURSOR FOR
SELECT al.id
	,MAX(inst.expected_date) AS previous_date
	,SUM(inst.interest) AS interest
FROM Activeloans(@date, 0) AS al
LEFT JOIN InstallmentSnapshot(@date) AS inst ON inst.contract_id=al.id
WHERE @date >= inst.expected_date
GROUP BY al.id
OPEN cur
	FETCH NEXT FROM	cur
	INTO @contract_id, @previous_date, @interest
	WHILE @@FETCH_STATUS = 0
	BEGIN
		INSERT INTO [dbo].[ContractEvents] 
				([event_type],[contract_id],[event_date],[user_id],[is_deleted],[is_exported]) 
				VALUES('AILE',@contract_id,@previous_date,1,0,0)
		SELECT @eventId = SCOPE_IDENTITY()
		INSERT INTO [dbo].[AccrualInterestLoanEvents] ([id],[interest]) VALUES(@eventId, @interest)
			
		FETCH NEXT FROM cur
				INTO @contract_id, @previous_date, @interest
	END
CLOSE cur
DEALLOCATE cur

DECLARE cur1 CURSOR FOR
WITH summary AS (SELECT al.id
						,inst.expected_date
						,inst.interest
						,ROW_NUMBER() OVER(PARTITION BY al.id
													 ORDER BY al.id) AS rk
						FROM Activeloans(@date,0) AS al
							LEFT JOIN InstallmentSnapshot(@date) AS inst ON inst.contract_id=al.id
							WHERE @date < inst.expected_date)
SELECT s.id, s.interest
FROM summary s
WHERE s.rk = 1
OPEN cur1
	FETCH NEXT FROM	cur1
	INTO @contract_id, @interest
	WHILE @@FETCH_STATUS = 0
	BEGIN	
		SELECT @previous_installment_date = NULL
		SELECT @previous_installment_date = MAX(inst.expected_date)
			FROM Activeloans(@date, 0) AS al
			LEFT JOIN InstallmentSnapshot(@date) AS inst ON inst.contract_id=al.id
			WHERE @date >= inst.expected_date AND al.id=@contract_id
			group by al.id
		IF @previous_installment_date=NULL
		BEGIN
			SELECT @previous_installment_date= start_date
				 FROM Contracts
				 WHERE id=@contract_id
		END;
		
		SET @date_ink = DATEADD(dd,1,@previous_installment_date)
		SET @interest_per_day = @interest / 30
		
		WHILE @date >= @date_ink
		BEGIN
			IF (SELECT DATEDIFF(dd,@previous_installment_date,@date_ink)) > 30
				SELECT @interest_per_day = 0;
			INSERT INTO [dbo].[ContractEvents] 
				([event_type],[contract_id],[event_date],[user_id],[is_deleted],[is_exported]) 
				VALUES('AILE',@contract_id,@date_ink,1,0,0)
			SELECT @eventId = SCOPE_IDENTITY()
			INSERT INTO [dbo].[AccrualInterestLoanEvents] ([id],[interest]) VALUES(@eventId, @interest_per_day)
			SELECT @date_ink = DATEADD(dd,1,@date_ink)
		END
		FETCH NEXT FROM cur1
				INTO @contract_id, @interest
	END
CLOSE cur1
DEALLOCATE cur1
GO

UPDATE  [TechnicalParameters]
SET     [value] = 'v13.8.0.0'
WHERE   [name] = 'VERSION'
GO
