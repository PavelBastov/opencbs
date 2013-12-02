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
using System.Linq;
using System.Collections.Generic;
using OpenCBS.CoreDomain;
using OpenCBS.CoreDomain.Contracts.Loans;
using OpenCBS.CoreDomain.Contracts.Loans.Installments;
using OpenCBS.CoreDomain.Contracts.Loans.LoanRepayment;
using OpenCBS.CoreDomain.Products;
using OpenCBS.Enums;
using NUnit.Framework;
using OpenCBS.CoreDomain.Events;
using OpenCBS.Shared;
using OpenCBS.CoreDomain.Accounting;
using OpenCBS.Shared.Settings;

namespace OpenCBS.Test.CoreDomain.Contracts.Loans
{
    /// <summary>
    /// Summary description for TestCreditContractRepayment.
    /// </summary>
    [TestFixture]
    public class TestCreditContractRepayment
    {
        private NonWorkingDateSingleton nonWorkingDateHelper;
        private ApplicationSettings dataParam;
        public ProvisionTable _provisionningTable;
		
        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            nonWorkingDateHelper = NonWorkingDateSingleton.GetInstance("");
            nonWorkingDateHelper.WeekEndDay1 = 6;
            nonWorkingDateHelper.WeekEndDay2 = 0;
            nonWorkingDateHelper.PublicHolidays = new Dictionary<DateTime, string>();
            nonWorkingDateHelper.PublicHolidays.Add(new DateTime(2006,1,1),"New Year Eve");
            nonWorkingDateHelper.PublicHolidays.Add(new DateTime(2006,3,8),"New Year Eve");
            nonWorkingDateHelper.PublicHolidays.Add(new DateTime(2006,3,21),"New Year Eve");
            nonWorkingDateHelper.PublicHolidays.Add(new DateTime(2006,3,22),"New Year Eve");
            nonWorkingDateHelper.PublicHolidays.Add(new DateTime(2006,5,1),"New Year Eve");
            nonWorkingDateHelper.PublicHolidays.Add(new DateTime(2006,5,9),"New Year Eve");
            nonWorkingDateHelper.PublicHolidays.Add(new DateTime(2006,6,27),"New Year Eve");
            nonWorkingDateHelper.PublicHolidays.Add(new DateTime(2006,9,9),"New Year Eve");
            nonWorkingDateHelper.PublicHolidays.Add(new DateTime(2006,11,6),"New Year Eve");
            nonWorkingDateHelper.PublicHolidays.Add(new DateTime(2006,11,26),"New Year Eve");
            nonWorkingDateHelper.PublicHolidays.Add(new DateTime(2006,1,6),"Christmas");

            _provisionningTable = ProvisionTable.GetInstance(new User());
            _provisionningTable.ProvisioningRates = new List<ProvisioningRate>();
            _provisionningTable.Add(new ProvisioningRate { Number = 1, NbOfDaysMin = 0, NbOfDaysMax = 0, ProvisioningValue = 0.02 });
            _provisionningTable.Add(new ProvisioningRate { Number = 2, NbOfDaysMin = 1, NbOfDaysMax = 30, ProvisioningValue = 0.1 });
            _provisionningTable.Add(new ProvisioningRate { Number = 3, NbOfDaysMin = 31, NbOfDaysMax = 60, ProvisioningValue = 0.25 });
            _provisionningTable.Add(new ProvisioningRate { Number = 4, NbOfDaysMin = 61, NbOfDaysMax = 90, ProvisioningValue = 0.5 });
            _provisionningTable.Add(new ProvisioningRate { Number = 5, NbOfDaysMin = 91, NbOfDaysMax = 180, ProvisioningValue = 0.75 });
            _provisionningTable.Add(new ProvisioningRate { Number = 6, NbOfDaysMin = 181, NbOfDaysMax = 365, ProvisioningValue = 1 });
            _provisionningTable.Add(new ProvisioningRate { Number = 7, NbOfDaysMin = 366, NbOfDaysMax = 99999, ProvisioningValue = 1 });

            dataParam = ApplicationSettings.GetInstance("");
            dataParam.DeleteAllParameters();
            
            dataParam.AddParameter(OGeneralSettings.CALCULATIONLATEFEESDURINGPUBLICHOLIDAYS, true);
            dataParam.AddParameter(OGeneralSettings.DONOTSKIPWEEKENDSININSTALLMENTSDATE, false);
            dataParam.AddParameter(OGeneralSettings.ACCOUNTINGPROCESS, OAccountingProcesses.Cash);
            dataParam.AddParameter(OGeneralSettings.INCREMENTALDURINGDAYOFF, true);
            dataParam.AddParameter(OGeneralSettings.PAYFIRSTINSTALLMENTREALVALUE, true);
            dataParam.AddParameter(OGeneralSettings.STOP_WRITEOFF_PENALTY, true);
            dataParam.AddParameter(OGeneralSettings.BAD_LOAN_DAYS, "180");
            dataParam.AddParameter(OGeneralSettings.INTEREST_RATE_DECIMAL_PLACES, 2);
        }

        [SetUp]
        public void SetUp()
        {
            dataParam.UpdateParameter(OGeneralSettings.CALCULATIONLATEFEESDURINGPUBLICHOLIDAYS, true);
            dataParam.UpdateParameter(OGeneralSettings.STOP_WRITEOFF_PENALTY, true);
        }

        #region private methods
        private static Loan _SetContract(OLoanTypes pLoansType, int pNumberOfInstallments, int pGracePeriod, NonRepaymentPenalties pNonRepaymentPenalties, bool pKeepExpectedInstallment,
                                         OAnticipatedRepaymentPenaltiesBases pAnticipatedBase, bool pBadLoan)
        {
            return _SetContract(pLoansType,pNumberOfInstallments, pGracePeriod, pNonRepaymentPenalties, pKeepExpectedInstallment, pAnticipatedBase, 0.01,pBadLoan);
        }

        private static Loan _SetContract(OLoanTypes pLoansType, int pNumberOfInstallments, int pGracePeriod, NonRepaymentPenalties pNonRepaymentPenalties, bool pKeepExpectedInstallment,
                                         OAnticipatedRepaymentPenaltiesBases pAnticipatedBase, double pAnticipatedPenalties, bool pBadLoan)
        {
            LoanProduct package = new LoanProduct
                                      {
                                          InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
                                          LoanType = pLoansType,
                                          ChargeInterestWithinGracePeriod = true,
                                          Currency = new Currency {Id = 1, UseCents = true},
                                          KeepExpectedInstallment = pKeepExpectedInstallment,
                                          AnticipatedTotalRepaymentPenaltiesBase = pAnticipatedBase,
                                          RoundingType = ORoundingType.Approximate
                                      };

            Loan myContract = new Loan(package, 1000, 0.03m, pNumberOfInstallments, pGracePeriod, new DateTime(2006, 1, 1), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()))
                                  {
                                      BadLoan = pBadLoan,
                                      AnticipatedTotalRepaymentPenalties = pAnticipatedPenalties,
                                      NonRepaymentPenalties = pNonRepaymentPenalties
                                  };

