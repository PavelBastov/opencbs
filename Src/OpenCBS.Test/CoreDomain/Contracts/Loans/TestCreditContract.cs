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
using NUnit.Framework;
using OpenCBS.CoreDomain;
using OpenCBS.CoreDomain.Clients;
using OpenCBS.CoreDomain.Contracts.Loans;
using OpenCBS.CoreDomain.Contracts.Loans.Installments;
using OpenCBS.CoreDomain.Contracts.Loans.LoanRepayment;
using OpenCBS.CoreDomain.Products;
using OpenCBS.Enums;
using OpenCBS.CoreDomain.Events;
using OpenCBS.Shared;
using OpenCBS.CoreDomain.Accounting;
using OpenCBS.Shared.Settings;

namespace OpenCBS.Test.CoreDomain.Contracts.Loans
{
    /// <summary>
    /// This is the test class for the class credit 
    /// </summary>
    [TestFixture]
    public class TestCredit
    {
        private readonly Loan _contract = new Loan(new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));
        private NonWorkingDateSingleton _nonWorkingDateHelper;
		
        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            ProvisionTable provisionTable = ProvisionTable.GetInstance(new User());
            provisionTable.Add(new ProvisioningRate { Number = 1, NbOfDaysMin = 0, NbOfDaysMax = 30, ProvisioningValue = 10, ProvisioningInterest = 10, ProvisioningPenalty = 10 });
            provisionTable.Add(new ProvisioningRate { Number = 2, NbOfDaysMin = 31, NbOfDaysMax = 60, ProvisioningValue = 25, ProvisioningInterest = 25, ProvisioningPenalty = 25 });
            provisionTable.Add(new ProvisioningRate { Number = 3, NbOfDaysMin = 61, NbOfDaysMax = 90, ProvisioningValue = 50, ProvisioningInterest = 50, ProvisioningPenalty = 50 });
            provisionTable.Add(new ProvisioningRate { Number = 4, NbOfDaysMin = 91, NbOfDaysMax = 180, ProvisioningValue = 75, ProvisioningInterest = 75, ProvisioningPenalty = 75 });
            provisionTable.Add(new ProvisioningRate { Number = 5, NbOfDaysMin = 181, NbOfDaysMax = 365, ProvisioningValue = 100, ProvisioningInterest = 100, ProvisioningPenalty = 100 });
            provisionTable.Add(new ProvisioningRate { Number = 6, NbOfDaysMin = 366, NbOfDaysMax = 99999, ProvisioningValue = 100, ProvisioningInterest = 100, ProvisioningPenalty = 100 });
            _nonWorkingDateHelper = NonWorkingDateSingleton.GetInstance("");
            _nonWorkingDateHelper.WeekEndDay1 = 6;
            _nonWorkingDateHelper.WeekEndDay2 = 0;
            _nonWorkingDateHelper.PublicHolidays = new Dictionary<DateTime, string>();
            _nonWorkingDateHelper.PublicHolidays.Add(new DateTime(2006,1,1),"New Year Eve");
            _nonWorkingDateHelper.PublicHolidays.Add(new DateTime(2006,12,25),"Christmas");
            ApplicationSettings dataParam = ApplicationSettings.GetInstance("");
            dataParam.DeleteAllParameters();
            dataParam.AddParameter(OGeneralSettings.ACCOUNTINGPROCESS, OAccountingProcesses.Cash);
            dataParam.AddParameter(OGeneralSettings.PAYFIRSTINSTALLMENTREALVALUE, true);
            dataParam.AddParameter(OGeneralSettings.CALCULATIONLATEFEESDURINGPUBLICHOLIDAYS, true);
            dataParam.AddParameter(OGeneralSettings.DONOTSKIPWEEKENDSININSTALLMENTSDATE, false);
            dataParam.AddParameter(OGeneralSettings.INCREMENTALDURINGDAYOFF, true);
            dataParam.AddParameter(OGeneralSettings.STOP_WRITEOFF_PENALTY, true);
            dataParam.AddParameter(OGeneralSettings.INTEREST_RATE_DECIMAL_PLACES, 2);
        }

        [SetUp]
        public void SetUp()
        {
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.ACCOUNTINGPROCESS, OAccountingProcesses.Cash);
        }

        [Test]
        public void GuarantorsEmptyByDefaultCorrectlySetAndRetrieved()
        {
            Loan newContract = new Loan(new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User())); new Loan(new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));
            Assert.AreEqual(0,newContract.Guarantors.Count);
        }

        [Test]
        public void GuarantorsCorrectlySetAndRetrieved()
        {
            List<Guarantor> guarantors = new List<Guarantor> {new Guarantor()};
            Loan newContract = new Loan(new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User())); new Loan(new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));
            newContract.Guarantors = guarantors;
            Assert.AreEqual(1,newContract.Guarantors.Count);
        }

        [Test]
        public void TestAddGuarantorAndGetGuarantor()
        {
            Loan newContract = new Loan(new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User())); new Loan(new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));
            newContract.AddGuarantor(new Guarantor{Tiers = new Person(), Amount = 1000.50m});
            Assert.AreEqual(1000.50m,newContract.GetGuarantor(0).Amount.Value);
        }

        [Test]
        public void TestAddInstallmentAndGetInstallment()
        {
            Loan newContract = new Loan(new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User())); new Loan(new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));
            Installment i = new Installment {PaidCapital = 100};
            newContract.AddInstallment(i);
            Assert.AreEqual(100m,newContract.GetInstallment(0).PaidCapital.Value);
        }

        [Test]
        public void TestIfLoanOfficerIsCorrectlySetAndGet()
        {
            User user = new User {UserName = "francesco"};
            _contract.LoanOfficer = user;
            Assert.IsTrue(user.Equals(_contract.LoanOfficer));
        }

        [Test]
        public void TestRescheduled()
        {
            Loan myContract = new Loan(new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User())); new Loan(new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));
            myContract.Rescheduled = true;
            Assert.IsTrue(myContract.Rescheduled);
        }

        [Test]
        public void TestBadLoan()
        {
            Loan myContract = new Loan(new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User())); new Loan(new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));
            myContract.BadLoan = true;
            Assert.IsTrue(myContract.BadLoan);
        }
		
        [Test]
        public void TestOLBCorrectlyCalculated()
        {
            LoanProduct package = new LoanProduct
                                      {
                                          InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
                                          LoanType = OLoanTypes.Flat,
                                          ChargeInterestWithinGracePeriod = true,
                                          Currency = new Currency {Id = 1},
                                          KeepExpectedInstallment = false
                                      };
            Loan myContract = new Loan(package,1000,0.03m,6,2,new DateTime(2006,1,1), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));

            Assert.AreEqual(1000m,myContract.CalculateActualOlb().Value);
            myContract.Repay(1,new DateTime(2006,2,1),30,true,false);
            Assert.AreEqual(1000m,myContract.CalculateActualOlb().Value);
            myContract.Repay(2,new DateTime(2006,3,1),30,true,false);
            Assert.AreEqual(1000m,myContract.CalculateActualOlb().Value);
            myContract.Repay(3,new DateTime(2006,4,3),280,true,false);
            Assert.AreEqual(750m,myContract.CalculateActualOlb().Value);
        }

        [Test]
        public void TestCalculateEntryFeesAmount()
        {
            Loan myContract = new Loan(new User(),
                ApplicationSettings.GetInstance(""),
                NonWorkingDateSingleton.GetInstance(""), 
                ProvisionTable.GetInstance(new User()), 
                ChartOfAccounts.GetInstance(new User())); 
            new Loan(new User(), 
                ApplicationSettings.GetInstance(""), 
                NonWorkingDateSingleton.GetInstance(""), 
                ProvisionTable.GetInstance(new User()), 
                ChartOfAccounts.GetInstance(new User()));
            myContract.Amount = 500;
            myContract.LoanEntryFeesList = new List<LoanEntryFee>();
            LoanEntryFee fee = new LoanEntryFee();
            fee.FeeValue = (decimal) 0.02;
            
            myContract.LoanEntryFeesList.Add(fee);
            Assert.AreEqual(10m,myContract.CalculateEntryFeesAmount(true)[0].Value);
        }


        [Test]
        public void TestCalculatePastDueSinceLastRepaymentWhenSameYearAndLimitBypassed()
        {
            LoanProduct package = new LoanProduct
                                      {
                                          InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
                                          LoanType = OLoanTypes.Flat,
                                          ChargeInterestWithinGracePeriod = true,
                                          Currency = new Currency { Id = 1 },
                                          KeepExpectedInstallment = false
                                      };
            Loan myContract = new Loan(package, 1000, 0.03m, 6, 2, new DateTime(2006, 1, 1), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));

            myContract.Repay(1, new DateTime(2006, 2, 1), 30, true, false);
            DateTime limit = new DateTime(2006, 5, 1);

            Assert.AreEqual(61, myContract.CalculatePastDueSinceLastRepayment(limit));
        }

        [Test]
        public void TestCalculatePastDueSinceLastRepayment()
        {
            LoanProduct package = new LoanProduct
                                      {
                                          InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
                                          LoanType = OLoanTypes.Flat,
                                          ChargeInterestWithinGracePeriod = true,
                                          KeepExpectedInstallment = false,
                                          Currency = new Currency { Id = 1 }
                                      };
            Loan myContract = new Loan(package, 1000, 0.03m, 6, 2, new DateTime(2006, 1, 1), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));

            //before repayment
            Assert.AreEqual(0, myContract.CalculatePastDueSinceLastRepayment(new DateTime(2006, 2, 1)));
            Assert.AreEqual(14, myContract.CalculatePastDueSinceLastRepayment(new DateTime(2006, 2, 15)));
            Assert.AreEqual(59, myContract.CalculatePastDueSinceLastRepayment(new DateTime(2006, 4, 1)));
            Assert.AreEqual(152, myContract.CalculatePastDueSinceLastRepayment(new DateTime(2006, 7, 3)));

            //partially repayment
            PaymentMethod paymentMethod = new PaymentMethod(1, "Cash", "", false);
            myContract.Repay(1, new DateTime(2006, 2, 1), 30, true, 0, 0, false, 0, false, false, false, paymentMethod);
            myContract.Repay(2, new DateTime(2006, 3, 6), 10, true, 0, 0, false, 0, false, false, false, paymentMethod);

            Assert.AreEqual(0, myContract.CalculatePastDueSinceLastRepayment(new DateTime(2006, 2, 1)));
            Assert.AreEqual(0, myContract.CalculatePastDueSinceLastRepayment(new DateTime(2006, 2, 15)));
            Assert.AreEqual(28, myContract.CalculatePastDueSinceLastRepayment(new DateTime(2006, 4, 3)));
            Assert.AreEqual(119, myContract.CalculatePastDueSinceLastRepayment(new DateTime(2006, 7, 3)));

            //totally repayment
            myContract.Repay(2, new DateTime(2006, 3, 6), 20, true, 0, 0, false, 0, false, false, false, paymentMethod);
            Assert.AreEqual(0, myContract.CalculatePastDueSinceLastRepayment(new DateTime(2006, 2, 1)));
            Assert.AreEqual(0, myContract.CalculatePastDueSinceLastRepayment(new DateTime(2006, 2, 15)));
            Assert.AreEqual(0, myContract.CalculatePastDueSinceLastRepayment(new DateTime(2006, 4, 3)));
            Assert.AreEqual(91, myContract.CalculatePastDueSinceLastRepayment(new DateTime(2006, 7, 3)));
        }

        [Test]
        public void TestCalculatePastDueSinceLastRepayment2()
        {
            LoanProduct package = new LoanProduct
                                      {
                                          InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
                                          LoanType = OLoanTypes.Flat,
                                          ChargeInterestWithinGracePeriod = true,
                                          KeepExpectedInstallment = false,
                                          Currency = new Currency { Id = 1 }
                                      };
            Loan myContract = new Loan(package, 1000, 0.03m, 6, 2, new DateTime(2006, 1, 1), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));
            PaymentMethod paymentMethod = new PaymentMethod(1, "Cash", "", false);
            myContract.Repay(1, new DateTime(2006, 6, 1), 30, true, 0, 0, false, 0, true, false, false, paymentMethod);
            
            Assert.IsTrue(myContract.GetInstallment(0).IsRepaid);

            Assert.AreEqual(122, myContract.CalculatePastDueSinceLastRepayment(new DateTime(2006, 7, 1)));
        }

        [Test]
        public void TestCalculatePendingRepayment()
        {
            LoanProduct package = new LoanProduct
            {
                InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
                LoanType = OLoanTypes.Flat,
                ChargeInterestWithinGracePeriod = true,
                KeepExpectedInstallment = false,
                Currency = new Currency { Id = 1 }
            };
            Loan myContract = new Loan(package, 1000, 0.03m, 6, 2, new DateTime(2006, 1, 1), new User(),
                                       ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""),
                                       ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));

            PaymentMethod paymentMethod = new PaymentMethod(2, "Voucher", "", true);
            RepaymentEvent pEvent = myContract.Repay(1, new DateTime(2006, 6, 1), 30, true, 0, 0, false, 0, true, false, true,
                                                     paymentMethod);

            Assert.IsTrue(myContract.GetInstallment(0).IsRepaid);
            Assert.AreEqual(pEvent.Code, "PBLR");
            Assert.IsTrue(myContract.GetInstallment(0).IsPending);
            myContract.ConfirmPendingRepayment();
            Assert.IsFalse(myContract.GetInstallment(0).IsPending);
        }

        [Test]
        public void TestIfPastDueEqualZeroWhenSameYearAndLimitNotBypassed()
        {
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
            myContract.Repay(3, new DateTime(2006, 4, 3), 200, true, false);

            DateTime today = new DateTime(2006, 4, 5);

            Assert.AreEqual(2, myContract.CalculatePastDueSinceLastRepayment(today));
        }

        [Test]
        public void TestCountFeesIfWeHavePartialRepayment()
        {
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.CALCULATIONLATEFEESDURINGPUBLICHOLIDAYS, true);
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.ACCOUNTINGPROCESS, OAccountingProcesses.Accrual);
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.STOP_WRITEOFF_PENALTY, true);

            LoanProduct package = new LoanProduct
                                      {
                                          InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
                                          LoanType = OLoanTypes.Flat,
                                          ChargeInterestWithinGracePeriod = true,
                                          Currency = new Currency { Id = 1, UseCents = true }
                                      };

            Loan myContract = new Loan(package, 1000, 0.03m, 6, 1, new DateTime(2006, 1, 1), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));
            myContract.NonRepaymentPenalties.OverDuePrincipal = 0.01;

            Assert.AreEqual(new DateTime(2006, 2, 1), myContract.GetInstallment(0).ExpectedDate);
            Assert.AreEqual(new DateTime(2006, 3, 1), myContract.GetInstallment(1).ExpectedDate);

            myContract.Repay(1, new DateTime(2006, 2, 1), 30, true, false);
            myContract.Repay(2, new DateTime(2006, 2, 26), 200, true, false);

            RepaymentEvent rPE = myContract.Repay(2, new DateTime(2006, 3, 4), 30m, false, false);
            Assert.AreEqual(4.96m, rPE.Fees.Value);
            Assert.AreEqual(2.66m, rPE.Interests.Value);
            Assert.AreEqual(22.38m, rPE.Principal.Value);
        }

        [Test]
        public void TestCountFeesWithSmallPrepayment()
        {
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.CALCULATIONLATEFEESDURINGPUBLICHOLIDAYS, true);
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.ACCOUNTINGPROCESS, OAccountingProcesses.Accrual);
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.STOP_WRITEOFF_PENALTY, true);

            LoanProduct package = new LoanProduct
            {
                InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
                LoanType = OLoanTypes.DecliningFixedInstallments,
                ChargeInterestWithinGracePeriod = true,
                Currency = new Currency { Id = 1, UseCents = true }
            };

            Loan myContract = new Loan(package, 1000, 0.03m, 6, 1, new DateTime(2010, 1, 1), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));
            myContract.NonRepaymentPenalties.OverDuePrincipal = 0.01;

            Assert.AreEqual(new DateTime(2010, 2, 1), myContract.GetInstallment(0).ExpectedDate);
            Assert.AreEqual(new DateTime(2010, 3, 1), myContract.GetInstallment(1).ExpectedDate);

            myContract.Repay(1, new DateTime(2010, 2, 1), 30, true, false);
            myContract.Repay(2, new DateTime(2010, 2, 26), 30, true, false);

            RepaymentEvent rPE = myContract.Repay(2, new DateTime(2010, 3, 4), 196.58m, false, false);
            Assert.AreEqual(5.63m, rPE.Fees.Value);
            Assert.AreEqual(3.2m, rPE.Interests.Value);
            Assert.AreEqual(187.75m, rPE.Principal.Value);

            Assert.AreEqual(myContract.GetInstallment(1).IsRepaid, true);
        }

        [Test]
        public void TestCalculateMaximumAmount()
        {
            LoanProduct package = new LoanProduct
                                      {
                                          InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
                                          LoanType = OLoanTypes.DecliningFixedPrincipal,
                                          ChargeInterestWithinGracePeriod = true,
                                          Currency = new Currency { Id = 1 }
                                      };
            package.KeepExpectedInstallment = true;
            package.NonRepaymentPenalties.OLB = 0.001;
            Loan myContract = new Loan(package, 1000, 0.06m, 6, 0, new DateTime(2007, 3, 28), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));

            OCurrency amountToRetreived = Math.Round(myContract.CalculateMaximumAmountAuthorizedToRepay(1, new DateTime(2007, 3, 28), false, 0, 0, false, 0, true).Value);
            Assert.AreEqual(1214m, amountToRetreived.Value);
        }

        [Test]
        public void TestCalculateMaximumAmountForExoticProductTypeForInstallment3()
        {
            LoanProduct package = new LoanProduct
                                      {
                                          InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
                                          LoanType = OLoanTypes.Flat,
                                          ChargeInterestWithinGracePeriod = true,
                                          Currency = new Currency { Id = 1 }
                                      };
            package.KeepExpectedInstallment = true;
            package.NonRepaymentPenalties.OLB = 0.01;
            Loan myContract = new Loan(package, 1000, 0.01m, 3, 0, new DateTime(2008, 12, 17), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));
            myContract.Repay(1, new DateTime(2008, 12, 19), 103, true, true);
            myContract.Repay(2, new DateTime(2008, 12, 19), 618, true, true);

            OCurrency amountToRetreived = Math.Round(myContract.CalculateMaximumAmountAuthorizedToRepay(3, new DateTime(2007, 3, 28), false, 0, 0, false, 0, true, true).Value);

            Assert.AreEqual(310m, amountToRetreived.Value);
        }

        [Test]
        public void TestIfPastDueCorrectlyCalculatedWhenLimitDateYearIsLowerThanInstallmentDateOne()
        {
            LoanProduct package = new LoanProduct
                                      {
                                          InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
                                          LoanType = OLoanTypes.Flat,
                                          ChargeInterestWithinGracePeriod = true,
                                          Currency = new Currency { Id = 1 }
                                      };
            package.KeepExpectedInstallment = false;
            package.NonRepaymentPenalties.OLB = 0.001;
            Loan myContract = new Loan(package, 1000, 0.03m, 6, 1, new DateTime(2006, 1, 1), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));

            Assert.AreEqual(1000m,myContract.GetInstallment(0).OLB.Value);
            Assert.AreEqual(1000m,myContract.GetInstallment(1).OLB.Value);
            Assert.AreEqual(800m,myContract.GetInstallment(2).OLB.Value);
            Assert.AreEqual(600m,myContract.GetInstallment(3).OLB.Value);
        }

        [Test]
        public void TestIfPastDueCorrectlyCalculate()
        {
            LoanProduct package = new LoanProduct
                                      {
                                          InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
                                          LoanType = OLoanTypes.Flat,
                                          ChargeInterestWithinGracePeriod = true,
                                          Currency = new Currency { Id = 1 }
                                      };
            package.KeepExpectedInstallment = false;
            Loan myContract = new Loan(package, 1000, 0.03m, 6, 1, new DateTime(2006, 1, 1), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));

            DateTime limit = new DateTime(2006,1,15);

            Assert.AreEqual(0,myContract.CalculatePastDueSinceLastRepayment(limit));	
        }

        [Test]
        public void TestIfPastDueCorrectlyCalculatedWhenYearAreDifferentAndDifferenceLowerThanOneYear()
        {
            LoanProduct package = new LoanProduct
                                      {
                                          InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
                                          LoanType = OLoanTypes.Flat,
                                          ChargeInterestWithinGracePeriod = true,
                                          Currency = new Currency { Id = 1 }
                                      };
            package.KeepExpectedInstallment = false;
            Loan myContract = new Loan(package, 1000, 0.03m, 6, 1, new DateTime(2006, 1, 1), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));

            DateTime limit = new DateTime(2007,1,1);

            Assert.AreEqual(334,myContract.CalculatePastDueSinceLastRepayment(limit));		
        }

        [Test]
        public void TestIfPastDueCorrectlyCalculatedWhenYearAreDifferentAndDifferenceGreaterThanOneYear()
        {
            LoanProduct package = new LoanProduct
                                      {
                                          InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
                                          LoanType = OLoanTypes.Flat,
                                          ChargeInterestWithinGracePeriod = true,
                                          Currency = new Currency { Id = 1 }
                                      };
            package.KeepExpectedInstallment = false;
            Loan myContract = new Loan(package, 1000, 0.03m, 6, 1, new DateTime(2006, 1, 1), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));

            DateTime limit = new DateTime(2007,3,1);

            Assert.AreEqual(393,myContract.CalculatePastDueSinceLastRepayment(limit));		
        }

        [Test]
        public void TestIfPastDueIsEqualToZeroWhenAllInstallmentsAreRepaid()
        {
            LoanProduct package = new LoanProduct
                                      {
                                          InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
                                          LoanType = OLoanTypes.Flat,
                                          ChargeInterestWithinGracePeriod = true,
                                          Currency = new Currency { Id = 1 }
                                      };
            package.KeepExpectedInstallment = false;
            Loan myContract = new Loan(package, 1000, 0.03m, 6, 2, new DateTime(2006, 1, 1), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));

            myContract.Repay(1, new DateTime(2006, 2, 1), 30, true, false);
            myContract.Repay(2, new DateTime(2006, 3, 1), 30, true, false);
            myContract.Repay(3, new DateTime(2006, 4, 1), 280, true, false);
            myContract.Repay(4, new DateTime(2006, 5, 1), 280, true, false);
            myContract.Repay(5, new DateTime(2006, 6, 1), 280, true, false);
            myContract.Repay(6, new DateTime(2006, 7, 1), 280, true, false);

            Assert.AreEqual(0, myContract.CalculatePastDueSinceLastRepayment(new DateTime(2005, 8, 7)));	
        }

        [Test]
        public void TestSortInstallments()
        {
            LoanProduct package = new LoanProduct {Currency = new Currency {Id = 1},KeepExpectedInstallment = true};
            Loan myContract = new Loan(new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User())); new Loan(new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));
            myContract.Amount = 1000;
            myContract.Product = package;
			
            Installment installment1 = new Installment
                                           {
                                               ExpectedDate = new DateTime(2005, 9, 5),
                                               Number = 1,
                                               InterestsRepayment = 0,
                                               CapitalRepayment = 100
                                           };

            Installment installment2 = new Installment
                                           {
                                               ExpectedDate = new DateTime(2005, 8, 5),
                                               Number = 1,
                                               InterestsRepayment = 0,
                                               CapitalRepayment = 100
                                           };

            Installment installment3 = new Installment
                                           {
                                               ExpectedDate = new DateTime(2005, 12, 5),
                                               Number = 1,
                                               InterestsRepayment = 0,
                                               CapitalRepayment = 100
                                           };

            Installment installment4 = new Installment
                                           {
                                               ExpectedDate = new DateTime(2005, 10, 5),
                                               Number = 1,
                                               InterestsRepayment = 0,
                                               CapitalRepayment = 100
                                           };

            myContract.InstallmentList = new List<Installment> {installment1,installment2,installment3,installment4};

            myContract.InstallmentList.Sort((x, y) => x.ExpectedDate.CompareTo(y.ExpectedDate));
			
            Assert.AreEqual(new DateTime(2005,8,5),myContract.InstallmentList[0].ExpectedDate);
            Assert.AreEqual(new DateTime(2005,9,5),myContract.InstallmentList[1].ExpectedDate);
            Assert.AreEqual(new DateTime(2005,10,5),myContract.InstallmentList[2].ExpectedDate);
            Assert.AreEqual(new DateTime(2005,12,5),myContract.InstallmentList[3].ExpectedDate);
        }

        [Test]
        public void TestVPM()
        {
            LoanProduct package = new LoanProduct
                                      {
                                          InstallmentType = new InstallmentType(1, "Bi-Weekly", 14, 0),
                                          LoanType = OLoanTypes.DecliningFixedInstallments,
                                          ChargeInterestWithinGracePeriod = true,
                                          Currency = new Currency { Id = 1 }
                                      };
            Loan myContract = new Loan(package, 1000, 0.03m, 7, 1, new DateTime(2006, 1, 1), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));
            Assert.AreEqual(160.51m,Decimal.Round(myContract.VPM(1000,7).Value,2));
        }

        [Test]
        public void TestCalculateOverDueAmountWithoutInterest()
        {
            LoanProduct package = new LoanProduct
                                      {
                                          InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
                                          LoanType = OLoanTypes.Flat,
                                          ChargeInterestWithinGracePeriod = true,
                                          Currency = new Currency { Id = 1 }
                                      };
            Loan myContract = new Loan(package, 1000, 0.03m, 6, 1, new DateTime(2006, 1, 1), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));

            Assert.AreEqual(30m, myContract.GetInstallment(0).InterestsRepayment.Value);
            Assert.AreEqual(0m, myContract.GetInstallment(0).CapitalRepayment.Value);
            Assert.AreEqual(new DateTime(2006, 2, 1), myContract.GetInstallment(0).ExpectedDate);

            Assert.AreEqual(30m, myContract.GetInstallment(1).InterestsRepayment.Value);
            Assert.AreEqual(200m, myContract.GetInstallment(1).CapitalRepayment.Value);
            Assert.AreEqual(new DateTime(2006, 3, 1), myContract.GetInstallment(1).ExpectedDate);
            Assert.AreEqual(new DateTime(2006, 4, 3), myContract.GetInstallment(2).ExpectedDate);

            Assert.AreEqual(0m, myContract.CalculateOverduePrincipal(new DateTime(2006, 1, 9)).Value);

            Assert.AreEqual(400m, myContract.CalculateOverduePrincipal(new DateTime(2006, 4, 9)).Value);

            Assert.AreEqual(400m, myContract.CalculateOverduePrincipal(new DateTime(2006, 4, 9)).Value);

            myContract.Repay(1, new DateTime(2006, 2, 1), 30, true, false);
            Assert.AreEqual(400m, myContract.CalculateOverduePrincipal(new DateTime(2006, 4, 9)).Value);

            myContract.Repay(2, new DateTime(2006, 3, 1), 230, true, false);
            Assert.AreEqual(200m, myContract.CalculateOverduePrincipal(new DateTime(2006, 4, 9)).Value);

            myContract.Repay(3, new DateTime(2006, 4, 3), 130, true, false);
            Assert.AreEqual(100m, myContract.CalculateOverduePrincipal(new DateTime(2006, 4, 9)).Value);
        }

        [Test]
        public void TestCalculateOverDueAmountWithInterest()
        {
            LoanProduct package = new LoanProduct
                                      {
                                          InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
                                          LoanType = OLoanTypes.DecliningFixedInstallments,
                                          ChargeInterestWithinGracePeriod = true,
                                          Currency = new Currency { Id = 1, UseCents = true},
                                          RoundingType = ORoundingType.Approximate
                                      };
            Loan myContract = new Loan(package, 1000, 0.03m, 12, 0, new DateTime(2006, 1, 1), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));
            myContract.NonRepaymentPenalties.OLB = 0.0016;

            Assert.AreEqual(30m,myContract.GetInstallment(0).InterestsRepayment.Value);
            Assert.AreEqual(70.46m,myContract.GetInstallment(0).CapitalRepayment.Value);
            Assert.AreEqual(new DateTime(2006,2,1),myContract.GetInstallment(0).ExpectedDate);


            Assert.AreEqual(1000, _CheckInstalmentsTotalPrincipal(myContract).Value);

            Assert.AreEqual(27.89m,myContract.GetInstallment(1).InterestsRepayment.Value);
            Assert.AreEqual(72.57m,myContract.GetInstallment(1).CapitalRepayment.Value);
            Assert.AreEqual(new DateTime(2006,3,1),myContract.GetInstallment(1).ExpectedDate);
            Assert.AreEqual(new DateTime(2006,4,3),myContract.GetInstallment(2).ExpectedDate);
            Assert.AreEqual(new DateTime(2006,5,1),myContract.GetInstallment(3).ExpectedDate);
            Assert.AreEqual(new DateTime(2006,6,1),myContract.GetInstallment(4).ExpectedDate);
            Assert.AreEqual(new DateTime(2006,7,3),myContract.GetInstallment(5).ExpectedDate);
        }

        private static OCurrency _CheckInstalmentsTotalPrincipal(Loan pLoan)
        {
            OCurrency amount = 0;
            foreach (Installment installment in pLoan.InstallmentList)
            {
                amount += installment.CapitalRepayment;
            }
            return amount;
        }

        [Test]
        public void TestIfExtraAmountIsCorrectlySpreadOutFlatRateAndExotics1()
        {
            Assert.Ignore();
            //ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.CALCULATIONLATEFEESDURINGPUBLICHOLIDAYS, true);

            //LoanProduct package = new LoanProduct
            //                          {
            //                              InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
            //                              LoanType = OLoanTypes.Flat,
            //                              ChargeInterestWithinGracePeriod = true,
            //                              Currency = new Currency { Id = 1 }
            //                          };
			
            //ExoticInstallmentsTable exoticProduct = new ExoticInstallmentsTable();
            //exoticProduct.Add(new ExoticInstallment(1,0,0.2));
            //exoticProduct.Add(new ExoticInstallment(2,0,0.2));
            //exoticProduct.Add(new ExoticInstallment(3,0.5,0.2));
            //exoticProduct.Add(new ExoticInstallment(4,0.3,0.1));
            //exoticProduct.Add(new ExoticInstallment(5,0.2,0.3));
            //package.ExoticProduct = exoticProduct;

            //Loan myContract = new Loan(package, 1000, 0.03m, 7, 2, new DateTime(2006, 1, 1), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisioningTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));
            //Assert.AreEqual(new DateTime(2006,2,1),myContract.GetInstallment(0).ExpectedDate);
			
            //myContract.Repay(1,new DateTime(2006,2,1),810,true,false);

            //Assert.AreEqual(0m,myContract.GetInstallment(0).CapitalRepayment.Value);
            //Assert.AreEqual(30m,myContract.GetInstallment(0).InterestsRepayment.Value);
            //Assert.AreEqual(1000m,myContract.GetInstallment(0).OLB.Value);
            //Assert.AreEqual(0m,myContract.GetInstallment(0).PaidCapital.Value);
            //Assert.AreEqual(30m,myContract.GetInstallment(0).PaidInterests.Value);

            ////210 interest 600 overdue principal 400 OLB
            //Assert.AreEqual(0m,myContract.GetInstallment(1).CapitalRepayment.Value);
            //Assert.AreEqual(0m,myContract.GetInstallment(1).InterestsRepayment.Value);
            //Assert.AreEqual(400m,myContract.GetInstallment(1).OLB.Value);
            //Assert.AreEqual(0m,myContract.GetInstallment(1).PaidCapital.Value);
            //Assert.AreEqual(0m,myContract.GetInstallment(1).PaidInterests.Value);

            //Assert.AreEqual(0m,myContract.GetInstallment(2).CapitalRepayment.Value);   //0 
            //Assert.AreEqual(0m,myContract.GetInstallment(2).InterestsRepayment.Value);//0.2
            //Assert.AreEqual(400m,myContract.GetInstallment(2).OLB.Value);
            //Assert.AreEqual(0m,myContract.GetInstallment(2).PaidCapital.Value);
            //Assert.AreEqual(0m,myContract.GetInstallment(2).PaidInterests.Value);

            //Assert.AreEqual(0m,myContract.GetInstallment(3).CapitalRepayment.Value); //0
            //Assert.AreEqual(0m,myContract.GetInstallment(3).InterestsRepayment.Value);//0.2
            //Assert.AreEqual(400m,myContract.GetInstallment(3).OLB.Value);
            //Assert.AreEqual(0m,myContract.GetInstallment(3).PaidCapital.Value);
            //Assert.AreEqual(0m,myContract.GetInstallment(3).PaidInterests.Value);

            //Assert.AreEqual(200m,myContract.GetInstallment(4).CapitalRepayment.Value);   //0.5
            //Assert.AreEqual(0m,myContract.GetInstallment(4).InterestsRepayment.Value); //0.2
            //Assert.AreEqual(400m,myContract.GetInstallment(4).OLB.Value);
            //Assert.AreEqual(0m,myContract.GetInstallment(4).PaidCapital.Value);
            //Assert.AreEqual(0m,myContract.GetInstallment(4).PaidInterests.Value);

            //Assert.AreEqual(120m,myContract.GetInstallment(5).CapitalRepayment.Value);   //0.3
            //Assert.AreEqual(0m,myContract.GetInstallment(5).InterestsRepayment.Value); //0.1
            //Assert.AreEqual(200m,myContract.GetInstallment(5).OLB.Value);
            //Assert.AreEqual(0m,myContract.GetInstallment(5).PaidCapital.Value);
            //Assert.AreEqual(0m,myContract.GetInstallment(5).PaidInterests.Value);

            //Assert.AreEqual(80m,myContract.GetInstallment(6).CapitalRepayment.Value); //0.2
            //Assert.AreEqual(0m,myContract.GetInstallment(6).InterestsRepayment.Value); //0.3
            //Assert.AreEqual(80m,myContract.GetInstallment(6).OLB.Value);
            //Assert.AreEqual(0m,myContract.GetInstallment(6).PaidCapital.Value);
            //Assert.AreEqual(0m,myContract.GetInstallment(6).PaidInterests.Value);
        }

        [Test]
        public void TestIfExtraAmountIsCorrectlySpreadOutFlatRateAndExotics2()
        {
            Assert.Ignore();
            //ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.CALCULATIONLATEFEESDURINGPUBLICHOLIDAYS, true);

            //LoanProduct package = new LoanProduct
            //                          {
            //                              InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
            //                              LoanType = OLoanTypes.Flat,
            //                              ChargeInterestWithinGracePeriod = true,
            //                              Currency = new Currency { Id = 1 }
            //                          };

            //ExoticInstallmentsTable exoticProduct = new ExoticInstallmentsTable();
            //exoticProduct.Add(new ExoticInstallment(1,0  ,0.1));
            //exoticProduct.Add(new ExoticInstallment(2,0  ,0.1));
            //exoticProduct.Add(new ExoticInstallment(3,0.5,0.2));
            //exoticProduct.Add(new ExoticInstallment(4,0  ,0.3));
            //exoticProduct.Add(new ExoticInstallment(5,0  ,0.2));
            //exoticProduct.Add(new ExoticInstallment(6,0.5,0.1));
            //package.ExoticProduct = exoticProduct;

            //Loan myContract = new Loan(package, 1000, 0.03m, 7, 1, new DateTime(2006, 1, 1), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisioningTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));
            //myContract.Repay(1,new DateTime(2006,2,1),410,true,false);

            //Assert.AreEqual(0,myContract.GetInstallment(0).CapitalRepayment.Value);
            //Assert.AreEqual(30,myContract.GetInstallment(0).InterestsRepayment.Value);
            //Assert.AreEqual(1000,myContract.GetInstallment(0).OLB.Value);
            //Assert.AreEqual(0,myContract.GetInstallment(0).PaidCapital.Value);
            //Assert.AreEqual(30,myContract.GetInstallment(0).PaidInterests.Value);

            ////200 overdue 800 Of OLB
            //Assert.AreEqual(0,myContract.GetInstallment(1).CapitalRepayment.Value);//
            //Assert.AreEqual(0,myContract.GetInstallment(1).InterestsRepayment.Value);
            //Assert.AreEqual(800,myContract.GetInstallment(1).OLB.Value);
            //Assert.AreEqual(0,myContract.GetInstallment(1).PaidCapital.Value);
            //Assert.AreEqual(0,myContract.GetInstallment(1).PaidInterests.Value);

            //Assert.AreEqual(0,myContract.GetInstallment(2).CapitalRepayment.Value);
            //Assert.AreEqual(0,myContract.GetInstallment(2).InterestsRepayment.Value);
            //Assert.AreEqual(800,myContract.GetInstallment(2).OLB.Value);
            //Assert.AreEqual(0,myContract.GetInstallment(2).PaidCapital.Value);
            //Assert.AreEqual(0,myContract.GetInstallment(2).PaidInterests.Value);

            //Assert.AreEqual(400,myContract.GetInstallment(3).CapitalRepayment.Value);
            //Assert.AreEqual(0,myContract.GetInstallment(3).InterestsRepayment.Value);
            //Assert.AreEqual(800,myContract.GetInstallment(3).OLB.Value);
            //Assert.AreEqual(0,myContract.GetInstallment(3).PaidCapital.Value);
            //Assert.AreEqual(0,myContract.GetInstallment(3).PaidInterests.Value);

            //Assert.AreEqual(0,myContract.GetInstallment(4).CapitalRepayment.Value);
            //Assert.AreEqual(0,myContract.GetInstallment(4).InterestsRepayment.Value);
            //Assert.AreEqual(400,myContract.GetInstallment(4).OLB.Value);
            //Assert.AreEqual(0,myContract.GetInstallment(4).PaidCapital.Value);
            //Assert.AreEqual(0,myContract.GetInstallment(4).PaidInterests.Value);

            //Assert.AreEqual(0,myContract.GetInstallment(5).CapitalRepayment.Value);
            //Assert.AreEqual(0,myContract.GetInstallment(5).InterestsRepayment.Value);
            //Assert.AreEqual(400,myContract.GetInstallment(5).OLB.Value);
            //Assert.AreEqual(0,myContract.GetInstallment(5).PaidCapital.Value);
            //Assert.AreEqual(0,myContract.GetInstallment(5).PaidInterests.Value);

            //Assert.AreEqual(400,myContract.GetInstallment(6).CapitalRepayment.Value);
            //Assert.AreEqual(0,myContract.GetInstallment(6).InterestsRepayment.Value);
            //Assert.AreEqual(400,myContract.GetInstallment(6).OLB.Value);
            //Assert.AreEqual(0,myContract.GetInstallment(6).PaidCapital.Value);
            //Assert.AreEqual(0,myContract.GetInstallment(6).PaidInterests.Value);
        }

        [Test]
        public void TestIfExtraAmountIsCorrectlySpreadOutFlatRateAndExotics3()
        {
            Assert.Ignore();
            //ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.CALCULATIONLATEFEESDURINGPUBLICHOLIDAYS, true);

            //LoanProduct package = new LoanProduct
            //                          {
            //                              InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
            //                              LoanType = OLoanTypes.Flat,
            //                              ChargeInterestWithinGracePeriod = true,
            //                              Currency = new Currency { Id = 1 }
            //                          };

            //ExoticInstallmentsTable exoticProduct = new ExoticInstallmentsTable();
            //exoticProduct.Add(new ExoticInstallment(1,0  ,0.1));
            //exoticProduct.Add(new ExoticInstallment(2,0  ,0.1));
            //exoticProduct.Add(new ExoticInstallment(3,0.5,0.2));
            //exoticProduct.Add(new ExoticInstallment(4,0  ,0.3));
            //exoticProduct.Add(new ExoticInstallment(5,0  ,0.2));
            //exoticProduct.Add(new ExoticInstallment(6,0.5,0.1));
            //package.ExoticProduct = exoticProduct;

            //Loan myContract = new Loan(package, 1000, 0.03m, 7, 1, new DateTime(2006, 1, 1), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisioningTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));
            //myContract.Repay(1,new DateTime(2006,2,1),130,true,false);

            //Assert.AreEqual(0,myContract.GetInstallment(0).CapitalRepayment.Value);
            //Assert.AreEqual(30,myContract.GetInstallment(0).InterestsRepayment.Value);
            //Assert.AreEqual(1000,myContract.GetInstallment(0).OLB.Value);
            //Assert.AreEqual(0,myContract.GetInstallment(0).PaidCapital.Value);
            //Assert.AreEqual(30,myContract.GetInstallment(0).PaidInterests.Value);

            //Assert.AreEqual(0,myContract.GetInstallment(1).CapitalRepayment.Value);//
            //Assert.AreEqual(8,myContract.GetInstallment(1).InterestsRepayment.Value);
            //Assert.AreEqual(1000,myContract.GetInstallment(1).OLB.Value);
            //Assert.AreEqual(0,myContract.GetInstallment(1).PaidCapital.Value);
            //Assert.AreEqual(0,myContract.GetInstallment(1).PaidInterests.Value);

            //Assert.AreEqual(0,myContract.GetInstallment(2).CapitalRepayment.Value);
            //Assert.AreEqual(8,myContract.GetInstallment(2).InterestsRepayment.Value);
            //Assert.AreEqual(1000,myContract.GetInstallment(2).OLB.Value);
            //Assert.AreEqual(0,myContract.GetInstallment(2).PaidCapital.Value);
            //Assert.AreEqual(0,myContract.GetInstallment(2).PaidInterests.Value);

            //Assert.AreEqual(500,myContract.GetInstallment(3).CapitalRepayment.Value);
            //Assert.AreEqual(16,myContract.GetInstallment(3).InterestsRepayment.Value);
            //Assert.AreEqual(1000,myContract.GetInstallment(3).OLB.Value);
            //Assert.AreEqual(0,myContract.GetInstallment(3).PaidCapital.Value);
            //Assert.AreEqual(0,myContract.GetInstallment(3).PaidInterests.Value);

            //Assert.AreEqual(0,myContract.GetInstallment(4).CapitalRepayment.Value);
            //Assert.AreEqual(24,myContract.GetInstallment(4).InterestsRepayment.Value);
            //Assert.AreEqual(500,myContract.GetInstallment(4).OLB.Value);
            //Assert.AreEqual(0,myContract.GetInstallment(4).PaidCapital.Value);
            //Assert.AreEqual(0,myContract.GetInstallment(4).PaidInterests.Value);

            //Assert.AreEqual(0,myContract.GetInstallment(5).CapitalRepayment.Value);
            //Assert.AreEqual(16,myContract.GetInstallment(5).InterestsRepayment.Value);
            //Assert.AreEqual(500,myContract.GetInstallment(5).OLB.Value);
            //Assert.AreEqual(0,myContract.GetInstallment(5).PaidCapital.Value);
            //Assert.AreEqual(0,myContract.GetInstallment(5).PaidInterests.Value);

            //Assert.AreEqual(500,myContract.GetInstallment(6).CapitalRepayment.Value);
            //Assert.AreEqual(8,myContract.GetInstallment(6).InterestsRepayment.Value);
            //Assert.AreEqual(500,myContract.GetInstallment(6).OLB.Value);
            //Assert.AreEqual(0,myContract.GetInstallment(6).PaidCapital.Value);
            //Assert.AreEqual(0,myContract.GetInstallment(6).PaidInterests.Value);
        }
        [Test]
        public void TestIfInstallmentsAreCorrectlyRecalculatedWhenExoticDecliningOverPaid()
        {
            Assert.Ignore();
            //ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.CALCULATIONLATEFEESDURINGPUBLICHOLIDAYS, true);

            //LoanProduct package = new LoanProduct
            //                          {
            //                              InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
            //                              LoanType = OLoanTypes.DecliningFixedInstallments,
            //                              ChargeInterestWithinGracePeriod = true,
            //                              Currency = new Currency { Id = 1 }
            //                          };
            //ExoticInstallmentsTable exoticProduct = new ExoticInstallmentsTable();
            //exoticProduct.Add(new ExoticInstallment(1,0.1,null));
            //exoticProduct.Add(new ExoticInstallment(2,0.1,null));
            //exoticProduct.Add(new ExoticInstallment(3,0.1,null));
            //exoticProduct.Add(new ExoticInstallment(4,0.1,null));
            //exoticProduct.Add(new ExoticInstallment(5,0.1,null));
            //exoticProduct.Add(new ExoticInstallment(6,0.5,null));
            //package.ExoticProduct = exoticProduct;

            //Loan myContract = new Loan(package, 1000, 0.03m, 6, 0, new DateTime(2006, 1, 1), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisioningTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));
			
            //myContract.Repay(1,new DateTime(2006,2,1),130,false,false);
            //myContract.Repay(2,new DateTime(2006,3,1),127,false,false);
            //RepaymentEvent rPE = myContract.Repay(3,new DateTime(2006,4,3),174,true,false);

            //Assert.AreEqual(24,myContract.GetInstallment(2).PaidInterests.Value);
            //Assert.AreEqual(100,myContract.GetInstallment(2).PaidCapital.Value);
            //Assert.AreEqual(800,myContract.GetInstallment(2).OLB.Value);

            //Assert.AreEqual(19.5m,myContract.GetInstallment(3).InterestsRepayment.Value);
            //Assert.AreEqual(92.86m, myContract.GetInstallment(3).CapitalRepayment.Value);
            //Assert.AreEqual(650, myContract.GetInstallment(3).OLB.Value);

            //Assert.AreEqual(24,rPE.Interests.Value);
            //Assert.AreEqual(150,rPE.Principal.Value);
        }

        [Test]
        public void TestRepayFirstInstallmentDecliningRateNoOverPaid()
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
            Loan myContract = new Loan(package, 1000, 0.03m, 6, 0, new DateTime(2006, 1, 1), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));
            myContract.Repay(1, new DateTime(2006, 2, 1), 184.60m, false, false);

            Assert.AreEqual(154.60m, myContract.GetInstallment(0).PaidCapital.Value);
            Assert.AreEqual(30m, myContract.GetInstallment(0).PaidInterests.Value);
            Assert.IsTrue(myContract.GetInstallment(0).IsRepaid);
        }

        [Test]
        public void TestRepaySecondInstallmentDecliningRateNoOverPaid()
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
            Loan myContract = new Loan(package, 1000, 0.03m, 6, 0, new DateTime(2006, 1, 1), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));
            myContract.Repay(1, new DateTime(2006, 2, 1), 184.60m, false, false);
            myContract.Repay(2, new DateTime(2006, 3, 1), 184.60m, false, false);

            Assert.AreEqual(159.24m, myContract.GetInstallment(1).PaidCapital.Value);
            Assert.AreEqual(25.36m, myContract.GetInstallment(1).PaidInterests.Value);
            Assert.IsTrue(myContract.GetInstallment(1).IsRepaid);
        }

        [Test]
        public void TestRepayThirdInstallmentDecliningRateWithOverPaid()
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
            Loan myContract = new Loan(package, 1000, 0.03m, 6, 0, new DateTime(2006, 1, 1), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));

            Assert.AreEqual(1000, _CheckInstalmentsTotalPrincipal(myContract).Value);

            myContract.AnticipatedTotalRepaymentPenalties = 0.01;
            myContract.AnticipatedPartialRepaymentPenalties = 0.01;

            myContract.Repay(1, new DateTime(2006, 2, 1), 184.60m, true, false);
            myContract.Repay(2, new DateTime(2006, 3, 1), 184.60m, true, false);
            
            RepaymentEvent rPE = myContract.Repay(3, new DateTime(2006, 4, 3), 250m, false, false);

            Assert.AreEqual(5.22m, rPE.Fees.Value);
            Assert.AreEqual(20.58m, rPE.Interests.Value);
            Assert.AreEqual(224.2m, rPE.Principal.Value);

            Assert.AreEqual(164.02m, myContract.GetInstallment(2).PaidCapital.Value);
            Assert.AreEqual(20.58m, myContract.GetInstallment(2).PaidInterests.Value);
            Assert.IsTrue(myContract.GetInstallment(2).IsRepaid);
            Assert.AreEqual(149.46m, myContract.GetInstallment(3).CapitalRepayment.Value);
            Assert.AreEqual(13.86m, myContract.GetInstallment(3).InterestsRepayment.Value);
            Assert.AreEqual(461.96m, myContract.GetInstallment(3).OLB.Value);
        }

        [Test]
        public void TestRepayFirstInstallmentDecliningRateWithOverPaidAndFiveGracePeriod()
        {
            Assert.Ignore();
            //ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.CALCULATIONLATEFEESDURINGPUBLICHOLIDAYS, true);

            //LoanProduct package = new LoanProduct
            //                          {
            //                              InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
            //                              LoanType = OLoanTypes.DecliningFixedInstallments,
            //                              ChargeInterestWithinGracePeriod = true,
            //                              Currency = new Currency { Id = 1 }
            //                          };
            //Loan myContract = new Loan(package, 600, 0.02, 6, 5, new DateTime(2006, 1, 1), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisioningTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));
            //Assert.AreEqual(6, myContract.InstallmentList.Count);
            //Assert.AreEqual(myContract.GetInstallment(0).CapitalRepayment.Value, 0);
            //Assert.AreEqual(myContract.GetInstallment(0).InterestsRepayment.Value, 12m);

            //myContract.Repay(1, new DateTime(2006, 2, 1), 13, true, false);

            //Assert.AreEqual(0m, myContract.GetInstallment(0).PaidCapital.Value);
            //Assert.AreEqual(12m, myContract.GetInstallment(0).PaidInterests.Value);
            //Assert.AreEqual(600m, myContract.GetInstallment(0).OLB.Value);
            //Assert.IsTrue(myContract.GetInstallment(0).IsRepaid);

            //Assert.AreEqual(0m, myContract.GetInstallment(1).CapitalRepayment.Value);
            //Assert.AreEqual(11.98m, myContract.GetInstallment(1).InterestsRepayment.Value);
            //Assert.AreEqual(599m, myContract.GetInstallment(1).OLB.Value);
        }

        [Test]
        public void TestRepayEventCorrectlyGeneratedWithThirdInstallmentFlatRateWithOverPaid()
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
            Loan myContract = new Loan(package, 1000, 0.03m, 6, 1, new DateTime(2006, 1, 1), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));
            
            myContract.AnticipatedTotalRepaymentPenalties = 0.01;
            myContract.AnticipatedPartialRepaymentPenalties = 0.01;

            myContract.Repay(1, new DateTime(2006, 2, 1), 30m, false, false);
            myContract.Repay(2, new DateTime(2006, 3, 1), 230m, false, false);
            RepaymentEvent rPE = myContract.Repay(3, new DateTime(2006, 4, 3), 530m, false, false);

            Assert.AreEqual(6, rPE.Fees.Value);
            Assert.AreEqual(30, rPE.Interests.Value);
            Assert.AreEqual(494, rPE.Principal.Value);
        }

        [Test]
        public void TestRepayEventCorrectlyGeneratedWithThirdInstallmentDecliningWithOverPaid()
        {
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.ACCOUNTINGPROCESS, OAccountingProcesses.Accrual);

            LoanProduct package = new LoanProduct
                                      {
                                          InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
                                          LoanType = OLoanTypes.DecliningFixedInstallments,
                                          ChargeInterestWithinGracePeriod = true,
                                          Currency = new Currency { Id = 1 }
                                      };
            Loan myContract = new Loan(package, 600, 0.025m, 6, 0, new DateTime(2006, 1, 1), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));
            RepaymentEvent rPE = myContract.Repay(1, new DateTime(2006, 2, 1), 115m, false, false);

            Assert.AreEqual(15m, rPE.Interests.Value);
            Assert.AreEqual(100m, rPE.Principal.Value);
        }

        [Test]
        public void TestRepayEventCorrectlyGenerateWhenTenDaysLateAndPayFees()
        {
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.CALCULATIONLATEFEESDURINGPUBLICHOLIDAYS, true);
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.STOP_WRITEOFF_PENALTY, true);

            LoanProduct package = new LoanProduct
                                      {
                                          InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
                                          LoanType = OLoanTypes.Flat,
                                          ChargeInterestWithinGracePeriod = true,
                                          Currency = new Currency { Id = 1 }
                                      };
            Loan myContract = new Loan(package, 1000, 0.03m, 6, 1, new DateTime(2006, 1, 1), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));
            myContract.NonRepaymentPenalties.OLB = 0.003;

            Assert.AreEqual(new DateTime(2006, 2, 1), myContract.GetInstallment(0).ExpectedDate);
            Assert.AreEqual(new DateTime(2006, 3, 1), myContract.GetInstallment(1).ExpectedDate);

            myContract.Repay(1, new DateTime(2006, 2, 1), 30, true, false);

            RepaymentEvent rPE = myContract.Repay(2, new DateTime(2006, 3, 11), 230.41m, false, false);

            Assert.AreEqual(30m, rPE.Fees.Value);
            Assert.AreEqual(30m, rPE.Interests.Value);
            Assert.AreEqual(170.41m, rPE.Principal.Value);
        }

        [Test]
        public void TestRepayEventCorrectlyGeneratedWithThirdInstallmentFlatRateExoticLoanWithOverPaid()
        {
            Assert.Ignore();
            //ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.CALCULATIONLATEFEESDURINGPUBLICHOLIDAYS, true);

            //LoanProduct package = new LoanProduct
            //                          {
            //                              InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
            //                              LoanType = OLoanTypes.Flat,
            //                              ChargeInterestWithinGracePeriod = true,
            //                              Currency = new Currency { Id = 1 }
            //                          };

            //ExoticInstallmentsTable exoticProduct = new ExoticInstallmentsTable();
            //exoticProduct.Add(new ExoticInstallment(1,0,0.1));
            //exoticProduct.Add(new ExoticInstallment(2,0,0.1));
            //exoticProduct.Add(new ExoticInstallment(3,0.5,0.1));
            //exoticProduct.Add(new ExoticInstallment(4,0,0.1));
            //exoticProduct.Add(new ExoticInstallment(5,0,0.5));
            //exoticProduct.Add(new ExoticInstallment(6,0.5,0.1));
            //exoticProduct.Add(new ExoticInstallment(7,0,0));
            //package.ExoticProduct = exoticProduct;

            //Loan myContract = new Loan(package, 1000, 0.02m, 7, 1, new DateTime(2006, 1, 1), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisioningTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));
            //myContract.AnticipatedTotalRepaymentPenalties = 0.01;
            //myContract.Repay(1,new DateTime(2006,2,1),20,false,false);
            //myContract.Repay(2,new DateTime(2006,3,1),12,false,false);
            //myContract.Repay(3,new DateTime(2006,4,3),12,false,false);
            //RepaymentEvent rPE = myContract.Repay(4,new DateTime(2006,5,1),584,false,false);

            //Assert.AreEqual(500m,rPE.Principal.Value);
            //Assert.AreEqual(79m,rPE.Interests.Value);
            //Assert.AreEqual(5m,rPE.Fees.Value);
        }

        [Test]
        public void TestRepayWhenOverPaidGreaterThanMaximumAmountAuthorized()
        {
            LoanProduct package = new LoanProduct
                                      {
                                          InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
                                          LoanType = OLoanTypes.Flat,
                                          ChargeInterestWithinGracePeriod = true,
                                          Currency = new Currency { Id = 1 }
                                      };

            ExoticInstallmentsTable exoticProduct = new ExoticInstallmentsTable();
            exoticProduct.Add(new ExoticInstallment(1,0,1));
            exoticProduct.Add(new ExoticInstallment(2,0,1));
            exoticProduct.Add(new ExoticInstallment(3,0.5,1));
            exoticProduct.Add(new ExoticInstallment(4,0,1));
            exoticProduct.Add(new ExoticInstallment(5,0,1));
            exoticProduct.Add(new ExoticInstallment(6,0.5,1));
            package.ExoticProduct = exoticProduct;

            Loan myContract = new Loan(package, 1000, 0.02m, 7, 1, new DateTime(2006, 1, 1), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));
            myContract.AnticipatedTotalRepaymentPenalties = 0.01;
            myContract.Repay(1,new DateTime(2006,2,1),20,false,false);
            myContract.Repay(2,new DateTime(2006,3,1),20,false,false);
            myContract.Repay(3,new DateTime(2006,4,1),20,false,false);
            RepaymentEvent rPE = myContract.Repay(4,new DateTime(2006,1,1),2000,false,false);

            Assert.IsNull(rPE);
        }

        [Test]
        public void TestRepayWhenOverPaidGreaterThanMaximumAmountAuthorizedWhenBadLoan()
        {
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.CALCULATIONLATEFEESDURINGPUBLICHOLIDAYS, true);
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.STOP_WRITEOFF_PENALTY, true);

            LoanProduct package = new LoanProduct
                                      {
                                          InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
                                          LoanType = OLoanTypes.Flat,
                                          ChargeInterestWithinGracePeriod = true,
                                          Currency = new Currency { Id = 1 }
                                      };

            ExoticInstallmentsTable exoticProduct = new ExoticInstallmentsTable();
            exoticProduct.Add(new ExoticInstallment(1,0,0.1));
            exoticProduct.Add(new ExoticInstallment(2,0,0.1));
            exoticProduct.Add(new ExoticInstallment(3,0.5,0.1));
            exoticProduct.Add(new ExoticInstallment(4,0,0.1));
            exoticProduct.Add(new ExoticInstallment(5,0,0.1));
            exoticProduct.Add(new ExoticInstallment(6,0.5,0.1));
            exoticProduct.Add(new ExoticInstallment(7,0,0));
            package.ExoticProduct = exoticProduct;

            Loan myContract = new Loan(package, 1000, 0.02m, 7, 1, new DateTime(2006, 1, 1), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));
            myContract.AnticipatedTotalRepaymentPenalties = 0.01;
            myContract.NonRepaymentPenalties.OLB = 0.02;
            myContract.Repay(1,new DateTime(2006,2,1),20,false,false);
            myContract.Repay(2,new DateTime(2006,3,1),20,false,false);
            myContract.Repay(3,new DateTime(2006,4,1),20,false,false);
            RepaymentEvent rPE = myContract.Repay(4,new DateTime(2006,1,1),2000,false,false);

            Assert.IsNull(rPE);
        }

        [Test]
        public void TestRepayFlateLoanWhenKeepExpectedInstallment()
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
        public void TestRepayButRepaymentEventIsNull()
        {
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.CALCULATIONLATEFEESDURINGPUBLICHOLIDAYS, true);

            LoanProduct package = new LoanProduct
                                      {
                                          InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
                                          LoanType = OLoanTypes.DecliningFixedInstallments,
                                          ChargeInterestWithinGracePeriod = true,
                                          Currency = new Currency { Id = 1 }
                                      };
            Loan myContract = new Loan(package, 1000, 0.03m, 7, 1, new DateTime(2006, 1, 1), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));

            RepaymentEvent rE = myContract.Repay(1, new DateTime(2006, 2, 1), 1032, true, false);
            Assert.IsNull(rE);

            RepaymentEvent rE2 = myContract.Repay(1, new DateTime(2006, 2, 1), 30, true, false);
            Assert.IsNotNull(rE2);
        }

        [Test]
        public void TestRepayDecliningLoanWhenKeepNotExpectedInstallment()
        {
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.CALCULATIONLATEFEESDURINGPUBLICHOLIDAYS, true);

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
            myContract.Repay(2, new DateTime(2006, 3, 1), 184.6m, true, false);

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
            Assert.AreEqual(0m, myContract.GetInstallment(2).PaidCapital.Value);
            Assert.AreEqual(0m, myContract.GetInstallment(2).PaidInterests.Value);
        }

        [Test]
        public void TestRepayDecliningLoanWhenKeepExpectedInstallment()
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
            Loan myContract = new Loan(package, 1000, 0.03m, 7, 1, new DateTime(2006, 1, 1), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));

            myContract.Repay(1, new DateTime(2006, 2, 1), 30, true, false);
            myContract.Repay(2, new DateTime(2006, 3, 1), 184.6m, true, false);

            Assert.AreEqual(30, myContract.GetInstallment(0).InterestsRepayment.Value);
            Assert.AreEqual(0, myContract.GetInstallment(0).CapitalRepayment.Value);
            Assert.AreEqual(1000, myContract.GetInstallment(0).OLB.Value);
            Assert.AreEqual(0, myContract.GetInstallment(0).PaidCapital.Value);
            Assert.AreEqual(30, myContract.GetInstallment(0).PaidInterests.Value);

            Assert.AreEqual(30, myContract.GetInstallment(1).InterestsRepayment.Value);
            Assert.AreEqual(154.60m, myContract.GetInstallment(1).CapitalRepayment.Value);
            Assert.AreEqual(1000, myContract.GetInstallment(1).OLB.Value);
            Assert.AreEqual(154.60m, myContract.GetInstallment(1).PaidCapital.Value);
            Assert.AreEqual(30, myContract.GetInstallment(1).PaidInterests.Value);

            Assert.AreEqual(25.36m, myContract.GetInstallment(2).InterestsRepayment.Value);
            Assert.AreEqual(159.24m, myContract.GetInstallment(2).CapitalRepayment.Value);
            Assert.AreEqual(845.40m, myContract.GetInstallment(2).OLB.Value);
            Assert.AreEqual(0, myContract.GetInstallment(2).PaidCapital.Value);
            Assert.AreEqual(0, myContract.GetInstallment(2).PaidInterests.Value);
        }

        [Test]
        public void TestRepayFlateLoanWhenKeepExpectedInstallmentButSecondInstallmentNotEntirlyPaidTheSecondInstallment()
        {
            LoanProduct package = new LoanProduct
                                      {
                                          InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
                                          LoanType = OLoanTypes.Flat,
                                          ChargeInterestWithinGracePeriod = true,
                                          Currency = new Currency { Id = 1 }
                                      };
            package.KeepExpectedInstallment = true;
            Loan myContract = new Loan(package, 1000, 0.03m, 6, 1, new DateTime(2006, 1, 1), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));

            myContract.Repay(1, new DateTime(2006, 2, 1), 30, true, false);
            myContract.Repay(2, new DateTime(2006, 3, 1), 200, true, false);

            _AssertSpecifiedInstallment(myContract.GetInstallment(0), 1, new DateTime(2006, 2, 1), 30, 0, 1000, 0, 30);
            _AssertSpecifiedInstallment(myContract.GetInstallment(1), 2, new DateTime(2006, 3, 1), 30, 200, 1000, 170, 30);
            _AssertSpecifiedInstallment(myContract.GetInstallment(2), 3, new DateTime(2006, 4, 3), 30, 200, 800, 0, 0);
            _AssertSpecifiedInstallment(myContract.GetInstallment(3), 4, new DateTime(2006, 5, 1), 30, 200, 600, 0, 0);
            _AssertSpecifiedInstallment(myContract.GetInstallment(4), 5, new DateTime(2006, 6, 1), 30, 200, 400, 0, 0);
            _AssertSpecifiedInstallment(myContract.GetInstallment(5), 6, new DateTime(2006, 7, 3), 30, 200, 200, 0, 0);
        }

        private static void _AssertSpecifiedInstallment(Installment pInstallment,int pNumber, DateTime pExpectedDate,OCurrency pInterestRepayment, OCurrency pCapitalRepayment,OCurrency pOLB, OCurrency pPaidCapital, OCurrency pPaidInterest)
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
        public void Repay_DecliningLoan_KeepNotExpectedInstallment_SecondInstallmentNotEntirlyPaid()
        {
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.CALCULATIONLATEFEESDURINGPUBLICHOLIDAYS, true);

            LoanProduct package = new LoanProduct
            {
                InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
                LoanType = OLoanTypes.DecliningFixedInstallments,
                ChargeInterestWithinGracePeriod = true,
                KeepExpectedInstallment = false,
                Currency = new Currency { Id = 1, UseCents = true},
                RoundingType = ORoundingType.Approximate
            };
            Loan myContract = new Loan(package, 1000, 0.03m, 7, 1, new DateTime(2006, 1, 1), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));

            Assert.AreEqual(1000, _CheckInstalmentsTotalPrincipal(myContract).Value);

            myContract.Repay(1, new DateTime(2006, 2, 1), 30, true, false);
            myContract.Repay(2, new DateTime(2006, 3, 1), 100, true, false);

            _AssertSpecifiedInstallment(myContract.GetInstallment(0), 1, new DateTime(2006, 2, 1), 30, 0, 1000, 0, 30);
            _AssertSpecifiedInstallment(myContract.GetInstallment(1), 2, new DateTime(2006, 3, 1), 30, 154.60m, 1000, 70, 30);
            _AssertSpecifiedInstallment(myContract.GetInstallment(2), 3, new DateTime(2006, 4, 3), 25.36m, 159.24m, 845.40m, 0, 0);
            _AssertSpecifiedInstallment(myContract.GetInstallment(3), 4, new DateTime(2006, 5, 1), 20.58m, 164.02m, 686.16m, 0, 0);
            _AssertSpecifiedInstallment(myContract.GetInstallment(4), 5, new DateTime(2006, 6, 1), 15.66m, 168.93m, 522.14m, 0, 0);
            _AssertSpecifiedInstallment(myContract.GetInstallment(5), 6, new DateTime(2006, 7, 3), 10.60m, 173.99m, 353.21m, 0, 0);
            _AssertSpecifiedInstallment(myContract.GetInstallment(6), 7, new DateTime(2006, 8, 1), 5.38m, 179.22m, 179.22m, 0, 0);
        
        }
        
        [Test]
        public void TestRepayFlateLoanWhenKeepNotExpectedInstallmentButSecondInstallmentNotEntirlyPaidTheSecondInstallment()
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
            Loan myContract = new Loan(package, 1000, 0.03m, 6, 1, new DateTime(2006, 1, 1), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));

            myContract.Repay(1, new DateTime(2006, 2, 1), 30, true, false);
            myContract.Repay(2, new DateTime(2006, 3, 1), 200, true, false);

            Assert.AreEqual(30m, myContract.GetInstallment(0).InterestsRepayment.Value);
            Assert.AreEqual(0m, myContract.GetInstallment(0).CapitalRepayment.Value);
            Assert.AreEqual(1000m, myContract.GetInstallment(0).OLB.Value);
            Assert.AreEqual(0m, myContract.GetInstallment(0).PaidCapital.Value);
            Assert.AreEqual(30m, myContract.GetInstallment(0).PaidInterests.Value);

            Assert.AreEqual(30m, myContract.GetInstallment(1).InterestsRepayment.Value);
            Assert.AreEqual(200m, myContract.GetInstallment(1).CapitalRepayment.Value);
            Assert.AreEqual(1000m, myContract.GetInstallment(1).OLB.Value);
            Assert.AreEqual(170m, myContract.GetInstallment(1).PaidCapital.Value);
            Assert.AreEqual(30m, myContract.GetInstallment(1).PaidInterests.Value);

            Assert.AreEqual(30m, myContract.GetInstallment(2).InterestsRepayment.Value);
            Assert.AreEqual(200m, myContract.GetInstallment(2).CapitalRepayment.Value);
            Assert.AreEqual(800m, myContract.GetInstallment(2).OLB.Value);
            Assert.AreEqual(0m, myContract.GetInstallment(2).PaidCapital.Value);
            Assert.AreEqual(0m, myContract.GetInstallment(2).PaidInterests.Value);
        }

	
	
        [Test]
        public void TestRepayDecliningLoanWhenNotExpectedInstallmentWith5OfGracePeriodButCancelFees()
        {
            Assert.Ignore();
            //LoanProduct package = new LoanProduct
            //                          {
            //                              InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
            //                              LoanType = OLoanTypes.DecliningFixedInstallments,
            //                              ChargeInterestWithinGracePeriod = true,
            //                              Currency = new Currency { Id = 1 }
            //                          };
            //package.KeepExpectedInstallment = false;
            //Loan myContract = new Loan(package, 600, 0.02, 6, 5, new DateTime(2006, 1, 1), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisioningTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));

            //RepaymentEvent rE = myContract.Repay(1,new DateTime(2006,2,1),13,true,false);
            //Assert.AreEqual(0m,rE.Fees.Value);
            //Assert.AreEqual(12m,rE.Interests.Value);
            //Assert.AreEqual(1m,rE.Principal.Value);

            //Assert.AreEqual(12m,myContract.GetInstallment(0).InterestsRepayment.Value);
            //Assert.AreEqual(0m,myContract.GetInstallment(0).CapitalRepayment.Value);
            //Assert.AreEqual(600m,myContract.GetInstallment(0).OLB.Value);
            //Assert.AreEqual(0m,myContract.GetInstallment(0).PaidCapital.Value);
            //Assert.AreEqual(12m,myContract.GetInstallment(0).PaidInterests.Value);

            //Assert.AreEqual(11.98m,myContract.GetInstallment(1).InterestsRepayment.Value);
            //Assert.AreEqual(0m,myContract.GetInstallment(1).CapitalRepayment.Value);
            //Assert.AreEqual(599m,myContract.GetInstallment(1).OLB.Value);
            //Assert.AreEqual(0m,myContract.GetInstallment(1).PaidCapital.Value);
            //Assert.AreEqual(0m,myContract.GetInstallment(1).PaidInterests.Value);

            //Assert.AreEqual(11.98m,myContract.GetInstallment(2).InterestsRepayment.Value);
            //Assert.AreEqual(0m,myContract.GetInstallment(2).CapitalRepayment.Value);
            //Assert.AreEqual(599m,myContract.GetInstallment(2).OLB.Value);
            //Assert.AreEqual(0m,myContract.GetInstallment(2).PaidCapital.Value);
            //Assert.AreEqual(0m,myContract.GetInstallment(2).PaidInterests.Value);

            //Assert.AreEqual(11.98m,myContract.GetInstallment(3).InterestsRepayment.Value);
            //Assert.AreEqual(0m,myContract.GetInstallment(3).CapitalRepayment.Value);
            //Assert.AreEqual(599m,myContract.GetInstallment(3).OLB.Value);
            //Assert.AreEqual(0m,myContract.GetInstallment(3).PaidCapital.Value);
            //Assert.AreEqual(0m,myContract.GetInstallment(3).PaidInterests.Value);

            //Assert.AreEqual(11.98m,myContract.GetInstallment(4).InterestsRepayment.Value);
            //Assert.AreEqual(0m,myContract.GetInstallment(4).CapitalRepayment.Value);
            //Assert.AreEqual(599m,myContract.GetInstallment(4).OLB.Value);
            //Assert.AreEqual(0m,myContract.GetInstallment(4).PaidCapital.Value);
            //Assert.AreEqual(0m,myContract.GetInstallment(4).PaidInterests.Value);

            //Assert.AreEqual(11.98m,myContract.GetInstallment(5).InterestsRepayment.Value);
            //Assert.AreEqual(599m,myContract.GetInstallment(5).CapitalRepayment.Value);
            //Assert.AreEqual(599m,myContract.GetInstallment(5).OLB.Value);
            //Assert.AreEqual(0m,myContract.GetInstallment(5).PaidCapital.Value);
            //Assert.AreEqual(0m,myContract.GetInstallment(5).PaidInterests.Value);
        }

        [Test]
        public void Repay_DecliningLoan_KeepNotExpectedInstallment_EntirlyPaid_NoCancelFees()
        {
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.CALCULATIONLATEFEESDURINGPUBLICHOLIDAYS, true);
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.ACCOUNTINGPROCESS, OAccountingProcesses.Accrual);

            LoanProduct package = new LoanProduct
            {
                InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
                LoanType = OLoanTypes.DecliningFixedInstallments,
                ChargeInterestWithinGracePeriod = true,
                KeepExpectedInstallment = false,
                Currency = new Currency { Id = 1, UseCents = true},
                RoundingType = ORoundingType.Approximate
            };
            Loan myContract = new Loan(package, 1000, 0.03m, 6, 1, new DateTime(2006, 1, 1), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()))
                                  {
                                      NonRepaymentPenalties = { InitialAmount = 0.003 },
                                      AnticipatedTotalRepaymentPenalties = 0.003
                                  };
            //VPM => 218,3546
            myContract.Repay(1, new DateTime(2006, 2, 1), 30, true, false);
            RepaymentEvent repayEvent = myContract.Repay(2, new DateTime(2006, 3, 1), 1032.44m, false, false);

            _AssertSpecifiedEvent(repayEvent, 2.44m, 30.00m, 1000.00m);

            _AssertSpecifiedInstallment(myContract.GetInstallment(0), 1, new DateTime(2006, 2, 1), 30.00m, 0, 1000, 0, 30);
            _AssertSpecifiedInstallment(myContract.GetInstallment(1), 2, new DateTime(2006, 3, 1), 30.00m, 188.35m, 1000, 188.35m, 30);
            _AssertSpecifiedInstallment(myContract.GetInstallment(2), 3, new DateTime(2006, 4, 3), 0, 811.65m, 811.65m, 811.65m, 0);
            _AssertSpecifiedInstallment(myContract.GetInstallment(3), 4, new DateTime(2006, 5, 1), 0, 0, 0, 0, 0);
            _AssertSpecifiedInstallment(myContract.GetInstallment(4), 5, new DateTime(2006, 6, 1), 0, 0, 0, 0, 0);
            _AssertSpecifiedInstallment(myContract.GetInstallment(5), 6, new DateTime(2006, 7, 3), 0, 0, 0, 0, 0);
        }

        private static void _AssertSpecifiedEvent(RepaymentEvent pEvent, OCurrency pFees, OCurrency pInterest, OCurrency pCapital)
        {
            Assert.AreEqual(pFees, pEvent.Fees.Value);
            Assert.AreEqual(pInterest, pEvent.Interests.Value);
            Assert.AreEqual(pCapital, pEvent.Principal.Value);
        }


        [Test]
        public void TestRepayFlateLoanWhenKeepNotExpectedInstallmentButEntirlyPaidButNoCancelFees()
        {
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.CALCULATIONLATEFEESDURINGPUBLICHOLIDAYS, true);
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.ACCOUNTINGPROCESS, OAccountingProcesses.Accrual);
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.INTEREST_RATE_DECIMAL_PLACES, 2);


            LoanProduct package = new LoanProduct
                                      {
                                          InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
                                          LoanType = OLoanTypes.Flat,
                                          ChargeInterestWithinGracePeriod = true,
                                          Currency = new Currency {Id = 1, UseCents = true},
                                          KeepExpectedInstallment = false
                                      };
            Loan myContract = new Loan(package, 1000, 0.03m, 6, 1, new DateTime(2006, 1, 1), new User(),
                                       ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""),
                                       ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()))
                                  {
                                      NonRepaymentPenalties = {InitialAmount = 0.003},
                                      AnticipatedTotalRepaymentPenalties = 0.003
                                  };

            myContract.Repay(1, new DateTime(2006, 2, 1), 30, true, false);
            RepaymentEvent repayEvent = myContract.Repay(2, new DateTime(2006, 3, 1), 1032.4m, false, false);

            Assert.AreEqual(2.4m, repayEvent.Fees.Value);
            Assert.AreEqual(30, repayEvent.Interests.Value);
            Assert.AreEqual(1000, repayEvent.Principal.Value);

            Assert.AreEqual(30, myContract.GetInstallment(0).InterestsRepayment.Value);
            Assert.AreEqual(0, myContract.GetInstallment(0).CapitalRepayment.Value);
            Assert.AreEqual(1000, myContract.GetInstallment(0).OLB.Value);
            Assert.AreEqual(0, myContract.GetInstallment(0).PaidCapital.Value);
            Assert.AreEqual(30, myContract.GetInstallment(0).PaidInterests.Value);

            Assert.AreEqual(30, myContract.GetInstallment(1).InterestsRepayment.Value);
            Assert.AreEqual(200, myContract.GetInstallment(1).CapitalRepayment.Value);
            Assert.AreEqual(1000, myContract.GetInstallment(1).OLB.Value);
            Assert.AreEqual(200, myContract.GetInstallment(1).PaidCapital.Value);
            Assert.AreEqual(30, myContract.GetInstallment(1).PaidInterests.Value);

            Assert.AreEqual(0, myContract.GetInstallment(2).InterestsRepayment.Value);
            Assert.AreEqual(800, myContract.GetInstallment(2).CapitalRepayment.Value);
            Assert.AreEqual(800, myContract.GetInstallment(2).OLB.Value);
            Assert.AreEqual(800, myContract.GetInstallment(2).PaidCapital.Value);
            Assert.AreEqual(0, myContract.GetInstallment(2).PaidInterests.Value);

            Assert.AreEqual(0, myContract.GetInstallment(3).InterestsRepayment.Value);
            Assert.AreEqual(0, myContract.GetInstallment(3).CapitalRepayment.Value);
            Assert.AreEqual(0, myContract.GetInstallment(3).OLB.Value);
            Assert.AreEqual(0, myContract.GetInstallment(3).PaidCapital.Value);
            Assert.AreEqual(0, myContract.GetInstallment(3).PaidInterests.Value);

            Assert.AreEqual(0, myContract.GetInstallment(4).InterestsRepayment.Value);
            Assert.AreEqual(0, myContract.GetInstallment(4).CapitalRepayment.Value);
            Assert.AreEqual(0, myContract.GetInstallment(4).OLB.Value);
            Assert.AreEqual(0, myContract.GetInstallment(4).PaidCapital.Value);
            Assert.AreEqual(0, myContract.GetInstallment(4).PaidInterests.Value);

            Assert.AreEqual(0, myContract.GetInstallment(5).InterestsRepayment.Value);
            Assert.AreEqual(0, myContract.GetInstallment(5).CapitalRepayment.Value);
            Assert.AreEqual(0, myContract.GetInstallment(5).OLB.Value);
            Assert.AreEqual(0, myContract.GetInstallment(5).PaidCapital.Value);
            Assert.AreEqual(0, myContract.GetInstallment(5).PaidInterests.Value);
        }		

        [Test]
        public void TestRepayFlateLoanWhenKeepNotExpectedInstallmentButEntirlyPaidButCancelFees()
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
            Loan myContract = new Loan(package, 1000, 0.03m, 6, 1, new DateTime(2006, 1, 1), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));

            myContract.Repay(1, new DateTime(2006, 2, 1), 30, true, false);
            myContract.Repay(2, new DateTime(2006, 3, 1), 1030, true, false);

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

            Assert.AreEqual(0m, myContract.GetInstallment(2).InterestsRepayment.Value);
            Assert.AreEqual(800m, myContract.GetInstallment(2).CapitalRepayment.Value);
            Assert.AreEqual(800m, myContract.GetInstallment(2).OLB.Value);
            Assert.AreEqual(800m, myContract.GetInstallment(2).PaidCapital.Value);
            Assert.AreEqual(0m, myContract.GetInstallment(2).PaidInterests.Value);

            Assert.AreEqual(0m, myContract.GetInstallment(3).InterestsRepayment.Value);
            Assert.AreEqual(0m, myContract.GetInstallment(3).CapitalRepayment.Value);
            Assert.AreEqual(0m, myContract.GetInstallment(3).OLB.Value);
            Assert.AreEqual(0m, myContract.GetInstallment(3).PaidCapital.Value);
            Assert.AreEqual(0m, myContract.GetInstallment(3).PaidInterests.Value);

            Assert.AreEqual(0m, myContract.GetInstallment(4).InterestsRepayment.Value);
            Assert.AreEqual(0m, myContract.GetInstallment(4).CapitalRepayment.Value);
            Assert.AreEqual(0m, myContract.GetInstallment(4).OLB.Value);
            Assert.AreEqual(0m, myContract.GetInstallment(4).PaidCapital.Value);
            Assert.AreEqual(0m, myContract.GetInstallment(4).PaidInterests.Value);

            Assert.AreEqual(0m, myContract.GetInstallment(5).InterestsRepayment.Value);
            Assert.AreEqual(0, myContract.GetInstallment(5).CapitalRepayment.Value);
            Assert.AreEqual(0, myContract.GetInstallment(5).OLB.Value);
            Assert.AreEqual(0, myContract.GetInstallment(5).PaidCapital.Value);
            Assert.AreEqual(0, myContract.GetInstallment(5).PaidInterests.Value);
        }

        [Test]
        public void TestRepayFlateLoanWhenKeepExpectedInstallmentAndOverPaid()
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
            Loan myContract = new Loan(package,1000,0.03m,6,1,new DateTime(2006,1,1), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));

            myContract.Repay(1,new DateTime(2006,2,1),30,true,true);
            myContract.Repay(2,new DateTime(2006,3,1),550,true,true);

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
            Assert.AreEqual(60m, myContract.GetInstallment(3).PaidCapital.Value);
            Assert.AreEqual(30m, myContract.GetInstallment(3).PaidInterests.Value);

            Assert.AreEqual(30m, myContract.GetInstallment(4).InterestsRepayment.Value);
            Assert.AreEqual(200m, myContract.GetInstallment(4).CapitalRepayment.Value);
            Assert.AreEqual(400m, myContract.GetInstallment(4).OLB.Value);
            Assert.AreEqual(0m, myContract.GetInstallment(4).PaidCapital.Value);
            Assert.AreEqual(0m, myContract.GetInstallment(4).PaidInterests.Value);

            Assert.AreEqual(30m, myContract.GetInstallment(5).InterestsRepayment.Value);
            Assert.AreEqual(200m, myContract.GetInstallment(5).CapitalRepayment.Value);
            Assert.AreEqual(200m, myContract.GetInstallment(5).OLB.Value);
            Assert.AreEqual(0m, myContract.GetInstallment(5).PaidCapital.Value);
            Assert.AreEqual(0m,myContract.GetInstallment(5).PaidInterests.Value);
        }

        [Test]
        public void TestRepayFlateLoanWhenKeepNotExpectedInstallmentAndOverPaid()
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
            Loan myContract = new Loan(package, 1000, 0.03m, 6, 1, new DateTime(2006, 1, 1), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));

            myContract.Repay(1, new DateTime(2006, 2, 1), 30, true, false);
            RepaymentEvent rE = myContract.Repay(2, new DateTime(2006, 3, 1), 430, true, false);

            Assert.AreEqual(0, rE.Fees.Value);
            Assert.AreEqual(30, rE.Interests.Value);
            Assert.AreEqual(400, rE.Principal.Value);

            _AssertSpecifiedInstallment(myContract.GetInstallment(0), 30, 0, 1000, 0, 30);
            _AssertSpecifiedInstallment(myContract.GetInstallment(1), 30, 200, 1000, 200, 30);
            _AssertSpecifiedInstallment(myContract.GetInstallment(2), 18, 150, 600, 0, 0);
            _AssertSpecifiedInstallment(myContract.GetInstallment(3), 18, 150, 450, 0, 0);
            _AssertSpecifiedInstallment(myContract.GetInstallment(4), 18, 150, 300, 0, 0);
            _AssertSpecifiedInstallment(myContract.GetInstallment(5), 18, 150, 150, 0, 0);
        }

        [Test]
        public void TestCalculateActualInterestsToPay()
        {
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.CALCULATIONLATEFEESDURINGPUBLICHOLIDAYS, true);

            LoanProduct package = new LoanProduct
                                      {
                                          InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
                                          LoanType = OLoanTypes.Flat,
                                          ChargeInterestWithinGracePeriod = true,
                                          Currency = new Currency { Id = 1 }
                                      };
            Loan myContract = new Loan(package, 1000, 0.03m, 6, 1, new DateTime(2006, 6, 30), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));
            myContract.Repay(1, new DateTime(2006, 7, 31), 30m, true, false);
            myContract.Repay(2, new DateTime(2006, 8, 30), 230m, true, false);

            Assert.AreEqual(120m, myContract.CalculateActualInterestsToPay().Value);
        }

        [Test]
        public void TestCalculateTotalInterests()
        {
            LoanProduct package = new LoanProduct
                                      {
                                          InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
                                          LoanType = OLoanTypes.Flat,
                                          ChargeInterestWithinGracePeriod = true,
                                          Currency = new Currency { Id = 1 }
                                      };
            Loan myContract = new Loan(package,1000,0.03m,6,1,new DateTime(2006,6,30), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));
            Assert.AreEqual(181m, myContract.CalculateTotalInterests().Value);
        }

        [Test]
        public void TestCalculateTotalExpectedAmountWithoutEntryFees()
        {
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.CALCULATIONLATEFEESDURINGPUBLICHOLIDAYS, true);

            LoanProduct package = new LoanProduct
                                      {
                                          InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
                                          LoanType = OLoanTypes.Flat,
                                          ChargeInterestWithinGracePeriod = true,
                                          Currency = new Currency { Id = 1 }
                                      };
            Loan myContract = new Loan(package, 1000, 0.03m, 6, 1, new DateTime(2006, 6, 30), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));

            Assert.AreEqual(1181m, myContract.CalculateTotalExpectedAmount().Value);
        }

        [Test]
        public void TestDisburseWhenDisburseDateGreaterThanContractStartDate()
        {
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.CALCULATIONLATEFEESDURINGPUBLICHOLIDAYS, true);
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.INCREMENTALDURINGDAYOFF, true);
            //startdate = 1/1/2006
            //expectedFirstInstallment = 1/2/2006
            //real startdate = 6/1/2006
            LoanProduct package = new LoanProduct
                                      {
                                          InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
                                          LoanType = OLoanTypes.Flat,
                                          ChargeInterestWithinGracePeriod = true,
                                          Currency = new Currency { Id = 1 }
                                      };
            EntryFee productEntryFee = new EntryFee();
            productEntryFee.IsRate = true;
            productEntryFee.Value = 5;//5% of an amount
            productEntryFee.Id = 21;
            productEntryFee.Name = "Test entry fee";
            package.EntryFees = new List<EntryFee>();
            package.EntryFees.Add(productEntryFee);

            Loan myContract = new Loan( 
                                        package,
                                        1000,
                                        0.03m,
                                        6,
                                        1,
                                        new DateTime(2006,1,1),
                                        new User(),
                                        ApplicationSettings.GetInstance(""),
                                        NonWorkingDateSingleton.GetInstance(""),
                                        ProvisionTable.GetInstance(new User()),
                                        ChartOfAccounts.GetInstance(new User())
                                        );
            
            LoanEntryFee fee = new LoanEntryFee();
            fee.ProductEntryFee = package.EntryFees[0];
            fee.FeeValue = (decimal)fee.ProductEntryFee.Value;
            fee.ProductEntryFeeId = (int) package.EntryFees[0].Id;
            myContract.LoanEntryFeesList=new List<LoanEntryFee>();
            myContract.LoanEntryFeesList.Add(fee);

            LoanDisbursmentEvent lDE = myContract.Disburse(new DateTime(2006,1,6),false,false);
            Assert.AreEqual(new DateTime(2006,1,1),myContract.StartDate);
            Assert.AreEqual(25m,myContract.GetInstallment(0).InterestsRepayment.Value);
            Assert.AreEqual(1000m,lDE.Amount.Value);
            Assert.AreEqual(50m, lDE.Commissions[0].Fee.Value);
        }

        [Test]
        public void TestDisburseWhenDisburseDateGreaterThanContractStartDateButDisburseDateDayLowerThanStartDateDay()
        {
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.CALCULATIONLATEFEESDURINGPUBLICHOLIDAYS, true);

            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.INCREMENTALDURINGDAYOFF, true);
            LoanProduct package = new LoanProduct
                                      {
                                          InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
                                          LoanType = OLoanTypes.Flat,
                                          ChargeInterestWithinGracePeriod = true,
                                          Currency = new Currency { Id = 1 }
                                      };
            Loan myContract = new Loan(
                                            package,
                                            1000,
                                            0.03m,
                                            6,
                                            1,
                                            new DateTime(2006,1,20),
                                            new User(),
                                            ApplicationSettings.GetInstance(""),
                                            NonWorkingDateSingleton.GetInstance(""),
                                            ProvisionTable.GetInstance(new User()), 
                                            ChartOfAccounts.GetInstance(new User())
                                        );
            EntryFee entryFee = new EntryFee();
            entryFee.Value = 5;
            entryFee.IsRate = true;
            entryFee.Id = 21;
            LoanEntryFee loanEntryFee = new LoanEntryFee();
            loanEntryFee.ProductEntryFee = entryFee;
            loanEntryFee.FeeValue = 5;
            loanEntryFee.ProductEntryFeeId = 21;
            myContract.LoanEntryFeesList = new List<LoanEntryFee>();
            myContract.LoanEntryFeesList.Add(loanEntryFee);
            LoanDisbursmentEvent lDE = myContract.Disburse(new DateTime(2006,2,1),false,false);
            Assert.AreEqual(new DateTime(2006,1,20),myContract.StartDate);
            Assert.AreEqual(18m,myContract.GetInstallment(0).InterestsRepayment.Value);
            Assert.AreEqual(1000m,lDE.Amount.Value);
            Assert.AreEqual(50m,lDE.Commissions[0].Fee.Value);
        }

        [Test]
        public void TestDisburseWhenDisburseDateGreaterThanContractStartDateNotInSameMonthThanFirstIntallmentDate()
        {
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.CALCULATIONLATEFEESDURINGPUBLICHOLIDAYS, true);
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.INCREMENTALDURINGDAYOFF, true);

            LoanProduct package = new LoanProduct
                                      {
                                          InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
                                          LoanType = OLoanTypes.Flat,
                                          ChargeInterestWithinGracePeriod = true,
                                          Currency = new Currency { Id = 1 }
                                      };
            Loan myContract = new Loan(package,1000,0.03m,6,1,new DateTime(2006,1,20), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));
            EntryFee entryFee = new EntryFee();
            entryFee.Value = 5;
            entryFee.IsRate = true;
            entryFee.Id = 21;
            
            LoanEntryFee loanEntryFee = new LoanEntryFee();
            loanEntryFee.FeeValue = 5;
            loanEntryFee.ProductEntryFee = entryFee;
            loanEntryFee.ProductEntryFeeId = 21;
            myContract.LoanEntryFeesList = new List<LoanEntryFee>();
            myContract.LoanEntryFeesList.Add(loanEntryFee);

            LoanDisbursmentEvent lDE = myContract.Disburse(new DateTime(2006,1,25),false,false);
            Assert.AreEqual(new DateTime(2006,1,20),myContract.StartDate);
            Assert.AreEqual(25m,myContract.GetInstallment(0).InterestsRepayment.Value);
            Assert.AreEqual(1000m,lDE.Amount.Value);
            Assert.AreEqual(50m,lDE.Commissions[0].Fee.Value);
        }

        [Test]
        public void TestDisburseWhenDisburseDateEqualsToContractStartDate()
        {
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.INCREMENTALDURINGDAYOFF, true);
            LoanProduct package = new LoanProduct
                                      {
                                          InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
                                          LoanType = OLoanTypes.Flat,
                                          ChargeInterestWithinGracePeriod = true,
                                          Currency = new Currency { Id = 1 }
                                      };
            Loan myContract = new Loan(package,1000,0.03m,6,1,new DateTime(2006,1,1), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));
            EntryFee entryFee = new EntryFee();
            entryFee.Value = 5;
            entryFee.IsRate = true;
            entryFee.Id = 21;

            LoanEntryFee loanEntryFee = new LoanEntryFee();
            loanEntryFee.FeeValue = 5;
            loanEntryFee.ProductEntryFee = entryFee;
            loanEntryFee.ProductEntryFeeId = 21;
            myContract.LoanEntryFeesList = new List<LoanEntryFee>();
            myContract.LoanEntryFeesList.Add(loanEntryFee);

            LoanDisbursmentEvent lDE = myContract.Disburse(new DateTime(2006,1,1),false,false);
            Assert.AreEqual(new DateTime(2006,1,1),myContract.StartDate);
            Assert.AreEqual(30m,myContract.GetInstallment(0).InterestsRepayment.Value);
            Assert.AreEqual(1000m,lDE.Amount.Value);
            Assert.AreEqual(50m,lDE.Commissions[0].Fee.Value);
        }

        [Test]
        public void TestDisburseWhenDisburseDateLowerThanContractStartDate()
        {
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.CALCULATIONLATEFEESDURINGPUBLICHOLIDAYS, true);
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.INCREMENTALDURINGDAYOFF, true);

            LoanProduct package = new LoanProduct
                                      {
                                          InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
                                          LoanType = OLoanTypes.Flat,
                                          ChargeInterestWithinGracePeriod = true,
                                          Currency = new Currency { Id = 1 }
                                      };
            Loan myContract = new Loan(package,1000,0.03m,6,1,new DateTime(2006,2,1), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));
            Assert.AreEqual(new DateTime(2006,2,1),myContract.StartDate);

            myContract.Disburse(new DateTime(2005,12,25),false,false);
            Assert.AreEqual(new DateTime(2006,2,1),myContract.StartDate);
            Assert.AreEqual(68m,myContract.GetInstallment(0).InterestsRepayment.Value);
        }

        [Test]
        public void Disburse_DisburseDateGreaterThanContractFirstInstallmentDate_NoAlignDates()
        {
            LoanProduct package = new LoanProduct{
                                                     InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
                                                     LoanType = OLoanTypes.Flat,
                                                     ChargeInterestWithinGracePeriod = true,
                                                     Currency = new Currency { Id = 1 }
                                                 };
            Loan myContract = new Loan(package, 750, 0.03m,6,1,new DateTime(2006,1,1), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));
            LoanDisbursmentEvent lDE = myContract.Disburse(new DateTime(2006,3,1),false,false);
			
            Assert.IsNotNull(lDE);
            Assert.AreEqual(new DateTime(2006,1,1),myContract.StartDate);
            Assert.AreEqual(0m,myContract.GetInstallment(0).InterestsRepayment.Value);
        }

        [Test]
        public void TestDisburseWhenBiWeeklyInstallmentType()
        {
            LoanProduct package = new LoanProduct
                                      {
                                          InstallmentType = new InstallmentType(1, "Bi-Weelky", 14, 0),
                                          LoanType = OLoanTypes.Flat,
                                          ChargeInterestWithinGracePeriod = true,
                                          Currency = new Currency { Id = 1 }
                                      };
            Loan myContract = new Loan(package, 1000, 0.02m, 6, 1, new DateTime(2006, 1, 2), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));
            
            EntryFee entryFee = new EntryFee();
            entryFee.Value = 5;
            entryFee.IsRate = true;
            entryFee.Id = 21;

            LoanEntryFee loanEntryFee = new LoanEntryFee();
            loanEntryFee.FeeValue = 5;
            loanEntryFee.ProductEntryFee = entryFee;
            loanEntryFee.ProductEntryFeeId = 21;
            myContract.LoanEntryFeesList = new List<LoanEntryFee>();
            myContract.LoanEntryFeesList.Add(loanEntryFee);
            
            LoanDisbursmentEvent lDE = myContract.Disburse(new DateTime(2006, 1, 6), false, false);

            Assert.AreEqual(new DateTime(2006,1,2), myContract.StartDate);
            Assert.AreEqual(14.29m, myContract.GetInstallment(0).InterestsRepayment.Value);
            Assert.AreEqual(1000m, lDE.Amount.Value);
            Assert.AreEqual(50m, lDE.Commissions[0].Fee.Value);
        }

        [Test]
        public void TestDisburseWhenMonthlyInstallmentTypeAndNotAlignWithRealDisburseDate()
        {
            ApplicationSettings pDataParam = ApplicationSettings.GetInstance("");
            pDataParam.DeleteAllParameters();
            pDataParam.AddParameter("ALIGN_INSTALLMENTS_ON_REAL_DISBURSEMENT_DATE", false);
            pDataParam.AddParameter("PAY_FIRST_INSTALLMENT_REAL_VALUE", true);
            pDataParam.AddParameter(OGeneralSettings.DONOTSKIPWEEKENDSININSTALLMENTSDATE, false);
            pDataParam.AddParameter(OGeneralSettings.INCREMENTALDURINGDAYOFF, true);

            LoanProduct package = new LoanProduct
                                      {
                                          InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
                                          LoanType = OLoanTypes.Flat,
                                          ChargeInterestWithinGracePeriod = true,
                                          Currency = new Currency { Id = 1 }
                                      };
            Loan myContract = new Loan(package, 1000, 0.03m, 6, 1, new DateTime(2006, 1, 2), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));
            myContract.Disburse(new DateTime(2006,1,6),false,true);
            Assert.AreEqual(26m,myContract.GetInstallment(0).InterestsRepayment.Value);
        }

        [Test]
        public void TestDisburseWhenMontlyInstallmentTypeAndAlignDateWithRealDisbuseDate()
        {
            ApplicationSettings pDataParam = ApplicationSettings.GetInstance("");
            pDataParam.DeleteAllParameters();
            pDataParam.AddParameter("ALIGN_INSTALLMENTS_ON_REAL_DISBURSEMENT_DATE", true);
            pDataParam.AddParameter(OGeneralSettings.DONOTSKIPWEEKENDSININSTALLMENTSDATE, false);
            pDataParam.AddParameter(OGeneralSettings.INCREMENTALDURINGDAYOFF, true);
            pDataParam.AddParameter(OGeneralSettings.PAYFIRSTINSTALLMENTREALVALUE, true);

            LoanProduct package = new LoanProduct
                                      {
                                          InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
                                          LoanType = OLoanTypes.Flat,
                                          ChargeInterestWithinGracePeriod = true,
                                          Currency = new Currency { Id = 1 }
                                      };
            Loan myContract = new Loan(package,1000,0.02m,6,1,new DateTime(2006,1,2), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));
            EntryFee entryFee = new EntryFee();
            entryFee.Value = 5;
            entryFee.IsRate = true;
            entryFee.Id = 21;

            LoanEntryFee loanEntryFee = new LoanEntryFee();
            loanEntryFee.FeeValue = 5;
            loanEntryFee.ProductEntryFee = entryFee;
            loanEntryFee.ProductEntryFeeId = 21;
            myContract.LoanEntryFeesList = new List<LoanEntryFee>();
            myContract.LoanEntryFeesList.Add(loanEntryFee);

            LoanDisbursmentEvent lDE = myContract.Disburse(new DateTime(2006,1,2),true,false);
            Assert.AreEqual(new DateTime(2006, 1, 2), myContract.StartDate);
            Assert.AreEqual(20m,myContract.GetInstallment(0).InterestsRepayment.Value);
            Assert.AreEqual(new DateTime(2006,2,2),myContract.GetInstallment(0).ExpectedDate);
            Assert.AreEqual(1000m,lDE.Amount.Value);
            Assert.AreEqual(50m, lDE.Commissions[0].Fee.Value);
        }

        [Test]
        public void TestDisburseWhenDisableFeesTrue()
        {
            ApplicationSettings pDataParam = ApplicationSettings.GetInstance("");
            pDataParam.DeleteAllParameters();
            pDataParam.AddParameter("ALIGN_INSTALLMENTS_ON_REAL_DISBURSEMENT_DATE", false);
            pDataParam.AddParameter("PAY_FIRST_INSTALLMENT_REAL_VALUE", true);
            pDataParam.AddParameter(OGeneralSettings.DONOTSKIPWEEKENDSININSTALLMENTSDATE, false);
            pDataParam.AddParameter(OGeneralSettings.INCREMENTALDURINGDAYOFF, true);

            LoanProduct package = new LoanProduct
                                      {
                                          InstallmentType = new InstallmentType(1, "Bi-Weelky", 14, 0),
                                          LoanType = OLoanTypes.Flat,
                                          ChargeInterestWithinGracePeriod = true,
                                          Currency = new Currency { Id = 1 }
                                      };
            Loan myContract = new Loan(package,1000,0.02m,6,1,new DateTime(2006,1,2), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));
            EntryFee productFee = new EntryFee();
            productFee.Value = 5;
            productFee.IsRate = true;

            LoanEntryFee entryFee = new LoanEntryFee();
            entryFee.FeeValue = 5;
            entryFee.ProductEntryFee = productFee;
            myContract.LoanEntryFeesList = new List<LoanEntryFee>();
            myContract.LoanEntryFeesList.Add(entryFee);

            LoanDisbursmentEvent lDE = myContract.Disburse(new DateTime(2006,1,6),false,true);
            Assert.AreEqual(new DateTime(2006,1,2),myContract.StartDate);
            Assert.AreEqual(14.29m,myContract.GetInstallment(0).InterestsRepayment.Value);
            Assert.AreEqual(1000m,lDE.Amount.Value);
            Assert.AreEqual(null, lDE.Commissions);
        }

        [Test]
        public void TestCalculateInstallmentWhenChangeDateSetToFalse()
        {
            ApplicationSettings pDataParam = ApplicationSettings.GetInstance("");
            pDataParam.DeleteAllParameters();
            pDataParam.AddParameter("ALIGN_INSTALLMENTS_ON_REAL_DISBURSEMENT_DATE", false);
            pDataParam.AddParameter("PAY_FIRST_INSTALLMENT_REAL_VALUE", true);
            pDataParam.AddParameter(OGeneralSettings.CALCULATIONLATEFEESDURINGPUBLICHOLIDAYS, true);
            pDataParam.AddParameter(OGeneralSettings.DONOTSKIPWEEKENDSININSTALLMENTSDATE, false);
            pDataParam.AddParameter(OGeneralSettings.INCREMENTALDURINGDAYOFF, true);

            LoanProduct package = new LoanProduct
                                      {
                                          InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
                                          LoanType = OLoanTypes.Flat,
                                          ChargeInterestWithinGracePeriod = true,
                                          Currency = new Currency { Id = 1 }
                                      };
            Loan myContract = new Loan(package,1000,0.02m,6,1,new DateTime(2006,1,2), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));

            Assert.AreEqual(new DateTime(2006,2,2),myContract.GetInstallment(0).ExpectedDate);
            Assert.AreEqual(new DateTime(2006,3,2),myContract.GetInstallment(1).ExpectedDate);

            myContract.Disburse(new DateTime(2006,1,6),false,false);
			
            Assert.AreEqual(new DateTime(2006,1,2),myContract.StartDate);
            Assert.AreEqual(new DateTime(2006,2,2),myContract.GetInstallment(0).ExpectedDate);
            Assert.AreEqual(new DateTime(2006,3,2),myContract.GetInstallment(1).ExpectedDate);

            myContract.Repay(1,new DateTime(2006,2,2),100,true,false);
            Assert.AreEqual(new DateTime(2006,2,2),myContract.GetInstallment(0).ExpectedDate);
            Assert.AreEqual(new DateTime(2006,3,2),myContract.GetInstallment(1).ExpectedDate);			
        }

        [Test]
        public void TestCalculateRemainingInterests()
        {
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.CALCULATIONLATEFEESDURINGPUBLICHOLIDAYS, true);

            LoanProduct package = new LoanProduct
                                      {
                                          InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
                                          LoanType = OLoanTypes.Flat,
                                          ChargeInterestWithinGracePeriod = true,
                                          Currency = new Currency { Id = 1 }
                                      };

            Loan myContract = new Loan(package, 1000, 0.02m, 6, 1, new DateTime(2006, 1, 2), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));

            myContract.Repay(1, new DateTime(2006, 2, 2), 20, false, false);

            Assert.AreEqual(100m, myContract.CalculateRemainingInterests().Value);
        }

        [Test]
        public void Copy_NoChange()
        {
            Loan loan = new Loan
                            {
                                Code = "Code",
                                Amount = 1000,
                                InterestRate = 0.3m,
                                ClientType = OClientTypes.Group,
                                InstallmentList = new List<Installment>
                                                      {
                                                          new Installment{Number = 1,CapitalRepayment = 100,InterestsRepayment = 2,ExpectedDate = new DateTime(2007, 1, 1),OLB = 10},
                                                          new Installment{Number = 2,CapitalRepayment = 222,InterestsRepayment = 24,ExpectedDate = new DateTime(2007, 2, 1),OLB = 100}
                                                      },
                                Events = new EventStock
                                             {
                                                 new RepaymentEvent{InstallmentNumber = 1,Principal = 200,Interests = 100,Commissions = 0, Penalties = 0,ClientType = OClientTypes.Group},
//                                                 new LoanDisbursmentEvent{Date = TimeProvider.Today, Amount = 100, Commission = 0, ClientType = OClientTypes.Group }
                                             }
                            };
            
            Loan cloneLoan = loan.Copy();

            Assert.AreEqual("Code", cloneLoan.Code);
            Assert.AreEqual(1000, cloneLoan.Amount.Value);
            Assert.AreEqual(0.3, cloneLoan.InterestRate);
            Assert.AreEqual(OClientTypes.Group, cloneLoan.ClientType);

            _AssertSpecifiedInstallment(cloneLoan.GetInstallment(0), 1, new DateTime(2007, 1, 1), 2, 100, 10, 0, 0);
            _AssertSpecifiedInstallment(cloneLoan.GetInstallment(1), 2, new DateTime(2007, 2, 1), 24, 222, 100, 0, 0);
        }

        [Test]
        public void TestMethodeCopyCredit()
        {
            LoanProduct package = new LoanProduct
                                      {
                                          InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
                                          LoanType = OLoanTypes.DecliningFixedInstallments,
                                          ChargeInterestWithinGracePeriod = true,
                                          Currency = new Currency { Id = 1 }
                                      };
            package.KeepExpectedInstallment = false;
            Loan contractSource = new Loan(package,1500,0.03m,9,1,new DateTime(2006,1,2), 
                new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), 
                ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));
            RepaymentEvent rE1 = new RepaymentEvent
                                     {
                                         Id = 1,
                                         InstallmentNumber = 1,
                                         Principal = 200,
                                         Interests = 100,
                                         Commissions = 0,
                                         Penalties = 0,
                                         ClientType = OClientTypes.Group
                                     };
            LoanDisbursmentEvent lDE = new LoanDisbursmentEvent{Id = 2,Date = DateTime.Today,Amount = 100, ClientType = OClientTypes.Group};
			
            contractSource.Code = "HHHHJKLLJJBNBJJ";
            contractSource.AnticipatedTotalRepaymentPenalties = 0.02;
            contractSource.Events.Add(rE1);
            contractSource.Events.Add(lDE);

            Loan contractDestination = contractSource.Copy();

            Assert.AreEqual(contractSource.Amount.Value, contractDestination.Amount.Value);
            Assert.AreEqual(contractSource.AnticipatedTotalRepaymentPenalties,contractDestination.AnticipatedTotalRepaymentPenalties);
            Assert.AreEqual(contractSource.Code,contractDestination.Code);
            Assert.AreEqual(contractSource.ClientType,contractDestination.ClientType);
            Assert.AreEqual(contractSource.InstallmentList.Count,contractDestination.InstallmentList.Count);
            Assert.AreEqual(contractSource.InstallmentList[0].Number,contractDestination.InstallmentList[0].Number);
            Assert.AreEqual(contractSource.InstallmentList[1].Number,contractDestination.InstallmentList[1].Number);

