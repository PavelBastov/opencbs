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

using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using Dapper;
using OpenCBS.CoreDomain;
using OpenCBS.CoreDomain.Accounting;
using OpenCBS.CoreDomain.Alerts;
using OpenCBS.CoreDomain.Clients;
using OpenCBS.CoreDomain.Contracts;
using OpenCBS.CoreDomain.Contracts.Collaterals;
using OpenCBS.CoreDomain.Contracts.Loans;
using OpenCBS.CoreDomain.Contracts.Loans.Installments;
using OpenCBS.CoreDomain.Contracts.Loans.LoanRepayment;
using OpenCBS.CoreDomain.Events;
using OpenCBS.CoreDomain.Events.Loan;
using OpenCBS.CoreDomain.Products.Collaterals;
using OpenCBS.CoreDomain.SearchResult;
using OpenCBS.Enums;
using OpenCBS.Manager.Clients;
using OpenCBS.Manager.Events;
using OpenCBS.Manager.Products;
using OpenCBS.Shared;
using OpenCBS.Shared.Settings;

namespace OpenCBS.Manager.Contracts
{
    /// <summary>
    /// CreditContractManagement contains all methods relative to selecting, inserting, updating
    /// and deleting creditContract objects to and from our database.
    /// </summary>
    public class LoanManager : Manager
    {
        private readonly LoanProductManager _packageManager;
        private readonly InstallmentTypeManager _installmentTypeManagement;
        private readonly InstallmentManager _installmentManagement;
        private readonly UserManager _userManager;
        private readonly EventManager _eventManagement;
        private readonly FundingLineManager _fundingLineManager;
        private readonly ProjectManager _projectManager;
        private readonly ClientManager _clientManager;
        private readonly PaymentMethodManager _paymentMethodManager;
        private readonly CollateralProductManager _collateralProductManager;
        private readonly EconomicActivityManager _economicActivityManager;

        private readonly User _user = new User();

        public LoanManager(User pUser)
            : base(pUser)
        {
            _user = pUser;
            _userManager = new UserManager(pUser);
            _paymentMethodManager = new PaymentMethodManager(pUser);
            _packageManager = new LoanProductManager(pUser);
            _installmentTypeManagement = new InstallmentTypeManager(pUser);
            _installmentManagement = new InstallmentManager(pUser);
            _eventManagement = new EventManager(pUser);
            _fundingLineManager = new FundingLineManager(pUser);
            _projectManager = new ProjectManager(pUser, false);
            _clientManager = new ClientManager(pUser, false, false);
            _collateralProductManager = new CollateralProductManager(pUser);
            _economicActivityManager = new EconomicActivityManager(pUser);
        }

        public LoanManager(string pTestDb)
            : base(pTestDb)
        {
            _user = User.CurrentUser;
            _userManager = new UserManager(pTestDb);
            _packageManager = new LoanProductManager(pTestDb);
            _installmentTypeManagement = new InstallmentTypeManager(pTestDb);
            _installmentManagement = new InstallmentManager(pTestDb);
            _eventManagement = new EventManager(pTestDb);
            _fundingLineManager = new FundingLineManager(pTestDb);
            _collateralProductManager = new CollateralProductManager(pTestDb);
            _paymentMethodManager = new PaymentMethodManager(pTestDb);
            _economicActivityManager = new EconomicActivityManager(pTestDb);
        }

        public LoanManager(string pTestDb, User pUser)
            : base(pTestDb)
        {
            _user = pUser;
            _userManager = new UserManager(pTestDb, _user);
            _packageManager = new LoanProductManager(pTestDb);
            _installmentTypeManagement = new InstallmentTypeManager(pTestDb);
            _installmentManagement = new InstallmentManager(pTestDb);
            _eventManagement = new EventManager(pTestDb);
            _fundingLineManager = new FundingLineManager(pTestDb);
            _clientManager = new ClientManager(pTestDb);
            _collateralProductManager = new CollateralProductManager(pTestDb);
            _paymentMethodManager = new PaymentMethodManager(pTestDb, pUser);
        }

        public int Add(Loan pLoan, int pProjectId)
        {
            using (SqlConnection connection = GetConnection())
            using (SqlTransaction transaction = connection.BeginTransaction())
            {
                int result = Add(pLoan, pProjectId, transaction);
                transaction.Commit();
                return result;
            }
        }

        /// <summary>
        /// Add a loan in database
        /// </summary>
        /// <param name="pLoan">loan to add</param>
        /// <param name="pProjectId">loan's project id</param>
        /// <param name="pSqlTransac">use an sql transaction?</param>
        /// <returns></returns>
        public int Add(Loan pLoan, int pProjectId, SqlTransaction pSqlTransac)
        {
            pLoan.Id = _AddContract(pLoan, pProjectId, pSqlTransac);

            const string q = @"INSERT INTO [Credit](
                        [id], 
                        [package_id],
                        [fundingLine_id],
                        [amount], 
                        [interest_rate], 
                        [installment_type], 
                        [nb_of_installment], 
                        [anticipated_total_repayment_penalties],
                        [anticipated_partial_repayment_penalties], 
                        [disbursed], 
                        [loanofficer_id], 
                        [grace_period], 
                        [written_off], 
                        [rescheduled], 
                        [bad_loan],[synchronize],
                        [non_repayment_penalties_based_on_initial_amount],
                        [non_repayment_penalties_based_on_olb],
                        [non_repayment_penalties_based_on_overdue_interest],
                        [non_repayment_penalties_based_on_overdue_principal],                        
                        [grace_period_of_latefees],
                        [number_of_drawings_loc],
                        [amount_under_loc],
                        [maturity_loc],
                        [anticipated_partial_repayment_base],
                        [anticipated_total_repayment_base],
                        [amount_min],
                        [amount_max],
                        [ir_min],
                        [ir_max],
                        [nmb_of_inst_min],
                        [nmb_of_inst_max],
                        [loan_cycle], 
                        [insurance]) 
                        VALUES(@id, 
                        @packageId, 
                        @fundingLine_id, 
                        @amount, @interestRate, @installmentType, @nbOfInstallments, 
                        @anticipatedTotalRepaymentPenalties,
                        @anticipatedPartialRepaymentPenalties,  
                        @disbursed, 
                        @loanOfficerId,
                        @gracePeriod,
                        @writtenOff,
                        @rescheduled,
                        @badLoan,
                        @synchronize, 
                        @nonRepaymentPenaltiesInitialAmount,
                        @nonRepaymentPenaltiesOLB,
                        @nonRepaymentPenaltiesOverdueInterest,
                        @nonRepaymentPenaltiesOverduePrincipal,
                        @grace_period_of_latefees,
                        @DrawingsNumber, @AmountUnderLoc, @MaturityLoc,
                        @AnticipatedPartialRepaymentPenaltiesBase, @AnticipatedTotalRepaymentPenaltiesBase,
                        @amount_min,
                        @amount_max,
                        @ir_min,
                        @ir_max,
                        @nmb_of_inst_min,
                        @nmb_of_inst_max,
                        @loan_cycle,
                        @insurance
                        )";

            using (OpenCbsCommand c = new OpenCbsCommand(q, pSqlTransac.Connection, pSqlTransac))
            {
                SetLoan(pLoan, c);
                c.ExecuteNonQuery();
            }

            if (OClientTypes.Group == pLoan.ClientType) _SetLoanShareAmount(pLoan, pSqlTransac);

            _installmentManagement.AddInstallments(pLoan.InstallmentList, pLoan.Id, pSqlTransac);
            return pLoan.Id;
        }

        /// <summary>
        /// Select the latest active loan for a client
        /// </summary>
        /// <param name="pClientId"></param>
        /// <returns></returns>
        public List<Loan> SelectActiveLoans(int pClientId)
        {
            const string q = @"SELECT Contracts.id as id
                                 FROM Contracts 
                                 INNER JOIN Projects ON Contracts.project_id = Projects.id
                                 WHERE Projects.tiers_id = @id 
                                   AND Contracts.status IN (@status1, @status2, @status3, @status4)
                                   AND Contracts.nsg_id IS NOT NULL";

            List<Loan> list = new List<Loan>();
            using (SqlConnection conn = GetConnection())
            using (OpenCbsCommand c = new OpenCbsCommand(q, conn))
            {
                c.AddParam("@id", pClientId);
                c.AddParam("@status1", (int) OContractStatus.Active);
                c.AddParam("@status2", (int) OContractStatus.Validated);
                c.AddParam("@status3", (int) OContractStatus.Pending);
                c.AddParam("@status4", (int) OContractStatus.Postponed);

                using (OpenCbsReader r = c.ExecuteReader())
                {
                    if (r == null || r.Empty) return list;

                    while (r.Read())
                        list.Add(SelectLoan(r.GetInt("id"), true, true, true));
                }
            }
            return list;
        }

        public List<Loan> SelectAllLoansOfClient(int pClientId)
        {
            const string sql = @"SELECT Contracts.id as id
                                 FROM Contracts 
                                 INNER JOIN Projects ON Contracts.project_id = Projects.id
                                 WHERE Projects.tiers_id = @id
                                   AND Contracts.nsg_id IS NOT NULL";

            List<Loan> list = new List<Loan>();
            using (SqlConnection conn = GetConnection())
            using (OpenCbsCommand c = new OpenCbsCommand(sql, conn))
            {
                c.AddParam("@id", pClientId);

                using (OpenCbsReader r = c.ExecuteReader())
                {
                    if (r == null || r.Empty) return list;

                    while (r.Read())
                        list.Add(SelectLoan(r.GetInt("id"), true, true, true));
                }
            }
            return list;
        }

        public int SelectLoanID(string pLoanContractCode)
        {
            const string q = @"SELECT ID 
                                     FROM Contracts
                                     WHERE contract_code = @contractCode";
            using (SqlConnection conn = GetConnection())
            using (OpenCbsCommand c = new OpenCbsCommand(q, conn))
            {
                c.AddParam("@contractCode", pLoanContractCode);
                object val = c.ExecuteScalar();
                return null == val ? 0 : Convert.ToInt32(val);
            }
        }

        public int GetNbOfLoansForClosure()
        {
            const string q =
                @"SELECT COUNT(Credit.id) FROM Credit WHERE Credit.disbursed = 1 AND Credit.written_off = 0 AND 
                                (NOT ((SELECT SUM(interest_repayment) + SUM(capital_repayment) - SUM(paid_interest) - SUM(paid_capital) 
                                FROM Installments WHERE contract_id = Credit.id) < 0.02))";
            using (SqlConnection conn = GetConnection())
            using (OpenCbsCommand c = new OpenCbsCommand(q, conn))
            {
                return Convert.ToInt32(c.ExecuteScalar());
            }
        }

