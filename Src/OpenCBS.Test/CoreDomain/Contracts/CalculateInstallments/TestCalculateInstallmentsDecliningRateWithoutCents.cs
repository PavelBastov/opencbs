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
    public class DecliningRateWithoutCents
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
            nonWorkingDateHelper.PublicHolidays = new Dictionary<DateTime, string>();
            nonWorkingDateHelper.PublicHolidays.Add(new DateTime(2006, 1, 1), "New Year Eve");
            nonWorkingDateHelper.PublicHolidays.Add(new DateTime(2006, 12, 25), "Christmas");
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
        public void DecliningRate_FixedInstallment_GracePeriod()
        {
            LoanProduct package = new LoanProduct
            {
                InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
                LoanType = OLoanTypes.DecliningFixedInstallments,
                ChargeInterestWithinGracePeriod = true,
                Currency = new Currency { Id = 1 },
                RoundingType = ORoundingType.Approximate
            };
            Loan myContract = new Loan(package, 1000, 0.03m, 6, 3, new DateTime(2006, 1, 1), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));

            _AssertSpecifiedInstallment(myContract.GetInstallment(0), 1, new DateTime(2006, 2, 1), 30, 0, 1000);
            _AssertSpecifiedInstallment(myContract.GetInstallment(1), 2, new DateTime(2006, 3, 1), 30, 0, 1000);
            _AssertSpecifiedInstallment(myContract.GetInstallment(2), 3, new DateTime(2006, 4, 3), 30, 0, 1000);
            _AssertSpecifiedInstallment(myContract.GetInstallment(3), 4, new DateTime(2006, 5, 1), 30, 324, 1000);
            _AssertSpecifiedInstallment(myContract.GetInstallment(4), 5, new DateTime(2006, 6, 1), 20, 333, 676);
            _AssertSpecifiedInstallment(myContract.GetInstallment(5), 6, new DateTime(2006, 7, 3), 10, 343, 343);
        }

        [Test]
        public void DecliningRate_FixedInstallment_NoGracePeriod_RoundingTypeBegin()
        {
            LoanProduct package = new LoanProduct
            {
                InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
                LoanType = OLoanTypes.DecliningFixedInstallments,
                ChargeInterestWithinGracePeriod = true,
                Currency = new Currency { Id = 1 },
                RoundingType = ORoundingType.Begin
            };

            Loan myContract = new Loan(package, 10000, 0.05m, 6, 0, new DateTime(2010, 1, 1), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));

            _AssertSpecifiedInstallment(myContract.GetInstallment(0), 1, new DateTime(2010, 2, 1), 500, 1471, 10000);
            _AssertSpecifiedInstallment(myContract.GetInstallment(1), 2, new DateTime(2010, 3, 1), 427, 1543, 8529);
            _AssertSpecifiedInstallment(myContract.GetInstallment(2), 3, new DateTime(2010, 4, 1), 349, 1621, 6986);
            _AssertSpecifiedInstallment(myContract.GetInstallment(3), 4, new DateTime(2010, 5, 3), 268, 1702, 5365);
            _AssertSpecifiedInstallment(myContract.GetInstallment(4), 5, new DateTime(2010, 6, 1), 183, 1787, 3663);
            _AssertSpecifiedInstallment(myContract.GetInstallment(5), 6, new DateTime(2010, 7, 1), 94, 1876, 1876);
        }

        [Test]
        public void DecliningRate_FixedInstallment_NoGracePeriod_RoundingTypeEnd()
        {
            LoanProduct package = new LoanProduct
            {
                InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
                LoanType = OLoanTypes.DecliningFixedInstallments,
                ChargeInterestWithinGracePeriod = true,
                Currency = new Currency { Id = 1 },
                RoundingType = ORoundingType.End
            };

            Loan myContract = new Loan(package, 10000, 0.05m, 6, 0, new DateTime(2010, 1, 1), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));

            _AssertSpecifiedInstallment(myContract.GetInstallment(0), 1, new DateTime(2010, 2, 1), 500, 1470, 10000);
            _AssertSpecifiedInstallment(myContract.GetInstallment(1), 2, new DateTime(2010, 3, 1), 427, 1543, 8530);
            _AssertSpecifiedInstallment(myContract.GetInstallment(2), 3, new DateTime(2010, 4, 1), 349, 1621, 6987);
            _AssertSpecifiedInstallment(myContract.GetInstallment(3), 4, new DateTime(2010, 5, 3), 268, 1702, 5366);
            _AssertSpecifiedInstallment(myContract.GetInstallment(4), 5, new DateTime(2010, 6, 1), 183, 1787, 3664);
            _AssertSpecifiedInstallment(myContract.GetInstallment(5), 6, new DateTime(2010, 7, 1), 94, 1877, 1877);
        }

        [Test]
        public void DecliningRate_FixedInstallment_NewWayOfCalcul()
        {
            LoanProduct package = new LoanProduct
            {
                InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
                LoanType = OLoanTypes.DecliningFixedInstallments,
                ChargeInterestWithinGracePeriod = true,
                Currency = new Currency { Id = 1 },
                RoundingType = ORoundingType.Approximate
            };
            Loan myContract = new Loan(package, 800, 0.03m, 6, 0, new DateTime(2006, 1, 1), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));

            _AssertSpecifiedInstallment(myContract.GetInstallment(0), 1, new DateTime(2006, 2, 1), 24, 124, 800);
            _AssertSpecifiedInstallment(myContract.GetInstallment(1), 2, new DateTime(2006, 3, 1), 20, 128, 676);
            _AssertSpecifiedInstallment(myContract.GetInstallment(2), 3, new DateTime(2006, 4, 3), 16, 131, 548);
            _AssertSpecifiedInstallment(myContract.GetInstallment(3), 4, new DateTime(2006, 5, 1), 13, 134, 417);
            _AssertSpecifiedInstallment(myContract.GetInstallment(4), 5, new DateTime(2006, 6, 1), 8, 140, 283);
            _AssertSpecifiedInstallment(myContract.GetInstallment(5), 6, new DateTime(2006, 7, 3), 4, 143, 143);
        }

        [Test]
        public void DecliningRate_FixedPrincipal_GracePeriod()
        {
            LoanProduct package = new LoanProduct
            {
                InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
                LoanType = OLoanTypes.DecliningFixedPrincipal,
                ChargeInterestWithinGracePeriod = true,
                Currency = new Currency { Id = 1 }
            };
            Loan myContract = new Loan(package, 1000, 0.03m, 6, 3, new DateTime(2006, 1, 1), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));

            _AssertSpecifiedInstallment(myContract.GetInstallment(0), 1, new DateTime(2006, 2, 1), 30, 0, 1000);
            _AssertSpecifiedInstallment(myContract.GetInstallment(1), 2, new DateTime(2006, 3, 1), 30, 0, 1000);
            _AssertSpecifiedInstallment(myContract.GetInstallment(2), 3, new DateTime(2006, 4, 3), 30, 0, 1000);
            _AssertSpecifiedInstallment(myContract.GetInstallment(3), 4, new DateTime(2006, 5, 1), 30, 333, 1000);
            _AssertSpecifiedInstallment(myContract.GetInstallment(4), 5, new DateTime(2006, 6, 1), 20, 334, 667);
            _AssertSpecifiedInstallment(myContract.GetInstallment(5), 6, new DateTime(2006, 7, 3), 10, 333, 333);
        }

        [Test]
        public void DecliningRate_FixedInstallment_NoGracePeriod()
        {
            LoanProduct package = new LoanProduct
            {
                InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
                LoanType = OLoanTypes.DecliningFixedInstallments,
                ChargeInterestWithinGracePeriod = true,
                Currency = new Currency { Id = 1 },
                RoundingType = ORoundingType.Approximate
            };
            Loan myContract = new Loan(package, 1000, 0.03m, 6, 0, new DateTime(2006, 1, 1), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));

            _AssertSpecifiedInstallment(myContract.GetInstallment(0), 1, new DateTime(2006, 2, 1), 30, 155, 1000);
            _AssertSpecifiedInstallment(myContract.GetInstallment(1), 2, new DateTime(2006, 3, 1), 25, 160, 845);
            _AssertSpecifiedInstallment(myContract.GetInstallment(2), 3, new DateTime(2006, 4, 3), 21, 163, 685);
            _AssertSpecifiedInstallment(myContract.GetInstallment(3), 4, new DateTime(2006, 5, 1), 16, 169, 522);
            _AssertSpecifiedInstallment(myContract.GetInstallment(4), 5, new DateTime(2006, 6, 1), 11, 173, 353);
            _AssertSpecifiedInstallment(myContract.GetInstallment(5), 6, new DateTime(2006, 7, 3), 5, 180, 180);
        }

        [Test]
        public void DecliningRate_FixedInstallment_NoGracePeriodCheckRounding()
        {
            LoanProduct package = new LoanProduct
            {
                InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
                LoanType = OLoanTypes.DecliningFixedInstallments,
                ChargeInterestWithinGracePeriod = true,
                Currency = new Currency { Id = 1 },
                RoundingType = ORoundingType.Approximate
            };
            Loan myContract = new Loan(package, 1000, 0.02m, 6, 0, new DateTime(2010, 1, 1), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));
           
            _AssertSpecifiedInstallment(myContract.GetInstallment(0), 1, new DateTime(2010, 2, 1), 20, 159, 1000);
            _AssertSpecifiedInstallment(myContract.GetInstallment(1), 2, new DateTime(2010, 3, 1), 17, 161, 841);
            _AssertSpecifiedInstallment(myContract.GetInstallment(2), 3, new DateTime(2010, 4, 1), 14, 165, 680);
            _AssertSpecifiedInstallment(myContract.GetInstallment(3), 4, new DateTime(2010, 5, 3), 10, 169, 515);
            _AssertSpecifiedInstallment(myContract.GetInstallment(4), 5, new DateTime(2010, 6, 1), 7, 171, 346);
            _AssertSpecifiedInstallment(myContract.GetInstallment(5), 6, new DateTime(2010, 7, 1), 4, 175, 175);
        }

        [Test]
        public void DecliningRate_FixedInstallmentRoundingTypeEnd()
        {
            LoanProduct package = new LoanProduct
            {
                InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
                LoanType = OLoanTypes.DecliningFixedInstallments,
                ChargeInterestWithinGracePeriod = true,
                Currency = new Currency { Id = 1 },
                RoundingType = ORoundingType.End
            };
            Loan myContract = new Loan(package, 1000, 0.03m, 6, 0, new DateTime(2006, 1, 1), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));

            _AssertSpecifiedInstallment(myContract.GetInstallment(0), 1, new DateTime(2006, 2, 1), 30, 155, 1000);
            _AssertSpecifiedInstallment(myContract.GetInstallment(1), 2, new DateTime(2006, 3, 1), 25, 160, 845);
            _AssertSpecifiedInstallment(myContract.GetInstallment(2), 3, new DateTime(2006, 4, 3), 21, 164, 685);
            _AssertSpecifiedInstallment(myContract.GetInstallment(3), 4, new DateTime(2006, 5, 1), 16, 169, 521);
            _AssertSpecifiedInstallment(myContract.GetInstallment(4), 5, new DateTime(2006, 6, 1), 11, 174, 352);
            _AssertSpecifiedInstallment(myContract.GetInstallment(5), 6, new DateTime(2006, 7, 3), 5, 178, 178);
        }

        [Test]
        public void DecliningRate_FixedInstallmentRoundingTypeBegin()
        {
            LoanProduct package = new LoanProduct
            {
                InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
                LoanType = OLoanTypes.DecliningFixedInstallments,
                ChargeInterestWithinGracePeriod = true,
                Currency = new Currency { Id = 1 },
                RoundingType = ORoundingType.Begin
            };
            Loan myContract = new Loan(package, 1000, 0.03m, 6, 0, new DateTime(2006, 1, 1), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));

            _AssertSpecifiedInstallment(myContract.GetInstallment(0), 1, new DateTime(2006, 2, 1), 30, 153, 1000);
            _AssertSpecifiedInstallment(myContract.GetInstallment(1), 2, new DateTime(2006, 3, 1), 25, 160, 847);
            _AssertSpecifiedInstallment(myContract.GetInstallment(2), 3, new DateTime(2006, 4, 3), 21, 164, 687);
            _AssertSpecifiedInstallment(myContract.GetInstallment(3), 4, new DateTime(2006, 5, 1), 16, 169, 523);
            _AssertSpecifiedInstallment(myContract.GetInstallment(4), 5, new DateTime(2006, 6, 1), 11, 174, 354);
            _AssertSpecifiedInstallment(myContract.GetInstallment(5), 6, new DateTime(2006, 7, 3), 5, 180, 180);
        }
        [Test]
        public void DecliningRate_FixedPrincipal_NoGracePeriod()
        {
            LoanProduct package = new LoanProduct
            {
                InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
                LoanType = OLoanTypes.DecliningFixedPrincipal,
                ChargeInterestWithinGracePeriod = true,
                Currency = new Currency { Id = 1 }
            };
            Loan myContract = new Loan(package, 1000, 0.03m, 6, 0, new DateTime(2006, 1, 1), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));

            _AssertSpecifiedInstallment(myContract.GetInstallment(0), 1, new DateTime(2006, 2, 1), 30, 167, 1000);
            _AssertSpecifiedInstallment(myContract.GetInstallment(1), 2, new DateTime(2006, 3, 1), 25, 167, 833);
            _AssertSpecifiedInstallment(myContract.GetInstallment(2), 3, new DateTime(2006, 4, 3), 20, 166, 666);
            _AssertSpecifiedInstallment(myContract.GetInstallment(3), 4, new DateTime(2006, 5, 1), 15, 167, 500);
            _AssertSpecifiedInstallment(myContract.GetInstallment(4), 5, new DateTime(2006, 6, 1), 10, 166, 333);
            _AssertSpecifiedInstallment(myContract.GetInstallment(5), 6, new DateTime(2006, 7, 3), 5, 167, 167);
        }

        [Test]
        public void DecliningRate_FixedPrincipal_NoGracePeriod_BiWeekly()
        {
            LoanProduct package = new LoanProduct
            {
                InstallmentType = new InstallmentType(1, "Bi-Weekly", 14, 0),
                LoanType = OLoanTypes.DecliningFixedPrincipal,
                ChargeInterestWithinGracePeriod = true,
                Currency = new Currency { Id = 1 }
            };
            Loan myContract = new Loan(package, 1000, 0.03m, 5, 0, new DateTime(2006, 1, 1), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));

            _AssertSpecifiedInstallment(myContract.GetInstallment(0), 1, new DateTime(2006, 1, 16), 32, 200, 1000);
            _AssertSpecifiedInstallment(myContract.GetInstallment(1), 2, new DateTime(2006, 1, 30), 24, 200, 800);
            _AssertSpecifiedInstallment(myContract.GetInstallment(2), 3, new DateTime(2006, 2, 13), 18, 200, 600);
            _AssertSpecifiedInstallment(myContract.GetInstallment(3), 4, new DateTime(2006, 2, 27), 12, 200, 400);
            _AssertSpecifiedInstallment(myContract.GetInstallment(4), 5, new DateTime(2006, 3, 13), 6, 200, 200);
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

            _AssertSpecifiedInstallment(myContract.GetInstallment(0), 1, new DateTime(2006, 2, 1), 30, 100, 1000);
            _AssertSpecifiedInstallment(myContract.GetInstallment(1), 2, new DateTime(2006, 3, 1), 27, 100, 900);
            _AssertSpecifiedInstallment(myContract.GetInstallment(2), 3, new DateTime(2006, 4, 3), 24, 100, 800);
            _AssertSpecifiedInstallment(myContract.GetInstallment(3), 4, new DateTime(2006, 5, 1), 21, 100, 700);
            _AssertSpecifiedInstallment(myContract.GetInstallment(4), 5, new DateTime(2006, 6, 1), 18, 100, 600);
            _AssertSpecifiedInstallment(myContract.GetInstallment(5), 6, new DateTime(2006, 7, 3), 15, 500, 500);
        }

        [Test]
        public void DecliningRate_ExoticInstallments_NoGracePeriod_RoundingValue()
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
            Loan myContract = new Loan(package, 950, 0.03m, 6, 0, new DateTime(2006, 1, 1), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));

            _AssertSpecifiedInstallment(myContract.GetInstallment(0), 1, new DateTime(2006, 2, 1), 28, 95, 950);
            _AssertSpecifiedInstallment(myContract.GetInstallment(1), 2, new DateTime(2006, 3, 1), 26, 95, 855);
            _AssertSpecifiedInstallment(myContract.GetInstallment(2), 3, new DateTime(2006, 4, 3), 23, 95, 760);
            _AssertSpecifiedInstallment(myContract.GetInstallment(3), 4, new DateTime(2006, 5, 1), 20, 95, 665);
            _AssertSpecifiedInstallment(myContract.GetInstallment(4), 5, new DateTime(2006, 6, 1), 17, 95, 570);
            _AssertSpecifiedInstallment(myContract.GetInstallment(5), 6, new DateTime(2006, 7, 3), 14, 475, 475);
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

            _AssertSpecifiedInstallment(myContract.GetInstallment(0), 1, new DateTime(2006, 2, 1), 30, 0, 1000);
            _AssertSpecifiedInstallment(myContract.GetInstallment(1), 2, new DateTime(2006, 3, 1), 30, 100, 1000);
            _AssertSpecifiedInstallment(myContract.GetInstallment(2), 3, new DateTime(2006, 4, 3), 27, 100, 900);
            _AssertSpecifiedInstallment(myContract.GetInstallment(3), 4, new DateTime(2006, 5, 1), 24, 100, 800);
            _AssertSpecifiedInstallment(myContract.GetInstallment(4), 5, new DateTime(2006, 6, 1), 21, 100, 700);
            _AssertSpecifiedInstallment(myContract.GetInstallment(5), 6, new DateTime(2006, 7, 3), 18, 100, 600);
            _AssertSpecifiedInstallment(myContract.GetInstallment(6), 7, new DateTime(2006, 8, 1), 15, 500, 500);
        }

        [Test]
        public void TestCalculateInstallmentDateWhenNotPublicHolidayOrWeekEnd()
        {
            LoanProduct package = new LoanProduct
            {
                InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
                LoanType = OLoanTypes.DecliningFixedInstallments,
                ChargeInterestWithinGracePeriod = true,
                Currency = new Currency { Id = 1 }
            };
            Loan myContract = new Loan(package, 1000, 0.03m, 6, 0, new DateTime(2006, 1, 1), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));
            Assert.AreEqual(new DateTime(2006, 2, 1), myContract.CalculateInstallmentDate(myContract.StartDate, 1));
        }

        [Test]
        public void TestCalculateInstallmentDateWhenPublicHoliday()
        {
            LoanProduct package = new LoanProduct
            {
                InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
                LoanType = OLoanTypes.DecliningFixedInstallments,
                ChargeInterestWithinGracePeriod = true,
                Currency = new Currency { Id = 1 }
            };
            Loan myContract = new Loan(package, 1000, 0.03m, 6, 0, new DateTime(2006, 11, 25), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));
            Assert.AreEqual(new DateTime(2006, 12, 26), myContract.CalculateInstallmentDate(myContract.StartDate, 1));
        }

        [Test]
        public void TestCalculateInstallmentDateWhenWeekEndDay1IsSaturdayAndWeekEndDay2IsSunday()
        {
            LoanProduct package = new LoanProduct
            {
                InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
                LoanType = OLoanTypes.DecliningFixedInstallments,
                ChargeInterestWithinGracePeriod = true,
                Currency = new Currency { Id = 1 }
            };
            Loan myContract = new Loan(package, 1000, 0.03m, 6, 0, new DateTime(2006, 6, 29), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));
            Assert.AreEqual(new DateTime(2006, 7, 31), myContract.CalculateInstallmentDate(myContract.StartDate, 1));
        }

        [Test]
        public void TestCalculateInstallmentDateWhenWeekEndDay2IsSunday()
        {
            LoanProduct package = new LoanProduct
            {
                InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
                LoanType = OLoanTypes.DecliningFixedInstallments,
                ChargeInterestWithinGracePeriod = true,
                Currency = new Currency { Id = 1 }
            };
            Loan myContract = new Loan(package, 1000, 0.03m, 6, 0, new DateTime(2006, 6, 30), new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()));
            Assert.AreEqual(new DateTime(2006, 7, 31), myContract.CalculateInstallmentDate(myContract.StartDate, 1));
        }
    }
}