//            ((LoanDisbursmentEvent)contractSource.Events.GetEvent(1)).Commission = 22;
//            Assert.IsTrue(((LoanDisbursmentEvent)contractDestination.Events.GetEvent(1)).Commission == 0);
//            Assert.IsFalse(((LoanDisbursmentEvent)contractDestination.Events.GetEvent(1)).Commission == 22);
			
            contractSource.InstallmentList[0].Number = 4;
            Assert.IsFalse(contractDestination.InstallmentList[0].Number == 4);
            Assert.IsTrue(contractDestination.InstallmentList[0].Number == 1);
        }

        [Test]
        public void TestContract9InstallmentsDecliningRate()
        {
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.CALCULATIONLATEFEESDURINGPUBLICHOLIDAYS, true);
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.ACCOUNTINGPROCESS, OAccountingProcesses.Accrual);

            LoanProduct package = new LoanProduct
                                      {
                                          InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
                                          LoanType = OLoanTypes.DecliningFixedInstallments,
                                          ChargeInterestWithinGracePeriod = true,
                                          Currency = new Currency { Id = 1 },
                                          RoundingType = ORoundingType.Approximate
                                      };
            package.KeepExpectedInstallment = false;
            Loan myContract = new Loan(package, 1500, 0.03m, 9, 1, new DateTime(2006, 1, 2), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));
            myContract.AnticipatedTotalRepaymentPenalties = 0.02;

            Assert.AreEqual(45m, myContract.GetInstallment(0).InterestsRepayment.Value);
            Assert.AreEqual(0m, myContract.GetInstallment(0).CapitalRepayment.Value);

            RepaymentEvent rPE = myContract.Repay(1, new DateTime(2006, 2, 2), 1575, false, false);
            Assert.AreEqual(45m, myContract.GetInstallment(0).PaidInterests.Value);
            Assert.AreEqual(0m, myContract.GetInstallment(0).PaidCapital.Value);
            Assert.AreEqual(30m, rPE.Fees.Value);
            Assert.AreEqual(45m, rPE.Interests.Value);
            Assert.AreEqual(1500m, rPE.Principal.Value);
        }

        [Test]
        public void TestContract9InstallmentsDecliningRateWhenLittleOverPaid()
        {
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.CALCULATIONLATEFEESDURINGPUBLICHOLIDAYS, true);

            LoanProduct package = new LoanProduct
                                      {
                                          InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
                                          LoanType = OLoanTypes.DecliningFixedInstallments,
                                          ChargeInterestWithinGracePeriod = true,
                                          Currency = new Currency { Id = 1 }
                                      };
            package.KeepExpectedInstallment = false;
            Loan myContract = new Loan(package,1500,0.03m,9,1,new DateTime(2006,1,1), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));
            myContract.AnticipatedTotalRepaymentPenalties = 0.02;

            Assert.AreEqual(45m,myContract.GetInstallment(0).InterestsRepayment.Value);
            Assert.AreEqual(0,myContract.GetInstallment(0).CapitalRepayment.Value);
            Assert.AreEqual(new DateTime(2006,2,1),myContract.GetInstallment(0).ExpectedDate);

            RepaymentEvent rPE = myContract.Repay(1,new DateTime(2006,2,1),45m,false,false);
            Assert.AreEqual(45,myContract.GetInstallment(0).PaidInterests.Value);
            Assert.AreEqual(0,myContract.GetInstallment(0).PaidCapital.Value);
            Assert.AreEqual(0m,rPE.Fees.Value);
            Assert.AreEqual(45m,rPE.Interests.Value);
            Assert.AreEqual(0m,rPE.Principal.Value);
        }

        [Test]
        public void TestCalculateRemainingInterestsWithDateTimeParameter()
        {
            LoanProduct package = new LoanProduct
                                      {
                                          InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
                                          LoanType = OLoanTypes.DecliningFixedInstallments,
                                          ChargeInterestWithinGracePeriod = true,
                                          Currency = new Currency { Id = 1, UseCents = true}
                                      };
            Loan myContract = new Loan(package,750,0.03m,9,1,new DateTime(2006,6,22), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));
            Assert.AreEqual(46.5m, Math.Round(myContract.CalculateRemainingInterests(new DateTime(2006, 8, 22)).Value, 2));
        }

        [Test]
        public void CalculateRemainingInterest_CashAccounting_BeforeFirstInstallment()
        {
            LoanProduct package = new LoanProduct
                                      {
                                          InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
                                          LoanType = OLoanTypes.Flat,
                                          ChargeInterestWithinGracePeriod = true,
                                          Currency = new Currency { Id = 1 }
                                      };
            Loan myContract = new Loan(package, 1000, 0.03m, 6, 1, new DateTime(2006, 1, 1), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));

            Assert.AreEqual(0, Math.Round(myContract.CalculateRemainingInterests(new DateTime(2006, 1, 12)).Value, 2));
        }

        [Test]
        public void CalculateRemainingInterest_AccrualAccounting_ExactlyDisbursmentDate()
        {
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.ACCOUNTINGPROCESS, OAccountingProcesses.Accrual);
            LoanProduct package = new LoanProduct
                                      {
                                          InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
                                          LoanType = OLoanTypes.Flat,
                                          ChargeInterestWithinGracePeriod = true,
                                          Currency = new Currency { Id = 1 }
                                      };
            Loan myContract = new Loan(package, 1000, 0.03m, 6, 1, new DateTime(2006, 1, 1), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));

            Assert.AreEqual(0, Math.Round(myContract.CalculateRemainingInterests(new DateTime(2006, 1, 1)).Value, 2));
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.ACCOUNTINGPROCESS, OAccountingProcesses.Cash);
        }

        

        [Test]
        public void CalculateRemainingInterest_AccrualAccounting()
        {
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.ACCOUNTINGPROCESS, OAccountingProcesses.Accrual);
            LoanProduct package = new LoanProduct
            {
                InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
                LoanType = OLoanTypes.Flat,
                ChargeInterestWithinGracePeriod = true,
                Currency = new Currency { Id = 1, UseCents = true}
            };
            Loan myContract = new Loan(package, 1000, 0.03m, 6, 0, new DateTime(2009, 12, 25), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));

            myContract.Repay(1, new DateTime(2010, 1, 25), 196.67m, false, true);

            Assert.AreEqual(17.42m, Math.Round(myContract.CalculateRemainingInterests(new DateTime(2010, 2, 12)).Value, 2));

            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.ACCOUNTINGPROCESS, OAccountingProcesses.Cash);
        }

        [Test]
        public void CalculateRemainingInterest_CashAccounting_ExactlyFirstInstallment()
        {
            LoanProduct package = new LoanProduct
                                      {
                                          InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
                                          LoanType = OLoanTypes.Flat,
                                          ChargeInterestWithinGracePeriod = true,
                                          Currency = new Currency { Id = 1 }
                                      };
            Loan myContract = new Loan(package, 1000, 0.03m, 6, 1, new DateTime(2006, 1, 1), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));

            Assert.AreEqual(30, Math.Round(myContract.CalculateRemainingInterests(new DateTime(2006, 2, 1)).Value, 2));
        }

        [Test]
        public void CalculateRemainingInterest_CashAccounting_AfterFirstInstallment()
        {
            LoanProduct package = new LoanProduct
                                      {
                                          InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
                                          LoanType = OLoanTypes.Flat,
                                          ChargeInterestWithinGracePeriod = true,
                                          Currency = new Currency { Id = 1 }
                                      };
            Loan myContract = new Loan(package, 1000, 0.03m, 6, 1, new DateTime(2006, 1, 1), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));

            Assert.AreEqual(30, Math.Round(myContract.CalculateRemainingInterests(new DateTime(2006, 2, 14)).Value, 2));
        }

        [Test]
        public void CalculateRemainingInterest_CashAccounting_ExactlySecondInstallment()
        {
            LoanProduct package = new LoanProduct
                                      {
                                          InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
                                          LoanType = OLoanTypes.Flat,
                                          ChargeInterestWithinGracePeriod = true,
                                          Currency = new Currency { Id = 1 }
                                      };
            Loan myContract = new Loan(package, 1000, 0.03m, 6, 1, new DateTime(2006, 1, 1), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));

            Assert.AreEqual(60, Math.Round(myContract.CalculateRemainingInterests(new DateTime(2006, 3, 1)).Value, 2));
        }

        [Test]
        public void CalculateRemainingInterest_CashAccounting_AfterSecondInstallment()
        {
            LoanProduct package = new LoanProduct
                                      {
                                          InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
                                          LoanType = OLoanTypes.Flat,
                                          ChargeInterestWithinGracePeriod = true,
                                          Currency = new Currency { Id = 1 }
                                      };
            Loan myContract = new Loan(package, 1000, 0.03m, 6, 1, new DateTime(2006, 1, 1), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));

            Assert.AreEqual(60, Math.Round(myContract.CalculateRemainingInterests(new DateTime(2006, 3, 15)).Value, 2));
        }

        [Test]
        public void TestRepayDecliningExoticLoanWith2MonthOfGracePeriod()
        {
            Assert.Ignore();
            //ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.CALCULATIONLATEFEESDURINGPUBLICHOLIDAYS, true);

            //LoanProduct package = new LoanProduct
            //                          {
            //                              InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
            //                              LoanType = OLoanTypes.DecliningFixedInstallments,
            //                              ChargeInterestWithinGracePeriod = true,
            //                              Currency = new Currency { Id = 1 }
            //                          };

            //ExoticInstallmentsTable exoticProduct = new ExoticInstallmentsTable();

            //exoticProduct.Add(new ExoticInstallment(1, 0.1, null));
            //exoticProduct.Add(new ExoticInstallment(2, 0.1, null));
            //exoticProduct.Add(new ExoticInstallment(3, 0.1, null));
            //exoticProduct.Add(new ExoticInstallment(4, 0.1, null));
            //exoticProduct.Add(new ExoticInstallment(5, 0.1, null));
            //exoticProduct.Add(new ExoticInstallment(6, 0.5, null));
            //package.ExoticProduct = exoticProduct;

            //Loan myContract = new Loan(package, 1000, 0.03m, 8, 2, new DateTime(2006, 1, 1), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisioningTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));
            //myContract.Repay(1, new DateTime(2006, 2, 1), 100, true, false);

            //Assert.AreEqual(1000m, myContract.GetInstallment(0).OLB.Value, "1");
            //Assert.AreEqual(960m, myContract.GetInstallment(1).OLB.Value, "2");
            //Assert.AreEqual(960m, myContract.GetInstallment(2).OLB.Value, "3");
            //Assert.AreEqual(837m, myContract.GetInstallment(3).OLB.Value, "4");
            //Assert.AreEqual(744m, myContract.GetInstallment(4).OLB.Value, "5");
            //Assert.AreEqual(651m, myContract.GetInstallment(5).OLB.Value, "6");
            //Assert.AreEqual(558m, myContract.GetInstallment(6).OLB.Value, "7");
            //Assert.AreEqual(465m, myContract.GetInstallment(7).OLB.Value, "8");

            //Assert.AreEqual(30m, myContract.GetInstallment(0).InterestsRepayment.Value, "1");
            //Assert.AreEqual(27.90m, myContract.GetInstallment(1).InterestsRepayment.Value, "2");
            //Assert.AreEqual(27.90m, myContract.GetInstallment(2).InterestsRepayment.Value, "3");
            //Assert.AreEqual(25.11m, myContract.GetInstallment(3).InterestsRepayment.Value, "4");
            //Assert.AreEqual(22.32m, myContract.GetInstallment(4).InterestsRepayment.Value, "5");
            //Assert.AreEqual(19.53m, myContract.GetInstallment(5).InterestsRepayment.Value, "6");
            //Assert.AreEqual(16.74m, myContract.GetInstallment(6).InterestsRepayment.Value, "7");
            //Assert.AreEqual(13.95m, myContract.GetInstallment(7).InterestsRepayment.Value, "8");

            //Assert.AreEqual(0m, myContract.GetInstallment(0).CapitalRepayment.Value);
            //Assert.AreEqual(0m, myContract.GetInstallment(1).CapitalRepayment.Value);
            //Assert.AreEqual(93m, myContract.GetInstallment(2).CapitalRepayment.Value);
            //Assert.AreEqual(93m, myContract.GetInstallment(3).CapitalRepayment.Value);
            //Assert.AreEqual(93m, myContract.GetInstallment(4).CapitalRepayment.Value);
            //Assert.AreEqual(93m, myContract.GetInstallment(5).CapitalRepayment.Value);
            //Assert.AreEqual(93m, myContract.GetInstallment(6).CapitalRepayment.Value);
            //Assert.AreEqual(465m, myContract.GetInstallment(7).CapitalRepayment.Value);
        }

        [Test]
        public void TestBadLoanAndGoBackToNormal()
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
            myContract.BadLoan = true;
            myContract.NonRepaymentPenalties.OLB = 0.003;
            myContract.AnticipatedTotalRepaymentPenalties = 0.01;

            CreditContractOptions cCO = new CreditContractOptions(package.LoanType, package.KeepExpectedInstallment, false, 0, 0, false, 0, package.AnticipatedTotalRepaymentPenaltiesBase);
            CreditContractRepayment cCR = new CreditContractRepayment(myContract, cCO, new DateTime(2006, 3, 15), 1, new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""));
            Assert.AreEqual(1194, Math.Round(cCR.MaximumAmountAuthorizeToRepay.Value, 2));
            Assert.AreEqual(386.00m, Math.Round(cCR.MaximumAmountToRegradingLoan.Value, 2));

            RepaymentEvent rPE = myContract.Repay(1,new DateTime(2006,3,15),386,false,true);
            Assert.AreEqual(126,rPE.Fees.Value);
            Assert.AreEqual(60,rPE.Interests.Value);
            Assert.AreEqual(200,rPE.Principal.Value);
        }

        [Test]
        public void TestCalculateMaximumAmountAuthorizedToRepay()
        {
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.ACCOUNTINGPROCESS, OAccountingProcesses.Cash);
            LoanProduct package = new LoanProduct
                                      {
                                          InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
                                          LoanType = OLoanTypes.Flat,
                                          ChargeInterestWithinGracePeriod = true,
                                          Currency = new Currency { Id = 1 }
                                      };
            package.KeepExpectedInstallment = false;
            Loan myContract = new Loan(package, 1000, 0.03m,6,1,new DateTime(2006,1,1), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));
            myContract.BadLoan = true;
            myContract.NonRepaymentPenalties.OLB = 0.003;
            myContract.AnticipatedTotalRepaymentPenalties = 0.01;

            Assert.AreEqual(1030, myContract.CalculateMaximumAmountAuthorizedToRepay(1, new DateTime(2006, 2, 1), true, 0, 0, false, 0, false).Value);
        }

        [Test]
        public void TestCalculateMaximumAmountToRegradingLoan()
        {
            LoanProduct package = new LoanProduct
                                      {
                                          InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
                                          LoanType = OLoanTypes.Flat,
                                          ChargeInterestWithinGracePeriod = true,
                                          Currency = new Currency { Id = 1 }
                                      };
            package.KeepExpectedInstallment = false;
            Loan myContract = new Loan(package, 1000, 0.03m,6,1,new DateTime(2006,1,1), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));
            myContract.BadLoan = true;
            myContract.NonRepaymentPenalties.OLB = 0.003;
            myContract.AnticipatedTotalRepaymentPenalties = 0.01;

            Assert.AreEqual(30m, myContract.CalculateMaximumAmountToRegradingLoan(1, new DateTime(2006, 2, 1), true, 0, 0, false, 0, false).Value);
        }

        [Test]
        public void TestIfAllInstallmentsAreRepaid()
        {
            LoanProduct package = new LoanProduct
                                      {
                                          InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
                                          LoanType = OLoanTypes.Flat,
                                          ChargeInterestWithinGracePeriod = true,
                                          Currency = new Currency { Id = 1 }
                                      };
            package.KeepExpectedInstallment = false;
            Loan myContract = new Loan(package, 750, 0.03m,6,1,new DateTime(2006,1,1), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));
            myContract.BadLoan = true;
            myContract.NonRepaymentPenalties.OLB = 0.003;
            myContract.AnticipatedTotalRepaymentPenalties = 0.01;
            myContract.InstallmentList = new List<Installment>();
            Assert.IsFalse(myContract.AllInstallmentsRepaid);
        }

        private static void _AssertSpecifiedInstallment(Installment pInstallment, OCurrency pInterestRepayment, OCurrency pCapitalRepayment, OCurrency pOLB, OCurrency pPaidCapital, OCurrency pPaidInterest)
        {
            Assert.AreEqual(pInterestRepayment.Value, pInstallment.InterestsRepayment.Value);
            Assert.AreEqual(pCapitalRepayment.Value, pInstallment.CapitalRepayment.Value);
            Assert.AreEqual(pOLB.Value, pInstallment.OLB.Value);
            Assert.AreEqual(pPaidCapital.Value, pInstallment.PaidCapital.Value);
            Assert.AreEqual(pPaidInterest.Value, pInstallment.PaidInterests.Value);
        }

        [Test]
        public void NonRepaidInstallments()
        {
            LoanProduct package = new LoanProduct
                                      {
                                          InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
                                          LoanType = OLoanTypes.Flat,
                                          Currency = new Currency {Id = 1}
                                      };
            Loan loan = new Loan(package, 1000, 0.02m, 5, 0, new DateTime(2006, 1, 1), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));
            loan.Repay(1, new DateTime(2006, 2, 1), 220, true, true);
            Assert.AreEqual(loan.GetInstallment(0).IsRepaid, true);
            int num = 2;
            foreach (Installment i in loan.NonRepaidInstallments)
            {
                Assert.AreEqual(i.Number, num);
                num++;
            }
        }
    }
}
