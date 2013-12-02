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
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using OpenCBS.CoreDomain;
using OpenCBS.CoreDomain.Contracts.Loans;
using OpenCBS.CoreDomain.Contracts.Loans.Installments;
using OpenCBS.CoreDomain.Products;
using OpenCBS.Enums;
using OpenCBS.Shared;
using OpenCBS.CoreDomain.Accounting;
using OpenCBS.Shared.Settings;

namespace OpenCBS.Test.CoreDomain.Contracts.CalculateInstallments
{
    [TestFixture]
    public class FlatRateWithCents
    {
        private NonWorkingDateSingleton nonWorkingDateHelper;
        private ApplicationSettings dataParam;

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

            nonWorkingDateHelper = NonWorkingDateSingleton.GetInstance("");
            nonWorkingDateHelper.WeekEndDay1 = 6;
            nonWorkingDateHelper.WeekEndDay2 = 0;
            nonWorkingDateHelper.PublicHolidays = new Dictionary<DateTime, string>
                                                      {
                                                          {new DateTime(2006, 1, 1), "New Year Eve"},
                                                          {new DateTime(2006, 12, 25), "Christmas"}
                                                      };
            dataParam = ApplicationSettings.GetInstance("");
            dataParam.DeleteAllParameters();
            dataParam.AddParameter(OGeneralSettings.PAYFIRSTINSTALLMENTREALVALUE, true);
            dataParam.AddParameter(OGeneralSettings.DONOTSKIPWEEKENDSININSTALLMENTSDATE, false);
            dataParam.AddParameter(OGeneralSettings.INCREMENTALDURINGDAYOFF, true);
        }

        private static void _AssertSpecifiedInstallment(Installment pInstallment, int pNumber, DateTime pExpectedDate, OCurrency pInterestRepayment, OCurrency pCapitalRepayment, OCurrency pOLB)
        {
            Assert.AreEqual(pNumber, pInstallment.Number);
            Assert.AreEqual(pExpectedDate, pInstallment.ExpectedDate);
            Assert.AreEqual(pInterestRepayment.Value, pInstallment.InterestsRepayment.Value);
            Assert.AreEqual(pCapitalRepayment.Value, pInstallment.CapitalRepayment.Value);
            Assert.AreEqual(pOLB.Value, pInstallment.OLB.Value);
        }

        [Test]
        public void FlatRate_MonthlyInstallmentType_NoGracePeriod()
        {
            LoanProduct package = new LoanProduct
            {
                InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
                LoanType = OLoanTypes.Flat,
                ChargeInterestWithinGracePeriod = true,
                Currency = new Currency { Id = 1, UseCents = true},
                RoundingType = ORoundingType.Approximate
            };
            Loan myContract = new Loan(package, 1000, 0.03m, 6, 0, new DateTime(2006, 1, 1) , new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));

            Assert.AreEqual(6, myContract.InstallmentList.Count);

