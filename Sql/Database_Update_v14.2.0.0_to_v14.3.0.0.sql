CREATE NONCLUSTERED INDEX [IX_ContractEvents_type_contract_id] ON [dbo].[ContractEvents] 
(
	[event_type] ASC,
	[contract_id] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
GO

UPDATE ce SET ce.event_type = 'RGLE'
FROM ContractEvents ce
INNER JOIN RepaymentEvents re ON re.id = ce.id
WHERE ce.event_type = 'RRLE'
    AND re.past_due_days = 0
GO
    
UPDATE ce SET ce.event_type = 'RBLE'
FROM ContractEvents ce
INNER JOIN RepaymentEvents re ON re.id = ce.id
WHERE ce.event_type = 'RRLE'
    AND re.past_due_days <> 0
GO

DELETE lam FROM LoanAccountingMovements lam
INNER JOIN AccountingRules ar ON ar.id = lam.rule_id
WHERE ar.event_attribute_id IN
    (SELECT id FROM EventAttributes WHERE event_type = 'RRLE')
GO
    
DELETE car FROM ContractAccountingRules car
INNER JOIN AccountingRules ar ON ar.id = car.id
WHERE ar.event_attribute_id IN
    (SELECT id FROM EventAttributes WHERE event_type = 'RRLE')
GO

DELETE FROM AccountingRules WHERE event_attribute_id in 
    (SELECT id FROM EventAttributes where event_type = 'RRLE')
GO

DELETE FROM EventAttributes WHERE event_type = 'RRLE'
GO

DELETE FROM EventTypes WHERE event_type = 'RRLE'
GO
 IF (not exists(SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE  TABLE_NAME = 'LateDaysRange'))
		BEGIN 
			CREATE TABLE LateDaysRange
			(
			 [Min] INT not  null, [Max] INT not  null, [Label] NVARCHAR(15) null, [Color] NVARCHAR(30) null
			)
		END
GO

INSERT INTO [Packages]
	([code]
	,[name]
	,[amount_min]
	,[amount_max]
	,[interest_rate]
	,[installment_type]
	,[grace_period]
	,[number_of_installments_min]
	,[number_of_installments_max]
	,[anticipated_total_repayment_penalties]
	,[loan_type]
	,[keep_expected_installment]
	,[charge_interest_within_grace_period]
	,[non_repayment_penalties_based_on_overdue_interest]
	,[non_repayment_penalties_based_on_initial_amount]
	,[non_repayment_penalties_based_on_olb]
	,[non_repayment_penalties_based_on_overdue_principal]
	,[fundingLine_id]
	,[currency_id]
	,[rounding_type]
	,[anticipated_partial_repayment_penalties]
	,[anticipated_partial_repayment_base]
	,[anticipated_total_repayment_base])
	VALUES('default', 'Loan Product', 1000, 1000000, 0.03, 1, 0, 3, 36, 0, 3, 1, 1, 0.001, 0,0, 0.001, 1, 1, 3, 0, 2, 2)
	
DECLARE @packageId INT
SELECT @packageId = SCOPE_IDENTITY()
INSERT INTO [PackagesClientTypes] ([client_type_id], [package_id]) SELECT id, @packageId FROM [ClientTypes]
GO

INSERT INTO [SavingProducts]
	([name]
    ,[code]
    ,[initial_amount_min]
    ,[initial_amount_max]
    ,[balance_min]
    ,[balance_max]
    ,[deposit_min]
    ,[deposit_max]
    ,[withdraw_min]
    ,[withdraw_max]
    ,[transfer_min]
    ,[transfer_max]
    ,[interest_rate]
    ,[entry_fees]
    ,[currency_id]
    ,[product_type])
      VALUES('Saving Product', 'default', 0, 100000000, 0, 100000000, 1, 100000000, 1, 100000000, 1, 100000000, 0, 0, 1, 'B')
      
DECLARE @savingProductId INT
SELECT @savingProductId = SCOPE_IDENTITY()

INSERT INTO [SavingBookProducts] 
	([id]
	,[interest_base]
	,[interest_frequency]
	,[calcul_amount_base]
	,[withdraw_fees_type]
	,[flat_withdraw_fees]
	,[transfer_fees_type]
	,[flat_transfer_fees]
	,[is_ibt_fee_flat]
	,[ibt_fee]
	,[deposit_fees]
	,[close_fees]
	,[management_fees]
	,[management_fees_freq]
	,[overdraft_fees]
	,[agio_fees]
	,[agio_fees_freq]
	,[cheque_deposit_min]
	,[cheque_deposit_max]
	,[cheque_deposit_fees]
	,[reopen_fees]
	,[use_term_deposit]
	)
	VALUES 
	(@savingProductId, 10, 10, 0, 1, 0, 1, 0, 1, 0, 0, 0, 0, 1, 0, 0, 3, 1, 100000000, 0, 0, 0) 
INSERT INTO [SavingProductsClientTypes] ([saving_product_id] ,[client_type_id]) SELECT @savingProductId, id FROM [ClientTypes]
GO

INSERT INTO [GeneralParameters]([key], [value]) VALUES('USE_MANDATORY_SAVING_ACCOUNT', 0)
GO

INSERT INTO [GeneralParameters]([key], [value]) VALUES('USE_DAILY_ACCRUAL_OF_PENALTY', 0)
GO

UPDATE  [TechnicalParameters]
SET     [value] = 'v14.3.0.0'
WHERE   [name] = 'VERSION'
GO
