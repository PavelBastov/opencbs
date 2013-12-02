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
using System.Linq;
using NUnit.Framework;
using OpenCBS.CoreDomain;
using OpenCBS.CoreDomain.Accounting;
using OpenCBS.CoreDomain.Contracts.Loans;
using OpenCBS.CoreDomain.Contracts.Loans.Installments;
using OpenCBS.CoreDomain.Contracts.Loans.LoanRepayment;
using OpenCBS.CoreDomain.Contracts.Savings;
using OpenCBS.CoreDomain.Events;
using OpenCBS.CoreDomain.Events.Saving;
using OpenCBS.CoreDomain.FundingLines;
using OpenCBS.CoreDomain.Products;
using OpenCBS.Enums;
using OpenCBS.Shared;
using OpenCBS.Shared.Settings;

namespace OpenCBS.Test.CoreDomain.Contracts.Loans
{
    [TestFixture]
    public class TestLoan
    {
        private Loan _myContract;
        private ApplicationSettings _generalSettings;
        private ProvisionTable _provisionningTable;
        private NonWorkingDateSingleton _nonWorkingDate;
        private static DateTime _contractStartDate = new DateTime(2006, 1, 1);
       
        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            _generalSettings = ApplicationSettings.GetInstance("");
            _generalSettings.DeleteAllParameters();
            _generalSettings.AddParameter(OGeneralSettings.CALCULATIONLATEFEESDURINGPUBLICHOLIDAYS, true);
            _generalSettings.AddParameter(OGeneralSettings.DONOTSKIPWEEKENDSININSTALLMENTSDATE, false);
            _generalSettings.AddParameter(OGeneralSettings.INCREMENTALDURINGDAYOFF, true);
            _generalSettings.AddParameter(OGeneralSettings.PAYFIRSTINSTALLMENTREALVALUE, true);
            _generalSettings.AddParameter(OGeneralSettings.BAD_LOAN_DAYS, "180");
            _generalSettings.AddParameter(OGeneralSettings.STOP_WRITEOFF_PENALTY, true);

            _provisionningTable = ProvisionTable.GetInstance(new User());
            _provisionningTable.ProvisioningRates = new List<ProvisioningRate>();
            _provisionningTable.Add(new ProvisioningRate { Number = 1, NbOfDaysMin = 0, NbOfDaysMax = 30, ProvisioningValue = 10 });
            _provisionningTable.Add(new ProvisioningRate { Number = 2, NbOfDaysMin = 31, NbOfDaysMax = 60, ProvisioningValue = 25 });
            _provisionningTable.Add(new ProvisioningRate { Number = 3, NbOfDaysMin = 61, NbOfDaysMax = 90, ProvisioningValue = 50 });
            _provisionningTable.Add(new ProvisioningRate { Number = 4, NbOfDaysMin = 91, NbOfDaysMax = 180, ProvisioningValue = 75 });
            _provisionningTable.Add(new ProvisioningRate { Number = 5, NbOfDaysMin = 181, NbOfDaysMax = 365, ProvisioningValue = 100 });
            _provisionningTable.Add(new ProvisioningRate { Number = 6, NbOfDaysMin = 366, NbOfDaysMax = 99999, ProvisioningValue = 100 });

            _nonWorkingDate = NonWorkingDateSingleton.GetInstance("");
            _nonWorkingDate.PublicHolidays = new Dictionary<DateTime, string>();
            _nonWorkingDate.WeekEndDay1 = 0;
            _nonWorkingDate.WeekEndDay2 = 1;
        }

        [SetUp]
        public void SetUp()
        {
            LoanProduct package = new LoanProduct
                                      {
                                          Code = "IL",
                                          InstallmentType = new InstallmentType(1, "Monthly", 0, 1),
                                          LoanType = OLoanTypes.Flat,
                                          ChargeInterestWithinGracePeriod = true,
                                          Currency = new Currency { Id = 1, UseCents = true }
                                      };
            _myContract = new Loan(package, 1000, 0.03m, 6, 1, _contractStartDate, new User(), _generalSettings, _nonWorkingDate, _provisionningTable, ChartOfAccounts.GetInstance(new User()));

        }

        [Test]
        public void Get_Set_Product()
        {
            _myContract.Product = new LoanProduct{Name = "Test", Currency = new Currency{Id = 1}};
            Assert.AreEqual("Test", _myContract.Product.Name);
        }

        [Test]
        public void Get_Set_Schedule()
        {
            Loan loan = new Loan {InstallmentList = new List<Installment>{new Installment(),new Installment(),new Installment()}};
            Assert.AreEqual(3, loan.InstallmentList.Count);
        }

        [Test]
        public void Get_Set_Disbursed()
        {
            _myContract.Disbursed = true;
            Assert.IsTrue(_myContract.Disbursed);
        }

        [Test]
        public void Get_Set_GracePeriod()
        {
            _myContract.GracePeriod = 4;
            Assert.AreEqual(4, _myContract.GracePeriod.Value);
        }

