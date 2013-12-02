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
using System.Diagnostics;
using System.Linq;
using OpenCBS.CoreDomain.Accounting;
using OpenCBS.CoreDomain.Clients;
using OpenCBS.CoreDomain.Contracts.Collaterals;
using OpenCBS.CoreDomain.Contracts.Loans.CalculateInstallments;
using OpenCBS.CoreDomain.Contracts.Loans.Installments;
using OpenCBS.CoreDomain.Contracts.Loans.LoanRepayment;
using OpenCBS.CoreDomain.Contracts.Loans.LoanRepayment.Repayment.RepayLateInstallments;
using OpenCBS.CoreDomain.Contracts.Savings;
using OpenCBS.CoreDomain.EconomicActivities;
using OpenCBS.CoreDomain.Events;
using OpenCBS.CoreDomain.Events.Loan;
using OpenCBS.CoreDomain.Events.Saving;
using OpenCBS.CoreDomain.FundingLines;
using OpenCBS.CoreDomain.Products;
using OpenCBS.Enums;
using OpenCBS.ExceptionsHandler;
using OpenCBS.Shared;
using OpenCBS.Shared.Settings;

namespace OpenCBS.CoreDomain.Contracts.Loans
{
    [Serializable]
    public class Loan : IContract, IDisposable
    {
        public static OCurrency InitialOlbOfContractBeforeRescheduling;
        private string _branchCode;
        private DateTime _startDate;
        private DateTime _alignAlignDisbursementDate;
        
        private DateTime _firstInstallmentDate;

        private List<Guarantor> _guarantors;
        private List<ContractCollateral> _collaterals;
        private EventStock _events;

        private OClientTypes _clientType;
        private OCurrency _amount;
        private decimal _interestRate;
        private int _nbOfInstallments;
        private InstallmentType _installmentType;
        public string CreditCommiteeComment { get; set; }
        private LoanProduct _product;
        private List<Installment> _installmentList;

        private bool _rescheduled;
        public int? NsgID { get; set; }
    
        private bool _disbursed;
        private bool _writtenOff;
        private bool _badLoan;
        private bool _closed;
        private ContractChartOfAccounts _chartOfAccounts;
        public int? GracePeriodOfLateFees { get; set; }
        
        private readonly User _user;
        private readonly ApplicationSettings _generalSettings;
        private readonly NonWorkingDateSingleton _nwdS;
        public DateTime? CreditCommiteeDate { get; set; }
        private List<LoanShare> _loanShares = new List<LoanShare>();

        public int Id { get; set; }
        public int EconomicActivityId { get; set; }
        private EconomicActivity _economicActivity;
        public EconomicActivity EconomicActivity
        {
            get { return _economicActivity; }
            set
            {
                _economicActivity = value;
                EconomicActivityId = _economicActivity == null ? 0 :_economicActivity.Id;
            }
        }

        public string Code { get; set; }
        public Project Project { get; set; }
        public Member EscapedMember { get; set; }

        public SavingBookContract CompulsorySavings { get; set; }
        public int? CompulsorySavingsPercentage { get; set; }

        public int? DrawingsNumber { get; set; }
        public OCurrency AmountUnderLoc { get; set; }
        public int? MaturityLoc { get; set; }
        public bool ScheduleChangedManually { get; set; }

        public OCurrency AmountMin { get; set; }
        public OCurrency AmountMax { get; set; }

        public decimal? InterestRateMin { get; set; }
        public decimal? InterestRateMax { get; set; }

        public int? NmbOfInstallmentsMin { get; set; }
        public int? NmbOfInstallmentsMax { get; set; }

        public int? LoanCycle { get; set; }

        public OAnticipatedRepaymentPenaltiesBases AnticipatedTotalRepaymentPenaltiesBase { get; set; }
        public OAnticipatedRepaymentPenaltiesBases AnticipatedPartialRepaymentPenaltiesBase { get; set; }

        public List<TrancheEvent> GivenTranches = new List<TrancheEvent>();

        public Loan()
        {
        }
        /// <summary>
        /// Instancy a new CreditContract. Use it if installments need'nt calculate 
        /// </summary>
        /// <param name="pUser"></param>
        /// <param name="pGeneralSettings"></param>
        /// <param name="pNwds"></param>
        /// <param name="pPt"></param>
        /// <param name="pChartOfAccounts"></param>
        public Loan(User pUser, ApplicationSettings pGeneralSettings, NonWorkingDateSingleton pNwds, ProvisionTable pPt, ChartOfAccounts pChartOfAccounts)
        {
            _user = pUser;
            _generalSettings = pGeneralSettings;
            _nwdS = pNwds;

            NonRepaymentPenalties = new NonRepaymentPenalties();
            _installmentList = new List<Installment>();
            _guarantors = new List<Guarantor>();
            _collaterals = new List<ContractCollateral>();
            _events = new EventStock();
        }

        public OCurrency GetSumOfFees()
        {
            if (LoanEntryFeesList == null) return 0;
            OCurrency sum = 0;
            foreach (LoanEntryFee fee in LoanEntryFeesList)
            {
                if (fee.ProductEntryFee.IsRate)
                    sum += _amount*fee.FeeValue/100.00;
                else
                    sum += fee.FeeValue;
            }
            return sum;
        }


        public OCurrency GetSumOfFees(OCurrency loanAmount)
        {
            if (LoanEntryFeesList == null) return 0;
            OCurrency sum = 0;
            foreach (LoanEntryFee fee in LoanEntryFeesList)
            {
                if (fee.ProductEntryFee.IsRate)
                    sum += loanAmount * fee.FeeValue / 100.00;
                else
                    sum += fee.FeeValue;
            }
            return sum;
        }

        /// <summary>
        /// Instancy a new CreditContract. Installments are directly calculated
        /// </summary>
        /// <param name="pAckage"></param>
        /// <param name="pAmount"></param>
        /// <param name="pInterestRate"></param>
        /// <param name="pNbOfInstallments"></param>
        /// <param name="pGracePeriod"></param>
        /// <param name="pStartDate"></param>
        /// <param name="pUser"></param>
        /// <param name="pGeneralSettings"></param>
        /// <param name="pNwds"></param>
        /// <param name="pPt"></param>
        /// <param name="pChartOfAccounts"></param>
        public Loan(LoanProduct pAckage, OCurrency pAmount, decimal pInterestRate, int pNbOfInstallments, int pGracePeriod, DateTime pStartDate, User pUser, ApplicationSettings pGeneralSettings, NonWorkingDateSingleton pNwds, ProvisionTable pPt, ChartOfAccounts pChartOfAccounts)
        {
            _user = pUser;
            _generalSettings = pGeneralSettings;
            _nwdS = pNwds;
            Product = pAckage;

            NonRepaymentPenalties = new NonRepaymentPenalties();
            _events = new EventStock();
            _guarantors = new List<Guarantor>();
            _collaterals = new List<ContractCollateral>();
            _installmentType = pAckage.InstallmentType;
            _amount = pAmount;
            _interestRate = pInterestRate;
            _nbOfInstallments = pNbOfInstallments;
            GracePeriod = pGracePeriod;
            GracePeriodOfLateFees = pAckage.GracePeriodOfLateFees;
            CreationDate = pStartDate;
            _startDate = pStartDate;

            _firstInstallmentDate = CalculateInstallmentDate(pStartDate, 1);
            _alignAlignDisbursementDate = pStartDate;

            //with this constructor, installment are directly calculated when a new CreditContract is instanciated
            _installmentList = CalculateInstallments(true);
            CalculateStartDates();
        }

        public Loan(LoanProduct pAckage, OCurrency pAmount, decimal pInterestRate, int pNbOfInstallments, int pGracePeriod,
                      DateTime pStartDate, User pUser, ApplicationSettings pGeneralSettings, NonWorkingDateSingleton pNwds, 
                        ProvisionTable pPt, ContractChartOfAccounts pChartOfAccounts)
        {
            _user = pUser;
            _generalSettings = pGeneralSettings;
            _nwdS = pNwds;

            NonRepaymentPenalties = new NonRepaymentPenalties();
            _events = new EventStock();
            _guarantors = new List<Guarantor>();
            _collaterals = new List<ContractCollateral>();
            _chartOfAccounts = pChartOfAccounts;
            Product = pAckage;
            _installmentType = pAckage.InstallmentType;
            _amount = pAmount;
            _interestRate = pInterestRate;
            _nbOfInstallments = pNbOfInstallments;
            GracePeriod = pGracePeriod;
            GracePeriodOfLateFees = pAckage.GracePeriodOfLateFees;
            CreationDate = pStartDate;
            _startDate = pStartDate;

            _firstInstallmentDate = CalculateInstallmentDate(pStartDate, 1);
            _alignAlignDisbursementDate = pStartDate;

            //with this constructor, installment are directly calculated when a new CreditContract is instanciated
            _installmentList = CalculateInstallments(true);
            CalculateStartDates();
        }

        public Loan(LoanProduct pAckage, OCurrency pAmount, decimal pInterestRate, int pNbOfInstallments, int pGracePeriod,
                      DateTime pStartDate, DateTime pFirstInstallmentDate, User pUser, ApplicationSettings pGeneralSettings, 
                        NonWorkingDateSingleton pNwds, ProvisionTable pPt, ChartOfAccounts pChartOfAccounts)
        {
            _user = pUser;
            _generalSettings = pGeneralSettings;
            _nwdS = pNwds;
            Product = pAckage;

            NonRepaymentPenalties = new NonRepaymentPenalties();
            _events = new EventStock();
            _guarantors = new List<Guarantor>();
            _collaterals = new List<ContractCollateral>();
            _installmentType = pAckage.InstallmentType;
            _amount = pAmount;
            _interestRate = pInterestRate;
            _nbOfInstallments = pNbOfInstallments;
            GracePeriod = pGracePeriod;
            CreationDate = pStartDate;
            _startDate = pStartDate;

            _firstInstallmentDate = pFirstInstallmentDate;

            if (_firstInstallmentDate < _startDate)
                _firstInstallmentDate = _startDate;

            _alignAlignDisbursementDate = CalculateAlignDisbursementDate(_firstInstallmentDate);

            //with this constructor, installment are directly calculated when a new CreditContract is instanciated
            _installmentList = CalculateInstallments(true);
            CalculateStartDates();
        }

        public Loan(LoanProduct pAckage, OCurrency pAmount, decimal pInterestRate, int pNbOfInstallments, int pGracePeriod,
                      DateTime pStartDate, DayOfWeek? meetingDay, User pUser, ApplicationSettings pGeneralSettings,
                        NonWorkingDateSingleton pNwds, ProvisionTable pPt, ChartOfAccounts pChartOfAccounts)
        {
            _user = pUser;
            _generalSettings = pGeneralSettings;
            _nwdS = pNwds;
            Product = pAckage;

            NonRepaymentPenalties = new NonRepaymentPenalties();
            _events = new EventStock();
            _guarantors = new List<Guarantor>();
            _collaterals = new List<ContractCollateral>();
            _installmentType = pAckage.InstallmentType;
            _amount = pAmount;
            _interestRate = pInterestRate;
            _nbOfInstallments = pNbOfInstallments;
            GracePeriod = pGracePeriod;
            CreationDate = pStartDate;
            _startDate = pStartDate;

            _firstInstallmentDate = CalculateInstallmentDate(pStartDate, 1);
            if (meetingDay.HasValue)
                _firstInstallmentDate = GetMeetingDate(_firstInstallmentDate, meetingDay);
          
            _alignAlignDisbursementDate = CalculateAlignDisbursementDate(_firstInstallmentDate);

            //with this constructor, installment are directly calculated when a new CreditContract is instanciated
            _installmentList = CalculateInstallments(true);
            CalculateStartDates();
        }

        private DateTime GetMeetingDate(DateTime startDate, DayOfWeek? dayOfWeek)
        {
            DateTime date = startDate;
            int delta = _generalSettings.IsIncrementalDuringDayOff ? 1 : -1;
            while (date.DayOfWeek != dayOfWeek)
            {
                date = date.AddDays(delta);
            }

            return _nwdS.GetTheNearestValidDate(date, _generalSettings.IsIncrementalDuringDayOff, _generalSettings.DoNotSkipNonWorkingDays, false);
        }