            _AssertSpecifiedInstallment(myContract.GetInstallment(0), 1, new DateTime(2006, 2, 1), 30.00m, 166.67m, 1000);
            _AssertSpecifiedInstallment(myContract.GetInstallment(1), 2, new DateTime(2006, 3, 1), 30.00m, 166.67m,  833.33m);
            _AssertSpecifiedInstallment(myContract.GetInstallment(2), 3, new DateTime(2006, 4, 3), 30.00m, 166.67m,  666.66m);
            _AssertSpecifiedInstallment(myContract.GetInstallment(3), 4, new DateTime(2006, 5, 1), 30.00m, 166.66m,  499.99m);
            _AssertSpecifiedInstallment(myContract.GetInstallment(4), 5, new DateTime(2006, 6, 1), 30.00m, 166.67m,  333.33m);
            _AssertSpecifiedInstallment(myContract.GetInstallment(5), 6, new DateTime(2006, 7, 3), 30.00m, 166.66m,  166.66m);
        }

        [Test]
        public void FlatRate_BiWeeklyInstallmentType_NoGracePeriod()
        {
            LoanProduct package = new LoanProduct
            {
                InstallmentType = new InstallmentType(1, "Bi-Weekly", 14, 0),
                LoanType = OLoanTypes.Flat,
                ChargeInterestWithinGracePeriod = true,
                Currency = new Currency { Id = 1, UseCents = true}
            };
            Loan myContract = new Loan(package, 1000, 0.03m, 6, 0, new DateTime(2006, 1, 1), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));

            Assert.AreEqual(6, myContract.InstallmentList.Count);

            _AssertSpecifiedInstallment(myContract.GetInstallment(0), 1, new DateTime(2006, 1, 16), 32.14m, 166.67m, 1000);
            _AssertSpecifiedInstallment(myContract.GetInstallment(1), 2, new DateTime(2006, 1, 30), 30.00m, 166.67m, 833.33m);
            _AssertSpecifiedInstallment(myContract.GetInstallment(2), 3, new DateTime(2006, 2, 13), 30.00m, 166.67m, 666.66m);
            _AssertSpecifiedInstallment(myContract.GetInstallment(3), 4, new DateTime(2006, 2, 27), 30.00m, 166.66m, 499.99m);
            _AssertSpecifiedInstallment(myContract.GetInstallment(4), 5, new DateTime(2006, 3, 13), 30.00m, 166.67m, 333.33m);
            _AssertSpecifiedInstallment(myContract.GetInstallment(5), 6, new DateTime(2006, 3, 27), 30.00m, 166.66m, 166.66m);
        }

        [Test]
        public void FlatRate_BiWeeklyInstallmentType_GracePeriod()
        {
            LoanProduct package = new LoanProduct
            {
                InstallmentType = new InstallmentType(1, "Bi-Weekly", 14, 0),
                LoanType = OLoanTypes.Flat,
                ChargeInterestWithinGracePeriod = true,
                Currency = new Currency { Id = 1, UseCents = true}
            };
            Loan myContract = new Loan(package, 1000, 0.03m, 6, 2, new DateTime(2006, 1, 1), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));

            _AssertSpecifiedInstallment(myContract.GetInstallment(0), 1, new DateTime(2006, 1, 16), 32.14m, 0, 1000);
            _AssertSpecifiedInstallment(myContract.GetInstallment(1), 2, new DateTime(2006, 1, 30), 30.00m,   0, 1000);
            _AssertSpecifiedInstallment(myContract.GetInstallment(2), 3, new DateTime(2006, 2, 13), 30.00m, 250, 1000);
            _AssertSpecifiedInstallment(myContract.GetInstallment(3), 4, new DateTime(2006, 2, 27), 30.00m, 250,  750);
            _AssertSpecifiedInstallment(myContract.GetInstallment(4), 5, new DateTime(2006, 3, 13), 30.00m, 250,  500);
            _AssertSpecifiedInstallment(myContract.GetInstallment(5), 6, new DateTime(2006, 3, 27), 30.00m, 250,  250);
        }

        [Test]
        public void FlatRate_ExoticInstallments_GracePeriod()
        {
            LoanProduct package = new LoanProduct
                                      {
                                          InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
                                          LoanType = OLoanTypes.Flat,
                                          ChargeInterestWithinGracePeriod = true,
                                          ExoticProduct = new ExoticInstallmentsTable
                                                              {
                                                                  new ExoticInstallment(1, 0.1, 0.1),
                                                                  new ExoticInstallment(2, 0.1, 0.1),
                                                                  new ExoticInstallment(3, 0.1, 0.1),
                                                                  new ExoticInstallment(4, 0.1, 0.1),
                                                                  new ExoticInstallment(5, 0.1, 0.1),
                                                                  new ExoticInstallment(6, 0.5, 0.5)
                                                              },
                                          Currency = new Currency { Id = 1 }
                                      };

            Loan myContract = new Loan(package, 1000, 0.03m, 7, 1, new DateTime(2006, 1, 1), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));

            _AssertSpecifiedInstallment(myContract.GetInstallment(0), 1, new DateTime(2006, 2, 1), 30.00m,   0, 1000);
            _AssertSpecifiedInstallment(myContract.GetInstallment(1), 2, new DateTime(2006, 3, 1), 18.000m, 100.00m, 1000);
            _AssertSpecifiedInstallment(myContract.GetInstallment(2), 3, new DateTime(2006, 4, 3), 18.00m, 100.00m, 900);
            _AssertSpecifiedInstallment(myContract.GetInstallment(3), 4, new DateTime(2006, 5, 1), 18.00m, 100.00m, 800);
            _AssertSpecifiedInstallment(myContract.GetInstallment(4), 5, new DateTime(2006, 6, 1), 18.00m, 100.00m, 700);
            _AssertSpecifiedInstallment(myContract.GetInstallment(5), 6, new DateTime(2006, 7, 3), 18.00m, 100.00m, 600);
            _AssertSpecifiedInstallment(myContract.GetInstallment(6), 7, new DateTime(2006, 8, 1), 90.00m, 500.00m, 500);
        }

        [Test]
        public void FlatRate_ExoticInstallments_GracePeriod_RoundingInterest()
        {
            LoanProduct package = new LoanProduct{
                InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
                LoanType = OLoanTypes.Flat,
                ChargeInterestWithinGracePeriod = true,
            
                ExoticProduct = new ExoticInstallmentsTable
                                                              {
                                                                  new ExoticInstallment(1, 0.1, 0.1),
                                                                  new ExoticInstallment(2, 0.1, 0.1),
                                                                  new ExoticInstallment(3, 0.1, 0.1),
                                                                  new ExoticInstallment(4, 0.1, 0.1),
                                                                  new ExoticInstallment(5, 0.1, 0.1),
                                                                  new ExoticInstallment(6, 0.5, 0.5)
                                                              },
                Currency = new Currency { Id = 1, UseCents = true}
            };

            Loan myContract = new Loan(package, 900, 0.026m, 9, 3, new DateTime(2006, 1, 1), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));

            _AssertSpecifiedInstallment(myContract.GetInstallment(0), 1, new DateTime(2006, 2, 1), 23.4m, 0, 900);
            _AssertSpecifiedInstallment(myContract.GetInstallment(1), 2, new DateTime(2006, 3, 1), 23.4m, 0, 900);
            _AssertSpecifiedInstallment(myContract.GetInstallment(2), 3, new DateTime(2006, 4, 3), 23.4m, 0, 900);
            _AssertSpecifiedInstallment(myContract.GetInstallment(3), 4, new DateTime(2006, 5, 1), 14.04m, 90.00m, 900);
            _AssertSpecifiedInstallment(myContract.GetInstallment(4), 5, new DateTime(2006, 6, 1), 14.04m, 90.00m, 810);
            _AssertSpecifiedInstallment(myContract.GetInstallment(5), 6, new DateTime(2006, 7, 3), 14.04m, 90.00m, 720);
            _AssertSpecifiedInstallment(myContract.GetInstallment(6), 7, new DateTime(2006, 8, 1), 14.04m, 90.00m, 630);
            _AssertSpecifiedInstallment(myContract.GetInstallment(7), 8, new DateTime(2006, 9, 1), 14.04m, 90.00m, 540);
            _AssertSpecifiedInstallment(myContract.GetInstallment(8), 9, new DateTime(2006,10, 2), 70.20m, 450.00m, 450);
        }

        [Test]
        public void FlatRate_ExoticInstallments_GracePeriod_50PercentMiddle_50PercentEnd()
        {
            LoanProduct package = new LoanProduct
                                      {
                                          InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
                                          LoanType = OLoanTypes.Flat,
                                          ChargeInterestWithinGracePeriod = true,

                                          ExoticProduct = new ExoticInstallmentsTable
                                                              {
                                                                  new ExoticInstallment(1, 0, 0),
                                                                  new ExoticInstallment(2, 0, 0),
                                                                  new ExoticInstallment(3, 0.5, 0.5),
                                                                  new ExoticInstallment(4, 0, 0),
                                                                  new ExoticInstallment(5, 0, 0),
                                                                  new ExoticInstallment(6, 0.5, 0.5)
                                                              },
                                          Currency = new Currency { Id = 1 }
                                      };

            Loan myContract = new Loan(package, 1000, 0.02m, 7, 1, new DateTime(2006, 1, 1), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));

            _AssertSpecifiedInstallment(myContract.GetInstallment(0), 1, new DateTime(2006, 2, 1), 20.00m,0, 1000);
            _AssertSpecifiedInstallment(myContract.GetInstallment(1), 2, new DateTime(2006, 3, 1),  0,   0, 1000);
            _AssertSpecifiedInstallment(myContract.GetInstallment(2), 3, new DateTime(2006, 4, 3),  0,   0, 1000);
            _AssertSpecifiedInstallment(myContract.GetInstallment(3), 4, new DateTime(2006, 5, 1), 60, 500, 1000);
            _AssertSpecifiedInstallment(myContract.GetInstallment(4), 5, new DateTime(2006, 6, 1),  0,   0, 500);
            _AssertSpecifiedInstallment(myContract.GetInstallment(5), 6, new DateTime(2006, 7, 3),  0,   0, 500);
            _AssertSpecifiedInstallment(myContract.GetInstallment(6), 7, new DateTime(2006, 8, 1), 60, 500, 500);
        }

        [Test]
        public void FlatRate_ExoticInstallments_NoGracePeriod()
        {
            LoanProduct package = new LoanProduct
                                      {
                                          InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
                                          LoanType = OLoanTypes.Flat,
                                          ChargeInterestWithinGracePeriod = true,

                                          ExoticProduct = new ExoticInstallmentsTable
                                                              {
                                                                  new ExoticInstallment(1, 0.1, 0.1),
                                                                  new ExoticInstallment(2, 0.1, 0.1),
                                                                  new ExoticInstallment(3, 0.1, 0.1),
                                                                  new ExoticInstallment(4, 0.1, 0.1),
                                                                  new ExoticInstallment(5, 0.1, 0.1),
                                                                  new ExoticInstallment(6, 0.5, 0.5)
                                                              },
                                          Currency = new Currency { Id = 1 }
                                      };

            Loan myContract = new Loan(package, 1000, 0.03m, 6, 0, new DateTime(2006, 1, 1), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));

            _AssertSpecifiedInstallment(myContract.GetInstallment(0), 1, new DateTime(2006, 2, 1), 18.00m, 100.00m, 1000);
            _AssertSpecifiedInstallment(myContract.GetInstallment(1), 2, new DateTime(2006, 3, 1), 18.00m, 100.00m, 900);
            _AssertSpecifiedInstallment(myContract.GetInstallment(2), 3, new DateTime(2006, 4, 3), 18.00m, 100.00m, 800);
            _AssertSpecifiedInstallment(myContract.GetInstallment(3), 4, new DateTime(2006, 5, 1), 18.00m, 100.00m, 700);
            _AssertSpecifiedInstallment(myContract.GetInstallment(4), 5, new DateTime(2006, 6, 1), 18.00m, 100.00m, 600);
            _AssertSpecifiedInstallment(myContract.GetInstallment(5), 6, new DateTime(2006, 7, 3), 90.00m, 500.00m, 500);
        }
    }
}