        public List<CreditSearchResult> SearchCreditContractByCriteres(int pageNumber, string pQuery, out int count)
        {
            int startRow = 20*(pageNumber - 1) + 1;
            int endRow = 20*pageNumber;
            const string query = @"
                SELECT * FROM (			
		                SELECT ROW_NUMBER() OVER (ORDER BY [user_id]) row, COUNT(1) OVER(PARTITION BY [user_id]) row_count, * FROM (
                            SELECT  
                                      Contracts.id, 
                                      Contracts.contract_code, 
                                      Contracts.status, 
                                      Contracts.start_date, 
                                      Contracts.align_disbursed_date, 
                                      Contracts.close_date, 
                                      Persons.identification_data as identification_data,
                                      Credit.amount, 
                                      Credit.loanofficer_id, 
                                      Tiers.client_type_code, 
                                      ISNULL(Users.first_name + SPACE(1) + Users.last_name, Users.user_name) AS user_name,
                                      ISNULL(ISNULL(ISNULL(Groups.name, Persons.first_name + SPACE(1) + Persons.last_name), Corporates.name),'Error!') AS client_name,
                                      ISNULL(Users.first_name + SPACE(1) + Users.last_name, Users.user_name) AS loanofficer_name,
                                      Credit.[amount_min],
                                      Credit.[amount_max],
                                      Credit.[ir_min],
                                      Credit.[ir_max],
                                      Credit.[nmb_of_inst_min],
                                      Credit.[nmb_of_inst_max],
                                      Credit.[loan_cycle],
                                      @user_id [user_id]
                            FROM Contracts 
                            INNER JOIN Credit ON Contracts.id = Credit.id 
                            INNER JOIN Projects ON Contracts.project_id = Projects.id
                            INNER JOIN Tiers ON Projects.tiers_id = Tiers.id AND Tiers.branch_id IN (select branch_id from dbo.UsersBranches where user_id = @user_id) 
                            LEFT JOIN Users ON Users.id = Credit.loanofficer_id 
                            LEFT OUTER JOIN Persons ON Tiers.id = Persons.id 
                            LEFT OUTER JOIN Groups ON Tiers.id = Groups.id 
                            LEFT OUTER JOIN Corporates ON Tiers.id = Corporates.id
                            
                            UNION ALL
                            
                            SELECT ISNULL(Contracts.id,0) AS id, ISNULL(Contracts.contract_code,'No contract') AS contract_code, 
                              ISNULL(Contracts.status,0) AS status, ISNULL(Contracts.start_date,'01-01-1900') AS start_date, 
                              ISNULL(Contracts.align_disbursed_date,'01-01-1900') AS align_disbursed_date, 
                              ISNULL(Contracts.close_date,'01-01-1900') AS close_date, 
                              Persons.identification_data AS identification_data, ISNULL(Credit.amount,0) AS amount, 
                              ISNULL(Credit.loanofficer_id,0) AS loanofficer_id, 
                              CASE (Tiers.client_type_code) WHEN 'I' THEN 'V' ELSE '-' END AS client_type_code, 
                              ISNULL(ISNULL(Users.first_name + SPACE(1) + Users.last_name, Users.user_name),'No contract') AS user_name,
                              Villages.name AS client_name,
                              ISNULL(ISNULL(Users.first_name + SPACE(1) + Users.last_name, Users.user_name),'No contract') AS loanofficer_name,
                              Credit.[amount_min],
                              Credit.[amount_max],
                              Credit.[ir_min],
                              Credit.[ir_max],
                              Credit.[nmb_of_inst_min],
                              Credit.[nmb_of_inst_max],
                              Credit.[loan_cycle],
                              @user_id [user_id]
                            FROM Villages
                            INNER JOIN VillagesPersons ON VillagesPersons.village_id = Villages.id
                            INNER JOIN Persons ON Persons.id = VillagesPersons.person_id
                            INNER JOIN Tiers ON Persons.id = Tiers.id AND Tiers.branch_id IN (select branch_id from dbo.UsersBranches where user_id = @user_id)
                            INNER JOIN Projects ON Tiers.id = Projects.tiers_id
                            INNER JOIN Contracts ON Contracts.project_id = Projects.id
                            INNER JOIN Credit ON Credit.id = Contracts.id
                            LEFT JOIN Users ON Users.id = Credit.loanofficer_id
	                ) R WHERE (ISNULL(contract_code, '') + ' ' + ISNULL(client_name, '') + ' ' + ISNULL([user_name], '') + ' ' + ISNULL(identification_data, '') + ' ' + ISNULL(loanofficer_name, '')) LIKE @criteria
                ) T WHERE row BETWEEN @startRow AND @endRow
            ";

            count = 0;
            var list = new List<CreditSearchResult>();
            using (SqlConnection conn = GetConnection())
            using (OpenCbsCommand cmd = new OpenCbsCommand(query, conn))
            {
                cmd.AddParam("user_id", User.CurrentUser.Id);
                cmd.AddParam("startRow", startRow);
                cmd.AddParam("endRow", endRow);
                cmd.AddParam("criteria", string.Format("%{0}%", pQuery));
                using (OpenCbsReader reader = cmd.ExecuteReader())
                    while (reader.Read())
                    {
                        if (count == 0) count = reader.GetInt("row_count");
                        var result = new CreditSearchResult
                            {
                                Id = reader.GetInt("id"),
                                ContractCode = reader.GetString("contract_code"),
                                ClientType = reader.GetString("client_type_code"),
                                ClientName = reader.GetString("client_name"),
                                ContractStartDate = reader.GetDateTime("start_date").ToShortDateString(),
                                ContractEndDate = reader.GetDateTime("close_date").ToShortDateString(),
                                ContractStatus = ((OContractStatus) reader.GetSmallInt("status")).ToString(),
                                LoanOfficer = new User {Id = reader.GetInt("loanofficer_id")}
                            };

                        list.Add(result);
                    }
            }
            return list;
        }

        private List<Guarantor> GetGuarantors(int pLoanId)
        {
            const string q = @"SELECT [tiers_id], 
                                       [guarantee_amount], 
                                       [guarantee_desc], 
                                       [client_type_code], 
                                       Groups.name, 
                                       Persons.first_name, 
                                       Persons.last_name, 
                                       district_id
                                     FROM [LinkGuarantorCredit] 
                                     INNER JOIN Tiers ON LinkGuarantorCredit.tiers_id = Tiers.id
                                     LEFT OUTER JOIN Groups ON Groups.id = Tiers.id 
                                     LEFT OUTER JOIN Persons ON Persons.id = Tiers.id
                                     WHERE LinkGuarantorCredit.contract_id = @id";
            using (SqlConnection conn = GetConnection())
            using (OpenCbsCommand c = new OpenCbsCommand(q, conn))
            {
                c.AddParam("@id", pLoanId);

                using (OpenCbsReader r = c.ExecuteReader())
                {
                    if (r == null || r.Empty) return new List<Guarantor>();

                    List<Guarantor> list = new List<Guarantor>();
                    while (r.Read())
                    {
                        Guarantor guarantor = new Guarantor
                            {
                                Amount = r.GetMoney("guarantee_amount"),
                                Description = r.GetString("guarantee_desc")
                            };

                        if (r.GetChar("client_type_code") == 'I')
                        {
                            guarantor.Tiers = new Person
                                {
                                    FirstName = r.GetString("first_name"),
                                    LastName = r.GetString("last_name"),
                                    District = new District
                                        {
                                            Id = r.GetInt("district_id"),
                                            Name = null
                                        }
                                };
                        }
                        else
                            guarantor.Tiers = new Group {Name = r.GetString("name")};

                        guarantor.Tiers.Id = r.GetInt("tiers_id");
                        list.Add(guarantor);
                    }
                    return list;
                }
            }
        }