        public OClientTypes ClientType
        {
            get { return _clientType; }
            set { _clientType = value; }
        }

        public string BranchCode
        {
            get { return _branchCode; }
            set { _branchCode = value; }
        }

        public ContractChartOfAccounts ChartOfAccounts
        {
            get { return _chartOfAccounts; }
            set { _chartOfAccounts = value; }
        }

        public DateTime StartDate
        {
            get { return _startDate; }
            set { _startDate = value; }
        }

        public DateTime FirstInstallmentDate
        {
            get { return _firstInstallmentDate; }
            set { _firstInstallmentDate = value; }
        }

        public DateTime AlignDisbursementDate
        {
            get { return _alignAlignDisbursementDate; }
            set { _alignAlignDisbursementDate = value; }
        }

        public DateTime CreationDate { get; set; }

        public DateTime CloseDate { get; set; }

        public bool Synchronize{get; set;}

        public List<ContractCollateral> Collaterals
        {
            get { return _collaterals; }
            set { _collaterals = value; }
        }

        public List<Guarantor> Guarantors
        {
            get { return _guarantors; }
            set { _guarantors = value; }
        }

        public EventStock Events
        {
            get { return _events; }
            set { _events = value; }
        }

        public Guarantor GetGuarantor(int pRank)
        {
            return pRank < _guarantors.Count ? _guarantors[pRank] : null;
        }

        public void AddGuarantor(Guarantor pGuarantor)
        {
            _guarantors.Add(pGuarantor);
        }

        public void AddCollateral(ContractCollateral pCollateral)
        {
            _collaterals.Add(pCollateral);
        }

        public OCurrency Amount
        {
            get { return _amount; }
            set { _amount = value; }
        }

        public OCurrency AmountRounded
        {
            get { return Math.Round(_amount.Value, 2); }
        }

        public decimal InterestRate
        {
            get { return _interestRate; }
            set { _interestRate = value; }
        }

        public int NbOfInstallmentsWhereInterestNotRepay
        {
            get
            {
                int nb = 0;
                foreach (Installment inst in _installmentList)
                {
                    if (!inst.IsRepaid && (inst.InterestsRepayment - inst.PaidInterests) != 0)
                        nb++;
                }
                return nb;
            }
        }

        public int NbOfInstallmentsWherePrincipalNotRepay
        {
            get
            {
                int nb = 0;
                foreach (Installment installment in _installmentList)
                {
                    if (!installment.IsRepaid && (installment.CapitalRepayment - installment.PaidCapital) != 0)
                        nb++;
                }
                return nb;
            }
        }

        public int NbOfInstallmentsNotRepaid
        {
            get
            {
                int nb = 0;
                foreach (Installment inst in InstallmentList)
                {
                    if (!inst.IsRepaid)
                        nb++;
                }
                return nb;
            }
        }
        /// <summary>
        /// This property gets information about next installment
        /// </summary>
        public Installment NextInstallment
        {
            get
            {
                for (int i = 0; i < InstallmentList.Count; i++)
                {
                    if (!InstallmentList[i].IsRepaid)
                        return InstallmentList[i];
                }
                return null;
            }
        }

        public string LoanPurpose { get; set; }

        public string Comments { get; set; }

        public int NbOfInstallments
        {
            get { return _nbOfInstallments; }
            set { _nbOfInstallments = value; }
        }

        public NonRepaymentPenalties NonRepaymentPenalties { get; set; }

        public double AnticipatedTotalRepaymentPenalties { get; set; }
        public double AnticipatedPartialRepaymentPenalties { get; set; }

        public int? GracePeriod { get; set; }

        public InstallmentType InstallmentType
        {
            get { return _installmentType; }
            set { _installmentType = value; }
        }

        public bool Disbursed
        {
            get { return _disbursed; }
            set { _disbursed = value; }
        }

        public bool Closed
        {
            get { return _closed; }
            set { _closed = value; }
        }

        public bool WrittenOff
        {
            get { return _writtenOff; }
            set { _writtenOff = value; }
        }

        public User LoanOfficer { get; set; }

        public User LoanInitialOfficer { get; set; }

        public FundingLine FundingLine { get; set; }

        public bool Rescheduled
        {
            get { return _rescheduled; }
            set { _rescheduled = value; }
        }

        public List<Installment> InstallmentList
        {
            get { return _installmentList; }
            set { _installmentList = value; }
        }

        public bool BadLoan
        {
            get { return _badLoan; }
            set { _badLoan = value; }
        }

        /// <summary>
        /// We need this copy method to perform credit repayment or credit disbursment simulations
        /// without any modification on the contract
        /// </summary>
        /// <returns>A copy of the current CreditContract object</returns>
        public Loan Copy()
        {
            Loan contract = (Loan)MemberwiseClone();
            contract.InstallmentList = new List<Installment>();
            foreach (Installment installment in InstallmentList)
            {
                contract.InstallmentList.Add(installment.Copy());
            }

            contract.Events = new EventStock();

            foreach (Event selectedEvent in Events)
            {
                contract.Events.Add(selectedEvent.Copy());
            }

            return contract;
        }

        /// <summary>
        /// This method calculates total due interests at the creation of the contract
        /// </summary>
        /// <returns></returns>
        public OCurrency CalculateTotalInterests()
        {
            OCurrency amount = 0;

            foreach (Installment installment in _installmentList)
            {
                amount += installment.InterestsRepayment;
            }
            return amount;
        }

        /// <summary>
        /// this method calculates the remaining interests for the contract (ie the due interests)
        /// </summary>
        /// <returns></returns>
        public OCurrency CalculateRemainingInterests()
        {
            OCurrency amount = 0;

            foreach (Installment installment in _installmentList)
            {
                amount += installment.InterestsRepayment - installment.PaidInterests;
            }

            return amount;
        }

        /// <summary>
        /// Method required for bad loan repayment
        /// this method calculates the remaining interests for the contract (ie the due interests)
        /// before a specific date
        /// </summary>
        /// <param name="pDate"></param>
        /// <returns></returns>
        public OCurrency CalculateRemainingInterests(DateTime pDate)
        {
            OCurrency amount = _generalSettings.AccountingProcesses == OAccountingProcesses.Accrual 
                ? GenerateEvents.Accrual.CalculateRemainingInterests(this, pDate) 
                : GenerateEvents.Cash.CalculateRemainingInterests(this, pDate);

            return amount;
        }

        /// <summary>
        /// This method used to calculate the total amount (principal + interests + entry commission) of the contract when its created
        /// Now entry fee is removed
        /// </summary>
        /// <returns></returns>
        public OCurrency CalculateTotalExpectedAmount()
        {
            return _amount + CalculateTotalInterests();
            //return _amount + CalculateTotalInterests() + CalculateEntryFeesAmount();
        }

        public List<OCurrency> CalculateEntryFeesAmount()
        {
            List<OCurrency> fees=new List<OCurrency>();
            decimal amount;
            foreach (LoanEntryFee entryFee in LoanEntryFeesList)
            {
                if (entryFee.ProductEntryFee.IsRate)
                {
                    amount = _amount.Value*entryFee.FeeValue;
                }
                else
                {
                    amount = entryFee.FeeValue;
                }
                var fee = new OCurrency(amount);
                fees.Add(fee);
            }
            return fees;
        }

        public List<OCurrency> CalculateEntryFeesAmount(bool isRate)
        {
            List<OCurrency> fees = new List<OCurrency>();
            decimal amount;
            foreach (LoanEntryFee entryFee in LoanEntryFeesList)
            {
                if (isRate)
                {
                    amount = _amount.Value * entryFee.FeeValue;
                }
                else
                {
                    amount = entryFee.FeeValue;
                }
                var fee = new OCurrency(amount);
                fees.Add(fee);
            }
            return fees;
        }

        public OCurrency CalculateTotalNonRepaymentPenalties(DateTime pDate)
        {
            OCurrency fees = 0;
            
            bool firstInstallmentNotRepaid = false;
                for (int i = 0; i < NbOfInstallments; i++)
                {
                    Installment installment = GetInstallment(i);

                    if (!installment.IsRepaid && !firstInstallmentNotRepaid)
                    {
                        firstInstallmentNotRepaid = true;
                        
                        bool doNotCalculateNewFees = false;
                        foreach (RepaymentEvent rPayment in Events.GetRepaymentEvents())
                        {
                            if (installment.FeesUnpaid != 0 && rPayment.Date == pDate && rPayment.Deleted == false && installment.PaidInterests == 0 && installment.PaidCapital == 0)
                            {
                                doNotCalculateNewFees = true;
                            }
                        }

                        if (!doNotCalculateNewFees)
                        {
                            fees += CalculationBaseForLateFees.FeesBasedOnOverduePrincipal(this, pDate, installment.Number, false, _generalSettings, _nwdS)
                                                     + CalculationBaseForLateFees.FeesBasedOnOverdueInterest(this, pDate, installment.Number, false, _generalSettings, _nwdS)
                                                     + CalculationBaseForLateFees.FeesBasedOnInitialAmount(this, pDate, installment.Number, false, _generalSettings, _nwdS)
                                                     + CalculationBaseForLateFees.FeesBasedOnOlb(this, pDate, installment.Number, false, _generalSettings, _nwdS);
                       }
                    }
                    else if (!installment.IsRepaid && firstInstallmentNotRepaid)
                    {
                        fees += CalculationBaseForLateFees.FeesBasedOnOverdueInterest(this, pDate, installment.Number, false, _generalSettings, _nwdS) +
                            CalculationBaseForLateFees.FeesBasedOnOverduePrincipal(this, pDate, installment.Number, false, _generalSettings, _nwdS);
                    }
                }
            
            return fees;
        }

        public OCurrency CalculateNonRepaymentPenaltiesAmountForClosure(DateTime pDate)
        {
            bool firstInstallmentNotRepaid = false;
            OCurrency fees = 0;
            for (int i = 0; i < NbOfInstallments; i++)
            {
                Installment installment = GetInstallment(i);
                if (!installment.IsRepaid && !firstInstallmentNotRepaid)
                {
                    firstInstallmentNotRepaid = true;

                    fees += CalculationBaseForLateFees.FeesBasedOnInitialAmount(this, pDate, installment.Number, true, _generalSettings, _nwdS);
                    fees += CalculationBaseForLateFees.FeesBasedOnOlb(this, pDate, installment.Number, true, _generalSettings, _nwdS);
                    fees += CalculationBaseForLateFees.FeesBasedOnOverdueInterest(this, pDate, installment.Number, true, _generalSettings, _nwdS);
                    fees += CalculationBaseForLateFees.FeesBasedOnOverduePrincipal(this, pDate, installment.Number, true, _generalSettings, _nwdS);
                }
                else if (!installment.IsRepaid && firstInstallmentNotRepaid)
                {
                    fees += CalculationBaseForLateFees.FeesBasedOnOverdueInterest(this, pDate, installment.Number, true, _generalSettings, _nwdS);
                    fees += CalculationBaseForLateFees.FeesBasedOnOverduePrincipal(this, pDate, installment.Number, true, _generalSettings, _nwdS);
                }
            }
            return fees;
        }

        public OCurrency CalculateAnticipateInteresAmountForClosure(DateTime pDate, OPaymentType pPaymentType, bool payProportion)
        {
            if(payProportion)
            {
                OCurrency amount = 0;
                foreach (Installment installment in _installmentList)
                {
                    if (!installment.IsRepaid && installment.CapitalRepayment != 0)
                    {
                        amount += installment.InterestsRepayment;
                    }
                }
                return amount;
            }
            return CalculationBaseForLateFees.CalculateAnticipateRepayment(this, pDate, _generalSettings, _nwdS, pPaymentType); 
        } 
        /// <summary>
        /// Over due amount is the principal (not interest) that should have been paid
        /// at date but that has not been paid yet.
        /// </summary>
        /// <param name="pDate"></param>
        /// <returns>the principal due</returns>
        public OCurrency CalculateOverduePrincipal(DateTime pDate)
        {
            OCurrency result = 0;
            _installmentList.Sort((x,y)=> x.ExpectedDate.CompareTo(y.ExpectedDate));
            foreach (Installment installment in _installmentList)
            {
                if (!installment.IsRepaid && installment.ExpectedDate <= pDate)
                {
                    result += (installment.CapitalRepayment - installment.PaidCapital);
                }
            }
            return result;
        }

