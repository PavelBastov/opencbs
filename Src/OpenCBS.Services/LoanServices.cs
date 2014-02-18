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
using System.ComponentModel.Composition;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using AutoMapper;
using OpenCBS.CoreDomain;
using OpenCBS.CoreDomain.Accounting;
using OpenCBS.CoreDomain.Alerts;
using OpenCBS.CoreDomain.Clients;
using OpenCBS.CoreDomain.Contracts;
using OpenCBS.CoreDomain.Contracts.Loans;
using OpenCBS.CoreDomain.Contracts.Loans.LoanRepayment;
using OpenCBS.CoreDomain.Contracts.Savings;
using OpenCBS.CoreDomain.EconomicActivities;
using OpenCBS.CoreDomain.Events;
using OpenCBS.CoreDomain.Events.Loan;
using OpenCBS.CoreDomain.Events.Saving;
using OpenCBS.CoreDomain.FundingLines;
using OpenCBS.CoreDomain.SearchResult;
using OpenCBS.Engine;
using OpenCBS.Engine.Interfaces;
using OpenCBS.Engine.PeriodPolicy;
using OpenCBS.Engine.YearPolicy;
using OpenCBS.Enums;
using OpenCBS.ExceptionsHandler;
using OpenCBS.ExceptionsHandler.Exceptions.SavingExceptions;
using OpenCBS.Extensions;
using OpenCBS.Manager;
using OpenCBS.Manager.Clients;
using OpenCBS.Manager.Contracts;
using OpenCBS.Manager.Events;
using OpenCBS.Services.Accounting;
using OpenCBS.Services.Events;
using OpenCBS.Shared;
using OpenCBS.Shared.Settings;
using Installment = OpenCBS.CoreDomain.Contracts.Loans.Installments.Installment;

namespace OpenCBS.Services
{
    public class LoanServices : Services
    {
        private readonly FundingLineServices _fundingLineServices;
        private readonly AccountingServices _accountingServices;
        private readonly BranchService _branchService;
        private readonly LoanManager _loanManager;
        private readonly InstallmentManager _instalmentManager;
        private readonly ClientManager _clientManager;
        private readonly SavingEventManager _savingEventManager;
        private readonly EconomicActivityServices _econimcActivityServices;
        private readonly EventProcessorServices _ePs;
        private readonly SavingServices _savingServices;
        private ProductServices _productServices;
        private readonly User _user;
        private static List<Alert_v2> _alerts;

        private readonly OctopusScheduleConfigurationFactory _configurationFactory;

        [ImportMany(typeof (IContractCodeGenerator))]
        private Lazy<IContractCodeGenerator, IDictionary<string, object>>[] ContractCodeGenerators { get; set; }

        [ImportMany(typeof(IEventInterceptor))]
        private Lazy<IEventInterceptor, IDictionary<string, object>>[] EventInterceptors { get; set; }

        public LoanServices(User pUser)
            : base(pUser)
        {
            _user = pUser;
            _loanManager = new LoanManager(pUser);
            _instalmentManager = new InstallmentManager(pUser);
            _clientManager = new ClientManager(pUser, true, true);
            _branchService = new BranchService(pUser);
            _econimcActivityServices = new EconomicActivityServices(pUser);
            _ePs = ServicesProvider.GetInstance().GetEventProcessorServices();
            _accountingServices = new AccountingServices(pUser);
            _fundingLineServices = new FundingLineServices(pUser);
            _savingServices = new SavingServices(pUser);
            _savingEventManager = new SavingEventManager(pUser);

            var settings = ApplicationSettings.GetInstance(string.Empty);
            var nonWorkingDate = NonWorkingDateSingleton.GetInstance(string.Empty);
            _configurationFactory = new OctopusScheduleConfigurationFactory(nonWorkingDate, settings);

            MefContainer.Current.Bind(this);
        }

        public LoanServices(InstallmentManager pInstalmentManager, ClientManager pClientManager,
                            LoanManager pLoanManager)
        {
            _user = new User();
            _instalmentManager = pInstalmentManager;
            _clientManager = pClientManager;
            _loanManager = pLoanManager;

            MefContainer.Current.Bind(this);
        }

        /// <summary>
        /// Get from database ALL instalments
        /// </summary>
        /// <returns>a list of pair (loanId,instalment)</returns>
        public List<KeyValuePair<int, Installment>> FindAllInstalments()
        {
            return _instalmentManager.SelectInstalments();
        }

        /// <summary>
        /// Update all instalments' expected date with the nearest valid date 
        /// </summary>
        /// <param name="pInstallmentList">list of isntalments</param>
        /// <returns>Number of modified instalments</returns>
        public int UpdateAllInstallmentsDate(List<KeyValuePair<int, Installment>> pInstallmentList)
        {
            ApplicationSettings appSettings = ApplicationSettings.GetInstance(_user.Md5);
            NonWorkingDateSingleton nonWorkingDate = NonWorkingDateSingleton.GetInstance(_user.Md5);

            if (appSettings.DoNotSkipNonWorkingDays) return 0;

            int nbOfModifiedInstalments = 0;
            foreach (KeyValuePair<int, Installment> pair in pInstallmentList)
            {
                DateTime date = nonWorkingDate.GetTheNearestValidDate(pair.Value.ExpectedDate,
                                                                      appSettings.IsIncrementalDuringDayOff,
                                                                      appSettings.DoNotSkipNonWorkingDays, true);

                if (pair.Value.ExpectedDate == date) continue;

                nbOfModifiedInstalments++;
                pair.Value.ExpectedDate = date;
                _instalmentManager.UpdateInstallment(pair.Value, pair.Key, null, true);
            }
            return nbOfModifiedInstalments;
        }

        public int UpdateAllInstallmentsDate(DateTime date, Dictionary<int, int> list)
        {
            int nbOfModifiedInstalments = 0;
            Loan contract;
            foreach (KeyValuePair<int, int> pair in list)
            {
                contract = _loanManager.SelectLoan(pair.Key, true, false, false);
                if (contract.CalculateInstallmentDate(contract.AlignDisbursementDate, pair.Value) == date)
                {
                    _instalmentManager.UpdateInstallment(date, pair.Key, pair.Value);
                }
                nbOfModifiedInstalments++;
            }

            return nbOfModifiedInstalments;
        }

        public Dictionary<int, int> GetListOfInstallmentsOnDate(DateTime date)
        {
            return _loanManager.GetListOfInstallmentsOnDate(date);
        }

        public void UpdateInstallmentComment(string pComment, int pContractId, int pNumber)
        {
            _instalmentManager.UpdateInstallmentComment(pComment, pContractId, pNumber);
        }

        /// <summary>
        /// Check loan filling before calculate Instalments
        /// </summary>
        /// <param name="pLoan"></param>
        public void CheckLoanFilling(Loan pLoan)
        {
            _CheckLoanFilling(pLoan, null);
        }

        public void RemoveCompulsorySavings(int loanId)
        {
            _loanManager.RemoveCompulsorySavings(loanId, null);
        }

        public void SaveLoan(ref Loan loan, int projectId, ref IClient client)
        {
            SaveLoan(ref loan, projectId, ref client, null);
        }