        public void UpdateLoanToWriteOff(int pLoanId)
        {
            using (SqlConnection connection = GetConnection())
            using (SqlTransaction transaction = connection.BeginTransaction())
                try
                {
                    UpdateLoanToWriteOff(pLoanId, transaction);
                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
        }

        public void UpdateLoanToWriteOff(int pLoanId, SqlTransaction pSqlTransac)
        {
            const string q = @"UPDATE Credit SET written_off = 1, bad_loan = 0 WHERE id = @id";

            using (OpenCbsCommand c = new OpenCbsCommand(q, pSqlTransac.Connection, pSqlTransac))
            {
                c.AddParam("@id", pLoanId);
                c.ExecuteNonQuery();
            }

            // Updating contract status to WritenOff
            const string sqlTextContract = @"UPDATE Contracts SET status = @status WHERE id = @id";

            using (OpenCbsCommand c = new OpenCbsCommand(sqlTextContract, pSqlTransac.Connection, pSqlTransac))
            {
                c.AddParam("@id", pLoanId);
                c.AddParam("@status", (int) OContractStatus.WrittenOff);
                c.ExecuteNonQuery();
            }
        }

        public void UpdateLoanToRescheduled(decimal pNewInterestRate, int pNbOfMaturity, Loan pLoan)
        {
            using (SqlConnection connection = GetConnection())
            using (SqlTransaction transaction = connection.BeginTransaction())
                try
                {
                    UpdateLoanToRescheduled(pNewInterestRate, pNbOfMaturity, pLoan, transaction);
                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
        }

        public void UpdateLoanToRescheduled(decimal pNewInterestRate, int pNbOfMaturity, Loan pLoan,
                                            SqlTransaction pSqlTransac)
        {
            const string q = @"UPDATE Credit 
                                     SET rescheduled = 1, 
                                         nb_of_installment = @nbOfInstallment, 
                                         interest_rate = CAST(@newInterestRate AS NUMERIC(16,12))                                         
                                     WHERE id = @id";

            using (OpenCbsCommand c = new OpenCbsCommand(q, pSqlTransac.Connection, pSqlTransac))
            {
                c.AddParam("@id", pLoan.Id);
                c.AddParam("@nbOfInstallment", pLoan.NbOfInstallments);
                c.AddParam("@newInterestRate", Convert.ToDouble(pNewInterestRate));
                c.ExecuteNonQuery();
            }
        }

        public void UpdateLoanWithinTranche(decimal pNewInterestRate, int pNbOfMaturity, Loan pLoan,
                                            SqlTransaction pSqlTransac)
        {
            const string q = @"UPDATE Credit 
                                     SET nb_of_installment = @nbOfInstallment, 
                                         interest_rate = @newInterestRate,
                                         amount = @amount 
                                     WHERE id = @id";

            using (OpenCbsCommand c = new OpenCbsCommand(q, pSqlTransac.Connection, pSqlTransac))
            {
                c.AddParam("@id", pLoan.Id);
                c.AddParam("@nbOfInstallment", pLoan.NbOfInstallments);
                c.AddParam("@newInterestRate", Convert.ToDouble(pNewInterestRate));
                c.AddParam("@amount", pLoan.Amount);
                c.ExecuteNonQuery();
            }
        }

        public void UpdateLoanToBadLoan(int pLoanId)
        {
            using (SqlConnection connection = GetConnection())
            using (SqlTransaction transaction = connection.BeginTransaction())
                try
                {
                    UpdateLoanToBadLoan(pLoanId, transaction);
                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
        }

        public void UpdateLoanToBadLoan(int pLoanId, SqlTransaction pSqlTransac)
        {
            const string q = @"UPDATE Credit 
                                     SET bad_loan = 1
                                     WHERE id = @id";

            using (OpenCbsCommand c = new OpenCbsCommand(q, pSqlTransac.Connection, pSqlTransac))
            {
                c.AddParam("@id", pLoanId);
                c.ExecuteNonQuery();
            }
        }

        public void UpdateLoan(Loan pLoan)
        {
            using (SqlConnection connection = GetConnection())
            using (SqlTransaction transaction = connection.BeginTransaction())
            {
                UpdateLoan(pLoan, transaction);
                transaction.Commit();
            }
        }

        public void UpdateLoan(Loan pLoan, SqlTransaction pSqlTransac)
        {
            string q = @"UPDATE Credit 
                              SET loanofficer_id = @loanOfficerId, 
                                fundingLine_id = @fundingLine_id, 
                                disbursed = @disbursed,
                                rescheduled = @rescheduled,
                                nb_of_installment = @NbOfInstallment, 
                                amount = @Amount, 
                                interest_rate = @InterestRate, 
                                grace_period = @GracePeriode,
                                anticipated_total_repayment_penalties = @AnticipatedTotalRepayment,
                                anticipated_partial_repayment_penalties = @AnticipatedPartialRepayment,
                                non_repayment_penalties_based_on_overdue_principal = @NRPBOOP,
                                non_repayment_penalties_based_on_initial_amount = @NRPBOIA,
                                non_repayment_penalties_based_on_olb = @NRPBOOLB,
                                non_repayment_penalties_based_on_overdue_interest = @NRPBOOI,
                                synchronize = @synchronize,
                                grace_period_of_latefees = @grace_period_of_latefees,
                                [number_of_drawings_loc] = @DrawingsNumber, 
                                [amount_under_loc] = @AmountUnderLoc,
                                [maturity_loc] = @MaturityLoc,
                                [anticipated_partial_repayment_base] = @AnticipatedPartialRepaymentPenaltiesBase, 
                                [anticipated_total_repayment_base] = @AnticipatedTotalRepaymentPenaltiesBase,
                                [schedule_changed] = @schedule_changed,
                                [written_off] = @written_off,
                                [insurance]=@insurance
                              WHERE id = @id";

            using (OpenCbsCommand c = new OpenCbsCommand(q, pSqlTransac.Connection, pSqlTransac))
            {
                SetLoanForUpdate(c, pLoan);

                c.ExecuteNonQuery();
            }

            q = @"UPDATE Contracts 
                        SET start_date = @startDate,
                        align_disbursed_date = @align_disbursed_date,
                        close_date = @closeDate, 
                        closed = @closed, 
                        status = @status,
                        loan_purpose = @loanPurpose,
                        comments = @comments,
                        activity_id = @activityId,
                        preferred_first_installment_date = @preferredFirstInstallmentDate
                        WHERE id = @id";

            using (OpenCbsCommand c = new OpenCbsCommand(q, pSqlTransac.Connection, pSqlTransac))
            {
                c.AddParam("@startDate", pLoan.StartDate);
                c.AddParam("@align_disbursed_date", pLoan.AlignDisbursementDate);
                c.AddParam("@closeDate", pLoan.CloseDate);
                c.AddParam("@closed", pLoan.Closed);
                c.AddParam("@status", Convert.ToInt32(pLoan.ContractStatus));
                c.AddParam("@id", pLoan.Id);
                c.AddParam("@loanPurpose", pLoan.LoanPurpose);
                c.AddParam("@comments", pLoan.Comments);
                c.AddParam("@activityId", pLoan.EconomicActivityId);
                c.AddParam("@preferredFirstInstallmentDate", pLoan.FirstInstallmentDate);
                c.ExecuteNonQuery();
            }

            // Updating Tiers status to 'active'
            if (pLoan.Project != null && pLoan.Project.Client != null)
            {
                q = @"UPDATE Tiers 
                            SET active = @active 
                            WHERE id = @id";

                using (OpenCbsCommand c = new OpenCbsCommand(q, pSqlTransac.Connection, pSqlTransac))
                {
                    c.AddParam("@active", pLoan.Project.Client.Active);
                    c.AddParam("@id", pLoan.Project.Client.Id);
                    c.ExecuteNonQuery();
                }
            }

            if (pLoan.EscapedMember != null && pLoan.Project != null && pLoan.Project.Client != null)
            {
                //delete member from the group
                _clientManager.UpdatePersonFromGroup(pLoan.EscapedMember.Tiers.Id, pLoan.Project.Client.Id, pSqlTransac);

                q = @"UPDATE LoanShareAmounts 
                            SET payment_date = @payment_date,
                            event_id = @event_id
                            WHERE person_id = @person_id
                            AND group_id = @group_id
                            AND contract_id = @contract_id";

                using (OpenCbsCommand c = new OpenCbsCommand(q, pSqlTransac.Connection, pSqlTransac))
                {
                    c.AddParam("@payment_date", pLoan.GetLastNonDeletedEvent().Date);
                    c.AddParam("@event_id", pLoan.GetLastNonDeletedEvent().Id);

                    c.AddParam("@person_id", pLoan.EscapedMember.Tiers.Id);
                    c.AddParam("@group_id", pLoan.Project.Client.Id);
                    c.AddParam("@contract_id", pLoan.Id);
                    c.ExecuteNonQuery();
                }

                pLoan.EscapedMember = null;
            }

            _DeleteGuarantorsFromLoan(pLoan.Id, pSqlTransac);
            foreach (Guarantor guarantor in pLoan.Guarantors)
            {
                _AddGuarantor(guarantor, pLoan.Id, pSqlTransac);
            }

            _DeleteCollateralsFromLoan(pLoan.Id, pSqlTransac);
            foreach (ContractCollateral collateral in pLoan.Collaterals)
            {
                AddCollateral(collateral, pLoan.Id, pSqlTransac);
            }

            // Compulsory savings handling
            if (pLoan.CompulsorySavings != null)
            {
                int loanSavingsId = 0;

                string sqlCompulsory = @"SELECT id
                                         FROM LoansLinkSavingsBook
                                         WHERE loan_id = @loan_id";

                using (OpenCbsCommand c = new OpenCbsCommand(sqlCompulsory, pSqlTransac.Connection, pSqlTransac))
                {
                    c.AddParam("@loan_id", pLoan.Id);
                    using (OpenCbsReader r = c.ExecuteReader())
                    {
                        if (r == null || r.Empty)
                        {
                            loanSavingsId = 0;
                        }
                        else
                        {
                            r.Read();
                            loanSavingsId = r.GetInt("id");
                        }
                    }
                }

                if (loanSavingsId == 0)
                {
                    sqlCompulsory = @"INSERT INTO LoansLinkSavingsBook ([loan_id], [savings_id], [loan_percentage])
                                      VALUES (@loanId, @savingsId, @loanPercentage)";

                    using (OpenCbsCommand c = new OpenCbsCommand(sqlCompulsory, pSqlTransac.Connection, pSqlTransac))
                    {
                        if (pLoan.CompulsorySavings != null)
                            c.AddParam("@savingsId", pLoan.CompulsorySavings.Id);
                        else
                            c.AddParam("@savingsId", null);

                        c.AddParam("@loanId", pLoan.Id);
                        c.AddParam("@loanPercentage", pLoan.CompulsorySavingsPercentage);
                        c.ExecuteNonQuery();
                    }
                }
                else
                {
                    sqlCompulsory = @"UPDATE [LoansLinkSavingsBook] 
                                             SET savings_id = @savingsId, loan_percentage = @loanPercentage
                                             WHERE loan_id = @loanId";

                    using (OpenCbsCommand c = new OpenCbsCommand(sqlCompulsory, pSqlTransac.Connection, pSqlTransac))
                    {
                        c.AddParam("@savingsId", pLoan.CompulsorySavings.Id);
                        c.AddParam("@loanId", pLoan.Id);
                        c.AddParam("@loanPercentage", pLoan.CompulsorySavingsPercentage);
                        c.ExecuteNonQuery();
                    }
                }
            }
        }

        private void SetLoanForUpdate(OpenCbsCommand c, Loan pLoan)
        {
            c.AddParam("@id", pLoan.Id);
            c.AddParam("@fundingLine_id", pLoan.FundingLine.Id);
            c.AddParam("@loanOfficerId", pLoan.LoanOfficer.Id);
            c.AddParam("@disbursed", pLoan.Disbursed);
            c.AddParam("@rescheduled", pLoan.Rescheduled);
            c.AddParam("@synchronize", pLoan.Synchronize);

            c.AddParam("@NbOfInstallment", pLoan.NbOfInstallments);
            c.AddParam("@Amount", pLoan.Amount);
            c.AddParam("@InterestRate", pLoan.InterestRate);
            c.AddParam("@GracePeriode", pLoan.GracePeriod);
            c.AddParam("@AnticipatedPartialRepayment", pLoan.AnticipatedPartialRepaymentPenalties);
            c.AddParam("@AnticipatedTotalRepayment", pLoan.AnticipatedTotalRepaymentPenalties);
            c.AddParam("@NRPBOOP", pLoan.NonRepaymentPenalties.OverDuePrincipal);
            c.AddParam("@NRPBOIA", pLoan.NonRepaymentPenalties.InitialAmount);
            c.AddParam("@NRPBOOLB", pLoan.NonRepaymentPenalties.OLB);
            c.AddParam("@NRPBOOI", pLoan.NonRepaymentPenalties.OverDueInterest);
            c.AddParam("@grace_period_of_latefees", pLoan.GracePeriodOfLateFees);
            c.AddParam("@schedule_changed", pLoan.ScheduleChangedManually);
            c.AddParam("@written_off", pLoan.WrittenOff);

            c.AddParam("@DrawingsNumber", pLoan.DrawingsNumber);
            c.AddParam("@AmountUnderLoc", pLoan.AmountUnderLoc);
            c.AddParam("@MaturityLoc", pLoan.MaturityLoc);

            c.AddParam("@AnticipatedTotalRepaymentPenaltiesBase", (int) pLoan.AnticipatedTotalRepaymentPenaltiesBase);
            c.AddParam("@AnticipatedPartialRepaymentPenaltiesBase", (int) pLoan.AnticipatedPartialRepaymentPenaltiesBase);
            c.AddParam("@insurance", pLoan.Insurance);
        }

        private void _DeleteGuarantorsFromLoan(int pLoanId, SqlTransaction pSqlTransac)
        {
            const string q = "DELETE FROM [LinkGuarantorCredit] WHERE contract_id = @contractId";

            using (OpenCbsCommand c = new OpenCbsCommand(q, pSqlTransac.Connection, pSqlTransac))
            {
                c.AddParam("@contractId", pLoanId);
                c.ExecuteNonQuery();
            }
        }

        private void _DeleteCollateralsFromLoan(int pLoanId, SqlTransaction pSqlTransac)
        {
            List<int> collateralIds = new List<int>();

            const string q =
                @"SELECT [id] FROM [CollateralsLinkContracts] WHERE contract_id = @contract_id ";

            using (OpenCbsCommand c = new OpenCbsCommand(q, pSqlTransac.Connection, pSqlTransac))
            {
                c.AddParam("@contract_id", pLoanId);
                using (OpenCbsReader r = c.ExecuteReader())
                {
                    if (r.Empty) return; // nothing is coming... (c)
                    while (r.Read()) collateralIds.Add(r.GetInt("id"));
                }
            }

            foreach (int collateralId in collateralIds)
            {
                string sqlText =
                    @"DELETE FROM CollateralPropertyValues WHERE [contract_collateral_id] = @contract_collateral_id";

                using (OpenCbsCommand c = new OpenCbsCommand(sqlText, pSqlTransac.Connection, pSqlTransac))
                {
                    c.AddParam("@contract_collateral_id", collateralId);
                    c.ExecuteNonQuery();
                }
            }

            string sqlText2 = @"DELETE FROM CollateralsLinkContracts WHERE [contract_id] = @contract_id";

            using (OpenCbsCommand c = new OpenCbsCommand(sqlText2, pSqlTransac.Connection, pSqlTransac))
            {
                c.AddParam("@contract_id", pLoanId);
                c.ExecuteNonQuery();
            }
        }

        public void ReassignLoans(int[] ids, int loanOfficerId, SqlTransaction transaction)
        {
            var query = ReadQuery("LoanManager.ReassignLoansHistory.sql");
            transaction.Connection.Execute(query, new {To = loanOfficerId, Ids = ids}, transaction);
            query = ReadQuery("LoanManager.ReassignLoans.sql");
            transaction.Connection.Execute(query, new {To = loanOfficerId, Ids = ids}, transaction);
        }

        public void UpdateLoanLoanOfficer(int pLoanId, int pOfficerToId, int pOfficerFromId, SqlTransaction pTransac)
        {
            string q = @"UPDATE Credit SET loanofficer_id = @loanofficerID WHERE id = @ID";
            using (OpenCbsCommand c = new OpenCbsCommand(q, pTransac.Connection, pTransac))
            {
                c.AddParam("@loanofficerID", pOfficerToId);
                c.AddParam("@ID", pLoanId);

                c.ExecuteNonQuery();
            }

            if (pOfficerFromId != pOfficerToId)
            {
                q = @"INSERT INTO ContractAssignHistory (loanofficerFrom_id, loanofficerTo_id, contract_id)
                            VALUES (@loanofficerFrom_id, @loanofficerTo_id, @contract_id)";

                using (OpenCbsCommand c = new OpenCbsCommand(q, pTransac.Connection, pTransac))
                {
                    c.AddParam("@loanofficerFrom_id", pOfficerFromId);
                    c.AddParam("@loanofficerTo_id", pOfficerToId);
                    c.AddParam("@contract_id", pLoanId);

                    c.ExecuteNonQuery();
                }
            }
        }

        private static Alert GetAlert(OpenCbsReader pReader, char pType)
        {
            int i = 0;
            var alert = new Alert();

            while (i < pReader.FieldCount)
            {
                alert.AddParameter(pReader.GetName(i), pReader.GetValue(i));
                i++;
            }

            alert.Type = pType;
            return alert;
        }

        public AlertStock SelectLoansByLoanOfficer(int pLoanOfficerId)
        {
            string addLoanOfficer = "";

            if (pLoanOfficerId != 0)
                addLoanOfficer = " AND (Credit.loanofficer_id = @loanOfficerId) ";

            string sqlTextRepaymentAlert =
                string.Format(@"SELECT Credit.id AS contract_id, 
                                  Credit.interest_rate, 
                                  Contracts.contract_code,
                                  Contracts.creation_date, 
                                  Contracts.start_date, 
                                  Contracts.align_disbursed_date, 
                                  Contracts.close_date, 
                                  CASE Contracts.status
                                    WHEN 1 THEN 'Pending'
                                    WHEN 2 THEN 'Validated'
                                    WHEN 3 THEN 'Refused'
                                    WHEN 4 THEN 'Abandoned'
                                    WHEN 5 THEN 'Active'
                                    WHEN 6 THEN 'Closed'
                                    WHEN 7 THEN 'WrittenOff'
                                    ELSE '-'
                                  END AS loan_status,
                                  InstallmentTypes.name AS installment_types,  
                                  ISNULL(Groups.name, ISNULL(Persons.first_name + ' ' + Persons.last_name,Corporates.name)) AS client_name,
                                  Districts.name as district_name, Installments.capital_repayment + Installments.interest_repayment
                                  - Installments.paid_capital - Installments.paid_interest AS amount,
                                  Installments.expected_date AS effect_date, ISNULL(( SELECT SUM(principal) FROM contractEvents
                                  INNER JOIN repaymentEvents ON repaymentEvents.id = contractEvents.id WHERE is_deleted = 0
                                  AND contract_id = Contracts.id ), 0) AS olb FROM Credit
                                  INNER JOIN Contracts ON Contracts.id = Credit.id
                                  INNER JOIN Installments ON Installments.contract_id = Credit.id
                                  INNER JOIN InstallmentTypes ON dbo.Credit.installment_type = dbo.InstallmentTypes.id
                                  INNER JOIN Projects ON Contracts.project_id = Projects.id
                                  INNER JOIN Tiers ON Projects.tiers_id = Tiers.id
                                  LEFT OUTER JOIN Corporates ON Tiers.id=Corporates.id
                                  LEFT OUTER JOIN Persons ON dbo.Tiers.id = Persons.id
                                  LEFT OUTER JOIN Groups ON dbo.Tiers.id = Groups.id
                                  LEFT OUTER JOIN Districts ON dbo.Tiers.district_id = Districts.id
                                  WHERE ( Installments.capital_repayment + Installments.interest_repayment - Installments.paid_capital - Installments.paid_interest > 0.02 ) AND Contracts.status = 5 
                                  {0}
                                  ORDER BY contract_id, effect_date DESC", addLoanOfficer);
            using (SqlConnection conn = GetConnection())
            using (OpenCbsCommand cmdSelectRepayment = new OpenCbsCommand(sqlTextRepaymentAlert, conn))
            {
                if (pLoanOfficerId != 0)
                    cmdSelectRepayment.AddParam("@loanOfficerId", pLoanOfficerId);

                using (OpenCbsReader reader = cmdSelectRepayment.ExecuteReader())
                {
                    if (reader.Empty) return new AlertStock();

                    AlertStock alertStock = new AlertStock();
                    while (reader.Read())
                    {
                        alertStock.Add(GetAlert(reader, 'R'));
                    }
                    return alertStock;
                }
            }
        }

        public IEnumerable<ReassignContractItem> SelectLoansByLoanOfficerId(int id, bool onlyActive)
        {
            var query = ReadQuery("LoanManager.GetReassignContractItems.sql");
            using (var connection = GetConnection())
            {
                return connection.Query<ReassignContractItem>(query, new {Id = id});
            }
        }

        public List<KeyValuePair<DateTime, decimal>> CalculateCashToDisburseByDay(DateTime pStartDate, DateTime pEndDate)
        {
            const string q = @"SELECT SUM(Credit.amount) AS amount, Contracts.start_date AS date FROM Contracts 
                    INNER JOIN Credit ON Contracts.id = Credit.id WHERE (Credit.disbursed = 0)
                    AND Contracts.start_date >= @startDate AND Contracts.start_date <= @endDate
                    GROUP BY Contracts.start_date ORDER BY Contracts.start_date ";
            using (SqlConnection conn = GetConnection())
            using (OpenCbsCommand c = new OpenCbsCommand(q, conn))
            {
                c.AddParam("@startDate", pStartDate);
                c.AddParam("@endDate", pEndDate);

                using (OpenCbsReader r = c.ExecuteReader())
                {
                    if (r.Empty) return new List<KeyValuePair<DateTime, decimal>>();

                    List<KeyValuePair<DateTime, decimal>> list = new List<KeyValuePair<DateTime, decimal>>();
                    while (r.Read())
                    {
                        list.Add(new KeyValuePair<DateTime, decimal>(r.GetDateTime("date"), (r.GetMoney("amount")).Value));
                    }
                    return list;
                }
            }
        }

        public List<KeyValuePair<DateTime, decimal>> CalculateCashToRepayByDay(DateTime pStartDate, DateTime pEndDate)
        {
            const string q = @"SELECT SUM(Installments.interest_repayment + Installments.capital_repayment - 
                        Installments.paid_interest - Installments.paid_capital) AS amount, Installments.expected_date AS date
                        FROM Credit INNER JOIN Contracts ON Credit.id = Contracts.id
                        INNER JOIN Installments ON Credit.id = Installments.contract_id WHERE (Credit.disbursed = 1)
                        AND (Credit.written_off = 0) AND (Credit.bad_loan = 0) AND Installments.expected_date >= @startDate 
                        AND Installments.expected_date <= @endDate AND
                        (NOT (Installments.interest_repayment = Installments.paid_interest)) AND
                                    (NOT (Installments.capital_repayment = Installments.paid_capital))
                                    GROUP BY Installments.expected_date
                                    ORDER BY Installments.expected_date";
            using (SqlConnection conn = GetConnection())
            using (OpenCbsCommand c = new OpenCbsCommand(q, conn))
            {
                c.AddParam("@startDate", pStartDate);
                c.AddParam("@endDate", pEndDate);

                using (OpenCbsReader r = c.ExecuteReader())
                {
                    if (r == null || r.Empty) return new List<KeyValuePair<DateTime, decimal>>();

                    List<KeyValuePair<DateTime, decimal>> list = new List<KeyValuePair<DateTime, decimal>>();
                    while (r.Read())
                    {
                        list.Add(new KeyValuePair<DateTime, decimal>(r.GetDateTime("date"), (r.GetMoney("amount")).Value));
                    }
                    return list;
                }
            }
        }

        public List<KeyValuePair<DateTime, decimal>> CalculateCashToRepayByDayByFundingLine(int pFundingLineId,
                                                                                            bool
                                                                                                pAssumeLateLoansRepaidToday,
                                                                                            bool
                                                                                                pCreditInterestsInFundingLine)
        {
            string q = pCreditInterestsInFundingLine
                           ? @"SELECT SUM(Installments.capital_repayment + Installments.interest_repayment) AS amount,"
                           : "SELECT SUM(Installments.capital_repayment) AS amount,";
            q += @"Installments.expected_date AS date
                            FROM Credit INNER JOIN Contracts ON Credit.id = Contracts.id
                            INNER JOIN Installments ON Credit.id = Installments.contract_id
                            WHERE (Credit.disbursed = 1) AND (Credit.written_off = 0) 
							AND (Credit.bad_loan = 0) AND Credit.fundingline_id = 1 AND Installments.expected_date > GETDATE()
                            GROUP BY Installments.expected_date ";

            if (pAssumeLateLoansRepaidToday)
            {
                q += " UNION ALL ";
                q += pCreditInterestsInFundingLine
                         ? @"SELECT SUM(Installments.capital_repayment + Installments.interest_repayment
                                        - Installments.paid_capital-Installments.paid_interest) AS amount,"
                         : "SELECT SUM(Installments.capital_repayment - Installments.paid_capital) AS amount,";

                q += @" Installments.expected_date AS date
                            FROM Credit INNER JOIN Contracts ON Credit.id = Contracts.id
                            INNER JOIN Installments ON Credit.id = Installments.contract_id
                            WHERE (Credit.disbursed = 1) AND (Credit.written_off = 0) 
							AND (Credit.bad_loan = 0) AND Credit.fundingline_id = @fundingLineId AND Installments.expected_date <= GETDATE()
                            GROUP BY Installments.expected_date";
            }
            using (SqlConnection conn = GetConnection())
            using (OpenCbsCommand c = new OpenCbsCommand(q, conn))
            {
                c.AddParam("@fundingLineId", pFundingLineId);
                using (OpenCbsReader r = c.ExecuteReader())
                {
                    if (r == null || r.Empty) return new List<KeyValuePair<DateTime, decimal>>();

                    List<KeyValuePair<DateTime, decimal>> list = new List<KeyValuePair<DateTime, decimal>>();
                    while (r.Read())
                    {
                        list.Add(new KeyValuePair<DateTime, decimal>(r.GetDateTime("date"), (r.GetMoney("amount")).Value));
                    }
                    return list;
                }
            }
        }

        public decimal GetGlobalOLBForProvisionning()
        {
            const string q = @"SELECT ISNULL(SUM(Installments.capital_repayment - Installments.paid_capital),0) 
                    FROM Credit 
                   INNER JOIN Installments ON Credit.id = Installments.contract_id 
                    WHERE (Credit.disbursed = 1) 
                      AND (Credit.written_off = 0) 
                      AND (Credit.bad_loan = 0)";
            using (SqlConnection conn = GetConnection())
            using (OpenCbsCommand c = new OpenCbsCommand(q, conn))
            {
                return Convert.ToDecimal(c.ExecuteScalar());
            }
        }

        public List<Loan> SelectLoansByClientId(int clientId)
        {
            List<int> ids = new List<int>();
            const string q = @"SELECT Cont.id FROM Contracts AS Cont
                                     INNER JOIN Projects AS Pr ON Cont.project_id = Pr.id
                                     INNER JOIN Tiers AS Tr ON Tr.id = Pr.tiers_id
                                     WHERE Tr.id = @id";
            using (SqlConnection conn = GetConnection())
            using (OpenCbsCommand c = new OpenCbsCommand(q, conn))
            {
                c.AddParam("@id", clientId);

                using (OpenCbsReader r = c.ExecuteReader())
                {
                    if (r == null || r.Empty) return new List<Loan>();

                    while (r.Read())
                        ids.Add(r.GetInt("id"));
                }
            }

            List<Loan> loans = new List<Loan>();
            foreach (int id in ids)
                loans.Add(SelectLoan(id, true, true, true));

            return loans;
        }

        public List<Loan> SelectLoansByProject(int pProjectId)
        {
            List<int> ids = new List<int>();
            const string q = @"SELECT Credit.id 
                                    FROM Contracts,Credit 
                                    WHERE Contracts.id = Credit.id AND project_id = @id";
            using (SqlConnection conn = GetConnection())
            using (OpenCbsCommand c = new OpenCbsCommand(q, conn))
            {
                c.AddParam("@id", pProjectId);

                using (OpenCbsReader r = c.ExecuteReader())
                {
                    if (r == null || r.Empty) return new List<Loan>();
                    while (r.Read())
                    {
                        ids.Add(r.GetInt("id"));
                    }
                }
            }

            List<Loan> loans = new List<Loan>();
            foreach (int id in ids)
            {
                loans.Add(SelectLoan(id, true, true, true));
            }
            return loans;
        }

        public void UpdateLoanStatus(Loan pLoan)
        {
            using (SqlConnection conn = GetConnection())
            using (SqlTransaction t = conn.BeginTransaction())
            {
                UpdateLoanStatus(pLoan, t);
                t.Commit();
            }
        }

        public void UpdateLoanStatus(Loan pLoan, SqlTransaction pTransaction)
        {
            string q = @"UPDATE [Contracts] SET [status] = @status, 
                                                      [credit_commitee_date] = @date, 
                                                      [credit_commitee_comment] = @comment, 
                                                      [credit_commitee_code] = @credit_commitee_code,
                                                      [closed] = @closed WHERE id = @id";

            using (OpenCbsCommand c = new OpenCbsCommand(q, pTransaction.Connection, pTransaction))
            {
                c.AddParam("@id", pLoan.Id);
                c.AddParam("@status", (int) pLoan.ContractStatus);
                c.AddParam("@date", pLoan.CreditCommiteeDate);
                c.AddParam("@comment", pLoan.CreditCommiteeComment);
                c.AddParam("@credit_commitee_code", pLoan.CreditCommitteeCode);
                c.AddParam("@closed", pLoan.Closed);

                c.ExecuteNonQuery();
            }

            // Updating Tiers status to 'active'
            if (pLoan.Project != null)
            {
                q = @"UPDATE Tiers SET active = @active WHERE id = @id";

                using (OpenCbsCommand c = new OpenCbsCommand(q, pTransaction.Connection, pTransaction))
                {
                    c.AddParam("@active", pLoan.Project.Client.Active);
                    c.AddParam("@id", pLoan.Project.Client.Id);
                    c.ExecuteNonQuery();
                }
            }
        }

        public List<Loan> SelectLoansForClosure()
        {
            List<int> ids = new List<int>();
            const string q = @"SELECT Credit.id AS id 
                                     FROM Credit 
                                     INNER JOIN Contracts ON Credit.id = Contracts.id
                                     WHERE Credit.disbursed = 1 
                                       AND Credit.written_off = 0 
                                       AND Contracts.closed = 0";

            using (SqlConnection conn = GetConnection())
            using (OpenCbsCommand c = new OpenCbsCommand(q, conn))
            using (OpenCbsReader r = c.ExecuteReader())
            {
                if (r == null || r.Empty) return new List<Loan>();
                while (r.Read())
                {
                    ids.Add(r.GetInt("id"));
                }
            }

            List<Loan> loans = new List<Loan>();
            foreach (int i in ids)
            {
                var loan = SelectLoan(i, true, true, false);
                if (_projectManager != null)
                    loan.Project = _projectManager.SelectProjectByContractId(loan.Id);
                loans.Add(loan);
                System.Diagnostics.Debug.WriteLine(i);
            }
            return loans;
        }

        public List<Loan> SelectLoansForClosure(OClosureTypes pClosureType)
        {
            List<int> ids = new List<int>();
            string q = pClosureType == OClosureTypes.Degradation
                           ? @"SELECT DISTINCT Credit.id AS id 
                                     FROM Credit 
                                     WHERE Credit.disbursed = 1 
                                        AND Credit.written_off = 0 
                                        AND (NOT ((SELECT SUM(interest_repayment) + SUM(capital_repayment) - 
                                                   SUM(paid_interest) - SUM(paid_capital) 
                                                   FROM Installments 
                                                   WHERE contract_id = Credit.id) < 0.02))"
                           : @"SELECT DISTINCT Contracts.id 
                                    FROM Contracts 
                                    INNER JOIN Credit ON Contracts.id = Credit.id 
                                        WHERE (Credit.disbursed = 1)                                           
                                          AND (Credit.written_off = 0) 
                                          AND (Credit.bad_loan = 0) 
                                          AND (Contracts.closed = 0)";
            using (SqlConnection conn = GetConnection())
            using (OpenCbsCommand c = new OpenCbsCommand(q, conn))
            {
                using (OpenCbsReader r = c.ExecuteReader())
                {
                    if (r == null || r.Empty) return new List<Loan>();

                    while (r.Read())
                    {
                        ids.Add(r.GetInt("id"));
                    }
                }
            }

            List<Loan> loans = new List<Loan>();
            foreach (int i in ids)
            {
                var loan = SelectLoan(i, true, true, false);
                if (_projectManager != null)
                    loan.Project = _projectManager.SelectProjectByContractId(loan.Id);
                loans.Add(loan);
                System.Diagnostics.Debug.WriteLine(i);
            }
            return loans;
        }

        public void DeleteLoanShareAmountWhereNotDisbursed(int groupId)
        {
            const string q = @"
                                  DELETE FROM [dbo].[LoanShareAmounts]
                                  WHERE group_id=@group_id 
                                  AND 
                                  ( contract_id NOT IN
	                                (SELECT id 
	                                 FROM [dbo].[Credit] 
	                                 WHERE disbursed=1)
                                   )";
            using (SqlConnection conn = GetConnection())
            using (OpenCbsCommand c = new OpenCbsCommand(q, conn))
            {
                c.AddParam("@group_id", groupId);
                c.ExecuteNonQuery();
            }
        }

        public void UpdateLoanShare(LoanShare pLoanShare, int pLoanId, int pGroupId, SqlTransaction pSqlTransac)
        {
            // Check if share exists
            const string q = @"SELECT COUNT(*) 
                                   FROM dbo.LoanShareAmounts 
                                   WHERE person_id = @person_id
                                     AND group_id = @group_id 
                                     AND contract_id = @contract_id";
            bool exists;
            using (OpenCbsCommand c = new OpenCbsCommand(q, pSqlTransac.Connection, pSqlTransac))
            {
                c.AddParam("@person_id", pLoanShare.PersonId);
                c.AddParam("@group_id", pGroupId);
                c.AddParam("@contract_id", pLoanId);
                c.AddParam("@amount", pLoanShare.Amount);
                exists = Convert.ToInt32(c.ExecuteScalar()) > 0;
            }

            string query;
            if (exists)
            {
                query = @"UPDATE LoanShareAmounts 
                         SET amount = @amount 
                         WHERE person_id = @person_id 
                         AND group_id = @group_id 
                           AND contract_id = @contract_id";
            }
            else
            {
                query = @"INSERT INTO LoanShareAmounts (person_id, group_id, contract_id, amount)
                          VALUES (@person_id, @group_id, @contract_id, @amount)";
            }

            using (OpenCbsCommand c = new OpenCbsCommand(query, pSqlTransac.Connection, pSqlTransac))
            {
                c.AddParam("@person_id", pLoanShare.PersonId);
                c.AddParam("@group_id", pGroupId);
                c.AddParam("@contract_id", pLoanId);
                c.AddParam("@amount", pLoanShare.Amount);
                c.ExecuteNonQuery();
            }
        }

        private int _AddContract(Loan pContract, int pProjectId, SqlTransaction pSqlTransac)
        {
            const string q = @"INSERT INTO [Contracts]([contract_code], 
            [branch_code], 
            [closed], 
            [creation_date], 
            [start_date], 
            [align_disbursed_date], 
            [close_date], 
            [project_id],
            [status],
            [credit_commitee_date],
            [credit_commitee_comment],
            [credit_commitee_code],
            [loan_purpose],
            [comments],
            [nsg_id],
            [activity_id],
            preferred_first_installment_date)
            VALUES(@code, 
            @branchCode, 
            @closed, 
            @creationDate, 
            @startDate,
            @align_disbursed_date, 
            @closeDate, 
            @projectId,
            @status,
            @creditCommiteeDate,
            @creditCommiteeComment,
            @creditCommiteeCode,
            @loanPurpose,
            @comments,
            @NsgID,
            @activityId,
            @preferredFirstInstallmentDate)
            SELECT SCOPE_IDENTITY()";

            using (OpenCbsCommand c = new OpenCbsCommand(q, pSqlTransac.Connection, pSqlTransac))
            {

                c.AddParam("@code", "fake_code");
                c.AddParam("@status", (int) pContract.ContractStatus);
                c.AddParam("@creditCommiteeDate", pContract.CreditCommiteeDate);
                c.AddParam("@creditCommiteeComment", pContract.CreditCommiteeComment);
                c.AddParam("@creditCommiteeCode", pContract.CreditCommitteeCode);
                c.AddParam("@branchCode", pContract.BranchCode);
                c.AddParam("@closed", pContract.Closed);
                c.AddParam("@creationDate", pContract.CreationDate);
                c.AddParam("@startDate", pContract.StartDate);
                c.AddParam("@align_disbursed_date", pContract.AlignDisbursementDate);
                c.AddParam("@closeDate", pContract.CloseDate);
                c.AddParam("@projectId", pProjectId);
                c.AddParam("@loanPurpose", pContract.LoanPurpose);
                c.AddParam("@comments", pContract.Comments);
                c.AddParam("@NsgID", pContract.NsgID);
                c.AddParam("activityId", pContract.EconomicActivityId);
                c.AddParam("@preferredFirstInstallmentDate", pContract.FirstInstallmentDate);

                pContract.Id = Convert.ToInt32(c.ExecuteScalar());
            }

            if (string.IsNullOrEmpty(pContract.Code)) pContract.Code = "-";

            if (pContract.Guarantors.Count != 0)
                foreach (Guarantor guarantor in pContract.Guarantors)
                    _AddGuarantor(guarantor, pContract.Id, pSqlTransac);

            if (pContract.Collaterals.Count != 0)
                foreach (ContractCollateral contractCollateral in pContract.Collaterals)
                    AddCollateral(contractCollateral, pContract.Id, pSqlTransac);

            // Compulsory savings handling
            //if (pContract.Product.UseCompulsorySavings)
            if (pContract.CompulsorySavings != null)
            {
                const string sqlText = @"INSERT INTO LoansLinkSavingsBook ([loan_id], [savings_id], [loan_percentage])
                                                   VALUES (@loanId, @savingsId, @loanPercentage)";

                using (OpenCbsCommand c = new OpenCbsCommand(sqlText, pSqlTransac.Connection, pSqlTransac))
                {
                    if (pContract.CompulsorySavings != null)
                        c.AddParam("@savingsId", pContract.CompulsorySavings.Id);
                    else
                        c.AddParam("@savingsId", null);

                    c.AddParam("@loanId", pContract.Id);
                    c.AddParam("@loanPercentage", pContract.CompulsorySavingsPercentage);
                    c.ExecuteNonQuery();
                }
            }

            return pContract.Id;
        }

        public void UpdateContractCode(int contractId, string contractCode, SqlTransaction pSqlTransac)
        {
            const string q = @"UPDATE Contracts SET contract_code = @code WHERE id = @id";
            using (OpenCbsCommand c = new OpenCbsCommand(q, pSqlTransac.Connection, pSqlTransac))
            {
                c.AddParam("@code", contractCode);
                c.AddParam("@id", contractId);
                c.ExecuteNonQuery();
            }
        }

        public void AddCollateral(ContractCollateral contractCollateral, int contractId, SqlTransaction pSqlTransac)
        {
            string q = @"INSERT INTO [CollateralsLinkContracts] ([contract_id]) 
                                      VALUES (@contract_id) SELECT CONVERT(int, SCOPE_IDENTITY())";

            using (OpenCbsCommand c = new OpenCbsCommand(q, pSqlTransac.Connection, pSqlTransac))
            {
                c.AddParam("@contract_id", contractId);
                contractCollateral.Id = Convert.ToInt32(c.ExecuteScalar());
            }

            foreach (CollateralPropertyValue propertyValue in contractCollateral.PropertyValues)
            {
                AddCollateralPropertyValue(contractCollateral, propertyValue, pSqlTransac);
            }
        }

        public void AddCollateralPropertyValue(ContractCollateral contractCollateral,
                                               CollateralPropertyValue propertyValue, SqlTransaction pSqlTransac)
        {
            string q = @"INSERT INTO [CollateralPropertyValues] ([contract_collateral_id], [property_id], [value]) 
                                       VALUES (@contract_collateral_id, @property_id, @value)";

            using (OpenCbsCommand c = new OpenCbsCommand(q, pSqlTransac.Connection, pSqlTransac))
            {
                c.AddParam("@contract_collateral_id", contractCollateral.Id);
                c.AddParam("@property_id", propertyValue.Property.Id);
                c.AddParam("@value", propertyValue.Value);
                c.ExecuteNonQuery();
            }
        }

        private void _AddGuarantor(Guarantor pGuarantor, int pLoanId, SqlTransaction pSqlTransac)
        {
            const string q =
                @"INSERT INTO [LinkGuarantorCredit]([tiers_id], [contract_id], [guarantee_amount], [guarantee_desc]) 
                            VALUES(@tiersId, @contractId, @guaranteeAmount, @guaranteeDesc)";

            using (OpenCbsCommand c = new OpenCbsCommand(q, pSqlTransac.Connection, pSqlTransac))
            {
                c.AddParam("@tiersId", pGuarantor.Tiers.Id);
                c.AddParam("@contractId", pLoanId);
                c.AddParam("@guaranteeAmount", pGuarantor.Amount);
                c.AddParam("@guaranteeDesc", pGuarantor.Description);

                c.ExecuteNonQuery();
            }
        }

        public void RemoveCompulsorySavings(int loanId, SqlTransaction pSqlTransac)
        {
            const string q = @"UPDATE [LoansLinkSavingsBook] 
                                           SET savings_id = NULL 
                                           WHERE loan_id = @loanId";

            using (OpenCbsCommand c = new OpenCbsCommand(q, pSqlTransac.Connection, pSqlTransac))
            {
                c.AddParam("@loanId", loanId);
                c.ExecuteNonQuery();
            }
        }

        public void UpdateMeetingAttendees(VillageAttendee attendee)
        {
            if (attendee.Id == 0)
            {
                const string q =
                    @"INSERT INTO dbo.VillagesAttendance (village_id, person_id, [date], attended, comment, loan_id)
                                         VALUES (@village_id, @person_id, @attended_date, @attended, @comment, @loan_id) 
                                         SELECT SCOPE_IDENTITY()";

                using (SqlConnection conn = GetConnection())
                using (OpenCbsCommand c = new OpenCbsCommand(q, conn))
                {
                    c.AddParam("@attended", attendee.Attended);
                    c.AddParam("@comment", attendee.Comment);
                    c.AddParam("@village_id", attendee.VillageId);
                    c.AddParam("@person_id", attendee.TiersId);
                    c.AddParam("@attended_date", attendee.AttendedDate);
                    c.AddParam("@loan_id", attendee.LoanId);
                    attendee.Id = Convert.ToInt32(c.ExecuteScalar());
                }
            }
            else
            {
                const string q = @"UPDATE dbo.VillagesAttendance
                                         SET attended = @attended, comment = @comment 
                                         WHERE village_id = @village_id 
                                            AND person_id = @person_id 
                                        AND [date] = @attended_date";
                using (SqlConnection conn = GetConnection())
                using (OpenCbsCommand c = new OpenCbsCommand(q, conn))
                {
                    c.AddParam("@attended", attendee.Attended);
                    c.AddParam("@comment", attendee.Comment);
                    c.AddParam("@village_id", attendee.VillageId);
                    c.AddParam("@person_id", attendee.TiersId);
                    c.AddParam("@attended_date", attendee.AttendedDate);
                    c.ExecuteNonQuery();
                }
            }
        }

        public List<VillageAttendee> SelectMeetingAttendees(int villageId, DateTime date)
        {
            List<VillageAttendee> attendees = new List<VillageAttendee>();

            string q =
                @"SELECT
                    VillagesAttendance.id AS attendee_id,
                    Persons.id AS person_id,
                    Persons.first_name + SPACE(1) + Persons.last_name AS person_name,
                    [date],
                    attended,
                    comment,
                    loan_id
                    FROM dbo.VillagesAttendance
                    INNER JOIN dbo.Persons ON dbo.VillagesAttendance.person_id = dbo.Persons.id
                    WHERE village_id = @village_id AND [date] = @attended_date";

            using (SqlConnection conn = GetConnection())
            using (OpenCbsCommand c = new OpenCbsCommand(q, conn))
            {
                c.AddParam("@village_id", villageId);
                c.AddParam("@attended_date", date);
                using (OpenCbsReader r = c.ExecuteReader())
                {
                    while (r.Read())
                    {
                        VillageAttendee attendee = new VillageAttendee();
                        attendee.Id = r.GetInt("attendee_id");
                        attendee.VillageId = villageId;
                        attendee.TiersId = r.GetInt("person_id");
                        attendee.PersonName = r.GetString("person_name");
                        attendee.AttendedDate = r.GetDateTime("date");
                        attendee.Attended = r.GetBool("attended");
                        attendee.Comment = r.GetString("comment");
                        attendee.LoanId = r.GetInt("loan_id");
                        attendees.Add(attendee);
                    }
                }
            }

            if (attendees.Count == 0)
            {
                q =
                    @"SELECT
                    CAST(0 AS int) AS attendee_id,
                    Persons.id AS person_id,
                    Persons.first_name + SPACE(1) + Persons.last_name AS person_name,
                    Installments.expected_date AS [date],
                    CASE
                        WHEN (SELECT COUNT(*) FROM dbo.ContractEvents 
                             INNER JOIN dbo.RepaymentEvents ON dbo.ContractEvents.id = dbo.RepaymentEvents.id
                             WHERE ContractEvents.contract_id = c.id AND event_date = @attended_date 
                             AND RepaymentEvents.installment_number = Installments.number) > 0 THEN CAST(1 AS bit) 
                        ELSE CAST(0 AS bit) 
                    END AS attended,
                    '' AS comment,
                    c.id AS loan_id
                    FROM dbo.VillagesPersons vp
                    INNER JOIN dbo.Persons ON vp.person_id = dbo.Persons.id
                    INNER JOIN dbo.Projects pr ON pr.tiers_id = vp.person_id
                    INNER JOIN dbo.Contracts c ON c.project_id = pr.id
                    INNER JOIN dbo.Installments ON Installments.contract_id = c.id
                    WHERE village_id = @village_id AND c.nsg_id IS NOT NULL 
                    AND Installments.expected_date = @attended_date
                    AND c.[status] = 5";

                using (SqlConnection conn = GetConnection())
                using (OpenCbsCommand c = new OpenCbsCommand(q, conn))
                {
                    c.AddParam("@village_id", villageId);
                    c.AddParam("@attended_date", date);
                    using (OpenCbsReader r = c.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            VillageAttendee attendee = new VillageAttendee();
                            attendee.Id = r.GetInt("attendee_id");
                            attendee.VillageId = villageId;
                            attendee.TiersId = r.GetInt("person_id");
                            attendee.PersonName = r.GetString("person_name");
                            attendee.AttendedDate = r.GetDateTime("date");
                            attendee.Attended = r.GetBool("attended");
                            attendee.Comment = r.GetString("comment");
                            attendee.LoanId = r.GetInt("loan_id");
                            attendees.Add(attendee);
                        }
                    }
                }
            }

            return attendees;
        }