        /// <summary>
        /// Calculate the past due days since the last repayment
        /// </summary>
        /// <param name="pDate"></param>
        /// <returns></returns>
        public int CalculatePastDueSinceLastRepayment(DateTime pDate)
        {
            DateTime date = DateTime.MinValue;
            int installmentNumber = 0;

            foreach (Installment installment in _installmentList)
            {
                if (!installment.IsRepaid)
                {
                    date = installment.ExpectedDate;
                    installmentNumber = installment.Number;
                    break;
                }
            }

            foreach (RepaymentEvent rPayment in Events.GetRepaymentEvents())
            {
                if (rPayment.Date == pDate 
                    && rPayment.Deleted == false 
                    && installmentNumber == rPayment.InstallmentNumber)
                {
                    date = pDate;
                }

                if (rPayment.InstallmentNumber == installmentNumber 
                    && rPayment.Deleted == false 
                    && rPayment.Date != pDate 
                    && pDate > date)
                {
                    if (rPayment.Date >= date)
                    {
                        date = rPayment.Date;
                    }
                }
            }

            int pstDue = (pDate - date).Days;

            pstDue = pstDue < 0 ? 0 : pstDue;

            return pstDue;
        }

        /// <summary>
        /// Use it to calculate absolute past due -days
        /// </summary>
        /// <param name="pDate">Today</param>
        /// <returns>past due days</returns>
        private int _CalculatePastDue(DateTime pDate)
        {
            foreach (Installment installment in _installmentList)
            {
                if (!installment.IsRepaid)
                {
                    TimeSpan time = pDate - installment.ExpectedDate;
                    int pstDueDays = time.Days;

                    return pstDueDays < 0 ? 0 : pstDueDays;
                }
            }
            return 0;
        }

        public int CalculatePastDueForClosure(DateTime pDate)
        {
            int pstDueforContract = CalculatePastDueSinceLastRepayment(pDate);
            
            int pstDue = 0;
            
            if (pstDueforContract != 0)
            {
                return pstDue <= 0 ? pstDueforContract : pstDue;
            }
            return 0;
        }

        public int CalculatePastDueForLastClosure(DateTime pDate)
        {
            return 0;
        }

        public bool AllInstallmentsRepaid
        {
            get
            {
                if (_installmentList.Count == 0)
                    return false;

                foreach (Installment installment in _installmentList)
                {
                    if (!installment.IsRepaid)
                        return false;
                }
                return true;
            }
        }

        public OContractStatus ContractStatus { get; set; }

        public string CreditCommitteeCode { get; set;}

        public void AddInstallment(Installment pInstallment)
        {
            _installmentList.Add(pInstallment);
        }

        /// <summary>
        /// an installment object or null if number not found
        /// </summary>
        /// <param name="pInstallmentRank"></param>
        /// <returns></returns>
        public Installment GetInstallment(int pInstallmentRank)
        {
            return pInstallmentRank < _installmentList.Count ? _installmentList[pInstallmentRank] : null;
        }

        /// <summary>
        /// Calculate the expected date for an installment and modify it if it is public holiday or week-end
        /// Rules are : +1 day for public holiday and for week-end 
        /// </summary>
        /// <returns>the installment date</returns>
        public DateTime CalculateInstallmentDate(DateTime pStartDate, int pInstallmentNumber)
        {
            pStartDate = pStartDate.AddMonths(_installmentType.NbOfMonths * pInstallmentNumber).AddDays(_installmentType.NbOfDays * pInstallmentNumber);
            return _nwdS.GetTheNearestValidDate(pStartDate, _generalSettings.IsIncrementalDuringDayOff, _generalSettings.DoNotSkipNonWorkingDays, true);
        }

        public DateTime CalculateInstallmentDateWithOutCheckingWeekend(DateTime pStartDate, int pInstallmentNumber)
        {
            pStartDate = pStartDate.AddMonths(_installmentType.NbOfMonths * pInstallmentNumber).AddDays(_installmentType.NbOfDays * pInstallmentNumber);
            return _nwdS.GetTheNearestValidDate(pStartDate, _generalSettings.IsIncrementalDuringDayOff, _generalSettings.DoNotSkipNonWorkingDays, false);
        }

        public DateTime CalculateInstallmentExpectedDate(DateTime pStartDate, int pInstallmentNumber)
        {
            pStartDate = pStartDate.AddMonths(_installmentType.NbOfMonths * pInstallmentNumber).AddDays(_installmentType.NbOfDays * pInstallmentNumber);
            return pStartDate;
        }

        public DateTime CalculateAlignDisbursementDate(DateTime pFirstInstallmentDate)
        {
            InstallmentType installmentType = _installmentType;

            if (installmentType == null)
                installmentType = Product.InstallmentType;

            pFirstInstallmentDate = pFirstInstallmentDate.AddMonths(-installmentType.NbOfMonths).AddDays(-installmentType.NbOfDays);
            return pFirstInstallmentDate;
        }

        public DateTime CheckDateOfInstallment(DateTime pStartDate)
        {
            return _nwdS.GetTheNearestValidDate(pStartDate, _generalSettings.IsIncrementalDuringDayOff, _generalSettings.DoNotSkipNonWorkingDays, true);
        }

        /// <summary>
        /// Calculate the VPM based on OLB and remaining installments
        /// </summary>
        /// <param name="pOLB">owed amount by the customer</param>
        /// <param name="pNbOfInstallmentsToPay">remaining installments</param>
        /// <returns>decimal value calculated</returns>
        public OCurrency VPM(OCurrency pOLB, int pNbOfInstallmentsToPay)
        {
            return (pOLB.Value * Convert.ToDecimal(_interestRate * (decimal) Math.Pow((double) (_interestRate + 1), pNbOfInstallmentsToPay))) /
                   Convert.ToDecimal(Math.Pow((double) (_interestRate + 1), pNbOfInstallmentsToPay) - 1);
        }

        public OCurrency VPM(OCurrency pOLB, int pNbOfInstallmentsToPay, decimal pInterestRate)
        {
            return (pOLB.Value * Convert.ToDecimal(pInterestRate * (decimal) Math.Pow((double) (pInterestRate + 1), pNbOfInstallmentsToPay))) /
                   Convert.ToDecimal(Math.Pow((double) (pInterestRate + 1), pNbOfInstallmentsToPay) - 1);
        }

        /// <summary>
        /// This method calculates the correct set of installments for a contract.
        /// 4 cases are treated :
        /// - Flat rate non exotic
        /// - Flat rate exotic
        /// - Declining rate non exotic
        /// - Declining rate exotic
        /// </summary>
        /// <param name="pChangeDate">must be set to true when creation of the contract (here in constructor), must be false in all others cases</param>
        public List<Installment> CalculateInstallments(bool pChangeDate)
        {
            CalculateInstallmentsOptions cIo = new CalculateInstallmentsOptions(_startDate, Product.LoanType, Product.IsExotic, this, pChangeDate);
            CalculateInstallmentsStrategy cIs = new CalculateInstallmentsStrategy(cIo, _amount, _nbOfInstallments, _generalSettings);

            List<Installment> installments = cIs.CalculateInstallments(pChangeDate);
            for (int i = 0; i < installments.Count; i++)
            {
                installments[i].StartDate = 0 == i ? StartDate : installments[i - 1].ExpectedDate;
            }
            return installments;
        }

        public void CalculateStartDates()
        {
            for (int i = 0; i < _installmentList.Count; i++)
            {
                _installmentList[i].StartDate = 0 == i ? StartDate : _installmentList[i - 1].ExpectedDate;
            }
        }

        private OCurrency GetRepaymentEventAmountByNumber(int pNumber)
        {
            OCurrency amount = 0;

            foreach (RepaymentEvent repEvent in Events.GetLoanRepaymentEvents())
            {
                if (repEvent.InstallmentNumber == pNumber && !repEvent.Deleted)
                    amount += repEvent.Principal;
            }
            
            return amount;
        }

        private OCurrency GetTrancheEventAmountByNumber(int pNumber)
        {
            OCurrency amount = 0;

            foreach (TrancheEvent trancheEvent in GivenTranches)
            {
                if (trancheEvent.StartedFromInstallment  == pNumber && !trancheEvent.Deleted && trancheEvent.Number>0)
                    amount = trancheEvent.Amount;
            }

            return amount;
        }

        public OCurrency CalculateExpectedOlb(int pInstallmentNumber, bool pKeepExpectedInstallment)
        {
            OCurrency[] olbTable = new OCurrency[InstallmentList.Count];
            OCurrency initialAmount = Amount;

            if(GivenTranches.Count > 1)
            {
                foreach(TrancheEvent tEvent in GivenTranches)
                {
                    if(tEvent.Number > 0 && !tEvent.Deleted)
                        initialAmount -= tEvent.Amount;
                }
            }

            for (int n = 0; n <= InstallmentList.Count - 1; n++)
            {
                if (n + 1 == 1)
                {
                    olbTable[n] = initialAmount - (GetRepaymentEventAmountByNumber(n + 1) - InstallmentList[n].PaidCapital) + GetTrancheEventAmountByNumber(n);
                }
                else
                {
                    olbTable[n] = olbTable[n - 1] + GetTrancheEventAmountByNumber(n) -
                                  (InstallmentList[n - 1].CapitalRepayment +
                                   (GetRepaymentEventAmountByNumber(n + 1) - InstallmentList[n].PaidCapital));
                }
            }

            return Math.Round(olbTable[pInstallmentNumber - 1].Value, 2);
        }

        /// <summary>
        /// Calculate OLB based on remaining capital
        /// </summary>
        /// <returns></returns>
        public OCurrency CalculateActualOlb()
        {
            OCurrency amount = 0;

            foreach (Installment installment in _installmentList)
            {
                amount += (installment.CapitalRepayment - installment.PaidCapital);
            }

            return amount;
        }
           


        /// <summary>
        /// Calculate OLB based on remaining capital at date d
        /// </summary>
        /// <returns></returns>
        public OCurrency CalculateActualOlb(DateTime d)
        {
            OCurrency amount = 0;

            foreach (Installment installment in _installmentList)
            {
                if (DateTime.Compare(installment.ExpectedDate, d) <= 0)
                    amount += (installment.CapitalRepayment - installment.PaidCapital);
            }
            return amount;
        }

        public OCurrency GetOlb() 
        {
            foreach (Installment installment in _installmentList)
            {
                if (!installment.IsRepaid)
                {
                    return installment.OLB;
                }
            }
            return 0;
        }

        public OCurrency GetRemainAmount()
        {
            OCurrency remainAmount = 0;

            foreach (Installment installment in InstallmentList)
            {
                if (!installment.IsRepaid)
                {
                    remainAmount += installment.CapitalRepayment - installment.PaidCapital;
                }
            }

            return remainAmount;
        }

        /// <summary>
        /// Calculate the remaining interests
        /// </summary>
        /// <returns></returns>
        public OCurrency CalculateActualInterestsToPay()
        {
            OCurrency amount = 0;

            foreach (Installment installment in _installmentList)
            {
                amount += (installment.InterestsRepayment - installment.PaidInterests);
            }
            return amount;
        }

        private CreditInsuranceEvent GetCreditInsuranceEvent(LoanDisbursmentEvent disbursmentEvent)
        {
            CreditInsuranceEvent cie = new CreditInsuranceEvent
                                           {
                                               Date = disbursmentEvent.Date,
                                               Cancelable = true,
                                               User = User.CurrentUser,
                                               Code = "LCIE",
                                               Principal = 0,
                                               Commission = Amount * Insurance / 100,
                                               TellerId = disbursmentEvent.TellerId
                                           };
            return cie;
        }