        [Test]
        public void GetEventsByType()
        {
            _myContract.Events = new EventStock
                                     {
                                         new LoanDisbursmentEvent(),
                                         new RepaymentEvent(),
                                         new WriteOffEvent(),
                                         new LoanDisbursmentEvent()
                                     };
;
            Assert.AreEqual(1, _myContract.Events.GetEventsByType(typeof(WriteOffEvent)).Count);
            Assert.AreEqual(2, _myContract.Events.GetEventsByType(typeof(LoanDisbursmentEvent)).Count);
        }

        [Test]
        public void Get_Set_NonRepaymentPenaltiesOnInitalAmount()
        {
            _myContract.NonRepaymentPenalties.InitialAmount = 2.5;
            Assert.AreEqual(2.5, _myContract.NonRepaymentPenalties.InitialAmount);
        }

        [Test]
        public void Get_Set_AnticipatedTotalRepaymentPenalties()
        {
            _myContract.AnticipatedTotalRepaymentPenalties = 1.2;
            Assert.AreEqual(1.2, _myContract.AnticipatedTotalRepaymentPenalties);
        }

        [Test]
        public void Get_Set_EntryFees()
        {
            LoanEntryFee entryFee = new LoanEntryFee();
            entryFee.FeeValue = (decimal) 3.2;
            _myContract.LoanEntryFeesList = new List<LoanEntryFee>();
            _myContract.LoanEntryFeesList.Add(entryFee);
            Assert.AreEqual(3.2, _myContract.LoanEntryFeesList[0].FeeValue);
        }

        [Test]
        public void Get_Set_WriteOff()
        {
            _myContract.WrittenOff = true;
            Assert.IsTrue(_myContract.WrittenOff);
        }

        [Test]
        public void Get_Set_FundingLine()
        {
            _myContract.FundingLine = new FundingLine("AFD130", true);
            Assert.AreEqual("AFD130", _myContract.FundingLine.Name);
        }

        [Test]
        public void Get_Set_Amount()
        {
            _myContract.Amount = 155.50m;
            Assert.AreEqual(155.50m, _myContract.Amount.Value);
        }

        [Test]
        public void Get_Set_InterestRate()
        {
            _myContract.InterestRate = 20;
            Assert.AreEqual(20, _myContract.InterestRate);
        }

        [Test]
        public void Get_Set_NumberOfInstallments()
        {
            _myContract.NbOfInstallments = 12;
            Assert.AreEqual(12, _myContract.NbOfInstallments);
        }

        [Test]
        public void Get_Set_InstallmentType()
        {
            _myContract.InstallmentType = new InstallmentType { Name = "weekly", NbOfDays = 7 };
            Assert.AreEqual("weekly",_myContract.InstallmentType.Name);
            Assert.AreEqual(7,_myContract.InstallmentType.NbOfDays);
        }

        [Test]
        public void Get_Set_Events()
        {
            _myContract.Events = new EventStock {new LoanDisbursmentEvent()};
            Assert.AreEqual(1, _myContract.Events.GetNumberOfEvents);
        }

        /*[Test]
        public void Get_Set_SavingsIsMandatory()
        {
            _myContract.IsSavingsMandatory = true;
            Assert.AreEqual(true, _myContract.IsSavingsMandatory);
        }*/

        [Test]
        public void GetEvents_RankLowerThanNumberOfEvents()
        {
            _myContract.Events = new EventStock { new LoanDisbursmentEvent() };
            Assert.AreEqual("LODE", _myContract.Events.GetEvent(0).Code);
        }

        [Test]
        public void GetEvents_RankGreaterThanNumberOfEvents()
        {
            _myContract.Events = new EventStock { new LoanDisbursmentEvent() };
            Assert.IsNull(_myContract.Events.GetEvent(2));
        }

        [Test]
        public void Get_Set_Code()
        {
            _myContract.Code = "CRED_00001";
            Assert.AreEqual("CRED_00001", _myContract.Code);
        }

        [Test]
        public void Get_Set_Id()
        {
            _myContract.Id = 1;
            Assert.AreEqual(1, _myContract.Id);
        }

        [Test]
        public void Get_Set_Close()
        {
            _myContract.Closed = true;
            Assert.AreEqual(true, _myContract.Closed);
        }

        [Test]
        public void Get_Set_StartDate()
        {
            DateTime date = new DateTime(2005, 12, 25);
            _myContract.StartDate = date;
            Assert.AreEqual(date, _myContract.StartDate);
        }

        [Test]
        public void Get_Set_CreationDate()
        {
            DateTime date = new DateTime(2005, 12, 25);
            _myContract.CreationDate = date;
            Assert.AreEqual(date, _myContract.CreationDate);
        }

        [Test]
        public void Get_Set_CloseDate()
        {
            DateTime date = new DateTime(2005, 12, 25);
            _myContract.CloseDate = date;
            Assert.AreEqual(date, _myContract.CloseDate);
        }