            return myContract;
        }

        private static CreditContractRepayment _SetRepaymentOptions(Loan pContract, DateTime pDate, int pInstallmentNumber, bool pCancelFees, OCurrency pManualFeesAmount, OCurrency pManualCommissionAmount, bool pCancelInterest, OCurrency pManualInterestAmount)
        {
            CreditContractOptions cCO = new CreditContractOptions(pContract.Product.LoanType, pContract.Product.KeepExpectedInstallment, 
                pCancelFees, pManualFeesAmount, pManualCommissionAmount, 
                pCancelInterest, pManualInterestAmount, pContract.Product.AnticipatedTotalRepaymentPenaltiesBase);

            return new CreditContractRepayment(pContract, cCO, pDate, pInstallmentNumber, new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""));
        }

        private static void _AssertMaximumAmount(OCurrency pMaxExpectedToRegrad, OCurrency pMaxActualToRegrade, OCurrency pMaxExpectedToRepay,
                                                 OCurrency pMaxActualToRepay, OCurrency pMaxExpectedToRepayInstallment, OCurrency pMaxActualToRepayInstallment)
        {
            Assert.AreEqual(pMaxExpectedToRegrad.Value, Math.Round(pMaxActualToRegrade.Value, 2));
            Assert.AreEqual(pMaxExpectedToRepay.Value, Math.Round(pMaxActualToRepay.Value, 2));
            Assert.AreEqual(pMaxExpectedToRepayInstallment.Value, Math.Round(pMaxActualToRepayInstallment.Value, 2));
        }

        private static void _AssertRepayEvent(OCurrency pActualInterest, OCurrency pExpectedInterest, OCurrency pActualPrincipal, OCurrency pExpectedPrincipal, OCurrency pActualAmountPaid, OCurrency pExpectedAmountPaid)
        {
            Assert.AreEqual(pExpectedInterest.Value, pActualInterest.Value);
            Assert.AreEqual(pExpectedPrincipal.Value, pActualPrincipal.Value);
            Assert.AreEqual(pExpectedAmountPaid.Value, pActualAmountPaid.Value);
        }

        private static void _AssertSpecifiedInstallment(Installment pInstallment, OCurrency pInterestRepayment, OCurrency pCapitalRepayment, OCurrency pPaidCapital, OCurrency pPaidInterest)
        {
            Assert.AreEqual(pInterestRepayment.Value, pInstallment.InterestsRepayment.Value);
            Assert.AreEqual(pCapitalRepayment.Value, pInstallment.CapitalRepayment.Value);
            Assert.AreEqual(pPaidCapital.Value, pInstallment.PaidCapital.Value);
            Assert.AreEqual(pPaidInterest.Value, pInstallment.PaidInterests.Value);
        }

        #endregion

        [Test]
        public void AccrualMode_RepaymentOneWeekAfterDisbursment()
        {
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.ACCOUNTINGPROCESS, OAccountingProcesses.Accrual);

            Loan myContract = _SetContract(OLoanTypes.Flat,5, 0, new NonRepaymentPenalties(0, 0.001, 0, 0), true, OAnticipatedRepaymentPenaltiesBases.RemainingOLB,false);
            myContract.Disburse(new DateTime(2006, 1, 1), true, false);

            RepaymentEvent rPE = myContract.Repay(1, new DateTime(2006, 1, 7), 230, false, true);
            _AssertRepayEvent(0, rPE.Fees, 30, rPE.Interests, 200, rPE.Principal);
                  

            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.ACCOUNTINGPROCESS, OAccountingProcesses.Cash);
        }

        [Test]
        public void CalculateMaximumAmount_Flat_BadLoan_42DaysLate_InitialAmount_RemainingOLB_KeepExpectedInstallment()
        {
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.CALCULATIONLATEFEESDURINGPUBLICHOLIDAYS, true);
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.ACCOUNTINGPROCESS, OAccountingProcesses.Cash);
            Loan myContract = _SetContract(OLoanTypes.Flat, 6, 1, new NonRepaymentPenalties(0.003, 0, 0, 0), true, OAnticipatedRepaymentPenaltiesBases.RemainingOLB, true);

            CreditContractRepayment cCR = _SetRepaymentOptions(myContract, new DateTime(2006, 3, 15), 1, false, 0, 0, false, 0);
            
            _AssertMaximumAmount(386, cCR.MaximumAmountToRegradingLoan.Value, 1306, cCR.MaximumAmountAuthorizeToRepay, 156, cCR.AmountToRepayInstallment);

            RepaymentEvent rPE = myContract.Repay(1, new DateTime(2006, 3, 15), 386, false, true); //regrading loan
            _AssertRepayEvent(126, rPE.Fees, 60, rPE.Interests, 200, rPE.Principal);

            _AssertSpecifiedInstallment(myContract.GetInstallment(0), 30, 0, 0, 30);
            _AssertSpecifiedInstallment(myContract.GetInstallment(1), 30, 200, 200, 30);
            _AssertSpecifiedInstallment(myContract.GetInstallment(2), 30, 200, 0, 0);
            _AssertSpecifiedInstallment(myContract.GetInstallment(3), 30, 200, 0, 0);
            _AssertSpecifiedInstallment(myContract.GetInstallment(4), 30, 200, 0, 0);
            _AssertSpecifiedInstallment(myContract.GetInstallment(5), 30, 200, 0, 0);


            CreditContractRepayment cCR2 = _SetRepaymentOptions(myContract, new DateTime(2006, 4, 3), 3, false, 0, 0, false, 0);
            _AssertMaximumAmount(230, cCR2.MaximumAmountToRegradingLoan, 920, cCR2.MaximumAmountAuthorizeToRepay, 230, cCR2.AmountToRepayInstallment);

            RepaymentEvent rPE2 = myContract.Repay(3, new DateTime(2006, 4, 3), 920, false, true);
            _AssertRepayEvent(0, rPE2.Fees, 120, rPE2.Interests, 800, rPE2.Principal);

            _AssertSpecifiedInstallment(myContract.GetInstallment(0), 30, 0, 0, 30);
            _AssertSpecifiedInstallment(myContract.GetInstallment(1), 30, 200, 200, 30);
            _AssertSpecifiedInstallment(myContract.GetInstallment(2), 30, 200, 200, 30);
            _AssertSpecifiedInstallment(myContract.GetInstallment(3), 30, 200, 200, 30);
            _AssertSpecifiedInstallment(myContract.GetInstallment(4), 30, 200, 200, 30);
            _AssertSpecifiedInstallment(myContract.GetInstallment(5), 30, 200, 200, 30);
        }

        [Test]
        public void CalculateOLB_BeforeRepayment()
        {
            Loan myContract = _SetContract(OLoanTypes.Flat, 6, 1, new NonRepaymentPenalties(0.003, 0, 0, 0), true, OAnticipatedRepaymentPenaltiesBases.RemainingOLB, true);

            foreach (Installment installment in myContract.InstallmentList)
            {
                installment.OLB = myContract.CalculateExpectedOlb(installment.Number,true);
            }

            Assert.AreEqual(1000m, myContract.GetInstallment(0).OLB.Value);
            Assert.AreEqual(1000m, myContract.GetInstallment(1).OLB.Value);
            Assert.AreEqual(800m, myContract.GetInstallment(2).OLB.Value);
            Assert.AreEqual(600m, myContract.GetInstallment(3).OLB.Value);
            Assert.AreEqual(400m, myContract.GetInstallment(4).OLB.Value);
            Assert.AreEqual(200m, myContract.GetInstallment(5).OLB.Value);
        }

        [Test]
        public void SmartRepayment_partialInterestRepayment()
        {
            Loan myContract = _SetContract(OLoanTypes.DecliningFixedInstallments, 6, 0, new NonRepaymentPenalties(0.003, 0, 0, 0), true, OAnticipatedRepaymentPenaltiesBases.RemainingOLB, true);
            myContract.Disburse(new DateTime(2006, 1, 1), true, false);

            Assert.AreEqual(1000, myContract.InstallmentList.Sum(pItem => pItem.CapitalRepayment.Value));

            _AssertSpecifiedInstallment(myContract.GetInstallment(0), 1, new DateTime(2006, 2, 1),     30, 154.60m, 1000, 0, 0);
            _AssertSpecifiedInstallment(myContract.GetInstallment(1), 2, new DateTime(2006, 3, 1), 25.36m, 159.24m, 845.40m, 0, 0);
            _AssertSpecifiedInstallment(myContract.GetInstallment(2), 3, new DateTime(2006, 4, 3), 20.58m, 164.02m, 686.16m, 0, 0);
            _AssertSpecifiedInstallment(myContract.GetInstallment(3), 4, new DateTime(2006, 5, 2), 15.66m, 168.93m, 522.14m, 0, 0);
            _AssertSpecifiedInstallment(myContract.GetInstallment(4), 5, new DateTime(2006, 6, 1), 10.60m,     173.99m, 353.21m, 0, 0);
            _AssertSpecifiedInstallment(myContract.GetInstallment(5), 6, new DateTime(2006, 7, 3),  5.38m, 179.22m, 179.22m, 0, 0);

            myContract.Repay(1, new DateTime(2006, 1, 15), 20, false, true);
            Assert.AreEqual(1000, myContract.InstallmentList.Sum(pItem => pItem.CapitalRepayment.Value));

            _AssertSpecifiedInstallment(myContract.GetInstallment(0), 1, new DateTime(2006, 2, 1), 30, 154.60m, 1000, 0, 20);
            _AssertSpecifiedInstallment(myContract.GetInstallment(1), 2, new DateTime(2006, 3, 1), 25.36m, 159.24m, 845.40m, 0, 0);
            _AssertSpecifiedInstallment(myContract.GetInstallment(2), 3, new DateTime(2006, 4, 3), 20.58m, 164.02m, 686.16m, 0, 0);
            _AssertSpecifiedInstallment(myContract.GetInstallment(3), 4, new DateTime(2006, 5, 2), 15.66m, 168.93m, 522.14m, 0, 0);
            _AssertSpecifiedInstallment(myContract.GetInstallment(4), 5, new DateTime(2006, 6, 1), 10.60m, 173.99m, 353.21m, 0, 0);
            _AssertSpecifiedInstallment(myContract.GetInstallment(5), 6, new DateTime(2006, 7, 3), 5.38m, 179.22m, 179.22m, 0, 0);

            myContract.Repay(1, new DateTime(2006, 2, 1), 164.60m, false, true);
            Assert.AreEqual(1000, myContract.InstallmentList.Sum(pItem => pItem.CapitalRepayment.Value));

            _AssertSpecifiedInstallment(myContract.GetInstallment(0), 1, new DateTime(2006, 2, 1), 30, 154.60m, 1000, 154.60m, 30);
            _AssertSpecifiedInstallment(myContract.GetInstallment(1), 2, new DateTime(2006, 3, 1), 25.36m, 159.24m, 845.40m, 0, 0);
            _AssertSpecifiedInstallment(myContract.GetInstallment(2), 3, new DateTime(2006, 4, 3), 20.58m, 164.02m, 686.16m, 0, 0);
            _AssertSpecifiedInstallment(myContract.GetInstallment(3), 4, new DateTime(2006, 5, 2), 15.66m, 168.93m, 522.14m, 0, 0);
            _AssertSpecifiedInstallment(myContract.GetInstallment(4), 5, new DateTime(2006, 6, 1), 10.60m, 173.99m, 353.21m, 0, 0);
            _AssertSpecifiedInstallment(myContract.GetInstallment(5), 6, new DateTime(2006, 7, 3), 5.38m, 179.22m, 179.22m, 0, 0);

        }

        [Test]
        public void CalculateOLBAfterRepayment()
        {
            Loan myContract = _SetContract(OLoanTypes.Flat, 6, 1, new NonRepaymentPenalties(0.003, 0, 0, 0), true, OAnticipatedRepaymentPenaltiesBases.RemainingOLB, true);

            foreach (Installment installment in myContract.InstallmentList)
            {
                installment.OLB = myContract.CalculateExpectedOlb(installment.Number, true);
            }

            Assert.AreEqual(1000m, myContract.GetInstallment(0).OLBAfterRepayment.Value);
            Assert.AreEqual(800m, myContract.GetInstallment(1).OLBAfterRepayment.Value);
            Assert.AreEqual(600m, myContract.GetInstallment(2).OLBAfterRepayment.Value);
            Assert.AreEqual(400m, myContract.GetInstallment(3).OLBAfterRepayment.Value);
            Assert.AreEqual(200m, myContract.GetInstallment(4).OLBAfterRepayment.Value);
            Assert.AreEqual(0m, myContract.GetInstallment(5).OLBAfterRepayment.Value);
        }

        [Test]
        public void Flat_BadLoan_42DaysLate_InitialAmount_RemainingOLB_KeepExpectedInstallment()
        {
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.CALCULATIONLATEFEESDURINGPUBLICHOLIDAYS, true);
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.ACCOUNTINGPROCESS, OAccountingProcesses.Cash);

            Loan myContract = _SetContract(OLoanTypes.Flat, 6, 1, new NonRepaymentPenalties(0.003, 0, 0, 0), true, OAnticipatedRepaymentPenaltiesBases.RemainingOLB, true);

            CreditContractRepayment cCR = _SetRepaymentOptions(myContract, new DateTime(2006, 3, 15), 1, false, 0, 0, false, 0);
            _AssertMaximumAmount(386m, cCR.MaximumAmountToRegradingLoan, 1306, cCR.MaximumAmountAuthorizeToRepay, 156, cCR.AmountToRepayInstallment);

            RepaymentEvent rPE = myContract.Repay(1, new DateTime(2006, 3, 15), 16, false, true);
            _AssertRepayEvent(16m, rPE.Fees, 0, rPE.Interests, 0, rPE.Principal); //110 => feesUnpaid

            CreditContractRepayment cCR2 = _SetRepaymentOptions(myContract, new DateTime(2006, 3, 20), 1, false, 0, 0, false, 0);
            _AssertMaximumAmount(385m, cCR2.MaximumAmountToRegradingLoan, 1305, cCR2.MaximumAmountAuthorizeToRepay, 155, cCR2.AmountToRepayInstallment);

            RepaymentEvent rPE2 = myContract.Repay(1, new DateTime(2006, 3, 20), 136, false, true);
            _AssertRepayEvent(125m, rPE2.Fees, 11, rPE2.Interests, 0, rPE2.Principal);//15
        }

        [Test]
        public void Flat_BadLoan_42DaysLate_InitialAmount_RemainingInterest_KeepExpectedInstallment()
        {
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.CALCULATIONLATEFEESDURINGPUBLICHOLIDAYS, true);
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.ACCOUNTINGPROCESS, OAccountingProcesses.Cash);
            
            Loan myContract = _SetContract(OLoanTypes.Flat, 6, 1, new NonRepaymentPenalties(0.003, 0, 0, 0), true, OAnticipatedRepaymentPenaltiesBases.RemainingInterest, true);

            CreditContractRepayment cCR = _SetRepaymentOptions(myContract, new DateTime(2006, 3, 15), 1, false, 0, 0, false, 0);
            _AssertMaximumAmount(386m, cCR.MaximumAmountToRegradingLoan, 1306, cCR.MaximumAmountAuthorizeToRepay, 156, cCR.AmountToRepayInstallment);
			
            RepaymentEvent rPE = myContract.Repay(1,new DateTime(2006,3,15),386,false,true);
            _AssertRepayEvent(126m, rPE.Fees, 60, rPE.Interests, 200, rPE.Principal);

            CreditContractRepayment cCR2 = _SetRepaymentOptions(myContract, new DateTime(2006, 4, 3), 3, false, 0, 0, false, 0);
            _AssertMaximumAmount(230, cCR2.MaximumAmountToRegradingLoan, 920, cCR2.MaximumAmountAuthorizeToRepay, 230, cCR2.AmountToRepayInstallment);

            RepaymentEvent rPE2 = myContract.Repay(3, new DateTime(2006, 4, 3), 920, false, true);
            _AssertRepayEvent(0m, rPE2.Fees, 120, rPE2.Interests, 800, rPE2.Principal);
        }

        [Test]
        public void Flat_BadLoan_42dayslate_InitialAmount_KeepExpectedInstallment_CancelFees()
        {
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.CALCULATIONLATEFEESDURINGPUBLICHOLIDAYS, true);
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.ACCOUNTINGPROCESS, OAccountingProcesses.Cash);
            
            Loan myContract = _SetContract(OLoanTypes.Flat, 6, 1, new NonRepaymentPenalties(0.003, 0, 0, 0), true, OAnticipatedRepaymentPenaltiesBases.RemainingOLB, true);

            CreditContractRepayment cCR = _SetRepaymentOptions(myContract, new DateTime(2006, 3, 15), 1, true, 0, 0, false, 0);
            _AssertMaximumAmount(260, cCR.MaximumAmountToRegradingLoan.Value, 1180, cCR.MaximumAmountAuthorizeToRepay, 30, cCR.AmountToRepayInstallment);
			
            RepaymentEvent rPE = myContract.Repay(1,new DateTime(2006,3,15),260,true,true);
            _AssertRepayEvent(0, rPE.Fees, 60, rPE.Interests, 200, rPE.Principal);

            CreditContractRepayment cCR2 = _SetRepaymentOptions(myContract, new DateTime(2006, 4, 3), 3, true, 0, 0, false, 0);
            _AssertMaximumAmount(230m, cCR2.MaximumAmountToRegradingLoan, 920, cCR2.MaximumAmountAuthorizeToRepay, 230, cCR2.AmountToRepayInstallment.Value);
			
            RepaymentEvent rPE2 = myContract.Repay(3,new DateTime(2006,4,3),920,true,true);
            _AssertRepayEvent(0, rPE2.Fees, 120, rPE2.Interests, 800, rPE2.Principal);
        }

        [Test]
        public void TestRepayContract()
        {
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.ACCOUNTINGPROCESS, OAccountingProcesses.Cash);

            Loan myContract = _SetContract(OLoanTypes.Flat, 6, 1, new NonRepaymentPenalties(0, 0, 0, 0), true, OAnticipatedRepaymentPenaltiesBases.RemainingInterest, true);

            CreditContractRepayment cCR = _SetRepaymentOptions(myContract, new DateTime(2006, 2, 1), 1, false, 0, 0, false, 0);
            _AssertMaximumAmount(30m, cCR.MaximumAmountToRegradingLoan.Value, 1180, cCR.MaximumAmountAuthorizeToRepay, 30, cCR.AmountToRepayInstallment.Value);

            RepaymentEvent rPE = myContract.Repay(1, new DateTime(2006, 3, 15), 30, true, false);
            _AssertRepayEvent(0, rPE.Fees.Value, 30, rPE.Interests, 0, rPE.Principal);

            CreditContractRepayment cCR2 = _SetRepaymentOptions(myContract, new DateTime(2006, 3, 3), 2, false, 0, 0, false, 0);
            _AssertMaximumAmount(230m, cCR2.MaximumAmountToRegradingLoan, 1150m, cCR2.MaximumAmountAuthorizeToRepay, 230, cCR2.AmountToRepayInstallment.Value);
        }

        [Test]
        public void TestRepayContract3()
        {
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.ACCOUNTINGPROCESS, OAccountingProcesses.Accrual);

            Loan myContract = _SetContract(OLoanTypes.Flat, 6, 1, new NonRepaymentPenalties(0, 0, 0, 0), false, OAnticipatedRepaymentPenaltiesBases.RemainingOLB, 0, true);

            RepaymentEvent rPE = myContract.Repay(1, new DateTime(2006, 2, 1), 510, false, false);
            Assert.AreEqual(0, rPE.Fees.Value);
            Assert.AreEqual(30, rPE.Interests.Value);
            Assert.AreEqual(480, rPE.Principal.Value);

            RepaymentEvent rPE2 = myContract.Repay(2, new DateTime(2006, 3, 1), 230, false, false);
            Assert.AreEqual(0, rPE2.Fees.Value);
            Assert.AreEqual(15.6m, rPE2.Interests.Value);
            Assert.AreEqual(214.4m, rPE2.Principal.Value);
        }

        [Test]
        public void TestCalculateAmountToRegradingLoanWithFlateBadLoanWith42dayslateWhenNonRepaymentFeesBaseOnInitialAmountAndKeepExpectedInstallmentAndCancelFeesSetToTrue()
        {
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.CALCULATIONLATEFEESDURINGPUBLICHOLIDAYS, true);
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.ACCOUNTINGPROCESS, OAccountingProcesses.Cash);

            LoanProduct package = new LoanProduct
                                      {
                                          InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
                                          LoanType = OLoanTypes.Flat,
                                          ChargeInterestWithinGracePeriod = true,
                                          Currency = new Currency { Id = 1 }
                                      };
            package.KeepExpectedInstallment = true;
            package.AnticipatedTotalRepaymentPenaltiesBase = OAnticipatedRepaymentPenaltiesBases.RemainingOLB;
            Loan myContract = new Loan(package, 1000, 0.03m, 6, 1, new DateTime(2006, 1, 1), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));
            myContract.BadLoan = true;
            myContract.NonRepaymentPenalties.InitialAmount = 0.003;
            myContract.AnticipatedTotalRepaymentPenalties = 0.01;

            CreditContractOptions cCO = new CreditContractOptions(package.LoanType, package.KeepExpectedInstallment, true, 0, 0, false, 0,
                                                                  package.AnticipatedTotalRepaymentPenaltiesBase);
            CreditContractRepayment cCR = new CreditContractRepayment(myContract, cCO, new DateTime(2006, 3, 15), 1, new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""));

            Assert.AreEqual(260, Math.Round(cCR.MaximumAmountToRegradingLoan.Value, 2));
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.ACCOUNTINGPROCESS, OAccountingProcesses.Accrual);
        }

        [Test]
        public void RepayTotallyFlat_BadLoan_42dayslate_OnInitialAmount_KeepExpectedInstallment()
        {
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.CALCULATIONLATEFEESDURINGPUBLICHOLIDAYS, true);
            LoanProduct package = new LoanProduct
                                      {
                                          InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
                                          LoanType = OLoanTypes.Flat,
                                          ChargeInterestWithinGracePeriod = true,
                                          Currency = new Currency { Id = 1 }
                                      };
            package.KeepExpectedInstallment = true;
            package.AnticipatedTotalRepaymentPenaltiesBase = OAnticipatedRepaymentPenaltiesBases.RemainingOLB;
            Loan myContract = new Loan(package, 1000, 0.03m, 6, 1, new DateTime(2006, 1, 1), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));
            myContract.BadLoan = true;
            myContract.NonRepaymentPenalties.InitialAmount = 0.003;
            myContract.AnticipatedTotalRepaymentPenalties = 0.01;

            CreditContractOptions cCO = new CreditContractOptions(package.LoanType, package.KeepExpectedInstallment, false, 0, 0, false, 0, package.AnticipatedTotalRepaymentPenaltiesBase);
            CreditContractRepayment cCR = new CreditContractRepayment(myContract, cCO, new DateTime(2006, 3, 15), 1, new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""));
            Assert.AreEqual(1306, Math.Round(cCR.MaximumAmountAuthorizeToRepay.Value, 2));

            RepaymentEvent bLRE = myContract.Repay(1, new DateTime(2006, 3, 15), 1306, false, true);
            Assert.AreEqual(126, bLRE.Fees.Value);
            Assert.AreEqual(180, bLRE.Interests.Value);
            Assert.AreEqual(1000, bLRE.Principal.Value);
        }

        [Test]
        public void TestCalculateMaximumAmountWithFlateBadLoanWith42dayslateWhenNonRepaymentFeesBaseOnInitialAmountAndKeepNotExpectedInstallment()
        {
            Assert.Ignore();
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.CALCULATIONLATEFEESDURINGPUBLICHOLIDAYS, true);
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.ACCOUNTINGPROCESS, OAccountingProcesses.Accrual);
            
            LoanProduct package = new LoanProduct
                                      {
                                          InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
                                          LoanType = OLoanTypes.Flat,
                                          ChargeInterestWithinGracePeriod = true,
                                          Currency = new Currency { Id = 1 }
                                      };
            
            package.KeepExpectedInstallment = false;
            package.AnticipatedTotalRepaymentPenaltiesBase = OAnticipatedRepaymentPenaltiesBases.RemainingOLB;
            
            Loan myContract = new Loan(package, 1000, 0.03m, 6, 1, new DateTime(2006, 1, 1), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));
            myContract.BadLoan = true;
            myContract.NonRepaymentPenalties.InitialAmount = 0.003;
            myContract.AnticipatedTotalRepaymentPenalties = 0.01;

            CreditContractOptions cCO = new CreditContractOptions(package.LoanType, package.KeepExpectedInstallment, false, 0, 0, false, 0,
                                                                  package.AnticipatedTotalRepaymentPenaltiesBase, false, false);
            
            CreditContractRepayment cCR = new CreditContractRepayment(myContract, cCO, new DateTime(2006, 3, 15), 1, new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""));

            Assert.AreEqual(1194, Math.Round(cCR.MaximumAmountAuthorizeToRepay.Value, 2));
            Assert.AreEqual(386.00m, Math.Round(cCR.MaximumAmountToRegradingLoan.Value, 2));
            RepaymentEvent rPE = myContract.Repay(1, new DateTime(2006, 3, 15), 386.00m, false, false);
            Assert.AreEqual(0m, rPE.Fees.Value);
            Assert.AreEqual(30m, rPE.Interests.Value);
            Assert.AreEqual(356m, rPE.Principal.Value);

            CreditContractRepayment cCR2 = new CreditContractRepayment(myContract, cCO, new DateTime(2006, 4, 3), 3, new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""));
            Assert.AreEqual(932.09, Math.Round(cCR2.MaximumAmountAuthorizeToRepay.Value, 2));

            RepaymentEvent rPE2 = myContract.Repay(1, new DateTime(2006, 4, 3), 836, false, false);
            Assert.AreEqual(4, rPE2.Fees.Value);
            Assert.AreEqual(90, rPE2.Interests.Value);
            Assert.AreEqual(742, rPE2.Principal.Value);

            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.ACCOUNTINGPROCESS, OAccountingProcesses.Cash);
        }

        [Test]
        public void TestCalculateEventWhenRepayTotallyFlateBadLoanWith42dayslateWhenNonRepaymentFeesBaseOnInitialAmountAndKeepNotExpectedInstallment()
        {
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.CALCULATIONLATEFEESDURINGPUBLICHOLIDAYS, true);
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.ACCOUNTINGPROCESS, OAccountingProcesses.Accrual);

            LoanProduct package = new LoanProduct
                                      {
                                          InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
                                          LoanType = OLoanTypes.Flat,
                                          ChargeInterestWithinGracePeriod = true,
                                          Currency = new Currency { Id = 1, UseCents = true }
                                      };
            package.KeepExpectedInstallment = false;
            package.AnticipatedTotalRepaymentPenaltiesBase = OAnticipatedRepaymentPenaltiesBases.RemainingOLB;
            Loan myContract = new Loan(package, 1000, 0.03m, 6, 1, new DateTime(2006, 1, 1), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));
            myContract.BadLoan = true;
            myContract.NonRepaymentPenalties.InitialAmount = 0.003;
            myContract.AnticipatedTotalRepaymentPenalties = 0.01;

            RepaymentEvent bLRE = myContract.Repay(1, new DateTime(2006, 3, 15), 1206.73m, false, false);

            Assert.AreEqual(134, bLRE.Fees.Value);
            Assert.AreEqual(72.73m, bLRE.Interests.Value);
            Assert.AreEqual(1000m, bLRE.Principal.Value);
        }

        [Test]
        public void TestCalculateMaximumAmountWithFlateBadLoanWith42dayslateWhenNonRepaymentFeesBaseOnOLBAndKeepExpectedInstallment()
        {
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.CALCULATIONLATEFEESDURINGPUBLICHOLIDAYS, true);
            LoanProduct package = new LoanProduct
                                      {
                                          InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
                                          LoanType = OLoanTypes.Flat,
                                          ChargeInterestWithinGracePeriod = true,
                                          Currency = new Currency { Id = 1 }
                                      };
            package.KeepExpectedInstallment = true;
            package.AnticipatedTotalRepaymentPenaltiesBase = OAnticipatedRepaymentPenaltiesBases.RemainingOLB;
            Loan myContract = new Loan(package, 1000, 0.03m, 6, 1, new DateTime(2006, 1, 1), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));
            myContract.BadLoan = true;
            myContract.NonRepaymentPenalties.OLB = 0.003;
            myContract.AnticipatedTotalRepaymentPenalties = 0.01;

            CreditContractOptions cCO = new CreditContractOptions(package.LoanType, package.KeepExpectedInstallment, false, 0, 0, false, 0,
                                                                  package.AnticipatedTotalRepaymentPenaltiesBase);
            CreditContractRepayment cCR = new CreditContractRepayment(myContract, cCO, new DateTime(2006, 3, 15), 1, new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""));

            Assert.AreEqual(1306, Math.Round(cCR.MaximumAmountAuthorizeToRepay.Value, 2));
        }

        [Test]
        public void TestCalculateEventWhenRepayTotallyFlateBadLoanWith42dayslateWhenNonRepaymentFeesBaseOnOLBAndKeepExpectedInstallment()
        {
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.CALCULATIONLATEFEESDURINGPUBLICHOLIDAYS, true);
            LoanProduct package = new LoanProduct
                                      {
                                          InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
                                          LoanType = OLoanTypes.Flat,
                                          ChargeInterestWithinGracePeriod = true,
                                          Currency = new Currency { Id = 1 }
                                      };
            package.KeepExpectedInstallment = true;
            package.AnticipatedTotalRepaymentPenaltiesBase = OAnticipatedRepaymentPenaltiesBases.RemainingOLB;
            Loan myContract = new Loan(package, 1000, 0.03m, 6, 1, new DateTime(2006, 1, 1), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));
            myContract.BadLoan = true;
            myContract.NonRepaymentPenalties.OLB = 0.003;
            myContract.AnticipatedTotalRepaymentPenalties = 0.01;

            CreditContractOptions cCO = new CreditContractOptions(package.LoanType, package.KeepExpectedInstallment, false, 0, 0, false, 0,
                                                                  package.AnticipatedTotalRepaymentPenaltiesBase);
            CreditContractRepayment cCR = new CreditContractRepayment(myContract, cCO, new DateTime(2006, 3, 15), 1, new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""));

            Assert.AreEqual(1306, Math.Round(cCR.MaximumAmountAuthorizeToRepay.Value, 2));

            RepaymentEvent bLRE = myContract.Repay(1, new DateTime(2006, 3, 15), 1306, false, true);

            Assert.AreEqual(126m, bLRE.Fees.Value);
            Assert.AreEqual(180m, bLRE.Interests.Value);
            Assert.AreEqual(1000m, bLRE.Principal.Value);
        }
	
        [Test]
        public void TestCalculateMaximumAmountWithFlateBadLoanWith42dayslateWhenNonRepaymentFeesBaseOnOLBAndKeepNotExpectedInstallment()
        {
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.CALCULATIONLATEFEESDURINGPUBLICHOLIDAYS, true);
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.ACCOUNTINGPROCESS, OAccountingProcesses.Cash);
            
            LoanProduct package = new LoanProduct
                                      {
                                          InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
                                          LoanType = OLoanTypes.Flat,
                                          ChargeInterestWithinGracePeriod = true,
                                          Currency = new Currency { Id = 1, UseCents = true }
                                      };
            package.KeepExpectedInstallment = false;
            package.AnticipatedTotalRepaymentPenaltiesBase = OAnticipatedRepaymentPenaltiesBases.RemainingOLB;
            Loan myContract = new Loan(package, 1000, 0.03m, 6, 1, new DateTime(2006, 1, 1), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));
            myContract.BadLoan = true;
            myContract.NonRepaymentPenalties.OLB = 0.003;
            myContract.AnticipatedTotalRepaymentPenalties = 0.01;

            CreditContractOptions cCO = new CreditContractOptions(package.LoanType, package.KeepExpectedInstallment, false, 0, 0, false, 0,
                                                                  package.AnticipatedTotalRepaymentPenaltiesBase);;
            CreditContractRepayment cCR = new CreditContractRepayment(myContract, cCO, new DateTime(2006, 3, 15), 1, new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""));

            Assert.AreEqual(1194, Math.Round(cCR.MaximumAmountAuthorizeToRepay.Value, 2));
        }

        [Test]
        public void TestCalculateEventWhenRepayTotallyFlateBadLoanWith42dayslateWhenNonRepaymentFeesBaseOnOLBAndKeepNotExpectedInstallment()
        {
            Assert.Ignore();
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.CALCULATIONLATEFEESDURINGPUBLICHOLIDAYS, true);
            LoanProduct package = new LoanProduct
                                      {
                                          InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
                                          LoanType = OLoanTypes.Flat,
                                          ChargeInterestWithinGracePeriod = true,
                                          Currency = new Currency { Id = 1 }
                                      };
            package.KeepExpectedInstallment = false;
            package.AnticipatedTotalRepaymentPenaltiesBase = OAnticipatedRepaymentPenaltiesBases.RemainingOLB;
            Loan myContract = new Loan(package, 1000, 0.03m, 6, 1, new DateTime(2006, 1, 1), new User(),
                                       ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""),
                                       ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));
            myContract.BadLoan = true;
            myContract.NonRepaymentPenalties.OLB = 0.003;
            myContract.AnticipatedTotalRepaymentPenalties = 0.01;

            CreditContractOptions cCO = new CreditContractOptions(package.LoanType, package.KeepExpectedInstallment,
                                                                  false, 0, 0, false, 0,
                                                                  package.AnticipatedTotalRepaymentPenaltiesBase);

            CreditContractRepayment cCR = new CreditContractRepayment(myContract, cCO, new DateTime(2006, 3, 15), 1,
                                                                      new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""));


            Assert.AreEqual(1194, Math.Round(cCR.MaximumAmountAuthorizeToRepay.Value, 2));


            RepaymentEvent bLRE = myContract.Repay(1, new DateTime(2006, 3, 15), 1194, false, false);

            Assert.AreEqual(134, bLRE.Fees.Value);
            Assert.AreEqual(60, bLRE.Interests.Value);
            Assert.AreEqual(1000, bLRE.Principal.Value);
        }


        [Test]
        public void TestCalculateMaximumAmountWithFlateBadLoanWith42dayslateWhenNonRepaymentFeesBaseOnOverDueWithoutIntereestAndKeepExpectedInstallment()
        {
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.CALCULATIONLATEFEESDURINGPUBLICHOLIDAYS, true);

            LoanProduct package = new LoanProduct
                                      {
                                          InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
                                          LoanType = OLoanTypes.Flat,
                                          ChargeInterestWithinGracePeriod = true,
                                          Currency = new Currency { Id = 1, UseCents = true }
                                      };
            package.KeepExpectedInstallment = true;
            package.AnticipatedTotalRepaymentPenaltiesBase = OAnticipatedRepaymentPenaltiesBases.RemainingOLB;
            Loan myContract = new Loan(package, 1000, 0.03m, 6, 1, new DateTime(2006, 1, 1), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));
            myContract.BadLoan = true;
            myContract.NonRepaymentPenalties.OverDuePrincipal = 0.003;
            myContract.AnticipatedTotalRepaymentPenalties = 0.01;

            CreditContractOptions cCO = new CreditContractOptions(package.LoanType, package.KeepExpectedInstallment, false, 0, 0, false, 0,
                                                                  package.AnticipatedTotalRepaymentPenaltiesBase);
            CreditContractRepayment cCR = new CreditContractRepayment(myContract, cCO, new DateTime(2006, 3, 15), 1, new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""));

            
        

            Assert.AreEqual(1188.40m, Math.Round(cCR.MaximumAmountAuthorizeToRepay.Value, 2));

            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.CALCULATIONLATEFEESDURINGPUBLICHOLIDAYS, false);

            cCR = new CreditContractRepayment(myContract, cCO, new DateTime(2006, 3, 15), 1, new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""));


            Assert.AreEqual(1186.60m, Math.Round(cCR.MaximumAmountAuthorizeToRepay.Value, 2));

        }

        [Test]
        public void TestCalculateEventWhenRepayTotallyFlateBadLoanWith42dayslateWhenNonRepaymentFeesBaseOnOverDueWithoutIntereestAndKeepExpectedInstallment()
        {
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.CALCULATIONLATEFEESDURINGPUBLICHOLIDAYS, true);
            LoanProduct package = new LoanProduct
                                      {
                                          InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
                                          LoanType = OLoanTypes.Flat,
                                          ChargeInterestWithinGracePeriod = true,
                                          Currency = new Currency { Id = 1, UseCents = true }
                                      };
            package.KeepExpectedInstallment = true;
            package.AnticipatedTotalRepaymentPenaltiesBase = OAnticipatedRepaymentPenaltiesBases.RemainingOLB;
            Loan myContract = new Loan(package, 1000, 0.03m, 6, 1, new DateTime(2006, 1, 1), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));
            myContract.BadLoan = true;
            myContract.NonRepaymentPenalties.OverDuePrincipal = 0.003;
            myContract.AnticipatedTotalRepaymentPenalties = 0.01;

            CreditContractOptions cCO = new CreditContractOptions(package.LoanType, package.KeepExpectedInstallment, false, 0, 0, false, 0,
                                                                  package.AnticipatedTotalRepaymentPenaltiesBase);
            CreditContractRepayment cCR = new CreditContractRepayment(myContract, cCO, new DateTime(2006, 3, 15), 1, new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""));

            Assert.AreEqual(1188.40m, Math.Round(cCR.MaximumAmountAuthorizeToRepay.Value, 2));

            RepaymentEvent bLRE = myContract.Repay(1, new DateTime(2006, 3, 15), 1188.40m, false, true);

            Assert.AreEqual(8.40m, bLRE.Fees.Value);
            Assert.AreEqual(180m, bLRE.Interests.Value);
            Assert.AreEqual(1000m, bLRE.Principal.Value);
        }

        [Test]
        public void TestCalculateMaximumAmountWithFlateBadLoanWith42dayslateWhenNonRepaymentFeesBaseOnOverDueWithoutIntereestAndKeepNotExpectedInstallment()
        {
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.CALCULATIONLATEFEESDURINGPUBLICHOLIDAYS, true);
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.ACCOUNTINGPROCESS, OAccountingProcesses.Cash);
            
            LoanProduct package = new LoanProduct
                                      {
                                          InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
                                          LoanType = OLoanTypes.Flat,
                                          ChargeInterestWithinGracePeriod = true,
                                          Currency = new Currency { Id = 1, UseCents = true }
                                      };
            package.KeepExpectedInstallment = false;
            package.AnticipatedTotalRepaymentPenaltiesBase = OAnticipatedRepaymentPenaltiesBases.RemainingOLB;
            Loan myContract = new Loan(package, 1000, 0.03m, 6, 1, new DateTime(2006, 1, 1), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));
            myContract.BadLoan = true;
            myContract.NonRepaymentPenalties.OverDuePrincipal = 0.003;
            myContract.AnticipatedTotalRepaymentPenalties = 0.01;

            CreditContractOptions cCO = new CreditContractOptions(package.LoanType, package.KeepExpectedInstallment, false, 0, 0, false, 0,
                                                                  package.AnticipatedTotalRepaymentPenaltiesBase);
            CreditContractRepayment cCR = new CreditContractRepayment(myContract, cCO, new DateTime(2006, 3, 15), 1, new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""));

            Assert.AreEqual(1076.4m, Math.Round(cCR.MaximumAmountAuthorizeToRepay.Value, 2));
        }

        [Test]
        public void TestCalculateEventWhenRepayTotallyFlateBadLoanWith42dayslateWhenNonRepaymentFeesBaseOnOverDueWithoutIntereestAndKeepNotExpectedInstallment()
        {
            Assert.Ignore();
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.CALCULATIONLATEFEESDURINGPUBLICHOLIDAYS, true);
            LoanProduct package = new LoanProduct
                                      {
                                          InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
                                          LoanType = OLoanTypes.Flat,
                                          ChargeInterestWithinGracePeriod = true,
                                          Currency = new Currency { Id = 1 }
                                      };
            package.KeepExpectedInstallment = false;
            package.AnticipatedTotalRepaymentPenaltiesBase = OAnticipatedRepaymentPenaltiesBases.RemainingOLB;
            Loan myContract = new Loan(package, 1000, 0.03m, 6, 1, new DateTime(2006, 1, 1), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));
            myContract.BadLoan = true;
            myContract.NonRepaymentPenalties.OverDuePrincipal = 0.003;
            myContract.AnticipatedTotalRepaymentPenalties = 0.01;

            CreditContractOptions cCO = new CreditContractOptions(package.LoanType, package.KeepExpectedInstallment, false, 0, 0, false, 0,
                                                                  package.AnticipatedTotalRepaymentPenaltiesBase);
            CreditContractRepayment cCR = new CreditContractRepayment(myContract, cCO, new DateTime(2006, 3, 15), 1, new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""));

            Assert.AreEqual(1076.4m, Math.Round(cCR.MaximumAmountAuthorizeToRepay.Value, 2));
            RepaymentEvent bLRE = myContract.Repay(1, new DateTime(2006, 3, 15), 1076.4m, false, false);

            Assert.AreEqual(16.40m, bLRE.Fees.Value);
            Assert.AreEqual(60, bLRE.Interests.Value);
            Assert.AreEqual(1000, bLRE.Principal.Value);
        }

        [Test]
        public void TestCalculateMaximumAmountWithFlateBadLoanWith42dayslateWhenNonRepaymentFeesBaseOnOverDueWithInterestAndKeepExpectedInstallment()
        {
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.CALCULATIONLATEFEESDURINGPUBLICHOLIDAYS, true);
            LoanProduct package = new LoanProduct
                                      {
                                          InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
                                          LoanType = OLoanTypes.Flat,
                                          ChargeInterestWithinGracePeriod = true,
                                          Currency = new Currency { Id = 1, UseCents = true }
                                      };
            package.KeepExpectedInstallment = true;
            package.AnticipatedTotalRepaymentPenaltiesBase = OAnticipatedRepaymentPenaltiesBases.RemainingOLB;
            Loan myContract = new Loan(package, 1000, 0.03m, 6, 1, new DateTime(2006, 1, 1), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));
            myContract.BadLoan = true;
            myContract.NonRepaymentPenalties.OverDuePrincipal = 0.003;
            myContract.NonRepaymentPenalties.OverDueInterest = 0.003;
            myContract.AnticipatedTotalRepaymentPenalties = 0.01;

            CreditContractOptions cCO = new CreditContractOptions(package.LoanType, package.KeepExpectedInstallment, false, 0, 0, false, 0,
                                                                  package.AnticipatedTotalRepaymentPenaltiesBase);
            CreditContractRepayment cCR = new CreditContractRepayment(myContract, cCO, new DateTime(2006, 3, 15), 1, new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""));

            Assert.AreEqual(1193.44m, Math.Round(cCR.MaximumAmountAuthorizeToRepay.Value, 2));
        }

        [Test]
        public void TestCalculateEventWhenRepayTotallyFlateBadLoanWith42dayslateWhenNonRepaymentFeesBaseOnOverDueWithInterestAndKeepExpectedInstallment()
        {
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.CALCULATIONLATEFEESDURINGPUBLICHOLIDAYS, true);
            LoanProduct package = new LoanProduct
                                      {
                                          InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
                                          LoanType = OLoanTypes.Flat,
                                          ChargeInterestWithinGracePeriod = true,
                                          Currency = new Currency { Id = 1, UseCents = true}
                                      };
            package.KeepExpectedInstallment = true;
            package.AnticipatedTotalRepaymentPenaltiesBase = OAnticipatedRepaymentPenaltiesBases.RemainingOLB;
            Loan myContract = new Loan(package, 1000, 0.03m, 6, 1, new DateTime(2006, 1, 1), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));
            myContract.BadLoan = true;
            myContract.NonRepaymentPenalties.OverDuePrincipal = 0.003;
            myContract.NonRepaymentPenalties.OverDueInterest = 0.003;
            myContract.AnticipatedTotalRepaymentPenalties = 0.01;

            CreditContractOptions cCO = new CreditContractOptions(package.LoanType, package.KeepExpectedInstallment, false, 0, 0, false, 0,
                                                                  package.AnticipatedTotalRepaymentPenaltiesBase);
            CreditContractRepayment cCR = new CreditContractRepayment(myContract, cCO, new DateTime(2006, 3, 15), 1, new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""));

            Assert.AreEqual(1193.44m, Math.Round(cCR.MaximumAmountAuthorizeToRepay.Value, 2));
            RepaymentEvent bLRE = myContract.Repay(1, new DateTime(2006, 3, 15), 1193.44m, false, true);

            Assert.AreEqual(13.44m, bLRE.Fees.Value);
            Assert.AreEqual(180, bLRE.Interests.Value);
            Assert.AreEqual(1000, bLRE.Principal.Value);
        }

        [Test]
        public void TestCalculateMaximumAmountWithFlateBadLoanWith42dayslateWhenNonRepaymentFeesBaseOnOverDueWithInterestAndKeepNotExpectedInstallment()
        {
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.CALCULATIONLATEFEESDURINGPUBLICHOLIDAYS, true);
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.ACCOUNTINGPROCESS, OAccountingProcesses.Cash);
            
            LoanProduct package = new LoanProduct
                                      {
                                          InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
                                          LoanType = OLoanTypes.Flat,
                                          ChargeInterestWithinGracePeriod = true,
                                          Currency = new Currency { Id = 1, UseCents = true }
                                      };
            package.KeepExpectedInstallment = false;
            package.AnticipatedTotalRepaymentPenaltiesBase = OAnticipatedRepaymentPenaltiesBases.RemainingOLB;
            Loan myContract = new Loan(package, 1000, 0.03m, 6, 1, new DateTime(2006, 1, 1), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));
            myContract.BadLoan = true;
            myContract.NonRepaymentPenalties.OverDueInterest = 0.003;
            myContract.NonRepaymentPenalties.OverDuePrincipal = 0.003;
            myContract.AnticipatedTotalRepaymentPenalties = 0.01;

            CreditContractOptions cCO = new CreditContractOptions(package.LoanType, package.KeepExpectedInstallment, false, 0, 0, false, 0,
                                                                  package.AnticipatedTotalRepaymentPenaltiesBase);
            CreditContractRepayment cCR = new CreditContractRepayment(myContract, cCO, new DateTime(2006, 3, 15), 1, new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""));

            Assert.AreEqual(1081.44m,Math.Round(cCR.MaximumAmountAuthorizeToRepay.Value,2));
        }

        [Test]
        public void TestTotalAnticipatedRepaymentFormerPrepaymentSameDayDecliningCash()
        {
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.CALCULATIONLATEFEESDURINGPUBLICHOLIDAYS, true);
            LoanProduct package = new LoanProduct
                                      {
                                          InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
                                          LoanType = OLoanTypes.DecliningFixedInstallments,
                                          ChargeInterestWithinGracePeriod = true,
                                          GracePeriod = 0,
                                          Currency = new Currency { Id = 1, UseCents = true}
                                      };
            package.KeepExpectedInstallment = true;
            package.AnticipatedTotalRepaymentPenaltiesBase = OAnticipatedRepaymentPenaltiesBases.RemainingOLB;
            Loan myContract = new Loan(package, 1000, 0.03m, 6, 0, new DateTime(2006, 1, 1), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));
            myContract.BadLoan = true;
            myContract.NonRepaymentPenalties.InitialAmount = 0.003;
            myContract.AnticipatedTotalRepaymentPenalties = 0.01;

            CreditContractOptions cCO = new CreditContractOptions(package.LoanType, package.KeepExpectedInstallment, false, 0, 0, false, 0,
                                                                  package.AnticipatedTotalRepaymentPenaltiesBase);
            CreditContractRepayment cCR = new CreditContractRepayment(myContract, cCO, new DateTime(2006, 1, 1), 1, new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""));

            Assert.AreEqual(1107.58m, Math.Round(cCR.MaximumAmountAuthorizeToRepay.Value, 2));

            RepaymentEvent bLRE = myContract.Repay(1, new DateTime(2006, 1, 1), 194.60m, false, true);

            Assert.AreEqual(0, bLRE.Fees.Value);
            Assert.AreEqual(40m, bLRE.Interests.Value);
            Assert.AreEqual(154.60, bLRE.Principal.Value);

            cCR = new CreditContractRepayment(myContract, cCO, new DateTime(2006, 1, 1), 1, new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""));

            Assert.AreEqual(912.98m, Math.Round(cCR.MaximumAmountAuthorizeToRepay.Value, 2));

            bLRE = myContract.Repay(2, new DateTime(2006, 1, 1), 912.98m, true, true);

            Assert.AreEqual(0, bLRE.Fees.Value);
            Assert.AreEqual(67.58m, bLRE.Interests.Value);
            Assert.AreEqual(845.40m, bLRE.Principal.Value);
        }

        [Test]
        public void TestCalculateEventWhenRepayTotallyFlateBadLoanWith42dayslateWhenNonRepaymentFeesBaseOnOverDueWithInterestAndKeepNotExpectedInstallment()
        {
            Assert.Ignore();
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.CALCULATIONLATEFEESDURINGPUBLICHOLIDAYS, true);
            LoanProduct package = new LoanProduct
                                      {
                                          InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
                                          LoanType = OLoanTypes.Flat,
                                          ChargeInterestWithinGracePeriod = true,
                                          Currency = new Currency { Id = 1 }
                                      };
            package.KeepExpectedInstallment = false;
            package.AnticipatedTotalRepaymentPenaltiesBase = OAnticipatedRepaymentPenaltiesBases.RemainingOLB;
            Loan myContract = new Loan(package, 1000, 0.03m, 6, 1, new DateTime(2006, 1, 1), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));
            myContract.BadLoan = true;
            myContract.NonRepaymentPenalties.OverDueInterest = 0.003;
            myContract.NonRepaymentPenalties.OverDuePrincipal = 0.003;
            myContract.AnticipatedTotalRepaymentPenalties = 0.01;

            CreditContractOptions cCO = new CreditContractOptions(package.LoanType, package.KeepExpectedInstallment, false, 0, 0, false, 0,
                                                                  package.AnticipatedTotalRepaymentPenaltiesBase);
            CreditContractRepayment cCR = new CreditContractRepayment(myContract, cCO, new DateTime(2006, 3, 15), 1, new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""));

            Assert.AreEqual(1081.44m, Math.Round(cCR.MaximumAmountAuthorizeToRepay.Value, 2));
            RepaymentEvent bLRE = myContract.Repay(1, new DateTime(2006, 3, 15), 1081.44m, false, false);

            Assert.AreEqual(21.44m, bLRE.Fees.Value);
            Assert.AreEqual(60, bLRE.Interests.Value);
            Assert.AreEqual(1000, bLRE.Principal.Value);
        }

        [Test]
        public void TestCalculateMaximumAmountWithDecliningBadLoanWith42dayslateWhenNonRepaymentFeesBaseOnInitialAmountAndKeepExpectedInstallment()
        {
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.CALCULATIONLATEFEESDURINGPUBLICHOLIDAYS, true);
            LoanProduct package = new LoanProduct
                                      {
                                          InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
                                          LoanType = OLoanTypes.DecliningFixedInstallments,
                                          ChargeInterestWithinGracePeriod = true,
                                          Currency = new Currency { Id = 1, UseCents = true},
                                          RoundingType = ORoundingType.Approximate
                                      };
            package.KeepExpectedInstallment = true;
            package.AnticipatedTotalRepaymentPenaltiesBase = OAnticipatedRepaymentPenaltiesBases.RemainingOLB;
            Loan myContract = new Loan(package, 1000, 0.03m, 6, 1, new DateTime(2006, 1, 1), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));
            myContract.BadLoan = true;
            myContract.NonRepaymentPenalties.InitialAmount = 0.003;
            myContract.AnticipatedTotalRepaymentPenalties = 0.01;

            CreditContractOptions cCO = new CreditContractOptions(package.LoanType, package.KeepExpectedInstallment, false, 0, 0, false, 0,
                                                                  package.AnticipatedTotalRepaymentPenaltiesBase);
            CreditContractRepayment cCR = new CreditContractRepayment(myContract, cCO, new DateTime(2006, 3, 15), 1, new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""));

            Assert.AreEqual(1247.77m, Math.Round(cCR.MaximumAmountAuthorizeToRepay.Value, 2));
        }

        [Test]
        public void CalculateEvent_RepayTotallyDecliningBadLoan_42dayslate_NonRepaymentFeesBaseOnInitialAmount_KeepExpectedInstallment()
        {
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.CALCULATIONLATEFEESDURINGPUBLICHOLIDAYS, true);
            LoanProduct package = new LoanProduct{
                                                     InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
                                                     LoanType = OLoanTypes.DecliningFixedInstallments,
                                                     ChargeInterestWithinGracePeriod = true,
                                                     Currency = new Currency {Id = 1, UseCents = true},
                                                     KeepExpectedInstallment = true,
                                                     AnticipatedTotalRepaymentPenaltiesBase = OAnticipatedRepaymentPenaltiesBases.RemainingOLB,
                                                     RoundingType = ORoundingType.Approximate
                                                 };
            Loan myContract = new Loan(package, 1000, 0.03m, 6, 1, new DateTime(2006, 1, 1), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));
            myContract.BadLoan = true;
            myContract.NonRepaymentPenalties.InitialAmount = 0.003;
            myContract.AnticipatedTotalRepaymentPenalties = 0.01;

            CreditContractOptions cCO = new CreditContractOptions(package.LoanType, package.KeepExpectedInstallment, false, 0, 0, false, 0,package.AnticipatedTotalRepaymentPenaltiesBase);
            CreditContractRepayment cCR = new CreditContractRepayment(myContract, cCO, new DateTime(2006, 3, 15), 1, new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""));

            Assert.AreEqual(1247.77m, Math.Round(cCR.MaximumAmountAuthorizeToRepay.Value, 2));
            RepaymentEvent bLRE = myContract.Repay(1, new DateTime(2006, 3, 15), 1247.77m, false, true);

            Assert.AreEqual(126m, bLRE.Fees.Value);
            Assert.AreEqual(121.77m, bLRE.Interests.Value);
            Assert.AreEqual(1000.00m, bLRE.Principal.Value);
        }

        [Test]
        public void TestCalculateMaximumAmountWithDecliningBadLoanWith42dayslateWhenNonRepaymentFeesBaseOnInitialAmountAndKeepNotExpectedInstallment()
        {
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.CALCULATIONLATEFEESDURINGPUBLICHOLIDAYS, true);
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.ACCOUNTINGPROCESS, OAccountingProcesses.Cash);
            
            LoanProduct package = new LoanProduct
                                      {
                                          InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
                                          LoanType = OLoanTypes.DecliningFixedInstallments,
                                          ChargeInterestWithinGracePeriod = true,
                                          Currency = new Currency { Id = 1, UseCents = true },
                                          RoundingType = ORoundingType.Approximate
                                      };
            package.KeepExpectedInstallment = false;
            package.AnticipatedTotalRepaymentPenaltiesBase = OAnticipatedRepaymentPenaltiesBases.RemainingOLB;
            Loan myContract = new Loan(package, 1000, 0.03m, 6, 1, new DateTime(2006, 1, 1), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));
            myContract.BadLoan = true;
            myContract.NonRepaymentPenalties.InitialAmount = 0.003;
            myContract.AnticipatedTotalRepaymentPenalties = 0.01;

            CreditContractOptions cCO = new CreditContractOptions(package.LoanType, package.KeepExpectedInstallment, false, 0, 0, false, 0,package.AnticipatedTotalRepaymentPenaltiesBase);
            CreditContractRepayment cCR = new CreditContractRepayment(myContract, cCO, new DateTime(2006, 3, 15), 1, new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""));

            Assert.AreEqual(1194.12m, Math.Round(cCR.MaximumAmountAuthorizeToRepay.Value, 2));
        }

        [Test]
        public void CalculateEvent_RepayTotallyDecliningBadLoan_42dayslate_NonRepaymentFeesBaseOnInitialAmount_KeepNotExpectedInstallment()
        {
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.CALCULATIONLATEFEESDURINGPUBLICHOLIDAYS, true);
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.ACCOUNTINGPROCESS, OAccountingProcesses.Accrual);
            LoanProduct package = new LoanProduct
                                      {
                                          InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
                                          LoanType = OLoanTypes.DecliningFixedInstallments,
                                          ChargeInterestWithinGracePeriod = true,
                                          Currency = new Currency { Id = 1, UseCents = true}
                                      };
            package.KeepExpectedInstallment = false;
            package.AnticipatedTotalRepaymentPenaltiesBase = OAnticipatedRepaymentPenaltiesBases.RemainingOLB;
            Loan myContract = new Loan(package, 1000, 0.03m, 6, 1, new DateTime(2006, 1, 1), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));
            myContract.BadLoan = true;
            myContract.NonRepaymentPenalties.InitialAmount = 0.003;
            myContract.AnticipatedTotalRepaymentPenalties = 0.01;

            CreditContractOptions cCO = new CreditContractOptions(package.LoanType, package.KeepExpectedInstallment, false, 0, 0, false, 0, package.AnticipatedTotalRepaymentPenaltiesBase);
            CreditContractRepayment cCR = new CreditContractRepayment(myContract, cCO, new DateTime(2006, 3, 15), 1, new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""));

            Assert.AreEqual(1204.45m, Math.Round(cCR.MaximumAmountAuthorizeToRepay.Value, 2));
            RepaymentEvent bLRE = myContract.Repay(1, new DateTime(2006, 3, 15), 1204.45m, false, false);

            Assert.AreEqual(134.12m, bLRE.Fees.Value);
            Assert.AreEqual(70.33m, bLRE.Interests.Value);
            Assert.AreEqual(1000m, bLRE.Principal.Value);
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.ACCOUNTINGPROCESS, OAccountingProcesses.Cash);
        }

        [Test]
        public void TestCalculateMaximumAmountWithDecliningBadLoanWith42dayslateWhenNonRepaymentFeesBaseOnOLBAndKeepExpectedInstallment()
        {
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.CALCULATIONLATEFEESDURINGPUBLICHOLIDAYS, true);
            LoanProduct package = new LoanProduct
                                      {
                                          InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
                                          LoanType = OLoanTypes.DecliningFixedInstallments,
                                          ChargeInterestWithinGracePeriod = true,
                                          Currency = new Currency { Id = 1, UseCents = true},
                                          RoundingType = ORoundingType.Approximate
                                      };
            package.KeepExpectedInstallment = true;
            package.AnticipatedTotalRepaymentPenaltiesBase = OAnticipatedRepaymentPenaltiesBases.RemainingOLB;
            Loan myContract = new Loan(package, 1000, 0.03m, 6, 1, new DateTime(2006, 1, 1), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));
            myContract.BadLoan = true;
            myContract.NonRepaymentPenalties.OLB = 0.003;
            myContract.AnticipatedTotalRepaymentPenalties = 0.01;

            CreditContractOptions cCO = new CreditContractOptions(package.LoanType, package.KeepExpectedInstallment, false, 0, 0, false, 0,
                                                                  package.AnticipatedTotalRepaymentPenaltiesBase);
            CreditContractRepayment cCR = new CreditContractRepayment(myContract, cCO, new DateTime(2006, 3, 15), 1, new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""));

            Assert.AreEqual(1247.77m, Math.Round(cCR.MaximumAmountAuthorizeToRepay.Value, 2));
        }

        [Test]
        public void CalculateEvent_RepayTotallyDecliningBadLoan_42dayslate_NonRepaymentFeesBaseOnOLB_KeepExpectedInstallment()
        {
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.CALCULATIONLATEFEESDURINGPUBLICHOLIDAYS, true);
            LoanProduct package = new LoanProduct
                                      {
                                          InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
                                          LoanType = OLoanTypes.DecliningFixedInstallments,
                                          ChargeInterestWithinGracePeriod = true,
                                          Currency = new Currency { Id = 1,UseCents = true},
                                          RoundingType = ORoundingType.Approximate
                                      };
            package.KeepExpectedInstallment = true;
            package.AnticipatedTotalRepaymentPenaltiesBase = OAnticipatedRepaymentPenaltiesBases.RemainingOLB;
            Loan myContract = new Loan(package, 1000, 0.03m, 6, 1, new DateTime(2006, 1, 1), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));
            myContract.BadLoan = true;
            myContract.NonRepaymentPenalties.OLB = 0.003;
            myContract.AnticipatedTotalRepaymentPenalties = 0.01;

            CreditContractOptions cCO = new CreditContractOptions(package.LoanType, package.KeepExpectedInstallment, false, 0, 0, false, 0,package.AnticipatedTotalRepaymentPenaltiesBase);
            CreditContractRepayment cCR = new CreditContractRepayment(myContract, cCO, new DateTime(2006, 3, 15), 1, new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""));

            Assert.AreEqual(1247.77m, Math.Round(cCR.MaximumAmountAuthorizeToRepay.Value, 2));

            RepaymentEvent bLRE = myContract.Repay(1, new DateTime(2006, 3, 15), 1247.77m, false, true);

            Assert.AreEqual(126m, bLRE.Fees.Value);
            Assert.AreEqual(121.77m, bLRE.Interests.Value);
            Assert.AreEqual(1000, bLRE.Principal.Value);
        }

        [Test]
        public void TestCalculateMaximumAmountWithDecliningBadLoanWith42dayslateWhenNonRepaymentFeesBaseOnOLBAndKeepNotExpectedInstallment()
        {
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.CALCULATIONLATEFEESDURINGPUBLICHOLIDAYS, true);
            LoanProduct package = new LoanProduct
                                      {
                                          InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
                                          LoanType = OLoanTypes.DecliningFixedInstallments,
                                          ChargeInterestWithinGracePeriod = true,
                                          Currency = new Currency { Id = 1, UseCents = true},
                                          RoundingType = ORoundingType.Approximate
                                      };
            package.KeepExpectedInstallment = false;
            package.AnticipatedTotalRepaymentPenaltiesBase = OAnticipatedRepaymentPenaltiesBases.RemainingOLB;
            Loan myContract = new Loan(package, 1000, 0.03m, 6, 1, new DateTime(2006, 1, 1), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));
            myContract.BadLoan = true;
            myContract.NonRepaymentPenalties.OLB = 0.003;
            myContract.AnticipatedTotalRepaymentPenalties = 0.01;

            CreditContractOptions cCO = new CreditContractOptions(package.LoanType, package.KeepExpectedInstallment, false, 0, 0, false, 0,
                                                                  package.AnticipatedTotalRepaymentPenaltiesBase);
            CreditContractRepayment cCR = new CreditContractRepayment(myContract, cCO, new DateTime(2006, 3, 15), 1, new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""));

            Assert.AreEqual(1194.12m, Math.Round(cCR.MaximumAmountAuthorizeToRepay.Value, 2));
        }

        [Test]
        public void CalculateEvent_RepayTotallyDecliningBadLoan_42dayslate_NonRepaymentFeesBaseOnOLB_KeepNotExpectedInstallment()
        {
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.CALCULATIONLATEFEESDURINGPUBLICHOLIDAYS, true);
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.ACCOUNTINGPROCESS, OAccountingProcesses.Cash);

            LoanProduct package = new LoanProduct
                                      {
                                          InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
                                          LoanType = OLoanTypes.DecliningFixedInstallments,
                                          ChargeInterestWithinGracePeriod = true,
                                          Currency = new Currency { Id = 1, UseCents = true}
                                      };
            package.KeepExpectedInstallment = false;
            package.AnticipatedTotalRepaymentPenaltiesBase = OAnticipatedRepaymentPenaltiesBases.RemainingOLB;
            Loan myContract = new Loan(package, 1000, 0.03m, 6, 1, new DateTime(2006, 1, 1), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));
            myContract.BadLoan = true;
            myContract.NonRepaymentPenalties.OLB = 0.003;
            myContract.AnticipatedTotalRepaymentPenalties = 0.01;

            CreditContractOptions cCO = new CreditContractOptions(package.LoanType, package.KeepExpectedInstallment, false, 0, 0, false, 0,
                                                                  package.AnticipatedTotalRepaymentPenaltiesBase);
            CreditContractRepayment cCR = new CreditContractRepayment(myContract, cCO, new DateTime(2006, 3, 15), 1, new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""));

            Assert.AreEqual(1194.12m, Math.Round(cCR.MaximumAmountAuthorizeToRepay.Value, 2));

            RepaymentEvent bLRE = myContract.Repay(1, new DateTime(2006, 3, 15), 1194.12m, false, false);

            Assert.AreEqual(134.12m, bLRE.Fees.Value);
            Assert.AreEqual(60m, bLRE.Interests.Value);
            Assert.AreEqual(1000m, bLRE.Principal.Value);
        }

        [Test]
        public void TestCalculateMaximumAmountWithDecliningBadLoanWith42dayslateWhenNonRepaymentFeesBaseOnOverDueWithoutInterestAndKeepExpectedInstallment()
        {
            LoanProduct package = new LoanProduct
                                      {
                                          InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
                                          LoanType = OLoanTypes.DecliningFixedInstallments,
                                          ChargeInterestWithinGracePeriod = true,
                                          Currency = new Currency { Id = 1, UseCents = true},
                                          RoundingType = ORoundingType.Approximate
                                      };
            package.KeepExpectedInstallment = true;
            package.AnticipatedTotalRepaymentPenaltiesBase = OAnticipatedRepaymentPenaltiesBases.RemainingOLB;
            Loan myContract = new Loan(package, 1000, 0.03m, 6, 1, new DateTime(2006, 1, 1), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));
            myContract.BadLoan = true;
            myContract.NonRepaymentPenalties.OverDuePrincipal = 0.003;
            myContract.AnticipatedTotalRepaymentPenalties = 0.01;

            CreditContractOptions cCO = new CreditContractOptions(package.LoanType, package.KeepExpectedInstallment, false, 0, 0, false, 0,package.AnticipatedTotalRepaymentPenaltiesBase);
            CreditContractRepayment cCR = new CreditContractRepayment(myContract, cCO, new DateTime(2006, 3, 15), 1, new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""));

            Assert.AreEqual(1129.68m, Math.Round(cCR.MaximumAmountAuthorizeToRepay.Value, 2));
        }

        [Test]
        public void CalculateEvent_RepayTotallyDecliningBadLoan_42dayslate_NonRepaymentFeesBaseOnOverDueWithoutInterest_KeepExpectedInstallment()
        {
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.CALCULATIONLATEFEESDURINGPUBLICHOLIDAYS, true);
            LoanProduct package = new LoanProduct
                                      {
                                          InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
                                          LoanType = OLoanTypes.DecliningFixedInstallments,
                                          ChargeInterestWithinGracePeriod = true,
                                          Currency = new Currency { Id = 1, UseCents = true},
                                          RoundingType = ORoundingType.Approximate
                                      };
            package.KeepExpectedInstallment = true;
            package.AnticipatedTotalRepaymentPenaltiesBase = OAnticipatedRepaymentPenaltiesBases.RemainingOLB;
            Loan myContract = new Loan(package, 1000, 0.03m, 6, 1, new DateTime(2006, 1, 1), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));
            myContract.BadLoan = true;
            myContract.NonRepaymentPenalties.OverDuePrincipal = 0.003;
            myContract.AnticipatedTotalRepaymentPenalties = 0.01;

            CreditContractOptions cCO = new CreditContractOptions(package.LoanType, package.KeepExpectedInstallment, false, 0, 0, false, 0,package.AnticipatedTotalRepaymentPenaltiesBase);
            CreditContractRepayment cCR = new CreditContractRepayment(myContract, cCO, new DateTime(2006, 3, 15), 1, new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""));

            Assert.AreEqual(1129.68m, Math.Round(cCR.MaximumAmountAuthorizeToRepay.Value, 2));

            RepaymentEvent bLRE = myContract.Repay(1, new DateTime(2006, 3, 15), 1129.68m, false, true);

            Assert.AreEqual(7.91m, bLRE.Fees.Value);
            Assert.AreEqual(121.77m, bLRE.Interests.Value);
            Assert.AreEqual(1000, bLRE.Principal.Value);
        }

        [Test]
        public void DecliningRate_OneMonthlate_FeesBasedOnOverdueAmountAndInterest_KeepNotExpectedInstallment()
        {
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.CALCULATIONLATEFEESDURINGPUBLICHOLIDAYS, true);
            LoanProduct package = new LoanProduct
                                      {
                                          InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
                                          LoanType = OLoanTypes.DecliningFixedInstallments,
                                          ChargeInterestWithinGracePeriod = true,
                                          Currency = new Currency { Id = 1, UseCents = true},
                                          RoundingType = ORoundingType.Approximate
                                      };
            package.KeepExpectedInstallment = false;
            Loan myContract = new Loan(package, 600, 0.03m, 6, 0, new DateTime(2006, 1, 4), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));
            myContract.BadLoan = true;
            myContract.NonRepaymentPenalties.OverDuePrincipal = 0.0016277;
            myContract.NonRepaymentPenalties.OverDueInterest = 0.0016277;

            CreditContractOptions cCO = new CreditContractOptions(package.LoanType, package.KeepExpectedInstallment, false, 0, 0, false, 0,package.AnticipatedTotalRepaymentPenaltiesBase);
            CreditContractRepayment cCR = new CreditContractRepayment(myContract, cCO, new DateTime(2006, 3, 6), 1, new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""));

            Assert.AreEqual(227.78m, cCR.MaximumAmountToRegradingLoan.Value);
        }

        [Test]
        public void TestCalculateMaximumAmountWithDecliningBadLoanWith42dayslateWhenNonRepaymentFeesBaseOnOverDueWithoutInterestAndKeepNotExpectedInstallment()
        {
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.CALCULATIONLATEFEESDURINGPUBLICHOLIDAYS, true);
            LoanProduct package = new LoanProduct
                                      {
                                          InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
                                          LoanType = OLoanTypes.DecliningFixedInstallments,
                                          ChargeInterestWithinGracePeriod = true,
                                          Currency = new Currency { Id = 1, UseCents = true},
                                          RoundingType = ORoundingType.Approximate
                                      };
            package.KeepExpectedInstallment = false;
            package.AnticipatedTotalRepaymentPenaltiesBase = OAnticipatedRepaymentPenaltiesBases.RemainingOLB;
            Loan myContract = new Loan(package, 1000, 0.03m, 6, 1, new DateTime(2006, 1, 1), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));
            myContract.BadLoan = true;
            myContract.NonRepaymentPenalties.OverDuePrincipal = 0.003;
            myContract.AnticipatedTotalRepaymentPenalties = 0.01;

            CreditContractOptions cCO = new CreditContractOptions(package.LoanType, package.KeepExpectedInstallment, false, 0, 0, false, 0,
                                                                  package.AnticipatedTotalRepaymentPenaltiesBase);
            CreditContractRepayment cCR = new CreditContractRepayment(myContract, cCO, new DateTime(2006, 3, 15), 1, new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""));

            Assert.AreEqual(1076.03m, Math.Round(cCR.MaximumAmountAuthorizeToRepay.Value, 2));
        }

        [Test]
        public void CalculateEvent_RepayTotallyDecliningBadLoan_42dayslate_NonRepaymentFeesBaseOnOverDueWithoutInterest_KeepNotExpectedInstallment()
        {
            //ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.CALCULATIONLATEFEESDURINGPUBLICHOLIDAYS, true);
            //LoanProduct package = new LoanProduct
            //                          {
            //                              InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
            //                              LoanType = OLoanTypes.DecliningFixedInstallments,
            //                              ChargeInterestWithinGracePeriod = true,
            //                              Currency = new Currency { Id = 1 }
            //                          };
            //package.KeepExpectedInstallment = false;
            //package.AnticipatedTotalRepaymentPenaltiesBase = OAnticipatedRepaymentPenaltiesBases.RemainingOLB;
            //Loan myContract = new Loan(package, 1000, 0.03m, 6, 1, new DateTime(2006, 1, 1), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisioningTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));
            //myContract.BadLoan = true;
            //myContract.NonRepaymentPenalties.OverDuePrincipal = 0.003;
            //myContract.AnticipatedTotalRepaymentPenalties = 0.01;

            //CreditContractOptions cCO = new CreditContractOptions(package.LoanType, package.KeepExpectedInstallment, false, 0, false, 0,package.AnticipatedTotalRepaymentPenaltiesBase);
            //CreditContractRepayment cCR = new CreditContractRepayment(myContract, cCO, new DateTime(2006, 3, 15), 1, new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""));

            //Assert.AreEqual(1076.03m, Math.Round(cCR.MaximumAmountAuthorizeToRepay.Value, 2));

            //RepaymentEvent bLRE = myContract.Repay(1, new DateTime(2006, 3, 15), 1076.03m, false, false);

            //Assert.AreEqual(16.03m, bLRE.Fees.Value);
            //Assert.AreEqual(60m, bLRE.Interests.Value);
            //Assert.AreEqual(1000, bLRE.Principal.Value);
        }

        [Test]
        public void TestCalculateMaximumAmountWithDecliningBadLoanWith42dayslateWhenNonRepaymentFeesBaseOnOverDueWithInterestAndKeepExpectedInstallment()
        {
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.CALCULATIONLATEFEESDURINGPUBLICHOLIDAYS, true);
            LoanProduct package = new LoanProduct
                                      {
                                          InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
                                          LoanType = OLoanTypes.DecliningFixedInstallments,
                                          ChargeInterestWithinGracePeriod = true,
                                          Currency = new Currency { Id = 1, UseCents = true},
                                          RoundingType = ORoundingType.Approximate
                                      };
            package.KeepExpectedInstallment = true;
            package.AnticipatedTotalRepaymentPenaltiesBase = OAnticipatedRepaymentPenaltiesBases.RemainingOLB;
            Loan myContract = new Loan(package, 1000, 0.03m, 6, 1, new DateTime(2006, 1, 1), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));
            myContract.BadLoan = true;
            myContract.NonRepaymentPenalties.OverDuePrincipal = 0.003;
            myContract.NonRepaymentPenalties.OverDueInterest = 0.003;
            myContract.AnticipatedTotalRepaymentPenalties = 0.01;

            CreditContractOptions cCO = new CreditContractOptions(package.LoanType, package.KeepExpectedInstallment, false, 0, 0, false, 0,
                                                                  package.AnticipatedTotalRepaymentPenaltiesBase);
            CreditContractRepayment cCR = new CreditContractRepayment(myContract, cCO, new DateTime(2006, 3, 15), 1, new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""));

            Assert.AreEqual(1134.72m, Math.Round(cCR.MaximumAmountAuthorizeToRepay.Value, 2));
        }

        [Test]
        public void CalculateEvent_RepayTotallyDecliningBadLoan_42dayslate_NonRepaymentFeesBaseOnOverDueWithInterest_KeepExpectedInstallment()
        {
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.CALCULATIONLATEFEESDURINGPUBLICHOLIDAYS, true);

            LoanProduct package = new LoanProduct
                                      {
                                          InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
                                          LoanType = OLoanTypes.DecliningFixedInstallments,
                                          ChargeInterestWithinGracePeriod = true,
                                          Currency = new Currency { Id = 1, UseCents = true},
                                          RoundingType = ORoundingType.Approximate
                                      };
            package.KeepExpectedInstallment = true;
            package.AnticipatedTotalRepaymentPenaltiesBase = OAnticipatedRepaymentPenaltiesBases.RemainingOLB;
            Loan myContract = new Loan(package, 1000, 0.03m, 6, 1, new DateTime(2006, 1, 1), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));
            myContract.BadLoan = true;
            myContract.NonRepaymentPenalties.OverDuePrincipal = 0.003;
            myContract.NonRepaymentPenalties.OverDueInterest = 0.003;
            myContract.AnticipatedTotalRepaymentPenalties = 0.01;

            CreditContractOptions cCO = new CreditContractOptions(package.LoanType, package.KeepExpectedInstallment, false, 0, 0, false, 0,package.AnticipatedTotalRepaymentPenaltiesBase);
            CreditContractRepayment cCR = new CreditContractRepayment(myContract, cCO, new DateTime(2006, 3, 15), 1, new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""));

            Assert.AreEqual(1134.72m,Math.Round(cCR.MaximumAmountAuthorizeToRepay.Value,2));

            RepaymentEvent bLRE = myContract.Repay(1, new DateTime(2006, 3, 15), 1134.72m, false, true);

            Assert.AreEqual(12.95m, bLRE.Fees.Value);
            Assert.AreEqual(121.77m, bLRE.Interests.Value);
            Assert.AreEqual(1000, bLRE.Principal.Value);
        }

        [Test]
        public void TestRepayFlatExoticWhenKeepNotExpectedInstallmentAndNoGracePeriod()
        {
            //ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.CALCULATIONLATEFEESDURINGPUBLICHOLIDAYS, true);
            //ExoticInstallmentsTable exoticProduct = new ExoticInstallmentsTable();
            //exoticProduct.Add(new ExoticInstallment(1, 0.1, 0.1));
            //exoticProduct.Add(new ExoticInstallment(2, 0.2, 0));
            //exoticProduct.Add(new ExoticInstallment(3, 0.1, 0.2));
            //exoticProduct.Add(new ExoticInstallment(4, 0, 0));
            //exoticProduct.Add(new ExoticInstallment(5, 0.4, 0.5));
            //exoticProduct.Add(new ExoticInstallment(6, 0.2, 0.2));

            //LoanProduct package = new LoanProduct
            //                          {
            //                              InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
            //                              LoanType = OLoanTypes.Flat,
            //                              ChargeInterestWithinGracePeriod = true,
            //                              Currency = new Currency { Id = 1 }
            //                          };
            //package.KeepExpectedInstallment = false;
            //package.AnticipatedTotalRepaymentPenaltiesBase = OAnticipatedRepaymentPenaltiesBases.RemainingOLB;
            //package.ExoticProduct = exoticProduct;

            //Loan myContract = new Loan(package, 1000, 0.03m, 6, 0, new DateTime(2006, 1, 1), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisioningTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));

            //myContract.NonRepaymentPenalties.InitialAmount = 0.003;
            //myContract.AnticipatedTotalRepaymentPenalties = 0.01;

            //myContract.Repay(1,new DateTime(2006,2,1),118,false,false);
            //myContract.Repay(2,new DateTime(2006,3,1),400,true,false); //200 overPaid

            //Assert.AreEqual(new DateTime(2006,2,1),myContract.GetInstallment(0).ExpectedDate); //1
            //Assert.AreEqual(18.0m,myContract.GetInstallment(0).InterestsRepayment.Value);
            //Assert.AreEqual(100m,myContract.GetInstallment(0).CapitalRepayment.Value);
            //Assert.AreEqual(1000m,myContract.GetInstallment(0).OLB.Value);
            //Assert.AreEqual(18m,myContract.GetInstallment(0).PaidInterests.Value);
            //Assert.AreEqual(100m,myContract.GetInstallment(0).PaidCapital.Value);

            //Assert.AreEqual(new DateTime(2006,3,1),myContract.GetInstallment(1).ExpectedDate); //2
            //Assert.AreEqual(0m,myContract.GetInstallment(1).InterestsRepayment.Value);
            //Assert.AreEqual(200m,myContract.GetInstallment(1).CapitalRepayment.Value);
            //Assert.AreEqual(900m,myContract.GetInstallment(1).OLB.Value);
            //Assert.AreEqual(0m,myContract.GetInstallment(1).PaidInterests.Value);
            //Assert.AreEqual(200m,myContract.GetInstallment(1).PaidCapital.Value);

            ////OVERPAID 200 -> 662
            //Assert.AreEqual(new DateTime(2006,4,3),myContract.GetInstallment(2).ExpectedDate); //1
            //Assert.AreEqual(0m,myContract.GetInstallment(2).InterestsRepayment.Value);
            //Assert.AreEqual(662m,myContract.GetInstallment(2).OLB.Value);
            //Assert.AreEqual(94.571m,myContract.GetInstallment(2).CapitalRepayment.Value);
            //Assert.AreEqual(0m,myContract.GetInstallment(2).PaidInterests.Value);
            //Assert.AreEqual(0m,myContract.GetInstallment(2).PaidCapital.Value);

            //Assert.AreEqual(new DateTime(2006,5,2),myContract.GetInstallment(3).ExpectedDate);//0
            //Assert.AreEqual(0m,myContract.GetInstallment(3).InterestsRepayment.Value);
            //Assert.AreEqual(0m,myContract.GetInstallment(3).CapitalRepayment.Value);
            //Assert.AreEqual(0m,myContract.GetInstallment(3).PaidInterests.Value);
            //Assert.AreEqual(0m,myContract.GetInstallment(3).PaidCapital.Value);

            //Assert.AreEqual(new DateTime(2006,6,1),myContract.GetInstallment(4).ExpectedDate);//4
            //Assert.AreEqual(0m,myContract.GetInstallment(4).InterestsRepayment.Value);
            //Assert.AreEqual(378.286m,myContract.GetInstallment(4).CapitalRepayment.Value);
            //Assert.AreEqual(0m,myContract.GetInstallment(4).PaidInterests.Value);
            //Assert.AreEqual(0m,myContract.GetInstallment(4).PaidCapital.Value);

            //Assert.AreEqual(new DateTime(2006,7,3),myContract.GetInstallment(5).ExpectedDate);//2
            //Assert.AreEqual(0m,myContract.GetInstallment(5).InterestsRepayment.Value);
            //Assert.AreEqual(189.143m,myContract.GetInstallment(5).CapitalRepayment.Value);
            //Assert.AreEqual(0m,myContract.GetInstallment(5).PaidInterests.Value);
            //Assert.AreEqual(0m,myContract.GetInstallment(5).PaidCapital.Value);
        }

        [Test]
        public void TestRepayFlatExoticWhenKeepNotExpectedInstallmentAndTwoGracePeriod()
        {
            Assert.Ignore();
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.CALCULATIONLATEFEESDURINGPUBLICHOLIDAYS, true);
            ExoticInstallmentsTable exoticProduct = new ExoticInstallmentsTable();
            exoticProduct.Add(new ExoticInstallment(1, 0.1, 0.1));
            exoticProduct.Add(new ExoticInstallment(2, 0.1, 0.1));
            exoticProduct.Add(new ExoticInstallment(3, 0.1, 0.1));
            exoticProduct.Add(new ExoticInstallment(4, 0.1, 0.1));
            exoticProduct.Add(new ExoticInstallment(5, 0.1, 0.1));
            exoticProduct.Add(new ExoticInstallment(6, 0.5, 0.5));

            LoanProduct package = new LoanProduct
                                      {
                                          InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
                                          LoanType = OLoanTypes.Flat,
                                          ChargeInterestWithinGracePeriod = true,
                                          Currency = new Currency { Id = 1, UseCents = true}
                                      };
            package.KeepExpectedInstallment = false;
            package.AnticipatedTotalRepaymentPenaltiesBase = OAnticipatedRepaymentPenaltiesBases.RemainingOLB;
            package.ExoticProduct = exoticProduct;

            Loan myContract = new Loan(package, 900, 0.026m, 9, 3, new DateTime(2006, 1, 1), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));

            PaymentMethod paymentMethod = new PaymentMethod(1, "Cash","",false );
            myContract.Repay(1, new DateTime(2006, 2, 1), 900, true, 0, 0, false, 0, false, false, false, paymentMethod);
            myContract.Repay(4, new DateTime(2006, 5, 2), 50, true, 0, 0, false, 0, false, false, false, paymentMethod);

            Assert.AreEqual(new DateTime(2006, 2, 1), myContract.GetInstallment(0).ExpectedDate);
            Assert.AreEqual(23.40m, myContract.GetInstallment(0).InterestsRepayment.Value);
            Assert.AreEqual(0m, myContract.GetInstallment(0).CapitalRepayment.Value);
            Assert.AreEqual(900m, myContract.GetInstallment(0).OLB.Value);
            Assert.AreEqual(23.40m, myContract.GetInstallment(0).PaidInterests.Value);
            Assert.AreEqual(0m, myContract.GetInstallment(0).PaidCapital.Value);

            Assert.AreEqual(new DateTime(2006, 3, 1), myContract.GetInstallment(1).ExpectedDate); //1
            Assert.AreEqual(0m, myContract.GetInstallment(1).InterestsRepayment.Value);
            Assert.AreEqual(0m, myContract.GetInstallment(1).CapitalRepayment.Value);
            Assert.AreEqual(210.60m, myContract.GetInstallment(1).OLB.Value);
            Assert.AreEqual(0m, myContract.GetInstallment(1).PaidInterests.Value);
            Assert.AreEqual(0m, myContract.GetInstallment(1).PaidCapital.Value);

            Assert.AreEqual(new DateTime(2006, 4, 3), myContract.GetInstallment(2).ExpectedDate); //2
            Assert.AreEqual(0m, myContract.GetInstallment(2).InterestsRepayment.Value);
            Assert.AreEqual(0m, myContract.GetInstallment(2).CapitalRepayment.Value);
            Assert.AreEqual(210.60m, myContract.GetInstallment(2).OLB.Value);
            Assert.AreEqual(0m, myContract.GetInstallment(2).PaidInterests.Value);
            Assert.AreEqual(0m, myContract.GetInstallment(2).PaidCapital.Value);

            Assert.AreEqual(new DateTime(2006, 5, 2), myContract.GetInstallment(3).ExpectedDate);//1
            Assert.AreEqual(0m, myContract.GetInstallment(3).InterestsRepayment.Value);
            Assert.AreEqual(21.06m, myContract.GetInstallment(3).CapitalRepayment.Value);
            Assert.AreEqual(210.60m, myContract.GetInstallment(3).OLB.Value);
            Assert.AreEqual(0m, myContract.GetInstallment(3).PaidInterests.Value);
            Assert.AreEqual(21.06m, myContract.GetInstallment(3).PaidCapital.Value);

            Assert.AreEqual(new DateTime(2006, 6, 1), myContract.GetInstallment(4).ExpectedDate);//0
            Assert.AreEqual(0m, myContract.GetInstallment(4).InterestsRepayment.Value);
            Assert.AreEqual(17.844m, myContract.GetInstallment(4).CapitalRepayment.Value);
            Assert.AreEqual(160.6m, myContract.GetInstallment(4).OLB.Value);
            Assert.AreEqual(0m, myContract.GetInstallment(4).PaidInterests.Value);
            Assert.AreEqual(0m, myContract.GetInstallment(4).PaidCapital.Value);

            Assert.AreEqual(new DateTime(2006, 7, 3), myContract.GetInstallment(5).ExpectedDate);//4
            Assert.AreEqual(0m, myContract.GetInstallment(5).InterestsRepayment.Value);
            Assert.AreEqual(17.844m, myContract.GetInstallment(5).CapitalRepayment.Value);
            Assert.AreEqual(142.76m, myContract.GetInstallment(5).OLB.Value);
            Assert.AreEqual(0m, myContract.GetInstallment(5).PaidInterests.Value);
            Assert.AreEqual(0m, myContract.GetInstallment(5).PaidCapital.Value);

            Assert.AreEqual(new DateTime(2006, 8, 1), myContract.GetInstallment(6).ExpectedDate);//2
            Assert.AreEqual(0m, myContract.GetInstallment(6).InterestsRepayment.Value);
            Assert.AreEqual(17.844m, myContract.GetInstallment(6).CapitalRepayment.Value);
            Assert.AreEqual(124.91m, myContract.GetInstallment(6).OLB.Value);
            Assert.AreEqual(0m, myContract.GetInstallment(6).PaidInterests.Value);
            Assert.AreEqual(0m, myContract.GetInstallment(6).PaidCapital.Value);

            Assert.AreEqual(new DateTime(2006, 9, 1), myContract.GetInstallment(7).ExpectedDate);//2
            Assert.AreEqual(0m, myContract.GetInstallment(7).InterestsRepayment.Value);
            Assert.AreEqual(17.844m, myContract.GetInstallment(7).CapitalRepayment.Value);
            Assert.AreEqual(107.07m, myContract.GetInstallment(7).OLB.Value);
            Assert.AreEqual(0m, myContract.GetInstallment(7).PaidInterests.Value);
            Assert.AreEqual(0m, myContract.GetInstallment(7).PaidCapital.Value);

            Assert.AreEqual(new DateTime(2006, 10, 2), myContract.GetInstallment(8).ExpectedDate);//2
            Assert.AreEqual(0m, myContract.GetInstallment(8).InterestsRepayment.Value);
            Assert.AreEqual(89.222m, myContract.GetInstallment(8).CapitalRepayment.Value);
            Assert.AreEqual(89.22m, myContract.GetInstallment(8).OLB.Value);
            Assert.AreEqual(0m, myContract.GetInstallment(8).PaidInterests.Value);
            Assert.AreEqual(0m, myContract.GetInstallment(8).PaidCapital.Value);
        }


        [Test]
        public void TestRepayFlatExoticWhenKeepNotExpectedInstallmentAndOneGracePeriod()
        {
            Assert.Ignore();
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.CALCULATIONLATEFEESDURINGPUBLICHOLIDAYS, true);
            ExoticInstallmentsTable exoticProduct = new ExoticInstallmentsTable();
            exoticProduct.Add(new ExoticInstallment(1, 0.1, 0.1));
            exoticProduct.Add(new ExoticInstallment(2, 0.2, 0));
            exoticProduct.Add(new ExoticInstallment(3, 0.1, 0.2));
            exoticProduct.Add(new ExoticInstallment(4, 0, 0));
            exoticProduct.Add(new ExoticInstallment(5, 0.4, 0.5));
            exoticProduct.Add(new ExoticInstallment(6, 0.2, 0.2));

            LoanProduct package = new LoanProduct
                                      {
                                          InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
                                          LoanType = OLoanTypes.Flat,
                                          ChargeInterestWithinGracePeriod = true,
                                          Currency = new Currency { Id = 1 }
                                      };
            package.KeepExpectedInstallment = false;
            package.AnticipatedTotalRepaymentPenaltiesBase = OAnticipatedRepaymentPenaltiesBases.RemainingOLB;
            package.ExoticProduct = exoticProduct;

            Loan myContract = new Loan(package, 1000, 0.03m, 7, 1, new DateTime(2006, 1, 1), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));
            myContract.NonRepaymentPenalties.InitialAmount = 0.003;
            myContract.AnticipatedTotalRepaymentPenalties = 0.01;

            myContract.Repay(1, new DateTime(2006, 2, 1), 30, false, false);
            myContract.Repay(2, new DateTime(2006, 3, 1), 318, true, false); //200 overPaid

            Assert.AreEqual(new DateTime(2006, 2, 1), myContract.GetInstallment(0).ExpectedDate);
            Assert.AreEqual(30m, myContract.GetInstallment(0).InterestsRepayment.Value);
            Assert.AreEqual(0m, myContract.GetInstallment(0).CapitalRepayment.Value);
            Assert.AreEqual(1000m, myContract.GetInstallment(0).OLB.Value);
            Assert.AreEqual(30m, myContract.GetInstallment(0).PaidInterests.Value);
            Assert.AreEqual(0m, myContract.GetInstallment(0).PaidCapital.Value);

            Assert.AreEqual(new DateTime(2006, 3, 1), myContract.GetInstallment(1).ExpectedDate); //1
            Assert.AreEqual(18m, myContract.GetInstallment(1).InterestsRepayment.Value);
            Assert.AreEqual(100m, myContract.GetInstallment(1).CapitalRepayment.Value);
            Assert.AreEqual(1000m, myContract.GetInstallment(1).OLB.Value);
            Assert.AreEqual(18m, myContract.GetInstallment(1).PaidInterests.Value);
            Assert.AreEqual(100m, myContract.GetInstallment(1).PaidCapital.Value);

            //OVERPAID 200 -> 862
            Assert.AreEqual(new DateTime(2006, 4, 3), myContract.GetInstallment(2).ExpectedDate); //2
            Assert.AreEqual(0m, myContract.GetInstallment(2).InterestsRepayment.Value);
            Assert.AreEqual(191.556m, myContract.GetInstallment(2).CapitalRepayment.Value);
            Assert.AreEqual(0m, myContract.GetInstallment(2).PaidInterests.Value);
            Assert.AreEqual(0m, myContract.GetInstallment(2).PaidCapital.Value);

            Assert.AreEqual(new DateTime(2006, 5, 2), myContract.GetInstallment(3).ExpectedDate);//1
            Assert.AreEqual(0m, myContract.GetInstallment(3).InterestsRepayment.Value);
            Assert.AreEqual(95.778m, myContract.GetInstallment(3).CapitalRepayment.Value);
            Assert.AreEqual(0m, myContract.GetInstallment(3).PaidInterests.Value);
            Assert.AreEqual(0m, myContract.GetInstallment(3).PaidCapital.Value);

            Assert.AreEqual(new DateTime(2006, 6, 1), myContract.GetInstallment(4).ExpectedDate);//0
            Assert.AreEqual(0m, myContract.GetInstallment(4).InterestsRepayment.Value);
            Assert.AreEqual(0m, myContract.GetInstallment(4).CapitalRepayment.Value);
            Assert.AreEqual(0m, myContract.GetInstallment(4).PaidInterests.Value);
            Assert.AreEqual(0m, myContract.GetInstallment(4).PaidCapital.Value);

            Assert.AreEqual(new DateTime(2006, 7, 3), myContract.GetInstallment(5).ExpectedDate);//4
            Assert.AreEqual(0m, myContract.GetInstallment(5).InterestsRepayment.Value);
            Assert.AreEqual(383.111m, myContract.GetInstallment(5).CapitalRepayment.Value);
            Assert.AreEqual(0, myContract.GetInstallment(5).PaidInterests.Value);
            Assert.AreEqual(0, myContract.GetInstallment(5).PaidCapital.Value);

            Assert.AreEqual(new DateTime(2006, 8, 1), myContract.GetInstallment(6).ExpectedDate);//2
            Assert.AreEqual(0, myContract.GetInstallment(6).InterestsRepayment.Value);
            Assert.AreEqual(191.556m, myContract.GetInstallment(6).CapitalRepayment.Value);
            Assert.AreEqual(0m, myContract.GetInstallment(6).PaidInterests.Value);
            Assert.AreEqual(0m, myContract.GetInstallment(6).PaidCapital.Value);
        }

        [Test]
        public void TestCalculateMaximumAmountWithDecliningBadLoanWith42dayslateWhenNonRepaymentFeesBaseOnOverDueWithInterestAndKeepNotExpectedInstallment()
        {
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.CALCULATIONLATEFEESDURINGPUBLICHOLIDAYS, true);
            LoanProduct package = new LoanProduct
                                      {
                                          InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
                                          LoanType = OLoanTypes.DecliningFixedInstallments,
                                          ChargeInterestWithinGracePeriod = true,
                                          Currency = new Currency { Id = 1, UseCents = true},
                                          RoundingType = ORoundingType.Approximate
                                      };
            package.KeepExpectedInstallment = false;
            package.AnticipatedTotalRepaymentPenaltiesBase = OAnticipatedRepaymentPenaltiesBases.RemainingOLB;
            Loan myContract = new Loan(package, 1000, 0.03m, 6, 1, new DateTime(2006, 1, 1), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));
            myContract.BadLoan = true;
            myContract.NonRepaymentPenalties.OverDueInterest = 0.003;
            myContract.NonRepaymentPenalties.OverDuePrincipal = 0.003;
            myContract.AnticipatedTotalRepaymentPenalties = 0.01;

            CreditContractOptions cCO = new CreditContractOptions(package.LoanType, package.KeepExpectedInstallment, false, 0, 0, false, 0,
                                                                  package.AnticipatedTotalRepaymentPenaltiesBase);
            CreditContractRepayment cCR = new CreditContractRepayment(myContract, cCO, new DateTime(2006, 3, 15), 1, new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""));

            Assert.AreEqual(1081.07m, Math.Round(cCR.MaximumAmountAuthorizeToRepay.Value, 2));
        }

        [Test]
        public void TestCalculateEventWhenRepayTotallyDecliningBadLoanWith42dayslateWhenNonRepaymentFeesBaseOnOverDueWithInterestAndKeepNotExpectedInstallment()
        {
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.CALCULATIONLATEFEESDURINGPUBLICHOLIDAYS, true);
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.ACCOUNTINGPROCESS, OAccountingProcesses.Accrual);
            
            LoanProduct package = new LoanProduct
                                      {
                                          InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
                                          LoanType = OLoanTypes.DecliningFixedInstallments,
                                          ChargeInterestWithinGracePeriod = true,
                                          Currency = new Currency { Id = 1 }
                                      };
            package.KeepExpectedInstallment = false;
            package.AnticipatedTotalRepaymentPenaltiesBase = OAnticipatedRepaymentPenaltiesBases.RemainingOLB;
            Loan myContract = new Loan(package, 1000, 0.03m, 6, 1, new DateTime(2006, 1, 1), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));
            myContract.BadLoan = true;
            myContract.NonRepaymentPenalties.OverDueInterest = 0.003;
            myContract.NonRepaymentPenalties.OverDuePrincipal = 0.003;
            myContract.AnticipatedTotalRepaymentPenalties = 0.01;

            CreditContractOptions cCO = new CreditContractOptions(package.LoanType, package.KeepExpectedInstallment, false, 0, 0, false, 0,
                                                                  package.AnticipatedTotalRepaymentPenaltiesBase);
            CreditContractRepayment cCR = new CreditContractRepayment(myContract, cCO, new DateTime(2006, 3, 15), 1, new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""));

            Assert.AreEqual(1091m, Math.Round(cCR.MaximumAmountAuthorizeToRepay.Value, 2));

            RepaymentEvent bLRE = myContract.Repay(1, new DateTime(2006, 3, 15), 1091m, false, false);

            Assert.AreEqual(21m, bLRE.Fees.Value);
            Assert.AreEqual(70m, bLRE.Interests.Value);
            Assert.AreEqual(1000, bLRE.Principal.Value);

            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.ACCOUNTINGPROCESS, OAccountingProcesses.Cash);
        }

        private static void _AssertSpecifiedInstallment(Installment pInstallment, int pNumber, DateTime pExpectedDate, OCurrency pInterestRepayment, OCurrency pCapitalRepayment, OCurrency pOLB, OCurrency pPaidCapital, OCurrency pPaidInterest)
        {
            Assert.AreEqual(pNumber, pInstallment.Number);
            Assert.AreEqual(pExpectedDate, pInstallment.ExpectedDate);
            Assert.AreEqual(pInterestRepayment.Value, pInstallment.InterestsRepayment.Value);
            Assert.AreEqual(pCapitalRepayment.Value, pInstallment.CapitalRepayment.Value);
            Assert.AreEqual(pOLB.Value, pInstallment.OLB.Value);
            Assert.AreEqual(pPaidCapital.Value, pInstallment.PaidCapital.Value);
            Assert.AreEqual(pPaidInterest.Value, pInstallment.PaidInterests.Value);
        }

        [Test]
        public void RepayDecliningLoan_2MonthsLate_NonRepaymentFeesBaseOnOverDue_KeepNotExpectedInstallment()
        {
            Assert.Ignore();
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.CALCULATIONLATEFEESDURINGPUBLICHOLIDAYS, true);
            LoanProduct package = new LoanProduct
                                      {
                                          InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
                                          LoanType = OLoanTypes.DecliningFixedInstallments,
                                          ChargeInterestWithinGracePeriod = true,
                                          Currency = new Currency { Id = 1 }
                                      };
            package.KeepExpectedInstallment = false;
            package.AnticipatedTotalRepaymentPenaltiesBase = OAnticipatedRepaymentPenaltiesBases.RemainingOLB;
            Loan myContract = new Loan(package, 1000, 0.03m, 12, 0, new DateTime(2006, 1, 1), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));
            myContract.BadLoan = false;
            myContract.NonRepaymentPenalties.OverDueInterest = 0.0016;
            myContract.NonRepaymentPenalties.OverDuePrincipal = 0.0016;
            myContract.AnticipatedTotalRepaymentPenalties = 0;

            myContract.Repay(1, new DateTime(2006,2,1), 100.46m, false, false);
            myContract.Repay(2, new DateTime(2006,3,1), 100.46m, false, false);
            myContract.Repay(3, new DateTime(2006,5,2), 50, true, false);
            myContract.Repay(3, new DateTime(2006,6,1), 55.22m, false, false);
            RepaymentEvent rE5 = myContract.Repay(4, new DateTime(2006, 7, 3), 312.36m, false, false);

            Assert.AreEqual(0m, rE5.Fees.Value);
            Assert.AreEqual(16.33m, rE5.Interests.Value);
            Assert.AreEqual(296.03m, rE5.Principal.Value);

            Assert.AreEqual(1000, myContract.InstallmentList.Sum(pItem => pItem.CapitalRepayment.Value));

            _AssertSpecifiedInstallment(myContract.GetInstallment(0), 1, new DateTime(2006, 2, 1), 30, 70.46m, 1000, 70.46m, 30);
            _AssertSpecifiedInstallment(myContract.GetInstallment(1), 2, new DateTime(2006, 3, 1), 27.89m, 72.57m, 929.54m, 72.57m, 27.89m);
            _AssertSpecifiedInstallment(myContract.GetInstallment(2), 3, new DateTime(2006, 4, 3), 95m, 74.75m, 856.97m, 0m, 95m);
            _AssertSpecifiedInstallment(myContract.GetInstallment(3), 4, new DateTime(2006, 5, 2), 23.47m, 76.99m, 782.22m, 76.99m, 23.47m);
            _AssertSpecifiedInstallment(myContract.GetInstallment(4), 5, new DateTime(2006, 6, 1), 21.16m, 79.3m, 705.23m, 79.3m, 21.16m);
            _AssertSpecifiedInstallment(myContract.GetInstallment(5), 6, new DateTime(2006, 7, 3), 18.78m, 81.69m, 625.93m, 77.55m, 18.78m);
            _AssertSpecifiedInstallment(myContract.GetInstallment(6), 7, new DateTime(2006, 8, 1), 16.33m, 84.14m, 544.24m, 0, 0);
            _AssertSpecifiedInstallment(myContract.GetInstallment(7), 8, new DateTime(2006, 9, 1), 13.80m, 86.66m, 460.1m, 0, 0);
            _AssertSpecifiedInstallment(myContract.GetInstallment(8), 9, new DateTime(2006,10, 2), 11.20m, 89.27m, 373.44m, 0, 0);
            _AssertSpecifiedInstallment(myContract.GetInstallment(9), 10,new DateTime(2006,11, 1),  8.53m, 91.93m, 284.17m, 0, 0);
            _AssertSpecifiedInstallment(myContract.GetInstallment(10),11,new DateTime(2006,12, 1),  5.77m, 94.70m, 192.24m, 0, 0);
            _AssertSpecifiedInstallment(myContract.GetInstallment(11),12,new DateTime(2007, 1, 1),  2.93m, 97.54m, 97.54m, 0, 0);
        }

        [Test]
        public void TestRepayDecliningLoan()
        {
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.CALCULATIONLATEFEESDURINGPUBLICHOLIDAYS, true);
            LoanProduct package = new LoanProduct
                                      {
                                          InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
                                          LoanType = OLoanTypes.DecliningFixedInstallments,
                                          ChargeInterestWithinGracePeriod = true,
                                          Currency = new Currency { Id = 1, UseCents = true }
                                      };
            package.KeepExpectedInstallment = false;
            package.AnticipatedTotalRepaymentPenaltiesBase = OAnticipatedRepaymentPenaltiesBases.RemainingOLB;
            Loan myContract = new Loan(package, 1000, 0.03m, 12, 0, new DateTime(2006, 1, 1), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));
            myContract.BadLoan = false;

            myContract.NonRepaymentPenalties.OverDueInterest = 0.0016;
            myContract.NonRepaymentPenalties.OverDuePrincipal = 0.0016;

            myContract.AnticipatedTotalRepaymentPenalties = 0;

            RepaymentEvent rE1 = myContract.Repay(1, new DateTime(2006, 2, 1), 100.46m, false, false);
            _AssertSpecifiedEvent(rE1, 0, 30, 70.46m);

            RepaymentEvent rE2 = myContract.Repay(2, new DateTime(2006, 3, 1), 100.46m, false, false);
            _AssertSpecifiedEvent(rE2, 0, 27.89m, 72.57m);

            RepaymentEvent rE3 = myContract.Repay(3, new DateTime(2006, 5, 2), 50, true, false);
            _AssertSpecifiedEvent(rE3, 0, 25.71m, 24.29m);

            RepaymentEvent rE4 = myContract.Repay(3, new DateTime(2006, 6, 1), 55.22m, false, false);
            _AssertSpecifiedEvent(rE4, 4.76m, 0m, 50.46m);
        }

        private static void _AssertSpecifiedEvent(RepaymentEvent pEvent, OCurrency pFees, OCurrency pInterest, OCurrency pCapital)
        {
            Assert.AreEqual(pFees.Value, pEvent.Fees.Value);
            Assert.AreEqual(pInterest.Value, pEvent.Interests.Value);
            Assert.AreEqual(pCapital.Value, pEvent.Principal.Value);
        }

        [Test]
        public void TestCalculateOLBWhenRepayFlatLoanWith2DaysLateAndOverPaid()
        {
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.CALCULATIONLATEFEESDURINGPUBLICHOLIDAYS, true);
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.ACCOUNTINGPROCESS, OAccountingProcesses.Accrual);
            
            LoanProduct package = new LoanProduct
                                      {
                                          InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
                                          LoanType = OLoanTypes.Flat,
                                          ChargeInterestWithinGracePeriod = true,
                                          Currency = new Currency { Id = 1 }
                                      };
            package.KeepExpectedInstallment = false;
            package.AnticipatedTotalRepaymentPenaltiesBase = OAnticipatedRepaymentPenaltiesBases.RemainingOLB;
            Loan myContract = new Loan(package, 1000, 0.03m, 6, 1, new DateTime(2006, 1, 1), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));
            myContract.BadLoan = false;
            myContract.AnticipatedTotalRepaymentPenalties = 0.03;

            myContract.Repay(1, new DateTime(2006, 2, 1), 30, false, false);
            Assert.AreEqual(1000m, myContract.GetInstallment(0).OLB.Value);
            Assert.AreEqual(1000m, myContract.GetInstallment(1).OLB.Value);
            Assert.AreEqual(800m, myContract.GetInstallment(2).OLB.Value);
            Assert.AreEqual(600m, myContract.GetInstallment(3).OLB.Value);
            Assert.AreEqual(400m, myContract.GetInstallment(4).OLB.Value);
            Assert.AreEqual(200m, myContract.GetInstallment(5).OLB.Value);

            RepaymentEvent rE = myContract.Repay(2, new DateTime(2006, 3, 3), 1056, false, false);
            Assert.AreEqual(24, rE.Fees.Value);
            Assert.AreEqual(32, rE.Interests.Value);
            Assert.AreEqual(1000, rE.Principal.Value);

            Assert.AreEqual(1000, myContract.GetInstallment(0).OLB.Value);
            Assert.AreEqual(1000, myContract.GetInstallment(1).OLB.Value);
            Assert.AreEqual(800, myContract.GetInstallment(2).OLB.Value);
            Assert.AreEqual(0, myContract.GetInstallment(3).OLB.Value);
            Assert.AreEqual(0, myContract.GetInstallment(4).OLB.Value);
            Assert.AreEqual(0, myContract.GetInstallment(5).OLB.Value);
        }

        [Test]
        public void CalculateExpectedOLB2()
        {
            LoanProduct package = new LoanProduct
                                      {
                                          InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
                                          LoanType = OLoanTypes.Flat,
                                          ChargeInterestWithinGracePeriod = true,
                                          Currency = new Currency { Id = 1 }
                                      };

            package.KeepExpectedInstallment = false;
            package.AnticipatedTotalRepaymentPenaltiesBase = OAnticipatedRepaymentPenaltiesBases.RemainingOLB;
            
            Loan myContract = new Loan(package, 20000, 0.02m, 10, 0, new DateTime(2009, 1, 17), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));
            myContract.BadLoan = false;

            myContract.Repay(1, new DateTime(2009, 1, 17), 1000, true, false);

            Assert.AreEqual(19000, myContract.CalculateExpectedOlb(1, false).Value);
            Assert.AreEqual(17100, myContract.CalculateExpectedOlb(2, false).Value);
            Assert.AreEqual(15200, myContract.CalculateExpectedOlb(3, false).Value);
            Assert.AreEqual(13300, myContract.CalculateExpectedOlb(4, false).Value);
            Assert.AreEqual(11400, myContract.CalculateExpectedOlb(5, false).Value);
            Assert.AreEqual(9500, myContract.CalculateExpectedOlb(6, false).Value);
            Assert.AreEqual(7600, myContract.CalculateExpectedOlb(7, false).Value);
            Assert.AreEqual(5700, myContract.CalculateExpectedOlb(8, false).Value);
            Assert.AreEqual(3800, myContract.CalculateExpectedOlb(9, false).Value);
            Assert.AreEqual(1900, myContract.CalculateExpectedOlb(10, false).Value);
        }

        [Test]
        public void CalculateExpectedOLB_Flat_Accrual()
        {
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.ACCOUNTINGPROCESS, OAccountingProcesses.Accrual);

            LoanProduct package = new LoanProduct
                                      {
                                          InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
                                          LoanType = OLoanTypes.Flat,
                                          ChargeInterestWithinGracePeriod = true,
                                          Currency = new Currency { Id = 1, UseCents = true}
                                      };
            package.KeepExpectedInstallment = false;
            package.AnticipatedTotalRepaymentPenaltiesBase = OAnticipatedRepaymentPenaltiesBases.RemainingOLB;
            Loan myContract = new Loan(package, 1000, 0.03m, 6, 1, new DateTime(2009, 2, 1), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));
            myContract.BadLoan = false;

            myContract.Repay(1, new DateTime(2009, 3, 2), 31.07m, true, false);
            myContract.Repay(2, new DateTime(2009, 4, 1), 230, true, false);
            myContract.Repay(3, new DateTime(2009, 4, 7), 806m, true, false);

            Assert.AreEqual(1000, myContract.CalculateExpectedOlb(1, false).Value);
            Assert.AreEqual(1000m, myContract.CalculateExpectedOlb(2, false).Value);
            Assert.AreEqual(800m, myContract.CalculateExpectedOlb(3, false).Value);
            Assert.AreEqual(0m, myContract.CalculateExpectedOlb(4, false).Value);
            Assert.AreEqual(0m, myContract.CalculateExpectedOlb(5, false).Value);
            Assert.AreEqual(0m, myContract.CalculateExpectedOlb(6, false).Value);
        }

        [Test]
        public void TestCalculateOLBWhenRepayFlatLoanWithOverPaid()
        {
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.CALCULATIONLATEFEESDURINGPUBLICHOLIDAYS, true);
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.ACCOUNTINGPROCESS, OAccountingProcesses.Accrual);

            LoanProduct package = new LoanProduct
                                      {
                                          InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
                                          LoanType = OLoanTypes.Flat,
                                          ChargeInterestWithinGracePeriod = true,
                                          Currency = new Currency { Id = 1 }
                                      };
            package.KeepExpectedInstallment = false;
            package.AnticipatedTotalRepaymentPenaltiesBase = OAnticipatedRepaymentPenaltiesBases.RemainingOLB;
            Loan myContract = new Loan(package, 1000, 0.03m, 6, 1, new DateTime(2006, 1, 1), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));
            myContract.BadLoan = false;
            
            myContract.AnticipatedTotalRepaymentPenalties = 0.03;
            myContract.AnticipatedPartialRepaymentPenalties = 0.03;

            myContract.Repay(1, new DateTime(2006, 2, 1), 30, false, false);
            _AssertSpecifiedInstallment(myContract.GetInstallment(0), 1, new DateTime(2006, 2, 1), 30, 0, 1000, 0, 30);
            _AssertSpecifiedInstallment(myContract.GetInstallment(1), 2, new DateTime(2006, 3, 1), 30, 200, 1000, 0, 0);
            _AssertSpecifiedInstallment(myContract.GetInstallment(2), 3, new DateTime(2006, 4, 3), 30, 200, 800, 0, 0);
            _AssertSpecifiedInstallment(myContract.GetInstallment(3), 4, new DateTime(2006, 5, 2), 30, 200, 600, 0, 0);
            _AssertSpecifiedInstallment(myContract.GetInstallment(4), 5, new DateTime(2006, 6, 1), 30, 200, 400, 0, 0);
            _AssertSpecifiedInstallment(myContract.GetInstallment(5), 6, new DateTime(2006, 7, 3), 30, 200, 200, 0, 0);


            RepaymentEvent rE = myContract.Repay(2, new DateTime(2006, 3, 1), 654, false, false);
            Assert.AreEqual(24, rE.Fees.Value);
            Assert.AreEqual(30, rE.Interests.Value);
            Assert.AreEqual(600m, rE.Principal.Value);
            _AssertSpecifiedInstallment(myContract.GetInstallment(0), 1, new DateTime(2006, 2, 1), 30, 0, 1000, 0, 30);
            _AssertSpecifiedInstallment(myContract.GetInstallment(1), 2, new DateTime(2006, 3, 1), 30, 200, 1000, 200, 30);
            _AssertSpecifiedInstallment(myContract.GetInstallment(2), 3, new DateTime(2006, 4, 3), 12, 100, 400, 0, 0);
            _AssertSpecifiedInstallment(myContract.GetInstallment(3), 4, new DateTime(2006, 5, 2), 12, 100, 300, 0, 0);
            _AssertSpecifiedInstallment(myContract.GetInstallment(4), 5, new DateTime(2006, 6, 1), 12, 100, 200, 0, 0);
            _AssertSpecifiedInstallment(myContract.GetInstallment(5), 6, new DateTime(2006, 7, 3), 12, 100, 100, 0, 0);
        }

        [Test]
        public void TestCalculateOLBWhenRepayTotallyFlatLoanWithOverPaid()
        {
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.CALCULATIONLATEFEESDURINGPUBLICHOLIDAYS, true);
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.ACCOUNTINGPROCESS, OAccountingProcesses.Accrual);

            LoanProduct package = new LoanProduct
                                      {
                                          InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
                                          LoanType = OLoanTypes.Flat,
                                          ChargeInterestWithinGracePeriod = true,
                                          Currency = new Currency { Id = 1 }
                                      };
            package.KeepExpectedInstallment = false;
            package.AnticipatedTotalRepaymentPenaltiesBase = OAnticipatedRepaymentPenaltiesBases.RemainingOLB;
            Loan myContract = new Loan(package, 1000, 0.03m, 6, 1, new DateTime(2006, 1, 1), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));
            myContract.BadLoan = false;
            myContract.AnticipatedTotalRepaymentPenalties = 0.03;
            myContract.AnticipatedPartialRepaymentPenalties = 0.03;

            myContract.Repay(1, new DateTime(2006, 2, 1), 30, false, false);

            RepaymentEvent rE = myContract.Repay(2, new DateTime(2006, 3, 1), 654, false, false);
            Assert.AreEqual(24, rE.Fees.Value);
            Assert.AreEqual(30, rE.Interests.Value);
            Assert.AreEqual(600, rE.Principal.Value);

            Assert.AreEqual(1000m, myContract.GetInstallment(0).OLB.Value);
            Assert.AreEqual(1000m, myContract.GetInstallment(1).OLB.Value);
            Assert.AreEqual(400m, myContract.GetInstallment(2).OLB.Value);
            Assert.AreEqual(300m, myContract.GetInstallment(3).OLB.Value);
            Assert.AreEqual(200m, myContract.GetInstallment(4).OLB.Value);
            Assert.AreEqual(100m, myContract.GetInstallment(5).OLB.Value);
        }

        [Test]
        public void TestCalculateOLBWhenRepayFlatLoanWith1MonthsLate()
        {
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.CALCULATIONLATEFEESDURINGPUBLICHOLIDAYS, true);
            LoanProduct package = new LoanProduct
                                      {
                                          InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
                                          LoanType = OLoanTypes.Flat,
                                          ChargeInterestWithinGracePeriod = true,
                                          Currency = new Currency { Id = 1 }
                                      };
            package.KeepExpectedInstallment = false;
            package.AnticipatedTotalRepaymentPenaltiesBase = OAnticipatedRepaymentPenaltiesBases.RemainingOLB;
            Loan myContract = new Loan(package, 1000, 0.03m, 6, 1, new DateTime(2006, 1, 1), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));
            myContract.BadLoan = false;
            myContract.AnticipatedTotalRepaymentPenalties = 0.03;

            myContract.Repay(1, new DateTime(2006, 2, 1), 30, false, false);

            RepaymentEvent rE = myContract.Repay(2, new DateTime(2006, 4, 3), 460, false, false);
            Assert.AreEqual(0m, rE.Fees.Value);
            Assert.AreEqual(60m, rE.Interests.Value);
            Assert.AreEqual(400m, rE.Principal.Value);

            Assert.AreEqual(1000m, myContract.GetInstallment(0).OLB.Value);
            Assert.AreEqual(1000m, myContract.GetInstallment(1).OLB.Value);
            Assert.AreEqual(800m, myContract.GetInstallment(2).OLB.Value);
            Assert.AreEqual(600m, myContract.GetInstallment(3).OLB.Value);
            Assert.AreEqual(400m, myContract.GetInstallment(4).OLB.Value);
            Assert.AreEqual(200m, myContract.GetInstallment(5).OLB.Value);
        }

        [Test]
        public void TestCalculateMaximumAmountWhenRepayFlatLoanWith1MonthLate()
        {
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.CALCULATIONLATEFEESDURINGPUBLICHOLIDAYS, true);
            LoanProduct package = new LoanProduct
                                      {
                                          InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
                                          LoanType = OLoanTypes.Flat,
                                          ChargeInterestWithinGracePeriod = true,
                                          Currency = new Currency { Id = 1 }
                                      };
            package.KeepExpectedInstallment = false;
            package.AnticipatedTotalRepaymentPenaltiesBase = OAnticipatedRepaymentPenaltiesBases.RemainingOLB;
            Loan myContract = new Loan(package, 1000, 0.03m, 6, 1, new DateTime(2006, 1, 1), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));
            myContract.BadLoan = false;
            myContract.AnticipatedTotalRepaymentPenalties = 0.03;

            myContract.Repay(1, new DateTime(2006, 2, 1), 30, false, false);

            CreditContractOptions cCO = new CreditContractOptions(package.LoanType, package.KeepExpectedInstallment, false, 0, 0, false, 0,
                                                                  package.AnticipatedTotalRepaymentPenaltiesBase);
            CreditContractRepayment cCR = new CreditContractRepayment(myContract, cCO, new DateTime(2006, 4, 3), 2, new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""));

            Assert.AreEqual(1078, Math.Round(cCR.MaximumAmountAuthorizeToRepay.Value, 2));
        }

        [Test]
        public void TestCalculateOLB()
        {
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.CALCULATIONLATEFEESDURINGPUBLICHOLIDAYS, true);
            LoanProduct package = new LoanProduct
                                      {
                                          InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
                                          LoanType = OLoanTypes.Flat,
                                          ChargeInterestWithinGracePeriod = true,
                                          Currency = new Currency { Id = 1 }
                                      };
            package.KeepExpectedInstallment = false;
            package.AnticipatedTotalRepaymentPenaltiesBase = OAnticipatedRepaymentPenaltiesBases.RemainingOLB;
            Loan myContract = new Loan(package, 1000, 0.03m, 6, 1, new DateTime(2006, 1, 1), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));
            myContract.BadLoan = false;
            myContract.AnticipatedTotalRepaymentPenalties = 0.03;

            Assert.AreEqual(1000m,myContract.CalculateExpectedOlb(1,false).Value);
            Assert.AreEqual(1000m, myContract.CalculateExpectedOlb(2, false).Value);
            Assert.AreEqual(800m, myContract.CalculateExpectedOlb(3, false).Value);
            Assert.AreEqual(600m, myContract.CalculateExpectedOlb(4, false).Value);
            Assert.AreEqual(400m, myContract.CalculateExpectedOlb(5, false).Value);
            Assert.AreEqual(200m, myContract.CalculateExpectedOlb(6, false).Value);
        }

        [Test]
        public void TestCalculateOLBWith2MonthOfGracePeriodWhenDecliningExotic()
        {
            Assert.Ignore();
            LoanProduct package = new LoanProduct
                                      {
                                          InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
                                          LoanType = OLoanTypes.DecliningFixedInstallments,
                                          ChargeInterestWithinGracePeriod = true,
                                          Currency = new Currency { Id = 1 }
                                      };

            ExoticInstallmentsTable exoticProduct = new ExoticInstallmentsTable();
            exoticProduct.Add(new ExoticInstallment(1, 0.1, null));
            exoticProduct.Add(new ExoticInstallment(2, 0.1, null));
            exoticProduct.Add(new ExoticInstallment(3, 0.1, null));
            exoticProduct.Add(new ExoticInstallment(4, 0.1, null));
            exoticProduct.Add(new ExoticInstallment(5, 0.1, null));
            exoticProduct.Add(new ExoticInstallment(6, 0.5, null));
            package.ExoticProduct = exoticProduct;

            Loan myContract = new Loan(package, 1000, 0.03m, 8, 2, new DateTime(2006, 1, 1), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));
            myContract.Repay(1, new DateTime(2006, 2, 1), 100, true, false);

            Assert.AreEqual(1000m, myContract.CalculateExpectedOlb(1, false).Value);
            Assert.AreEqual(930m, myContract.CalculateExpectedOlb(2, false).Value);
            Assert.AreEqual(930m, myContract.CalculateExpectedOlb(3, false).Value);
            Assert.AreEqual(837m, myContract.CalculateExpectedOlb(4, false).Value);
            Assert.AreEqual(744m, myContract.CalculateExpectedOlb(5, false).Value);
            Assert.AreEqual(651m, myContract.CalculateExpectedOlb(6, false).Value);
            Assert.AreEqual(558m, myContract.CalculateExpectedOlb(7, false).Value);
            Assert.AreEqual(465m, myContract.CalculateExpectedOlb(8, false).Value);

        }
        [Test]
        public void TestCalculateOLBWith2MonthOfGracePeriod()
        {
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.CALCULATIONLATEFEESDURINGPUBLICHOLIDAYS, true);
            LoanProduct package = new LoanProduct
                                      {
                                          InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
                                          LoanType = OLoanTypes.Flat,
                                          ChargeInterestWithinGracePeriod = true,
                                          Currency = new Currency { Id = 1 }
                                      };
            package.KeepExpectedInstallment = false;
            package.AnticipatedTotalRepaymentPenaltiesBase = OAnticipatedRepaymentPenaltiesBases.RemainingOLB;
            Loan myContract = new Loan(package, 1000, 0.03m, 7, 2, new DateTime(2006, 1, 1), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));
            myContract.BadLoan = false;
            myContract.AnticipatedTotalRepaymentPenalties = 0.03;

            Assert.AreEqual(1000m, myContract.CalculateExpectedOlb(1, false).Value);
            Assert.AreEqual(1000m, myContract.CalculateExpectedOlb(2, false).Value);
            Assert.AreEqual(1000m, myContract.CalculateExpectedOlb(3, false).Value);
            Assert.AreEqual(800m, myContract.CalculateExpectedOlb(4, false).Value);
            Assert.AreEqual(600m, myContract.CalculateExpectedOlb(5, false).Value);
            Assert.AreEqual(400m, myContract.CalculateExpectedOlb(6, false).Value);
            Assert.AreEqual(200m, myContract.CalculateExpectedOlb(7, false).Value);
        }

        [Test]
        public void TestCalculateOLB2()
        {
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.CALCULATIONLATEFEESDURINGPUBLICHOLIDAYS, true);
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.ACCOUNTINGPROCESS, OAccountingProcesses.Accrual);

            LoanProduct package = new LoanProduct
                                      {
                                          InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
                                          LoanType = OLoanTypes.Flat,
                                          ChargeInterestWithinGracePeriod = true,
                                          Currency = new Currency {Id = 1},
                                      };

            package.NonRepaymentPenalties.OLB = 0.001; 

            package.KeepExpectedInstallment = false;
            package.AnticipatedTotalRepaymentPenaltiesBase = OAnticipatedRepaymentPenaltiesBases.RemainingOLB;
            
            Loan myContract = new Loan(package, 1000, 0.03m, 6, 1, new DateTime(2006, 1, 1), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));
            myContract.BadLoan = false;
            
            myContract.AnticipatedTotalRepaymentPenalties = 0.03;
            myContract.AnticipatedPartialRepaymentPenalties = 0.03;

            myContract.NonRepaymentPenalties.OLB = 0.001;

            myContract.Repay(1, new DateTime(2006, 2, 1), 30, false, false);

            RepaymentEvent rE = myContract.Repay(2, new DateTime(2006, 3, 1), 654, false, false);
            Assert.AreEqual(24, rE.Fees.Value);
            Assert.AreEqual(30, rE.Interests.Value);
            Assert.AreEqual(600, rE.Principal.Value);

            Assert.AreEqual(1000, myContract.CalculateExpectedOlb(1, false).Value);
            Assert.AreEqual(1000, myContract.CalculateExpectedOlb(2, false).Value);
            Assert.AreEqual(400, myContract.CalculateExpectedOlb(3, false).Value);
            Assert.AreEqual(300, myContract.CalculateExpectedOlb(4, false).Value);
            Assert.AreEqual(200, myContract.CalculateExpectedOlb(5, false).Value);
            Assert.AreEqual(100, myContract.CalculateExpectedOlb(6, false).Value);
        }

        [Test]
        public void TestCalculateOLB3()
        {
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.CALCULATIONLATEFEESDURINGPUBLICHOLIDAYS, true);
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.ACCOUNTINGPROCESS, OAccountingProcesses.Accrual);
            
            LoanProduct package = new LoanProduct
                                      {
                                          InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
                                          LoanType = OLoanTypes.Flat,
                                          ChargeInterestWithinGracePeriod = true,
                                          Currency = new Currency { Id = 1, UseCents = true }
                                      };
            package.KeepExpectedInstallment = false;
            package.AnticipatedTotalRepaymentPenaltiesBase = OAnticipatedRepaymentPenaltiesBases.RemainingOLB;
            Loan myContract = new Loan(package, 1000, 0.03m, 6, 1, new DateTime(2006, 1, 1), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));
            myContract.BadLoan = false;
            myContract.AnticipatedTotalRepaymentPenalties = 0.03;

            myContract.Repay(1, new DateTime(2006, 2, 1), 30, false, false);

            RepaymentEvent rE = myContract.Repay(2, new DateTime(2006, 3, 3), 1055.82m, false, false);
            Assert.AreEqual(24, rE.Fees.Value);
            Assert.AreEqual(31.82m, rE.Interests.Value);
            Assert.AreEqual(1000, rE.Principal.Value);

            Assert.AreEqual(1000, myContract.CalculateExpectedOlb(1, false).Value);
            Assert.AreEqual(1000, myContract.CalculateExpectedOlb(2, false).Value);
            Assert.AreEqual(800m, myContract.CalculateExpectedOlb(3, false).Value);
            Assert.AreEqual(0, myContract.CalculateExpectedOlb(4, false).Value);
            Assert.AreEqual(0, myContract.CalculateExpectedOlb(5, false).Value);
            Assert.AreEqual(0, myContract.CalculateExpectedOlb(6, false).Value);
        }

        [Test]
        public void TestCalculateOLB6()
        {
            LoanProduct package = new LoanProduct
                                      {
                                          InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
                                          LoanType = OLoanTypes.Flat,
                                          ChargeInterestWithinGracePeriod = true,
                                          Currency = new Currency { Id = 1 }
                                      };
            package.KeepExpectedInstallment = false;
            package.AnticipatedTotalRepaymentPenaltiesBase = OAnticipatedRepaymentPenaltiesBases.RemainingOLB;
            Loan myContract = new Loan(package, 1000, 0.03m, 6, 1, new DateTime(2006, 1, 1), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));
            myContract.BadLoan = false;
            myContract.AnticipatedTotalRepaymentPenalties = 0.03;

            myContract.Repay(1, new DateTime(2006, 2, 1), 30, false, false);

            RepaymentEvent rE = myContract.Repay(2, new DateTime(2006, 3, 3), 1000, false, true);
            Assert.AreEqual(0m, rE.Fees.Value);
            Assert.AreEqual(150m, rE.Interests.Value);
            Assert.AreEqual(850m, rE.Principal.Value);

            Assert.AreEqual(1000m, myContract.CalculateExpectedOlb(1, true).Value);
            Assert.AreEqual(1000m, myContract.CalculateExpectedOlb(2, true).Value);
            Assert.AreEqual(800m, myContract.CalculateExpectedOlb(3, true).Value);
            Assert.AreEqual(600m, myContract.CalculateExpectedOlb(4, true).Value);
            Assert.AreEqual(400m, myContract.CalculateExpectedOlb(5, true).Value);
            Assert.AreEqual(200m, myContract.CalculateExpectedOlb(6, true).Value);
        }

        [Test]
        public void TestCalculateExpectedOLB()
        {
            LoanProduct package = new LoanProduct
                                      {
                                          InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
                                          LoanType = OLoanTypes.Flat,
                                          ChargeInterestWithinGracePeriod = true,
                                          Currency = new Currency { Id = 1 }
                                      };
            package.KeepExpectedInstallment = false;
            package.AnticipatedTotalRepaymentPenaltiesBase = OAnticipatedRepaymentPenaltiesBases.RemainingOLB;
            Loan myContract = new Loan(package, 20000, 0.02m, 10, 0, new DateTime(2009, 1, 17), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));
            myContract.BadLoan = false;

            myContract.Repay(1, new DateTime(2009, 1, 17), 10000, true, false);

            Assert.AreEqual(10000, myContract.CalculateExpectedOlb(1, false).Value);
            Assert.AreEqual(9000, myContract.CalculateExpectedOlb(2, false).Value);
            Assert.AreEqual(8000, myContract.CalculateExpectedOlb(3, false).Value);
            Assert.AreEqual(7000, myContract.CalculateExpectedOlb(4, false).Value);
            Assert.AreEqual(6000, myContract.CalculateExpectedOlb(5, false).Value);
            Assert.AreEqual(5000, myContract.CalculateExpectedOlb(6, false).Value);
            Assert.AreEqual(4000, myContract.CalculateExpectedOlb(7, false).Value);
            Assert.AreEqual(3000, myContract.CalculateExpectedOlb(8, false).Value);
            Assert.AreEqual(2000, myContract.CalculateExpectedOlb(9, false).Value);
            Assert.AreEqual(1000, myContract.CalculateExpectedOlb(10, false).Value);
        }

        [Test]
        public void TestCalculateOLB4()
        {
            LoanProduct package = new LoanProduct
                                      {
                                          InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
                                          LoanType = OLoanTypes.DecliningFixedInstallments,
                                          ChargeInterestWithinGracePeriod = true,
                                          Currency = new Currency { Id = 1, UseCents = true}
                                      };
            package.KeepExpectedInstallment = false;
            package.AnticipatedTotalRepaymentPenaltiesBase = OAnticipatedRepaymentPenaltiesBases.RemainingOLB;
            Loan myContract = new Loan(package, 1000, 0.03m, 6, 0, new DateTime(2006, 1, 1), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));
            myContract.BadLoan = false;
            myContract.AnticipatedTotalRepaymentPenalties = 0.03;

            Assert.AreEqual(1000m, myContract.CalculateExpectedOlb(1, false).Value);
            Assert.AreEqual(845.40m, myContract.CalculateExpectedOlb(2, false).Value);
            Assert.AreEqual(686.16m, myContract.CalculateExpectedOlb(3, false).Value);
            Assert.AreEqual(522.14m, myContract.CalculateExpectedOlb(4, false).Value);
            Assert.AreEqual(353.21m, myContract.CalculateExpectedOlb(5, false).Value);
            Assert.AreEqual(179.22m, myContract.CalculateExpectedOlb(6, false).Value);

            myContract.Repay(1,new DateTime(2006,2,1),184.60m,false,false);
	
            RepaymentEvent rE = myContract.Repay(2,new DateTime(2006,4,3),369.2m,false,false);
            Assert.AreEqual(0m, rE.Fees.Value);
            Assert.AreEqual(45.94m, rE.Interests.Value);
            Assert.AreEqual(323.26m, rE.Principal.Value);

            Assert.AreEqual(1000, myContract.CalculateExpectedOlb(1, false).Value);
            Assert.AreEqual(845.40m, myContract.CalculateExpectedOlb(2, false).Value);
            Assert.AreEqual(686.16m, myContract.CalculateExpectedOlb(3, false).Value);
            Assert.AreEqual(522.14m, myContract.CalculateExpectedOlb(4, false).Value);
            Assert.AreEqual(353.21m, myContract.CalculateExpectedOlb(5, false).Value);
            Assert.AreEqual(179.22m, myContract.CalculateExpectedOlb(6, false).Value);
        }

        [Test]
        public void TestRepayDecliningLoan2()
        {
            LoanProduct package = new LoanProduct
                                      {
                                          InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
                                          LoanType = OLoanTypes.DecliningFixedInstallments,
                                          ChargeInterestWithinGracePeriod = true,
                                          Currency = new Currency { Id = 1, UseCents = true }
                                      };
            package.KeepExpectedInstallment = false;
            package.AnticipatedTotalRepaymentPenaltiesBase = OAnticipatedRepaymentPenaltiesBases.RemainingOLB;
            Loan myContract = new Loan(package, 1000, 0.03m, 12, 0, new DateTime(2006, 1, 1), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));
            myContract.BadLoan = false;
            myContract.NonRepaymentPenalties.OverDuePrincipal = 0.0016;
            myContract.NonRepaymentPenalties.OverDueInterest = 0.0016;
            myContract.AnticipatedTotalRepaymentPenalties = 0;

            RepaymentEvent rE1 = myContract.Repay(1,new DateTime(2006,2,1),100.46m,false,false);
            Assert.AreEqual(0, rE1.Fees.Value);
            Assert.AreEqual(30, rE1.Interests.Value);
            Assert.AreEqual(70.46m, rE1.Principal.Value);

            RepaymentEvent rE2 = myContract.Repay(2,new DateTime(2006,3,6),51.04m,false,false);
            Assert.AreEqual(0.80m, rE2.Fees.Value);
            Assert.AreEqual(27.89m, rE2.Interests.Value);
            Assert.AreEqual(22.35m, rE2.Principal.Value);

            Assert.AreEqual(50.22m,myContract.GetInstallment(1).AmountHasToPayWithInterest.Value);

        }

        [Test]
        public void TestRepayDecliningLoan3()
        {
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.CALCULATIONLATEFEESDURINGPUBLICHOLIDAYS, true);
            LoanProduct package = new LoanProduct
                                      {
                                          InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
                                          LoanType = OLoanTypes.DecliningFixedInstallments,
                                          ChargeInterestWithinGracePeriod = true,
                                          Currency = new Currency { Id = 1 },
                                          RoundingType = ORoundingType.Approximate
                                      };
            package.KeepExpectedInstallment = false;

            package.AnticipatedTotalRepaymentPenaltiesBase = OAnticipatedRepaymentPenaltiesBases.RemainingOLB;
            Loan myContract = new Loan(package, 1000, 0.03m, 12, 0, new DateTime(2006, 1, 1), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));
            myContract.BadLoan = false;
            myContract.NonRepaymentPenalties.OverDuePrincipal = 0.0016;
            myContract.NonRepaymentPenalties.OverDueInterest = 0.0016;
            myContract.AnticipatedTotalRepaymentPenalties = 0;

            myContract.Repay(1,new DateTime(2006,2,1),100.46m,false,false);
            Assert.AreEqual(new DateTime(2006,2,1),myContract.GetInstallment(0).PaidDate.Value);

            myContract.Repay(2,new DateTime(2006,3,6),51.04m,true,false);
            Assert.AreEqual(new DateTime(2006,3,6),myContract.GetInstallment(1).PaidDate.Value);

            myContract.Repay(2,new DateTime(2006,3,9),20m,true,false);
            Assert.AreEqual(new DateTime(2006,3,9),myContract.GetInstallment(1).PaidDate.Value);

            myContract.Repay(2,new DateTime(2006,3,21),20m,true,false);
            Assert.AreEqual(new DateTime(2006,3,21),myContract.GetInstallment(1).PaidDate.Value);
        }

        [Test]
        public void TestRepayFlatLoan()
        {
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.ACCOUNTINGPROCESS, OAccountingProcesses.Cash);

            LoanProduct package = new LoanProduct
                                      {
                                          InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
                                          LoanType = OLoanTypes.Flat,
                                          ChargeInterestWithinGracePeriod = true,
                                          Currency = new Currency { Id = 1, UseCents = true }
                                      };
            package.KeepExpectedInstallment = false;

            package.AnticipatedTotalRepaymentPenaltiesBase = OAnticipatedRepaymentPenaltiesBases.RemainingOLB;
            Loan myContract = new Loan(package, 1000, 0.03m, 6, 1, new DateTime(2006, 1, 1), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));
            myContract.BadLoan = true;
            myContract.NonRepaymentPenalties.InitialAmount = 0.003;
            myContract.AnticipatedTotalRepaymentPenalties = 0.01;

            CreditContractOptions cCO = new CreditContractOptions(package.LoanType, package.KeepExpectedInstallment, false, 0, 0, false, 0,
                                                                  package.AnticipatedTotalRepaymentPenaltiesBase);
            CreditContractRepayment cCR = new CreditContractRepayment(myContract, cCO, new DateTime(2006, 3, 15), 1, new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""));
            Assert.AreEqual(1194,Math.Round(cCR.MaximumAmountAuthorizeToRepay.Value,2));
            Assert.AreEqual(386,Math.Round(cCR.MaximumAmountToRegradingLoan.Value,2));

            RepaymentEvent rPE = myContract.Repay(1,new DateTime(2006,3,15),386,false,false);
            Assert.AreEqual(126m,rPE.Fees.Value);
            Assert.AreEqual(60m,rPE.Interests.Value);
            Assert.AreEqual(200m,rPE.Principal.Value);

            CreditContractRepayment cCR2 = new CreditContractRepayment(myContract, cCO, new DateTime(2006, 4, 3), 3, new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""));
            Assert.AreEqual(836,Math.Round(cCR2.MaximumAmountAuthorizeToRepay.Value,2));
        }

        [Test]
        public void TestRepayFlatLoan2()
        {
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.ACCOUNTINGPROCESS, OAccountingProcesses.Cash);

            LoanProduct package = new LoanProduct
                                      {
                                          InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
                                          LoanType = OLoanTypes.Flat,
                                          ChargeInterestWithinGracePeriod = true,
                                          Currency = new Currency { Id = 1, UseCents = true }
                                      };
            package.KeepExpectedInstallment = false;

            package.AnticipatedTotalRepaymentPenaltiesBase = OAnticipatedRepaymentPenaltiesBases.RemainingOLB;
            Loan myContract = new Loan(package, 1000, 0.03m, 6, 1, new DateTime(2006, 1, 1), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));
            myContract.BadLoan = true;
            myContract.NonRepaymentPenalties.InitialAmount = 0.003;
            myContract.AnticipatedTotalRepaymentPenalties = 0.01;

            CreditContractOptions cCO = new CreditContractOptions(package.LoanType, package.KeepExpectedInstallment, false, 0, 0, false, 0,
                                                                  package.AnticipatedTotalRepaymentPenaltiesBase);
            CreditContractRepayment cCR = new CreditContractRepayment(myContract, cCO, new DateTime(2006, 3, 15), 1, new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""));
            Assert.AreEqual(1194,Math.Round(cCR.MaximumAmountAuthorizeToRepay.Value,2));
            Assert.AreEqual(386,Math.Round(cCR.MaximumAmountToRegradingLoan.Value,2));

            Assert.AreEqual(42, myContract.CalculatePastDueSinceLastRepayment(new DateTime(2006, 3, 15)));
            RepaymentEvent rPE = myContract.Repay(1,new DateTime(2006,3,15),386,false,false);
            Assert.AreEqual(126m,rPE.Fees.Value);
            Assert.AreEqual(60m,rPE.Interests.Value);
            Assert.AreEqual(200m,rPE.Principal.Value);
            Assert.IsFalse(myContract.BadLoan);

            Assert.AreEqual(0, myContract.CalculatePastDueSinceLastRepayment(new DateTime(2006, 3, 15)));
            CreditContractRepayment cCR2 = new CreditContractRepayment(myContract, cCO, new DateTime(2006, 3, 15), 3, new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""));
            Assert.AreEqual(0, Math.Round(cCR2.MaximumAmountToRegradingLoan.Value,2));
            Assert.AreEqual(808, Math.Round(cCR2.MaximumAmountAuthorizeToRepay.Value,2));
			
            RepaymentEvent rPE2 = myContract.Repay(3,new DateTime(2006,3,15), 808m, false, false);
            Assert.AreEqual(8m, rPE2.Fees.Value);
            Assert.AreEqual(0m, rPE2.Interests.Value);
            Assert.AreEqual(800m, rPE2.Principal.Value);

            Assert.IsTrue(myContract.AllInstallmentsRepaid);
        }

        [Test]
        public void TestRepayFlatLoanWhenCalculateFeesWithDelta()
        {
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.CALCULATIONLATEFEESDURINGPUBLICHOLIDAYS, true);

            var package = new LoanProduct
                                      {
                                          InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
                                          LoanType = OLoanTypes.Flat,
                                          ChargeInterestWithinGracePeriod = true,
                                          Currency = new Currency {Id = 1, UseCents = true}
                                      };
            package.KeepExpectedInstallment = false;

            package.AnticipatedTotalRepaymentPenaltiesBase = OAnticipatedRepaymentPenaltiesBases.RemainingOLB;
            var myContract = new Loan(package, 1000, 0.03m, 5, 0, new DateTime(2006, 1, 1), new User(), 
                ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), 
                ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));
            myContract.BadLoan = false;
            myContract.NonRepaymentPenalties.OLB = 0.003;
            myContract.AnticipatedTotalRepaymentPenalties = 0;

            Assert.AreEqual(14, myContract.CalculatePastDueSinceLastRepayment(new DateTime(2006, 2, 15)));
            RepaymentEvent rPE = myContract.Repay(1, new DateTime(2006, 2, 15), 80, false, false);
            Assert.AreEqual(42m, rPE.Fees.Value);
            Assert.AreEqual(30, rPE.Interests.Value);
            Assert.AreEqual(8, rPE.Principal.Value);

            Assert.AreEqual(13, myContract.CalculatePastDueSinceLastRepayment(new DateTime(2006, 2, 28)));
            RepaymentEvent rPE2 = myContract.Repay(1, new DateTime(2006, 2, 28), 231m, false, false);

            Assert.AreEqual(38.69, rPE2.Fees.Value);
            Assert.AreEqual(0.31, rPE2.Interests.Value);
            Assert.AreEqual(192m, rPE2.Principal.Value);
            Assert.IsTrue(myContract.GetInstallment(0).IsRepaid);
        }

        [Test]
        public void TestCalculatePastDue()
        {
            LoanProduct package = new LoanProduct
                                      {
                                          InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
                                          LoanType = OLoanTypes.Flat,
                                          ChargeInterestWithinGracePeriod = true,
                                          Currency = new Currency { Id = 1 }
                                      };
            package.KeepExpectedInstallment = false;

            package.AnticipatedTotalRepaymentPenaltiesBase = OAnticipatedRepaymentPenaltiesBases.RemainingOLB;
            Loan myContract = new Loan(package, 1000, 0.03m, 5, 0, new DateTime(2006, 1, 1), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));
            myContract.BadLoan = false;
            myContract.NonRepaymentPenalties.InitialAmount = 0.003;
            myContract.AnticipatedTotalRepaymentPenalties = 0;

            Assert.AreEqual(14, myContract.CalculatePastDueSinceLastRepayment(new DateTime(2006, 2, 15)));
            myContract.Repay(1,new DateTime(2006, 2, 15), 80, false, false);

            Assert.AreEqual(13, myContract.CalculatePastDueSinceLastRepayment(new DateTime(2006, 2, 28)));
        }

        [Test]
        public void TestRepayFlatLoanWhenCalculateFeesWithDelta2()
        {
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.CALCULATIONLATEFEESDURINGPUBLICHOLIDAYS, true);
            LoanProduct package = new LoanProduct
                                      {
                                          InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
                                          LoanType = OLoanTypes.DecliningFixedInstallments,
                                          ChargeInterestWithinGracePeriod = true,
                                          Currency = new Currency {Id = 1},
                                          KeepExpectedInstallment = false
                                      };

            Loan myContract = new Loan(package, 600, 0.03m, 6, 0, new DateTime(2006, 1, 4), new User(),
                                       ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""),
                                       ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()))
                                  {
                                      BadLoan = false,
                                      NonRepaymentPenalties = {OverDueInterest = 0.02, OverDuePrincipal = 0.02},
                                      AnticipatedTotalRepaymentPenalties = 0
                                  };

            Assert.AreEqual(new DateTime(2006, 2, 6), myContract.GetInstallment(0).ExpectedDate);

            RepaymentEvent rPE = myContract.Repay(1, new DateTime(2006, 3, 6), 50, false, false);

            Assert.AreEqual(50m, rPE.Fees.Value);
            Assert.AreEqual(0m, rPE.Interests.Value);
            Assert.AreEqual(0m, rPE.Principal.Value);

            CreditContractOptions cCO = new CreditContractOptions(package.LoanType,
                                                                  package.KeepExpectedInstallment,
                                                                  false,
                                                                  0, 0,
                                                                  false,
                                                                  0,
                                                                  package.AnticipatedTotalRepaymentPenaltiesBase);
            CreditContractRepayment cCR = new CreditContractRepayment(myContract, cCO, new DateTime(2006, 4, 4), 1, new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""));

            Assert.AreEqual(414m, cCR.MaximumAmountToRegradingLoan.Value);
        }

        [Test]
        public void TestRounding()
        {
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.CALCULATIONLATEFEESDURINGPUBLICHOLIDAYS, true);
            LoanProduct package = new LoanProduct
                                      {
                                          InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
                                          LoanType = OLoanTypes.Flat,
                                          ChargeInterestWithinGracePeriod = true,
                                          Currency = new Currency { Id = 1 }
                                      };
            package.KeepExpectedInstallment = true;
            package.AnticipatedTotalRepaymentPenaltiesBase = OAnticipatedRepaymentPenaltiesBases.RemainingOLB;
            Loan myContract = new Loan(package, 1000, 0.03m, 6, 1, new DateTime(2006, 1, 1), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));
            myContract.BadLoan = false;
            myContract.NonRepaymentPenalties.InitialAmount = 0.003;
            myContract.AnticipatedTotalRepaymentPenalties = 0.01;

            CreditContractOptions cCO = new CreditContractOptions(package.LoanType, package.KeepExpectedInstallment, false, 0, 0, false, 0,
                                                                  package.AnticipatedTotalRepaymentPenaltiesBase);
            CreditContractRepayment cCR = new CreditContractRepayment(myContract, cCO, new DateTime(2006, 2, 1), 1, new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""));
            Assert.AreEqual(1180, Math.Round(cCR.MaximumAmountAuthorizeToRepay.Value, 2));

            myContract.Repay(1,new DateTime(2006,2,1),1179.97m,false,true);
            Assert.AreEqual(0,myContract.GetInstallment(0).PaidCapital.Value);
            Assert.AreEqual(200, myContract.GetInstallment(1).PaidCapital.Value);
            Assert.AreEqual(200, myContract.GetInstallment(2).PaidCapital.Value);
            Assert.AreEqual(200, myContract.GetInstallment(3).PaidCapital.Value);
            Assert.AreEqual(200, myContract.GetInstallment(4).PaidCapital.Value);
            Assert.AreEqual(199.97m, myContract.GetInstallment(5).PaidCapital.Value);
        }

        [Test]
        public void TestIfExtraAmountIsCorrectlySpreadOutWhenGreaterThanInstallmentAmountAndTotalInterestsAmountForFlatRateNoExotic()
        {
            Assert.Ignore();
            LoanProduct package = new LoanProduct
            {
                InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
                LoanType = OLoanTypes.Flat,
                ChargeInterestWithinGracePeriod = true,
                KeepExpectedInstallment = false,
                Currency = new Currency { Id = 1 }
            };

            Loan myContract = new Loan(package, 1000, 0.03m, 6, 2, new DateTime(2006, 1, 1), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));

            myContract.Repay(1, new DateTime(2006, 2, 1), 30, true, false);
            myContract.Repay(2, new DateTime(2006, 3, 1), 30, true, false);
            myContract.Repay(3, new DateTime(2006, 4, 3), 540, true, false);

            _AssertSpecifiedInstallment(myContract.GetInstallment(0), 1, new DateTime(2006, 2, 1), 30, 0, 1000, 0, 30);
            _AssertSpecifiedInstallment(myContract.GetInstallment(1), 2, new DateTime(2006, 3, 1), 30, 0, 1000, 0, 30);
            _AssertSpecifiedInstallment(myContract.GetInstallment(2), 3, new DateTime(2006, 4, 3), 30, 250, 1000, 250, 30);
            _AssertSpecifiedInstallment(myContract.GetInstallment(3), 4, new DateTime(2006, 5, 2), 14.7m, 163.33m, 490, 0, 0);
            _AssertSpecifiedInstallment(myContract.GetInstallment(4), 5, new DateTime(2006, 6, 1), 14.7m, 163.34m, 326.67m, 0, 0);
            _AssertSpecifiedInstallment(myContract.GetInstallment(5), 6, new DateTime(2006, 7, 3), 14.7m, 163.33m, 163.33m, 0, 0);
        }

        public void TestRepayFlateLoanWhenKeepNotExpectedInstallment()
        {
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.CALCULATIONLATEFEESDURINGPUBLICHOLIDAYS, true);

            LoanProduct package = new LoanProduct{
                                                     InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
                                                     LoanType = OLoanTypes.Flat,
                                                     ChargeInterestWithinGracePeriod = true,
                                                     KeepExpectedInstallment = false,
                                                     Currency = new Currency { Id = 1 }
                                                 };
            Loan myContract = new Loan(package, 1000, 0.03m, 6, 1, new DateTime(2006, 1, 1), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));

            myContract.Repay(1, new DateTime(2006, 2, 1), 30, true, false);
            myContract.Repay(2, new DateTime(2006, 3, 1), 230, true, false);

            Assert.AreEqual(30m, myContract.GetInstallment(0).InterestsRepayment.Value);
            Assert.AreEqual(0m, myContract.GetInstallment(0).CapitalRepayment.Value);
            Assert.AreEqual(1000m, myContract.GetInstallment(0).OLB.Value);
            Assert.AreEqual(0m, myContract.GetInstallment(0).PaidCapital.Value);
            Assert.AreEqual(30m, myContract.GetInstallment(0).PaidInterests.Value);

            Assert.AreEqual(30m, myContract.GetInstallment(1).InterestsRepayment.Value);
            Assert.AreEqual(200m, myContract.GetInstallment(1).CapitalRepayment.Value);
            Assert.AreEqual(1000m, myContract.GetInstallment(1).OLB.Value);
            Assert.AreEqual(200m, myContract.GetInstallment(1).PaidCapital.Value);
            Assert.AreEqual(30m, myContract.GetInstallment(1).PaidInterests.Value);

            Assert.AreEqual(30m, myContract.GetInstallment(2).InterestsRepayment.Value);
            Assert.AreEqual(200m, myContract.GetInstallment(2).CapitalRepayment.Value);
            Assert.AreEqual(800m, myContract.GetInstallment(2).OLB.Value);
            Assert.AreEqual(0m, myContract.GetInstallment(2).PaidCapital.Value);
            Assert.AreEqual(0m, myContract.GetInstallment(2).PaidInterests.Value);
        }

        [Test]
        public void TestRepayDecliningLoanWhenKeepExpectedInstallmentButSecondInstallmentNotEntirlyPaidTheSecondInstallment()
        {
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.CALCULATIONLATEFEESDURINGPUBLICHOLIDAYS, true);

            LoanProduct package = new LoanProduct
            {
                InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
                LoanType = OLoanTypes.DecliningFixedInstallments,
                ChargeInterestWithinGracePeriod = true,
                KeepExpectedInstallment = true,
                Currency = new Currency { Id = 1, UseCents = true}
            };
            Loan myContract = new Loan(package, 1000, 0.03m, 7, 1, new DateTime(2006, 1, 1), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));

            myContract.Repay(1, new DateTime(2006, 2, 1), 30, true, false);
            myContract.Repay(2, new DateTime(2006, 3, 1), 100, true, false);

            _AssertSpecifiedInstallment(myContract.GetInstallment(0), 1, new DateTime(2006, 2, 1), 30, 0, 1000, 0, 30);
            _AssertSpecifiedInstallment(myContract.GetInstallment(1), 2, new DateTime(2006, 3, 1), 30, 154.60m, 1000, 70m, 30);
            _AssertSpecifiedInstallment(myContract.GetInstallment(2), 3, new DateTime(2006, 4, 3), 25.36m, 159.24m, 845.40m, 0, 0);
            _AssertSpecifiedInstallment(myContract.GetInstallment(3), 4, new DateTime(2006, 5, 2), 20.58m, 164.02m, 686.16m, 0, 0);
            _AssertSpecifiedInstallment(myContract.GetInstallment(4), 5, new DateTime(2006, 6, 1), 15.66m, 168.93m, 522.14m, 0, 0);
            _AssertSpecifiedInstallment(myContract.GetInstallment(5), 6, new DateTime(2006, 7, 3), 10.60m, 173.99m, 353.21m, 0, 0);
            _AssertSpecifiedInstallment(myContract.GetInstallment(6), 7, new DateTime(2006, 8, 1), 5.38m, 179.22m, 179.22m, 0, 0);
        }

        [Test]
        public void TestRepayFlateLoanWhenKeepExpectedInstallmentEntirlyPaidLoanAndNoCancelFees()
        {
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.CALCULATIONLATEFEESDURINGPUBLICHOLIDAYS, true);

            LoanProduct package = new LoanProduct
            {
                InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
                LoanType = OLoanTypes.Flat,
                ChargeInterestWithinGracePeriod = true,
                KeepExpectedInstallment = true,
                Currency = new Currency { Id = 1 }
            };
            Loan myContract = new Loan(package, 1000, 0.03m, 6, 1, new DateTime(2006, 1, 1), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()))
                                  {
                                      AnticipatedTotalRepaymentPenalties = 0.003,
                                      NonRepaymentPenalties = { InitialAmount = 0.003 }
                                  };
            myContract.Repay(1, new DateTime(2006, 2, 1), 30, true, false);
            RepaymentEvent repayEvent = myContract.Repay(2, new DateTime(2006, 3, 1), 1150, false, true);

            Assert.AreEqual(0m, repayEvent.Fees.Value);
            Assert.AreEqual(150m, repayEvent.Interests.Value);
            Assert.AreEqual(1000m, repayEvent.Principal.Value);

            Assert.AreEqual(30m, myContract.GetInstallment(0).InterestsRepayment.Value);
            Assert.AreEqual(0m, myContract.GetInstallment(0).CapitalRepayment.Value);
            Assert.AreEqual(1000m, myContract.GetInstallment(0).OLB.Value);
            Assert.AreEqual(0m, myContract.GetInstallment(0).PaidCapital.Value);
            Assert.AreEqual(30m, myContract.GetInstallment(0).PaidInterests.Value);

            Assert.AreEqual(30m, myContract.GetInstallment(1).InterestsRepayment.Value);
            Assert.AreEqual(200m, myContract.GetInstallment(1).CapitalRepayment.Value);
            Assert.AreEqual(1000m, myContract.GetInstallment(1).OLB.Value);
            Assert.AreEqual(200m, myContract.GetInstallment(1).PaidCapital.Value);
            Assert.AreEqual(30m, myContract.GetInstallment(1).PaidInterests.Value);

            Assert.AreEqual(30m, myContract.GetInstallment(2).InterestsRepayment.Value);
            Assert.AreEqual(200m, myContract.GetInstallment(2).CapitalRepayment.Value);
            Assert.AreEqual(800m, myContract.GetInstallment(2).OLB.Value);
            Assert.AreEqual(200m, myContract.GetInstallment(2).PaidCapital.Value);
            Assert.AreEqual(30m, myContract.GetInstallment(2).PaidInterests.Value);

            Assert.AreEqual(30m, myContract.GetInstallment(3).InterestsRepayment.Value);
            Assert.AreEqual(200m, myContract.GetInstallment(3).CapitalRepayment.Value);
            Assert.AreEqual(600m, myContract.GetInstallment(3).OLB.Value);
            Assert.AreEqual(200m, myContract.GetInstallment(3).PaidCapital.Value);
            Assert.AreEqual(30m, myContract.GetInstallment(3).PaidInterests.Value);

            Assert.AreEqual(30m, myContract.GetInstallment(4).InterestsRepayment.Value);
            Assert.AreEqual(200m, myContract.GetInstallment(4).CapitalRepayment.Value);
            Assert.AreEqual(400m, myContract.GetInstallment(4).OLB.Value);
            Assert.AreEqual(200m, myContract.GetInstallment(4).PaidCapital.Value);
            Assert.AreEqual(30m, myContract.GetInstallment(4).PaidInterests.Value);

            Assert.AreEqual(30m, myContract.GetInstallment(5).InterestsRepayment.Value);
            Assert.AreEqual(200m, myContract.GetInstallment(5).CapitalRepayment.Value);
            Assert.AreEqual(200m, myContract.GetInstallment(5).OLB.Value);
            Assert.AreEqual(200m, myContract.GetInstallment(5).PaidCapital.Value);
            Assert.AreEqual(30m, myContract.GetInstallment(5).PaidInterests.Value);
        }

        [Test]
        public void TestRepayFlateLoanWhenKeepExpectedInstallmentButEntirlyPaidButCancelFees()
        {
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.CALCULATIONLATEFEESDURINGPUBLICHOLIDAYS, true);

            LoanProduct package = new LoanProduct
                                      {
                                          InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
                                          LoanType = OLoanTypes.Flat,
                                          ChargeInterestWithinGracePeriod = true,
                                          Currency = new Currency { Id = 1 }
                                      };
            package.KeepExpectedInstallment = true;
            Loan myContract = new Loan(package, 1000, 0.03m, 6, 1, new DateTime(2006, 1, 1), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));

            myContract.Repay(1, new DateTime(2006, 2, 1), 30, true, true);
            myContract.Repay(2, new DateTime(2006, 3, 1), 1150, true, true);

            Assert.AreEqual(30m, myContract.GetInstallment(0).InterestsRepayment.Value);
            Assert.AreEqual(0m, myContract.GetInstallment(0).CapitalRepayment.Value);
            Assert.AreEqual(1000m, myContract.GetInstallment(0).OLB.Value);
            Assert.AreEqual(0m, myContract.GetInstallment(0).PaidCapital.Value);
            Assert.AreEqual(30m, myContract.GetInstallment(0).PaidInterests.Value);

            Assert.AreEqual(30m, myContract.GetInstallment(1).InterestsRepayment.Value);
            Assert.AreEqual(200m, myContract.GetInstallment(1).CapitalRepayment.Value);
            Assert.AreEqual(1000m, myContract.GetInstallment(1).OLB.Value);
            Assert.AreEqual(200m, myContract.GetInstallment(1).PaidCapital.Value);
            Assert.AreEqual(30m, myContract.GetInstallment(1).PaidInterests.Value);

            Assert.AreEqual(30m, myContract.GetInstallment(2).InterestsRepayment.Value);
            Assert.AreEqual(200m, myContract.GetInstallment(2).CapitalRepayment.Value);
            Assert.AreEqual(800m, myContract.GetInstallment(2).OLB.Value);
            Assert.AreEqual(200m, myContract.GetInstallment(2).PaidCapital.Value);
            Assert.AreEqual(30m, myContract.GetInstallment(2).PaidInterests.Value);

            Assert.AreEqual(30m, myContract.GetInstallment(3).InterestsRepayment.Value);
            Assert.AreEqual(200m, myContract.GetInstallment(3).CapitalRepayment.Value);
            Assert.AreEqual(600m, myContract.GetInstallment(3).OLB.Value);
            Assert.AreEqual(200m, myContract.GetInstallment(3).PaidCapital.Value);
            Assert.AreEqual(30m, myContract.GetInstallment(3).PaidInterests.Value);

            Assert.AreEqual(30m, myContract.GetInstallment(4).InterestsRepayment.Value);
            Assert.AreEqual(200m, myContract.GetInstallment(4).CapitalRepayment.Value);
            Assert.AreEqual(400m, myContract.GetInstallment(4).OLB.Value);
            Assert.AreEqual(200m, myContract.GetInstallment(4).PaidCapital.Value);
            Assert.AreEqual(30m, myContract.GetInstallment(4).PaidInterests.Value);

            Assert.AreEqual(30m, myContract.GetInstallment(5).InterestsRepayment.Value);
            Assert.AreEqual(200m, myContract.GetInstallment(5).CapitalRepayment.Value);
            Assert.AreEqual(200m, myContract.GetInstallment(5).OLB.Value);
            Assert.AreEqual(200m, myContract.GetInstallment(5).PaidCapital.Value);
            Assert.AreEqual(30m, myContract.GetInstallment(5).PaidInterests.Value);
        }

        [Test]
        public void TestRepayDecliningLoanWhenKeepExpectedInstallmentButEntirlyPaidButNoCancelFees()
        {
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.CALCULATIONLATEFEESDURINGPUBLICHOLIDAYS, true);

            LoanProduct package = new LoanProduct
            {
                InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
                LoanType = OLoanTypes.DecliningFixedInstallments,
                ChargeInterestWithinGracePeriod = true,
                KeepExpectedInstallment = true,
                Currency = new Currency { Id = 1, UseCents = true}
            };
            Loan myContract = new Loan(package, 1000, 0.03m, 7, 1, new DateTime(2006, 1, 1), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()))
                                  {
                                      AnticipatedTotalRepaymentPenalties = 0.003,
                                      NonRepaymentPenalties = { InitialAmount = 0.003 }
                                  };
            myContract.Repay(1, new DateTime(2006, 2, 1), 30, true, false);

            CreditContractOptions cCO = new CreditContractOptions(package.LoanType, package.KeepExpectedInstallment, false, 0, 0, false, 0, package.AnticipatedTotalRepaymentPenaltiesBase);
            CreditContractRepayment cCR = new CreditContractRepayment(myContract, cCO, new DateTime(2006, 3, 1), 1, new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""));

            Assert.AreEqual(1107.58m, cCR.MaximumAmountAuthorizeToRepay.Value);

            RepaymentEvent repayEvent = myContract.Repay(2, new DateTime(2006, 3, 1), 1107.58m, false, true);

            Assert.AreEqual(0m, repayEvent.Fees.Value);
            Assert.AreEqual(107.58m, repayEvent.Interests.Value);
            Assert.AreEqual(1000m, repayEvent.Principal.Value);

            Assert.AreEqual(30m, myContract.GetInstallment(0).InterestsRepayment.Value);
            Assert.AreEqual(0m, myContract.GetInstallment(0).CapitalRepayment.Value);
            Assert.AreEqual(1000m, myContract.GetInstallment(0).OLB.Value);
            Assert.AreEqual(0m, myContract.GetInstallment(0).PaidCapital.Value);
            Assert.AreEqual(30m, myContract.GetInstallment(0).PaidInterests.Value);

            Assert.AreEqual(30m, myContract.GetInstallment(1).InterestsRepayment.Value);
            Assert.AreEqual(154.60m, myContract.GetInstallment(1).CapitalRepayment.Value);
            Assert.AreEqual(1000m, myContract.GetInstallment(1).OLB.Value);
            Assert.AreEqual(154.60m, myContract.GetInstallment(1).PaidCapital.Value);
            Assert.AreEqual(30m, myContract.GetInstallment(1).PaidInterests.Value);

            Assert.AreEqual(25.36m, myContract.GetInstallment(2).InterestsRepayment.Value);
            Assert.AreEqual(159.24m, myContract.GetInstallment(2).CapitalRepayment.Value);
            Assert.AreEqual(845.40m, myContract.GetInstallment(2).OLB.Value);
            Assert.AreEqual(159.24m, myContract.GetInstallment(2).PaidCapital.Value);
            Assert.AreEqual(25.36m, myContract.GetInstallment(2).PaidInterests.Value);

            Assert.AreEqual(20.58m, myContract.GetInstallment(3).InterestsRepayment.Value);
            Assert.AreEqual(164.02m, myContract.GetInstallment(3).CapitalRepayment.Value);
            Assert.AreEqual(686.16m, myContract.GetInstallment(3).OLB.Value);
            Assert.AreEqual(164.02m, myContract.GetInstallment(3).PaidCapital.Value);
            Assert.AreEqual(20.58m, myContract.GetInstallment(3).PaidInterests.Value);

            Assert.AreEqual(15.66m, myContract.GetInstallment(4).InterestsRepayment.Value);
            Assert.AreEqual(168.93m, myContract.GetInstallment(4).CapitalRepayment.Value);
            Assert.AreEqual(522.14m, myContract.GetInstallment(4).OLB.Value);
            Assert.AreEqual(168.93m, myContract.GetInstallment(4).PaidCapital.Value);
            Assert.AreEqual(15.66m, myContract.GetInstallment(4).PaidInterests.Value);

            Assert.AreEqual(10.60m, myContract.GetInstallment(5).InterestsRepayment.Value);
            Assert.AreEqual(173.99m, myContract.GetInstallment(5).CapitalRepayment.Value);
            Assert.AreEqual(353.21m, myContract.GetInstallment(5).OLB.Value);
            Assert.AreEqual(173.99m, myContract.GetInstallment(5).PaidCapital.Value);
            Assert.AreEqual(10.60m, myContract.GetInstallment(5).PaidInterests.Value);

            Assert.AreEqual(5.38m, myContract.GetInstallment(6).InterestsRepayment.Value);
            Assert.AreEqual(179.22m, myContract.GetInstallment(6).CapitalRepayment.Value);
            Assert.AreEqual(179.22m, myContract.GetInstallment(6).OLB.Value);
            Assert.AreEqual(179.22m, myContract.GetInstallment(6).PaidCapital.Value);
            Assert.AreEqual(5.38m, myContract.GetInstallment(6).PaidInterests.Value);
        }

        [Test]
        public void TestRepayDecliningLoanWhenKeepNotExpectedInstallmentButEntirlyPaidButCancelFees()
        {
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.CALCULATIONLATEFEESDURINGPUBLICHOLIDAYS, true);
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.ACCOUNTINGPROCESS, OAccountingProcesses.Accrual);

            LoanProduct package = new LoanProduct
                                      {
                                          InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
                                          LoanType = OLoanTypes.DecliningFixedInstallments,
                                          ChargeInterestWithinGracePeriod = true,
                                          Currency = new Currency { Id = 1, UseCents = true}
                                      };
            package.KeepExpectedInstallment = false;
            Loan myContract = new Loan(package, 1000, 0.03m, 7, 1, new DateTime(2006, 1, 1), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));

            myContract.Repay(1, new DateTime(2006, 2, 1), 30, true, false);
            myContract.Repay(2, new DateTime(2006, 3, 1), 1030, true, false);

            Assert.AreEqual(30m, myContract.GetInstallment(0).InterestsRepayment.Value);
            Assert.AreEqual(0m, myContract.GetInstallment(0).CapitalRepayment.Value);
            Assert.AreEqual(1000m, myContract.GetInstallment(0).OLB.Value);
            Assert.AreEqual(0m, myContract.GetInstallment(0).PaidCapital.Value);
            Assert.AreEqual(30m, myContract.GetInstallment(0).PaidInterests.Value);

            Assert.AreEqual(30m, myContract.GetInstallment(1).InterestsRepayment.Value);
            Assert.AreEqual(154.60m, myContract.GetInstallment(1).CapitalRepayment.Value);
            Assert.AreEqual(1000m, myContract.GetInstallment(1).OLB.Value);
            Assert.AreEqual(154.60m, myContract.GetInstallment(1).PaidCapital.Value);
            Assert.AreEqual(30m, myContract.GetInstallment(1).PaidInterests.Value);

            Assert.AreEqual(0, myContract.GetInstallment(2).InterestsRepayment.Value);
            Assert.AreEqual(845.4m, myContract.GetInstallment(2).CapitalRepayment.Value);
            Assert.AreEqual(845.4m, myContract.GetInstallment(2).OLB.Value);
            Assert.AreEqual(845.4m, myContract.GetInstallment(2).PaidCapital.Value);
            Assert.AreEqual(0m, myContract.GetInstallment(2).PaidInterests.Value);

            Assert.AreEqual(0, myContract.GetInstallment(3).InterestsRepayment.Value);
            Assert.AreEqual(0, myContract.GetInstallment(3).CapitalRepayment.Value);
            Assert.AreEqual(0m, myContract.GetInstallment(3).OLB.Value);
            Assert.AreEqual(0m, myContract.GetInstallment(3).PaidCapital.Value);
            Assert.AreEqual(0m, myContract.GetInstallment(3).PaidInterests.Value);

            Assert.AreEqual(0, myContract.GetInstallment(4).InterestsRepayment.Value);
            Assert.AreEqual(0, myContract.GetInstallment(4).CapitalRepayment.Value);
            Assert.AreEqual(0m, myContract.GetInstallment(4).OLB.Value);
            Assert.AreEqual(0m, myContract.GetInstallment(4).PaidCapital.Value);
            Assert.AreEqual(0m, myContract.GetInstallment(4).PaidInterests.Value);

            Assert.AreEqual(0m, myContract.GetInstallment(5).InterestsRepayment.Value);
            Assert.AreEqual(0, myContract.GetInstallment(5).CapitalRepayment.Value);
            Assert.AreEqual(0m, myContract.GetInstallment(5).OLB.Value);
            Assert.AreEqual(0m, myContract.GetInstallment(5).PaidCapital.Value);
            Assert.AreEqual(0m, myContract.GetInstallment(5).PaidInterests.Value);

            Assert.AreEqual(0m, myContract.GetInstallment(6).InterestsRepayment.Value);
            Assert.AreEqual(0, myContract.GetInstallment(6).CapitalRepayment.Value);
            Assert.AreEqual(0m, myContract.GetInstallment(6).OLB.Value);
            Assert.AreEqual(0m, myContract.GetInstallment(6).PaidCapital.Value);
            Assert.AreEqual(0m, myContract.GetInstallment(6).PaidInterests.Value);
        }

        [Test]
        public void TestRepayDecliningLoanWhenKeepExpectedInstallmentButEntirlyPaidButCancelFees()
        {
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.CALCULATIONLATEFEESDURINGPUBLICHOLIDAYS, true);

            LoanProduct package = new LoanProduct
            {
                InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
                LoanType = OLoanTypes.DecliningFixedInstallments,
                ChargeInterestWithinGracePeriod = true,
                KeepExpectedInstallment = true,
                Currency = new Currency { Id = 1, UseCents = true}
            };
            Loan myContract = new Loan(package, 1000, 0.03m, 7, 1, new DateTime(2006, 1, 1), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));

            myContract.Repay(1, new DateTime(2006, 2, 1), 30, true, true);
            myContract.Repay(2, new DateTime(2006, 3, 1), 1107.58m, true, true);

            Assert.AreEqual(30m, myContract.GetInstallment(0).InterestsRepayment.Value);
            Assert.AreEqual(0m, myContract.GetInstallment(0).CapitalRepayment.Value);
            Assert.AreEqual(1000m, myContract.GetInstallment(0).OLB.Value);
            Assert.AreEqual(0m, myContract.GetInstallment(0).PaidCapital.Value);
            Assert.AreEqual(30m, myContract.GetInstallment(0).PaidInterests.Value);

            Assert.AreEqual(30m, myContract.GetInstallment(1).InterestsRepayment.Value);
            Assert.AreEqual(154.60m, myContract.GetInstallment(1).CapitalRepayment.Value);
            Assert.AreEqual(1000m, myContract.GetInstallment(1).OLB.Value);
            Assert.AreEqual(154.60m, myContract.GetInstallment(1).PaidCapital.Value);
            Assert.AreEqual(30m, myContract.GetInstallment(1).PaidInterests.Value);

            Assert.AreEqual(25.36m, myContract.GetInstallment(2).InterestsRepayment.Value);
            Assert.AreEqual(159.24m, myContract.GetInstallment(2).CapitalRepayment.Value);
            Assert.AreEqual(845.40m, myContract.GetInstallment(2).OLB.Value);
            Assert.AreEqual(159.24m, myContract.GetInstallment(2).PaidCapital.Value);
            Assert.AreEqual(25.36m, myContract.GetInstallment(2).PaidInterests.Value);

            Assert.AreEqual(20.58m, myContract.GetInstallment(3).InterestsRepayment.Value);
            Assert.AreEqual(164.02m, myContract.GetInstallment(3).CapitalRepayment.Value);
            Assert.AreEqual(686.16m, myContract.GetInstallment(3).OLB.Value);
            Assert.AreEqual(164.02m, myContract.GetInstallment(3).PaidCapital.Value);
            Assert.AreEqual(20.58m, myContract.GetInstallment(3).PaidInterests.Value);

            Assert.AreEqual(15.66m, myContract.GetInstallment(4).InterestsRepayment.Value);
            Assert.AreEqual(168.93m, myContract.GetInstallment(4).CapitalRepayment.Value);
            Assert.AreEqual(522.14m, myContract.GetInstallment(4).OLB.Value);
            Assert.AreEqual(168.93m, myContract.GetInstallment(4).PaidCapital.Value);
            Assert.AreEqual(15.66m, myContract.GetInstallment(4).PaidInterests.Value);

            Assert.AreEqual(10.60m, myContract.GetInstallment(5).InterestsRepayment.Value);
            Assert.AreEqual(173.99m, myContract.GetInstallment(5).CapitalRepayment.Value);
            Assert.AreEqual(353.21m, myContract.GetInstallment(5).OLB.Value);
            Assert.AreEqual(173.99m, myContract.GetInstallment(5).PaidCapital.Value);
            Assert.AreEqual(10.60m, myContract.GetInstallment(5).PaidInterests.Value);

            Assert.AreEqual(5.38m, myContract.GetInstallment(6).InterestsRepayment.Value);
            Assert.AreEqual(179.22m, myContract.GetInstallment(6).CapitalRepayment.Value);
            Assert.AreEqual(179.22m, myContract.GetInstallment(6).OLB.Value);
            Assert.AreEqual(179.22m, myContract.GetInstallment(6).PaidCapital.Value);
            Assert.AreEqual(5.38m, myContract.GetInstallment(6).PaidInterests.Value);
        }

        [Test]
        public void TestRepayDecliningLoanWhenKeepNotExpectedInstallmentAndOverPaid()
        {
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.CALCULATIONLATEFEESDURINGPUBLICHOLIDAYS, true);
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.ACCOUNTINGPROCESS, OAccountingProcesses.Accrual);

            LoanProduct package = new LoanProduct
            {
                InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
                LoanType = OLoanTypes.DecliningFixedInstallments,
                ChargeInterestWithinGracePeriod = true,
                KeepExpectedInstallment = false,
                Currency = new Currency { Id = 1, UseCents = true}
            };
            Loan myContract = new Loan(package, 1000, 0.03m, 7, 1, new DateTime(2006, 1, 1), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));

            myContract.Repay(1, new DateTime(2006, 2, 1), 30, true, false);
            myContract.Repay(2, new DateTime(2006, 3, 1), 550, true, false);

            Assert.AreEqual(30m, myContract.GetInstallment(0).InterestsRepayment.Value);
            Assert.AreEqual(0m, myContract.GetInstallment(0).CapitalRepayment.Value);
            Assert.AreEqual(1000m, myContract.GetInstallment(0).OLB.Value);
            Assert.AreEqual(0m, myContract.GetInstallment(0).PaidCapital.Value);
            Assert.AreEqual(30m, myContract.GetInstallment(0).PaidInterests.Value);

            Assert.AreEqual(30m, myContract.GetInstallment(1).InterestsRepayment.Value);
            Assert.AreEqual(154.60m, myContract.GetInstallment(1).CapitalRepayment.Value);
            Assert.AreEqual(1000m, myContract.GetInstallment(1).OLB.Value);
            Assert.AreEqual(154.60m, myContract.GetInstallment(1).PaidCapital.Value);
            Assert.AreEqual(30m, myContract.GetInstallment(1).PaidInterests.Value);

            Assert.AreEqual(14.40m, myContract.GetInstallment(2).InterestsRepayment.Value);
            Assert.AreEqual(90.41m, myContract.GetInstallment(2).CapitalRepayment.Value);
            Assert.AreEqual(480m, myContract.GetInstallment(2).OLB.Value);
            Assert.AreEqual(0, myContract.GetInstallment(2).PaidCapital.Value);
            Assert.AreEqual(0, myContract.GetInstallment(2).PaidInterests.Value);

            Assert.AreEqual(11.69m, myContract.GetInstallment(3).InterestsRepayment.Value);
            Assert.AreEqual(93.12m, myContract.GetInstallment(3).CapitalRepayment.Value);
            Assert.AreEqual(389.59m, myContract.GetInstallment(3).OLB.Value);
            Assert.AreEqual(0, myContract.GetInstallment(3).PaidCapital.Value);
            Assert.AreEqual(0, myContract.GetInstallment(3).PaidInterests.Value);

            Assert.AreEqual(8.89m, myContract.GetInstallment(4).InterestsRepayment.Value);
            Assert.AreEqual(95.92m, myContract.GetInstallment(4).CapitalRepayment.Value);
            Assert.AreEqual(296.47m, myContract.GetInstallment(4).OLB.Value);
            Assert.AreEqual(0, myContract.GetInstallment(4).PaidCapital.Value);
            Assert.AreEqual(0, myContract.GetInstallment(4).PaidInterests.Value);

            Assert.AreEqual(6.02m, myContract.GetInstallment(5).InterestsRepayment.Value);
            Assert.AreEqual(98.79m, myContract.GetInstallment(5).CapitalRepayment.Value);
            Assert.AreEqual(200.55m, myContract.GetInstallment(5).OLB.Value);
            Assert.AreEqual(0, myContract.GetInstallment(5).PaidCapital.Value);
            Assert.AreEqual(0, myContract.GetInstallment(5).PaidInterests.Value);

            Assert.AreEqual(3.05m, myContract.GetInstallment(6).InterestsRepayment.Value);
            Assert.AreEqual(101.76m, myContract.GetInstallment(6).CapitalRepayment.Value);
            Assert.AreEqual(101.76m, myContract.GetInstallment(6).OLB.Value);
            Assert.AreEqual(0, myContract.GetInstallment(6).PaidCapital.Value);
            Assert.AreEqual(0, myContract.GetInstallment(6).PaidInterests.Value);
        }

        [Test]
        public void TestRepayDecliningLoanWhenKeepExpectedInstallmentAndOverPaid()
        {
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.CALCULATIONLATEFEESDURINGPUBLICHOLIDAYS, true);

            LoanProduct package = new LoanProduct
            {
                InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
                LoanType = OLoanTypes.DecliningFixedInstallments,
                ChargeInterestWithinGracePeriod = true,
                KeepExpectedInstallment = true,
                Currency = new Currency { Id = 1, UseCents = true}
            };
            Loan myContract = new Loan(package, 1000, 0.03m, 7, 1, new DateTime(2006, 1, 1), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));

            myContract.Repay(1, new DateTime(2006, 2, 1), 30, true, true);
            myContract.Repay(2, new DateTime(2006, 3, 1), 550, true, true);

            Assert.AreEqual(30m, myContract.GetInstallment(0).InterestsRepayment.Value);
            Assert.AreEqual(0m, myContract.GetInstallment(0).CapitalRepayment.Value);
            Assert.AreEqual(1000m, myContract.GetInstallment(0).OLB.Value);
            Assert.AreEqual(0m, myContract.GetInstallment(0).PaidCapital.Value);
            Assert.AreEqual(30m, myContract.GetInstallment(0).PaidInterests.Value);

            Assert.AreEqual(30m, myContract.GetInstallment(1).InterestsRepayment.Value);
            Assert.AreEqual(154.60m, myContract.GetInstallment(1).CapitalRepayment.Value);
            Assert.AreEqual(1000m, myContract.GetInstallment(1).OLB.Value);
            Assert.AreEqual(154.60m, myContract.GetInstallment(1).PaidCapital.Value);
            Assert.AreEqual(30m, myContract.GetInstallment(1).PaidInterests.Value);

            Assert.AreEqual(25.36m, myContract.GetInstallment(2).InterestsRepayment.Value);
            Assert.AreEqual(159.24m, myContract.GetInstallment(2).CapitalRepayment.Value);
            Assert.AreEqual(845.40m, myContract.GetInstallment(2).OLB.Value);
            Assert.AreEqual(159.24m, myContract.GetInstallment(2).PaidCapital.Value);
            Assert.AreEqual(25.36m, myContract.GetInstallment(2).PaidInterests.Value);

            Assert.AreEqual(20.58m, myContract.GetInstallment(3).InterestsRepayment.Value);
            Assert.AreEqual(164.02m, myContract.GetInstallment(3).CapitalRepayment.Value);
            Assert.AreEqual(686.16m, myContract.GetInstallment(3).OLB.Value);
            Assert.AreEqual(160.22m, myContract.GetInstallment(3).PaidCapital.Value);
            Assert.AreEqual(20.58m, myContract.GetInstallment(3).PaidInterests.Value);

            Assert.AreEqual(15.66m, myContract.GetInstallment(4).InterestsRepayment.Value);
            Assert.AreEqual(168.93m, myContract.GetInstallment(4).CapitalRepayment.Value);
            Assert.AreEqual(522.14m, myContract.GetInstallment(4).OLB.Value);
            Assert.AreEqual(0, myContract.GetInstallment(4).PaidCapital.Value);
            Assert.AreEqual(0, myContract.GetInstallment(4).PaidInterests.Value);

            Assert.AreEqual(10.60m, myContract.GetInstallment(5).InterestsRepayment.Value);
            Assert.AreEqual(173.99m, myContract.GetInstallment(5).CapitalRepayment.Value);
            Assert.AreEqual(353.21m, myContract.GetInstallment(5).OLB.Value);
            Assert.AreEqual(0, myContract.GetInstallment(5).PaidCapital.Value);
            Assert.AreEqual(0, myContract.GetInstallment(5).PaidInterests.Value);

            Assert.AreEqual(5.38m, myContract.GetInstallment(6).InterestsRepayment.Value);
            Assert.AreEqual(179.22m, myContract.GetInstallment(6).CapitalRepayment.Value);
            Assert.AreEqual(179.22m, myContract.GetInstallment(6).OLB.Value);
            Assert.AreEqual(0, myContract.GetInstallment(6).PaidCapital.Value);
            Assert.AreEqual(0, myContract.GetInstallment(6).PaidInterests.Value);
        }

        [Test]
        public void TestRepayDecliningExoticLoanWithOverPaidOnTheThirdInstallment()
        {
            Assert.Ignore();
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.CALCULATIONLATEFEESDURINGPUBLICHOLIDAYS, true);

            LoanProduct package = new LoanProduct
                                      {
                                          InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
                                          LoanType = OLoanTypes.DecliningFixedInstallments,
                                          ChargeInterestWithinGracePeriod = true,
                                          Currency = new Currency { Id = 1 }
                                      };

            ExoticInstallmentsTable exoticProduct = new ExoticInstallmentsTable();
            exoticProduct.Add(new ExoticInstallment(1, 0.1, null));
            exoticProduct.Add(new ExoticInstallment(2, 0.1, null));
            exoticProduct.Add(new ExoticInstallment(3, 0.1, null));
            exoticProduct.Add(new ExoticInstallment(4, 0.1, null));
            exoticProduct.Add(new ExoticInstallment(5, 0.1, null));
            exoticProduct.Add(new ExoticInstallment(6, 0.5, null));
            package.ExoticProduct = exoticProduct;

            Loan myContract = new Loan(package, 1000, 0.03m, 6, 0, new DateTime(2006, 1, 1), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));

            //RepayFirstInstallmentPartially
            myContract.Repay(1, new DateTime(2006, 2, 1), 100, true, false);
            _AssertSpecifiedInstallment(myContract.GetInstallment(0), 1, new DateTime(2006, 2, 1), 30, 100, 1000, 70, 30);

            myContract.Repay(1, new DateTime(2006, 2, 1), 30, true, false);
            _AssertSpecifiedInstallment(myContract.GetInstallment(0), 1, new DateTime(2006, 2, 1), 30, 100, 1000, 100, 30);

            myContract.Repay(2, new DateTime(2006, 3, 1), 127, true, false);
            _AssertSpecifiedInstallment(myContract.GetInstallment(1), 2, new DateTime(2006, 3, 1), 27, 100, 900, 100, 27);

            myContract.Repay(3, new DateTime(2006, 4, 3), 174, true, false);
            _AssertSpecifiedInstallment(myContract.GetInstallment(2), 3, new DateTime(2006, 4, 3), 24, 100, 800, 100, 24);
            _AssertSpecifiedInstallment(myContract.GetInstallment(3), 4, new DateTime(2006, 5, 2), 19.5m, 92.86m, 650, 0, 0);
            _AssertSpecifiedInstallment(myContract.GetInstallment(4), 5, new DateTime(2006, 6, 1), 16.71m, 92.86m, 557.14m, 0, 0);
            _AssertSpecifiedInstallment(myContract.GetInstallment(5), 6, new DateTime(2006, 7, 3), 13.93m, 464.29m, 464.28m, 0, 0);
        }

        [Test]
        public void TestRepayFlat_GracePeriod_KeepNotExpectedInstallments()
        {
            dataParam.UpdateParameter(OGeneralSettings.CALCULATIONLATEFEESDURINGPUBLICHOLIDAYS, true);
            LoanProduct package = new LoanProduct
                                      {
                                          InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
                                          LoanType = OLoanTypes.Flat,
                                          ChargeInterestWithinGracePeriod = true,
                                          Currency = new Currency { Id = 1 }
                                      };
            package.KeepExpectedInstallment = false;
            package.AnticipatedTotalRepaymentPenaltiesBase = OAnticipatedRepaymentPenaltiesBases.RemainingOLB;
            Loan myContract = new Loan(package, 50000, 0.01m, 8, 2, new DateTime(2006, 1, 1), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));
            myContract.BadLoan = false;


            myContract.Repay(1, new DateTime(2006, 2, 1), 5000, false, false);
            myContract.Repay(2, new DateTime(2006, 6, 1), 47320, false, false);

            _AssertSpecifiedInstallment(myContract.GetInstallment(0), 1, new DateTime(2006, 2, 1), 500,    0, 50000,    0, 500);
            _AssertSpecifiedInstallment(myContract.GetInstallment(1), 2, new DateTime(2006, 3, 1), 455, 0, 45500, 0, 455);
            _AssertSpecifiedInstallment(myContract.GetInstallment(2), 3, new DateTime(2006, 4, 3), 455, 6500, 45500, 6500, 455);
            //_AssertSpecifiedInstallment(myContract.GetInstallment(3), 4, new DateTime(2006, 5, 2), 455, 6500, 0, 6500, 455);
            //_AssertSpecifiedInstallment(myContract.GetInstallment(4), 5, new DateTime(2006, 6, 1), 455, 6500, 0, 6500, 455);
            //_AssertSpecifiedInstallment(myContract.GetInstallment(5), 6, new DateTime(2006, 7, 3), 114, 3808, 0, 0, 0);
            //_AssertSpecifiedInstallment(myContract.GetInstallment(6), 7, new DateTime(2006, 8, 1), 114, 3808, 0, 0, 0);
            //_AssertSpecifiedInstallment(myContract.GetInstallment(7), 8, new DateTime(2006, 9, 1), 115, 3810, 0, 0, 0);

        }

        [Test]
        public void TestRepayContractAndCalculateOLB()
        {
            dataParam.UpdateParameter(OGeneralSettings.CALCULATIONLATEFEESDURINGPUBLICHOLIDAYS, true);
            LoanProduct package = new LoanProduct
                                      {
                                          InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
                                          LoanType = OLoanTypes.DecliningFixedInstallments,
                                          ChargeInterestWithinGracePeriod = true,
                                          Currency = new Currency { Id = 1 }
                                      };
            package.KeepExpectedInstallment = false;
            package.AnticipatedTotalRepaymentPenaltiesBase = OAnticipatedRepaymentPenaltiesBases.RemainingOLB;
            Loan myContract = new Loan(package, 1000, 0.03m, 7, 1, new DateTime(2006, 1, 1), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));
            myContract.BadLoan = false;
            myContract.NonRepaymentPenalties.OLB = 0.003;
            myContract.AnticipatedTotalRepaymentPenalties = 0.01;

            myContract.Repay(1, new DateTime(2006, 2, 1), 30, false, false);
            myContract.Repay(2, new DateTime(2006, 3, 1), 500, false, true);

            _AssertSpecifiedInstallment(myContract.GetInstallment(0), 1, new DateTime(2006, 2, 1), 30, 0, 1000, 0, 30);
            _AssertSpecifiedInstallment(myContract.GetInstallment(1), 2, new DateTime(2006, 3, 1), 30, 155, 1000, 155, 30);
            _AssertSpecifiedInstallment(myContract.GetInstallment(2), 3, new DateTime(2006, 4, 3), 25, 160, 845, 160, 25);
            _AssertSpecifiedInstallment(myContract.GetInstallment(3), 4, new DateTime(2006, 5, 2), 21, 163, 685, 109, 21);
            _AssertSpecifiedInstallment(myContract.GetInstallment(4), 5, new DateTime(2006, 6, 1), 16, 169, 522, 0, 0);
            _AssertSpecifiedInstallment(myContract.GetInstallment(5), 6, new DateTime(2006, 7, 3), 11, 173, 353, 0, 0);
            _AssertSpecifiedInstallment(myContract.GetInstallment(6), 7, new DateTime(2006, 8, 1), 5, 180, 180, 0, 0);

         }
    }
}