        /// <summary>
        /// Method to Disburse money for the contract
        /// </summary>
        /// <param name="pDisburseDate">Date of disbursment</param>
        /// <param name="pDisableFees">when true, entry commission paid for this contract are set to 0</param>
        /// <param name="pAlignInstallmentsDatesOnRealDisbursmentDate"></param>
        /// <returns>A LoanDisburment event if correct date of disbursment. /!\Becarful, loan must be non debursed </returns>
        public LoanDisbursmentEvent Disburse(DateTime pDisburseDate, bool pAlignInstallmentsDatesOnRealDisbursmentDate, bool pDisableFees)
        {
            LoanDisbursmentEvent e = _generalSettings.AccountingProcesses == OAccountingProcesses.Cash
                                         ? GenerateEvents.Cash.GenerateLoanDisbursmentEvent(this, _generalSettings,
                                                                                            pDisburseDate,
                                                                                            pAlignInstallmentsDatesOnRealDisbursmentDate,
                                                                                            pDisableFees, _user)
                                         : GenerateEvents.Accrual.GenerateLoanDisbursmentEvent(this, _generalSettings,
                                                                                               pDisburseDate,
                                                                                               pAlignInstallmentsDatesOnRealDisbursmentDate,
                                                                                               pDisableFees, _user);
            e.Interest = GetTotalInterestDue();
            if (!pDisableFees)
            {
                List<OCurrency> entryFees = GetEntryFees();
                if (entryFees != null)
                {
                    e.Commissions = new List<LoanEntryFeeEvent>();
                    for (int i=0; i<entryFees.Count; i++)
                    {
                        LoanEntryFeeEvent loanEntryFeeEvent = new LoanEntryFeeEvent
                                                                  {
                                                                      Fee =
                                                                          Product.Currency.UseCents
                                                                              ? Math.Round(entryFees[i].Value, 2)
                                                                              : Math.Round(entryFees[i].Value,
                                                                                           MidpointRounding.AwayFromZero),
                                                                      Code = "LEE" + LoanEntryFeesList[i].ProductEntryFee.Index,
                                                                      DisbursementEventId = e.Id,
                                                                      Cancelable = true,
                                                                      User = User.CurrentUser,
                                                                      Date = e.Date
                                                                  };
                        e.Commissions.Add(loanEntryFeeEvent);

                        if (Teller.CurrentTeller != null && Teller.CurrentTeller.Id != 0)
                            loanEntryFeeEvent.TellerId = Teller.CurrentTeller.Id;

                        if (loanEntryFeeEvent.Fee > 0)
                        {
                            Events.Add(loanEntryFeeEvent);
                        }
                    }
                }
            }

            if (HasCompulsoryAmount())
            {
                SavingBlockCompulsarySavingsEvent savingBlockEvent = new SavingBlockCompulsarySavingsEvent();
                Debug.Assert(CompulsorySavingsPercentage != null,
                             string.Format(
                                 "Loan with code {0}, should be cheked for compulsory existance, before adding saving block event",
                                 Code)
                    );
                OCurrency csAmount = (Amount.Value * CompulsorySavingsPercentage.Value) / 100m;
                int contracId = CompulsorySavings.Id;
                savingBlockEvent.ContracId = contracId;
                savingBlockEvent.User = _user;
                savingBlockEvent.Amount = csAmount;
                savingBlockEvent.Date = StartDate;
                savingBlockEvent.EntryDate = TimeProvider.Now;
                savingBlockEvent.Cancelable = true;
                CompulsorySavings.Events.Add(savingBlockEvent);
            }

            CreditInsuranceEvent cie = GetCreditInsuranceEvent(e);
            if (cie.Commission > 0)
                Events.Add(cie);

            GivenTranches.Clear();
            TrancheEvent trancheEvent = new TrancheEvent
            {
                StartDate = pDisburseDate,
                Maturity = NbOfInstallments,
                Amount = Amount,
                InterestRate = (decimal)InterestRate,
                ApplyNewInterest = false,
                Number = 0
            };

            GivenTranches.Add(trancheEvent);
            return e;
        }

        public bool HasCompulsoryAmount()
        {
            return CompulsorySavings != null &&
                   CompulsorySavingsPercentage != null &&
                   CompulsorySavingsPercentage.Value != 0;
        }

        public List<OCurrency> GetEntryFees()
        {
            List<OCurrency> entryFees = new List<OCurrency>();
            if (LoanEntryFeesList == null) return null;
            foreach (LoanEntryFee fee in LoanEntryFeesList)
            {
                OCurrency feeAmount;
                if (fee.ProductEntryFee.IsRate)
                    feeAmount = _amount.Value * fee.FeeValue/100;
                else
                    feeAmount = fee.FeeValue;
                entryFees.Add(feeAmount);
            }
            return entryFees;
        }

        /// <summary>
        /// Test if the client has repaid the total principal, interests and penalties owed at date D
        /// </summary>
        /// <param name="rPe"></param>
        /// <param name="date"></param>
        /// <returns></returns>
        public bool _IsBadLoan(RepaymentEvent rPe, DateTime date)
        {
            if (rPe is BadLoanRepaymentEvent)
            {
                ((BadLoanRepaymentEvent) rPe).OLB = CalculateActualOlb();

                if (AmountComparer.Equals(0, CalculateRemainingInterests(date)) &&
                    AmountComparer.Equals(0, CalculateActualOlb(date)))
                {
                    ((BadLoanRepaymentEvent) rPe).AccruedProvision += 0;
                    return false;
                }
                return true;
            }
            return false;
        }

        public OCurrency CalculateMaximumAmountAuthorizedToRepay(int pNumber, DateTime pDate,
            bool pCancelFees, OCurrency pManualFeesAmount, OCurrency pManualCommissionAmount,  
            bool pDisableInterests, OCurrency pManualInterests, bool pKeepExpectedInstallment)
        {
            Loan fakeLoan = Copy();

            if (_generalSettings.AccountingProcesses == OAccountingProcesses.Accrual && ! pKeepExpectedInstallment)
            {
                fakeLoan.GetAccruedInterestEvent(pDate);
            }

            CreditContractOptions cCO = new CreditContractOptions(Product.LoanType, pKeepExpectedInstallment, 
                pCancelFees, pManualFeesAmount, pManualCommissionAmount,
                pDisableInterests, pManualInterests, Product.AnticipatedTotalRepaymentPenaltiesBase);
            CreditContractRepayment cCR = new CreditContractRepayment(fakeLoan, cCO, pDate, pNumber, _user, _generalSettings, _nwdS);

            return cCR.MaximumAmountAuthorizeToRepay;
        }

        public OCurrency CalculateMaximumAmountAuthorizedToRepay(int pNumber, DateTime pDate, 
            bool pCancelFees, OCurrency pManualFeesAmount, OCurrency pManualCommissionAmount, 
            bool pDisableInterests, OCurrency pManualInterests, bool pKeepExpectedInstallment, bool pIsForExoticProduct)
        {
            Loan fakeLoan = Copy();
            if (_generalSettings.AccountingProcesses == OAccountingProcesses.Accrual && !pKeepExpectedInstallment)
            {
                fakeLoan.CreateLoanInterestAccruingEvent(pDate);
            }

            CreditContractOptions cCo = new CreditContractOptions(Product.LoanType, 
                                                                  pKeepExpectedInstallment, 
                                                                  pCancelFees, 
                                                                  pManualFeesAmount, 
                                                                  pManualCommissionAmount,
                                                                  pDisableInterests, 
                                                                  pManualInterests, 
                                                                  Product.AnticipatedTotalRepaymentPenaltiesBase, 
                                                                  pIsForExoticProduct, 
                                                                  false);
            
            CreditContractRepayment cCr = new CreditContractRepayment(fakeLoan, 
                                                                      cCo, 
                                                                      pDate, 
                                                                      pNumber, 
                                                                      _user, 
                                                                      _generalSettings, 
                                                                      _nwdS);

            return cCr.MaximumAmountAuthorizeToRepay;
        }

        public OCurrency CalculateAmountToRepaySpecifiedInstallment(int pNumber, DateTime pDate,
            bool pCancelFees, OCurrency pManualFeesAmount, OCurrency pManualCommissionAmount, 
            bool pDisableInterests, OCurrency pManualInterests, bool pKeepExpectedInstallment)
        {
            CreditContractOptions cCo =
                new CreditContractOptions(Product.LoanType, pKeepExpectedInstallment, 
                    pCancelFees, pManualFeesAmount, pManualCommissionAmount,
                    pDisableInterests, pManualInterests, Product.AnticipatedTotalRepaymentPenaltiesBase);

            CreditContractRepayment cCr = new CreditContractRepayment(this, cCo, pDate, pNumber, _user, _generalSettings, _nwdS);

            return cCr.AmountToRepayInstallment;
        }

        public OCurrency CalculateMaximumAmountToRegradingLoan(int pNumber, DateTime pDate, bool pCancelFees, OCurrency pManualFeesAmount, 
            OCurrency pManualCommissionAmount, bool pDisableInterests, OCurrency pManualInterests, bool pKeepExpectedInstallment)
        {
            CreditContractOptions cCo =
                new CreditContractOptions(Product.LoanType, pKeepExpectedInstallment, pCancelFees, pManualFeesAmount, pManualCommissionAmount,
                    pDisableInterests, pManualInterests, Product.AnticipatedTotalRepaymentPenaltiesBase);

            CreditContractRepayment cCr = new CreditContractRepayment(this, cCo, pDate, pNumber, _user, _generalSettings, _nwdS);

            return cCr.MaximumAmountToRegradingLoan;
        }

        public OCurrency CalculateMaximumAmountForEscapedMember(int pNumber, DateTime pDate,
            bool pCancelFees, OCurrency pManualFeesAmount, OCurrency pManualCommissionAmount,
            bool pDisableInterests, OCurrency pManualInterests, bool pKeepExpectedInstallment, OCurrency pLoanShareAmount)
        {
            var cCo = new CreditContractOptions(Product.LoanType, 
                                                pKeepExpectedInstallment,
                                                pCancelFees, 
                                                pManualFeesAmount, 
                                                pManualCommissionAmount,
                                                pDisableInterests, 
                                                pManualInterests,
                                                Product.AnticipatedTotalRepaymentPenaltiesBase, 
                                                Product.IsExotic,
                                                false);

            var cCr = new CreditContractRepayment(this, cCo, pDate, pNumber, _user, _generalSettings, _nwdS);

            return cCr.MaximumAmountForEscapedMember;
        }

        /// <summary>
        /// This method manages all the repayment cases implemented in the software
        /// </summary>
        /// <param name="pNumber">pNumber of the installment paid</param>
        /// <param name="pDate">pDate of the payment</param>
        /// <param name="pAmountPaid">amount paid by the client which can be lower, equal or greater than the expected amount for an installment</param>
        /// <param name="cancelFees">when true, cancel anticipated payment Commission</param>
        /// <returns>A RepaymentEvent or null if : 
        /// - repayment amount lower than 0
        /// - repayment amount greater than olb + interestsToPay + commission
        /// - installment already repaid
        /// - bad loan and past due days greater than 180
        /// </returns>
        /// <param name="pKeepExpectedInstallment"></param>
        public RepaymentEvent Repay(int pNumber, 
                                    DateTime pDate, 
                                    OCurrency pAmountPaid, 
                                    bool cancelFees, 
                                    bool pKeepExpectedInstallment)
        {
            PaymentMethod pm = new PaymentMethod(1, "Cash", "", false);
            return Repay(pNumber, 
                         pDate, 
                         pAmountPaid, 
                         cancelFees, 
                         0, 
                         0, 
                         false, 
                         0, 
                         pKeepExpectedInstallment,
                         false,
                         pm, 
                         null, 
                         false);
        }

        /// <summary>
        /// This method manages all the repayment cases implemented in software
        /// </summary>
        /// <param name="pNumber">pNumber of the installment paid</param>
        /// <param name="pDate">pDate of the payment</param>
        /// <param name="pAmountPaid">amount paid by the client which can be lower, equal or greater than the expected amount for an installment</param>
        /// <param name="cancelFees">when true, cancel anticipated payment Commission</param>
        /// <param name="manualFeesAmount">manual amount of commission (when anticipated payment commission are cancelled)</param>
        /// <returns>A RepaymentEvent or null if : 
        /// - repayment amount lower than 0
        /// - repayment amount greater than olb + interestsToPay + commission
        /// - installment already repaid
        /// - bad loan and past due days greater than 180
        /// </returns>
        /// <param name="manualCommissionAmount"></param>
        /// <param name="disableInterests"></param>
        /// <param name="manualInterests"></param>
        /// <param name="keepExpectedInstallment"></param>
        /// <param name="payProportion"></param>
        /// <param name="isPending"></param>
        /// <param name="paymentMethod"></param>
        public RepaymentEvent Repay(int pNumber, 
                                    DateTime pDate, 
                                    OCurrency pAmountPaid,
                                    bool cancelFees, 
                                    OCurrency manualFeesAmount, 
                                    OCurrency manualCommissionAmount,
                                    bool disableInterests, 
                                    OCurrency manualInterests, 
                                    bool keepExpectedInstallment,
                                    bool payProportion,
                                    bool isPending,
                                    PaymentMethod paymentMethod)
        {
            return Repay(pNumber, 
                         pDate, 
                         pAmountPaid, 
                         cancelFees, 
                         manualFeesAmount, 
                         manualCommissionAmount,
                         disableInterests, 
                         manualInterests, 
                         keepExpectedInstallment,
                         payProportion,
                         paymentMethod, 
                         null, 
                         isPending);
        }