        [Test]
        public void Get_Set_BranchCode()
        {
            _myContract.BranchCode = "DU";
            Assert.AreEqual("DU", _myContract.BranchCode);
        }

        [Test]
        public void Get_Set_Creditcommittee()
        {
            Loan loan = new Loan {CreditCommitteeCode = "TestCode"};
            Assert.AreEqual("TestCode",loan.CreditCommitteeCode);
        }
        
        [Test]
        public void Closed_WhenContractClosed()
        {
            _myContract.Disburse(new DateTime(2006, 1, 1), true, true);
            _myContract.Repay(1, new DateTime(2006, 2, 1), 1180, true, true);

            Assert.AreEqual(6,_myContract.NbOfInstallments);
            Assert.AreEqual(true,_myContract.AllInstallmentsRepaid);
            Assert.AreEqual(true, _myContract.Closed);
        }

        [Test]
        public void GetPaidFees_NoLatePenalties_NoCommissions()
        {

            _myContract.NonRepaymentPenalties = new NonRepaymentPenalties(0.03, 0, 0, 0);
            _myContract.Disburse(new DateTime(2006, 1, 1), true, true);
            _myContract.Repay(1, new DateTime(2006, 2, 1), 500, true, true); 

            Assert.AreEqual(0m,_myContract.GetPaidFees().Value);
        }

        [Test]
        public void GetEntryFees_DisableFees_True()
        {
            _myContract.Disburse(new DateTime(2006, 1, 1), true, true);

            Assert.AreEqual(null, _myContract.GetEntryFees());
        }

        [Test]
        public void GetEntryFees_DisableFees_False()
        {
            _myContract.Disburse(new DateTime(2006, 1, 1), true, false);
            EntryFee fee = new EntryFee();
            fee.Value = 3;
            fee.IsRate = true;
            fee.Id = 21;
            LoanEntryFee loanEntryFee = new LoanEntryFee();
            loanEntryFee.FeeValue = 3;
            loanEntryFee.ProductEntryFee = fee;
            loanEntryFee.ProductEntryFeeId = 21;
            _myContract.LoanEntryFeesList = new List<LoanEntryFee>();
            _myContract.LoanEntryFeesList.Add(loanEntryFee);
            Assert.AreEqual(30m, _myContract.GetEntryFees()[0].Value);
        }

        [Test]
        public void GetPaidFees_NoLatePenalties_Commissions()
        {
            _myContract.NonRepaymentPenalties = new NonRepaymentPenalties(0.03, 0, 0, 0);
            EntryFee fee = new EntryFee();
            fee.Value = 3;
            fee.IsRate = true;
            fee.Id = 21;
            LoanEntryFee loanEntryFee = new LoanEntryFee();
            loanEntryFee.FeeValue = 3;
            loanEntryFee.ProductEntryFee = fee;
            loanEntryFee.ProductEntryFeeId = 21;
            _myContract.LoanEntryFeesList = new List<LoanEntryFee>();
            _myContract.LoanEntryFeesList.Add(loanEntryFee);
            _myContract.Disburse(new DateTime(2006, 1, 1), true, false);
            _myContract.Repay(1, new DateTime(2006, 2, 1), 500, true, true); 

            Assert.AreEqual(30m,_myContract.GetPaidFees().Value);
        }

        [Test]
        public void GetPaidFees_LatePenalties_Commissions()
        {
            _myContract.NonRepaymentPenalties = new NonRepaymentPenalties(0.003, 0, 0, 0);
            
            EntryFee fee = new EntryFee();
            fee.Value = 3;
            fee.IsRate = true;
            fee.Id = 21;
            LoanEntryFee loanEntryFee = new LoanEntryFee();
            loanEntryFee.FeeValue = 3;
            loanEntryFee.ProductEntryFee = fee;
            loanEntryFee.ProductEntryFeeId = 21;
            _myContract.LoanEntryFeesList = new List<LoanEntryFee>();
            _myContract.LoanEntryFeesList.Add(loanEntryFee);

            _myContract.Disburse(new DateTime(2006, 1, 1), true, false); //commission:30
            _myContract.Repay(1, new DateTime(2006, 3, 1), 500, false, true); //late penalties:84

            Assert.AreEqual(114m, _myContract.GetPaidFees().Value);
        }

        [Test]
        public void GetPaidInterest_NoRepayment()
        {
            _myContract.Disburse(new DateTime(2006, 1, 1), true, false);

            Assert.AreEqual(0m,_myContract.GetPaidInterest().Value);
        }

        [Test]
        public void GetPaidInterest_Repayment()
        {
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.ACCOUNTINGPROCESS, OAccountingProcesses.Accrual);
            _myContract.NonRepaymentPenalties = new NonRepaymentPenalties(0.003, 0, 0, 0);
            _myContract.Disburse(new DateTime(2006, 1, 1), true, false);
            _myContract.Repay(1, new DateTime(2006, 3, 1), 500, false, false);

