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
    public class DecliningRateWithCents
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
            dataParam.AddParameter(OGeneralSettings.OLBBEFOREREPAYMENT, true);
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

        private static void _AssertSpecifiedInstallment(Installment pInstallment, int pNumber, DateTime pExpectedDate, OCurrency pInterestRepayment, OCurrency pCapitalRepayment, OCurrency pOLB, OCurrency pInstallmentTotalAmount)
        {
            Assert.AreEqual(pNumber, pInstallment.Number);
            Assert.AreEqual(pExpectedDate, pInstallment.ExpectedDate);
            Assert.AreEqual(pInterestRepayment.Value, pInstallment.InterestsRepayment.Value);
            Assert.AreEqual(pCapitalRepayment.Value, pInstallment.CapitalRepayment.Value);
            Assert.AreEqual(pOLB.Value, pInstallment.OLB.Value);
            Assert.AreEqual(pInstallmentTotalAmount.Value, pInstallment.AmountHasToPayWithInterest.Value);
        }

        [Test]
        public void DecliningRate_FixedInstallment_BiWeeklyInstallmentType_NoGracePeriod()
        {
            LoanProduct package = new LoanProduct
            {
                InstallmentType = new InstallmentType(1, "Bi-Weekly", 14, 0),
                LoanType = OLoanTypes.DecliningFixedInstallments,
                ChargeInterestWithinGracePeriod = true, 
                Currency = new Currency{Id = 1, UseCents = true},
                RoundingType = ORoundingType.Approximate
            };
            Loan myContract = new Loan(package, 1000, 0.03m, 6, 0, new DateTime(2006, 1, 1), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));

            Assert.AreEqual(1000, _CheckInstalmentsTotalPrincipal(myContract).Value);

            _AssertSpecifiedInstallment(myContract.GetInstallment(0), 1, new DateTime(2006, 1, 16), 32.14m, 154.60m, 1000);
            _AssertSpecifiedInstallment(myContract.GetInstallment(1), 2, new DateTime(2006, 1, 30), 25.36m, 159.24m, 845.40m);
            _AssertSpecifiedInstallment(myContract.GetInstallment(2), 3, new DateTime(2006, 2, 13), 20.58m, 164.02m, 686.16m);
            _AssertSpecifiedInstallment(myContract.GetInstallment(3), 4, new DateTime(2006, 2, 27), 15.66m, 168.93m, 522.14m);
            _AssertSpecifiedInstallment(myContract.GetInstallment(4), 5, new DateTime(2006, 3, 13), 10.60m, 173.99m, 353.21m);
            _AssertSpecifiedInstallment(myContract.GetInstallment(5), 6, new DateTime(2006, 3, 27),  5.38m, 179.22m, 179.22m);
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
        public void DecliningRate_FixedInstallment_NoGracePeriod_RoundingProblem()
        {
            LoanProduct package = new LoanProduct
            {
                InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
                LoanType = OLoanTypes.DecliningFixedInstallments,
                ChargeInterestWithinGracePeriod = true,
                Currency = new Currency{Id = 1, UseCents = true},
                RoundingType = ORoundingType.Approximate
            };
            Loan myContract = new Loan(package, 100, 0.01m, 10, 0, new DateTime(2006, 1, 1), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));

            _AssertSpecifiedInstallment(myContract.GetInstallment(0), 1, new DateTime(2006, 2, 1), 1, 9.56m, 100, 10.56m);
            _AssertSpecifiedInstallment(myContract.GetInstallment(1), 2, new DateTime(2006, 3, 1), 0.9m, 9.66m, 90.44m, 10.56m);
            _AssertSpecifiedInstallment(myContract.GetInstallment(2), 3, new DateTime(2006, 4, 3), 0.81m, 9.75m, 80.78m, 10.56m);
            _AssertSpecifiedInstallment(myContract.GetInstallment(3), 4, new DateTime(2006, 5, 1), 0.71m, 9.85m, 71.03m, 10.56m);
            _AssertSpecifiedInstallment(myContract.GetInstallment(4), 5, new DateTime(2006, 6, 1), 0.61m, 9.95m, 61.18m, 10.56m);
            _AssertSpecifiedInstallment(myContract.GetInstallment(5), 6, new DateTime(2006, 7, 3), 0.51m, 10.05m, 51.23m, 10.56m);
            _AssertSpecifiedInstallment(myContract.GetInstallment(6), 7, new DateTime(2006, 8, 1), 0.41m, 10.14m, 41.18m, 10.55m);
            _AssertSpecifiedInstallment(myContract.GetInstallment(7), 8, new DateTime(2006, 9, 1), 0.31m, 10.24m, 31.04m, 10.55m);
            _AssertSpecifiedInstallment(myContract.GetInstallment(8), 9, new DateTime(2006, 10, 2), 0.21m, 10.35m, 20.8m, 10.56m);
            _AssertSpecifiedInstallment(myContract.GetInstallment(9), 10, new DateTime(2006, 11, 1), 0.1m, 10.45m, 10.45m, 10.55m);
        }

        [Test]
        public void DecliningRate_FixedPrincipal_BiWeeklyInstallmentType_NoGracePeriod()
        {
            LoanProduct package = new LoanProduct
            {
                InstallmentType = new InstallmentType(1, "Bi-Weekly", 14, 0),
                LoanType = OLoanTypes.DecliningFixedPrincipal,
                ChargeInterestWithinGracePeriod = true,
                Currency = new Currency { Id = 1, UseCents = true}
            };
            Loan myContract = new Loan(package, 1000, 0.03m, 5, 0, new DateTime(2006, 1, 1), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));

            _AssertSpecifiedInstallment(myContract.GetInstallment(0), 1, new DateTime(2006, 1, 16), 32.14m, 200, 1000);
            _AssertSpecifiedInstallment(myContract.GetInstallment(1), 2, new DateTime(2006, 1, 30), 24.00m, 200, 800);
            _AssertSpecifiedInstallment(myContract.GetInstallment(2), 3, new DateTime(2006, 2, 13), 18.00m, 200, 600);
            _AssertSpecifiedInstallment(myContract.GetInstallment(3), 4, new DateTime(2006, 2, 27), 12.00m, 200, 400);
            _AssertSpecifiedInstallment(myContract.GetInstallment(4), 5, new DateTime(2006, 3, 13), 6.00m, 200, 200);
        }

        [Test]
        public void DecliningRate_FixedInstallments_GracePeriod()
        {
            LoanProduct package = new LoanProduct
            {
                InstallmentType = new InstallmentType(1, "Bi-Weekly", 14, 0),
                LoanType = OLoanTypes.DecliningFixedInstallments,
                ChargeInterestWithinGracePeriod = true,
                Currency = new Currency { Id = 1, UseCents = true},
                RoundingType = ORoundingType.Approximate
            };
            Loan myContract = new Loan(package, 1000, 0.03m, 7, 1, new DateTime(2006, 1, 1), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));

            Assert.AreEqual(1000, _CheckInstalmentsTotalPrincipal(myContract).Value);

            _AssertSpecifiedInstallment(myContract.GetInstallment(0), 1, new DateTime(2006, 1, 16), 32.14m, 0, 1000);
            _AssertSpecifiedInstallment(myContract.GetInstallment(1), 2, new DateTime(2006, 1, 30), 30.00m, 154.60m, 1000);
            _AssertSpecifiedInstallment(myContract.GetInstallment(2), 3, new DateTime(2006, 2, 13), 25.36m, 159.24m, 845.40m);
            _AssertSpecifiedInstallment(myContract.GetInstallment(3), 4, new DateTime(2006, 2, 27), 20.58m, 164.02m, 686.16m);
            _AssertSpecifiedInstallment(myContract.GetInstallment(4), 5, new DateTime(2006, 3, 13), 15.66m, 168.93m, 522.14m);
            _AssertSpecifiedInstallment(myContract.GetInstallment(5), 6, new DateTime(2006, 3, 27), 10.60m, 173.99m, 353.21m);
            _AssertSpecifiedInstallment(myContract.GetInstallment(6), 7, new DateTime(2006, 4, 10),  5.38m, 179.22m, 179.22m);
        }

        [Test]
        public void DecliningRate_ExoticInstallments_NoGracePeriod()
        {
            LoanProduct package = new LoanProduct
                                      {
                                          InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
                                          LoanType = OLoanTypes.DecliningFixedInstallments,
                                          ChargeInterestWithinGracePeriod = true,
                                          ExoticProduct = new ExoticInstallmentsTable
                                                              {
                                                                  new ExoticInstallment(1, 0.1, null),
                                                                  new ExoticInstallment(2, 0.1, null),
                                                                  new ExoticInstallment(3, 0.1, null),
                                                                  new ExoticInstallment(4, 0.1, null),
                                                                  new ExoticInstallment(5, 0.1, null),
                                                                  new ExoticInstallment(6, 0.5, null)
                                                              },
                                          Currency = new Currency { Id = 1 }
                                      };

            Loan myContract = new Loan(package, 1000, 0.03m, 6, 0, new DateTime(2006, 1, 1), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));

            _AssertSpecifiedInstallment(myContract.GetInstallment(0), 1, new DateTime(2006, 2, 1), 30.00m, 100.00m, 1000);
            _AssertSpecifiedInstallment(myContract.GetInstallment(1), 2, new DateTime(2006, 3, 1), 27.00m, 100.00m, 900);
            _AssertSpecifiedInstallment(myContract.GetInstallment(2), 3, new DateTime(2006, 4, 3), 24.00m, 100.00m, 800);
            _AssertSpecifiedInstallment(myContract.GetInstallment(3), 4, new DateTime(2006, 5, 1), 21.00m, 100.00m, 700);
            _AssertSpecifiedInstallment(myContract.GetInstallment(4), 5, new DateTime(2006, 6, 1), 18.00m, 100.00m, 600);
            _AssertSpecifiedInstallment(myContract.GetInstallment(5), 6, new DateTime(2006, 7, 3), 15.00m, 500.00m, 500);
        }

        [Test]
        public void DecliningRate_ExoticInstallments_NoGracePeriod_RoundingCapital_RoundingInterest()
        {
            LoanProduct package = new LoanProduct
            {
                InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
                LoanType = OLoanTypes.DecliningFixedInstallments,
                ChargeInterestWithinGracePeriod = true,
                ExoticProduct = new ExoticInstallmentsTable
                                                              {
                                                                  new ExoticInstallment(1, 0.1, null),
                                                                  new ExoticInstallment(2, 0.1, null),
                                                                  new ExoticInstallment(3, 0.1, null),
                                                                  new ExoticInstallment(4, 0.1, null),
                                                                  new ExoticInstallment(5, 0.1, null),
                                                                  new ExoticInstallment(6, 0.5, null)
                                                              },
                Currency = new Currency { Id = 1, UseCents = true}
            };

            Loan myContract = new Loan(package, 954, 0.03m, 6, 0, new DateTime(2006, 1, 1), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));

            _AssertSpecifiedInstallment(myContract.GetInstallment(0), 1, new DateTime(2006, 2, 1), 28.62m, 95.40m, 954);
            _AssertSpecifiedInstallment(myContract.GetInstallment(1), 2, new DateTime(2006, 3, 1), 25.76m, 95.40m, 858.6m);
            _AssertSpecifiedInstallment(myContract.GetInstallment(2), 3, new DateTime(2006, 4, 3), 22.90m, 95.40m, 763.2m);
            _AssertSpecifiedInstallment(myContract.GetInstallment(3), 4, new DateTime(2006, 5, 1), 20.03m, 95.40m, 667.8m);
            _AssertSpecifiedInstallment(myContract.GetInstallment(4), 5, new DateTime(2006, 6, 1), 17.17m, 95.40m, 572.4m);
            _AssertSpecifiedInstallment(myContract.GetInstallment(5), 6, new DateTime(2006, 7, 3), 14.31m, 477, 477);
        }

        [Test]
        public void DecliningRate_ExoticInstallments_GracePeriod()
        {
            LoanProduct package = new LoanProduct
            {
                InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
                LoanType = OLoanTypes.DecliningFixedInstallments,
                ChargeInterestWithinGracePeriod = true,
                                          ExoticProduct = new ExoticInstallmentsTable
                                                              {
                                                                  new ExoticInstallment(1, 0.1, null),
                                                                  new ExoticInstallment(2, 0.1, null),
                                                                  new ExoticInstallment(3, 0.1, null),
                                                                  new ExoticInstallment(4, 0.1, null),
                                                                  new ExoticInstallment(5, 0.1, null),
                                                                  new ExoticInstallment(6, 0.5, null)
                                                              },
                Currency = new Currency { Id = 1 }
                                      };
            Loan myContract = new Loan(package, 1000, 0.03m, 7, 1, new DateTime(2006, 1, 1), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));

            _AssertSpecifiedInstallment(myContract.GetInstallment(0), 1, new DateTime(2006, 2, 1), 30.00m,       0, 1000);
            _AssertSpecifiedInstallment(myContract.GetInstallment(1), 2, new DateTime(2006, 3, 1), 30.00m, 100.00m, 1000);
            _AssertSpecifiedInstallment(myContract.GetInstallment(2), 3, new DateTime(2006, 4, 3), 27.00m, 100.00m,  900);
            _AssertSpecifiedInstallment(myContract.GetInstallment(3), 4, new DateTime(2006, 5, 1), 24.00m, 100.00m,  800);
            _AssertSpecifiedInstallment(myContract.GetInstallment(4), 5, new DateTime(2006, 6, 1), 21.00m, 100.00m,  700);
            _AssertSpecifiedInstallment(myContract.GetInstallment(5), 6, new DateTime(2006, 7, 3), 18.00m, 100.00m,  600);
            _AssertSpecifiedInstallment(myContract.GetInstallment(6), 7, new DateTime(2006, 8, 1), 15.00m, 500.00m,  500);
        }
    }
}