        /// <summary>
        /// This method manages all the repayment cases implemented in the software
        /// </summary>
        /// <param name="pNumber">pNumber of the installment paid</param>
        /// <param name="pDate">pDate of the payment</param>
        /// <param name="pAmountPaid">amount paid by the client which can be lower, equal or greater than the expected amount for an installment</param>
        /// <param name="cancelFees">when true, cancel anticipated payment Commission</param>
        /// <param name="manualFeesAmount">manual amount of commission (when anticipated payment commission are cancelled)</param>
        /// <returns>A RepaymentEvent or null if : 
        /// - repayment amount lower than 0
        /// - repayment amount greater than olb + interestsToPay + commission
        /// - installment already repaid
        /// - bad loan and past due days greater than 180
        /// </returns>
        /// <param name="manualCommissionAmount"></param>
        /// <param name="disableInterests"></param>
        /// <param name="manualInterests"></param>
        /// <param name="keepExpectedInstallment"></param>
        /// <param name="payProportion"></param>
        /// <param name="paymentMethod"></param>
        /// <param name="comment"></param>
        /// <param name="pending"></param>
        public RepaymentEvent Repay(int pNumber, 
                                    DateTime pDate, 
                                    OCurrency pAmountPaid,
                                    bool cancelFees, 
                                    OCurrency manualFeesAmount, 
                                    OCurrency manualCommissionAmount,
                                    bool disableInterests, 
                                    OCurrency manualInterests, 
                                    bool keepExpectedInstallment,
                                    bool payProportion,
                                    PaymentMethod paymentMethod, 
                                    string comment, 
                                    bool pending)
        {
            OCurrency anticipatePayment =
                CalculateAnticipateInteresAmountForClosure(pDate, OPaymentType.PartialPayment, payProportion) +
                CalculateTotalNonRepaymentPenalties(pDate);

            if (anticipatePayment >= pAmountPaid 
                || (anticipatePayment == 0 && GetInstallment(pNumber - 1).ExpectedDate <= pDate))
            {
                keepExpectedInstallment = true;
                payProportion = false;
            }

            var cCo = new CreditContractOptions(Product.LoanType, 
                                                keepExpectedInstallment,
                                                cancelFees, 
                                                manualFeesAmount, 
                                                manualCommissionAmount,
                                                disableInterests, 
                                                manualInterests,
                                                Product.AnticipatedTotalRepaymentPenaltiesBase, 
                                                Product.IsExotic,
                                                payProportion);

            var cCr = new CreditContractRepayment(this, cCo, pDate, pNumber, _user, _generalSettings, _nwdS);

            if (AmountComparer.Compare(pAmountPaid, cCr.MaximumAmountAuthorizeToRepay, pNumber) > 0)
            {
                return null;
            }

            OCurrency principalEvent = 0;
            OCurrency interestEvent = 0;
            OCurrency interestPrepayment = 0;
            OCurrency penaltiesEvent = 0; 
            OCurrency commissionsEvent = 0;
            OCurrency manualInterestEvent = cCo.ManualInterestsAmount;

            int pastDueDays = CalculatePastDueSinceLastRepayment(pDate);

            OPaymentType paymentType = OPaymentType.StandardPayment;

            foreach (Installment installment in InstallmentList)
            {
                if (!installment.IsRepaid && installment.Number == pNumber && !keepExpectedInstallment)
                {
                    paymentType = OPaymentType.PartialPayment;
                }
            }

            //we have total repayment for a person
            if(EscapedMember != null)
            {
                paymentType = OPaymentType.PartialPayment;
                keepExpectedInstallment = false;
                cCo.KeepExpectedInstallments = false;
            }

            if (AmountComparer.Compare(pAmountPaid, cCr.MaximumAmountAuthorizeToRepay, pNumber) == 0 && !keepExpectedInstallment)
            {
                paymentType = OPaymentType.TotalPayment;
            }

            if (payProportion && _product.LoanType == OLoanTypes.DecliningFixedPrincipal)
            {
                paymentType = OPaymentType.ProportionalPayment;
                keepExpectedInstallment = false;
                cCo.KeepExpectedInstallments = false;
            }

            if (_product.LoanType == OLoanTypes.DecliningFixedPrincipalWithRealInterest)
                paymentType = OPaymentType.StandardPayment;

            // We have to calculate penalties here because
            // once the repayment is done it is impossible to do
            // reliably afterward. These values are later used (see below)
            // during the event generation.

            cCr.Repay(pAmountPaid, ref penaltiesEvent, ref commissionsEvent, ref interestEvent,
                      ref interestPrepayment, ref principalEvent, ref manualInterestEvent, ref paymentType);

            //this part of code to correct calculation of principal
            OCurrency principalAmount = principalEvent;
            foreach (RepaymentEvent rPayment in Events.GetRepaymentEvents())
            {
                if (!rPayment.Deleted)
                    principalAmount += rPayment.Principal;

                if (principalAmount > Amount)
                {
                    principalEvent -= Math.Round(principalAmount.Value, 2) - Amount;
                }
            }

            // when we keep initial schedule and total payment
            if (AmountComparer.Compare(pAmountPaid, cCr.MaximumAmountAuthorizeToRepay, pNumber) == 0 && (pNumber != InstallmentList.Count) && AllInstallmentsRepaid)
            {
                paymentType = OPaymentType.TotalPayment;
            }

            //////////////////////////////////////////////////////////////
            RepaymentEvent rPe = CreateRepaymentEvent(pNumber, 
                                                      pDate, 
                                                      penaltiesEvent, 
                                                      commissionsEvent, 
                                                      interestEvent,
                                                      interestPrepayment, 
                                                      principalEvent, 
                                                      pastDueDays,
                                                      _clientType == OClientTypes.Group, 
                                                      paymentType, 
                                                      pending);

            if (AllInstallmentsRepaid && !pending)
            {
                if (ContractStatus != OContractStatus.WrittenOff)
                {
                    _closed = true;
                    ContractStatus = OContractStatus.Closed;
                    CloseDate = pDate;
                }

                // check if Client has other 'active' loans if so, mark him as active client
                if (Project != null)
                    foreach (var loan in Project.Credits)
                        Project.Client.Active = loan.ContractStatus == OContractStatus.Active;
            }

            //Event identification
            Events.Add(GenerateRepaymentEvents(cCr, 
                                               pDate, 
                                               penaltiesEvent, commissionsEvent, interestEvent,
                                                   interestPrepayment, principalEvent, pastDueDays, paymentType,
                                                   pending, pNumber, paymentMethod, cancelFees, manualFeesAmount,
                                                   manualInterestEvent, 
                                               ref rPe));

            if (paymentType == OPaymentType.TotalPayment)
            {
                int installmentNumber = penaltiesEvent > 0 ? 0 : 1;
                Events.Add(GenerateCreditInsuranceEvent(installmentNumber));

                if (HasCompulsoryAmount())
                {
                    SavingBlockCompulsarySavingsEvent savingBlockEvent = CompulsorySavings.GetBlockCompulsorySavingEvent();
                    if (savingBlockEvent != null)
                        CompulsorySavings.GenerateUnblockCompulsoruSavingEvent(_user, true);
                }
            }

            //principal amount correction in case of shit which is taken place when we do big prepayment
            //please remove it when all shity contracts will be closed
            OCurrency paidPrincipal = 0;
            foreach (RepaymentEvent repaymentEvent in Events.GetLoanRepaymentEvents())
            {
                if(repaymentEvent.Deleted == false)
                {
                    paidPrincipal += repaymentEvent.Principal;
                }

                if(paidPrincipal >  Amount)
                {
                    repaymentEvent.Principal += Amount - paidPrincipal;
                }
            }
            //////////////////////////////////////////////////////////////////////////////////////////////


            _installmentList.Sort((x, y) => x.ExpectedDate.CompareTo(y.ExpectedDate));

            foreach (Installment installment in InstallmentList)
            {
                //setup paid date for installments
                if (installment.IsRepaid && installment.Number > pNumber)
                {
                    installment.PaidDate = pDate;
                    installment.Comment = comment;
                    installment.IsPending = pending;
                }
                else if (installment.Number == pNumber)
                {
                    installment.PaidDate = pDate;
                    installment.Comment = comment;
                    installment.IsPending = pending;
                }

                if (!keepExpectedInstallment && (paymentType == OPaymentType.PartialPayment || paymentType == OPaymentType.TotalPayment))
                    installment.OLB = CalculateExpectedOlb(installment.Number, keepExpectedInstallment);
            }
            EscapedMember = null;
            return rPe;
        }

        public CreditInsuranceEvent GenerateCreditInsuranceEvent(int installmentNumber)
        {
            foreach (RepaymentEvent item in Events.GetRepaymentEvents())
            {
                if (item.Code == "ATR" && item.Deleted == false && Insurance != 0)
                {
                    installmentNumber = item.InstallmentNumber - installmentNumber;
                }
            }

            decimal sumOfInstallations = 0;

            for (int i = 1; i <= InstallmentList.Count; i++)
                sumOfInstallations += i;

            decimal sumOfNotPaidInstallations = 0;

            for (int i = InstallmentList.Count - installmentNumber + 1; i <= InstallmentList.Count; i++)
            {
                sumOfNotPaidInstallations += i;
            }

            CreditInsuranceEvent cieOrigination = Events.GetCreditInsuranceEvent();
            OCurrency insuranceAmount = 0;

            if (cieOrigination != null)
                insuranceAmount = cieOrigination.Commission.Value;

            int roundPoint = UseCents ? 2 : 0;
            OCurrency earnedPremiumCurrencyValue =
                Math.Round(insuranceAmount.Value * sumOfNotPaidInstallations / sumOfInstallations, roundPoint,
                           MidpointRounding.AwayFromZero);

            OCurrency sumToRefund = Math.Round(insuranceAmount.Value - earnedPremiumCurrencyValue.Value, roundPoint,
                                               MidpointRounding.AwayFromZero);

            CreditInsuranceEvent creditInsuranceEvent = new CreditInsuranceEvent
                                                 {
                                                     Code = "LCIP",
                                                     User = User.CurrentUser,
                                                     Commission = earnedPremiumCurrencyValue,
                                                     Principal = sumToRefund,
                                                     Cancelable = true,
                                                     TellerId = Teller.CurrentTeller.Id
                                                 };
            return creditInsuranceEvent;
        }