        public List<DateTime> SelectInstallmentDatesForVillageActiveContracts(int villageId)
        {
            List<DateTime> installmentDates = new List<DateTime>();
            string q =
                @"SELECT  
                    Installments.expected_date
                    FROM dbo.VillagesPersons vp
                    INNER JOIN dbo.Projects pr ON pr.tiers_id = vp.person_id
                    INNER JOIN dbo.Contracts c ON c.project_id = pr.id
                    INNER JOIN dbo.Installments ON Installments.contract_id = c.id
                    WHERE village_id = @village_id
                    AND c.[status] = 5
                    GROUP BY Installments.expected_date
                    ORDER BY Installments.expected_date";
            using (SqlConnection conn = GetConnection())
            using (OpenCbsCommand c = new OpenCbsCommand(q, conn))
            {
                c.AddParam("@village_id", villageId);
                using (OpenCbsReader r = c.ExecuteReader())
                {
                    while (r.Read())
                    {
                        installmentDates.Add(r.GetDateTime("expected_date"));
                    }
                }
            }
            return installmentDates;
        }

        public List<LoanEntryFee> SelectInstalledLoanEntryFees(int loanId)
        {
            List<LoanEntryFee> loanEntryFees = new List<LoanEntryFee>();
            string q =
                @"SELECT id
                        ,[entry_fee_id]
                        ,[fee_value]
                  FROM [dbo].[CreditEntryFees] 
                  WHERE [credit_id]=@credit_id";
            using (SqlConnection conn = GetConnection())
            using (OpenCbsCommand c = new OpenCbsCommand(q, conn))
            {
                c.AddParam("@credit_id", loanId);
                using (OpenCbsReader r = c.ExecuteReader())
                {
                    while (r.Read())
                    {
                        LoanEntryFee lef = new LoanEntryFee();
                        lef.Id = r.GetInt("id");
                        lef.ProductEntryFeeId = r.GetInt("entry_fee_id");
                        lef.FeeValue = r.GetDecimal("fee_value");
                        loanEntryFees.Add(lef);
                    }
                }
            }
            return loanEntryFees;
        }