            Assert.AreEqual(60, _myContract.GetPaidInterest().Value);
        }

        [Test]
        public void GetPaidCapital_NoRepayment()
        {
            _myContract.Disburse(new DateTime(2006, 1, 1), true, false);

            Assert.AreEqual(0m, _myContract.GetPaidPrincipal().Value);
        }

        [Test]
        public void GetPaidCapital_Repayment()
        {
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.ACCOUNTINGPROCESS, OAccountingProcesses.Accrual);
            
            _myContract.NonRepaymentPenalties = new NonRepaymentPenalties(0.003, 0, 0, 0);
            _myContract.Disburse(new DateTime(2006, 1, 1), true, false);
            _myContract.Repay(1, new DateTime(2006, 3, 1), 500, false, false);

            Assert.AreEqual(356, _myContract.GetPaidPrincipal().Value);
        }


        [Test]
        public void GetPaidLatePenalties_NoRepayment_NoLate()
        {
            _myContract.NonRepaymentPenalties = new NonRepaymentPenalties(0.002, 0.003, 0, 0);
            _myContract.Disburse(new DateTime(2006, 1, 1), true, false);

            Assert.AreEqual(0m, _myContract.GetPaidLatePenalties().Value);
        }

        [Test]
        public void GetUnpaidLatePenalties_NoRepayment_NoLate()
        {
            _myContract.NonRepaymentPenalties = new NonRepaymentPenalties(0.002, 0.003, 0, 0);
            _myContract.Disburse(new DateTime(2006, 1, 1), true, false);

            Assert.AreEqual(0m, _myContract.GetUnpaidLatePenalties(new DateTime(2006, 2, 1)).Value);
        }

        [Test]
        public void GetUnpaidLatePenalties_NoRepayment_Late_InitialAmount_Olb()
        {
            _myContract.NonRepaymentPenalties = new NonRepaymentPenalties(0.002, 0.003, 0, 0);
            _myContract.Disburse(new DateTime(2006, 1, 1), true, false);

            Assert.AreEqual(20m, _myContract.GetUnpaidLatePenalties(new DateTime(2006, 2, 5)).Value); //4 days late
        }

        [Test]
        public void GetUnpaidLatePenalties_Repayment_NoLate1()
        {
            _myContract.NonRepaymentPenalties = new NonRepaymentPenalties(0.002, 0.003, 0, 0);
            _myContract.Disburse(new DateTime(2006, 1, 1), true, false);
            _myContract.Repay(1, new DateTime(2006, 2, 1), 30, false, false);

            Assert.AreEqual(0m, _myContract.GetUnpaidLatePenalties(new DateTime(2006, 2, 1)).Value);
        }

        [Test]
        public void GetUnpaidLatePenalties_PartiallyRepayment_Late()
        {
            _myContract.NonRepaymentPenalties = new NonRepaymentPenalties(0.002, 0.003, 0, 0);
            _myContract.Disburse(new DateTime(2006, 1, 1), true, false);
            _myContract.Repay(1, new DateTime(2006, 2, 1), 10, false, false);

            Assert.AreEqual(20m, _myContract.GetUnpaidLatePenalties(new DateTime(2006, 2, 5)).Value);
        }

        [Test]
        public void GetPaidLatePenalties_PartiallyRepayment_Late()
        {
            _myContract.NonRepaymentPenalties = new NonRepaymentPenalties(0.002, 0, 0, 0);
            _myContract.Disburse(new DateTime(2006, 1, 1), true, false);
            
            _myContract.Repay(1, new DateTime(2006, 2, 5), 10, false, true);

            Assert.AreEqual(8m, _myContract.GetPaidLatePenalties().Value);
        }

        [Test]
        public void GetUnpaidLatePenalties_PartiallyRepayment_NoLate()
        {
            _myContract.NonRepaymentPenalties = new NonRepaymentPenalties(0.002, 0.003, 0, 0);
            _myContract.Disburse(new DateTime(2006, 1, 1), true, false);
            _myContract.Repay(1, new DateTime(2006, 2, 1), 10, false, false);

            Assert.AreEqual(0m, _myContract.GetUnpaidLatePenalties(new DateTime(2006, 2, 1)).Value);
        }

        [Test]
        public void GetUnpaidLatePenalties_NoRepayment_Late_LessThan1Month()
        {
            _myContract.NonRepaymentPenalties = new NonRepaymentPenalties(0.002, 0.003, 0, 0);
            _myContract.Disburse(new DateTime(2006, 1, 1), true, false);

            Assert.AreEqual(45, _myContract.GetUnpaidLatePenalties(new DateTime(2006, 2, 10)).Value);
        }

        [Test]
        public void GetPaidLatePenalties_NoRepayment_Late_LessThan1Month()
        {
            _myContract.NonRepaymentPenalties = new NonRepaymentPenalties(0.002, 0.003, 0, 0);
            _myContract.Disburse(new DateTime(2006, 1, 1), true, false);

            Assert.AreEqual(0, _myContract.GetPaidLatePenalties().Value);
        }

        [Test]
        public void GetUnpaidLatePenalties_NoRepayment_Late_MoreThan1Month()
        {
            _myContract.NonRepaymentPenalties = new NonRepaymentPenalties(0.002, 0.003, 0, 0);
            _myContract.Disburse(new DateTime(2006, 1, 1), true, false);

            Assert.AreEqual(185, _myContract.GetUnpaidLatePenalties(new DateTime(2006, 3, 10)).Value);
        }

        [Test]
        public void GetPaidLatePenalties_NoRepayment_Late_MoreThan1Month()
        {
            _myContract.NonRepaymentPenalties = new NonRepaymentPenalties(0.002, 0.003, 0, 0);
            _myContract.Disburse(new DateTime(2006, 1, 1), true, false);

            Assert.AreEqual(0, _myContract.GetPaidLatePenalties().Value);
        }

        [Test]
        public void GetPastDueDays_NoRepayment_NoLate()
        {
            _myContract.Disburse(new DateTime(2006, 1, 1), true, false);

            Assert.AreEqual(0, _myContract.GetPastDueDays(new DateTime(2006, 2, 1)));
        }

        [Test]
        public void GetPastDueDays_Repayment_NoLate1()
        {
            _myContract.Disburse(new DateTime(2006, 1, 1), true, false);
            _myContract.Repay(1, new DateTime(2006, 2, 1), 30, false, false);

            Assert.AreEqual(0, _myContract.GetPastDueDays(new DateTime(2006, 2, 1)));
        }

        [Test]
        public void GetPastDueDays_PartiallyRepayment_Late()
        {
            _myContract.Disburse(new DateTime(2006, 1, 1), true, false);
            _myContract.Repay(1, new DateTime(2006, 2, 1), 10, false, false);

            Assert.AreEqual(4, _myContract.GetPastDueDays(new DateTime(2006, 2, 5)));
        }

        [Test]
        public void GetPastDueDays_PartiallyRepayment_NoLate()
        {
            _myContract.Disburse(new DateTime(2006, 1, 1), true, false);
            _myContract.Repay(1, new DateTime(2006, 2, 1), 10, false, false);

            Assert.AreEqual(0, _myContract.GetPastDueDays(new DateTime(2006, 2, 1)));
        }

        [Test]
        public void GetPastDueDays_Repayment_NoLate2()
        {
            _myContract.Disburse(new DateTime(2006, 1, 1), true, false);
            _myContract.Repay(1, new DateTime(2006, 2, 1), 30, false, false);

            Assert.AreEqual(0, _myContract.GetPastDueDays(new DateTime(2006, 2, 6)));
        }

        [Test]
        public void GetPastDueDays_NoRepayment_Late_LessThan1Month()
        {
            _myContract.Disburse(new DateTime(2006, 1, 1), true, false);

            Assert.AreEqual(9, _myContract.GetPastDueDays(new DateTime(2006, 2, 10)));
        }

        [Test]
        public void GetPastDueDays_NoRepayment_Late_MoreThan1Month()
        {
            _myContract.Disburse(new DateTime(2006, 1, 1), true, false);

            Assert.AreEqual(37, _myContract.GetPastDueDays(new DateTime(2006, 3, 10)));
        }

        [Test]
        public void GetUnpaidInterest_NoRepayment()
        {
            _myContract.Disburse(new DateTime(2006, 1, 1), true, false);

            Assert.AreEqual(60, _myContract.GetUnpaidInterest(new DateTime(2006, 3, 1)).Value);
        }

        [Test]
        public void GetUnpaidInterest_PartiallyRepayment()
        {
            _myContract.Disburse(new DateTime(2006, 1, 1), true, false);
            _myContract.Repay(1, new DateTime(2006, 2, 1), 10, false, false);

            Assert.AreEqual(50, _myContract.GetUnpaidInterest(new DateTime(2006, 3, 1)).Value);
        }

        [Test]
        public void GetUnpaidInterest_Repayment()
        {
            _myContract.Disburse(new DateTime(2006, 1, 1), true, false);
            _myContract.Repay(1, new DateTime(2006, 2, 1), 30, false, false);

            Assert.AreEqual(0, _myContract.GetUnpaidInterest(new DateTime(2006, 2, 1)).Value);
        }

        [Test]
        public void CalculatePastDueSinceLastRepayment()
        {
            _myContract.Disburse(new DateTime(2006, 1, 1), true, false);

            Assert.AreEqual(0, _myContract.CalculatePastDueSinceLastRepayment(new DateTime(2006, 2, 1)));
            Assert.AreEqual(3, _myContract.CalculatePastDueSinceLastRepayment(new DateTime(2006, 2, 4)));
            Assert.AreEqual(28, _myContract.CalculatePastDueSinceLastRepayment(new DateTime(2006, 3, 1)));

            _myContract.Repay(1, new DateTime(2006, 2, 3), 30, true, false); //repay tottaly the first installment

            Assert.AreEqual(0, _myContract.CalculatePastDueSinceLastRepayment(new DateTime(2006, 2, 4)));
            Assert.AreEqual(3, _myContract.CalculatePastDueSinceLastRepayment(new DateTime(2006, 3, 4)));

            _myContract.Repay(2, new DateTime(2006, 3, 1), 200, true, false); //repay partially the second installment (200 instead of 230)

            Assert.AreEqual(0, _myContract.CalculatePastDueSinceLastRepayment(new DateTime(2006, 3, 1)));
            Assert.AreEqual(3, _myContract.CalculatePastDueSinceLastRepayment(new DateTime(2006, 3, 4)));

            _myContract.Repay(2, new DateTime(2006, 3, 1), 30, true, false); //repay tottaly the second installment

            Assert.AreEqual(0, _myContract.CalculatePastDueSinceLastRepayment(new DateTime(2006, 3, 1)));
            Assert.AreEqual(0, _myContract.CalculatePastDueSinceLastRepayment(new DateTime(2006, 3, 4)));
        }

        [Test]
        public void GetFirstUnpaidInstallment()
        {
            _myContract.NonRepaymentPenalties = new NonRepaymentPenalties(0.001, 0, 0, 0);
            _myContract.ClientType = OClientTypes.Person;

            // Disburse and check if the first unpaid is number 1
            _myContract.Disburse(new DateTime(2006, 1, 1), true, false);
            Assert.AreEqual(_myContract.GetFirstUnpaidInstallment().Number, 1);

            // Repay the first installment and check if the first unpaid is number 2
            _myContract.Repay(1, new DateTime(2006, 2, 1), 30, true, true);
            Assert.AreEqual(_myContract.GetFirstUnpaidInstallment().Number, 2);

            // Fully repay and make sure there is no unpaid installments
            _myContract.Repay(2, new DateTime(2006, 3, 1), 1150, true, true);
            Assert.IsNull(_myContract.GetFirstUnpaidInstallment());
        }

        [Test]
        public void CalculateDuePenaltiesForInstallment_LateInterest()
        {
            _generalSettings.UpdateParameter(OGeneralSettings.CALCULATIONLATEFEESDURINGPUBLICHOLIDAYS, true);

            _myContract.NonRepaymentPenalties = new NonRepaymentPenalties(0, 0, 0, 0.01);
            _myContract.Disburse(new DateTime(2006, 1, 1), true, true);

            Assert.AreEqual(_myContract.CalculateDuePenaltiesForInstallment(1, new DateTime(2006, 2, 1)), 0m);
            // Calculate penalties for 10 late days (30 * 0.01 * 10 = 3)
            Assert.AreEqual(_myContract.CalculateDuePenaltiesForInstallment(1, new DateTime(2006, 2, 11)), 3m);
        }

        [Test]
        public void CalculateDuePenaltiesForInstallment_LatePrincipal()
        {
            _generalSettings.UpdateParameter(OGeneralSettings.CALCULATIONLATEFEESDURINGPUBLICHOLIDAYS, true);

            _myContract.NonRepaymentPenalties = new NonRepaymentPenalties(0, 0, 0.01, 0);
            _myContract.Disburse(new DateTime(2006, 1, 1), true, true);

            // Calculate penalties for the 1st installment for 10 late days - zero due to grace period
            Assert.AreEqual(_myContract.CalculateDuePenaltiesForInstallment(1, new DateTime(2006, 2, 11)), 0m);
            // Calculate penalties for the 2nd installment for 10 late days (200 * 0.01 * 10 = 20)
            Assert.AreEqual(_myContract.CalculateDuePenaltiesForInstallment(2, new DateTime(2006, 3, 11)), 20m);
        }

        [Test] 
        public void CalculateInsuranceAmount()
        {
            _myContract.Insurance = (decimal)0.7;
            _myContract.Disburse(new DateTime(2006, 1, 1), true, true);
            //Amount is 1000, insurance rate is 0.7%, so Insurance value should be 1000*0.07/100=7
            Assert.AreEqual(7, _myContract.Events.GetCreditInsuranceEvent().Commission.Value);
            //Principal should be always zero
            Assert.AreEqual(0, _myContract.Events.GetCreditInsuranceEvent().Principal.Value);
            Assert.AreEqual("LCIE", _myContract.Events.GetCreditInsuranceEvent().Code);
        }

        [Test]
        public void SavingBlockEventOnDisburse()
        {
            SavingsBookProduct savingsProduct = new SavingsBookProduct
            {
                Id = 1,
                Name = "SavingProduct",
                Code = "P123",
                InitialAmountMin = 1000,
                InitialAmountMax = 2000,
                DepositMin = 250,
                DepositMax = 400,
                WithdrawingMin = 400,
                WithdrawingMax = 450,
                TransferMin = 100,
                TransferMax = 300,
                InterestRateMin = 0.12,
                InterestRateMax = 0.20,
                BalanceMin = 1000,
                BalanceMax = 2000,
                InterestBase = OSavingInterestBase.Daily,
                InterestFrequency = OSavingInterestFrequency.EndOfMonth,
                EntryFeesMin = 10,
                EntryFeesMax = 20,
                Currency = new Currency { Id = 1 },
                WithdrawFeesType = OSavingsFeesType.Flat,
                FlatWithdrawFeesMin = 1,
                FlatWithdrawFeesMax = 5,
                TransferFeesType = OSavingsFeesType.Flat,
                FlatTransferFeesMin = 1,
                FlatTransferFeesMax = 5,
                DepositFeesMin = 1,
                DepositFeesMax = 5,
                ManagementFeeFreq = new InstallmentType { Id = 1, Name = "Monthly", NbOfDays = 0, NbOfMonths = 1 },
                AgioFeesFreq = new InstallmentType { Id = 2, Name = "Daily", NbOfDays = 1, NbOfMonths = 0 }
            };

            SavingBookContract compulsorySaving = new SavingBookContract(ApplicationSettings.GetInstance(""),  new User())
            {
                CreationDate = _contractStartDate,
                InterestRate = 0.13,
                Product = savingsProduct
            };

            _myContract.CompulsorySavings = compulsorySaving;
            _myContract.CompulsorySavingsPercentage = 5;
            _myContract.Disburse(_contractStartDate, true, true);

            IEnumerable<SavingEvent> savingBlockEvents =
                compulsorySaving.Events.Where(e => e.Code == OSavingEvents.BlockCompulsarySavings);
            
            Assert.AreEqual(1, savingBlockEvents.Count());
            SavingBlockCompulsarySavingsEvent savingBlockEvent = savingBlockEvents.First() as SavingBlockCompulsarySavingsEvent;

            Assert.IsNotNull(savingBlockEvent);
            Assert.IsTrue(!savingBlockEvent.Deleted);
            Assert.AreEqual(50m, savingBlockEvent.Amount.Value);
        }

        [Test]
        public void SavingUnblockEventOnLoanRepayment()
        {
            SavingsBookProduct savingsProduct = new SavingsBookProduct
            {
                Id = 1,
                Name = "SavingProduct",
                Code = "P123",
                InitialAmountMin = 1000,
                InitialAmountMax = 2000,
                DepositMin = 250,
                DepositMax = 400,
                WithdrawingMin = 400,
                WithdrawingMax = 450,
                TransferMin = 100,
                TransferMax = 300,
                InterestRateMin = 0.12,
                InterestRateMax = 0.20,
                BalanceMin = 1000,
                BalanceMax = 2000,
                InterestBase = OSavingInterestBase.Daily,
                InterestFrequency = OSavingInterestFrequency.EndOfMonth,
                EntryFeesMin = 10,
                EntryFeesMax = 20,
                Currency = new Currency { Id = 1 },
                WithdrawFeesType = OSavingsFeesType.Flat,
                FlatWithdrawFeesMin = 1,
                FlatWithdrawFeesMax = 5,
                TransferFeesType = OSavingsFeesType.Flat,
                FlatTransferFeesMin = 1,
                FlatTransferFeesMax = 5,
                DepositFeesMin = 1,
                DepositFeesMax = 5,
                ManagementFeeFreq = new InstallmentType { Id = 1, Name = "Monthly", NbOfDays = 0, NbOfMonths = 1 },
                AgioFeesFreq = new InstallmentType { Id = 2, Name = "Daily", NbOfDays = 1, NbOfMonths = 0 }
            };

            SavingBookContract compulsorySaving = new SavingBookContract(ApplicationSettings.GetInstance(""),  new User())
            {
                CreationDate = _contractStartDate,
                InterestRate = 0.13,
                Product = savingsProduct
            };

            _myContract.CompulsorySavings = compulsorySaving;
            _myContract.CompulsorySavingsPercentage = 5;
            _myContract.Disburse(_contractStartDate, true, true);

            _myContract.Repay(1, _contractStartDate, 1180, false, true); //Totall repayment
            Assert.IsTrue(compulsorySaving.Events.Any(e => !e.Deleted && e.Code == OSavingEvents.UnblockCompulsorySavings));
        }

        [Test]
        public void CalculateCreditInsurancePremiumForATR()
        {
            _myContract.Insurance = 2;
            _myContract.Disburse(new DateTime(2006, 1, 1), true, true);
            //number of installments=6, insuranceAmount should be 1000*2/100=20
            //we do ATR at the 4th installment
            //Commisions = (6+5+4+3)/(1+2+3+4+5+6)*20=0.8571*20=17.14
            //Premium = 20-17.14 = 2.86
            Assert.AreEqual(17.14, _myContract.GenerateCreditInsuranceEvent(4).Commission.Value);
            Assert.AreEqual(2.86, _myContract.GenerateCreditInsuranceEvent(4).Principal.Value);
            Assert.AreEqual("LCIP", _myContract.GenerateCreditInsuranceEvent(4).Code);
        }

        [Test]
        public void CalculateInsuranceAmountForWriteOff()
        {
            _myContract.Insurance = 2;
            _myContract.Disburse(new DateTime(2006, 1, 1), true, true);
            Assert.AreEqual(20d,_myContract.GenerateCreditInsuranceEventForWriteOff(DateTime.Today).Commission.Value );
            Assert.AreEqual(0,_myContract.GenerateCreditInsuranceEventForWriteOff(DateTime.Today).Principal.Value);
            Assert.AreEqual("LCIW", _myContract.GenerateCreditInsuranceEventForWriteOff(DateTime.Today).Code);
        }

        [Test]
        public void CalculateDuePenaltiesForInstallment_LateInitialAmount()
        {
            _generalSettings.UpdateParameter(OGeneralSettings.CALCULATIONLATEFEESDURINGPUBLICHOLIDAYS, true);

            _myContract.NonRepaymentPenalties = new NonRepaymentPenalties(0.01, 0, 0, 0);
            _myContract.Disburse(new DateTime(2006, 1, 1), true, true);

            // Calculate penalties for the 1st installment for 10 late days (1000 * 0.01 * 10 = 100)
            Assert.AreEqual(_myContract.CalculateDuePenaltiesForInstallment(1, new DateTime(2006, 2, 11)), 100m);
            // Calculate penalties for the 2nd installment for 10 late days
            // Should be zero, because at this point this kind of penalty is discarded
            Assert.AreEqual(_myContract.CalculateDuePenaltiesForInstallment(2, new DateTime(2006, 3, 11)), 0m);
        }

        [Test]
        public void CalculateDuePenaltiesForInstallment_LateOlb()
        {
            _generalSettings.UpdateParameter(OGeneralSettings.CALCULATIONLATEFEESDURINGPUBLICHOLIDAYS, true);

            _myContract.NonRepaymentPenalties = new NonRepaymentPenalties(0, 0.01, 0, 0);
            _myContract.Disburse(new DateTime(2006, 1, 1), true, true);
            _myContract.Repay(1, new DateTime(2006, 2, 1), 30, false, true);
            _myContract.Repay(2, new DateTime(2006, 3, 1), 230, false, true);
            // Calculate penalties for the 3rd installment for 10 late days (800 * 0.01 * 10 = 80)
            Assert.AreEqual(_myContract.CalculateDuePenaltiesForInstallment(3, new DateTime(2006, 4, 11)), 80m);
        }

        [Test]
        public void CalculateDuePenaltiesForInstallment_IgnoreWeekend()
        {
            _generalSettings.UpdateParameter(OGeneralSettings.CALCULATIONLATEFEESDURINGPUBLICHOLIDAYS, false);
            _generalSettings.UpdateParameter(OGeneralSettings.WEEKENDDAY1, 6);
            _generalSettings.UpdateParameter(OGeneralSettings.WEEKENDDAY2, 0);

            _myContract.NonRepaymentPenalties = new NonRepaymentPenalties(0.01, 0.01, 0.01, 0.01);
            _myContract.Disburse(new DateTime(2006, 1, 1), true, true);
            _myContract.Repay(1, new DateTime(2006, 2, 1), 30, false, true);
            _myContract.Repay(2, new DateTime(2006, 3, 1), 230, false, true);
            // Calculate penalties
            // Days = 7 (10 - 3 week end days)
            // Late OLB = 800
            // Late amount = 1000
            // Late principal = 200
            // Late interest = 30
            // Late fee: 1000 * 0.01 * 10 + (200 + 30 + 800) * 0.01 * 7 = 172.1
            Assert.AreEqual(_myContract.CalculateDuePenaltiesForInstallment(3, new DateTime(2006, 4, 11)), 172.1m);
        }

        [Test]
        public void CheckInstallmentsStartDates()
        {
            _generalSettings.UpdateParameter(OGeneralSettings.WEEKENDDAY1, 6);
            _generalSettings.UpdateParameter(OGeneralSettings.WEEKENDDAY2, 0);

            Assert.AreEqual(_myContract.InstallmentList[0].StartDate, new DateTime(2006, 1, 1));
            Assert.AreEqual(_myContract.InstallmentList[1].StartDate, new DateTime(2006, 2, 1));
            Assert.AreEqual(_myContract.InstallmentList[2].StartDate, new DateTime(2006, 3, 1));
            Assert.AreEqual(_myContract.InstallmentList[3].StartDate, new DateTime(2006, 4, 1));
            Assert.AreEqual(_myContract.InstallmentList[4].StartDate, new DateTime(2006, 5, 2));
            Assert.AreEqual(_myContract.InstallmentList[5].StartDate, new DateTime(2006, 6, 1));
        }
    }
}