        private EventStock GenerateRepaymentEvents(CreditContractRepayment cCr, 
                                                   DateTime payDate, 
                                                   OCurrency penaltiesEvent,
                                                   OCurrency commissionsEvent, 
                                                   OCurrency interestEvent, 
                                                   OCurrency interestPrepayment, 
                                                   OCurrency principalEvent,
                                                   int pastDueDays, 
                                                   OPaymentType paymentType, 
                                                   bool isPending, 
                                                   int instNumber,
                                                   PaymentMethod paymentMethod,
                                                   bool overridePenalty,
                                                   OCurrency manualPenalty,
                                                   OCurrency manualInterestEvent,
                                                   ref RepaymentEvent totalEvent)
        {
            RepaymentEvent rpEvent;
            totalEvent.Penalties = 0;
            totalEvent.Commissions = 0;
            EventStock listOfLoanEvents = new EventStock();

            #region Event identification
            //////////////////////////////////////////////////////////////////////////////////////////////////////////
            if (cCr.PaidIstallments.Count > 0)
            {
                List<Installment> lateInstallments = cCr.PaidIstallments.FindAll(i => i.CalculatedPenalty > 0);
                Installment lastLate = 0 == lateInstallments.Count ? null : lateInstallments.OrderBy(i => i.Number).Last();
                
                foreach (Installment paidInstallment in cCr.PaidIstallments)
                {
                    int number = paidInstallment.Number;
                    OCurrency rpeCapital = 0;
                    OCurrency rpeInterests = 0;
                    OCurrency rpeCommission = 0;
                    OCurrency rpePenalty = 0;
                    int overdueDays = (payDate.Date - paidInstallment.ExpectedDate).Days > 0
                                          ? (payDate.Date - paidInstallment.ExpectedDate).Days
                                          : 0;
                    
                    foreach (RepaymentEvent rpe in Events.GetLoanRepaymentEvents())
                    {
                        if (rpe.InstallmentNumber == paidInstallment.Number && !rpe.Deleted)
                        {
                            if (rpe.RepaymentType != OPaymentType.PersonTotalPayment)
                            {
                                if ((rpe.RepaymentType != OPaymentType.PartialPayment) 
                                    && (rpe.RepaymentType != OPaymentType.ProportionalPayment))
                                {
                                    rpeCapital += rpe.Principal;
                                    rpeInterests += rpe.Interests;
                                }

                                if (rpe.RepaymentType == OPaymentType.PartialPayment && paymentType == OPaymentType.TotalPayment)
                                {
                                    rpeCapital += rpe.Principal;
                                    rpeInterests += rpe.Interests;
                                }

                                rpePenalty += rpe.Penalties;
                            }

                            rpeCommission += rpe.Commissions;
                            if (rpe.RepaymentType == OPaymentType.PersonTotalPayment && cCr.LoanOptions.ManualCommissionAmount != 0)
                                rpeCommission -= rpe.Commissions;
                        }
                    }

                    if (rpeCapital > paidInstallment.PaidCapital)
                        rpeCapital = 0;

                    if (rpeInterests < 0)
                        rpeInterests = 0;

                    if (rpeInterests > paidInstallment.InterestsRepayment)
                    {
                        rpeInterests = 0;
                    }

                    OCurrency principalAmount = paidInstallment.CapitalRepayment -
                                                (paidInstallment.CapitalRepayment - (paidInstallment.PaidCapital - rpeCapital));

                    OCurrency interestAmount = paidInstallment.InterestsRepayment -
                                               (paidInstallment.InterestsRepayment - (paidInstallment.PaidInterests - rpeInterests));

                    if (interestAmount < 0)
                        interestAmount = 0;

                    principalEvent -= principalAmount;
                    interestEvent -= interestAmount;

                    if (principalAmount == 0 
                        && interestAmount == 0
                        && paidInstallment.PaidCapital == 0 && manualInterestEvent == 0)
                    {
                        principalAmount = principalEvent;
                        interestAmount = interestEvent;
                    }

                    if (interestAmount < 0)
                        interestAmount = 0;

                    OCurrency commissionAmount = paidInstallment.CommissionsUnpaid -
                                               (paidInstallment.CommissionsUnpaid - (paidInstallment.PaidCommissions - rpeCommission));

                    OCurrency penaltyAmount;
                    if (overridePenalty)
                    {
                        if (lastLate != null && paidInstallment.CalculatedPenalty <= manualPenalty && paidInstallment.Number < lastLate.Number)
                        {
                            penaltyAmount = paidInstallment.CalculatedPenalty;
                            manualPenalty -= penaltyAmount;
                        }
                        else
                        {
                            penaltyAmount = manualPenalty;
                            manualPenalty = 0m;
                        }
                    }
                    else
                    {
                        penaltyAmount = paidInstallment.FeesUnpaid -
                                                  (paidInstallment.FeesUnpaid - (paidInstallment.PaidFees - rpePenalty));
                    }

                    //just to be sure that we do not have negative in the base
                    penaltyAmount = penaltyAmount < 0 ? 0 : penaltyAmount;
                    commissionAmount = commissionAmount < 0 ? 0 : commissionAmount;

                    if (overridePenalty && penaltyAmount > 0)
                    {
                        penaltyAmount = (-1.0) * penaltyAmount;
                    }
                    
                    rpEvent = CreateRepaymentEvent(number, 
                                                       payDate,
                                                       penaltyAmount,
                                                       commissionAmount,
                                                       interestAmount,
                                                       interestAmount,
                                                       principalAmount,
                                                       overdueDays,
                                                       _clientType == OClientTypes.Group, 
                                                       paymentType, 
                                                       isPending);

                    if (overridePenalty && penaltyAmount < 0)
                        penaltyAmount = (-1.0)*penaltyAmount;
                    rpEvent.PaymentMethod = paymentMethod;
                    rpEvent.CalculatedPenalties = paidInstallment.CalculatedPenalty;
                    if (rpEvent.CalculatedPenalties > penaltyAmount)
                    {
                        if (overridePenalty)
                        {
                            rpEvent.WrittenOffPenalties = rpEvent.CalculatedPenalties - penaltyAmount;
                        }
                        else
                        {
                            rpEvent.UnpaidPenalties = rpEvent.CalculatedPenalties - penaltyAmount;
                        }
                    }
                    totalEvent.Penalties += rpEvent.Penalties;
                    totalEvent.Commissions += rpEvent.Commissions;

                    if (rpEvent.InterestPrepayment != 0 
                        || rpEvent.Principal != 0 
                        || rpEvent.Penalties != 0 
                        || rpEvent.Commissions != 0)
                        listOfLoanEvents.Add(rpEvent);
                }
            }
            else
            {
                rpEvent = CreateRepaymentEvent(instNumber, 
                                               payDate, 
                                               penaltiesEvent, 
                                               commissionsEvent,
                                               interestEvent, 
                                               interestPrepayment, 
                                               principalEvent,
                                               pastDueDays,
                                               _clientType == OClientTypes.Group, 
                                               paymentType, 
                                               isPending);
                rpEvent.PaymentMethod = paymentMethod;
                // Calculate penalties
                Installment installment = GetInstallment(instNumber - 1);
                rpEvent.CalculatedPenalties = installment.CalculatedPenalty;
                if (rpEvent.CalculatedPenalties > penaltiesEvent)
                {
                    if (overridePenalty)
                    {
                        rpEvent.WrittenOffPenalties = rpEvent.CalculatedPenalties - penaltiesEvent;
                    }
                    else
                    {
                        rpEvent.UnpaidPenalties = rpEvent.CalculatedPenalties - penaltiesEvent;
                    }
                }
                totalEvent.Penalties += rpEvent.Penalties;
                totalEvent.Commissions += rpEvent.Commissions;
                listOfLoanEvents.Add(rpEvent);
            }
            #endregion

            return listOfLoanEvents;
        }

        public RepaymentEvent ConfirmPendingRepayment()
        {
            PendingRepaymentEvent pendingEvent = _events.GetEvents().Last() as PendingRepaymentEvent;
            RepaymentEvent rPe = pendingEvent.CopyAsRepaymentEvent();
                
            if (AllInstallmentsRepaid)
            {
                _closed = true;
                ContractStatus = OContractStatus.Closed;

                // check if Client has other 'active' loans if so, mark him as active client
                if (Project != null)
                    foreach (var loan in Project.Credits)
                        Project.Client.Active = loan.ContractStatus == OContractStatus.Active;
            }

            Events.Add(rPe);

            _installmentList.Sort((x, y) => x.ExpectedDate.CompareTo(y.ExpectedDate));

            foreach (Installment installment in InstallmentList)
            {
                //setup paid date for installments
                if (installment.IsRepaid && installment.Number > pendingEvent.InstallmentNumber)
                {
                    installment.IsPending = false;
                }
                else if (installment.Number == pendingEvent.InstallmentNumber)
                {
                    installment.IsPending = false;
                }
            }
            return rPe;
        }

        private RepaymentEvent CreateRepaymentEvent(int installmentNumber, 
                                                    DateTime pDate, 
                                                    OCurrency penaltiesEvent, 
                                                    OCurrency commissionsEvent,
                                                    OCurrency interestEvent, 
                                                    OCurrency interestPrepayment, 
                                                    OCurrency pincipalEvent, 
                                                    int overDueDays,
                                                    bool isGroup, 
                                                    OPaymentType repaymentType, 
                                                    bool pending)
        {
            //We need to generate specific RepaymentEvent in case of bad loan or rescheduled loan
            RepaymentEvent rPe = !_badLoan && Rescheduled && !_writtenOff
                                     ? new RescheduledLoanRepaymentEvent()
                                     : (_badLoan && !_writtenOff
                                        ? new BadLoanRepaymentEvent()
                                            : (_writtenOff ?
                                                new RepaymentOverWriteOffEvent()
                                                : new RepaymentEvent()));

            if ((overDueDays > 0 || penaltiesEvent > 0) && !_writtenOff)
                rPe = new BadLoanRepaymentEvent();
            penaltiesEvent = penaltiesEvent > 0 ? penaltiesEvent : (-1.0)*penaltiesEvent;
            //identification of pending event must be the last action
            if (pending) rPe = new PendingRepaymentEvent(rPe);

            rPe.PastDueDays = overDueDays;
            rPe.InstallmentNumber = installmentNumber;
            rPe.Date = pDate;
            rPe.Penalties = penaltiesEvent;
            rPe.Commissions = commissionsEvent;
            rPe.Interests = interestEvent;

            if (_product.LoanType == OLoanTypes.DecliningFixedPrincipalWithRealInterest 
                && repaymentType == OPaymentType.TotalPayment)
            {
                repaymentType = OPaymentType.TotalPayment;
            }
            else
            {
                if (InstallmentList[installmentNumber - 1].ExpectedDate <= pDate)
                {
                    repaymentType = OPaymentType.StandardPayment;
                }
            }

            rPe.RepaymentType = repaymentType;
            
            if(EscapedMember != null)
            {
                rPe.RepaymentType = OPaymentType.PersonTotalPayment;
            }

            rPe.InterestPrepayment = interestPrepayment;
            rPe.Principal = pincipalEvent;
            rPe.Cancelable = true;
            rPe.ClientType = isGroup ? OClientTypes.Group : OClientTypes.Person;

            // Test if the client has repaid the total principal, interests and penalties owed at date D
            //	In this case, the loan will be regraded to good loan and provision will be cancelled.
            if (_badLoan && !pending)
                _badLoan = _IsBadLoan(rPe, pDate);
            
            return rPe;
        }

        public AccruedInterestEvent CreateLoanInterestAccruingEvent(DateTime today)
        {
            OCurrency accruedAmount = 0;
            AccruedInterestEvent accruedInterestEvent = new AccruedInterestEvent {ClientType = _clientType};

            foreach (Installment installment in _installmentList)
            {
                if (installment.IsRepaid)
                {
                    accruedAmount += installment.InterestsRepayment;
                }
                else
                {
                    DateTime date = installment.Number == 1 ? _startDate : GetInstallment(installment.Number - 2).ExpectedDate;
                    int days = (today - date).Days;
                    accruedAmount += days >= DateTime.DaysInMonth(date.Year, date.Month)
                                             ? installment.InterestsRepayment
                                             : installment.InterestsRepayment * (double)days / (double)DateTime.DaysInMonth(date.Year, date.Month);

                    accruedAmount = UseCents ? Math.Round(accruedAmount.Value, 2) : Math.Round(accruedAmount.Value, 0);
                    break;
                }
            }

            OCurrency alreadyAccrued = 0;
            foreach (AccruedInterestEvent loanInterestAccruingEvent in Events.GetEventsByType(typeof(AccruedInterestEvent)))
            {
                alreadyAccrued += loanInterestAccruingEvent.AccruedInterest + loanInterestAccruingEvent.Interest;
            }

            accruedAmount -= alreadyAccrued;
            accruedInterestEvent.Interest = accruedAmount;
            accruedInterestEvent.AccruedInterest = accruedAmount;
            accruedInterestEvent.Rescheduled = _rescheduled;
            accruedInterestEvent.Date = today;
            return accruedInterestEvent;
        }

