// Octopus MFS is an integrated suite for managing a Micro Finance Institution: 
// clients, contracts, accounting, reporting and risk
// Copyright © 2006,2007 OCTO Technology & OXUS Development Network
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public License along
// with this program; if not, write to the Free Software Foundation, Inc.,
// 51 Franklin Street, Fifth Floor, Boston, MA 02110-1301 USA.
//
// Website: http://www.opencbs.com
// Contact: contact@opencbs.com

using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using OpenCBS.CoreDomain;
using OpenCBS.CoreDomain.Accounting;
using OpenCBS.CoreDomain.Events;
using System;
using OpenCBS.CoreDomain.Events.Loan;
using OpenCBS.CoreDomain.Events.Teller;
using OpenCBS.CoreDomain.Products;
using OpenCBS.Enums;
using OpenCBS.CoreDomain.Events.Saving;
using OpenCBS.Shared;

namespace OpenCBS.Manager.Events
{
	public class EventManager : Manager
	{
        private readonly PaymentMethodManager _paymentMethodManager;
        public EventManager(User pUser) : base(pUser) { _paymentMethodManager = new PaymentMethodManager(pUser); }

        public EventManager(string testDb) : base(testDb) { _paymentMethodManager = new PaymentMethodManager(testDb); }

        public void WriteLog(string eventCode, string eventDescription, int userId)
        {
            string insertCommandText=string.Format(@"INSERT INTO dbo.TraceUserLogs 
                                            VALUES('{0}', GETDATE(),'{1}','{2}')", eventCode, userId, eventDescription);
            using (SqlConnection conn = GetConnection())
            using (OpenCbsCommand c = new OpenCbsCommand(insertCommandText, conn))
            c.ExecuteNonQuery();
        }
   
        public List<AuditTrailEvent> SelectAuditTrailEvents(AuditTrailFilter filter)
        {
            const string q = @"SELECT * FROM dbo.AuditTrailEvents(@from, @to, @user_id, @branch_id, @types, @del)";

            List<AuditTrailEvent> retval = new List<AuditTrailEvent>();
            using (SqlConnection conn = GetConnection())
            using (OpenCbsCommand c = new OpenCbsCommand(q, conn))
            {
                c.AddParam("@from", filter.From);
                c.AddParam("@to", filter.To);
                c.AddParam("@user_id", filter.UserId);
                c.AddParam("@branch_id", filter.BranchId);
                c.AddParam("@types", filter.Types);
                c.AddParam("@del", filter.IncludeDeleted);

                using (OpenCbsReader r = c.ExecuteReader())
                {
                    if (r.Empty) return retval;

                    while (r.Read())
                    {
                        AuditTrailEvent e = new AuditTrailEvent
                                                {
                                                    Code = r.GetString("event_type"),
                                                    Description = r.GetString("description"),
                                                    UserName = r.GetString("user_name"),
                                                    UserRole = r.GetString("user_role"),
                                                    Date = r.GetDateTime("event_date"),
                                                    EntryDate = r.GetDateTime("entry_date"),
                                                    BranchName = r.GetString("branch_name")
                                                };
                        retval.Add(e);
                    }
                }
            }

            return retval;
        }

	    public EventStock SelectEvents(string eventType, int userId, DateTime beginDate, DateTime endDate)
        {
            EventStock list = new EventStock();
            string q =
                string.Format(
                    @"SELECT 
                        ISNULL(Contracts.contract_code, SavingContracts.code) as contract_code,
                        ContractEvents.contract_id,
                        ContractEvents.parent_id,
                        union_events.id AS event_id, 
                        union_events.event_type AS code, 
                        union_events.entry_date,
                        union_events.event_date AS event_date, 
                        union_events.event_type AS event_type, 
                        union_events.is_deleted AS event_deleted,
                        union_events.cancel_date,
 
                        LoanDisbursmentEvents.id AS lde_id,
                        LoanDisbursmentEvents.amount AS lde_amount, 
                        LoanDisbursmentEvents.fees AS lde_fees,
                        LoanDisbursmentEvents.payment_method_id AS lde_pm,

                        LoanEntryFeeEvents.id AS ef_id,
                        LoanEntryFeeEvents.fee AS ef_fee,
                        LoanEntryFeeEvents.disbursement_event_id,

                        WriteOffEvents.id AS woe_id, 
                        WriteOffEvents.olb AS woe_olb, 
                        WriteOffEvents.accrued_interests AS woe_accrued_interests, 
                        WriteOffEvents.accrued_penalties AS woe_accrued_penalties, 
                        WriteOffEvents.past_due_days AS woe_past_due_days, 
                        WriteOffEvents.overdue_principal AS woe_overdue_principal, 

                        ReschedulingOfALoanEvents.id AS rle_id, 
                        ReschedulingOfALoanEvents.amount AS rle_amount, 
                        ReschedulingOfALoanEvents.nb_of_maturity AS rle_maturity, 
                        ReschedulingOfALoanEvents.preferred_first_installment_date AS rle_preferred_first_installment_date,
     
                        RepaymentEvents.id AS rpe_id, 
                        RepaymentEvents.principal AS rpe_principal, 
                        RepaymentEvents.interests AS rpe_interests, 
                        RepaymentEvents.penalties AS rpe_penalties,
                        RepaymentEvents.commissions AS rpe_commissions,
                        RepaymentEvents.past_due_days AS rpe_past_due_days, 
                        RepaymentEvents.installment_number AS rpe_installment_number, 
                        RepaymentEvents.payment_method_id AS rpe_pm,
                        RepaymentEvents.calculated_penalties rpe_calculated_penalties,
                        RepaymentEvents.written_off_penalties rpe_written_off_penalties,
                        RepaymentEvents.unpaid_penalties rpe_unpaid_penalties,
     
                        LoanInterestAccruingEvents.id AS liae_id, 
                        LoanInterestAccruingEvents.interest_prepayment AS liae_interestPrepayment, 
                        LoanInterestAccruingEvents.accrued_interest AS liae_accruedInterest, 
                        LoanInterestAccruingEvents.rescheduled AS liae_rescheduled, 
                        LoanInterestAccruingEvents.installment_number AS liae_installmentNumber,

                        TrancheEvents.amount AS tranche_amount,
                        TrancheEvents.interest_rate AS tranche_interest_rate,
                        TrancheEvents.maturity AS tranche_maturity,
                        TrancheEvents.start_date AS tranche_start_date,
                        TrancheEvents.id AS tranche_id,
                        TrancheEvents.grace_period AS tranche_grace_period,
                        TrancheEvents.first_repayment_date AS tranche_first_repayment_date,

                        OverdueEvents.id AS ov_id,
                        OverdueEvents.olb AS ov_olb,
                        OverdueEvents.overdue_days AS ov_overdue_days,
                        OverdueEvents.overdue_principal AS ov_overdue_principal,
     
                        ProvisionEvents.id AS pe_id,
                        ProvisionEvents.amount AS pe_amount,
                        ProvisionEvents.overdue_days AS pe_overdue_days,

                        SavingEvents.amount AS se_amount,
                        SavingEvents.fees AS se_fees,
                        SavingEvents.related_contract_code AS se_transfer_code,
                        Users.id AS user_id, 
                        Users.deleted AS user_deleted, 
                        Users.user_name AS user_username, 
                        Users.user_pass AS user_password, 
                        Users.role_code AS user_role, 
                        Users.first_name AS user_firstname, 
                        Users.last_name AS user_lastname,
                        0 AS currency_id,
                        '' AS client_type_code,
                        0 AS branch_id,
                        '' AS contract_code
                    FROM (SELECT id, 
                                event_type, 
                                entry_date, 
                                event_date, 
                                is_deleted, 
                                user_id,
                                cancel_date
						  FROM ContractEvents 
                          UNION ALL 
						  SELECT id, 
                                code AS event_type, 
                                creation_date AS entry_date, 
                                creation_date AS event_date, 
                                deleted AS is_deleted, 
                                user_id,
                                cancel_date
						  FROM SavingEvents WHERE code NOT IN('SIAE', 'SIPE')                         
                          )
					AS union_events 
                    LEFT JOIN ContractEvents ON union_events.ID = ContractEvents.id
                    LEFT JOIN SavingEvents ON union_events.ID = SavingEvents.id
                    LEFT JOIN Contracts ON Contracts.id = ContractEvents.contract_id 
                    LEFT JOIN SavingContracts ON SavingContracts.id = SavingEvents.contract_id
                    LEFT OUTER JOIN Users ON union_events.user_id = Users.id 
                    LEFT OUTER JOIN LoanDisbursmentEvents ON ContractEvents.id = LoanDisbursmentEvents.id 
                    LEFT OUTER JOIN LoanEntryFeeEvents ON ContractEvents.id = LoanEntryFeeEvents.id                    
                    LEFT OUTER JOIN LoanInterestAccruingEvents ON ContractEvents.id = LoanInterestAccruingEvents.id 
                    LEFT OUTER JOIN RepaymentEvents ON ContractEvents.id = RepaymentEvents.id 
                    LEFT OUTER JOIN ReschedulingOfALoanEvents ON ContractEvents.id = ReschedulingOfALoanEvents.id 
                    LEFT OUTER JOIN WriteOffEvents ON ContractEvents.id = WriteOffEvents.id 
                    LEFT OUTER JOIN TrancheEvents ON ContractEvents.id = TrancheEvents.id
                    LEFT OUTER JOIN OverdueEvents ON ContractEvents.id = OverdueEvents.id
                    LEFT OUTER JOIN ProvisionEvents ON ContractEvents.id = ProvisionEvents.id
                    WHERE union_events.event_date BETWEEN @beginDate AND @endDate 
                    AND union_events.event_type LIKE @eventType {0}",
                    userId > 0 ? "AND ContractEvents.user_id = @userId ORDER BY union_events.event_date" : "ORDER BY union_events.event_date");
            using (SqlConnection conn = GetConnection())
            using (OpenCbsCommand c = new OpenCbsCommand(q, conn))
            {
                c.AddParam("@beginDate", beginDate);
                c.AddParam("@endDate", endDate);
                c.AddParam("@userId", userId);
                c.AddParam("@eventType", string.Format("%{0}%", eventType) );
                
                using (OpenCbsReader r = c.ExecuteReader())
                {
                    if (!(r == null || r.Empty)) 
                    
                    while (r.Read())
                    {
                        list.Add(ReadEvent(r));
                    }
                }
            }
            return list;
        }

	    public EventStock SelectEvents(int pContractId)
		{
            const string q = @"SELECT 
                    ContractEvents.id AS event_id,
                    ContractEvents.contract_id, 
                    ContractEvents.event_date, 
                    ContractEvents.event_type, 
                    ContractEvents.event_type AS code, 
                    ContractEvents.is_deleted AS event_deleted, 
                    ContractEvents.entry_date AS entry_date,
                    ContractEvents.comment,
                    ContractEvents.teller_id,
                    ContractEvents.parent_id,
                    ContractEvents.cancel_date,

                    LoanDisbursmentEvents.id AS lde_id,
                    LoanDisbursmentEvents.amount AS lde_amount, 
                    LoanDisbursmentEvents.fees AS lde_fees,
                    LoanDisbursmentEvents.payment_method_id AS lde_pm,
                    
                    LoanEntryFeeEvents.id AS ef_id,
                    LoanEntryFeeEvents.fee AS ef_fee,
                    LoanEntryFeeEvents.disbursement_event_id,
                    
                    CreditInsuranceEvents.id AS cie_id,
                    CreditInsuranceEvents.commission AS cie_commission,
                    CreditInsuranceEvents.principal AS cie_principal,

                    WriteOffEvents.id AS woe_id, 
                    WriteOffEvents.olb AS woe_olb, 
                    WriteOffEvents.accrued_interests AS woe_accrued_interests, 
                    WriteOffEvents.accrued_penalties AS woe_accrued_penalties, 
                    WriteOffEvents.past_due_days AS woe_past_due_days, 
                    WriteOffEvents.overdue_principal AS woe_overdue_principal, 

                    ReschedulingOfALoanEvents.id AS rle_id, 
                    ReschedulingOfALoanEvents.amount AS rle_amount, 
                    ReschedulingOfALoanEvents.nb_of_maturity AS rle_maturity, 
                    ReschedulingOfALoanEvents.preferred_first_installment_date AS rle_preferred_first_installment_date, 

                    RepaymentEvents.id AS rpe_id, 
                    RepaymentEvents.principal AS rpe_principal, 
                    RepaymentEvents.interests AS rpe_interests, 
                    RepaymentEvents.penalties AS rpe_penalties,
                    RepaymentEvents.commissions AS rpe_commissions,
                    RepaymentEvents.past_due_days AS rpe_past_due_days, 
                    RepaymentEvents.installment_number As rpe_installment_number, 
                    RepaymentEvents.payment_method_id AS rpe_pm,
                    RepaymentEvents.calculated_penalties rpe_calculated_penalties,
                    RepaymentEvents.written_off_penalties rpe_written_off_penalties,
                    RepaymentEvents.unpaid_penalties rpe_unpaid_penalties,

                    LoanInterestAccruingEvents.id AS liae_id, 
                    LoanInterestAccruingEvents.interest_prepayment AS liae_interestPrepayment, 
                    LoanInterestAccruingEvents.accrued_interest AS liae_accruedInterest, 
                    LoanInterestAccruingEvents.rescheduled AS liae_rescheduled, 
                    LoanInterestAccruingEvents.installment_number AS liae_installmentNumber,

                    TrancheEvents.amount AS tranche_amount,
                    TrancheEvents.interest_rate AS tranche_interest_rate,
                    TrancheEvents.maturity AS tranche_maturity,
                    TrancheEvents.start_date AS tranche_start_date,
                    TrancheEvents.id AS tranche_id,
                    TrancheEvents.grace_period AS tranche_grace_period,
                    TrancheEvents.first_repayment_date AS tranche_first_repayment_date,

                    OverdueEvents.id AS ov_id,
                    OverdueEvents.olb AS ov_olb,
                    OverdueEvents.overdue_days AS ov_overdue_days,
                    OverdueEvents.overdue_principal AS ov_overdue_principal,
 
                    ProvisionEvents.id AS pe_id,
                    ProvisionEvents.amount AS pe_amount,
                    ProvisionEvents.overdue_days AS pe_overdue_days,
                    
                    LoanPenaltyAccrualEvents.id AS lpae_id,
                    LoanPenaltyAccrualEvents.penalty AS lpae_penalty,

                    AccrualInterestLoanEvents.id AS aile_id,
                    AccrualInterestLoanEvents.interest AS aile_interest,

                    LoanTransitionEvents.id AS glll_id,
                    LoanTransitionEvents.amount AS glll_amount,

                    Users.id AS user_id, 
                    Users.deleted AS user_deleted, 
                    Users.user_name AS user_username, 
                    Users.user_pass AS user_password, 
                    Users.role_code AS user_role, 
                    Users.first_name AS user_firstname, 
                    Users.last_name AS user_lastname, 
                    0 AS currency_id,
                    '' AS client_type_code,
                    0 AS branch_id,
                    '' AS contract_code,
                    CAST(0 AS bit) AS is_pivot, 
                    CAST(0 AS bit) AS is_swapped, 
                    '' AS currency_code,
                    0 AS product_id
                    FROM ContractEvents 
                    INNER JOIN Users ON ContractEvents.user_id = Users.id 
                    LEFT OUTER JOIN LoanDisbursmentEvents ON ContractEvents.id = LoanDisbursmentEvents.id 
                    LEFT OUTER JOIN LoanEntryFeeEvents ON ContractEvents.id = LoanEntryFeeEvents.id
                    LEFT OUTER JOIN CreditInsuranceEvents  ON ContractEvents.id = CreditInsuranceEvents.id
                    LEFT OUTER JOIN LoanInterestAccruingEvents ON ContractEvents.id = LoanInterestAccruingEvents.id 
                    LEFT OUTER JOIN RepaymentEvents ON ContractEvents.id = RepaymentEvents.id 
                    LEFT OUTER JOIN ReschedulingOfALoanEvents ON ContractEvents.id = ReschedulingOfALoanEvents.id 
                    LEFT OUTER JOIN WriteOffEvents ON ContractEvents.id = WriteOffEvents.id 
                    LEFT OUTER JOIN TrancheEvents ON ContractEvents.id = TrancheEvents.id
                    LEFT OUTER JOIN OverdueEvents ON ContractEvents.id = OverdueEvents.id
                    LEFT OUTER JOIN ProvisionEvents ON ContractEvents.id = ProvisionEvents.id
                    LEFT OUTER JOIN LoanPenaltyAccrualEvents ON ContractEvents.id = LoanPenaltyAccrualEvents.id
                    LEFT OUTER JOIN AccrualInterestLoanEvents ON ContractEvents.id = AccrualInterestLoanEvents.id
                    LEFT OUTER JOIN LoanTransitionEvents ON ContractEvents.id = LoanTransitionEvents.id
                    WHERE (ContractEvents.contract_id = @id)
                    ORDER BY ContractEvents.id";
            using (SqlConnection conn = GetConnection())
            using(OpenCbsCommand c = new OpenCbsCommand(q, conn))
            {
                c.AddParam("@id", pContractId);
                using (OpenCbsReader r = c.ExecuteReader())
                {
                    if(r == null || r.Empty) return new EventStock();
                    EventStock list = new EventStock();
                    while (r.Read())
                    {
                        list.Add(ReadEvent(r));
                    }
                    return list;
                }
            }
		}

        public List<TellerEvent> SelectTellerEventsForClosure(DateTime beginDate, DateTime endDate)
        {
            const string sql =
                        @"SELECT   te.[id]
                                  ,te.[teller_id]
                                  ,te.[event_code]
                                  ,te.[amount]
                                  ,te.[date]
                                  ,te.[is_exported]
                                  ,t.branch_id
                                  ,t.currency_id
                              FROM [dbo].[TellerEvents] te
                              INNER JOIN Tellers t on te.teller_id = t.id
                              WHERE te.date BETWEEN @beginDate AND @endDate
                                    AND te.[is_exported] = 0
                                    ";
            var tellerEvents = new List<TellerEvent>();
            using (SqlConnection connection = GetConnection())
            using (OpenCbsCommand cmd = new OpenCbsCommand(sql, connection))
            {
                cmd.AddParam("@beginDate", beginDate);
                cmd.AddParam("@endDate", endDate);
                using (OpenCbsReader r = cmd.ExecuteReader())
                {
                    if (r.Empty) return tellerEvents;
                    while (r.Read())
                    {
                        var tellerEvent = GetTellerEvent(r.GetString("event_code"));
                        tellerEvent.Id = r.GetInt("id");
                        tellerEvent.TellerId = r.GetInt("teller_id");
                        tellerEvent.Code = r.GetString("event_code");
                        tellerEvent.Amount = r.GetMoney("amount");
                        tellerEvent.Date = r.GetDateTime("date");
                        tellerEvent.Currency = new Currency { Id = r.GetInt("currency_id") };
                        tellerEvent.Branch = new Branch { Id = r.GetInt("branch_id") };
                        tellerEvent.ContracId = 0;
                        tellerEvents.Add(tellerEvent);
                    }
                }
            }
            return tellerEvents;
        }

        private static TellerEvent GetTellerEvent(string code)
        {
            TellerEvent e;
            switch (code)
            {
                case OTellerEvents.CashIn:
                    e = new TellerCashInEvent();
                    break;
                
                case OTellerEvents.CashOut:
                    e = new TellerCashOutEvent();
                    break;

                case OTellerEvents.OpenDay:
                    e = new OpenOfDayAmountEvent();
                    break;

                case OTellerEvents.CloseDay:
                    e = new CloseOfDayAmountEvent();
                    break;

                case OTellerEvents.OpenDayPositiveDifference:
                    e = new OpenAmountPositiveDifferenceEvent();
                    break;

                case OTellerEvents.OpenDayNegativeDifference:
                    e = new OpenAmountNegativeDifferenceEvent();
                    break;

                case OTellerEvents.CloseDayPositiveDifference:
                    e = new CloseAmountPositiveDifferenceEvent();
                    break;

                case OTellerEvents.CloseDayNegativeDifference:
                    e = new CloseAmountNegativeDifferenceEvent();
                    break;

                default:
                    Debug.Fail("Failed to create teller events");
                    throw new Exception();
            }

            return e;
        }

        public EventStock SelectEventsForClosure(DateTime beginDate, DateTime endDate, Branch branch)
        {
            const string q = @"SELECT 
                    ContractEvents.id AS event_id, 
                    ContractEvents.contract_id,
                    ContractEvents.event_date, 
                    ContractEvents.event_type, 
                    ContractEvents.event_type AS code, 
                    ContractEvents.is_deleted AS event_deleted, 
                    ContractEvents.entry_date AS entry_date,
                    ContractEvents.comment,
                    ContractEvents.teller_id,
                    ContractEvents.parent_id,
                    ContractEvents.cancel_date,

                    LoanDisbursmentEvents.id AS lde_id,
                    LoanDisbursmentEvents.amount AS lde_amount, 
                    LoanDisbursmentEvents.fees AS lde_fees,
                    LoanDisbursmentEvents.payment_method_id AS lde_pm,

                    LoanEntryFeeEvents.id AS ef_id,
                    LoanEntryFeeEvents.fee AS ef_fee,
                    LoanEntryFeeEvents.disbursement_event_id,
                    
                    CreditInsuranceEvents.id AS cie_id,
                    CreditInsuranceEvents.commission AS cie_commission,
                    CreditInsuranceEvents.principal AS cie_principal,

                    WriteOffEvents.id AS woe_id, 
                    WriteOffEvents.olb AS woe_olb, 
                    WriteOffEvents.accrued_interests AS woe_accrued_interests, 
                    WriteOffEvents.accrued_penalties AS woe_accrued_penalties, 
                    WriteOffEvents.past_due_days AS woe_past_due_days, 
                    WriteOffEvents.overdue_principal AS woe_overdue_principal, 

                    ReschedulingOfALoanEvents.id AS rle_id, 
                    ReschedulingOfALoanEvents.amount AS rle_amount, 
                    ReschedulingOfALoanEvents.nb_of_maturity AS rle_maturity, 
                    ReschedulingOfALoanEvents.preferred_first_installment_date AS rle_preferred_first_installment_date, 

                    RepaymentEvents.id AS rpe_id, 
                    RepaymentEvents.principal AS rpe_principal, 
                    RepaymentEvents.interests AS rpe_interests, 
                    RepaymentEvents.penalties AS rpe_penalties,
                    RepaymentEvents.commissions AS rpe_commissions,
                    RepaymentEvents.past_due_days AS rpe_past_due_days, 
                    RepaymentEvents.installment_number As rpe_installment_number, 
                    RepaymentEvents.payment_method_id AS rpe_pm,
                    RepaymentEvents.calculated_penalties rpe_calculated_penalties,
                    RepaymentEvents.written_off_penalties rpe_written_off_penalties,
                    RepaymentEvents.unpaid_penalties rpe_unpaid_penalties,

                    LoanInterestAccruingEvents.id AS liae_id, 
                    LoanInterestAccruingEvents.interest_prepayment AS liae_interestPrepayment, 
                    LoanInterestAccruingEvents.accrued_interest AS liae_accruedInterest, 
                    LoanInterestAccruingEvents.rescheduled AS liae_rescheduled, 
                    LoanInterestAccruingEvents.installment_number AS liae_installmentNumber,

                    TrancheEvents.amount AS tranche_amount,
                    TrancheEvents.interest_rate AS tranche_interest_rate,
                    TrancheEvents.maturity AS tranche_maturity,
                    TrancheEvents.start_date AS tranche_start_date,
                    TrancheEvents.id AS tranche_id,

                    OverdueEvents.id AS ov_id,
                    OverdueEvents.olb AS ov_olb,
                    OverdueEvents.overdue_days AS ov_overdue_days,
                    OverdueEvents.overdue_principal AS ov_overdue_principal,

                    ProvisionEvents.id AS pe_id,
                    ProvisionEvents.amount AS pe_amount,
                    ProvisionEvents.overdue_days AS pe_overdue_days,

                    Users.id AS user_id, 
                    Users.deleted AS user_deleted, 
                    Users.user_name AS user_username, 
                    Users.user_pass AS user_password, 
                    Users.role_code AS user_role, 
                    Users.first_name AS user_firstname, 
                    Users.last_name AS user_lastname,
                    Packages.currency_id,
                    t.client_type_code,
                    t.branch_id,
                    con.contract_code,
                    Currencies.is_pivot, 
                    Currencies.is_swapped, 
                    Currencies.code AS currency_code,
                    command.package_id AS product_id
                    FROM ContractEvents 
                    INNER JOIN Users ON ContractEvents.user_id = Users.id
                    INNER JOIN dbo.Credit command ON contract_id = command.id
                    INNER JOIN dbo.Packages ON command.package_id = dbo.Packages.id
                    INNER JOIN Currencies ON Packages.currency_id = Currencies.id
                    INNER JOIN Contracts con ON con.id = command.id
                    INNER JOIN Projects pr ON pr.id = con.project_id
                    INNER JOIN Tiers t ON t.id = pr.tiers_id
                    
                    LEFT OUTER JOIN LoanDisbursmentEvents ON ContractEvents.id = LoanDisbursmentEvents.id 
                    LEFT OUTER JOIN LoanEntryFeeEvents ON ContractEvents.id = LoanEntryFeeEvents.id 
                    LEFT OUTER JOIN LoanInterestAccruingEvents ON ContractEvents.id = LoanInterestAccruingEvents.id 
                    LEFT OUTER JOIN RepaymentEvents ON ContractEvents.id = RepaymentEvents.id 
                    LEFT OUTER JOIN ReschedulingOfALoanEvents ON ContractEvents.id = ReschedulingOfALoanEvents.id 
                    LEFT OUTER JOIN WriteOffEvents ON ContractEvents.id = WriteOffEvents.id 
                    LEFT OUTER JOIN TrancheEvents ON ContractEvents.id = TrancheEvents.id
                    LEFT OUTER JOIN OverdueEvents ON ContractEvents.id = OverdueEvents.id
                    LEFT OUTER JOIN ProvisionEvents ON ContractEvents.id = ProvisionEvents.id
                    LEFT OUTER JOIN CreditInsuranceEvents ON ContractEvents.id = CreditInsuranceEvents.id
                    WHERE 
                      ContractEvents.is_exported = 0 
                      AND ContractEvents.is_deleted = 0
                      AND EXISTS(SELECT event_type 
                                 FROM dbo.AccountingRules
                                 GROUP BY event_type)
                      AND ContractEvents.event_date BETWEEN @beginDate AND @endDate
                      AND (t.branch_id = @branch_id OR @branch_id = 0)
                      AND (LoanDisbursmentEvents.id IS NOT NULL 
                           OR RepaymentEvents.id IS NOT NULL
                           OR ReschedulingOfALoanEvents.id IS NOT NULL
                           OR WriteOffEvents.id IS NOT NULL
                           OR OverdueEvents.id IS NOT NULL
                           OR ProvisionEvents.id IS NOT NULL
                           OR TrancheEvents.id IS NOT NULL
                           OR LoanInterestAccruingEvents.id IS NOT NULL
                           OR LoanEntryFeeEvents.id IS NOT NULL
                           OR CreditInsuranceEvents.id IS NOT NULL)
                    ORDER BY ContractEvents.id";
            using (SqlConnection conn = GetConnection())
            {
                using (OpenCbsCommand c = new OpenCbsCommand(q, conn))
                {
                    c.AddParam("@beginDate", beginDate);
                    c.AddParam("@endDate", endDate);
                    c.AddParam("@branch_id", branch.Id);

                    using (OpenCbsReader r = c.ExecuteReader())
                    {
                        if (r == null || r.Empty) return new EventStock();
                        EventStock list = new EventStock();

                        while (r.Read())
                        {
                            list.Add(ReadEvent(r));
                        }

                        return list;
                    }
                }
            }
        }

        public List<EventType> SelectEventTypesForAccounting()
        {
            const string q = @"SELECT id, event_type, description
                               FROM dbo.EventTypes
                               WHERE accounting = 1
                               ORDER BY sort_order";

            List<EventType> list = new List<EventType>();
            using (SqlConnection conn = GetConnection())
            using (OpenCbsCommand c = new OpenCbsCommand(q, conn))
            using (OpenCbsReader r = c.ExecuteReader())
            {
                if (r.Empty) return list;

                while (r.Read())
                {
                    EventType et = new EventType
                    {
                        Id = r.GetInt("id"), 
                        Description = r.GetString("description"),
                        EventCode = r.GetString("event_type")
                    };
                    list.Add(et);
                }

                return list;
            }
        }

        public List<EventType> SelectEventTypes()
        {
            const string q = @"SELECT id, event_type, description
                               FROM dbo.EventTypes
                               ORDER BY event_type";

            List<EventType> list = new List<EventType>();
            using (SqlConnection conn = GetConnection())
            using (OpenCbsCommand c = new OpenCbsCommand(q, conn))
            using (OpenCbsReader r = c.ExecuteReader())
            {
                if (r.Empty) return list;

                while (r.Read())
                {
                    EventType et = new EventType
                    {
                        Id = r.GetInt("id"),
                        Description = r.GetString("description"),
                        EventCode = r.GetString("event_type")
                    };
                    list.Add(et);
                }

                return list;
            }
        }

        public List<EventAttribute> SelectEventAttributes(string eventType)
        {
            const string q = @"SELECT [id], [event_type], [name]
                                     FROM EventAttributes
                                     WHERE [event_type] = @event_type";
            using (SqlConnection conn = GetConnection())
            using (OpenCbsCommand c = new OpenCbsCommand(q, conn))
            {
                c.AddParam("@event_type", eventType);

                using (OpenCbsReader r = c.ExecuteReader())
                {
                    if (r == null || r.Empty) return new List<EventAttribute>();
                    List<EventAttribute> list = new List<EventAttribute>();
                    while (r.Read())
                    {
                        list.Add(new EventAttribute
                        {
                            Id = r.GetInt("id"),
                            Name = r.GetString("name"),
                            EventCode = r.GetString("event_type")
                        });
                    }

                    return list;
                }
            }
        }

        public EventType SelectEventTypeByEventType(string eventType)
        {
            const string q = @"SELECT id, event_type, description
                                     FROM EventTypes
                                     WHERE event_type = @event_type";
            using (SqlConnection conn = GetConnection())
            using (OpenCbsCommand c = new OpenCbsCommand(q, conn))
            {
                c.AddParam("@event_type", eventType);

                using (OpenCbsReader r = c.ExecuteReader())
                {
                    if (r == null || r.Empty) return new EventType();
                    EventType evntType = new EventType();
                    while (r.Read())
                    {
                        evntType = new EventType
                                     {
                                         Id = r.GetInt("id"),
                                         Description = r.GetString("description"),
                                         EventCode = r.GetString("event_type")
                                     };
                    }

                    return evntType;
                }
            }
        }

        public EventAttribute SelectEventAttributeByCode(string name)
        {
            const string q = @"SELECT id, event_type, name
                                     FROM EventAttributes
                                     WHERE name = @name";
            using (SqlConnection conn = GetConnection())
            using (OpenCbsCommand c = new OpenCbsCommand(q, conn))
            {
                c.AddParam("@name", name);

                using (OpenCbsReader r = c.ExecuteReader())
                {
                    if (r == null || r.Empty) return new EventAttribute();
                    EventAttribute eventType = new EventAttribute();
                    while (r.Read())
                    {
                        eventType = new EventAttribute
                        {
                            Id = r.GetInt("id"),
                            Name = r.GetString("name"),
                            EventCode = r.GetString("event_type")
                        };
                    }

                    return eventType;
                }
            }
        }

        public int AddLoanEventHead(Event pEvent, int pContractId, SqlTransaction pSqlTransac)
        {
            const string q = @"INSERT INTO [ContractEvents]
                                     ([event_type], 
                                      [contract_id], 
                                      [event_date], 
                                      [user_id], 
                                      [is_deleted], 
                                      [teller_id],
                                      [parent_id],
                                      [comment])
			                         VALUES
                                      (@eventType, 
                                       @contractId, 
                                       @eventDate, 
                                       @userId, 
                                       @deleted, 
                                       @tellerId,
                                       @parentId,
                                       @comment)
                                     SELECT SCOPE_IDENTITY()";

            using (OpenCbsCommand c = new OpenCbsCommand(q, pSqlTransac.Connection, pSqlTransac))
            {
                SetLoanEvent(c, pEvent, pContractId);
                return int.Parse(c.ExecuteScalar().ToString());
            }
        }

        public int AddLoanEvent(LoanDisbursmentEvent evnt, int contractId)
        {
            using (SqlConnection connection = GetConnection())
            using (SqlTransaction transaction = connection.BeginTransaction())
            {
                try
                {
                    int result = AddLoanEvent(evnt, contractId, transaction);
                    transaction.Commit();
                    return result;   
                }
                catch(Exception)
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

	    public int AddLoanEvent(LoanDisbursmentEvent evnt, int contractId, SqlTransaction transaction)
		{
            evnt.Id = AddLoanEventHead(evnt, contractId, transaction);

			const string q = @"INSERT INTO [LoanDisbursmentEvents]([id], [amount], [fees], [interest], [payment_method_id]) 
                                    VALUES(@id, @amount, @fees, @interest, @payment_method_id)";

            using(OpenCbsCommand c = new OpenCbsCommand(q, transaction.Connection, transaction))
            {
                GetLoanDisbursmentEvent(evnt, c);
                c.ExecuteNonQuery();
            }
            return evnt.Id;
		}

        public void AddLoanEntryFeesEvent(LoanEntryFeeEvent pEvent, SqlTransaction pSqlTransac)
        {
            const string q = @"INSERT INTO [LoanEntryFeeEvents]
                                    (
                                         [id]
                                        ,[fee]
                                        ,[disbursement_event_id]
                                    ) 
                                    VALUES
                                    ( 
                                        @id
                                        ,@fee
                                        ,@disbursement_event_id
                                    )";
            using (OpenCbsCommand c = new OpenCbsCommand(q, pSqlTransac.Connection, pSqlTransac))
            {
                SetEntryFeesEvent(pEvent, c);
                c.ExecuteNonQuery();
            }
        }

        private static void SetEntryFeesEvent(LoanEntryFeeEvent pEvent, OpenCbsCommand c)
        {
            c.AddParam("@id", pEvent.Id);
            c.AddParam("@fee", pEvent.Fee);
            c.AddParam("@disbursement_event_id", pEvent.DisbursementEventId);
        }

        public void AddCreditInsuranceEvent(CreditInsuranceEvent pEvent, SqlTransaction pSqlTransac)
        {
            const string q = @"
                        INSERT INTO [dbo].[CreditInsuranceEvents]
                       ([id]
                       ,[commission]
                        ,[principal])
                         VALUES
                       (@id
                       ,@commission
                       ,@principal)";
            using (OpenCbsCommand c = new OpenCbsCommand(q, pSqlTransac.Connection, pSqlTransac))
            {
                SetCreditInsuranceEvent(pEvent, c);
                c.ExecuteNonQuery();
            }
        }

	    private void SetCreditInsuranceEvent(CreditInsuranceEvent pEvent, OpenCbsCommand c)
	    {
	        c.AddParam("@id", pEvent.Id);
            c.AddParam("@commission", pEvent.Commission.Value);
            c.AddParam("@principal", pEvent.Principal.Value);
	    }

        public void AddLoanEvent(LoanCloseEvent evnt, int contractId, SqlTransaction transaction)
        {
            evnt.Id = AddLoanEventHead(evnt, contractId, transaction);
        }

        public void AddLoanEvent(LoanValidationEvent evnt, int contractId, SqlTransaction transaction)
        {
            evnt.Id=AddLoanEventHead(evnt, contractId, transaction);
        }

        public void AddLoanEvent(RepaymentEvent evnt, int contractId, SqlTransaction transaction)
		{
            const string q = @"INSERT INTO [RepaymentEvents]
                                       ([id],
                                        [past_due_days], 
                                        [principal], 
                                        [interests], 
                                        [installment_number], 
                                        [commissions], 
                                        [penalties],
                                        [payment_method_id],
                                        [calculated_penalties],
                                        [written_off_penalties],
                                        [unpaid_penalties]) 
                                     VALUES
                                       (@id, 
                                        @pastDueDays, 
                                        @principal, 
                                        @interests, 
                                        @installmentNumber, 
                                        @commissions, 
                                        @penalties,
                                        @payment_method_id,
                                        @calculated_penalties,
                                        @written_off_penalties,
                                        @unpaid_penalties)
                                     ";

            using (OpenCbsCommand c = new OpenCbsCommand(q, transaction.Connection, transaction))
            {
                SetLoanRepaymentEvent(evnt, c);
                c.ExecuteNonQuery();
            }
		}

        public void AddLoanEvent(TrancheEvent trancheEvent, int contractId, SqlTransaction transaction)
        {
            trancheEvent.Id = AddLoanEventHead(trancheEvent, contractId, transaction);

            const string query = @"
                INSERT INTO dbo.TrancheEvents
                (
                    [id],
                    [interest_rate],
                    [amount],
                    [maturity],
                    [start_date],
                    [applied_new_interest],
                    [started_from_installment],
                    first_repayment_date,
                    grace_period
                )
                VALUES
                (
                    @id, 
                    @interest_rate, 
                    @amount, 
                    @maturity, 
                    @start_date, 
                    @applied_new_interest, 
                    @started_from_installment,
                    @first_repayment_date,
                    @grace_period
                )
            ";

            using (var command = new OpenCbsCommand(query, transaction.Connection, transaction))
            {
                SetLoanTrancheEvent(trancheEvent, command);
                command.ExecuteNonQuery();
            }
        }

        public void AddLoanEvent(RescheduleLoanEvent rescheduleLoanEvent, int contractId)
        {
            using (SqlConnection connection = GetConnection())
            using (SqlTransaction transaction = connection.BeginTransaction())
            {
                try
                {
                    AddLoanEvent(rescheduleLoanEvent, contractId, transaction);
                    transaction.Commit();
                }
                catch (Exception)
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

	    public void AddLoanEvent(RescheduleLoanEvent rescheduleLoanEvent, int contractId, SqlTransaction transaction)
		{
            rescheduleLoanEvent.Id = AddLoanEventHead(rescheduleLoanEvent, contractId, transaction);

            const string q = @"
                INSERT INTO [ReschedulingOfALoanEvents]
                (
                    [id], 
                    [amount], 
                    [nb_of_maturity], 
                    [interest], 
                    [grace_period], 
                    [charge_interest_during_grace_period], 
                    [preferred_first_installment_date]
                ) 
                VALUES
                (
                    @id, 
                    @amount, 
                    @maturity,
                    @interest, 
                    @gracePeriod, 
                    @chargeInterestDuringGracePeriod, 
                    @preferredFirstInstallmentDate
                )
            ";

            using(OpenCbsCommand c = new OpenCbsCommand(q, transaction.Connection, transaction))
            {
                SetLoanReschedulingEvent(rescheduleLoanEvent, c);
                c.ExecuteNonQuery();
            }
		}

	    public void AddLoanEvent(ProvisionEvent provisionEvent, int contractId, SqlTransaction transaction)
        {
            provisionEvent.Id = AddLoanEventHead(provisionEvent, contractId, transaction);

            const string q = @"INSERT INTO [ProvisionEvents](
                                       id,
                                       amount,
                                       rate,
                                       overdue_days) 
                                     VALUES(@id, @amount, @rate, @overdue_days)";

            using (OpenCbsCommand c = new OpenCbsCommand(q, transaction.Connection, transaction))
            {
                c.AddParam("@id", provisionEvent.Id);
                c.AddParam("@amount", provisionEvent.Amount);
                c.AddParam("@rate", provisionEvent.Rate);
                c.AddParam("@overdue_days", provisionEvent.OverdueDays);
                c.ExecuteNonQuery();
            }
        }

        public void AddLoanEvent(OverdueEvent overdueEvent, int contractId, SqlTransaction transaction)
        {
            overdueEvent.Id = AddLoanEventHead(overdueEvent, contractId, transaction);

            const string q = @"INSERT INTO [OverdueEvents](
                                       [id], 
                                       [olb], 
                                       [overdue_days],
                                       [overdue_principal]) 
                                     VALUES(@id, @olb, @overdue_days, @overdue_principal)";

            using (OpenCbsCommand c = new OpenCbsCommand(q, transaction.Connection, transaction))
            {
                c.AddParam("@id", overdueEvent.Id);
                c.AddParam("@olb", overdueEvent.OLB);
                c.AddParam("@overdue_days", overdueEvent.OverdueDays);
                c.AddParam("@overdue_principal", overdueEvent.OverduePrincipal);
                c.ExecuteNonQuery();
            }
        }

        public void AddLoanEvent(WriteOffEvent writeOffEvent, int contractId)
        {
            using (SqlConnection connection = GetConnection())
            using (SqlTransaction transaction = connection.BeginTransaction())
            {
                try
                {
                    AddLoanEvent(writeOffEvent, contractId, transaction);
                    transaction.Commit();
                }
                catch (Exception)
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

	    public void AddLoanEvent(WriteOffEvent writeOffEvent, int contractId, SqlTransaction transaction)
		{
            writeOffEvent.Id = AddLoanEventHead(writeOffEvent, contractId, transaction);

            const string q = @"INSERT INTO [WriteOffEvents]
                                       ([id], 
                                        [olb], 
                                        [accrued_interests], 
                                        [accrued_penalties], 
                                        [past_due_days],
                                        [overdue_principal],
                                        [write_off_method])
                                     VALUES(@id, @olb, @accruedInterests, @accruedPenalties, @pastDueDays, @overdue_principal, @writeOffMethod)
                                    SELECT SCOPE_IDENTITY()";

            using(var c = new OpenCbsCommand(q, transaction.Connection, transaction))
            {
                SetLoanWriteOffEvent(writeOffEvent, c);
                c.ExecuteNonQuery();
            }
		}

        public void AddLoanEvent(AccruedInterestEvent pEvent, int contractId)
        {
            using (SqlConnection connection = GetConnection())
            using (SqlTransaction transaction = connection.BeginTransaction())
            {
                try
                {
                    AddLoanEvent(pEvent, contractId, transaction);
                    transaction.Commit();
                }
                catch (Exception)
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        public void AddLoanEvent(AccruedInterestEvent pEvent, int contractId, SqlTransaction transac)
        {
            pEvent.Id = AddLoanEventHead(pEvent, contractId, transac);

            const string q = @"INSERT INTO [LoanInterestAccruingEvents](
                                        [id], 
                                        [interest_prepayment],
                                        [accrued_interest],
                                        [rescheduled], 
                                        [installment_number]) 
                                     VALUES(@id, 
                                        @interestPrepayment, 
                                        @accruedInterest, 
                                        @rescheduled, 
                                        @installmentNumber)";

            using (OpenCbsCommand c = new OpenCbsCommand(q, transac.Connection, transac))
            {
                SetLoanInterestAccruingEvent(pEvent, c);
                c.ExecuteNonQuery();
            }
        }

        public void AddLoanEvent(RegEvent pEvent, int contractId)
        {
            using (SqlConnection connection = GetConnection())
            using (SqlTransaction transaction = connection.BeginTransaction())
            {
                try
                {
                    AddLoanEvent(pEvent, contractId, transaction);
                    transaction.Commit();
                }
                catch (Exception)
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

	    public void AddLoanEvent(RegEvent pEvent, int contractId, SqlTransaction transaction)
		{
            AddLoanEventHead(pEvent, contractId, transaction);
		}

        public void AddLoanEvent(ManualScheduleChangeEvent pEvent, int contractId, SqlTransaction transaction)
        {
            pEvent.Id = AddLoanEventHead(pEvent, contractId, transaction);
        }

        public void AddLoanEvent(LoanPenaltyAccrualEvent pEvent, int contractId, SqlTransaction transaction)
        {
            pEvent.Id = AddLoanEventHead(pEvent, contractId, transaction);
            const string q = @"INSERT INTO [LoanPenaltyAccrualEvents](
                                        [id], 
                                        [penalty]) 
                                     VALUES(@id, 
                                        @penalty)";

            using (var c = new OpenCbsCommand(q, transaction.Connection, transaction))
            {
                SetLoanPenaltyAccrualEvent(c, pEvent, pEvent.Penalty);
                c.ExecuteNonQuery();
            }
        }

        public void AddLoanEvent(LoanTransitionEvent pEvent, int contractId, SqlTransaction transaction)
        {
            pEvent.Id = AddLoanEventHead(pEvent, contractId, transaction);
            const string q = @"INSERT INTO [LoanTransitionEvents](
                                        [id], 
                                        [amount]) 
                                     VALUES(@id, 
                                        @amount)";

            using (var c = new OpenCbsCommand(q, transaction.Connection, transaction))
            {
                SetLoanTransitionEvent(c, pEvent, pEvent.Amount);
                c.ExecuteNonQuery();
            }
        }

        public void AddLoanEvent(LoanInterestAccrualEvent pEvent, int contractId, SqlTransaction transaction)
        {
            pEvent.Id = AddLoanEventHead(pEvent, contractId, transaction);
            const string q = @"INSERT INTO [AccrualInterestLoanEvents](
                                        [id], 
                                        [interest]) 
                                     VALUES(@id, 
                                        @interest)";

            using (var c = new OpenCbsCommand(q, transaction.Connection, transaction))
            {
                SetLoanInterestAccrualEvent(c, pEvent, pEvent.Interest);
                c.ExecuteNonQuery();
            }
        }

        public void AddTellerEvent(TellerEvent tellerEvent, SqlTransaction sqlTransaction)
        {
            const string sql =
                @"INSERT INTO [dbo].[TellerEvents]
                       ([teller_id]
                       ,[event_code]
                       ,[amount]
                       ,[date]
                       ,[description])
                 VALUES
		               (
		   	            @teller_id
		               ,@event_code 
		               ,@amount 
		               ,@date
                       ,@description
                       )";
            using (OpenCbsCommand cmd  = new OpenCbsCommand(sql, sqlTransaction.Connection, sqlTransaction))
            {
                cmd.AddParam("@teller_id", tellerEvent.TellerId);
                cmd.AddParam("@event_code", tellerEvent.Code);
                cmd.AddParam("@amount", tellerEvent.Amount);
                cmd.AddParam("@date", tellerEvent.Date);
                cmd.AddParam("@description", tellerEvent.Description);
                cmd.ExecuteNonQuery();
            }
        }

        public void UpdateCommentForLoanEvent(Event pEvent, SqlTransaction pSqlTransac)
        {
            const string q = @"UPDATE [ContractEvents] 
                                     SET [comment] = @comment 
                                     WHERE id = @id";

            using (OpenCbsCommand c = new OpenCbsCommand(q, pSqlTransac.Connection, pSqlTransac))
            {
                c.AddParam("@id", pEvent.Id);
                c.AddParam("@comment", pEvent.Comment);
                c.ExecuteNonQuery();
            }
        }

        public void DeleteLoanEvent(Event pEvent)
        {
            using (SqlConnection connection = GetConnection())
            using (SqlTransaction transaction = connection.BeginTransaction())
            {
                try
                {
                    DeleteLoanEvent(pEvent, transaction);
                    transaction.Commit();
                }
                catch (Exception)
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

	    public void DeleteLoanEvent(Event pEvent, SqlTransaction pSqlTransac)
        {
            const string q = @"UPDATE [ContractEvents] 
                               SET 
                                 [is_deleted] = 1, 
                                 [is_exported] = 0,
                                 [cancel_date] = @cancel_date
                               WHERE id = @id 
                                 OR parent_id IN (SELECT parent_id FROM [ContractEvents] WHERE id = @id)
                                 OR id IN (SELECT parent_id FROM [ContractEvents] WHERE id = @id)";

            using (OpenCbsCommand c = new OpenCbsCommand(q, pSqlTransac.Connection, pSqlTransac))
            {
                c.AddParam("@id", pEvent.Id);
                c.AddParam("@cancel_date", pEvent.CancelDate);
                c.ExecuteNonQuery();
            }
        }

        public void ExportTellerEvent(int eventId, SqlTransaction sqlTransaction)
        {
            const string sql = 
                      @"UPDATE [dbo].[TellerEvents]
                        SET [is_exported] = 1
                        WHERE id = @id";
            using (OpenCbsCommand cmd = new OpenCbsCommand(sql, sqlTransaction.Connection, sqlTransaction))
            {
                cmd.AddParam("@id", eventId);
                cmd.ExecuteNonQuery();
            }
        }

        public void ExportEvent(int eventId, SqlTransaction pSqlTransac)
        {
            const string q = @"UPDATE [ContractEvents] 
                               SET [is_exported] = 1 
                               WHERE id = @id";

            using (OpenCbsCommand c = new OpenCbsCommand(q, pSqlTransac.Connection, pSqlTransac))
            {
                c.AddParam("@id", eventId);
                c.ExecuteNonQuery();
            }
        }

        private static void SetLoanWriteOffEvent(WriteOffEvent pEvent, OpenCbsCommand c)
        {
            c.AddParam("@id", pEvent.Id);
            c.AddParam("@olb", pEvent.OLB);
            c.AddParam("@accruedInterests", pEvent.AccruedInterests);
            c.AddParam("@accruedPenalties", pEvent.AccruedPenalties);
            c.AddParam("@pastDueDays", pEvent.PastDueDays);
            c.AddParam("@overdue_principal", pEvent.OverduePrincipal);
            c.AddParam("@writeOffMethod", pEvent.WriteOffMethod);
        }

        private static void SetLoanTrancheEvent(TrancheEvent trancheEvent, OpenCbsCommand command)
        {
            command.AddParam("@id", trancheEvent.Id);
            command.AddParam("@interest_rate", trancheEvent.InterestRate);
            command.AddParam("@amount", trancheEvent.Amount);
            command.AddParam("@maturity", trancheEvent.Maturity);
            command.AddParam("@start_date", trancheEvent.StartDate);
            command.AddParam("@applied_new_interest", trancheEvent.ApplyNewInterest);
            command.AddParam("@started_from_installment", trancheEvent.StartedFromInstallment);
            command.AddParam("@first_repayment_date", trancheEvent.FirstRepaymentDate);
            command.AddParam("@grace_period", trancheEvent.GracePeriod);
        }

        private static void SetLoanReschedulingEvent(RescheduleLoanEvent pEvent, OpenCbsCommand c)
        {
            c.AddParam("@id", pEvent.Id);
            c.AddParam("@amount", pEvent.Amount);
            c.AddParam("@maturity", pEvent.NbOfMaturity);
            c.AddParam("@interest", pEvent.Interest);
            c.AddParam("@gracePeriod", pEvent.GracePeriod);
            c.AddParam("@chargeInterestDuringGracePeriod", pEvent.ChargeInterestDuringGracePeriod);
            c.AddParam("@preferredFirstInstallmentDate", pEvent.PreferredFirstInstallmentDate);
        }

        private static void SetLoanRepaymentEvent(RepaymentEvent evnt, OpenCbsCommand c)
        {
            c.AddParam("@id", evnt.Id);
            c.AddParam("@pastDueDays", evnt.PastDueDays);
            c.AddParam("@principal", evnt.Principal);
            c.AddParam("@interests", evnt.Interests);
            c.AddParam("@commissions", evnt.Commissions);
            c.AddParam("@penalties", evnt.Penalties);
            c.AddParam("@installmentNumber", evnt.InstallmentNumber);
            c.AddParam("@payment_method_id", evnt.PaymentMethod.Id);
            c.AddParam("@calculated_penalties", evnt.CalculatedPenalties);
            c.AddParam("@written_off_penalties", evnt.WrittenOffPenalties);
            c.AddParam("@unpaid_penalties", evnt.UnpaidPenalties);
        }

        private static void GetLoanDisbursmentEvent(LoanDisbursmentEvent evnt, OpenCbsCommand c)
        {
            c.AddParam("@id", evnt.Id);
            c.AddParam("@amount", evnt.Amount.Value);
            c.AddParam("@fees", 0);
            c.AddParam("@interest", evnt.Interest.HasValue ? evnt.Interest.Value : 0);
            c.AddParam("@payment_method_id", evnt.PaymentMethod.Id);
        }

        private static void SetLoanInterestAccruingEvent(AccruedInterestEvent pEvent, OpenCbsCommand c)
	    {
            c.AddParam("@id", pEvent.Id);
            c.AddParam("@interestPrepayment", pEvent.Interest.Value);
            c.AddParam("@accruedInterest", pEvent.AccruedInterest.Value);
            c.AddParam("@rescheduled", pEvent.Rescheduled);
            c.AddParam("@installmentNumber", pEvent.InstallmentNumber);
	    }

        private static void SetLoanEvent(OpenCbsCommand c, Event pEvent, int pContractId)
        {
            c.AddParam("@eventType", pEvent.Code);
            c.AddParam("@contractId", pContractId);
            c.AddParam("@eventDate", pEvent.Date);
            c.AddParam("@userId", pEvent.User.Id);
            c.AddParam("@deleted", pEvent.Deleted);
            c.AddParam("@tellerId", pEvent.TellerId);
            c.AddParam("@parentId", pEvent.ParentId);
            c.AddParam("@comment", pEvent.Comment);
        }

        private static void SetLoanPenaltyAccrualEvent(OpenCbsCommand c, Event pEvent, OCurrency penalty)
        {
            c.AddParam("@id", pEvent.Id);
            c.AddParam("@penalty", penalty);
        }

        private static void SetLoanTransitionEvent(OpenCbsCommand c, Event pEvent, OCurrency amount)
        {
            c.AddParam("@id", pEvent.Id);
            c.AddParam("@amount", amount);
        }

        private static void SetLoanInterestAccrualEvent(OpenCbsCommand c, Event pEvent, OCurrency interest)
        {
            c.AddParam("@id", pEvent.Id);
            c.AddParam("@interest", interest);
        }

        private Event ReadEvent(OpenCbsReader r)
        {
            Event e;

            if (r.GetNullInt("lde_id").HasValue)
            {
                e = GetLoanDisbursmentEvent(r);
            }
            else if (r.GetNullInt("woe_id").HasValue)
            {
                e = GetWriteOffEvent(r);
            }
            else if (r.GetNullInt("rle_id").HasValue)
            {
                e = GetReschedulingLoanEvent(r);
            }
            else if (r.GetNullInt("rpe_id").HasValue)
            {
                e = GetRepaymentEvent(r);
            }
            else if (r.GetNullInt("tranche_id").HasValue)
            {
                e = GetTrancheLoanEvent(r);
            }
            else if (r.GetNullInt("liae_id").HasValue)
            {
                e = GetLoanInterestAccruingEvent(r);
            }
            else if (r.GetNullInt("ov_id").HasValue)
            {
                e = GetOverdueEvent(r);
            }
            else if (r.GetNullInt("pe_id").HasValue)
            {
                e = GetProvisionEvent(r);
            }
            else if (r.GetNullInt("ef_id").HasValue)
            {
                e = GetEntryFeeEvent(r);
            }
            else if (r.GetNullInt("cie_id").HasValue)
            {
                e = GetCreditInsuranceEvent(r);
            }
            else if (r.GetString("code").StartsWith("S"))
            {
                e = GetSavingEvent(r);
            }
            else if (r.GetNullInt("lpae_id").HasValue)
            {
                e = GetLoanPenaltyAccrualEvent(r);
            }
            else if (r.GetNullInt("aile_id").HasValue)
            {
                e = GetLoanInterestAccrualEvent(r);
            }
            else if (r.GetNullInt("glll_id").HasValue)
            {
                e = GetLoanTransitionEvent(r);
            }
            else
            {
                if (r.GetString("code").Equals("LOVE"))
                    e = new LoanValidationEvent { Id = r.GetInt("event_id") };
                else if (r.GetString("code").Equals("LOCE"))
                    e = new LoanCloseEvent { Id = r.GetInt("event_id") };
                else if (r.GetString("code").Equals("MSCE"))
                    e = new ManualScheduleChangeEvent { Id = r.GetInt("event_id") };
                else
                    e = new RegEvent { Id = r.GetInt("event_id") };
            }

            GetEvent(r, e);

            return e;
        }

	    private static Event GetCreditInsuranceEvent(OpenCbsReader r)
	    {
	        CreditInsuranceEvent cie = new CreditInsuranceEvent();
            cie.Id = r.GetInt("cie_id");
	        cie.Commission = r.GetDecimal("cie_commission");
	        cie.Principal = r.GetDecimal("cie_principal");
	        return cie;
	    }

	    public  List<LoanEntryFeeEvent> GetEntryFeeEvents(int disbursementEventId)
	    {
	       List<LoanEntryFeeEvent> entryFeeEvents = new List<LoanEntryFeeEvent>();
            const string q = @"SELECT DISTINCT  entry_date
                                            ,event_type
                                            ,LoanEntryFeeEvents.fee
                                            ,[user_id]
                                            ,ContractEvents.id
                                            ,ContractEvents.is_deleted
                                            ,LoanEntryFeeEvents.disbursement_event_id
                                FROM ContractEvents 
                                INNER JOIN LoanEntryFeeEvents ON ContractEvents.id = LoanEntryFeeEvents.id
                                WHERE ContractEvents.event_type LIKE 'LEE%' AND
                                LoanEntryFeeEvents.[disbursement_event_id]=@disbursement_event_id";
            using (SqlConnection conn = GetConnection())
	        using (OpenCbsCommand c = new OpenCbsCommand(q, conn))
	        {
                c.AddParam("@disbursement_event_id", disbursementEventId);
	            using (OpenCbsReader r = c.ExecuteReader())
	            {
	                while (r.Read())
	                {
                        var loanEntryFeeEvent = new LoanEntryFeeEvent
                                                    {
                                                        Code = r.GetString("event_type"),
                                                        Fee = r.GetDecimal("fee"),
                                                        Cancelable = true,
                                                        Deleted = r.GetBool("is_deleted"),
                                                        User = new User { Id = r.GetInt("user_id") },
                                                        EntryDate = r.GetDateTime("entry_date"),
                                                        Id = r.GetInt("id"),
                                                        DisbursementEventId = r.GetInt("disbursement_event_id")
                                                    };
	                    entryFeeEvents.Add(loanEntryFeeEvent);
	                }
	            }
	        }
	        return entryFeeEvents;
	    }

	    private static Event GetSavingEvent(OpenCbsReader r)
        {
            SavingEvent e;

            switch (r.GetString("code"))
            {
                case OSavingEvents.Deposit:
                    e = new SavingDepositEvent();
                    break;
                case OSavingEvents.Withdraw:
                    e = new SavingWithdrawEvent();
                    break;
                case OSavingEvents.InitialDeposit:
                    e = new SavingInitialDepositEvent();
                    break;
                case OSavingEvents.CreditTransfer:
                    e = new SavingCreditTransferEvent();
                    break;
                case OSavingEvents.DebitTransfer:
                    e = new SavingDebitTransferEvent();
                    break;
                case OSavingEvents.SpecialOperationCredit:
                    e = new SavingCreditOperationEvent();
                    break;
                case OSavingEvents.SpecialOperationDebit:
                    e = new SavingDebitOperationEvent();
                    break;
                case OSavingEvents.InterBranchCreditTransfer:
                    e = new SavingCreditInterBranchTransferEvent();
                    break;
                case OSavingEvents.InterBranchDebitTransfer:
                    e = new SavingDebitInterBranchTransferEvent();
                    break;
                default:
                    throw new Exception();
            }

            e.Amount = r.GetMoney("se_amount");
            if (e is ISavingsFees)
                ((ISavingsFees)e).Fee = r.GetMoney("se_fees");
            if (e is SavingTransferEvent)
                ((SavingTransferEvent)e).RelatedContractCode = r.GetString("se_transfer_code");

            return e;
        }

        private static void GetEvent(OpenCbsReader r, Event pEvent)
        {
            //abstract class Event attributes
            string eventType = r.GetString("event_type");
            pEvent.Code = eventType;
            pEvent.ContracId = r.GetInt("contract_id");
            pEvent.Date = r.GetDateTime("event_date");
            pEvent.EntryDate = r.GetDateTime("entry_date");
            pEvent.Deleted = r.GetBool("event_deleted");
            pEvent.IsFired = true;
            pEvent.Cancelable = true;
            pEvent.ExportedDate = DateTime.MinValue;
            pEvent.Comment = r.GetString("comment");
            pEvent.TellerId = r.GetNullInt("teller_id");
            pEvent.ParentId = r.GetNullInt("parent_id");
            pEvent.CancelDate = r.GetNullDateTime("cancel_date");
            pEvent.ClientType = OClientTypes.All;

            switch (r.GetString("client_type_code"))
            {
                case "I":
                    pEvent.ClientType = OClientTypes.Person; 
                    break;
                case "C":
                    pEvent.ClientType = OClientTypes.Corporate; 
                    break;
                case "G":
                    pEvent.ClientType = OClientTypes.Group;
                    break;
                case "V":
                    pEvent.ClientType = OClientTypes.Village; 
                    break;
            }

            //User associated to the event
            pEvent.User = new User
                              {
                                  Id = r.GetInt("user_id"),
                                  UserName = r.GetString("user_username"),
                                  Password = r.GetString("user_password"),
                                  LastName = r.GetString("user_lastname"),
                                  FirstName = r.GetString("user_firstname")
                              };

            pEvent.Currency = new Currency
                                  {
                                      Id = r.GetInt("currency_id"),
                                      Code = r.GetString("currency_code"),
                                      IsPivot = r.GetBool("is_pivot"),
                                      IsSwapped = r.GetBool("is_swapped")
                                  };

            pEvent.Branch = new Branch { Id = r.GetInt("branch_id") };
            pEvent.LoanProduct = new LoanProduct { Id = r.GetInt("product_id") };

            pEvent.User.SetRole(r.GetString("user_role"));
            if (
                eventType.Equals("ULIE") ||
                eventType.Equals("ULOE")
                )
                return;

            if (r.HasColumn("contract_code"))
                pEvent.Description = r.GetString("contract_code");
        }

	    private static AccruedInterestEvent GetLoanInterestAccruingEvent(OpenCbsReader r)
        {
            return new AccruedInterestEvent{
                           Id = r.GetInt("liae_id"),
                           AccruedInterest = r.GetMoney("liae_accruedInterest"),
                           Interest = r.GetMoney("liae_interestPrepayment"),
                           Rescheduled = r.GetBool("liae_rescheduled"),
                           InstallmentNumber = r.GetInt("liae_installmentNumber")
                       };
        }

        private static LoanPenaltyAccrualEvent GetLoanPenaltyAccrualEvent(OpenCbsReader r)
        {
            return new LoanPenaltyAccrualEvent
                {
                    Id = r.GetInt("lpae_id"),
                    Penalty = r.GetMoney("lpae_penalty"),
                };
        }

        private static LoanInterestAccrualEvent GetLoanInterestAccrualEvent(OpenCbsReader r)
        {
            return new LoanInterestAccrualEvent
                {
                    Id = r.GetInt("aile_id"),
                    Interest = r.GetMoney("aile_interest"),
                };
        }

        private static LoanTransitionEvent GetLoanTransitionEvent(OpenCbsReader r)
        {
            return new LoanTransitionEvent
            {
                Id = r.GetInt("glll_id"),
                Amount = r.GetMoney("glll_amount"),
            };
        }
        
	    private static OverdueEvent GetOverdueEvent(OpenCbsReader r)
        {
            return new OverdueEvent{
                Id = r.GetInt("ov_id"),
                OLB = r.GetMoney("ov_olb"),
                OverdueDays = r.GetInt("ov_overdue_days"),
                OverduePrincipal = r.GetMoney("ov_overdue_principal")
            };
        }

        private static ProvisionEvent GetProvisionEvent(OpenCbsReader r)
        {
            return new ProvisionEvent{
                Id = r.GetInt("pe_id"),
                Amount = r.GetMoney("pe_amount"),
                OverdueDays = r.GetInt("pe_overdue_days")
            };
        }

        private static LoanEntryFeeEvent GetEntryFeeEvent(OpenCbsReader r)
        {
            return new LoanEntryFeeEvent
            {
                Id = r.GetInt("ef_id"),
                Fee = r.GetMoney("ef_fee"),
                Cancelable = true,
                DisbursementEventId = r.GetInt("disbursement_event_id")
            };
        }

        private static TrancheEvent GetTrancheLoanEvent(OpenCbsReader r)
        {
            return new TrancheEvent{
                Id = r.GetInt("tranche_id"),
                Amount = r.GetMoney("tranche_amount"),
                InterestRate = r.GetMoney("tranche_interest_rate").Value,
                Maturity = r.GetInt("tranche_maturity"),
                StartDate = r.GetDateTime("tranche_start_date"),
                GracePeriod = r.GetInt("tranche_grace_period"),
                FirstRepaymentDate = r.GetDateTime("tranche_first_repayment_date"),
            };
        }

        private RepaymentEvent GetRepaymentEvent(OpenCbsReader r)
	    {
            RepaymentEvent e = new RepaymentEvent {Id = r.GetInt("rpe_id")};
            switch (r.GetString("event_type"))
            {
                case "RBLE":
                    {
                        e = new BadLoanRepaymentEvent {Id = r.GetInt("rpe_id")};
                        break;
                    }
                case "RRLE":
                    {
                        e = new RescheduledLoanRepaymentEvent {Id = r.GetInt("rpe_id")};
                        break;
                    }
                case "ROWO":
                    {
                        e = new RepaymentOverWriteOffEvent { Id = r.GetInt("rpe_id") };
                        break;
                    }
                case "PRLR":
                    {
                        e = new PendingRepaymentEvent(r.GetString("event_type"))
                                {Id = r.GetInt("rpe_id")};
                        break;
                    }
                case "PBLR":
                    {
                        e = new PendingRepaymentEvent(r.GetString("event_type"))
                                {Id = r.GetInt("rpe_id")};
                        break;
                    }
                case "PRWO":
                    {
                        e = new PendingRepaymentEvent(r.GetString("event_type"))
                                {Id = r.GetInt("rpe_id")};
                        break;
                    }
                case "PERE":
                    {
                        e = new PendingRepaymentEvent(r.GetString("event_type"))
                                {Id = r.GetInt("rpe_id")};
                        break;
                    }
            }

	        e.Principal = r.GetMoney("rpe_principal");
	        e.Interests = r.GetMoney("rpe_interests");
	        e.Penalties = r.GetMoney("rpe_penalties");
            e.Commissions = r.GetMoney("rpe_commissions");
	        e.PastDueDays = r.GetInt("rpe_past_due_days");
	        e.InstallmentNumber = r.GetInt("rpe_installment_number");
            e.PaymentMethodId = r.GetNullInt("rpe_pm");
            e.PaymentMethod = e.PaymentMethodId == null ? null :
                _paymentMethodManager.SelectPaymentMethodById(e.PaymentMethodId.Value);

            e.CalculatedPenalties = r.GetMoney("rpe_calculated_penalties");
            e.WrittenOffPenalties = r.GetMoney("rpe_written_off_penalties");
            e.UnpaidPenalties = r.GetMoney("rpe_unpaid_penalties");

            e.Code = r.GetString("event_type");

            if (e.Code != "RBLE")
                e.RepaymentType = OPaymentType.StandardPayment;
            
            // set type of payment
            switch (r.GetString("event_type").Trim())
            {
                case "ATR":
                    {
                        e.RepaymentType = OPaymentType.TotalPayment;
                        break;
                    }
                case "APR":
                    {
                        e.RepaymentType = OPaymentType.PartialPayment;
                        break;
                    }
                case "APTR":
                    {
                        e.RepaymentType = OPaymentType.PersonTotalPayment;
                        break;
                    }
            }

	        return e;
	    }

        private static RescheduleLoanEvent GetReschedulingLoanEvent(OpenCbsReader r)
	    {
            return new RescheduleLoanEvent{
                Id = r.GetInt("rle_id"),
                Amount = r.GetMoney("rle_amount"),
                NbOfMaturity = r.GetInt("rle_maturity"),
                PreferredFirstInstallmentDate = r.GetDateTime("rle_preferred_first_installment_date")
            };
	    }

	    private static Event GetWriteOffEvent(OpenCbsReader r)
	    {
	        return new WriteOffEvent
                                  {
                                      Id = r.GetInt("woe_id"),
                                      OLB = r.GetMoney("woe_olb"),
                                      AccruedInterests = r.GetMoney("woe_accrued_interests"),
                                      AccruedPenalties = r.GetMoney("woe_accrued_penalties"),
                                      PastDueDays = r.GetInt("woe_past_due_days"),
                                      OverduePrincipal = r.GetMoney("woe_overdue_principal")
                                  };
	    }

	    private LoanDisbursmentEvent GetLoanDisbursmentEvent(OpenCbsReader r)
	    {
	        return new LoanDisbursmentEvent
	                   {
	                       Id = r.GetInt("lde_id"),
	                       Amount = r.GetMoney("lde_amount"),
	                       Fee = r.GetMoney("lde_fees"),
	                       PaymentMethodId = r.GetNullInt("lde_pm"),
	                       PaymentMethod = r.GetNullInt("lde_pm") == null
	                                           ? null
	                                           : _paymentMethodManager.SelectPaymentMethodById(
	                                               r.GetNullInt("lde_pm").Value)
	                   };
	    }

        public void LogClientSaveUpdateEvent(string client, bool save, int userId)
        {
            const string q = @"INSERT INTO dbo.TraceUserLogs
                               (event_code, event_date, user_id, event_description)
                               VALUES ('CSUE', GETDATE(), @user_id, @event_description)";

            string desc = save ? "{0} saved." : "{0} updated.";
            desc = string.Format(desc, client);
            using (SqlConnection conn = GetConnection())
            using (OpenCbsCommand c = new OpenCbsCommand(q, conn))
            {
                c.AddParam("@user_id", userId);
                c.AddParam("@event_description", desc);
                c.ExecuteNonQuery();
            }
        }

        public bool IsEventExported(int id)
        {
            const string query = @"SELECT is_exported
            FROM dbo.ContractEvents
            WHERE id = @id";
            using (SqlConnection conn = GetConnection())
            using (OpenCbsCommand cmd = new OpenCbsCommand(query, conn))
            {
                cmd.AddParam("@id", id);
                return Convert.ToBoolean(cmd.ExecuteScalar());
            }
        }
	}
}