        /// <summary>
        /// Inserts entry fees for the specified loan into database
        /// </summary>
        /// <param name="loanEntryFees">List of loan entry fees</param>
        /// <param name="loanId">Loan (credit) id</param>
        /// <param name="transaction">Transaction for action</param>
        public void InsertLoanEntryFees(List<LoanEntryFee> loanEntryFees, int loanId, SqlTransaction transaction)
        {
            const string q = @"INSERT INTO [dbo].[CreditEntryFees]
                 (credit_id, entry_fee_id, fee_value)
                 VALUES (@credit_id, @entry_fee_id, @fee_value)";
            foreach (LoanEntryFee entryFee in loanEntryFees)
            {
                using (var c = new OpenCbsCommand(q, transaction.Connection, transaction))
                {
                    c.AddParam("@credit_id", loanId);
                    c.AddParam("@entry_fee_id", entryFee.ProductEntryFee.Id);
                    c.AddParam("@fee_value", entryFee.FeeValue);
                    c.ExecuteNonQuery();
                }
            }
        }

        public void UpdateLoanEntryFees(List<LoanEntryFee> loanEntryFees, int loanId, SqlTransaction transaction)
        {
            string q =
                @"UPDATE [dbo].[CreditEntryFees]
                SET fee_value = @fee_value
                WHERE id=@loan_entry_fee_id";
            foreach (LoanEntryFee entryFee in loanEntryFees)
            {
                using (OpenCbsCommand c = new OpenCbsCommand(q, transaction.Connection, transaction))
                {
                    c.AddParam("@loan_entry_fee_id", entryFee.Id);
                    c.AddParam("@fee_value", entryFee.FeeValue);
                    c.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// Returns Loan
        /// </summary>
        /// <param name="pLoanId">Loan id</param>
        /// <param name="pAddGeneralInformation">add product, events, chartOfAccount, and funding line</param>
        /// <param name="pAddOptionalInformation"></param>
        /// <param name="pAddOptionalEventInformation"></param>
        /// <returns></returns>
        public Loan SelectLoan(int pLoanId, bool pAddGeneralInformation, bool pAddOptionalInformation,
                               bool pAddOptionalEventInformation)
        {
            Loan loan;
            int productId;
            int installmentTypeId;
            int loanOfficerId;
            int fundingLineId;

            const string q = @"SELECT Credit.id AS credit_id, 
                                        Credit.package_id, 
                                        Credit.amount, 
                                        Credit.interest_rate, 
                                        Credit.installment_type, 
                                        Credit.nb_of_installment, 
                                        Credit.non_repayment_penalties_based_on_overdue_principal,
                                        Credit.non_repayment_penalties_based_on_overdue_interest, 
                                        Credit.non_repayment_penalties_based_on_olb,
                                        Credit.non_repayment_penalties_based_on_initial_amount,
                                        Credit.anticipated_total_repayment_penalties, 
                                        Credit.anticipated_partial_repayment_penalties, 
                                        Credit.anticipated_total_repayment_base,
                                        Credit.anticipated_partial_repayment_base,
                                        Credit.disbursed, Credit.loanofficer_id, 
                                        Credit.fundingLine_id, 
                                        Credit.grace_period, 
                                        Credit.written_off, 
                                        Credit.rescheduled, 
                                        Credit.bad_loan,                                         
                                        Credit.grace_period_of_latefees,
                                        Contracts.contract_code, 
                                        Tiers.client_type_code, 
                                        Credit.synchronize,
                                        Credit.schedule_changed,
                                        ISNULL(Credit.amount_under_loc, 0) AS amount_under_loc,
                                        Contracts.branch_code, 
                                        Contracts.creation_date, 
                                        Contracts.start_date, 
                                        ISNULL(Contracts.align_disbursed_date, Contracts.start_date) AS align_disbursed_date, 
                                        Contracts.close_date, 
                                        Contracts.closed, 
                                        Contracts.status,
                                        Contracts.credit_commitee_date,
                                        Contracts.credit_commitee_comment,
                                        Contracts.credit_commitee_code,
                                        Contracts.loan_purpose,
                                        Contracts.comments,
                                        Contracts.nsg_id,
                                        Contracts.activity_id,
                                        Contracts.preferred_first_installment_date,
                                        LoansLinkSavingsBook.loan_percentage,
                                        Credit.[amount_min],
                                        Credit.[amount_max],
                                        Credit.[ir_min],
                                        Credit.[ir_max],
                                        Credit.[nmb_of_inst_min],
                                        Credit.[nmb_of_inst_max],
                                        Credit.[loan_cycle],
                                        Credit.insurance
                                        FROM Contracts 
                                        INNER JOIN Credit ON Contracts.id = Credit.id 
                                        INNER JOIN Projects ON Contracts.project_id = Projects.id 
                                        INNER JOIN Tiers ON Projects.tiers_id = Tiers.id
                                        LEFT JOIN LoansLinkSavingsBook ON LoansLinkSavingsBook.loan_id = Contracts.id
                                        WHERE Credit.id = @id";
            using (SqlConnection conn = GetConnection())
            using (OpenCbsCommand c = new OpenCbsCommand(q, conn))
            {
                c.AddParam("@id", pLoanId);
                using (OpenCbsReader r = c.ExecuteReader())
                {
                    if (r == null || r.Empty) return null;

                    r.Read();
                    loan = _GetLoan(r);

                    installmentTypeId = r.GetInt("installment_type");
                    productId = r.GetInt("package_id");
                    loanOfficerId = r.GetInt("loanofficer_id");
                    fundingLineId = r.GetInt("fundingLine_id");
                }
            }

            loan.InstallmentType = _installmentTypeManagement.SelectInstallmentType(installmentTypeId);
            loan.InstallmentList = _installmentManagement.SelectInstallments(loan.Id);
            loan.EconomicActivity = _economicActivityManager.SelectEconomicActivity(loan.EconomicActivityId);
            loan.GivenTranches = SelectTranches(loan.Id);

            if (pAddGeneralInformation)
            {
                loan.Product = _packageManager.Select(productId);
                loan.Events = _eventManagement.SelectEvents(loan.Id);

                foreach (Event loanEvent in loan.Events)
                {
                    if (loanEvent is LoanDisbursmentEvent)
                    {
                        if (((LoanDisbursmentEvent) loanEvent).PaymentMethodId == null) continue;
                        int paymentMethodId = (int) ((LoanDisbursmentEvent) loanEvent).PaymentMethodId;
                        loanEvent.PaymentMethod = _paymentMethodManager.SelectPaymentMethodById(paymentMethodId);
                    }
                    if (loanEvent is RepaymentEvent)
                    {
                        if (((RepaymentEvent) loanEvent).PaymentMethodId == null) continue;
                        int paymentMethodId = (int) ((RepaymentEvent) loanEvent).PaymentMethodId;
                        loanEvent.PaymentMethod = _paymentMethodManager.SelectPaymentMethodById(paymentMethodId);
                    }
                }

                if (_projectManager != null)
                    loan.Project = _projectManager.SelectProjectByContractId(loan.Id);
            }

            if (pAddOptionalInformation)
            {
                loan.FundingLine = _fundingLineManager.SelectFundingLineById(fundingLineId, false);
                loan.LoanOfficer = _userManager.SelectUser(loanOfficerId, true);
                loan.Guarantors = GetGuarantors(loan.Id);
                loan.Collaterals = GetCollaterals(loan.Id);

                if (loan.ClientType == OClientTypes.Group)
                    loan.LoanShares.AddRange(GetLoanShareAmount(pLoanId));
            }

            return loan;
        }

        private static void SetLoan(Loan pLoan, OpenCbsCommand c)
        {
            c.AddParam("@id", pLoan.Id);
            c.AddParam("@packageId", pLoan.Product.Id);
            c.AddParam("@amount", pLoan.Amount);
            c.AddParam("@interestRate", pLoan.InterestRate);
            c.AddParam("@installmentType", pLoan.InstallmentType.Id);
            c.AddParam("@nbOfInstallments", pLoan.NbOfInstallments);

            c.AddParam("@anticipatedTotalRepaymentPenalties", pLoan.AnticipatedTotalRepaymentPenalties);
            c.AddParam("@anticipatedPartialRepaymentPenalties", pLoan.AnticipatedPartialRepaymentPenalties);

            c.AddParam("@disbursed", pLoan.Disbursed);
            c.AddParam("@loanOfficerId", pLoan.LoanOfficer.Id);
            c.AddParam("@gracePeriod", pLoan.GracePeriod);
            c.AddParam("@writtenOff", pLoan.WrittenOff);
            c.AddParam("@rescheduled", pLoan.Rescheduled);
            c.AddParam("@synchronize", pLoan.Synchronize);
            c.AddParam("@nonRepaymentPenaltiesInitialAmount", pLoan.NonRepaymentPenalties.InitialAmount);
            c.AddParam("@nonRepaymentPenaltiesOLB", pLoan.NonRepaymentPenalties.OLB);
            c.AddParam("@nonRepaymentPenaltiesOverdueInterest", pLoan.NonRepaymentPenalties.OverDueInterest);
            c.AddParam("@nonRepaymentPenaltiesOverduePrincipal", pLoan.NonRepaymentPenalties.OverDuePrincipal);
            c.AddParam("@badLoan", pLoan.BadLoan);
            c.AddParam("@grace_period_of_latefees", pLoan.GracePeriodOfLateFees);

            c.AddParam("@DrawingsNumber", pLoan.DrawingsNumber);
            c.AddParam("@AmountUnderLoc", pLoan.AmountUnderLoc);
            c.AddParam("@MaturityLoc", pLoan.MaturityLoc);

            c.AddParam("@AnticipatedTotalRepaymentPenaltiesBase", (int) pLoan.AnticipatedTotalRepaymentPenaltiesBase);
            c.AddParam("@AnticipatedPartialRepaymentPenaltiesBase", (int) pLoan.AnticipatedPartialRepaymentPenaltiesBase);

            if (pLoan.FundingLine != null)
                c.AddParam("@fundingLine_id", pLoan.FundingLine.Id);
            else
                c.AddParam("@fundingLine_id", null);

            c.AddParam("@amount_min", pLoan.AmountMin);
            c.AddParam("@amount_max", pLoan.AmountMax);
            c.AddParam("@ir_min", pLoan.InterestRateMin);
            c.AddParam("@ir_max", pLoan.InterestRateMax);
            c.AddParam("@nmb_of_inst_min", pLoan.NmbOfInstallmentsMin);
            c.AddParam("@nmb_of_inst_max", pLoan.NmbOfInstallmentsMax);
            c.AddParam("@loan_cycle", pLoan.LoanCycle);
            c.AddParam("@insurance", pLoan.Insurance);
        }

        public Dictionary<int, int> GetListOfInstallmentsOnDate(DateTime date)
        {
            string q = @"SELECT dbo.Credit.id, dbo.Installments.number
                               FROM dbo.Credit
                               INNER JOIN dbo.Installments ON dbo.Credit.id = dbo.Installments.contract_id
                               WHERE dbo.Installments.expected_date BETWEEN DATEADD(dd, -1, @date) AND DATEADD(dd, 1, @date)";
            using (SqlConnection conn = GetConnection())
            using (OpenCbsCommand c = new OpenCbsCommand(q, conn))
            {
                c.AddParam("@date", date);

                using (OpenCbsReader r = c.ExecuteReader())
                {
                    Dictionary<int, int> ListOfContractsInstallment = new Dictionary<int, int>();

                    while (r.Read())
                    {
                        ListOfContractsInstallment.Add(r.GetInt("id"), r.GetInt("number"));
                    }

                    return ListOfContractsInstallment;
                }
            }
        }

        private Loan _GetLoan(OpenCbsReader r)
        {
            return new Loan(_user, ApplicationSettings.GetInstance(_user.Md5),
                            NonWorkingDateSingleton.GetInstance(_user.Md5),
                            ProvisionTable.GetInstance(_user), ChartOfAccounts.GetInstance(_user))
                {
                    Id = r.GetInt("credit_id"),
                    ClientType = r.GetChar("client_type_code") == 'I'
                                     ? OClientTypes.Person
                                     : r.GetChar("client_type_code") == 'G'
                                           ? OClientTypes.Group
                                           : OClientTypes.Corporate,
                    ContractStatus = (OContractStatus) r.GetSmallInt("status"),
                    CreditCommiteeDate = r.GetNullDateTime("credit_commitee_date"),
                    CreditCommiteeComment = r.GetString("credit_commitee_comment"),
                    CreditCommitteeCode = r.GetString("credit_commitee_code"),
                    Amount = r.GetMoney("amount"),
                    InterestRate = r.GetDecimal("interest_rate"),
                    NbOfInstallments = r.GetInt("nb_of_installment"),
                    NonRepaymentPenalties = new NonRepaymentPenalties
                        {
                            InitialAmount = r.GetDouble("non_repayment_penalties_based_on_initial_amount"),
                            OLB = r.GetDouble("non_repayment_penalties_based_on_olb"),
                            OverDueInterest = r.GetDouble("non_repayment_penalties_based_on_overdue_interest"),
                            OverDuePrincipal = r.GetDouble("non_repayment_penalties_based_on_overdue_principal")
                        },

                    AnticipatedTotalRepaymentPenalties = r.GetDouble("anticipated_total_repayment_penalties"),
                    AnticipatedPartialRepaymentPenalties = r.GetDouble("anticipated_partial_repayment_penalties"),
                    AnticipatedPartialRepaymentPenaltiesBase = (OAnticipatedRepaymentPenaltiesBases)
                                                               r.GetSmallInt("anticipated_partial_repayment_base"),
                    AnticipatedTotalRepaymentPenaltiesBase = (OAnticipatedRepaymentPenaltiesBases)
                                                             r.GetSmallInt("anticipated_total_repayment_base"),

                    Disbursed = r.GetBool("disbursed"),
                    GracePeriod = r.GetNullInt("grace_period"),
                    GracePeriodOfLateFees = r.GetNullInt("grace_period_of_latefees"),
                    WrittenOff = r.GetBool("written_off"),
                    Rescheduled = r.GetBool("rescheduled"),

                    Code = r.GetString("contract_code"),
                    BranchCode = r.GetString("branch_code"),
                    CreationDate = r.GetDateTime("creation_date"),
                    StartDate = r.GetDateTime("start_date"),
                    AlignDisbursementDate = r.GetDateTime("align_disbursed_date"),
                    CloseDate = r.GetDateTime("close_date"),
                    Closed = r.GetBool("closed"),
                    BadLoan = r.GetBool("bad_loan"),
                    Synchronize = r.GetBool("synchronize"),
                    ScheduleChangedManually = r.GetBool("schedule_changed"),
                    AmountUnderLoc = r.GetMoney("amount_under_loc"),
                    CompulsorySavingsPercentage = r.GetNullInt("loan_percentage"),
                    LoanPurpose = r.GetString("loan_purpose"),
                    Comments = r.GetString("comments"),
                    AmountMin = r.GetMoney("amount_min"),
                    AmountMax = r.GetMoney("amount_max"),
                    InterestRateMin = r.GetNullDecimal("ir_min"),
                    InterestRateMax = r.GetNullDecimal("ir_max"),
                    NmbOfInstallmentsMin = r.GetNullInt("nmb_of_inst_min"),
                    NmbOfInstallmentsMax = r.GetNullInt("nmb_of_inst_max"),
                    LoanCycle = r.GetNullInt("loan_cycle"),
                    Insurance = r.GetDecimal("insurance"),
                    NsgID = r.GetNullInt("nsg_id"),
                    EconomicActivityId = r.GetInt("activity_id"),
                    FirstInstallmentDate = r.GetDateTime("preferred_first_installment_date"),
                };
        }

        private void _SetLoanShareAmount(Loan pLoan, SqlTransaction pSqlTransac)
        {
            // Get group id
            int group_id;
            const string q = @"SELECT p.tiers_id
                FROM dbo.Projects AS p
                LEFT JOIN dbo.Contracts AS c ON c.project_id = p.id
                WHERE c.id = @contract_id";
            using (OpenCbsCommand c = new OpenCbsCommand(q, pSqlTransac.Connection, pSqlTransac))
            {
                c.AddParam("@contract_id", pLoan.Id);
                group_id = Convert.ToInt32(c.ExecuteScalar());
            }

            const string sqlText = @"INSERT INTO LoanShareAmounts (person_id, group_id, contract_id, amount)
                                     VALUES (@person_id, @group_id, @contract_id, @amount)";

            using (OpenCbsCommand c = new OpenCbsCommand(sqlText, pSqlTransac.Connection, pSqlTransac))
            {
                foreach (LoanShare ls in pLoan.LoanShares)
                {
                    c.ResetParams();
                    c.AddParam("@person_id", ls.PersonId);
                    c.AddParam("@group_id", group_id);
                    c.AddParam("@contract_id", pLoan.Id);
                    c.AddParam("@amount", ls.Amount.Value);

                    c.ExecuteNonQuery();
                }
            }
        }

        private List<ContractCollateral> GetCollaterals(int pLoanId)
        {
            List<int> collateralIds = new List<int>();

            const string q = @"SELECT [id] FROM [CollateralsLinkContracts] WHERE contract_id = @contract_id ";
            using (SqlConnection conn = GetConnection())
            using (OpenCbsCommand c = new OpenCbsCommand(q, conn))
            {
                c.AddParam("@contract_id", pLoanId);
                using (OpenCbsReader r = c.ExecuteReader())
                {
                    if (r == null || r.Empty) return new List<ContractCollateral>();
                    while (r.Read()) collateralIds.Add(r.GetInt("id"));
                }
            }

            List<ContractCollateral> contractCollaterals = new List<ContractCollateral>();

            foreach (int collateralId in collateralIds)
            {
                ContractCollateral contractCollateral = new ContractCollateral();
                List<CollateralPropertyValue> propertyValues = new List<CollateralPropertyValue>();

                string sqlPropertyText = @"SELECT [contract_collateral_id], [property_id], [value]
                                           FROM [CollateralPropertyValues] 
                                           WHERE contract_collateral_id = @contract_collateral_id ";
                using (SqlConnection conn = GetConnection())
                using (OpenCbsCommand c = new OpenCbsCommand(sqlPropertyText, conn))
                {
                    c.AddParam("@contract_collateral_id", collateralId);
                    using (OpenCbsReader r = c.ExecuteReader())
                    {
                        if (r == null || r.Empty) return new List<ContractCollateral>();

                        while (r.Read())
                        {
                            CollateralPropertyValue propertyValue = new CollateralPropertyValue
                                {
                                    Id = collateralId,
                                    Property = new CollateralProperty {Id = r.GetInt("property_id")},
                                    //Property. = _collateralProductManager.SelectCollateralProperty(r.GetInt("property_id")),
                                    Value = r.GetString("value")
                                };
                            propertyValues.Add(propertyValue);
                        }
                    }
                }

                foreach (CollateralPropertyValue propertyValue in propertyValues)
                {
                    propertyValue.Property =
                        _collateralProductManager.SelectCollateralProperty(propertyValue.Property.Id);
                }

                contractCollateral.PropertyValues = propertyValues;
                contractCollaterals.Add(contractCollateral);
            }

            return contractCollaterals;
        }

        public List<LoanShare> GetLoanShareAmount(int pContractId)
        {
            const string q = @"SELECT lsa.group_id, 
                                       lsa.person_id, 
                                       lsa.amount, 
                                       p.first_name + ' ' + p.last_name AS person_name
                                     FROM LoanShareAmounts AS lsa
                                     LEFT JOIN Persons AS p ON p.id = lsa.person_id 
                                     WHERE contract_id = @contract_id";
            using (SqlConnection conn = GetConnection())
            using (OpenCbsCommand c = new OpenCbsCommand(q, conn))
            {
                c.AddParam("contract_id", pContractId);
                using (OpenCbsReader r = c.ExecuteReader())
                {
                    if (r == null || r.Empty) return new List<LoanShare>();

                    List<LoanShare> loanShares = new List<LoanShare>();
                    while (r.Read())
                    {
                        loanShares.Add(new LoanShare
                            {
                                PersonId = r.GetInt("person_id"),
                                PersonName = r.GetString("person_name"),
                                Amount = r.GetMoney("amount")
                            });
                    }
                    return loanShares;
                }
            }
        }


        private static TrancheEvent GetTransh(OpenCbsReader r)
        {
            return new TrancheEvent
                {
                    Number = r.GetInt("Number"),
                    StartDate = r.GetDateTime("start_date"),
                    Amount = r.GetMoney("amount"),
                    Maturity = r.GetInt("countOfInstallments"),
                    ApplyNewInterest = r.GetBool("ApplyNewInterest"),
                    InterestRate = r.GetDecimal("interest_rate"),
                    StartedFromInstallment = r.GetInt("started_from_installment"),
                    Deleted = r.GetBool("is_deleted"),
                    Id = r.GetInt("event_id"),
                    GracePeriod = r.GetInt("grace_period"),
                    FirstRepaymentDate = r.GetDateTime("first_repayment_date"),
                };
        }

        public List<TrancheEvent> SelectTranches(int pLoanId)
        {
            using (SqlConnection conn = GetConnection())
            using (SqlTransaction t = conn.BeginTransaction())
            {
                List<TrancheEvent> list = SelectTranches(pLoanId, t);
                t.Commit();
                return list;
            }
        }

        public List<TrancheEvent> SelectTranches(int pLoanId, SqlTransaction pSqlTransac)
        {
            const string q = @" SELECT  con.id AS contract_id,
                                        con.start_date,
                                        amount,
                                        COUNT(ISNULL(h.id, nb_of_installment)) AS countOfInstallments,
                                        interest_rate,
                                        CONVERT(BIT, 0) AS ApplyNewInterest,
                                        0 AS started_from_installment,
                                        0 AS Number,
                                        cred.grace_period,
                                        con.preferred_first_installment_date first_repayment_date,
                                        ce.is_deleted,
                                        0 AS event_id
                                FROM    Credit cred
                                        INNER JOIN Contracts con ON cred.id = con.id
                                        INNER JOIN ContractEvents AS ce ON con.id = ce.contract_id
                                        LEFT JOIN dbo.InstallmentHistory h ON con.id = h.contract_id
                                          AND ce.id = h.event_id
                                WHERE   con.id = @id
                                        AND ce.is_deleted = 0
                                        AND ce.event_date <= (SELECT MIN(event_date)
                                                             FROM   dbo.ContractEvents
                                                             WHERE  contract_id = @id)
                                GROUP BY con.id,
                                        con.start_date,
                                        cred.grace_period,
                                        con.preferred_first_installment_date,
                                        amount,
                                        interest_rate,
                                        ce.is_deleted
                                UNION ALL
                                SELECT  contract_id,
                                        start_date,
                                        amount,
                                        maturity AS countOfInstallments,
                                        interest_rate,
                                        CONVERT(BIT, applied_new_interest) AS ApplyNewInterest,
                                        ISNULL(started_from_installment, 0),
                                        CONVERT(INT, ROW_NUMBER() OVER (ORDER BY start_date DESC)) + 1 AS Number,
                                        grace_period,
                                        first_repayment_date,
                                        ce.is_deleted,
                                        ce.id AS event_id
                                FROM    TrancheEvents te
                                        INNER JOIN ContractEvents ce ON te.id = ce.id
                                WHERE   contract_id = @id
                                  AND ce.is_deleted = 0";

            using (OpenCbsCommand c = new OpenCbsCommand(q, pSqlTransac.Connection, pSqlTransac))
            {
                c.AddParam("@id", pLoanId);
                using (OpenCbsReader r = c.ExecuteReader())
                {
                    List<TrancheEvent> trancheList = new List<TrancheEvent>();

                    if (r == null || r.Empty)
                        return trancheList;

                    while (r.Read())
                    {
                        trancheList.Add(GetTransh(r));
                    }
                    return trancheList;
                }
            }
        }

        public List<Alert_v2> SelectAllAlerts(DateTime date, int userId)
        {
            List<Alert_v2> alerts = new List<Alert_v2>();
            string q = "SELECT * FROM dbo.Alerts(@date, @user_id, @branch_id)";

            using (SqlConnection conn = GetConnection())
            using (OpenCbsCommand c = new OpenCbsCommand(q, conn))
            {
                c.CommandTimeout = 600;
                c.AddParam("@date", date.Date);
                c.AddParam("@user_id", userId);
                c.AddParam("@branch_id", 1);

                using (OpenCbsReader r = c.ExecuteReader())
                {
                    if (r.Empty) return alerts;

                    while (r.Read())
                    {
                        Alert_v2 alert = new Alert_v2
                            {
                                Address = r.GetString("address")
                                ,
                                Amount = r.GetMoney("amount")
                                ,
                                City = r.GetString("city")
                                ,
                                ClientName = r.GetString("client_name")
                                ,
                                ContractCode = r.GetString("contract_code")
                                ,
                                Date = r.GetDateTime("date")
                                ,
                                Id = r.GetInt("id")
                                ,
                                LateDays = r.GetInt("late_days")
                                ,
                                LoanOfficer = new User {Id = r.GetInt("loan_officer_id")}
                                ,
                                Phone = r.GetString("phone")
                                ,
                                Status = (OContractStatus) r.GetInt("status")
                                ,
                                UseCents = r.GetBool("use_cents")
                                ,
                                Kind = (AlertKind) r.GetInt("kind")
                                ,
                                BranchName = r.GetString("branch_name")
                            };
                        alerts.Add(alert);
                    }
                }

                return alerts;
            }
        }

        public List<int> FindAllLoanIds()
        {
            const string q = @"select id
            from dbo.Contracts";
            List<int> list = new List<int>();
            using (SqlConnection conn = GetConnection())
            using (OpenCbsCommand c = new OpenCbsCommand(q, conn))
            {
                using (OpenCbsReader r = c.ExecuteReader())
                {
                    while (r.Read())
                    {
                        list.Add(r.GetInt("id"));
                    }
                }
                return list;
            }
        }

        public string FindBranchCodeByPersonId(int clientId)
        {
            const string q = @"SELECT Branches.code FROM Tiers
                               INNER JOIN Branches ON Branches.id = Tiers.branch_id
                               WHERE Tiers.id = @id";

            string code = null;
            using (SqlConnection conn = GetConnection())
            using (OpenCbsCommand c = new OpenCbsCommand(q, conn))
            {
                c.AddParam("@id", clientId);
                using (OpenCbsReader r = c.ExecuteReader())
                {
                    if (r == null || r.Empty) return null;
                    while (r.Read())
                    {
                        code = r.GetString("id");
                    }
                }
                return code;
            }
        }

        public bool IsLoanDisbursed(int contractId)
        {
            const string query =
                "SELECT [disbursed] FROM Credit WHERE id = @contractId";
            using (var connection = GetConnection())
            using (var cmd = new OpenCbsCommand(query, connection))
            {
                cmd.AddParam("contractId", contractId);
                var result = cmd.ExecuteScalar();
                return result != null && Convert.ToBoolean(result);
            }
        }

        public Dictionary<int, DateTime> GetListOfLoansToAccruePenalty(DateTime launchDate)
        {
            const string q = @"SELECT al.id AS id,
                            CASE WHEN al.late_days=0
								THEN 1
								ELSE al.late_days
								END AS late_days,
                            CASE 
	                            WHEN lp.event_date IS NULL
	                            THEN CASE WHEN al.late_days=0
									THEN 1
									ELSE al.late_days
									END
	                            ELSE DATEDIFF(DD, lp.event_date, @date)
	                            END AS [not_accrued_days]
                            FROM dbo.ActiveLoans(@date, 0) AS al
                            LEFT JOIN dbo.Contracts c ON c.id=al.id
                            LEFT JOIN (
	                            SELECT contract_id
	                            , MAX(ce.event_date) AS event_date
	                            FROM dbo.LoanPenaltyAccrualEvents lp
	                            LEFT JOIN dbo.ContractEvents ce ON ce.id=lp.id
	                            WHERE is_deleted=0
	                            GROUP BY contract_id
	                            ) lp ON lp.contract_id=al.id
                            WHERE (al.late_days > 0 OR (al.late_days=0 AND 
                                    (al.principal_due!=0 or al.interest_due!=0))) AND c.[status]!=10";
            using (var connection = GetConnection())
            using (var c = new OpenCbsCommand(q, connection))
            {
                c.AddParam("date", launchDate);
                using (var r = c.ExecuteReader())
                {
                    var dict = new Dictionary<int, DateTime>();
                    while (r.Read())
                    {
                        var id = r.GetInt("id");
                        var lateDays = r.GetInt("late_days");
                        var notAccruedDays = r.GetInt("not_accrued_days");
                        dict.Add(id, launchDate.AddDays(-Math.Min(lateDays, notAccruedDays)));
                    }
                    return dict;
                }
            }
        }

//        public Dictionary<int, decimal> GetListOfLoansToAccruePenalty1(DateTime launchDate)
//        {
//            const string q = @"SELECT al.id, al.amount * cr.non_repayment_penalties_based_on_initial_amount +
//                            al.olb * cr.non_repayment_penalties_based_on_olb +
//                            al.principal_due * cr.non_repayment_penalties_based_on_overdue_principal +
//                            al.interest_due * cr.non_repayment_penalties_based_on_overdue_interest AS penalty
//                            FROM dbo.ActiveLoans(@date, 0) AS al
//                            LEFT JOIN dbo.Credit AS cr ON cr.id=al.id
//                            WHERE al.late_days > 0";
//            using (var connection = GetConnection())
//            using (var c = new OpenCbsCommand(q, connection))
//            {
//                c.AddParam("date", launchDate);
//                using (var r = c.ExecuteReader())
//                {
//                    var dict = new Dictionary<int, decimal>();
//                    while (r.Read())
//                        dict.Add(r.GetInt("id"), Convert.ToDecimal(r.GetDouble("penalty")));
//                    return dict;
//                }
//            }
//        }

        public decimal GetDailyPenaltyForLoan(int contractId, DateTime date)
        {
            const string q = @"SELECT al.amount * cr.non_repayment_penalties_based_on_initial_amount +
                            al.olb * cr.non_repayment_penalties_based_on_olb +
                            al.principal_due * cr.non_repayment_penalties_based_on_overdue_principal +
                            al.interest_due * cr.non_repayment_penalties_based_on_overdue_interest AS penalty
                            FROM dbo.ActiveLoans(@date, 0) AS al
                            LEFT JOIN dbo.Credit AS cr ON cr.id=al.id
                            where al.id = @contractId";
            using (var connection = GetConnection())
            using (var c = new OpenCbsCommand(q, connection))
            {
                c.AddParam("date", date);
                c.AddParam("contractId", contractId);
                var result = c.ExecuteScalar();
                return result != null ? Convert.ToDecimal(result) : 0;
            }
        }

        public Dictionary<int, DateTime> GetListOfLoansToAccrueInterest(DateTime launchDate)
        {
            const string q = @"SELECT al.id,
                            CASE
								WHEN li.event_date IS NULL
								THEN DATEADD(DD,-1,c.[start_date])
								ELSE li.event_date
							END AS event_date
                            FROM dbo.ActiveLoans(@date, 0) AS al
                            LEFT JOIN dbo.Contracts AS c ON c.id=al.id
                            LEFT JOIN (
                                SELECT contract_id
                                , MAX(ce.event_date) AS event_date
                                FROM dbo.AccrualInterestLoanEvents li
                                LEFT JOIN dbo.ContractEvents ce ON ce.id=li.id
                                WHERE is_deleted=0
                                GROUP BY contract_id
                                ) li ON li.contract_id=al.id
                            WHERE c.[status]!=10";
            using (var connection = GetConnection())
            using (var c = new OpenCbsCommand(q, connection))
            {
                c.AddParam("date", launchDate);
                using (var r = c.ExecuteReader())
                {
                    var dict = new Dictionary<int, DateTime>();
                    while (r.Read())
                    {
                        var id = r.GetInt("id");
                        var date = r.GetDateTime("event_date");
                        dict.Add(id, date);
                    }
                    return dict;
                }
            }
        }

        public decimal GetInstallmentInterest(int contractId, int installmentNumber, DateTime date)
        {
            const string q = @"SELECT interest FROM dbo.InstallmentSnapshot(@date) 
                            WHERE contract_id=@contractId AND number=@installmentId";
            using (var connection = GetConnection())
            using (var c = new OpenCbsCommand(q, connection))
            {
                c.AddParam("date", date);
                c.AddParam("contractId", contractId);
                c.AddParam("installmentId", installmentNumber);
                var result = c.ExecuteScalar();
                return result != null ? Convert.ToDecimal(result) : 0;
            }
        }

        public decimal GetSumOfAccruedInterests(int contractId, DateTime from, DateTime to)
        {
            const string q = @"SELECT CASE
	                            WHEN SUM(ai.interest) IS NULL
	                            THEN 0
	                            ELSE SUM(ai.interest)
	                            END AS [sum]
                                FROM dbo.AccrualInterestLoanEvents AS ai
                                LEFT JOIN dbo.ContractEvents AS ce ON ce.id=ai.id
                                WHERE ce.contract_id=@contractId 
                                and ce.event_date<@to 
                                and ce.event_date>=@from";
            using (var connection = GetConnection())
            using (var c = new OpenCbsCommand(q, connection))
            {
                c.AddParam("@to", to);
                c.AddParam("@from", from);
                c.AddParam("@contractId", contractId);
                using (var r = c.ExecuteReader())
                {
                    r.Read();
                    return r.GetDecimal("sum");
                }
            }
        }

        public Dictionary<int, decimal> GetListOfTransitionToLateLoan(DateTime launchDate)
        {
            const string q = @"SELECT al.id, ins.capital_repayment-ins.paid_capital installment_principal_due
                            FROM dbo.ActiveLoans(@date, 0) al
                            LEFT JOIN dbo.Installments ins ON ins.contract_id=al.id
                            WHERE ins.expected_date=CAST(DATEADD(d,-1,@date) AS DATE)
                            AND ins.capital_repayment>ins.paid_capital";
            using (var connection = GetConnection())
            using (var c = new OpenCbsCommand(q, connection))
            {
                c.AddParam("date", launchDate);
                using (var r = c.ExecuteReader())
                {
                    var dict = new Dictionary<int, decimal>();
                    while (r.Read())
                        dict.Add(r.GetInt("id"), r.GetDecimal("installment_principal_due"));
                    return dict;
                }
            }
        }

//        public Dictionary<int, decimal> GetListOfTransitionToGoodLoan(DateTime launchDate)
//        {
//            const string q = @"SELECT a1.id,a1.olb,a1.late_days
//                            FROM dbo.ActiveLoans(@date, 0) a1
//                            INNER JOIN dbo.ActiveLoans(DATEADD(dd,-1,@date), 0) a2 ON a2.id = a1.id
//                            WHERE a1.late_days = 0 and a2.late_days>0";
//            using (var connection = GetConnection())
//            using (var c = new OpenCbsCommand(q, connection))
//            {
//                c.AddParam("date", launchDate);
//                using (var r = c.ExecuteReader())
//                {
//                    var dict = new Dictionary<int, decimal>();
//                    while (r.Read())
//                        dict.Add(r.GetInt("id"), r.GetDecimal("olb"));
//                    return dict;
//                }
//            }
//        }

        public DateTime LastLoanTransitionEventDate()
        {
            const string q = @"SELECT TOP 1 ce.event_date 
                            FROM dbo.LoanTransitionEvents AS tr
                            LEFT JOIN dbo.ContractEvents AS ce ON ce.id=tr.id
                            ORDER BY ce.id DESC";
            using (var connection = GetConnection())
            using (var r = new OpenCbsCommand(q, connection).ExecuteReader())
            {
                return !r.Read() ? DateTime.Today.AddDays(-1) : r.GetDateTime("event_date");
            }
        }

        public List<int> GetLoanIdForRepayment(DateTime date)
        {
            var listLoanId = new List<int>();

            const string sqlText = @"SELECT al.id
                                    FROM Activeloans(@date, 0) AS al
                                    LEFT JOIN InstallmentSnapshot(@date) AS inst ON inst.contract_id=al.id
                                    WHERE inst.expected_date=CONVERT(date,@date,103)";
            using (var conn = GetConnection())
            using (var select = new OpenCbsCommand(sqlText, conn))
            {
                select.AddParam("@date", date);
                using (var reader = select.ExecuteReader())
                {
                    if (!reader.Empty)
                        while (reader.Read())
                            listLoanId.Add(reader.GetInt("id"));
                }
            }
            return listLoanId;
        }

        public string GetRepaymentModuleLastStartupDate()
        {
            const string sqlText = @"SELECT value 
                                    FROM dbo.TechnicalParameters
                                    WHERE name='repayment_module_last_startup_date'";
            using (var conn = GetConnection())
            using (var select = new OpenCbsCommand(sqlText, conn))
            {
                var re = select.ExecuteScalar();
                return re == null ? String.Empty : re.ToString();
            }
        }

        public void SetRepaymentModuleLastStartupDate(DateTime date)
        {
            const string sqlText = @"UPDATE dbo.TechnicalParameters
                                    SET value=CONVERT(NVARCHAR(10), @date, 103)
                                    WHERE name='repayment_module_last_startup_date'";
            using (var conn = GetConnection())
            using (var select = new OpenCbsCommand(sqlText, conn))
            {
                select.AddParam("@date", date);
                select.ExecuteScalar();
            }
        }

        public void CreateRepaymentModuleLastStartupDateRecord()
        {
            const string sqlText = @"INSERT INTO dbo.TechnicalParameters 
                                    VALUES ('repayment_module_last_startup_date', CONVERT(NVARCHAR(10), GETDATE(), 103))";
            using (var conn = GetConnection())
            using (var select = new OpenCbsCommand(sqlText, conn))
            {
                select.ExecuteScalar();
            }
        }
    }
}