        /// <summary>
        /// This method sets contract to write off. The loan is definitely considered to loss when the loan is bad for more
        /// than 1 year.
        /// </summary>
        /// <param name="onDate"></param>
        /// <returns>A WriteOffEvent or null if :
        /// - past due days is lower than 365 days
        /// - no PastDueLoanEvent with past due days greater than 180 days were fired before
        /// - loan has already been degraded to loan loss
        /// </returns>
        public WriteOffEvent WriteOff(DateTime onDate)
        {
            if (!_writtenOff)
            {
                _writtenOff = true;
                ContractStatus = OContractStatus.WrittenOff;
                WriteOffEvent wOe = new WriteOffEvent
                {
                    Date = onDate,
                    ClientType = _clientType,
                    OLB = CalculateActualOlb(),
                    PastDueDays = _CalculatePastDue(onDate),
                    AccruedInterests = GetUnpaidInterest(onDate),
                    AccruedPenalties = GetUnpaidLatePenalties(onDate),
                    OverduePrincipal = CalculateOverduePrincipal(onDate),
                    IsFired = false,
                    Cancelable = true,
                    InstallmentNumber = GetLastNumberOfInstallments()
                };
                Events.Add(wOe);
                CreditInsuranceEvent lciw = GenerateCreditInsuranceEventForWriteOff(onDate);
                if (lciw!=null)
                    Events.Add(lciw);
                return wOe;
            }
            return null;
        }

        public CreditInsuranceEvent GenerateCreditInsuranceEventForWriteOff(DateTime onDate)
        {
            if (Insurance == 0) return null;
            //lciw ~ Loan Credit Insurance write-off
            CreditInsuranceEvent cieOrigination = Events.GetCreditInsuranceEvent();

            CreditInsuranceEvent lciw = new CreditInsuranceEvent
                                            {
                                                User = User.CurrentUser,
                                                Date = onDate,
                                                Principal = 0,
                                                Cancelable = true,
                                                Code = "LCIW",
                                                Commission = cieOrigination != null ? cieOrigination.Commission : 0
                                            };

            return lciw;
        }

        public CreditInsuranceEvent GetNotDeletedInsuranceWriteOff()
        {
            foreach (Event item in Events)
            {
                if (item is CreditInsuranceEvent)
                {
                    if (item.Code=="LCIW" && item.Deleted==false)
                    {
                        return item as CreditInsuranceEvent;
                    }
                }
            }
            return null;
        }

        private OCurrency _GetPaidCommissions()
        {
            OCurrency balance = 0;
            foreach (LoanDisbursmentEvent e in _events.GetDisbursmentEvents())
            {
                if (e.Commissions!=null)
                {
                    foreach (LoanEntryFeeEvent feeEvent in e.Commissions)
                    {
                        balance += feeEvent.Fee;
                    }
                }
            }
            foreach (RepaymentEvent e in _events.GetRepaymentEvents())
            {
                balance += e.Commissions;
            }
            return balance;
        }

        private OCurrency _GetPaidLatePenalties()
        {
            OCurrency balance = 0;
            foreach (RepaymentEvent e in _events.GetRepaymentEvents())
            {
                balance += e.Penalties;
            }
            return balance;
        }

        /// <summary>
        /// Sum of paid fees (late penalties + commission) in this loan
        /// It's the result of "GetPaidCommissions" and "GetPaidLatePenaties"
        /// </summary>
        public OCurrency GetPaidFees()
        {
            OCurrency balance = 0;
            balance += _GetPaidCommissions();
            balance += _GetPaidLatePenalties();

            return balance;
        }

        /// <summary>
        /// Sum of paid commissions (entry + anticipated fees) in this loan
        /// (result of "GetPaidCommissions" and "GetPaidLatePenaties" equal result of GetPaidFees method
        /// </summary>
        /// <returns></returns>
        public OCurrency GetPaidCommissions()
        {
            return _GetPaidCommissions();
        }

        /// <summary>
        /// Sum of paid late penalties in this loan
        /// (result of "GetPaidCommissions" and "GetPaidLatePenaties" equal result of GetPaidFees method
        /// </summary>
        /// <returns></returns>
        public OCurrency GetPaidLatePenalties()
        {
            return _GetPaidLatePenalties();
        }

        /// <summary>
        /// Sum of paid interest in this loan
        /// </summary>
        /// <returns></returns>
        public OCurrency GetPaidInterest()
        {
            OCurrency balance = 0;
            foreach (RepaymentEvent e in _events.GetRepaymentEvents())
            {
                balance += e.Interests;
            }
            return balance;
        }

        /// <summary>
        /// Sum of paid principal in this loan 
        /// </summary>
        /// <returns></returns>
        public OCurrency GetPaidPrincipal()
        {
            OCurrency balance = 0;
            foreach (RepaymentEvent e in _events.GetRepaymentEvents())
            {
                balance += e.Principal;
            }
            return balance;
        }

        public OCurrency GetOverduePrincipal(DateTime date)
        {
            OCurrency amount = 0;
            foreach (Installment installment in _installmentList)
            {
                if(installment.ExpectedDate <= date)
                {
                    amount += installment.CapitalRepayment;
                }

                if (installment.Number > 1)
                {
                    if (installment.ExpectedDate > date && GetInstallment(installment.Number - 2).ExpectedDate <= date)
                    {
                        int days = (date - GetInstallment(installment.Number - 2).ExpectedDate).Days;
                        int totalDays = NumberOfDaysInTheInstallment(installment.Number, date);
                        amount += installment.CapitalRepayment.Value * days / totalDays;
                    }
                }
            }

            amount = UseCents
                         ? Math.Round((amount - GetPaidPrincipal()).Value, 2, MidpointRounding.AwayFromZero)
                         : Math.Round((amount - GetPaidPrincipal()).Value, 0, MidpointRounding.AwayFromZero);

            return amount < 0 ? 0 : amount;
        }

        /// <summary>
        /// Get absolute past due days for this loan (Days since last none totally repaid installment)
        /// </summary>
        /// <param name="pToday"></param>
        /// <returns></returns>
        public int GetPastDueDays(DateTime pToday)
        {
            return _CalculatePastDue(pToday);
        }

        /// <summary>
        /// Get unpaid late penalties since first non totally repaid installment
        /// (based on initial amount, olb, overdue interest and overdue principal)
        /// </summary>
        /// <param name="pToday"></param>
        /// <returns></returns>
        public OCurrency GetUnpaidLatePenalties(DateTime pToday)
        {
            return CalculateNonRepaymentPenaltiesAmountForClosure(pToday);
        }

        public int GetLastNumberOfInstallments()
        {
            List<Event> sortedEvents = Events.GetSortedEvents();
            for (int i = sortedEvents.Count-1; i >=0 ; i--)
            {
                if (sortedEvents[i] is RepaymentEvent)
                    return sortedEvents[i].InstallmentNumber;
            }
            return 0;
        }

        /// <summary>
        /// Get unpaid interests since first non totally repaid installment
        /// </summary>
        /// <param name="pToday"></param>
        /// <returns></returns>
        public OCurrency GetUnpaidInterest(DateTime pToday)
        {
            OCurrency balance = 0;
            foreach (Installment installment in _installmentList)
            {
                if (installment.ExpectedDate > pToday)
                    break;
                balance += (installment.InterestsRepayment - installment.PaidInterests);
            }
            return balance;
        }

        public string Duration
        {
            get
            {
                return string.Format("{0:d} ({1})", NbOfInstallments, InstallmentType.Name);
            }
        }

        public Installment GetFirstUnpaidInstallment()
        {
            _installmentList.Sort((x, y) => x.ExpectedDate.CompareTo(y.ExpectedDate));
            for (int i = 0; i < NbOfInstallments; i++ )
            {
                Installment installment = GetInstallment(i);
                if (!installment.IsRepaid || (installment.IsRepaid && installment.IsPending)) return installment;
            }
            return null;
        }

        public LoanDisbursmentEvent GetNotDeletedDisbursementEvent()
        {
            List<LoanDisbursmentEvent> evnts = Events.GetDisbursmentEvents();
            foreach (LoanDisbursmentEvent evnt in evnts)
            {
                if (evnt.Deleted == false) return evnt;
            }
            return null;
        }

        public RepaymentEvent GetNotDeletedRepaymentEvent()
        {
            RepaymentEvent repaymentEvent = new RepaymentEvent();
            List<RepaymentEvent> evnts = Events.GetRepaymentEvents();
            foreach (RepaymentEvent evnt in evnts)
            {
                if (evnt.Deleted) continue;
                if (evnt.Date.Date > repaymentEvent.Date)
                    repaymentEvent = evnt;
            }
            return repaymentEvent;
        }

        public Installment GetLastFullyRepaidInstallment()
        {
            for (int i = InstallmentList.Count-1; i >= 0; i--)
            {
                if (InstallmentList[i].IsRepaid)
                {
                    return InstallmentList[i];
                }
            }
            return null;
        }

        public OCurrency CalculateDuePenaltiesForInstallment(int pNumber, DateTime pDate)
        {
            if (pNumber < 1) return 0;
            if (pNumber > NbOfInstallments) return 0;
            if (GetInstallment(pNumber - 1).IsRepaid) return 0;

            OCurrency retval = 0;

            retval += CalculationBaseForLateFees.FeesBasedOnOverdueInterest(this, pDate, pNumber, false,_generalSettings, _nwdS);
            retval += CalculationBaseForLateFees.FeesBasedOnOverduePrincipal(this, pDate, pNumber, false, _generalSettings, _nwdS);
            if (1 == pNumber || GetInstallment(pNumber - 2).IsRepaid)
            {
                retval += CalculationBaseForLateFees.FeesBasedOnInitialAmount(this, pDate, pNumber, false, _generalSettings, _nwdS);
                retval += CalculationBaseForLateFees.FeesBasedOnOlb(this, pDate, pNumber, false, _generalSettings, _nwdS);
            }
            return retval;
        }

        public void ResetLoanShares()
        {
            _loanShares.Clear();
        }

       public OCurrency GetSumOfLoanShares()
        {
            OCurrency retval = 0;
            foreach (LoanShare ls in _loanShares)
            {
                retval += ls.Amount;
            }
            return retval;
        }

        public List<LoanShare> LoanShares
        {
            get{return _loanShares;}
            set{_loanShares = value;}
        }

        public LoanProduct Product
        {
            get { return _product; }
            set 
            { 
                _product = value;
            }
        }

        public OCurrency GetTotalInterestDue()
        {
            OCurrency retval = 0;
            foreach (Installment i in _installmentList)
            {
                retval += i.InterestsRepayment;
            }
            return retval;
        }

        public Event GetLastNonDeletedEvent()
        {
            return Events.GetLastLoanNonDeletedEvent;
        }

        public IEnumerable<Installment> NonRepaidInstallments
        {
            get
            {
                foreach (Installment i in _installmentList)
                {
                    if (!i.IsRepaid) yield return i;
                }
            }
        }

        public Installment LastInstallment
        {
            get { return InstallmentList[InstallmentList.Count - 1]; }
        }

        public int NbOfDaysInTheInstallment
        {
            get
            {
                int Nb_Of_Days_In_The_Installment = 0;

                if (InstallmentType.NbOfMonths == 0)
                {
                    Nb_Of_Days_In_The_Installment = InstallmentType.NbOfDays;
                }
                else
                {
                    int NbOfMonths = 0;
                    do
                    {
                        Nb_Of_Days_In_The_Installment += DateTime.DaysInMonth(StartDate.Year, StartDate.AddMonths(NbOfMonths).Date.Month);
                        NbOfMonths++;
                    } while (InstallmentType.NbOfMonths > NbOfMonths);

                    Nb_Of_Days_In_The_Installment += InstallmentType.NbOfDays;
                }

                return Nb_Of_Days_In_The_Installment;
            }
        }

        public int NumberOfDaysInTheInstallment(int numberOfInstallment, DateTime pDate)
        {
            int nbOfDaysInTheInstallment = 0;

            if (InstallmentType.NbOfMonths == 0)
            {
                nbOfDaysInTheInstallment = InstallmentType.NbOfDays;
            }
            else
            {
                if (InstallmentType.NbOfMonths == 1)
                {
                    nbOfDaysInTheInstallment = DateTime.DaysInMonth(pDate.Year, pDate.Month);
                    nbOfDaysInTheInstallment += InstallmentType.NbOfDays;
                }
                else
                {
                    int nbOfMonths = 0;
                    do
                    {
                        nbOfDaysInTheInstallment += DateTime.DaysInMonth(StartDate.Year, StartDate.AddMonths(nbOfMonths).Date.Month);
                        nbOfMonths++;
                    } while (InstallmentType.NbOfMonths > nbOfMonths);

                    nbOfDaysInTheInstallment += InstallmentType.NbOfDays;
                }
            }

            return nbOfDaysInTheInstallment;
        }