        public void SaveLoan(ref Loan pLoan, int pProjectId, ref IClient pClient, Action<SqlTransaction, int> action)
        {
            _CheckLoanFilling(pLoan, pClient);

            using (SqlConnection connection = _loanManager.GetConnection())
            using (SqlTransaction transaction = connection.BeginTransaction())
            {
                try
                {
                    if (pLoan.Id == 0)
                    {
                        List<LoanEntryFee> loanEntryFees = pLoan.LoanEntryFeesList;
                        AddLoan(ref pLoan, pProjectId, ref pClient, transaction);
                        _loanManager.InsertLoanEntryFees(loanEntryFees, pLoan.Id, transaction);
                    }
                    else
                    {
                        List<LoanEntryFee> loanEntryFees = pLoan.LoanEntryFeesList;
                        UpdateLoan(ref pLoan, transaction);
                        _loanManager.UpdateLoanEntryFees(loanEntryFees, pLoan.Id, transaction);
                    }

                    ClientServices clientServices = ServicesProvider.GetInstance().GetClientServices();
                    switch (pClient.Type)
                    {
                        case OClientTypes.Group:
                            clientServices.SetFavouriteLoanOfficerForGroup(
                                (Group) pClient, pLoan.LoanOfficer.Id, transaction);
                            break;
                        case OClientTypes.Corporate:
                            clientServices.SetFavouriteLoanOfficerForCorporate(
                                (Corporate) pClient, pLoan.LoanOfficer.Id);
                            break;
                        case OClientTypes.Person:
                            clientServices.SetFavouriteLoanOfficerForPerson(
                                (Person) pClient, pLoan.LoanOfficer.Id, transaction);
                            break;
                    }
                    if (action != null) action(transaction, pLoan.Id);
                    transaction.Commit();
                }
                catch (Exception)
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        public void UpdateVillageStatus(Village pVillage)
        {
            using (SqlConnection conn = _clientManager.GetConnection())
            using (SqlTransaction sqlTransaction = conn.BeginTransaction())
            {
                try
                {
                    _clientManager.UpdateTiers(pVillage, sqlTransaction);
                    if (sqlTransaction != null) sqlTransaction.Commit();
                }
                catch (Exception)
                {
                    if (sqlTransaction != null) sqlTransaction.Rollback();
                    throw;
                }
            }
        }

        /// <summary>
        /// Simulate a disbursement
        /// </summary>
        /// <param name="pLoan"></param>
        /// <param name="pAlignInstallmentsDatesOnRealDisbursmentDate"></param>
        /// <param name="pDateToDisburse"></param>
        /// <param name="pDisableFees"></param>
        /// <returns>a Loan disbursement event</returns>
        public LoanDisbursmentEvent DisburseSimulation(Loan pLoan, bool pAlignInstallmentsDatesOnRealDisbursmentDate,
                                                       DateTime pDateToDisburse, bool pDisableFees)
        {
            Loan clonedLoan = pLoan.Copy();
            LoanDisbursmentEvent loanDisbursmentEvent = clonedLoan.Disburse(pDateToDisburse,
                                                                            pAlignInstallmentsDatesOnRealDisbursmentDate,
                                                                            pDisableFees);

            if (loanDisbursmentEvent == null)
                throw new OpenCbsContractSaveException(OpenCbsContractSaveExceptionEnum.DisburseIsNull);

            loanDisbursmentEvent.User = _user;
            return loanDisbursmentEvent;
        }

        public bool CanUserModifyEntryFees()
        {
            //This method works only if user has permission
            return true;
        }

        private void SetEconomicActivity(Loan pLoan, SqlTransaction sqlTransaction)
        {
            // Write EconomicActivityLoanHistory object
            EconomicActivityLoanHistory activityLoanHistory;

            if (pLoan.Project.Client is Person)
            {
                if (
                    !_econimcActivityServices.EconomicActivityLoanHistoryExists(pLoan.Id, pLoan.Project.Client.Id,
                                                                                sqlTransaction))
                {
                    activityLoanHistory = new EconomicActivityLoanHistory
                        {
                            Contract = pLoan,
                            Deleted = false,
                            Group = null,
                            Person = pLoan.Project.Client,
                            EconomicActivity = ((Person) pLoan.Project.Client).Activity
                        };
                    _econimcActivityServices.AddEconomicActivityLoanHistory(activityLoanHistory, sqlTransaction);
                }
                else
                {
                    if (_econimcActivityServices.EconomicActivityLoanHistoryDeleted(pLoan.Id,
                                                                                    pLoan.Project.Client.Id,
                                                                                    sqlTransaction))
                        _econimcActivityServices.UpdateDeletedEconomicActivityLoanHistory(pLoan.Id,
                                                                                          pLoan.Project.Client.Id,
                                                                                          ((Person)
                                                                                           pLoan.Project.Client).
                                                                                              Activity.Id,
                                                                                          sqlTransaction, false);
                }
            }
            else if (pLoan.Project.Client is Group)
            {
                foreach (Member member in ((Group) pLoan.Project.Client).Members)
                {
                    if (
                        !_econimcActivityServices.EconomicActivityLoanHistoryExists(pLoan.Id, member.Tiers.Id,
                                                                                    sqlTransaction))
                    {
                        activityLoanHistory = new EconomicActivityLoanHistory
                            {
                                Contract = pLoan,
                                Deleted = false,
                                Group = (Group) pLoan.Project.Client,
                                Person = member.Tiers,
                                EconomicActivity = ((Person) member.Tiers).Activity
                            };
                        _econimcActivityServices.AddEconomicActivityLoanHistory(activityLoanHistory, sqlTransaction);
                    }
                    else
                    {
                        if (_econimcActivityServices.EconomicActivityLoanHistoryDeleted(pLoan.Id, member.Tiers.Id,
                                                                                        sqlTransaction))
                            _econimcActivityServices.UpdateDeletedEconomicActivityLoanHistory(pLoan.Id,
                                                                                              member.Tiers.Id,
                                                                                              ((Person) member.Tiers)
                                                                                                  .Activity.Id,
                                                                                              sqlTransaction, false);
                    }
                }
            }
            else if (pLoan.Project.Client is Corporate)
            {
                if (((Corporate) pLoan.Project.Client).Activity == null)
                    throw new OpenCbsTiersSaveException(OpenCbsTiersSaveExceptionEnum.EconomicActivityIsNull);

                if (
                    !_econimcActivityServices.EconomicActivityLoanHistoryExists(pLoan.Id,
                                                                                pLoan.Project.Client.Id,
                                                                                sqlTransaction))
                {
                    activityLoanHistory = new EconomicActivityLoanHistory
                        {
                            Contract = pLoan,
                            Deleted = false,
                            Group = null,
                            Person = pLoan.Project.Client,
                            EconomicActivity = ((Corporate) pLoan.Project.Client).Activity
                        };
                    _econimcActivityServices.AddEconomicActivityLoanHistory(activityLoanHistory, sqlTransaction);
                }
                else
                {
                    if (_econimcActivityServices.EconomicActivityLoanHistoryDeleted(pLoan.Id,
                                                                                    pLoan.Project.Client.Id,
                                                                                    sqlTransaction))
                        _econimcActivityServices.UpdateDeletedEconomicActivityLoanHistory(pLoan.Id,
                                                                                          pLoan.Project.Client.
                                                                                                Id,
                                                                                          ((Corporate)
                                                                                           pLoan.Project.Client)
                                                                                              .Activity.Id,
                                                                                          sqlTransaction, false);
                }
            }
        }

        public Loan Disburse(Loan pLoan, DateTime pDateToDisburse, bool pAlignInstallmentsDatesOnRealDisbursmentDate,
                             bool pDisableFees, PaymentMethod method)
        {
            Loan copyLoan = pLoan.Copy();
            CheckDisbursedLoan(copyLoan, pDateToDisburse);
            CheckOperationDate(pDateToDisburse);

            using (SqlConnection connection = _loanManager.GetConnection())
            using (SqlTransaction sqlTransaction = connection.BeginTransaction())
            {
                try
                {
                    LoanDisbursmentEvent loanDisbursmentEvent = copyLoan.Disburse(pDateToDisburse,
                                                                                  pAlignInstallmentsDatesOnRealDisbursmentDate,
                                                                                  pDisableFees);
                    loanDisbursmentEvent.User = _user;
                    loanDisbursmentEvent.PaymentMethod = method;

                    if (pLoan.CompulsorySavings != null)
                        loanDisbursmentEvent.Comment = pLoan.CompulsorySavings.Code;

                    FundingLineEvent flFundingLineEvent = new FundingLineEvent
                        {
                            Code = String.Concat("DE_", copyLoan.Code),
                            Type = OFundingLineEventTypes.Disbursment,
                            Amount = loanDisbursmentEvent.Amount,
                            Movement = OBookingDirections.Debit,
                            CreationDate = TimeProvider.Now,
                            FundingLine =
                                _fundingLineServices.SelectFundingLineById(
                                    copyLoan.FundingLine.Id, sqlTransaction),
                            AttachTo = loanDisbursmentEvent
                        };

                    if (Teller.CurrentTeller != null && Teller.CurrentTeller.Id != 0)
                    {
                        loanDisbursmentEvent.TellerId = Teller.CurrentTeller.Id;
                        flFundingLineEvent.TellerId = Teller.CurrentTeller.Id;
                    }
                    loanDisbursmentEvent.Comment = copyLoan.Comments;
                    _ePs.FireEvent(loanDisbursmentEvent, copyLoan, sqlTransaction);

                    flFundingLineEvent = _fundingLineServices.AddFundingLineEvent(flFundingLineEvent, sqlTransaction);

                    foreach (Event item in copyLoan.Events)
                    {
                        if (item is CreditInsuranceEvent)
                        {
                            _ePs.FireEvent(item, copyLoan, sqlTransaction);
                            break;
                        }
                    }

                    copyLoan.ContractStatus = OContractStatus.Active;

                    if (copyLoan.Project != null)
                        copyLoan.Project.Client.Active = true;

                    _loanManager.UpdateLoan(copyLoan, sqlTransaction);

                    int loanDisbEventId = loanDisbursmentEvent.Id;
                    if (pAlignInstallmentsDatesOnRealDisbursmentDate ||
                        ApplicationSettings.GetInstance(_user != null ? _user.Md5 : string.Empty).
                                            PayFirstInterestRealValue)
                        foreach (Installment installment in copyLoan.InstallmentList)
                            _instalmentManager.UpdateInstallment(installment, copyLoan.Id, loanDisbEventId,
                                                                 sqlTransaction);

                    copyLoan.FundingLine.AddEvent(flFundingLineEvent);

                    loanDisbursmentEvent.Cancelable = true;

                    if (copyLoan.HasCompulsoryAmount())
                    {
                        SavingBookContract compulsorySaving = copyLoan.CompulsorySavings;
                        SavingBlockCompulsarySavingsEvent savingBlockEvent =
                            compulsorySaving.GetBlockCompulsorySavingEvent();
                        Debug.Assert(savingBlockEvent != null,
                                     "Saving should have block event on disbursment if it has compulsary saving");
                        savingBlockEvent.LoanEventId = loanDisbEventId;

                        _ePs.FireSavingBlockEvent(savingBlockEvent, compulsorySaving.Id, sqlTransaction);
                    }

                    _clientManager.IncrementLoanCycleByContractId(pLoan.Id, sqlTransaction);

                    SetEconomicActivity(pLoan, sqlTransaction);

                    loanDisbursmentEvent.PaymentMethodId = method.Id;
                    CallInterceptor(new Dictionary<string, object>
                    {
                        {"Loan", pLoan},
                        {"Event", loanDisbursmentEvent},
                        {"SqlTransaction", sqlTransaction}
                    });
                    CallInterceptor(new Dictionary<string, object>
                    {
                        {"Loan", pLoan},
                        {
                            "Event", new LoanEntryFeeEvent
                                {
                                    Id = loanDisbursmentEvent.Commissions.First().Id,
                                    Fee = loanDisbursmentEvent.Commissions.Sum(i => i.Fee.Value),
                                    Code = "LEE0"
                                }
                        },
                        {"SqlTransaction", sqlTransaction}
                    });

                    sqlTransaction.Commit();
                    return copyLoan;
                }
                catch
                {
                    if (sqlTransaction != null) sqlTransaction.Rollback();
                    throw;
                }
            }
        }

        private void CheckDisbursedLoan(Loan copyLoan, DateTime pDateToDisburse)
        {
            if (copyLoan == null) throw new ArgumentNullException("copyLoan");

            if (copyLoan.FundingLine == null)
                throw new OpenCbsContractSaveException(OpenCbsContractSaveExceptionEnum.FundingLineIsNull);

            if (!copyLoan.FundingLine.Currency.Equals(copyLoan.Product.Currency))
                throw new OpenCbsContractSaveException(OpenCbsContractSaveExceptionEnum.CurrencyMisMatch);

            if (((DateTime) copyLoan.CreditCommiteeDate).Date > pDateToDisburse.Date)
                throw new OpenCbsContractSaveException(
                    OpenCbsContractSaveExceptionEnum.LoanWasValidatedLaterThanDisbursed);

            if (copyLoan.Product.UseCompulsorySavings && copyLoan.CompulsorySavings == null)
                throw new OpenCbsContractSaveException(OpenCbsContractSaveExceptionEnum.LoanHasNoCompulsorySavings);

            if (copyLoan.Disbursed || _loanManager.IsLoanDisbursed(copyLoan.Id))
                throw new OpenCbsContractSaveException(OpenCbsContractSaveExceptionEnum.LoanAlreadyDisbursed);
        }

        public Loan ChangeRepaymentType(Loan loan, IClient client, int installmentNumber, DateTime date,
                                        OCurrency amount, bool disableFees, OCurrency manualFeesAmount,
                                        OCurrency manualCommission,
                                        bool disableInterests, OCurrency manualInterestsAmount,
                                        bool keepExpectedInstallment, bool proportionPayment,
                                        PaymentMethod paymentMethod, string comment, bool pending)
        {
            return Repay(loan, client, installmentNumber, date, amount, disableFees, manualFeesAmount, manualCommission,
                         disableInterests, manualInterestsAmount, keepExpectedInstallment, proportionPayment,
                         paymentMethod, comment, pending);
        }

        public Loan ManualInterestCalculation(Loan loan, IClient client, int installmentNumber, DateTime date,
                                              OCurrency amount, bool disableFees, OCurrency manualFeesAmount,
                                              OCurrency manualCommission,
                                              bool disableInterests, OCurrency manualInterestsAmount,
                                              bool keepExpectedInstallment, bool proportionPayment,
                                              PaymentMethod paymentMethod, string comment, bool pending)
        {
            return Repay(loan, client, installmentNumber, date, amount, disableFees, manualFeesAmount, manualCommission,
                         disableInterests, manualInterestsAmount, keepExpectedInstallment, proportionPayment,
                         paymentMethod, comment, pending);
        }

        public Loan ManualFeesCalculation(Loan loan, IClient client, int installmentNumber, DateTime date,
                                          OCurrency amount, bool disableFees, OCurrency manualFeesAmount,
                                          OCurrency manualCommission,
                                          bool disableInterests, OCurrency manualInterestsAmount,
                                          bool keepExpectedInstallment, bool proportionPayment,
                                          PaymentMethod paymentMethod, string comment, bool pending)
        {
            return Repay(loan, client, installmentNumber, date, amount, disableFees, manualFeesAmount, manualCommission,
                         disableInterests, manualInterestsAmount, keepExpectedInstallment, proportionPayment,
                         paymentMethod, comment, pending);
        }

        public Loan Repay(Loan curentLoan,
                          IClient client,
                          int installmentNumber,
                          DateTime payDate,
                          OCurrency amount,
                          bool disableFees,
                          OCurrency manualFeesAmount,
                          OCurrency manualCommission,
                          bool disableInterests,
                          OCurrency manualInterestsAmount,
                          bool keepExpectedInstallment,
                          bool proportionPayment,
                          PaymentMethod paymentMethod,
                          string comment,
                          bool isPending)
        {
            if (payDate.Date < curentLoan.StartDate.Date)
            {
                throw new OpenCbsRepayException(OpenCbsRepayExceptionsEnum.RepaymentBeforeDisburse);
            }

            if (curentLoan.Events.GetRepaymentEvents().Any(r => !r.Deleted && r.Date > payDate))
            {
                throw new OpenCbsRepayException(OpenCbsRepayExceptionsEnum.RepaymentBeforeLastEventDate);
            }
            CheckOperationDate(payDate);

            Loan savedContract = curentLoan.Copy();
            using (SqlConnection conn = _loanManager.GetConnection())
            using (SqlTransaction sqlTransaction = conn.BeginTransaction())
            {
                try
                {
                    //Repay
                    // Accrued Interest Event generation
                    if (ApplicationSettings.GetInstance(_user.Md5).AccountingProcesses == OAccountingProcesses.Accrual)
                    {
                        DateTime dateTime = payDate;

                        AccruedInterestEvent e = savedContract.GetAccruedInterestEvent(dateTime);

                        if (e != null)
                        {
                            if (Teller.CurrentTeller != null && Teller.CurrentTeller.Id != 0)
                                e.TellerId = Teller.CurrentTeller.Id;

                            ServicesProvider.GetInstance().GetContractServices().AddAccruedInterestEvent(savedContract,
                                                                                                         e,
                                                                                                         sqlTransaction);
                        }
                    }

                    RepaymentEvent repayEvent = savedContract.Repay(installmentNumber, payDate, amount, disableFees,
                                                                    manualFeesAmount, manualCommission,
                                                                    disableInterests, manualInterestsAmount,
                                                                    keepExpectedInstallment, proportionPayment,
                                                                    paymentMethod, comment,
                                                                    isPending);

                    savedContract.EscapedMember = curentLoan.EscapedMember;
                    repayEvent.User = _user;
                    //the code below should be moved into event generation proccess
                    if (Teller.CurrentTeller != null && Teller.CurrentTeller.Id != 0)
                        repayEvent.TellerId = Teller.CurrentTeller.Id;

                    if (!isPending)
                    {
                        FundingLineEvent repayFlFundingLineEvent = new FundingLineEvent
                            {
                                Code =
                                    String.Concat("RE_", curentLoan.Code,
                                                  "_INS_", installmentNumber),
                                Type = OFundingLineEventTypes.Repay,
                                Amount =
                                    ApplicationSettings.GetInstance(
                                        _user != null
                                            ? _user.Md5
                                            : "")
                                                       .InterestsCreditedInFL
                                        ? amount
                                        : repayEvent.Principal,
                                Movement = OBookingDirections.Credit,
                                CreationDate = TimeProvider.Now,
                                FundingLine = curentLoan.FundingLine,
                                AttachTo = repayEvent
                            };
                        //the code below should be moved into event generation proccess
                        if (Teller.CurrentTeller != null && Teller.CurrentTeller.Id != 0)
                            repayFlFundingLineEvent.TellerId = Teller.CurrentTeller.Id;

                        //this line is to prevent errors from popping up when client makes a repayment of 
                        //everything but the principal 
                        if (repayFlFundingLineEvent.Amount > 0
                            || ApplicationSettings.GetInstance(_user != null ? _user.Md5 : "").InterestsCreditedInFL)
                            curentLoan.FundingLine.AddEvent(
                                new FundingLineServices(_user).AddFundingLineEvent(repayFlFundingLineEvent,
                                                                                   sqlTransaction));
                    }

                    foreach (RepaymentEvent loanEvent in savedContract.Events.GetLoanRepaymentEvents())
                    {
                        if (loanEvent.IsFired) continue;
                        loanEvent.Comment = comment;
                    }

                    _ePs.FireEvent(repayEvent, savedContract, sqlTransaction);
                    if (paymentMethod.Method == OPaymentMethods.Savings)
                        DoRepaymentFromSavings(savedContract, repayEvent, amount, sqlTransaction);

                    if (repayEvent.RepaymentType == OPaymentType.TotalPayment && savedContract.HasCompulsoryAmount())
                    {
                        SavingBookContract compulsorySaving = savedContract.CompulsorySavings;
                        SavingUnblockCompulsorySavingsEvent savingUnblockEvent =
                            compulsorySaving.GetUnblockCompulsorySavingEvent();
                        if (savingUnblockEvent != null)
                            _ePs.FireSavingUnblockEvent(savingUnblockEvent, compulsorySaving.Id, repayEvent.Id,
                                                        sqlTransaction);
                    }

                    // Put a copy of installments into the history
                    ArchiveInstallments(curentLoan, repayEvent, sqlTransaction);

                    CreditInsuranceEvent cie = savedContract.Events.GetCreditInsuranceEvents();
                    if (cie != null)
                        _ePs.FireEvent(cie, savedContract, sqlTransaction);

                    //Update Installments
                    foreach (Installment installment in savedContract.InstallmentList)
                        _instalmentManager.UpdateInstallment(installment, savedContract.Id, repayEvent.Id,
                                                             sqlTransaction);

                    for (int i = savedContract.Events.GetNumberOfEvents - 1; i >= 0; i--)
                    {
                        Event e = savedContract.Events.GetEvent(i);
                        if ((e is RepaymentEvent) || (e is LoanDisbursmentEvent))
                        {
                            e.Cancelable = true;
                            if (!e.Cancelable)
                                break;
                        }
                        else
                        {
                            break;
                        }
                    }

                    var repaymentEvents = (from item in savedContract.Events.GetRepaymentEvents()
                                           where item.ParentId == repayEvent.Id || item.Id == repayEvent.Id
                                           select item).ToList();
                    var listOfRble = (from item in repaymentEvents where item.Code == "RBLE" select item).ToList();
                    var listOfRgle = repaymentEvents.Except(listOfRble).ToList();
                    if (repayEvent.Code == "RBLE")
                        CallInterceptor(new Dictionary<string, object>
                        {
                            {"Loan", savedContract},
                            {
                                "Event", new RepaymentEvent
                                {
                                    Code = "RBLE",
                                    Principal = listOfRble.Sum(item => item.Principal.Value),
                                    Interests = listOfRble.Sum(item => item.Interests.Value),
                                    Commissions = listOfRble.Sum(item => item.Commissions.Value),
                                    Penalties = listOfRble.Sum(item => item.Fees.Value),
                                    Id = repayEvent.Id
                                }
                            },
                            {"SqlTransaction", sqlTransaction}
                        });
                    CallInterceptor(new Dictionary<string, object>
                    {
                        {"Loan", savedContract},
                        {
                            "Event", new RepaymentEvent
                            {
                                Code = "RGLE",
                                Principal = listOfRgle.Sum(item => item.Principal.Value),
                                Interests = listOfRgle.Sum(item => item.Interests.Value),
                                Commissions = listOfRgle.Sum(item => item.Commissions.Value),
                                Penalties = listOfRgle.Sum(item => item.Fees.Value),
                                Id = repayEvent.Id
                            }
                        },
                        {"SqlTransaction", sqlTransaction}
                    });

                    if (savedContract.AllInstallmentsRepaid)
                    {
                        _ePs.FireEvent(savedContract.GetCloseEvent(payDate), savedContract, sqlTransaction);
                    }

                    _loanManager.UpdateLoan(savedContract, sqlTransaction);



                    if (sqlTransaction != null)
                    {
                        sqlTransaction.Commit();
                        sqlTransaction.Dispose();
                    }

                    SetClientStatus(savedContract, client);
                    savedContract.EscapedMember = null;
                    return savedContract;
                }
                catch (Exception)
                {
                    if (sqlTransaction != null) sqlTransaction.Rollback();
                    throw;
                }
            }
        }

        private void DoRepaymentFromSavings(Loan savedContract, RepaymentEvent repayEvent, OCurrency amount,
                                            SqlTransaction sqlTransaction)
        {
            if (savedContract.CompulsorySavings == null)
                throw new OpenCbsSavingException(OpenCbsSavingExceptionEnum.NoCompulsorySavings);

            string description = string.Format("Repayment for loan {0}", savedContract.Code);

            _savingServices.RepayLoanFromSaving(savedContract, repayEvent, savedContract.CompulsorySavings,
                                                repayEvent.Date, amount, description, sqlTransaction);
        }

        public Loan ConfirmPendingRepayment(Loan pLoan, IClient pClient)
        {
            Loan savedContract = pLoan.Copy();
            using (SqlConnection conn = _loanManager.GetConnection())
            using (SqlTransaction sqlTransaction = conn.BeginTransaction())
            {
                try
                {
                    RepaymentEvent repayEvent = savedContract.ConfirmPendingRepayment();
                    repayEvent.User = _user;
                    if (Teller.CurrentTeller != null && Teller.CurrentTeller.Id != 0)
                        repayEvent.TellerId = Teller.CurrentTeller.Id;

                    FundingLineEvent repayFlFundingLineEvent = new FundingLineEvent
                        {
                            Code =
                                String.Concat("RE_", pLoan.Code, "_INS_",
                                              repayEvent.InstallmentNumber),
                            Type = OFundingLineEventTypes.Repay,
                            Amount =
                                ApplicationSettings.GetInstance(_user != null
                                                                    ? _user.
                                                                          Md5
                                                                    : "").
                                                    InterestsCreditedInFL
                                    ? repayEvent.Principal +
                                      repayEvent.Interests
                                    : repayEvent.Principal,
                            Movement = OBookingDirections.Credit,
                            CreationDate = TimeProvider.Now,
                            FundingLine = pLoan.FundingLine
                            ,
                            AttachTo = repayEvent
                        };

                    if (Teller.CurrentTeller != null && Teller.CurrentTeller.Id != 0)
                        repayFlFundingLineEvent.TellerId = Teller.CurrentTeller.Id;

                    //this line is to prevent errors from popping up when client makes a repayment of 
                    //everything but the principal 
                    if (repayFlFundingLineEvent.Amount > 0 ||
                        ApplicationSettings.GetInstance(_user != null ? _user.Md5 : "").InterestsCreditedInFL)
                        pLoan.FundingLine.AddEvent(
                            new FundingLineServices(_user).AddFundingLineEvent(repayFlFundingLineEvent, sqlTransaction));

                    _ePs.FireEvent(repayEvent, savedContract, sqlTransaction);
                    // Put a copy of installments into the history
                    ArchiveInstallments(pLoan, repayEvent, sqlTransaction);

                    //Update Installments
                    foreach (Installment installment in savedContract.InstallmentList)
                    {
                        _instalmentManager.UpdateInstallment(installment, savedContract.Id, repayEvent.Id,
                                                             sqlTransaction);
                    }

                    for (int i = savedContract.Events.GetNumberOfEvents - 1; i >= 0; i--)
                    {
                        Event e = savedContract.Events.GetEvent(i);
                        if ((e is RepaymentEvent) || (e is LoanDisbursmentEvent))
                        {
                            e.Cancelable = true; //EventIsCancelable(e.Id, sqlTransaction);
                            if (!e.Cancelable)
                                break;
                        }
                        else
                        {
                            break;
                        }
                    }

                    _loanManager.UpdateLoan(savedContract, sqlTransaction);
                    if (sqlTransaction != null) sqlTransaction.Commit();

                    SetClientStatus(savedContract, pClient);

                    return savedContract;
                }
                catch (Exception)
                {
                    if (sqlTransaction != null) sqlTransaction.Rollback();
                    throw;
                }
            }
        }

        public void SetClientStatus(Loan pCredit, IClient pClient)
        {
            List<Loan> loansOfClient = _loanManager.SelectLoansByClientId(pClient.Id);
            pClient.ActiveLoans = new List<Loan>();
            List<IClient> membersOfGroup = new List<IClient>();

            foreach (var loan in loansOfClient)
            {
                if (loan.ContractStatus == OContractStatus.Active)
                    pClient.ActiveLoans.Add(loan);
            }
            if (pClient.ActiveLoans.Count == 0)
            {
                pClient.Active = false;
                pClient.Status = OClientStatus.Inactive;
            }
            else
            {
                pClient.Active = true;
                pClient.Status = OClientStatus.Active;
            }

            if (pClient is Group)
            {
                foreach (var member in ((Group) pClient).Members)
                {
                    membersOfGroup.Add(_clientManager.SelectPersonById(member.Tiers.Id));
                }
                //If corporate is Active it means all its members are active in anyway
                if (pClient.Active && pClient.Status == OClientStatus.Active)
                {
                    foreach (var client in membersOfGroup)
                    {
                        if (pCredit.EscapedMember != null && pCredit.EscapedMember.Tiers.Id == client.Id)
                        {
                            client.Active = false;
                            client.Status = OClientStatus.Inactive;
                        }
                        else
                        {
                            client.Active = true;
                            client.Status = OClientStatus.Active;
                        }
                    }
                }
                else
                {
                    foreach (var client in membersOfGroup)
                    {
                        client.ActiveLoans = new List<Loan>();
                        List<Loan> loansOfMember = _loanManager.SelectLoansByClientId(client.Id);
                        foreach (var loan in loansOfMember)
                        {
                            if (loan.ContractStatus == OContractStatus.Active
                                || loan.PendingOrPostponed()
                                || loan.ContractStatus == OContractStatus.Validated)
                                client.ActiveLoans.Add(loan);
                        }
                        if (client.ActiveLoans.Count == 0)
                        {
                            client.Status = OClientStatus.Inactive;
                            client.Active = false;
                        }
                        else
                        {
                            client.Status = OClientStatus.Active;
                            client.Active = true;
                        }
                    }
                }
            }
            using (SqlConnection conn = _clientManager.GetConnection())
            using (SqlTransaction sqlTransaction = conn.BeginTransaction())
            {
                try
                {
                    _clientManager.UpdateClientStatus(pClient, sqlTransaction);
                    if (pClient is Group)
                    {
                        foreach (var client in membersOfGroup)
                        {
                            _clientManager.UpdateClientStatus(client, sqlTransaction);
                        }
                    }

                    foreach (var guarantor in pCredit.Guarantors)
                    {
                        ((Person) guarantor.Tiers).Active = (pClient.Active ||
                                                             (_clientManager.GetNumberOfGuarantorsLoansCover(
                                                                 guarantor.Tiers.Id, sqlTransaction) != 0));
                        if (((Person) guarantor.Tiers).Active == false)
                        {
                            ((Person) guarantor.Tiers).Status = OClientStatus.Inactive;
                        }
                        _clientManager.UpdateClientStatus(guarantor.Tiers, sqlTransaction);
                    }
                    sqlTransaction.Commit();
                    sqlTransaction.Dispose();
                }
                catch (Exception)
                {
                    sqlTransaction.Rollback();
                    throw;
                }
            }
        }

        public List<Installment> SimulateScheduleCreation(Loan loan)
        {
            var scheduleConfiguration = _configurationFactory
                .Init()
                .WithLoan(loan)
                .Finish()
                .GetConfiguration();

            var scheduleBuilder = new ScheduleBuilder();
            var installmentList = scheduleBuilder.BuildSchedule(scheduleConfiguration);
            var schedule = Mapper.Map<IEnumerable<IInstallment>, List<Installment>>(installmentList);
            return schedule;
        }

        public Loan SimulateRescheduling(Loan loan, ScheduleConfiguration rescheduleConfiguration)
        {
            var copyOfLoan = loan.Copy();
            var scheduleConfiguration = _configurationFactory
                .Init()
                .WithLoan(copyOfLoan)
                .Finish()
                .GetConfiguration();

            var schedule = Mapper.Map<IEnumerable<Installment>, IEnumerable<IInstallment>>(copyOfLoan.InstallmentList);
            var scheduleBuilder = new ScheduleBuilder();
            var rescheduleAssembler = new RescheduleAssembler();
            var copyOfRescheduleConfiguration = (IScheduleConfiguration) rescheduleConfiguration.Clone();
            copyOfRescheduleConfiguration.InterestRate *=
                (decimal) scheduleConfiguration.PeriodPolicy.GetNumberOfPeriodsInYear(
                    copyOfRescheduleConfiguration.StartDate,
                    scheduleConfiguration.YearPolicy);

            schedule = rescheduleAssembler.AssembleRescheduling(
                schedule,
                scheduleConfiguration,
                copyOfRescheduleConfiguration,
                scheduleBuilder,
                loan.CalculateActualOlb().Value);

            var newSchedule = Mapper.Map<IEnumerable<IInstallment>, List<Installment>>(schedule);

            copyOfLoan.InstallmentList = newSchedule;
            copyOfLoan.NbOfInstallments = newSchedule.Count();
            return copyOfLoan;
        }

        public Loan SimulateTranche(Loan loan, ITrancheConfiguration trancheConfiguration)
        {
            var copyOfLoan = loan.Copy();
            var scheduleConfiguration = _configurationFactory
                .Init()
                .WithLoan(copyOfLoan)
                .Finish()
                .GetConfiguration();

            var schedule = Mapper.Map<IEnumerable<Installment>, IEnumerable<IInstallment>>(copyOfLoan.InstallmentList);
            var scheduleBuilder = new ScheduleBuilder();
            var trancheBuilder = new TrancheBuilder();
            var trancheAssembler = new TrancheAssembler();
            var copyOfTrancheConfiguration = (ITrancheConfiguration) trancheConfiguration.Clone();
            copyOfTrancheConfiguration.InterestRate *=
                (decimal) scheduleConfiguration.PeriodPolicy.GetNumberOfPeriodsInYear(
                    copyOfTrancheConfiguration.StartDate,
                    scheduleConfiguration.YearPolicy);

            schedule = trancheAssembler.AssembleTranche(
                schedule,
                scheduleConfiguration,
                copyOfTrancheConfiguration,
                scheduleBuilder,
                trancheBuilder);

            var newSchedule = Mapper.Map<IEnumerable<IInstallment>, List<Installment>>(schedule);

            foreach (var installment in newSchedule)
            {
                var oldInstallment = copyOfLoan.InstallmentList.Find(i => i.Number == installment.Number);
                if (oldInstallment == null) break;

                installment.Comment = oldInstallment.Comment;
                installment.PaidFees = oldInstallment.PaidFees;
                installment.PaidCommissions = oldInstallment.PaidCommissions;
                installment.FeesUnpaid = oldInstallment.FeesUnpaid;
                installment.CommissionsUnpaid = oldInstallment.CommissionsUnpaid;
                installment.IsPending = oldInstallment.IsPending;
                //installment.PaidDate = oldInstallment.PaidDate;
            }
            copyOfLoan.InstallmentList = newSchedule;
            copyOfLoan.NbOfInstallments = newSchedule.Count();
            copyOfLoan.Amount += trancheConfiguration.Amount;
            return copyOfLoan;
        }

        public Loan AddOverdueEvent(Loan loan, OverdueEvent overdueEvent)
        {
            using (SqlConnection conn = _savingEventManager.GetConnection())
            using (SqlTransaction sqlTransaction = conn.BeginTransaction())
                try
                {
                    _ePs.FireEvent(overdueEvent, loan, sqlTransaction);
                    sqlTransaction.Commit();

                    return loan;
                }
                catch (Exception ex)
                {
                    sqlTransaction.Rollback();
                    throw ex;
                }
        }

        public Loan AddAccruedInterestEvent(Loan loan, AccruedInterestEvent accruedInterestEvent)
        {
            using (SqlConnection conn = _savingEventManager.GetConnection())
            using (SqlTransaction sqlTransaction = conn.BeginTransaction())
            {
                try
                {
                    _ePs.FireEvent(accruedInterestEvent, loan, sqlTransaction);

                    //loan.Events.Add(accruedInterestEvent);
                    sqlTransaction.Commit();
                    sqlTransaction.Dispose();
                    return loan;
                }
                catch (Exception ex)
                {
                    sqlTransaction.Rollback();
                    throw ex;
                }
            }
        }

        public Loan AddAccruedInterestEvent(Loan loan, AccruedInterestEvent accruedInterestEvent,
                                            SqlTransaction sqlTransaction)
        {
            try
            {
                _ePs.FireEvent(accruedInterestEvent, loan, sqlTransaction);

                return loan;
            }
            catch (Exception ex)
            {
                sqlTransaction.Rollback();
                throw ex;
            }
        }

        public void PostEvents(List<Loan> loans, EventStock eventStock)
        {
            using (SqlConnection conn = _savingEventManager.GetConnection())
            using (SqlTransaction sqlTransaction = conn.BeginTransaction())
            {
                try
                {
                    foreach (Event e in eventStock)
                    {
                        if (e is SavingEvent) continue;

                        Loan loan = new Loan();
                        foreach (Loan l in loans)
                        {
                            if (l.Id == e.ContracId)
                            {
                                loan = l;
                            }
                        }

                        if (loan.Id > 0)
                            _ePs.FireEvent(e, loan, sqlTransaction);
                    }

                    sqlTransaction.Commit();
                }
                catch (Exception ex)
                {
                    if (sqlTransaction != null)
                        sqlTransaction.Rollback();

                    throw ex;
                }
            }
        }

        public Loan AddProvisionEvent(Loan loan, ProvisionEvent provisionEvent)
        {
            using (SqlConnection conn = _savingEventManager.GetConnection())
            using (SqlTransaction sqlTransaction = conn.BeginTransaction())
            {
                try
                {
                    //insert into table ProvisionEvent
                    _ePs.FireEvent(provisionEvent, loan, sqlTransaction);

                    //loan.Events.Add(provisionEvent);
                    sqlTransaction.Commit();

                    return loan;
                }
                catch (Exception ex)
                {
                    if (sqlTransaction != null)
                        sqlTransaction.Rollback();

                    throw ex;
                }
            }
        }

        public Loan AddManualScheduleChangeEvent(Loan loan, ManualScheduleChangeEvent manualScheduleChangeEvent)
        {
            using (SqlConnection conn = _savingEventManager.GetConnection())
            using (SqlTransaction sqlTransaction = conn.BeginTransaction())
            {
                try
                {
                    Loan copyOfLoan = loan.Copy();
                    _ePs.FireEvent(manualScheduleChangeEvent, loan, sqlTransaction);
                    ArchiveInstallments(copyOfLoan, manualScheduleChangeEvent, sqlTransaction);

                    sqlTransaction.Commit();
                    return loan;
                }
                catch (Exception ex)
                {
                    sqlTransaction.Rollback();
                    throw ex;
                }
            }
        }

        public Loan AddTranche(Loan loan, IClient client, ITrancheConfiguration trancheConfiguration, IList<LoanEntryFee> entryFees)
        {
            using (var connection = _loanManager.GetConnection())
            using (var transaction = connection.BeginTransaction())
            {
                try
                {
                    CheckTranche(trancheConfiguration.StartDate, loan, trancheConfiguration.Amount);

                    var copyOfLoan = SimulateTranche(loan, trancheConfiguration);
                    var startInstallment =
                        copyOfLoan.InstallmentList
                                  .FindAll(i => i.ExpectedDate <= trancheConfiguration.StartDate)
                                  .LastOrDefault();
                    var trancheEvent = new TrancheEvent
                        {
                            Amount = trancheConfiguration.Amount,
                            ApplyNewInterest = trancheConfiguration.ApplyNewInterestRateToOlb,
                            Maturity = trancheConfiguration.NumberOfInstallments,
                            StartDate = trancheConfiguration.StartDate,
                            Date = trancheConfiguration.StartDate,
                            InterestRate = trancheConfiguration.InterestRate/100,
                            Number = copyOfLoan.GivenTranches.Count,
                            FirstRepaymentDate = trancheConfiguration.PreferredFirstInstallmentDate,
                            GracePeriod = trancheConfiguration.GracePeriod,
                            StartedFromInstallment = startInstallment == null ? 0 : startInstallment.Number,
                            User = _user,
                        };

                    trancheEvent.User = _user;

                    //insert into table TrancheEvent
                    _ePs.FireEvent(trancheEvent, copyOfLoan, transaction);
                    copyOfLoan.Events.Add(trancheEvent);

                    CallInterceptor(new Dictionary<string, object>
                    {
                        {"Loan", copyOfLoan},
                        {"Event", trancheEvent},
                        {"SqlTransaction", transaction}
                    });

                    // Add entry fee events
                    foreach (var entryFee in entryFees)
                    {
                        if (entryFee.FeeValue == 0) continue;
                        var entryFeeEvent = new LoanEntryFeeEvent
                        {
                            Fee = entryFee.FeeValue,
                            Code = "LEE" + entryFee.ProductEntryFee.Index,
                            DisbursementEventId = trancheEvent.Id,
                            Cancelable = true,
                            User = User.CurrentUser,
                            Date = trancheEvent.Date
                        };
                        _ePs.FireEvent(entryFeeEvent, copyOfLoan, transaction);
                        copyOfLoan.Events.Add(entryFeeEvent);
                    }

                    var trancheEntryFeeEvent =
                        copyOfLoan.Events.OfType<LoanEntryFeeEvent>()
                                  .First(i => i.DisbursementEventId == trancheEvent.Id);
                    if (trancheEntryFeeEvent != null)
                        CallInterceptor(new Dictionary<string, object>
                        {
                            {"Loan", copyOfLoan},
                            {
                                "Event", new LoanEntryFeeEvent
                                    {
                                        Id = trancheEntryFeeEvent.Id,
                                        Fee = entryFees.Sum(i => i.FeeValue),
                                        Code = "LEE0"
                                    }
                            },
                            {"SqlTransaction", transaction}
                        });

                    ArchiveInstallments(loan, trancheEvent, transaction);

                    //delete all the old installments of the table Installments
                    _instalmentManager.DeleteInstallments(loan.Id, transaction);

                    //insert all the new installments in the table Installments
                    _instalmentManager.AddInstallments(copyOfLoan.InstallmentList, copyOfLoan.Id, transaction);

                    //Activate the contract if it's closed because of new tranch
                    if (copyOfLoan.Closed)
                    {
                        copyOfLoan.ContractStatus = OContractStatus.Active;
                        copyOfLoan.Closed = false;
                        _loanManager.UpdateLoan(copyOfLoan, transaction);
                    }
                    //in the feature might be combine UpdateLoan + UpdateLoanWithinTranche
                    _loanManager.UpdateLoanWithinTranche(
                        trancheConfiguration.InterestRate/100,
                        copyOfLoan.NbOfInstallments,
                        copyOfLoan,
                        transaction);
                    copyOfLoan.GivenTranches.Add(trancheEvent);
                    transaction.Commit();

                    SetClientStatus(copyOfLoan, client);
                    return copyOfLoan;
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        public void CheckLoanForTranche(Loan loan)
        {
            if (loan.Product.DrawingsNumber.Value <= loan.GivenTranches.Count - 1)
            {
                throw new OpenCbsContractSaveException(OpenCbsContractSaveExceptionEnum.TrancheMaturityError);
            }
        }

        private static void CheckTranche(DateTime date, Loan loan, decimal amount)
        {
            var repayment = (from e in loan.Events.GetEvents()
                             where
                                 e is RepaymentEvent || e is RepaymentOverWriteOffEvent
                                 
                             orderby e.Date
                             select e).LastOrDefault();
            if (repayment != null && date.Date < repayment.Date.Date)
            {
                throw new OpenCbsContractSaveException(OpenCbsContractSaveExceptionEnum.TrancheDate);
            }
            if (loan.StartDate > date.Date)
            {
                throw new OpenCbsContractSaveException(OpenCbsContractSaveExceptionEnum.TrancheDate);
            }

            if (loan.AmountUnderLoc.HasValue)
            {
                if (loan.Amount + amount > loan.AmountUnderLoc)
                {
                    throw new OpenCbsContractSaveException(OpenCbsContractSaveExceptionEnum.TrancheAmount);
                }
            }
            else
            {
                if (amount > loan.Product.AmountUnderLoc)
                {
                    throw new OpenCbsContractSaveException(OpenCbsContractSaveExceptionEnum.TrancheAmount);
                }
            }

            if (amount == 0)
                throw new OpenCbsContractSaveException(OpenCbsContractSaveExceptionEnum.TrancheAmount);
        }

        public Loan Reschedule(Loan loan, IClient client, ScheduleConfiguration rescheduleConfiguration)
        {
            using (var connection = _loanManager.GetConnection())
            using (var transaction = connection.BeginTransaction())
            {
                try
                {
                    var copyOfLoan = SimulateRescheduling(loan, rescheduleConfiguration);
                    var rescheduleLoanEvent = new RescheduleLoanEvent
                        {
                            Amount = copyOfLoan.CalculateActualOlb(),
                            Date = rescheduleConfiguration.StartDate,
                            ClientType = client.Type,
                            Interest = rescheduleConfiguration.InterestRate,
                            BadLoan = loan.BadLoan,
                            NbOfMaturity = rescheduleConfiguration.NumberOfInstallments,
                            GracePeriod = rescheduleConfiguration.GracePeriod,
                            ChargeInterestDuringGracePeriod = rescheduleConfiguration.ChargeInterestDuringGracePeriod,
                            InstallmentNumber =
                                copyOfLoan.GetLastFullyRepaidInstallment() == null
                                    ? 1
                                    : copyOfLoan.GetLastFullyRepaidInstallment().Number + 1,
                            PreferredFirstInstallmentDate = rescheduleConfiguration.PreferredFirstInstallmentDate,
                            User = _user,
                        };

                    //insert into table TrancheEvent
                    _ePs.FireEvent(rescheduleLoanEvent, copyOfLoan, transaction);

                    ArchiveInstallments(loan, rescheduleLoanEvent, transaction);

                    //delete all the old installments of the table Installments
                    _instalmentManager.DeleteInstallments(loan.Id, transaction);

                    //insert all the new installments in the table Installments
                    _instalmentManager.AddInstallments(copyOfLoan.InstallmentList, copyOfLoan.Id, transaction);

                    copyOfLoan.Events.Add(rescheduleLoanEvent);
                    transaction.Commit();

                    SetClientStatus(copyOfLoan, client);
                    return copyOfLoan;
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        public void UpdateCredit(decimal interestRate, int nbOfMaturity, Loan loan)
        {
            _loanManager.UpdateLoanToRescheduled(interestRate, nbOfMaturity, loan);
        }

        public KeyValuePair<Loan, RepaymentEvent> ShowNewContract(Loan pContract,
                                                                  int installmentNumber,
                                                                  DateTime date,
                                                                  OCurrency amount,
                                                                  bool disableFees,
                                                                  OCurrency manualFeesAmount,
                                                                  OCurrency manualCommissionAmount,
                                                                  bool disableInterests,
                                                                  OCurrency manualInterests,
                                                                  bool pKeepExpectedInstallment,
                                                                  bool doProportionPayment,
                                                                  PaymentMethod pPaymentMethod,
                                                                  bool pPending,
                                                                  bool isTotalRepayment)
        {

            if (!pContract.UseCents)
            {
                if (amount != Decimal.Ceiling(amount.Value))
                {
                    throw new OpenCbsRepayException(OpenCbsRepayExceptionsEnum.DecimalAmount);
                }
            }
            else if (amount < 0)
                throw new OpenCbsRepayException(OpenCbsRepayExceptionsEnum.NegativeAmount);

            Loan fakeContract = pContract.Copy();

            OCurrency expectedMaxAmount = fakeContract.CalculateMaximumAmountAuthorizedToRepay(installmentNumber, date,
                                                                                               disableFees,
                                                                                               manualFeesAmount,
                                                                                               manualCommissionAmount,
                                                                                               disableInterests,
                                                                                               manualInterests,
                                                                                               pKeepExpectedInstallment);

            expectedMaxAmount = fakeContract.UseCents
                                    ? Math.Round(expectedMaxAmount.Value, 2, MidpointRounding.AwayFromZero)
                                    : expectedMaxAmount;


            if (!disableFees && !isTotalRepayment)
            {
                if (AmountComparer.Compare(amount, expectedMaxAmount) > 0)
                    throw new OpenCbsRepayException(OpenCbsRepayExceptionsEnum.AmountGreaterThanTotalRemainingAmount);

            }
            else
            {
                if (isTotalRepayment)
                    amount = expectedMaxAmount;
            }


            RepaymentEvent e = fakeContract.Repay(installmentNumber, date, amount, disableFees, manualFeesAmount,
                                                  manualCommissionAmount, disableInterests, manualInterests,
                                                  pKeepExpectedInstallment, doProportionPayment, pPaymentMethod, null,
                                                  pPending);

            return new KeyValuePair<Loan, RepaymentEvent>(fakeContract, e);
        }

        public List<LoanEntryFee> GetDefaultLoanEntryFees(Loan loan, IClient client)
        {
            _productServices = ServicesProvider.GetInstance().GetProductServices();
            List<EntryFee> entryFees = _productServices.GetProductEntryFees(loan.Product, client);
            var loanEntryFees = new List<LoanEntryFee>();

            foreach (EntryFee fee in entryFees)
            {
                LoanEntryFee loanFee = new LoanEntryFee();
                loanFee.ProductEntryFee = fee;
                if (fee.Min != null)
                    loanFee.FeeValue = (decimal) fee.Min;
                else
                    loanFee.FeeValue = (decimal) fee.Value;
                loanEntryFees.Add(loanFee);
            }
            return loanEntryFees;
        }

        public List<LoanEntryFee> GetInstalledLoanEntryFees(Loan loan)
        {
            _productServices = ServicesProvider.GetInstance().GetProductServices();
            List<LoanEntryFee> loanEntryFees = _loanManager.SelectInstalledLoanEntryFees(loan.Id);
            foreach (LoanEntryFee loanEntryFee in loanEntryFees)
            {
                loanEntryFee.ProductEntryFee = _productServices.GetEntryFeeById(loanEntryFee.ProductEntryFeeId);
            }
            return loanEntryFees;
        }

        public List<Alert> FindContractsByOfficer(int officerCode)
        {
            AlertStock alertStock = _loanManager.SelectLoansByLoanOfficer(officerCode);
            return alertStock.SortAlertsByLoanStatus();
        }

        public IEnumerable<ReassignContractItem> FindContractsByLoanOfficerId(int officerCode, bool onlyActive)
        {
            return _loanManager.SelectLoansByLoanOfficerId(officerCode, onlyActive);
        }

        public void ReassignContract(int contractId, int officerToId, int officerFromId)
        {
            using (SqlConnection conn = _loanManager.GetConnection())
            using (SqlTransaction sqlTransac = conn.BeginTransaction())
            {
                try
                {
                    _loanManager.UpdateLoanLoanOfficer(contractId, officerToId, officerFromId, sqlTransac);
                    sqlTransac.Commit();
                }
                catch (Exception ex)
                {
                    sqlTransac.Rollback();
                    throw ex;
                }
            }
        }

        public void ReassignLoans(int[] ids, int to)
        {
            using (var connection = _loanManager.GetConnection())
            using (var transaction = connection.BeginTransaction())
            {
                try
                {
                    _loanManager.ReassignLoans(ids, to, transaction);
                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        public List<Loan> FindAllContractsForClosure()
        {
            return _loanManager.SelectLoansForClosure(OClosureTypes.Degradation);
        }

        public List<Loan> SelectContractsForClosure()
        {
            return _loanManager.SelectLoansForClosure();
        }

        public int GetNumberOfContractsForClosure()
        {
            return _loanManager.GetNbOfLoansForClosure();
        }

        /// <summary>
        /// Cancels last event from given contract, restores associated installment status.
        /// and restores Client(individual, corporate) status 
        /// </summary>
        /// <param name="contract">Contract</param>
        /// <param name="pClient"></param>
        /// <param name="comment"> </param>
        /// <returns>Cancelled event</returns>
        public Event CancelLastEvent(Loan contract, IClient pClient, string comment)
        {
            using (SqlConnection conn = _loanManager.GetConnection())
            using (SqlTransaction sqlTransaction = conn.BeginTransaction())
            {
                Event cancelledEvent;
                try
                {
                    Event evnt = contract.GetLastNonDeletedEvent();

                    if (null == evnt)
                        throw new OpenCbsContractSaveException(OpenCbsContractSaveExceptionEnum.EventIsNull);

                    if (!evnt.Cancelable)
                        throw new OpenCbsContractSaveException(OpenCbsContractSaveExceptionEnum.EventNotCancelable);

                    if (string.IsNullOrEmpty(comment))
                        throw new OpenCbsContractSaveException(OpenCbsContractSaveExceptionEnum.EventCommentIsEmpty);

                    if (pClient is Person)
                        evnt.ClientType = OClientTypes.Person;
                    else if (pClient is Group)
                        evnt.ClientType = OClientTypes.Group;
                    else if (pClient is Corporate)
                        evnt.ClientType = OClientTypes.Corporate;
                    else if (pClient is Village)
                        evnt.ClientType = OClientTypes.Village;

                    var evntCopy = evnt.Copy();
                    evntCopy.Id = evnt.ParentId ?? evnt.Id;
                    CallInterceptor(new Dictionary<string, object>
                    {
                        {"Loan", contract},
                        {"Event", evntCopy},
                        {"Deleted", true},
                        {"SqlTransaction", sqlTransaction}
                    });
                    if (ApplicationSettings.GetInstance(User.CurrentUser.Md5).UseMandatorySavingAccount)
                    {
                        var id = 0;
                        if (int.TryParse(evnt.Comment, out id))
                            ServicesProvider.GetInstance()
                                            .GetSavingServices()
                                            .DeleteEvent(new SavingWithdrawEvent()
                                                {
                                                    Id = id,
                                                    CancelDate = TimeProvider.Today
                                                });
                    }
                    evnt.Comment = comment;
                    evnt.CancelDate = TimeProvider.Now;

                    // if event is loan close event, we delete it first
                    if (evnt is LoanCloseEvent)
                    {
                        _ePs.CancelFireEvent(evnt, sqlTransaction, contract, contract.Product.Currency.Id);
                        evnt.Deleted = true;
                        evnt = contract.GetLastNonDeletedEvent();
                        if (pClient is Person)
                            evnt.ClientType = OClientTypes.Person;
                        else if (pClient is Group)
                            evnt.ClientType = OClientTypes.Group;
                        else if (pClient is Corporate)
                            evnt.ClientType = OClientTypes.Corporate;
                        else if (pClient is Village)
                            evnt.ClientType = OClientTypes.Village;
                        evnt.Comment = comment;
                        evnt.CancelDate = TimeProvider.Now;
                    }
                    _ePs.CancelFireEvent(evnt, sqlTransaction, contract, contract.Product.Currency.Id);
                    _ePs.UpdateCommentForLoanEvent(evnt, sqlTransaction);

                    evnt.Deleted = true;
                    //in case total repayment there could be several rep events
                    foreach (RepaymentEvent evt in contract.Events.GetRepaymentEvents())
                    {
                        if ((evt.ParentId == evnt.ParentId && evnt.ParentId != null) || (evt.Id == evnt.ParentId))
                        {
                            evt.Deleted = true;
                            evt.Comment = evnt.Comment;
                            _ePs.UpdateCommentForLoanEvent(evt, sqlTransaction);
                        }
                    }

                    if (evnt.Code == "ATR" || evnt.Code == "RBLE")
                    {
                        foreach (Event cie in contract.Events)
                        {
                            if (cie is CreditInsuranceEvent)
                                if (cie.Deleted == false && cie.Code == "LCIP")
                                {
                                    _ePs.CancelFireEvent(cie, sqlTransaction, contract, contract.Product.Currency.Id);
                                    cie.Deleted = true;
                                }
                        }
                    }

                    cancelledEvent = evnt;
                    // Restore the installment status.
                    UnarchiveInstallments(contract, cancelledEvent, sqlTransaction);
                    contract.InstallmentList = _instalmentManager.SelectInstallments(contract.Id, sqlTransaction);
                    contract.GivenTranches = _loanManager.SelectTranches(contract.Id, sqlTransaction);
                    contract.NbOfInstallments = contract.InstallmentList.Count;

                    if (evnt is LoanDisbursmentEvent)
                    {
                        contract.Disbursed = false;
                    }
                    else if (evnt is RescheduleLoanEvent)
                    {
                        contract.Rescheduled = false;
                    }
                    else if (cancelledEvent is TrancheEvent)
                    {
                        contract.Amount = contract.Amount - (cancelledEvent as TrancheEvent).Amount;

                        TrancheEvent trancheEventToDelete = new TrancheEvent();

                        foreach (var trancheEvent in contract.GivenTranches)
                        {
                            if (trancheEvent.Id == cancelledEvent.Id)
                            {
                                trancheEventToDelete = trancheEvent;
                            }
                        }

                        contract.GivenTranches.Remove(trancheEventToDelete);

                        if (contract.AllInstallmentsRepaid)
                        {
                            contract.ContractStatus = OContractStatus.Closed;
                            contract.Closed = true;
                            //Restore interest rate
                            contract.InterestRate =
                                contract.GivenTranches[contract.GivenTranches.Count - 1].InterestRate.Value;
                        }
                    }
                    else if (cancelledEvent is RepaymentEvent)
                    {
                        //restor a person of the corporate
                        _clientManager.RestorMemberOfGroupByEventId(cancelledEvent.Id, contract, sqlTransaction);
                        contract.EscapedMember = null;
                        if (cancelledEvent.RepaymentType == OPaymentType.TotalPayment)
                        {
                            if (contract.HasCompulsoryAmount())
                            {
                                SavingEvent savingUnblockEvent =
                                    contract.CompulsorySavings.Events.FirstOrDefault(
                                        e => e.LoanEventId == cancelledEvent.Id);
                                if (savingUnblockEvent != null)
                                {
                                    _savingEventManager.DeleteSavingsEventByLoanEventId(
                                        cancelledEvent.ParentId ?? cancelledEvent.Id, sqlTransaction);
                                    savingUnblockEvent.Deleted = true;
                                }
                            }
                        }
                    }

                    if (!contract.WrittenOff && !contract.AllInstallmentsRepaid)
                    {
                        contract.Closed = false;

                        if (evnt is LoanDisbursmentEvent)
                            contract.ContractStatus = OContractStatus.Validated;
                        else
                        {
                            contract.ContractStatus = evnt is LoanValidationEvent
                                                          ? OContractStatus.Pending
                                                          : OContractStatus.Active;
                        }

                    }
                    //come back after write off
                    if (evnt is WriteOffEvent)
                    {
                        contract.WrittenOff = false;
                        contract.ContractStatus = OContractStatus.Active;
                        CreditInsuranceEvent lciw = contract.GetNotDeletedInsuranceWriteOff();
                        if (lciw != null)
                        {
                            _ePs.CancelFireEvent(lciw, sqlTransaction, contract, contract.Product.Currency.Id);
                            lciw.Deleted = true;
                        }


                    }

                    _loanManager.UpdateLoan(contract, sqlTransaction);

                    FundingLineEvent flFundingLineEvent;

                    if (cancelledEvent is LoanDisbursmentEvent)
                    {
                        if (contract.HasCompulsoryAmount())
                        {
                            _savingEventManager.DeleteSavingsEventByLoanEventId(
                                cancelledEvent.ParentId ?? cancelledEvent.Id, sqlTransaction);
                            SavingBlockCompulsarySavingsEvent savingBlockEvent =
                                contract.CompulsorySavings.GetBlockCompulsorySavingEvent();
                            savingBlockEvent.Deleted = true;
                        }


                        LoanDisbursmentEvent temp = (LoanDisbursmentEvent) cancelledEvent;
                        flFundingLineEvent = new FundingLineEvent
                            {
                                Code = String.Concat("DE_", contract.Code),
                                Type = OFundingLineEventTypes.Disbursment,
                                Amount = temp.Amount,
                                Movement = OBookingDirections.Debit,
                                CreationDate = TimeProvider.Now,
                                FundingLine = contract.FundingLine
                            };
                        DeleteFundingLineEvent(ref contract, flFundingLineEvent, sqlTransaction);
                        _clientManager.DecrementLoanCycleByContractId(contract.Id, sqlTransaction);
                        // delete entry fee events
                        foreach (Event contractEvent in contract.Events)
                        {
                            if (contractEvent.Deleted)
                                continue;
                            if (contractEvent is LoanEntryFeeEvent)
                            {
                                _ePs.CancelFireEvent(contractEvent, sqlTransaction, contract,
                                                     contract.Product.Currency.Id);
                                contractEvent.Deleted = true;
                            }
                            if (contractEvent is CreditInsuranceEvent)
                            {
                                _ePs.CancelFireEvent(contractEvent, sqlTransaction, contract,
                                                     contract.Product.Currency.Id);
                                contractEvent.Deleted = true;
                            }
                        }

                        if (evnt.ClientType == OClientTypes.Person)
                        {
                            if (_econimcActivityServices.EconomicActivityLoanHistoryExists(contract.Id, pClient.Id,
                                                                                           sqlTransaction))
                                _econimcActivityServices.UpdateDeletedEconomicActivityLoanHistory(contract.Id,
                                                                                                  pClient.Id,
                                                                                                  ((Person) pClient)
                                                                                                      .Activity.Id,
                                                                                                  sqlTransaction, true);
                        }
                        else if (evnt.ClientType == OClientTypes.Group)
                        {
                            foreach (Member member in ((Group) pClient).Members)
                            {
                                if (_econimcActivityServices.EconomicActivityLoanHistoryExists(contract.Id,
                                                                                               member.Tiers.Id,
                                                                                               sqlTransaction))
                                    _econimcActivityServices.UpdateDeletedEconomicActivityLoanHistory(contract.Id,
                                                                                                      member.Tiers.Id,
                                                                                                      ((Person)
                                                                                                       member.Tiers)
                                                                                                          .Activity.Id,
                                                                                                      sqlTransaction,
                                                                                                      true);
                            }
                        }
                        else if (evnt.ClientType == OClientTypes.Corporate)
                        {
                            if (_econimcActivityServices.EconomicActivityLoanHistoryExists(contract.Id, pClient.Id,
                                                                                           sqlTransaction))
                                _econimcActivityServices.UpdateDeletedEconomicActivityLoanHistory(contract.Id,
                                                                                                  pClient.Id,
                                                                                                  ((Corporate) pClient)
                                                                                                      .Activity.Id,
                                                                                                  sqlTransaction, true);
                        }

                    }
                    else if (cancelledEvent is RepaymentEvent)
                    {
                        RepaymentEvent temp = (RepaymentEvent) cancelledEvent;
                        decimal amountCalc = (temp.Principal.HasValue ? temp.Principal.Value : 0) +
                                             (ApplicationSettings.GetInstance(_user != null ? _user.Md5 : "").
                                                                  InterestsCreditedInFL
                                                  ? ((temp.Interests.HasValue ? temp.Interests.Value : 0)
                                                     + (temp.Penalties.HasValue ? temp.Penalties.Value : 0))
                                                  : 0);

                        if (amountCalc > 0 ||
                            ApplicationSettings.GetInstance(_user != null ? _user.Md5 : "").InterestsCreditedInFL)
                        {
                            flFundingLineEvent = new FundingLineEvent
                                {
                                    Code = String.Concat("RE_", contract.Code, "_INS_", temp.InstallmentNumber),
                                    Type = OFundingLineEventTypes.Repay,
                                    Amount = amountCalc,
                                    CreationDate = TimeProvider.Now,
                                    FundingLine =
                                        _fundingLineServices.SelectFundingLineById(contract.FundingLine.Id,
                                                                                   sqlTransaction)
                                };

                            //temporary line to check whether funding line has enough amount to debit repayment event
                            flFundingLineEvent.Movement = OBookingDirections.Debit;
                            _fundingLineServices.ApplyRulesAmountEventFundingLine(flFundingLineEvent);
                            flFundingLineEvent.Movement = OBookingDirections.Credit;
                            DeleteFundingLineEvent(ref contract, flFundingLineEvent, sqlTransaction);
                        }
                    }

                    CancelSavingsEvent(cancelledEvent, sqlTransaction);
                    sqlTransaction.Commit();
                    sqlTransaction.Dispose();
                    SetClientStatus(contract, pClient);
                }
                catch (Exception)
                {
                    sqlTransaction.Rollback();
                    throw;
                }

                return cancelledEvent;
            }
        }

        private void CancelSavingsEvent(Event cancelledEvent, SqlTransaction sqlTransaction)
        {
            if (cancelledEvent.PaymentMethod != null)
                if (cancelledEvent.PaymentMethod.Method == OPaymentMethods.Savings
                    &&
                    (cancelledEvent.Code == "RBLE" ||
                     cancelledEvent.Code == "RGLE" ||
                     cancelledEvent.Code == "APR" ||
                     cancelledEvent.Code == "ATR" ||
                     cancelledEvent.Code == "RRLE" ||
                     cancelledEvent.Code == "APTR"
                    )
                    )
                {
                    int loanEventId = cancelledEvent.ParentId ?? cancelledEvent.Id;
                    _savingServices.DeleteRepaymentFromSavingEvent(loanEventId, sqlTransaction);
                }
        }

        private void UpdateGroupMembersStatuses(Loan contract)
        {
            var group = (Group) contract.Project.Client;
            foreach (var member in group.Members)
            {
                List<Loan> loansOfMember = _loanManager.SelectLoansByClientId(member.Tiers.Id);
                IClient memberOfgroup = _clientManager.SelectClientById(member.Tiers.Id);
                memberOfgroup.ActiveLoans = new List<Loan>();

                foreach (var loan in loansOfMember)
                {
                    if (loan.ContractStatus == OContractStatus.Active ||
                        loan.ContractStatus == OContractStatus.Validated ||
                        loan.PendingOrPostponed())
                        memberOfgroup.ActiveLoans.Add(loan);
                }
                using (SqlConnection conn = _clientManager.GetConnection())
                using (SqlTransaction sqlTransaction = conn.BeginTransaction())
                {
                    try
                    {
                        if (memberOfgroup.ActiveLoans.Count == 0)
                        {
                            memberOfgroup.Active = false;
                            memberOfgroup.Status = OClientStatus.Inactive;

                            _clientManager.UpdateClientStatus(memberOfgroup, sqlTransaction);
                        }
                        else
                        {
                            memberOfgroup.Active = true;
                            memberOfgroup.Status = OClientStatus.Active;
                            _clientManager.UpdateClientStatus(memberOfgroup, sqlTransaction);
                        }
                        sqlTransaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        sqlTransaction.Rollback();
                        throw ex;
                    }
                }
            }
        }

        public Loan UpdateContractStatus(Loan credit, Project project, IClient client, bool undoValidation)
        {
            CheckOperationDate(credit.CreditCommiteeDate.Value);
            using (SqlConnection conn = _loanManager.GetConnection())
            using (SqlTransaction sqlTransaction = conn.BeginTransaction())
            {


                try
                {
                    if (credit.StartDate.Date < ((DateTime) credit.CreditCommiteeDate).Date)
                        throw new OpenCbsContractSaveException(
                            OpenCbsContractSaveExceptionEnum.LoanWasValidatedLaterThanDisbursed);

                    Loan tempLoan = credit.Copy();

                    LoanValidationEvent lve = null;
                    if (undoValidation)
                    {
                        if (tempLoan.Events.GetLastLoanNonDeletedEvent != null &&
                            tempLoan.Events.GetLastLoanNonDeletedEvent is LoanValidationEvent)
                        {
                            ((LoanValidationEvent) tempLoan.Events.GetLastLoanNonDeletedEvent).Amount = credit.Amount;
                            _ePs.CancelFireEvent(tempLoan.Events.GetLastLoanNonDeletedEvent, sqlTransaction, tempLoan,
                                                 tempLoan.Product.Currency.Id);
                            tempLoan.Events.GetLastLoanNonDeletedEvent.Deleted = true;
                        }
                    }
                    else if (tempLoan.ContractStatus == OContractStatus.Validated)
                    {
                        lve = new LoanValidationEvent();
                        lve.Amount = tempLoan.Amount;
                        lve.Date = tempLoan.CreditCommiteeDate.Value;
                        lve.Cancelable = true;
                        lve.User = _user;
                        lve.Deleted = false;

                        if (Teller.CurrentTeller != null && Teller.CurrentTeller.Id != 0)
                            lve.TellerId = Teller.CurrentTeller.Id;

                        _ePs.FireEvent(lve, tempLoan, sqlTransaction);
                        tempLoan.Events.Add(lve);
                    }
                    _loanManager.UpdateLoanStatus(tempLoan, sqlTransaction);
                    project.SetStatus();
                    new ProjectServices(_user).UpdateProjectStatus(project, sqlTransaction);
                    client.SetStatus();
                    new ClientServices(_user).UpdateClientStatus(client, sqlTransaction);

                    FundingLineEvent pendingFundingLineEvent = new FundingLineEvent
                        {
                            Code =
                                string.Concat(
                                    OFundingLineEventTypes.Commitment.
                                                           ToString(), "/", tempLoan.Code),
                            Type = OFundingLineEventTypes.Commitment,
                            FundingLine =
                                _fundingLineServices.SelectFundingLineById(
                                    tempLoan.FundingLine.Id, sqlTransaction),
                            Movement = OBookingDirections.Debit,
                            CreationDate = TimeProvider.Now,
                            Amount = tempLoan.Amount
                            ,
                            AttachTo = lve
                        };
                    //if this is a new validate event, register it, otherwise delete previous validate event
                    if (tempLoan.ContractStatus == OContractStatus.Validated)
                        tempLoan.FundingLine.AddEvent(_fundingLineServices.AddFundingLineEvent(pendingFundingLineEvent,
                                                                                               sqlTransaction));
                    else if (undoValidation)
                    {
                        DeleteFundingLineEvent(ref tempLoan, pendingFundingLineEvent, sqlTransaction);
                    }
                    sqlTransaction.Commit();
                    return tempLoan;
                }
                catch (Exception ex)
                {
                    sqlTransaction.Rollback();
                    throw ex;
                }
            }
        }

        private void DeleteFundingLineEvent(ref Loan contract, FundingLineEvent flFundingLineEvent,
                                            SqlTransaction sqlTransaction)
        {
            if (flFundingLineEvent != null)
            {
                flFundingLineEvent.Id = _fundingLineServices.SelectFundingLineEventId(flFundingLineEvent, sqlTransaction);

                if (flFundingLineEvent.Id > 0)
                {
                    _fundingLineServices.DeleteFundingLineEvent(flFundingLineEvent, sqlTransaction);
                    contract.FundingLine.RemoveEvent(flFundingLineEvent);
                }
            }
        }

        public void UpdateTiersStatus(Loan credit, IClient client)
        {
            using (SqlConnection conn = _clientManager.GetConnection())
            using (SqlTransaction sqlTransact = conn.BeginTransaction())
            {
                try
                {
                    client.Active = false;
                    _clientManager.UpdateClientStatus(client, sqlTransact);
                    sqlTransact.Commit();
                }
                catch (Exception)
                {
                    sqlTransact.Rollback();
                    throw;
                }
            }
        }

        public void UpdateLoanShares(Loan pLoan, int pGroupId)
        {
            using (SqlConnection conn = _loanManager.GetConnection())
            using (SqlTransaction sqlTransaction = conn.BeginTransaction())
            {
                try
                {
                    foreach (LoanShare ls in pLoan.LoanShares)
                    {
                        _loanManager.UpdateLoanShare(ls, pLoan.Id, pGroupId, sqlTransaction);
                    }
                    sqlTransaction.Commit();
                }
                catch
                {
                    sqlTransaction.Rollback();
                    throw;
                }
            }
        }

        public void DeleteLoanShareAmountWhereNotDisbursed(int groupId)
        {
            _loanManager.DeleteLoanShareAmountWhereNotDisbursed(groupId);
        }

        public List<CreditSearchResult> FindContracts(int pCurrentlyPage, out int pNumbersTotalPage,
                                                      out int pNumberOfRecords, string pQuery)
        {
            List<CreditSearchResult> list = _loanManager.SearchCreditContractByCriteres(pCurrentlyPage, pQuery,
                                                                                        out pNumberOfRecords);
            UserServices us = ServicesProvider.GetInstance().GetUserServices();
            foreach (CreditSearchResult sr in list)
                sr.LoanOfficer = us.Find(sr.LoanOfficer.Id);

            pNumbersTotalPage = (pNumberOfRecords/20) + 1;
            return list;
        }

        public List<DateTime> FindInstallmentDatesForVillageActiveContracts(int villageId)
        {
            return _loanManager.SelectInstallmentDatesForVillageActiveContracts(villageId);
        }

        public List<VillageAttendee> FindMeetingAttendees(int villageId, DateTime date)
        {
            return _loanManager.SelectMeetingAttendees(villageId, date);
        }

        public void UpdateMeetingAttendees(VillageAttendee attendee)
        {
            _loanManager.UpdateMeetingAttendees(attendee);
        }

        public List<Loan> FindActiveContracts(int pClientId)
        {
            _productServices = ServicesProvider.GetInstance().GetProductServices();
            List<Loan> loans = _loanManager.SelectActiveLoans(pClientId);
            // should be reviewed this part of code and moved to correct place, ??????????
            foreach (var loan in loans)
            {
                if (loan.Id != 0)
                {
                    loan.LoanEntryFeesList = _loanManager.SelectInstalledLoanEntryFees(loan.Id);
                    foreach (LoanEntryFee fee in loan.LoanEntryFeesList)
                    {
                        fee.ProductEntryFee = _productServices.GetEntryFeeById(fee.ProductEntryFeeId);
                    }
                }
            }
            //////////////////////////////////////////////////////////////////////////////////
            return loans;
        }

        public DateTime EventsExportedDate(int eventId, SqlTransaction sqlTransaction)
        {
            AccountingTransaction e = _accountingServices.FindMovementSet(eventId, sqlTransaction);
            if (null == e) return DateTime.Today;

            return e.ExportedDate;
        }

        private static void _CheckLoanFilling(Loan pLoan, IClient pClient)
        {
            if (pLoan.Amount == 0)
                throw new OpenCbsContractSaveException(OpenCbsContractSaveExceptionEnum.AmountIsNull);
            if (pLoan.InterestRate == -1)
                throw new OpenCbsContractSaveException(OpenCbsContractSaveExceptionEnum.InterestRateIsNull);
            if (pLoan.NbOfInstallments == 0)
                throw new OpenCbsContractSaveException(OpenCbsContractSaveExceptionEnum.NumberOfInstallmentIsNull);
            if (pLoan.InstallmentType == null)
                throw new OpenCbsContractSaveException(OpenCbsContractSaveExceptionEnum.InstallmentTypeIsNull);
            if (pLoan.AnticipatedTotalRepaymentPenalties == -1)
                throw new OpenCbsContractSaveException(
                    OpenCbsContractSaveExceptionEnum.AnticipatedRepaymentPenaltiesIsNull);
            if (pLoan.NonRepaymentPenalties.InitialAmount == -1)
                throw new OpenCbsContractSaveException(OpenCbsContractSaveExceptionEnum.NonRepaymentPenaltiesIsNull);
            if (pLoan.NonRepaymentPenalties.OLB == -1)
                throw new OpenCbsContractSaveException(OpenCbsContractSaveExceptionEnum.NonRepaymentPenaltiesIsNull);
            if (pLoan.NonRepaymentPenalties.OverDueInterest == -1)
                throw new OpenCbsContractSaveException(OpenCbsContractSaveExceptionEnum.NonRepaymentPenaltiesIsNull);
            if (pLoan.NonRepaymentPenalties.OverDuePrincipal == -1)
                throw new OpenCbsContractSaveException(OpenCbsContractSaveExceptionEnum.NonRepaymentPenaltiesIsNull);
            if (!pLoan.GracePeriod.HasValue)
                throw new OpenCbsContractSaveException(OpenCbsContractSaveExceptionEnum.GracePeriodIsNull);
            if (pLoan.LoanEntryFeesList == null)
                throw new OpenCbsContractSaveException(OpenCbsContractSaveExceptionEnum.EntryFeesIsNull);
            if (pLoan.LoanOfficer == null)
                throw new OpenCbsContractSaveException(OpenCbsContractSaveExceptionEnum.LoanOfficerIsNull);
            if (pLoan.FundingLine == null)
                throw new OpenCbsContractSaveException(OpenCbsContractSaveExceptionEnum.FundingLineIsNull);
            if (!pLoan.FundingLine.Currency.Equals(pLoan.Product.Currency))
                throw new OpenCbsContractSaveException(OpenCbsContractSaveExceptionEnum.CurrencyMisMatch);
            if (pClient != null)
                if (pClient.BadClient)
                    throw new OpenCbsContractSaveException(OpenCbsContractSaveExceptionEnum.BeneficiaryIsBad);
            if (pLoan.EconomicActivityId == 0)
                throw new OpenCbsContractSaveException(OpenCbsContractSaveExceptionEnum.EconomicActivityNotSet);
        }

        private void AddLoan(ref Loan pLoan, int pProjectId, ref IClient pClient, SqlTransaction transaction)
        {
            if (pProjectId == 0)
                throw new OpenCbsContractSaveException(OpenCbsContractSaveExceptionEnum.ProjectIsNull);

            ApplicationSettings appSettings = ApplicationSettings.GetInstance(_user.Md5);

            if (!appSettings.IsAllowMultipleLoans && pClient.Active)
                throw new OpenCbsContractSaveException(OpenCbsContractSaveExceptionEnum.BeneficiaryIsActive);

            Loan clonedLoan = pLoan.Copy();
            IClient clonedClient = pClient.Copy();
            try
            {
                string branchCode = pClient.Branch.Code;
                if (String.IsNullOrEmpty(branchCode))
                    branchCode = _branchService.FindBranchCodeByClientId(pClient.Id, transaction);

                pLoan.ContractStatus = OContractStatus.Pending;
                pLoan.CreationDate = pLoan.StartDate;
                pLoan.CloseDate = (pLoan.InstallmentList[pLoan.InstallmentList.Count - 1]).ExpectedDate;
                pLoan.BranchCode = branchCode;

                pLoan.Id = AddLoanInDatabase(pLoan, pProjectId, pClient, transaction);
                pLoan.Code = GenerateContractCode(pClient, pLoan, transaction);
                _loanManager.UpdateContractCode(pLoan.Id, pLoan.Code, transaction);
            }
            catch
            {
                pLoan = clonedLoan;
                pClient = clonedClient;
                throw;
            }
        }

        public List<LoanShare> GetLoanShares(int pContractId)
        {
            return _loanManager.GetLoanShareAmount(pContractId);
        }

        public void SaveSchedule(List<Installment> pInstallments, Loan pLoan)
        {
            using (SqlConnection conn = _loanManager.GetConnection())
            using (SqlTransaction sqlTransaction = conn.BeginTransaction())
            {
                try
                {
                    foreach (Installment installment in pInstallments)
                    {
                        _instalmentManager.UpdateInstallment(installment, pLoan.Id, 0, sqlTransaction);
                    }

                    _loanManager.UpdateLoan(pLoan, sqlTransaction);
                    sqlTransaction.Commit();
                }
                catch (Exception)
                {
                    if (sqlTransaction != null) sqlTransaction.Rollback();
                    throw;
                }
            }
        }

        private int AddLoanInDatabase(Loan pLoan, int pProjectId, IClient pClient, SqlTransaction pSqlTransaction)
        {
            int contractId = _loanManager.Add(pLoan, pProjectId, pSqlTransaction);
            pClient.Status = OClientStatus.Pending;
            _clientManager.UpdateClientStatus(pClient, pSqlTransaction);
            if (pClient is Group)
            {
                foreach (Member member in ((Group) pClient).Members)
                {
                    member.Tiers.Status = OClientStatus.Active;
                    _clientManager.UpdateClientStatus(member.Tiers, pSqlTransaction);
                }
            }
            return contractId;
        }

        private void UpdateInstalmentsInDatabase(Loan pLoan, SqlTransaction pSqlTransaction)
        {
            _instalmentManager.DeleteInstallments(pLoan.Id, pSqlTransaction);
            _instalmentManager.AddInstallments(pLoan.InstallmentList, pLoan.Id, pSqlTransaction);
        }

        private void UpdateLoanInDatabase(Loan pContract, SqlTransaction pSqlTransaction)
        {
            _loanManager.UpdateLoan(pContract, pSqlTransaction);

            if (pContract.LoanInitialOfficer != null)
                _loanManager.UpdateLoanLoanOfficer(pContract.Id, pContract.LoanOfficer.Id,
                                                   pContract.LoanInitialOfficer.Id, pSqlTransaction);

            //update Guarantor
            foreach (Guarantor guarantor in pContract.Guarantors)
            {
                guarantor.Tiers.Status = OClientStatus.Active;
                _clientManager.UpdateClientStatus(guarantor.Tiers, pSqlTransaction);
            }
        }

        private void UpdateLoan(ref Loan pLoan, SqlTransaction transaction)
        {
            UpdateInstalmentsInDatabase(pLoan, transaction);
            UpdateLoanInDatabase(pLoan, transaction);
        }

        public void ArchiveInstallments(Loan loan, Event e, SqlTransaction t)
        {
            foreach (Installment i in loan.InstallmentList)
            {
                _instalmentManager.ArchiveInstallment(i, loan.Id, e, t);
            }
        }

        public List<Installment> GetArchivedInstallments(int eventId)
        {
            using (SqlConnection conn = _instalmentManager.GetConnection())
            using (SqlTransaction sqlTransaction = conn.BeginTransaction())
            {
                try
                {
                    List<Installment> list = _instalmentManager.GetArchivedInstallments(eventId, sqlTransaction);
                    sqlTransaction.Commit();
                    return list;
                }

                catch (Exception)
                {
                    sqlTransaction.Rollback();
                    throw;
                }
            }
        }

        public void UnarchiveInstallments(Loan loan, Event e, SqlTransaction t)
        {
            _instalmentManager.UnarchiveInstallments(loan, e, t);
        }

        public Loan SelectLoan(int pLoanId,
                               bool pAddGeneralInformations,
                               bool pAddOptionalInformations,
                               bool pAddOptionalEventInformations)
        {
            return _loanManager.SelectLoan(pLoanId, pAddGeneralInformations, pAddOptionalInformations,
                                           pAddOptionalEventInformations);
        }

        public int SelectLoanId(string pLoanCode)
        {
            return _loanManager.SelectLoanID(pLoanCode);
        }

        public void CancelPendingInstallments(Loan pLoan)
        {
            Loan fakeLoan = pLoan.Copy();

            foreach (var installment in pLoan.InstallmentList.FindAll(item => item.IsPending))
            {
                var fakeInstallment = fakeLoan.InstallmentList.Find(item => item.Number == installment.Number);
                _instalmentManager.UpdateInstallment(fakeInstallment, pLoan.Id, null, null);
            }
        }

        private void LoadAlerts()
        {
            if (_alerts != null) return;

            DateTime now = TimeProvider.Now;
            int userId = User.CurrentUser.Id;
            _alerts = _loanManager.SelectAllAlerts(now, userId);

            UserServices us = ServicesProvider.GetInstance().GetUserServices();
            foreach (Alert_v2 alert in _alerts)
            {
                Debug.Assert(alert.LoanOfficer != null, "Loan officer is null");
                alert.LoanOfficer = us.Find(alert.LoanOfficer.Id);
            }
        }

        public List<Alert_v2> FindAlerts()
        {
            LoadAlerts();
            return _alerts;
        }

        public List<Alert_v2> FindAlerts(bool late, bool pending, bool posponed, bool overdraft, bool savingsPending,
                                         bool validated)
        {
            LoadAlerts();
            return _alerts.FindAll(
                i => i.IsPerformingLoan
                     || (late && i.IsLateLoan)
                     || (pending && i.IsLoan && i.Status == OContractStatus.Pending)
                     || (validated && i.IsLoan && i.Status == OContractStatus.Validated)
                     || (posponed && i.IsLoan && i.Status == OContractStatus.Postponed)
                     || (overdraft && i.IsSaving && i.Amount < 0)
                     || (savingsPending && i.IsSaving && i.Status == OContractStatus.Pending)
                // Pending savings! (not loan)

                );
        }

        public void ClearAlerts()
        {
            if (_alerts != null)
                _alerts = null;
        }

        public List<Loan> GetActiveLoans(int clientId)
        {
            var loans = _loanManager.SelectLoansByClientId(clientId);
            return loans.Where(loan => loan.ContractStatus == OContractStatus.Active).ToList();
        }

        public void WriteOff(Loan loan, DateTime onDate, int writeOffMethodId)
        {
            using (SqlConnection conn = _loanManager.GetConnection())
            using (SqlTransaction sqlTransaction = conn.BeginTransaction())
            {
                try
                {
                    WriteOffEvent writeOffEvent = loan.WriteOff(onDate);
                    writeOffEvent.User = User.CurrentUser;
                    writeOffEvent.WriteOffMethod = writeOffMethodId;

                    if (Teller.CurrentTeller != null && Teller.CurrentTeller.Id != 0)
                        writeOffEvent.TellerId = Teller.CurrentTeller.Id;

                    _ePs.FireEvent(writeOffEvent, loan, sqlTransaction);

                    //lciw ~ loan credit insurance write-off
                    foreach (Event item in loan.Events)
                    {
                        if (item is CreditInsuranceEvent)
                            if (item.Code == "LCIW" && item.Deleted == false)
                                _ePs.FireEvent(item, loan, sqlTransaction);
                    }

                    if (loan.Project.Client is Person)
                    {
                        loan.Project.Client.Active = false;
                        loan.Project.Client.Status = OClientStatus.Inactive;
                        _clientManager.UpdateClientStatus(loan.Project.Client, sqlTransaction);
                    }
                    UpdateLoan(ref loan, sqlTransaction);

                    CallInterceptor(new Dictionary<string, object>
                    {
                        {"Loan", loan},
                        {"Event", writeOffEvent},
                        {"SqlTransaction", sqlTransaction}
                    });

                    if (sqlTransaction != null)
                        sqlTransaction.Commit();

                    if (loan.Project.Client is Group)
                        UpdateGroupMembersStatuses(loan);

                }
                catch (Exception)
                {
                    if (sqlTransaction != null) sqlTransaction.Rollback();
                    throw;
                }
            }
        }

        /// <summary>
        /// This method determines if current user is allowed to
        /// perform a credit contract operation in the past
        /// </summary>
        public void PerformBackDateOperations()
        {
            return;
        }

        /// <summary>
        /// This method determines if current user is allowed to 
        /// perform a credit contract operation in the future
        /// </summary>
        public void PerformFutureDateOperations()
        {
            return;
        }

        public void ModifyDisbursementDate(DateTime date)
        {
            return;
        }

        public void ModifyGuarantorsCollaterals()
        {
            return;
        }

        public void ModifyFirstInstalmentDate(DateTime date)
        {
            return;
        }

        public List<int> SelectAllLoanIds()
        {
            return _loanManager.FindAllLoanIds();
        }

        public List<Loan> FindAllLoansOfClient(int pClientId)
        {
            return _loanManager.SelectAllLoansOfClient(pClientId);
        }

        public void WaiveFee(ref Loan credit, ref IClient client)
        {
            Event foundEvent = credit.GetLastNonDeletedEvent();

            if (foundEvent == null)
                throw new OpenCbsContractSaveException(OpenCbsContractSaveExceptionEnum.EventIsNull);
            if (!(foundEvent is RepaymentEvent))
                throw new OpenCbsContractSaveException(OpenCbsContractSaveExceptionEnum.WrongEvent);
            if (!foundEvent.Cancelable)
                throw new OpenCbsContractSaveException(OpenCbsContractSaveExceptionEnum.EventNotCancelable);
            if (((RepaymentEvent) foundEvent).Fees == 0)
                throw new OpenCbsContractSaveException(OpenCbsContractSaveExceptionEnum.ZeroFee);

            string fee = ((RepaymentEvent) foundEvent).Fees.GetFormatedValue(credit.UseCents);
            String comment = "FEE WAIVED [" + fee.Replace("�", string.Empty) + "]";
            //foundEvent.Comment = comment;
            ////EventProcessorServices eps = ServicesProvider.GetInstance().GetEventProcessorServices();
            ////eps.UpdateCommentForLoanEvent(foundEvent, null);

            CancelLastEvent(credit, client, comment);

            //update a loan for a client
            foreach (Project prj in client.Projects)
            {
                foreach (Loan loan in prj.Credits)
                {
                    if (loan.Code == credit.Code)
                    {
                        loan.Disbursed = credit.Disbursed;
                    }
                }
            }

            OCurrency amount = ((RepaymentEvent) foundEvent).Principal +
                               ((RepaymentEvent) foundEvent).Interests + ((RepaymentEvent) foundEvent).Fees;

            comment = "ID[" + foundEvent.Id + "] FEE WAIVED [" + fee.Replace("�", string.Empty) + "]";
            Loan l = Repay(credit,
                           client,
                           foundEvent.InstallmentNumber,
                           foundEvent.Date,
                           amount,
                           true,
                           0,
                           0,
                           false,
                           0,
                           true,
                           false,
                           foundEvent.PaymentMethod,
                           comment,
                           false);

            credit.Events = l.Events;
        }

        private bool IsDateWithinCurrentFiscalYear(DateTime date)
        {
            ChartOfAccountsServices coaService = ServicesProvider.GetInstance().GetChartOfAccountsServices();
            FiscalYear year =
                coaService.SelectFiscalYears()
                          .Find(y => date >= y.OpenDate && (y.CloseDate == null || date <= y.CloseDate));
            return null != year && year.Open;
        }

        private void CheckOperationDate(DateTime date)
        {
            if (!IsDateWithinCurrentFiscalYear(date))
                throw new OpenCbsContractSaveException(
                    OpenCbsContractSaveExceptionEnum.OperationOutsideCurrentFiscalYear);
        }

        private string GenerateContractCode(IClient client, Loan loan, SqlTransaction transaction)
        {
            // Find non-default implementation
            var generator = (
                                from gen in ContractCodeGenerators
                                where
                                    gen.Metadata.ContainsKey("Implementation") &&
                                    gen.Metadata["Implementation"].ToString() != "Default"
                                select gen.Value).FirstOrDefault();
            if (generator != null) return generator.GenerateContractCode(client, loan, transaction);

            // Otherwise, find the default one
            generator = (
                            from gen in ContractCodeGenerators
                            where
                                gen.Metadata.ContainsKey("Implementation") &&
                                gen.Metadata["Implementation"].ToString() == "Default"
                            select gen.Value).FirstOrDefault();
            if (generator != null) return generator.GenerateContractCode(client, loan, transaction);

            throw new ApplicationException("Cannot find contract code generator.");
        }

        public void CallInterceptor(IDictionary<string, object> interceptorParams)
        {
            // Find non-default implementation
            var creator = (
                                from item in EventInterceptors
                                where
                                    item.Metadata.ContainsKey("Implementation") &&
                                    item.Metadata["Implementation"].ToString() != "Default"
                                select item.Value).FirstOrDefault();
            if (creator != null)
            {
                creator.CallInterceptor(interceptorParams);
                return;
            }

            // Otherwise, find the default one
            creator = (
                            from item in EventInterceptors
                            where
                                item.Metadata.ContainsKey("Implementation") &&
                                item.Metadata["Implementation"].ToString() == "Default"
                            select item.Value).FirstOrDefault();
            if (creator != null) creator.CallInterceptor(interceptorParams);
        }

        public void ManualScheduleBeforeDisbursement()
        {
        }

        public void ManualScheduleAfterDisbursement()
        {
        }

        public void LoanPenaltyAccrual()
        {
            var launchDate = DateTime.Today;
            var listOfLoans = _loanManager.GetListOfLoansToAccruePenalty(launchDate);
            var em = new EventManager(User.CurrentUser);
            using (var connection=_loanManager.GetConnection())
                foreach (var item in listOfLoans)
                {
                    var date = item.Value;
                    while (date < launchDate)
                    {
                        date = date.AddDays(1);
                        var penaltyEvent = new LoanPenaltyAccrualEvent
                            {
                                Penalty = _loanManager.GetDailyPenaltyForLoan(item.Key, date),
                                Date = date,
                                User = User.CurrentUser,
                                ContracId = item.Key
                            };
                        using (var transaction = connection.BeginTransaction())
                            try
                            {
                                em.AddLoanEvent(penaltyEvent, penaltyEvent.ContracId, transaction);
                                transaction.Commit();
                            }
                            catch (Exception)
                            {
                                transaction.Rollback();
                                //throw;
                            }
                    }
                }
        }
        //public void LoanPenaltyAccrual2()
        //{
        //    var launchDate = DateTime.Today;
        //    var listOfLoans = _loanManager.GetListOfLoansToAccruePenalty1(launchDate);
        //    var em = new EventManager(User.CurrentUser);
        //    using (var connection = _loanManager.GetConnection())
        //        foreach (var item in listOfLoans)
        //        {
        //            var date = launchDate.AddDays(-1);
        //            while (date < launchDate)
        //            {
        //                date = date.AddDays(1);
        //                var penaltyEvent = new LoanPenaltyAccrualEvent
        //                {
        //                    Penalty = item.Value,
        //                    Date = date,
        //                    User = User.CurrentUser,
        //                    ContracId = item.Key
        //                };
        //                using (var transaction = connection.BeginTransaction())
        //                    try
        //                    {
        //                        em.AddLoanEvent(penaltyEvent, penaltyEvent.ContracId, transaction);
        //                        transaction.Commit();
        //                    }
        //                    catch (Exception)
        //                    {
        //                        transaction.Rollback();
        //                        //throw;
        //                    }
        //            }
        //        }
        //}
        public void LoanInterestAccrual()
        {
            var launchDate = DateTime.Today;
            var listOfLoans = _loanManager.GetListOfLoansToAccrueInterest(launchDate);
            var em = new EventManager(User.CurrentUser);
            using (var connection = _loanManager.GetConnection())
                foreach (var item in listOfLoans)
                {
                    var date = item.Value;
                    var loan = SelectLoan(item.Key, true, false, false);
                    while (date < launchDate)
                    {
                        date = date.AddDays(1);
                        var interest = GetDailyInterestForLoan(loan, date);
                        if (interest <= 0) continue;
                        var interestEvent = new LoanInterestAccrualEvent
                            {
                                Interest = interest,
                                Date = date,
                                User = User.CurrentUser,
                                ContracId = item.Key
                            };
                        using (var transaction = connection.BeginTransaction())
                            try
                            {
                                _ePs.FireEvent(interestEvent, loan, transaction);
                                CallInterceptor(new Dictionary<string, object>
                                {
                                    {"Loan", loan},
                                    {"Event", interestEvent},
                                    {"SqlTransaction", transaction}
                                });
                                transaction.Commit();
                            }
                            catch (Exception)
                            {
                                transaction.Rollback();
                                //throw;
                            }
                    }
                }
        }

        public void LoanInterestAccrual(DateTime launchDate)
        {
            var listOfLoans = _loanManager.GetListOfLoansToAccrueInterest(launchDate);
            var em = new EventManager(User.CurrentUser);
            using (var connection = _loanManager.GetConnection())
                foreach (var item in listOfLoans)
                {
                    var date = item.Value;
                    var loan = SelectLoan(item.Key, true, false, false);
                    while (date < launchDate)
                    {
                        date = date.AddDays(1);
                        var interest = GetDailyInterestForLoan(loan, date);
                        if (interest <= 0) continue;
                        var interestEvent = new LoanInterestAccrualEvent
                        {
                            Interest = interest,
                            Date = date,
                            User = User.CurrentUser,
                            ContracId = item.Key
                        };
                        using (var transaction = connection.BeginTransaction())
                            try
                            {
                                _ePs.FireEvent(interestEvent, loan, transaction);
                                CallInterceptor(new Dictionary<string, object>
                                {
                                    {"Loan", loan},
                                    {"Event", interestEvent},
                                    {"SqlTransaction", transaction}
                                });
                                transaction.Commit();
                            }
                            catch (Exception)
                            {
                                transaction.Rollback();
                                //throw;
                            }
                    }
                }
        }

        private decimal GetDailyInterestForLoan(Loan loan, DateTime date)
        {
            if (date >= loan.LastInstallment.ExpectedDate) return 0;
            var nextInstallment = loan.InstallmentList.First(installment => date < installment.ExpectedDate);
            var nextDate = nextInstallment.ExpectedDate.AddDays(-1);
            var previousDate = nextInstallment.Number > 1
                                   ? loan.InstallmentList[nextInstallment.Number - 2].ExpectedDate
                                   : loan.StartDate;
            if ((date - previousDate).Days > 30) return 0;
            var interest = date == DateTime.Today
                               ? nextInstallment.InterestsRepayment
                               : _loanManager.GetInstallmentInterest(loan.Id, nextInstallment.Number, date);
            if (nextDate == date || (date - previousDate).Days == 30)
                interest = interest - _loanManager.GetSumOfAccruedInterests(loan.Id, previousDate, date);
            else
                interest = interest / 30;
            return interest.Value;
        }

        public void GenerateLoanTransitionEvent()
        {
            var em = new EventManager(User.CurrentUser);
            var date = _loanManager.LastLoanTransitionEventDate();

            while (date.Date < DateTime.Now.Date)
            {
                date = date.AddDays(1);
                using (var connection = _loanManager.GetConnection())
                using (var transaction = connection.BeginTransaction())
                    try
                    {
                        var glll = _loanManager.GetListOfTransitionToLateLoan(date);
                        var transitionEventList = glll.Select(item => new LoanTransitionEvent
                            {
                                Code = "GLLL",
                                Date = date,
                                User = User.CurrentUser,
                                Amount = item.Value,
                                ContracId = item.Key
                            }).ToList();

                        //var llgl = _loanManager.GetListOfTransitionToGoodLoan(date);
                        //transitionEventList.AddRange(llgl.Select(item => new LoanTransitionEvent
                        //    {
                        //        Code = "LLGL",
                        //        Date = date,
                        //        User = User.CurrentUser,
                        //        Amount = item.Value,
                        //        ContracId = item.Key
                        //    }));

                        foreach (var transitionEvent in transitionEventList)
                            em.AddLoanEvent(transitionEvent, transitionEvent.ContracId, transaction);

                        transaction.Commit();
                    }
                    catch (Exception)
                    {
                        transaction.Rollback();
                        throw;
                    }
            }
        }

        public Loan RepayLoanFromTransitAccount(RepaymentConfiguration config)
        {
            var currentUser = User.CurrentUser ?? ServicesProvider.GetInstance().GetUserServices().Find(1);
            var balance = config.Saving.GetBalance().Value;
            var amount = config.Loan.CalculateOverduePrincipal(config.Date).Value +
                         config.Loan.GetUnpaidInterest(config.Date).Value +
                         config.Loan.GetUnpaidLatePenalties(config.Date);
            if (amount > balance || !config.KeepExpectedInstallment)
                amount = balance;
            if (amount <= 0) return config.Loan;
            ServicesProvider.GetInstance()
                            .GetSavingServices()
                            .Withdraw(config.Saving, TimeProvider.Today, amount,
                                      "Withdraw for loan repayment " + config.Loan.Code, currentUser, new Teller());
            var paymentMethod =
                ServicesProvider.GetInstance().GetPaymentMethodServices().GetPaymentMethodByName("Savings");
            var installment = config.Loan.GetFirstUnpaidInstallment() ?? config.Loan.InstallmentList.First();
            return Repay(config.Loan,
                         config.Client,
                         installment.Number,
                         config.Date.Date,
                         amount,
                         config.DisableFees,
                         config.ManualFeesAmount,
                         config.ManualCommission,
                         config.DisableInterests,
                         config.ManualInterestsAmount,
                         config.KeepExpectedInstallment,
                         config.ProportionPayment,
                         paymentMethod,
                         config.Saving.Events.Last().Id.ToString(CultureInfo.InvariantCulture),
                         config.IsPending);
        }

        public void RepayAllExpectedInstallments(DateTime date)
        {
            var loanIdList = _loanManager.GetLoanIdForRepayment(date);
            foreach (var id in loanIdList)
            {
                var client = ServicesProvider.GetInstance()
                                             .GetClientServices()
                                             .FindTiersByContractId(id);
                if (!(from item in client.Savings where item.Product.Code == "default" select item).Any())
                    continue;
                var repaymentConfiguration = new RepaymentConfiguration
                {
                    Loan = SelectLoan(id, true, true, false),
                    Client = client,
                    Saving = (from item in client.Savings where item.Product.Code == "default" select item).First(),
                    KeepExpectedInstallment = !false,
                    Date = date,
                    DisableFees = false,
                    ManualFeesAmount = 0,
                    DisableInterests = false,
                    ManualInterestsAmount = 0,
                    ManualCommission = 0,
                    ProportionPayment = false,
                    IsPending = false
                };
                RepayLoanFromTransitAccount(repaymentConfiguration);
            }
        }

        public DateTime GetRepaymentModuleLastStartupDate()
        {
            var repaymentDate = _loanManager.GetRepaymentModuleLastStartupDate();
            if (repaymentDate != "")
                return Convert.ToDateTime(repaymentDate);
            _loanManager.CreateRepaymentModuleLastStartupDateRecord();
            return DateTime.Today;
        }

        public void SetRepaymentModuleLastStartupDate(DateTime date)
        {
            _loanManager.SetRepaymentModuleLastStartupDate(date);
        }
    }
}