        public bool UseCents
        {
            get
            {
                return Product.Currency.UseCents;
            }
        }

        public DateTime GetLastExpectedDateOfNotRepaidInstallment()
        {
            DateTime date = _installmentList[0].ExpectedDate;

            foreach (Installment installment in _installmentList)
            {
                if (installment.IsRepaid && installment.ExpectedDate > date)
                {
                    date = installment.ExpectedDate;
                }
            }

            return date;
        }

        public DateTime GetLastRepaymentDate()
        {
            var date = new DateTime();
            
            foreach (RepaymentEvent rPayment in Events.GetRepaymentEvents())
            {
                if(rPayment.Interests > 0  && rPayment.Deleted == false)
                {
                    if (rPayment.Date.Date > date.Date)
                        date = rPayment.Date.Date;
                }
            }
            return date;
        }

        public bool HasPendingInstallment
        {
            get
            {
                return _installmentList.FirstOrDefault(item => item.IsPending) != null;
            }
        }

        public bool FirstDateChanged
        {
            get
            {
                return StartDate != AlignDisbursementDate;
            }
        }

        public bool IsViewableBy(User user)
        {
            Debug.Assert(LoanOfficer != null, "Loan officer is null.");
            return LoanOfficer.Equals(user)
                ? true
                : user.HasAsSubordinate(LoanOfficer);
        }

        private static string GetLateLoanEventCode(OLoanLateStatus loanStatus, string eventCode)
        {
            string code = eventCode;

            switch (loanStatus)
            {
                case OLoanLateStatus.Good:
                    {
                        if (eventCode == "GLBL" || eventCode == "LLBL")
                        {
                            code = "BLGL";
                        }
                        else if (eventCode == "GLLL" || eventCode == "BLLL")
                        {
                            code = "LLGL";
                        }
                        break;
                    }
                case OLoanLateStatus.Late:
                    {
                        if (eventCode == "LLGL" || eventCode == "BLGL")
                        {
                            code = "GLLL";
                        }
                        else if (eventCode == "LLBL" || eventCode == "GLBL")
                        {
                            code = "BLLL";
                        }
                        break;
                    }
                case OLoanLateStatus.Bad:
                    {
                        if (eventCode == "GLLL" || eventCode == "BLLL")
                        {
                            code = "LLBL";
                        }
                        else if (eventCode == "LLGL" || eventCode == "BLGL")
                        {
                            code = "GLBL";
                        }
                        break;
                    }
            }
            return code;
        }

        public OverdueEvent AddRecheduleTransformationEvent(DateTime dateTime)
        {
            OverdueEvent ovEvent = null;
            bool toAddEvent = true;
            foreach (OverdueEvent dueEvent in Events.GetOverdueEvents())
            {
                if (dueEvent.Code == "GLRL" || dueEvent.Code == "BLRL" || dueEvent.Code == "LLRL")
                {
                    toAddEvent = false;
                }
            }

            if (toAddEvent)
            {
                foreach (Event reschEvent in Events)
                {
                    if (reschEvent is RescheduleLoanEvent)
                    {
                        ovEvent = new OverdueEvent
                                      {
                                          OLB = CalculateActualOlb(),
                                          OverdueDays = CalculatePastDueSinceLastRepayment(reschEvent.Date),
                                          OverduePrincipal = GetOverduePrincipal(reschEvent.Date),
                                          User = User.CurrentUser,
                                          Date = dateTime,
                                          Description = Code,
                                          ContracId = Id
                                      };
                        
                        if (ovEvent.OverdueDays > 0)
                        {
                            ovEvent.Code = ovEvent.OverdueDays > _generalSettings.BadLoanDays ? "BLRL" : "LLRL";
                        }
                        else
                        {
                            ovEvent.Code = "GLRL";
                        }
                    }
                }
            }

            if(ovEvent != null)
                Events.Add(ovEvent);

            return ovEvent;
        }

        public OverdueEvent GetOverdueEvent(DateTime dateTime)
        {
            int badLoanDays = _generalSettings.BadLoanDays;
            OLoanLateStatus loanStatus = OLoanLateStatus.Good;
            int lateDays = CalculatePastDueSinceLastRepayment(dateTime);
            OverdueEvent oEvent = new OverdueEvent { Id = 0, Code = "", ContracId = Id};

            if (lateDays > badLoanDays)
            {
                loanStatus = OLoanLateStatus.Bad;
                oEvent.Code = "GLBL";
            }
            else if (lateDays > 0)
            {
                loanStatus = OLoanLateStatus.Late;
                oEvent.Code = "GLLL";
            }

            Events.SortEventsByDate();
            List<OverdueEvent> overdueEvents = Events.GetOverdueEvents();

            //Get last overdue event if it's not exist it must be empty
            OverdueEvent lastEvent = new OverdueEvent {Code = ""};
            foreach (OverdueEvent dueEvent in overdueEvents)
            {
                if ((dueEvent.Date > lastEvent.Date) &&
                    (dueEvent.Code != "GLRL" && dueEvent.Code != "LLRL" && dueEvent.Code != "BLRL"))
                {
                    lastEvent = dueEvent;
                    oEvent = dueEvent;
                }
            }

            if (oEvent.Code != "")
            {
                OverdueEvent overdueEvent = new OverdueEvent
                {
                    OLB = CalculateActualOlb(),
                    OverdueDays = lateDays,
                    OverduePrincipal = GetOverduePrincipal(dateTime),
                    User = User.CurrentUser,
                    Date = dateTime,
                    Description = Code,
                    ContracId = Id
                };

                overdueEvent.Code = GetLateLoanEventCode(loanStatus, oEvent.Code);
                //do not double same event and same situation
                if (overdueEvent.Code != lastEvent.Code)
                {
                    //listEvents.Add(overdueEvent);
                    Events.Add(overdueEvent);
                    return overdueEvent;
                }
            }
            return null;
        }

        public ProvisionEvent GetProvisionEvent(DateTime dateTime, ProvisionTable provisionTable)
        {
            int lateDays = CalculatePastDueSinceLastRepayment(dateTime);
            OCurrency rate = 0;

            foreach (ProvisioningRate provisioningRate in provisionTable.ProvisioningRates)
            {
                if(lateDays >= provisioningRate.NbOfDaysMin && lateDays <= provisioningRate.NbOfDaysMax)
                {
                    rate = (decimal)provisioningRate.ProvisioningValue;
                }

                if(Rescheduled && provisioningRate.NbOfDaysMin  < 0 && provisioningRate.NbOfDaysMax < 0)
                {
                    rate = (decimal)provisioningRate.ProvisioningValue;
                }
            }
            
            ProvisionEvent provisionEvent = new ProvisionEvent
            {
                Id = 0,
                Code = "LLPE",
                Amount = GetOlb() * rate,
                OverdueDays = lateDays,
                Rate = rate,
                Date = TimeProvider.Now,
                User = User.CurrentUser,
                Description = Code,
                ContracId = Id
            };
            
            //check if we have a event for that date
            ProvisionEvent p = Events.GetProvisionEvents().Find(item => item.Date == provisionEvent.Date.Date 
                                                     && item.Amount == provisionEvent.Amount
                                                     && item.OverdueDays == provisionEvent.OverdueDays);
            

            if (provisionEvent.Amount > 0 && p == null)
            {
                Events.Add(provisionEvent);
                return provisionEvent;
            }
            return null;
        }

        public AccruedInterestEvent GetAccruedInterestEvent(DateTime currentDate)
        {
            AccruedInterestEvent accruedInterestEvent = new AccruedInterestEvent
                                                            {
                                                                User = User.CurrentUser,
                                                                Date = TimeProvider.Now,
                                                                Description = Code,
                                                                ContracId = Id
                                                            };

            OCurrency accruedAmount = 0;

            DateTime ceaseLateDate = InstallmentList[0].ExpectedDate;

            foreach (Installment installment in InstallmentList)
            {
                if (!installment.IsRepaid)
                {
                    ceaseLateDate = installment.ExpectedDate;
                    break;
                }
            }

            ceaseLateDate = ceaseLateDate.AddDays(_generalSettings.CeaseLateDays);

            foreach (Installment installment in InstallmentList)
            {
                if (installment.ExpectedDate <= currentDate)
                {
                    accruedAmount += installment.InterestsRepayment;
                }

                DateTime date = installment.Number == 1
                                        ? StartDate
                                        : GetInstallment(installment.Number - 2).ExpectedDate;

                //!installment.IsRepaid && 
                if (installment.ExpectedDate > currentDate && date <= currentDate)
                {
                    // do not calculate more then cease Late Days
                    if (ceaseLateDate < currentDate)
                        currentDate = ceaseLateDate;

                    int days = (currentDate - date).Days;
                    int daysInInstallment = installment.Number == 1
                                                      ? (installment.ExpectedDate - StartDate).Days
                                                      : (installment.ExpectedDate -
                                                         GetInstallment(installment.Number - 2).ExpectedDate)
                                                            .Days;

                    accruedAmount += days >= DateTime.DaysInMonth(date.Year, date.Month)
                                         ? installment.InterestsRepayment
                                         : installment.InterestsRepayment* (double) days /
                                           (double) daysInInstallment;

                    accruedInterestEvent.InstallmentNumber = installment.Number - 1;
                }
            }

            foreach (AccruedInterestEvent accruedEvent in Events.GetAccruedInterestEvents())
            {
                if (!accruedEvent.Deleted)
                    accruedAmount -= accruedEvent.AccruedInterest;
            }

            accruedInterestEvent.Interest = accruedAmount < 0 ? -1.0m * accruedAmount : 0;
            accruedAmount = accruedAmount < 0 ? 0 : accruedAmount;

            accruedAmount = UseCents
                                       ? Math.Round(accruedAmount.Value, 2)
                                       : Math.Round(accruedAmount.Value, 0);

            accruedInterestEvent.AccruedInterest = accruedAmount;
            accruedInterestEvent.Date = currentDate;
            accruedInterestEvent.Cancelable = true;

            if (accruedInterestEvent.AccruedInterest > 0)
            {
                Events.Add(accruedInterestEvent);
                return accruedInterestEvent;
            }

            return null;
        }

        public string ProductName
        {
            get { return Product.Name; }
        }
        public string ProductType
        {
            get { return Product.ProductType.ToString(); }
        }
        public decimal LoanAmount
        {
            get { return Amount.Value; }
        }
        public DateTime LastEventDate
        {
            get { return Events.GetLastLoanNonDeletedEvent.Date; }
        }
        public decimal OLB
        {
            get { return GetOlb().Value; }
        }

        public List<LoanEntryFee> LoanEntryFeesList { get; set; }

        public decimal Insurance { get; set; }

        private bool disposed;

        public void Dispose()
        {
            Dispose(true);
            // Take yourself off the Finalization queue 
            // to prevent finalization code for this object
            // from executing a second time.
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            // Check to see if Dispose has already been called.
            if (!this.disposed)
            {
                // If disposing equals true, dispose all managed 
                // and unmanaged resources.
                if (disposing)
                {
                    // Dispose managed resources.
                    Events = null;
                    InstallmentList = null;
                }
            }
            disposed = true;
        }

        public OCurrency GetOutstandingInterest()
        {
            OCurrency interest = 0;
            foreach (Installment i in InstallmentList)
            {
                if (i.IsRepaid) continue;
                interest += i.InterestsRepayment - i.PaidInterests;
            }
            return interest;
        }

        public LoanCloseEvent GetCloseEvent(DateTime date)
        {
            int? teller = 0;
            if (Teller.CurrentTeller != null && Teller.CurrentTeller.Id != 0)
                teller = Teller.CurrentTeller.Id;
            LoanCloseEvent loanCloseEvent =  new LoanCloseEvent
                       {
                           Date = date,
                           Cancelable = true,
                           User = _user,
                           Deleted = false,
                           TellerId = teller
                       };

            Events.Add(loanCloseEvent);
            return loanCloseEvent;
        }
    }
}
