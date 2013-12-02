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
using System.Data.SqlClient;
using NUnit.Framework;
using NUnit.Mocks;
using OpenCBS.CoreDomain;
using OpenCBS.CoreDomain.Accounting;
using OpenCBS.CoreDomain.Clients;
using OpenCBS.CoreDomain.Contracts.Loans;
using OpenCBS.CoreDomain.Contracts.Loans.Installments;
using OpenCBS.CoreDomain.Contracts.Loans.LoanRepayment;
using OpenCBS.CoreDomain.Contracts.Savings;
using OpenCBS.CoreDomain.Events;
using OpenCBS.CoreDomain.Events.Saving;
using OpenCBS.CoreDomain.FundingLines;
using OpenCBS.CoreDomain.Products;
using OpenCBS.Enums;
using OpenCBS.ExceptionsHandler;
using OpenCBS.Manager;
using OpenCBS.Manager.Accounting;
using OpenCBS.Manager.Contracts;
using OpenCBS.Services;
using OpenCBS.Shared;
using OpenCBS.Shared.Settings;
using OpenCBS.Manager.Clients;

namespace OpenCBS.Test.Services
{
	/// <summary>
	/// Description r�sum�e de TestContractServices.
	/// </summary>

    [TestFixture]
    public class TestLoanServices : BaseServicesTest
    {
        [SetUp]
        protected override void SetUp()
        {
            base.SetUp();
        }

        [TearDown]
        protected override void Dispose()
        {
            base.Dispose();
        }

        private readonly Installment _installment1 = new Installment
        {
            Number = 1,
            CapitalRepayment = 200,
            InterestsRepayment = 100,
            FeesUnpaid = 0,
            ExpectedDate = new DateTime(2009,1,1)
        };

        private readonly Installment _installment2 = new Installment
        {
            Number = 2,
            CapitalRepayment = 200,
            InterestsRepayment = 100,
            FeesUnpaid = 0,
            ExpectedDate = new DateTime(2009, 2, 1)
        };

        [Test]
        public void FindAllInstalments_NoResult()
        {
            DynamicMock mockInstalmentManager = new DynamicMock(typeof(InstallmentManager));

            mockInstalmentManager.SetReturnValue("SelectInstalments", new List<KeyValuePair<int, Installment>>());
            LoanServices loanServices = new LoanServices((InstallmentManager)mockInstalmentManager.MockInstance, null, null);

            Assert.AreEqual(0,loanServices.FindAllInstalments().Count);
        }

        [Test]
        public void FindAllInstalments_Result()
        {
            DynamicMock mockInstalmentManager = new DynamicMock(typeof(InstallmentManager));
            List<KeyValuePair<int, Installment>> list = new List<KeyValuePair<int, Installment>>
                                                            {
                                                                new KeyValuePair<int, Installment>(1, new Installment()),
                                                                new KeyValuePair<int, Installment>(2, new Installment())
                                                            };

            mockInstalmentManager.SetReturnValue("SelectInstalments", list);
            LoanServices loanServices = new LoanServices((InstallmentManager)mockInstalmentManager.MockInstance, null, null);

            Assert.AreEqual(2, loanServices.FindAllInstalments().Count);
        }

        [Test]
        public void UpdateAllInstalmentsDate_DontSkipNonWorkingDays()
        {
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.DONOTSKIPWEEKENDSININSTALLMENTSDATE, true);
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.INCREMENTALDURINGDAYOFF, true);

            DynamicMock mockInstalmentManager = new DynamicMock(typeof(InstallmentManager));
            List<KeyValuePair<int, Installment>> list = new List<KeyValuePair<int, Installment>>
                                      {
                                          new KeyValuePair<int, Installment>(1, new Installment
                                          {
                                              Number = 1,
                                              CapitalRepayment = 200,
                                              InterestsRepayment = 100,
                                              FeesUnpaid = 0,
                                              ExpectedDate = DateTime.Today.AddDays(-1)
                                          })
                                       };
            LoanServices loanServices = new LoanServices((InstallmentManager)mockInstalmentManager.MockInstance, null, null);

            Assert.AreEqual(0, loanServices.UpdateAllInstallmentsDate(list));
        }

        [Test]
        public void UpdateAllInstalmentsDate_NoChange()
        {
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.DONOTSKIPWEEKENDSININSTALLMENTSDATE, true);
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.INCREMENTALDURINGDAYOFF, true);
            NonWorkingDateSingleton.GetInstance("").PublicHolidays = new Dictionary<DateTime, string>();

            DynamicMock mockInstalmentManager = new DynamicMock(typeof(InstallmentManager));
            List<KeyValuePair<int, Installment>> list = new List<KeyValuePair<int, Installment>>
                                      {
                                          new KeyValuePair<int, Installment>(1, new Installment
                                          {
                                              Number = 1,
                                              CapitalRepayment = 200,
                                              InterestsRepayment = 100,
                                              FeesUnpaid = 0,
                                              ExpectedDate = DateTime.Today.AddDays(-1)
                                          })
                                       };
            LoanServices loanServices = new LoanServices((InstallmentManager)mockInstalmentManager.MockInstance, null, null);

            Assert.AreEqual(0, loanServices.UpdateAllInstallmentsDate(list));
        }

        [Test]
        public void UpdateAllInstalmentsDate()
        {
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.DONOTSKIPWEEKENDSININSTALLMENTSDATE, false);
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.INCREMENTALDURINGDAYOFF, true);
            NonWorkingDateSingleton.GetInstance("").PublicHolidays = new Dictionary<DateTime, string>
                                                                         {{DateTime.Today.AddDays(-1), "dfsdf"}};
            Installment installment = new Installment
                                          {
                                              Number = 1,
                                              CapitalRepayment = 200,
                                              InterestsRepayment = 100,
                                              FeesUnpaid = 0,
                                              ExpectedDate = DateTime.Today.AddDays(-1)
                                          };

            DynamicMock mockInstalmentManager = new DynamicMock(typeof(InstallmentManager));
            mockInstalmentManager.Expect("UpdateInstallment",installment, 1, null, true);

            List<KeyValuePair<int, Installment>> list = new List<KeyValuePair<int, Installment>>
                                                            {
                                                                new KeyValuePair<int, Installment>(1, installment)
                                                            };

            LoanServices loanServices = new LoanServices((InstallmentManager)mockInstalmentManager.MockInstance, null, null);
            Assert.AreEqual(1, loanServices.UpdateAllInstallmentsDate(list));
        }

        [Test]
        [ExpectedException(typeof(OpenCbsContractSaveException))]
        public void CheckLoanFilling_AmountIsEmpty()
        {
            Loan loan = new Loan{Amount = 0};
            LoanServices loanServices = new LoanServices(new User());
            loanServices.CheckLoanFilling(loan);
        }

        [Test]
        [ExpectedException(typeof(OpenCbsContractSaveException))]
        public void CheckLoanFilling_InterestRateIsEmpty()
        {
            Loan loan = new Loan { Amount = 10, InterestRate = -1};
            LoanServices loanServices = new LoanServices(new User());
            loanServices.CheckLoanFilling(loan);
        }

        [Test]
        [ExpectedException(typeof(OpenCbsContractSaveException))]
        public void CheckLoanFilling_NbOfInstallmentsIsEmpty()
        {
            Loan loan = new Loan { Amount = 11, InterestRate = 1, NbOfInstallments = 0};
            
            LoanServices loanServices = new LoanServices(new User());
            loanServices.CheckLoanFilling(loan);
        }

        [Test]
        [ExpectedException(typeof(OpenCbsContractSaveException))]
        public void CheckLoanFilling_InstallmentTypeIsEmpty()
        {
            Loan loan = new Loan
                            {
                                Amount = 11,
                                InterestRate = 1,
                                NbOfInstallments = 3,
                                InstallmentType = null
                            };

            LoanServices loanServices = new LoanServices(new User());
            loanServices.CheckLoanFilling(loan);
        }

        [Test]
        [ExpectedException(typeof(OpenCbsContractSaveException))]
        public void CheckLoanFilling_AnticipatedRepaymentPenaltiesIsEmpty()
        {
            Loan loan = new Loan
            {
                Amount = 11,
                InterestRate = 1,
                NbOfInstallments = 3,
                InstallmentType = new InstallmentType(),
                AnticipatedTotalRepaymentPenalties = -1
            };
            
            LoanServices loanServices = new LoanServices(new User());
            loanServices.CheckLoanFilling(loan);
        }

        [Test]
        [ExpectedException(typeof(OpenCbsContractSaveException))]
        public void CheckLoanFilling_NonRepaymentPenalties_InitialAmountIsEmpty()
        {
            Loan loan = new Loan
                            {
                                Amount = 11,
                                InterestRate = 1,
                                NbOfInstallments = 3,
                                InstallmentType = new InstallmentType(),
                                AnticipatedTotalRepaymentPenalties = 2,
                                NonRepaymentPenalties = new NonRepaymentPenalties(-1, 0, 0, 0)
                            };
            
            LoanServices loanServices = new LoanServices(new User());
            loanServices.CheckLoanFilling(loan);
        }

        [Test]
        [ExpectedException(typeof(OpenCbsContractSaveException))]
        public void CheckLoanFilling_NonRepaymentPenalties_OLBIsEmpty()
        {
            Loan loan = new Loan
            {
                Amount = 11,
                InterestRate = 1,
                NbOfInstallments = 3,
                InstallmentType = new InstallmentType(),
                AnticipatedTotalRepaymentPenalties = 2,
                NonRepaymentPenalties = new NonRepaymentPenalties(1, -1, 0, 0)
            };

            LoanServices loanServices = new LoanServices(new User());
            loanServices.CheckLoanFilling(loan);
        }

        [Test]
        [ExpectedException(typeof(OpenCbsContractSaveException))]
        public void CheckLoanFilling_NonRepaymentPenalties_OverDueInterestIsEmpty()
        {
            Loan loan = new Loan
            {
                Amount = 11,
                InterestRate = 1,
                NbOfInstallments = 3,
                InstallmentType = new InstallmentType(),
                AnticipatedTotalRepaymentPenalties = 2,
                NonRepaymentPenalties = new NonRepaymentPenalties(1, 1, -1, 0)
            };
            LoanServices loanServices = new LoanServices(new User());
            loanServices.CheckLoanFilling(loan);
        }

        [Test]
        [ExpectedException(typeof(OpenCbsContractSaveException))]
        public void CheckLoanFilling_NonRepaymentPenalties_OverDuePrincipalIsEmpty()
        {
            Loan loan = new Loan
            {
                Amount = 11,
                InterestRate = 1,
                NbOfInstallments = 3,
                InstallmentType = new InstallmentType(),
                AnticipatedTotalRepaymentPenalties = 2,
                NonRepaymentPenalties = new NonRepaymentPenalties(1, 1, 1, -1)
            };
            LoanServices loanServices = new LoanServices(new User());
            loanServices.CheckLoanFilling(loan);
        }

        [Test]
        [ExpectedException(typeof(OpenCbsContractSaveException))]
        public void CheckLoanFilling_NonRepaymentPenalties_EntryFeesIsEmpty()
        {
            Loan loan = new Loan
            {
                Amount = 11,
                InterestRate = 1,
                NbOfInstallments = 3,
                InstallmentType = new InstallmentType(),
                AnticipatedTotalRepaymentPenalties = 2,
                NonRepaymentPenalties = new NonRepaymentPenalties(1, 1, 1, 1),
                LoanEntryFeesList = null
            };
            LoanServices loanServices = new LoanServices(new User());
            loanServices.CheckLoanFilling(loan);
        }

        [Test]
        [ExpectedException(typeof(OpenCbsContractSaveException))]
        public void CheckLoanFilling_NonRepaymentPenalties_GracePeriodIsEmpty()
        {
            Loan loan = new Loan
            {
                Amount = 11,
                InterestRate = 1,
                NbOfInstallments = 3,
                InstallmentType = new InstallmentType(),
                AnticipatedTotalRepaymentPenalties = 2,
                NonRepaymentPenalties = new NonRepaymentPenalties(1, 1, 1, 1),
                GracePeriod = null
            };
            LoanServices loanServices = new LoanServices(new User());
            loanServices.CheckLoanFilling(loan);
        }

        [Test]
        public void CheckLoanFilling_EverythingIsOk()
        {
            Currency currency = new Currency {Name = "USD", Code = "USD"};
            LoanProduct loanProduct = new LoanProduct { Currency = currency, Name = "test" };
            FundingLine fundingLine = new FundingLine { Currency = currency };

            Loan loan = new Loan
            {
                Amount = 11,
                Product = loanProduct,
                InterestRate = 1,
                NbOfInstallments = 3,
                InstallmentType = new InstallmentType(),
                AnticipatedTotalRepaymentPenalties = 2,
                NonRepaymentPenalties = new NonRepaymentPenalties(1, 1, 1, 1),
                LoanEntryFeesList = new List<LoanEntryFee>(),
                LoanOfficer = User.CurrentUser,
                FundingLine = fundingLine,
                GracePeriod = 2,
                EconomicActivityId = 1,
            };
            LoanServices loanServices = new LoanServices(new User());
            loanServices.CheckLoanFilling(loan);
            Assert.AreEqual(1,1);
        }

        [Test]
        [ExpectedException(typeof(OpenCbsContractSaveException))]
        public void SaveLoan_AddLoan_ProjectIsEmpty()
        {
            Loan loan = new Loan
            {
                Amount = 11,
                InterestRate = 1,
                NbOfInstallments = 3,
                InstallmentType = new InstallmentType(),
                AnticipatedTotalRepaymentPenalties = 2,
                NonRepaymentPenalties = new NonRepaymentPenalties(1, 1, 1, 1),
                GracePeriod = 2
            };
            LoanServices loanServices = new LoanServices(new User());
            IClient person = new Person();
            loanServices.SaveLoan(ref loan, 0, ref person);
        }

        [Test]
        [ExpectedException(typeof(OpenCbsContractSaveException))]
        public void SaveLoan_AddLoan_FundingLineIsEmpty()
        {
            Loan loan = new Loan
            {
                Amount = 11,
                InterestRate = 1,
                NbOfInstallments = 3,
                InstallmentType = new InstallmentType(),
                AnticipatedTotalRepaymentPenalties = 2,
                NonRepaymentPenalties = new NonRepaymentPenalties(1, 1, 1, 1),
                GracePeriod = 2,
                FundingLine = null
            };
            LoanServices loanServices = new LoanServices(new User());
            IClient person = new Person();
            loanServices.SaveLoan(ref loan, 1, ref person);
        }

        [Test]
        [ExpectedException(typeof(OpenCbsContractSaveException))]
        public void SaveLoan_AddLoan_LoanOfficerIsEmpty()
        {
            Loan loan = new Loan
            {
                Amount = 11,
                InterestRate = 1,
                NbOfInstallments = 3,
                InstallmentType = new InstallmentType(),
                AnticipatedTotalRepaymentPenalties = 2,
                NonRepaymentPenalties = new NonRepaymentPenalties(1, 1, 1, 1),
                GracePeriod = 2,
                FundingLine = new FundingLine(),
                LoanOfficer = null
            };
            LoanServices loanServices = new LoanServices(new User());
            IClient person = new Person();
            loanServices.SaveLoan(ref loan, 1, ref person);
        }

        [Test]
        [ExpectedException(typeof(OpenCbsContractSaveException))]
        public void SaveLoan_AddLoan_DontAllowMultipleLoans_ClientIsActive()
        {
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.ALLOWSMULTIPLELOANS, false);
            Loan loan = new Loan
            {
                Amount = 11,
                InterestRate = 1,
                NbOfInstallments = 3,
                InstallmentType = new InstallmentType(),
                AnticipatedTotalRepaymentPenalties = 2,
                NonRepaymentPenalties = new NonRepaymentPenalties(1, 1, 1, 1),
                GracePeriod = 2,
                FundingLine = new FundingLine{Currency = new Currency{Code = "Code",Name = "Name"}},
                Product = new LoanProduct { Currency = new Currency { Code = "Code", Name = "Name" } },
                LoanOfficer = new User()
            };

            LoanServices loanServices = new LoanServices(new User());
            IClient person = new Person {Active = true};
            loanServices.SaveLoan(ref loan, 1, ref person);
        }

        [Test]
        [ExpectedException(typeof(OpenCbsContractSaveException))]
        public void SaveLoan_AddLoan_ClientIsBad()
        {
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.ALLOWSMULTIPLELOANS, false);
            Loan loan = new Loan
            {
                Amount = 11,
                InterestRate = 1,
                NbOfInstallments = 3,
                InstallmentType = new InstallmentType(),
                AnticipatedTotalRepaymentPenalties = 2,
                NonRepaymentPenalties = new NonRepaymentPenalties(1, 1, 1, 1),
                GracePeriod = 2,
                FundingLine = new FundingLine { Currency = new Currency { Code = "Code", Name = "Name" } },
                Product = new LoanProduct { Currency = new Currency { Code = "Code", Name = "Name" } },
                LoanOfficer = new User()
            };

            LoanServices loanServices = new LoanServices(new User());
            IClient person = new Person { Active = true, BadClient = true };
            loanServices.SaveLoan(ref loan, 1, ref person);
        }

        [Test]
        [ExpectedException(typeof(OpenCbsContractSaveException))]
        public void SaveLoan_AddLoan_FundingLineCurrencyIsNotEqualToProductCurrency()
        {
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.ALLOWSMULTIPLELOANS, false);
            Loan loan = new Loan
            {
                Amount = 11,
                InterestRate = 1,
                NbOfInstallments = 3,
                InstallmentType = new InstallmentType(),
                AnticipatedTotalRepaymentPenalties = 2,
                NonRepaymentPenalties = new NonRepaymentPenalties(1, 1, 1, 1),
                GracePeriod = 2,
                StartDate = new DateTime(2009, 1, 1),
                FundingLine = new FundingLine{Currency = new Currency{Code = "Code",Name = "Name"}},
                LoanOfficer = new User(),
                Product = new LoanProduct { Currency = new Currency { Code = "Code2", Name = "Name2" } }
            };

            LoanServices loanServices = new LoanServices(new User());
            IClient person = new Person { Active = false };
            loanServices.SaveLoan(ref loan, 1, ref person);
        }

        [Test, Ignore]
        public void SaveLoan_UpdateLoan_Person_NoGuarantor()
        {
            DynamicMock mockInstalmentManager = new DynamicMock(typeof(InstallmentManager));
            DynamicMock mockLoanManager = new DynamicMock(typeof(LoanManager));

            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.ALLOWSMULTIPLELOANS, false);
            Loan loan = new Loan
            {
                Id = 1,
                Amount = 11,
                Code = "Test",
                InterestRate = 1,
                NbOfInstallments = 2,
                InstallmentType = new InstallmentType(),
                AnticipatedTotalRepaymentPenalties = 2,
                NonRepaymentPenalties = new NonRepaymentPenalties(1, 1, 1, 1),
                GracePeriod = 2,
                StartDate = new DateTime(2009, 1, 1),
                FundingLine = new FundingLine { Currency = new Currency { Code = "Code", Name = "Name" } },
                LoanOfficer = new User(),
                Product = new LoanProduct { Currency = new Currency { Code = "Code", Name = "Name" } },
                InstallmentList = new List<Installment> { _installment1, _installment2 },
                Guarantors = new List<Guarantor>()
            };

            EntryFee prodEntryFee = new EntryFee();
            prodEntryFee.Value = 2;
            prodEntryFee.Id = 21;
            prodEntryFee.Name = "Tested";
            LoanEntryFee loanEntryFee = new LoanEntryFee();
            loanEntryFee.FeeValue = 2;
            loanEntryFee.ProductEntryFee = prodEntryFee;
            loanEntryFee.ProductEntryFeeId = 21;

            loan.LoanEntryFeesList = new List<LoanEntryFee>();
            loan.LoanEntryFeesList.Add(loanEntryFee);
            IClient person = new Person { Active = false };

            mockInstalmentManager.Expect("DeleteInstallments", loan.Id, null);
            mockInstalmentManager.Expect("AddInstallments", loan.InstallmentList, loan.Id, null);
            mockLoanManager.Expect("UpdateLoan", loan, null);

            LoanServices loanServices = new LoanServices((InstallmentManager)mockInstalmentManager.MockInstance, null, (LoanManager)mockLoanManager.MockInstance);
            loanServices.SaveLoan(ref loan, 2, ref person);
        }

        [Test]
        public void SaveLoan_AddLoan_Person_RaisedException_RollBackObjects()
        {
            DynamicMock mockClientManager = new DynamicMock(typeof(ClientManager));
            DynamicMock mockLoanManager = new DynamicMock(typeof(LoanManager));

            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.ALLOWSMULTIPLELOANS, false);
            Loan loan = new Loan
            {
                Amount = 11,
                InterestRate = 1,
                NbOfInstallments = 2,
                InstallmentType = new InstallmentType(),
                AnticipatedTotalRepaymentPenalties = 2,
                NonRepaymentPenalties = new NonRepaymentPenalties(1, 1, 1, 1),
                GracePeriod = 2,
                StartDate = new DateTime(2009, 1, 1),
                FundingLine = new FundingLine { Currency = new Currency { Code = "Code", Name = "Name" } },
                LoanOfficer = new User(),
                Events = new EventStock(),
                Product = new LoanProduct { Currency = new Currency { Code = "Code", Name = "Name" } },
                InstallmentList = new List<Installment> { _installment1, _installment2 }
            };

            IClient person = new Person { Active = false, LoanCycle = 1 };

            mockLoanManager.ExpectAndReturn("Add", 1, loan, 2, null);
            mockClientManager.Expect("UpdateClientStatus", person, null);
            mockClientManager.Expect("UpdateClientLoanCycle", person, null);

            LoanServices loanServices = new LoanServices(null, (ClientManager)mockClientManager.MockInstance, (LoanManager)mockLoanManager.MockInstance);
            try
            {
                loanServices.SaveLoan(ref loan, 2, ref person); //Exception because branchCode is null
                Assert.Fail();
            }
            catch (Exception)
            {
                Assert.AreEqual(0, loan.Id);
                Assert.AreEqual(DateTime.MinValue, loan.CloseDate);
                Assert.AreEqual(DateTime.MinValue, loan.CreationDate);
                Assert.AreEqual(1, person.LoanCycle);
            }    
        }

	    [Test, Ignore]
        public void SaveLoan_AddLoan_Person()
        {
            DynamicMock mockClientManager = new DynamicMock(typeof(ClientManager));
            DynamicMock mockLoanManager = new DynamicMock(typeof(LoanManager));

            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.ALLOWSMULTIPLELOANS, false);

            Loan loan = new Loan
            {
                Amount = 11,
                Code = "Test",
                InterestRate = 1,
                NbOfInstallments = 2,
                InstallmentType = new InstallmentType(),
                AnticipatedTotalRepaymentPenalties = 2,
                Events = new EventStock(),
                NonRepaymentPenalties = new NonRepaymentPenalties(1, 1, 1, 1),
                LoanEntryFeesList = new List<LoanEntryFee>(),
                GracePeriod = 2,
                StartDate = new DateTime(2009,1,1),
                FundingLine = new FundingLine { Currency = new Currency { Code = "Code", Name = "Name" } },
                LoanOfficer = new User(),
                Product = new LoanProduct { Currency = new Currency { Code = "Code", Name = "Name" } },
                InstallmentList = new List<Installment>{_installment1,_installment2}
            };

            IClient person = new Person { Active = false, LoanCycle = 1, Branch = new Branch() { Code = "test" } };

            mockLoanManager.ExpectAndReturn("Add", 1, loan, 2, null);
            mockClientManager.Expect("UpdateClientStatus", person, null);
            mockClientManager.Expect("UpdateClientLoanCycle", person, null);

            LoanServices loanServices = new LoanServices(null, (ClientManager)mockClientManager.MockInstance, (LoanManager)mockLoanManager.MockInstance);
            loanServices.SaveLoan(ref loan, 2, ref person);

            Assert.AreEqual(1, loan.Id);
            Assert.AreEqual(new DateTime(2009,2,1),loan.CloseDate);
            Assert.AreEqual(OContractStatus.Pending, loan.ContractStatus);
            Assert.AreEqual(1, person.LoanCycle);
        }

        [Test]
        public void SaveLoan_AddLoan_Group_RaisedException_RollBackObjects()
        {
           
            DynamicMock mockClientManager = new DynamicMock(typeof(ClientManager));
            DynamicMock mockLoanManager = new DynamicMock(typeof(LoanManager));

            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.ALLOWSMULTIPLELOANS, false);
            Loan loan = new Loan
            {
                Amount = 11,
                Code = "Test",
                InterestRate = 1,
                NbOfInstallments = 2,
                InstallmentType = new InstallmentType(),
                AnticipatedTotalRepaymentPenalties = 2,
                NonRepaymentPenalties = new NonRepaymentPenalties(1, 1, 1, 1),
                GracePeriod = 2,
                Events = new EventStock(),
                StartDate = new DateTime(2009, 1, 1),
                FundingLine = new FundingLine { Currency = new Currency { Code = "Code", Name = "Name" } },
                LoanOfficer = new User(),
                Product = new LoanProduct { Currency = new Currency { Code = "Code", Name = "Name" } },
                InstallmentList = new List<Installment> { _installment1, _installment2 }
            };

            IClient group = new Group { Active = false, LoanCycle = 1, Members = null };

            mockLoanManager.ExpectAndReturn("Add", 1, loan, 1, null);
            mockClientManager.Expect("UpdateClientStatus", group, null);
            mockClientManager.Expect("UpdateClientLoanCycle", group, null);

            LoanServices loanServices = new LoanServices(null, (ClientManager)mockClientManager.MockInstance, (LoanManager)mockLoanManager.MockInstance);
            try
            {
                loanServices.SaveLoan(ref loan, 1, ref group); //Exception because Members list is null
                Assert.Fail();
            }
            catch (Exception)
            {
                Assert.AreEqual(0, loan.Id);
                Assert.AreEqual(DateTime.MinValue, loan.CloseDate);
                Assert.AreEqual(DateTime.MinValue, loan.CreationDate);
                Assert.AreEqual(1, group.LoanCycle);
            } 
        }

        [Test, Ignore]
        public void SaveLoan_AddLoan_Group()
        {
            DynamicMock mockClientManager = new DynamicMock(typeof(ClientManager));
            DynamicMock mockLoanManager = new DynamicMock(typeof(LoanManager));

            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.ALLOWSMULTIPLELOANS, false);
            Loan loan = new Loan
            {
                Amount = 11,
                Code = "Test",
                InterestRate = 1,
                NbOfInstallments = 2,
                InstallmentType = new InstallmentType(),
                AnticipatedTotalRepaymentPenalties = 2,
                NonRepaymentPenalties = new NonRepaymentPenalties(1, 1, 1, 1),
                LoanEntryFeesList = new List<LoanEntryFee>(),
                GracePeriod = 2,
                Events = new EventStock(),
                StartDate = new DateTime(2009, 1, 1),
                FundingLine = new FundingLine { Currency = new Currency { Code = "Code", Name = "Name" } },
                LoanOfficer = new User(),
                Product = new LoanProduct { Currency = new Currency { Code = "Code", Name = "Name" } },
                InstallmentList = new List<Installment> { _installment1, _installment2 }
            };

            Person person = new Person { Active = false, LoanCycle = 1 };
            IClient group = new Group
                                {
                                    Active = false,
                                    LoanCycle = 1,
                                    Members = new List<Member> {new Member {Tiers = person}},
                                    Branch = new Branch(){Code = "test"}
                                };

            mockLoanManager.ExpectAndReturn("Add", 1, loan, 1, null);
            //mockLoanManager.ExpectAndReturn("GetConnection", new SqlConnection());
            mockClientManager.Expect("UpdateClientStatus", group, null);
            mockClientManager.Expect("UpdateClientStatus", person, null);

            LoanServices loanServices =  new LoanServices(null, (ClientManager)mockClientManager.MockInstance, (LoanManager)mockLoanManager.MockInstance);
            loanServices.SaveLoan(ref loan, 1, ref group);

            Assert.AreEqual(1, loan.Id);
            Assert.AreEqual(new DateTime(2009, 2, 1), loan.CloseDate);
            Assert.AreEqual(OContractStatus.Pending, loan.ContractStatus);
            Assert.AreEqual(1, ((Group)group).Members[0].Tiers.LoanCycle);
            Assert.AreEqual(1, group.LoanCycle);
        }

        [Test]
        public void DisburseLoanSimulation()
        {
            ChartOfAccounts.SuppressAll();
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.ACCOUNTINGPROCESS, OAccountingProcesses.Cash);
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.INCREMENTALDURINGDAYOFF, true);
            Loan loan = new Loan (new User(), ApplicationSettings.GetInstance(""),NonWorkingDateSingleton.GetInstance(""),ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()))
            {
                Amount = 11,
                Code = "Test",
                InterestRate = 1,
                NbOfInstallments = 2,
                InstallmentType = new InstallmentType(),
                AnticipatedTotalRepaymentPenalties = 2,
                NonRepaymentPenalties = new NonRepaymentPenalties(1, 1, 1, 1),
                GracePeriod = 2,
                Events = new EventStock(),
                StartDate = new DateTime(2009, 1, 1),
                FundingLine = new FundingLine { Currency = new Currency { Id = 1, Code = "Code", Name = "Name" } },
                LoanOfficer = new User(),
                Product = new LoanProduct { Currency = new Currency { Id =  1, Code = "Code", Name = "Name" } },
                InstallmentList = new List<Installment> { _installment1, _installment2 }
            };

            LoanServices loanServices = new LoanServices(new User());
            LoanDisbursmentEvent loanDisbursmentEvent = loanServices.DisburseSimulation(loan, true, new DateTime(2009,1,1), false);

            Assert.AreEqual(new DateTime(2009,1,1),loanDisbursmentEvent.Date);
        }

        [Test]
        [ExpectedException(typeof(OpenCbsContractSaveException))]
        public void DisburseLoan_FundingLineIsNull()
        {
            Loan loan = new Loan(new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()))
            {
                Amount = 11,
                Code = "Test",
                InterestRate = 1,
                NbOfInstallments = 2,
                InstallmentType = new InstallmentType(),
                AnticipatedTotalRepaymentPenalties = 2,
                NonRepaymentPenalties = new NonRepaymentPenalties(1, 1, 1, 1),
                GracePeriod = 2,
                Events = new EventStock(),
                StartDate = new DateTime(2009, 1, 1),
                FundingLine = null,
                LoanOfficer = new User(),
                Product = new LoanProduct { Currency = new Currency { Code = "Code", Name = "Name" } },
                InstallmentList = new List<Installment> { _installment1, _installment2 }
            };

            LoanServices loanServices = new LoanServices(new User());
            loanServices.Disburse(loan, new DateTime(2009, 1, 1), true, false, null);
        }

        [Test]
        [ExpectedException(typeof(OpenCbsContractSaveException))]
        public void DisburseLoan_FundingLineCurrencyIsNotEgualToProductCurrency()
        {
            Loan loan = new Loan(new User(), ApplicationSettings.GetInstance(""), NonWorkingDateSingleton.GetInstance(""), ProvisionTable.GetInstance(new User()), ChartOfAccounts.GetInstance(new User()))
            {
                Amount = 11,
                Code = "Test",
                InterestRate = 1,
                NbOfInstallments = 2,
                InstallmentType = new InstallmentType(),
                AnticipatedTotalRepaymentPenalties = 2,
                NonRepaymentPenalties = new NonRepaymentPenalties(1, 1, 1, 1),
                GracePeriod = 2,
                Events = new EventStock(),
                StartDate = new DateTime(2009, 1, 1),
                FundingLine = new FundingLine { Currency = new Currency { Code = "Code2", Name = "Name2" } },
                LoanOfficer = new User(),
                Product = new LoanProduct { Currency = new Currency { Code = "Code", Name = "Name" } },
                InstallmentList = new List<Installment> { _installment1, _installment2 }
            };

            LoanServices loanServices = new LoanServices(new User());
            loanServices.Disburse(loan, new DateTime(2009, 1, 1), true, false, null);
        }

        [Test]
        public void RepayWrittenOffLoan()
        {
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.ALLOWSMULTIPLELOANS, false);
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.INCREMENTALDURINGDAYOFF, false);
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.ACCOUNTINGPROCESS, OAccountingProcesses.Cash);
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.CALCULATIONLATEFEESDURINGPUBLICHOLIDAYS, 0);
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.INTERESTS_ALSO_CREDITED_IN_FL, 1);
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.DONOTSKIPWEEKENDSININSTALLMENTSDATE, 0);
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.BAD_LOAN_DAYS, "180");

            ProvisionTable _provisionningTable = ProvisionTable.GetInstance(new User() {Id = 1});
            _provisionningTable.ProvisioningRates = new List<ProvisioningRate>();
            _provisionningTable.Add(new ProvisioningRate { Number = 1, NbOfDaysMin = 0, NbOfDaysMax = 0, ProvisioningValue = 0.02, ProvisioningInterest = 0.02, ProvisioningPenalty = 0.02 });
            _provisionningTable.Add(new ProvisioningRate { Number = 2, NbOfDaysMin = 1, NbOfDaysMax = 30, ProvisioningValue = 0.1, ProvisioningInterest = 0.01, ProvisioningPenalty = 0.01 });
            _provisionningTable.Add(new ProvisioningRate { Number = 3, NbOfDaysMin = 31, NbOfDaysMax = 60, ProvisioningValue = 0.25, ProvisioningInterest = 0.025, ProvisioningPenalty = 0.025 });
            _provisionningTable.Add(new ProvisioningRate { Number = 4, NbOfDaysMin = 61, NbOfDaysMax = 90, ProvisioningValue = 0.5, ProvisioningInterest = 0.5, ProvisioningPenalty = 0.5 });
            _provisionningTable.Add(new ProvisioningRate { Number = 5, NbOfDaysMin = 91, NbOfDaysMax = 180, ProvisioningValue = 0.75, ProvisioningInterest = 0.75, ProvisioningPenalty = 0.75 });
            _provisionningTable.Add(new ProvisioningRate { Number = 6, NbOfDaysMin = 181, NbOfDaysMax = 365, ProvisioningValue = 1, ProvisioningInterest = 1, ProvisioningPenalty = 1 });
            _provisionningTable.Add(new ProvisioningRate { Number = 7, NbOfDaysMin = 366, NbOfDaysMax = 99999, ProvisioningValue = 1, ProvisioningInterest = 1, ProvisioningPenalty = 1 });

            Teller.CurrentTeller = null;

            //DynamicMock mockClientManager = new DynamicMock(typeof(ClientManager));
            //DynamicMock mockLoanManager = new DynamicMock(typeof(LoanManager));

            //Loan loan = new Loan
            //{
            //    Amount = 11,
            //    Code = "Test",
            //    InterestRate = 1,
            //    NbOfInstallments = 2,
            //    InstallmentType = new InstallmentType(),
            //    AnticipatedTotalRepaymentPenalties = 2,
            //    NonRepaymentPenalties = new NonRepaymentPenalties(1, 1, 1, 1),
            //    GracePeriod = 2,
            //    Events = new EventStock(),
            //    StartDate = new DateTime(2009, 1, 1),
            //    FundingLine = new FundingLine { Currency = new Currency { Id = 1, Code = "Code", Name = "Name" } },
            //    LoanOfficer = new User(),
            //    Product = new LoanProduct { Currency = new Currency { Id = 1, Code = "Code", Name = "Name" } },
            //    InstallmentList = new List<Installment> { _installment1, _installment2 }
            //};

            //Person person = new Person { Active = false, LoanCycle = 1 };
            //IClient group = new Group { Active = false, LoanCycle = 1, Members = new List<Member> { new Member { Tiers = person } } };

            //mockClientManager.Expect("UpdateClientStatus", group, null);
            //mockClientManager.Expect("UpdateClientLoanCycle", group, null);

            //mockClientManager.Expect("UpdateClientStatus", person, null);
            //mockClientManager.Expect("UpdateClientLoanCycle", person, null);

            //LoanServices loanServices = new LoanServices(null, (ClientManager)mockClientManager.MockInstance, (LoanManager)mockLoanManager.MockInstance);
            //loanServices.SaveLoan(ref loan, 1, ref group);

            //loanServices.Disburse(loan, new DateTime(2008, 1, 1), true, true);
            //loanServices.Repay(loan, group, 1, DateTime.Now, 1, true, 0, 0, false, 0, true);

            //Loan disbursedLoan = loan;
            //disbursedLoan.Disbursed = true;
            //LoanDisbursmentEvent loanDisbursmentEvent = new LoanDisbursmentEvent
            //                                                {
            //                                                    Date = new DateTime(2008, 1, 1),
            //                                                    Amount = disbursedLoan.Amount,
            //                                                    Commission = disbursedLoan.CalculateEntryFeesAmount(),
            //                                                    ClientType = disbursedLoan.ClientType
            //                                                };
            //disbursedLoan.Events.Add(loanDisbursmentEvent);
            //loanServices.Repay(disbursedLoan, group, 1, DateTime.Now, 1, true, 0,0, false, 1, true);
            AddDataForTestingTransaction dataHelper = new AddDataForTestingTransaction();
            int _creditId = dataHelper.AddGenericCreditContractIntoDatabase();


            IClient _client = new Group {Id = 5};
            LoanManager _loanManager = new LoanManager(new User() {Id = 1});
            Loan _loan = _loanManager.SelectLoan(_creditId, true, true, true);
            
            _loan.CreditCommiteeDate = _loan.StartDate;

            AccountManager accountManager = new AccountManager(new User() {Id = 1});
            //accountManager.AddForCurrency(_loan.Product.Currency.Id);
            
            FundingLine f = _loan.FundingLine;
            f.Currency.Id = _loan.Product.Currency.Id;
            FundingLineServices fundingLineServices = new FundingLineServices(new User() {Id = 1});

            var ev = new FundingLineEvent
            {
                Code = "KAO",
                Type = OFundingLineEventTypes.Entry,
                CreationDate = new DateTime(2008,1,1),
                EndDate = DateTime.Now.AddDays(1),
                Amount = 1000,
                FundingLine = f,
                Movement = OBookingDirections.Credit,
                IsDeleted = false
            };
            fundingLineServices.AddFundingLineEvent(ev, null);
            f.AddEvent(ev);
            _loan.LoanEntryFeesList=new List<LoanEntryFee>();
            
            EntryFee productEntryFee = new EntryFee();
            productEntryFee.Value = 1;
            productEntryFee.IsRate = false;
            productEntryFee.Id = 21;
            LoanEntryFee loanEntryFee = new LoanEntryFee();
            loanEntryFee.FeeValue = 1;
            loanEntryFee.ProductEntryFee = productEntryFee;
            loanEntryFee.ProductEntryFeeId = 21;

            LoanServices loanServices = new LoanServices(new User() { Id = 1 });

            PaymentMethod method = new PaymentMethod(1, "Savings", "Savings method", false);
            loanServices.Disburse(_loan, new DateTime(2008, 1, 1), true, false, method);

            ////_loan.WrittenOff = true;
            ////_loan = loanServices.Repay(_loan, _client, 1, DateTime.Now, 10, true, 0, 0,false, 1, true);
            //Assert.AreEqual(_loan.Disbursed, true);
            //_loan.WrittenOff = true;
            //OCurrency prevAmount = _loan.ChartOfAccounts.GetAccountByNumber(OAccounts.RECOVERY_OF_CHARGED_OFF_ASSETS, _loan.Product.Currency.Id).Balance;
            //_loan = loanServices.Repay(_loan, _client, 1, DateTime.Now, 100, true, 0, 0, false, 5, false, OPaymentMethods.Cash, null, false);

            //Assert.AreEqual(_loan.Events.GetNumberOfEvents, 1);
            //_loan.Events.SortEventsByDate();
            //Assert.AreEqual(_loan.Events.GetEvent(0).Code, "ROWO");

            //_loan = loanServices.Repay(_loan, _client, 2, DateTime.Now, 100, true, 0, 0, false, 5, false, OPaymentMethods.Cash, null, false);


            //Assert.AreEqual(_loan.Events.GetNumberOfEvents, 2);
            
            //_loan.Events.SortEventsByDate();
            //Assert.AreEqual(_loan.Events.GetEvent(0).Code, "ROWO");

        }

        [Test]
        [ExpectedException(typeof(OpenCbsRepayException))]
        public void RepayLoanBeforeDisburse()
        {
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.ALLOWSMULTIPLELOANS, false);
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.INCREMENTALDURINGDAYOFF, false);
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.ACCOUNTINGPROCESS, OAccountingProcesses.Cash);
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.CALCULATIONLATEFEESDURINGPUBLICHOLIDAYS, 0);
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.INTERESTS_ALSO_CREDITED_IN_FL, 1);
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.DONOTSKIPWEEKENDSININSTALLMENTSDATE, 0);
            ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.BAD_LOAN_DAYS, "180");
            ProvisionTable _provisionningTable = ProvisionTable.GetInstance(new User() { Id = 1 });
            _provisionningTable.ProvisioningRates = new List<ProvisioningRate>();
            _provisionningTable.Add(new ProvisioningRate { Number = 1, NbOfDaysMin = 0, NbOfDaysMax = 0, ProvisioningValue = 0.02, ProvisioningInterest = 0.02, ProvisioningPenalty = 0.02 });
            _provisionningTable.Add(new ProvisioningRate { Number = 2, NbOfDaysMin = 1, NbOfDaysMax = 30, ProvisioningValue = 0.1, ProvisioningInterest = 0.1, ProvisioningPenalty = 0.1 });
            _provisionningTable.Add(new ProvisioningRate { Number = 3, NbOfDaysMin = 31, NbOfDaysMax = 60, ProvisioningValue = 0.25, ProvisioningInterest = 0.25, ProvisioningPenalty = 0.25 });
            _provisionningTable.Add(new ProvisioningRate { Number = 4, NbOfDaysMin = 61, NbOfDaysMax = 90, ProvisioningValue = 0.5, ProvisioningInterest= 0.5, ProvisioningPenalty= 0.5 });
            _provisionningTable.Add(new ProvisioningRate { Number = 5, NbOfDaysMin = 91, NbOfDaysMax = 180, ProvisioningValue = 0.75, ProvisioningInterest = 0.75, ProvisioningPenalty = 0.75 });
            _provisionningTable.Add(new ProvisioningRate { Number = 6, NbOfDaysMin = 181, NbOfDaysMax = 365, ProvisioningValue = 1, ProvisioningInterest = 1, ProvisioningPenalty= 1 });
            _provisionningTable.Add(new ProvisioningRate { Number = 7, NbOfDaysMin = 366, NbOfDaysMax = 99999, ProvisioningValue = 1, ProvisioningInterest= 1, ProvisioningPenalty= 1 });

            Teller.CurrentTeller = null;

             AddDataForTestingTransaction dataHelper = new AddDataForTestingTransaction();
            int _creditId = dataHelper.AddGenericCreditContractIntoDatabase();


            IClient _client = new Group { Id = 5 };
            LoanManager _loanManager = new LoanManager(new User() { Id = 1 });
            Loan _loan = _loanManager.SelectLoan(_creditId, true, true, true);

            _loan.CreditCommiteeDate = _loan.StartDate;

            FundingLine f = _loan.FundingLine;
            f.Currency.Id = _loan.Product.Currency.Id;
            FundingLineServices fundingLineServices = new FundingLineServices(new User() { Id = 1 });

            var ev = new FundingLineEvent
            {
                Code = "KAO",
                Type = OFundingLineEventTypes.Entry,
                CreationDate = new DateTime(2008, 1, 10),
                EndDate = DateTime.Now.AddDays(1),
                Amount = 1000,
                FundingLine = f,
                Movement = OBookingDirections.Credit,
                IsDeleted = false
            };
            fundingLineServices.AddFundingLineEvent(ev, null);
            f.AddEvent(ev);
            LoanServices loanServices = new LoanServices(new User() { Id = 1 });

            PaymentMethod method = new PaymentMethod(1, "Savings", "Savings method", false);
            
            _loan = loanServices.Disburse(_loan, new DateTime(2005, 10, 10), true, false, method);

            Assert.AreEqual(_loan.Disbursed, true);
            PaymentMethod paymentMethod = new PaymentMethod(1, "Cash", "", false);
            loanServices.Repay(_loan, _client, 1, new DateTime(2005, 1, 5), 100, true, 0, 0, false, 5, false, false, paymentMethod, null, false);

        }

        [Test]       
        public void ShouldNotDisburseMultipleTimes()
        {
            var settings = ApplicationSettings.GetInstance("");
            settings.UpdateParameter(OGeneralSettings.ALLOWSMULTIPLELOANS, true);
            settings.UpdateParameter(OGeneralSettings.ACCOUNTINGPROCESS, OAccountingProcesses.Cash);

            Teller.CurrentTeller = null;

            AddDataForTestingTransaction dataHelper = new AddDataForTestingTransaction();
            int creditId = dataHelper.AddGenericCreditContractIntoDatabase();


            LoanManager loanManager = new LoanManager(new User { Id = 1 });
            Loan loan = loanManager.SelectLoan(creditId, true, true, true);

            FundingLine f = loan.FundingLine;
            f.Currency.Id = loan.Product.Currency.Id;
            FundingLineServices fundingLineServices = new FundingLineServices(new User() { Id = 1 });
            var ev = new FundingLineEvent
            {
                Code = "KAO",
                Type = OFundingLineEventTypes.Entry,
                CreationDate = new DateTime(2008, 1, 10),
                EndDate = DateTime.Now.AddDays(1),
                Amount = 1000,
                FundingLine = f,
                Movement = OBookingDirections.Credit,
                IsDeleted = false
            };
            fundingLineServices.AddFundingLineEvent(ev, null);
            f.AddEvent(ev);

            loan.CreditCommiteeDate = loan.StartDate;
            LoanServices loanServices = new LoanServices(new User { Id = 1 });
            PaymentMethod method = new PaymentMethod(1, "Savings", "Savings method", false);
            try
            {
                loan.Disbursed = true;
                loanServices.Disburse(loan, new DateTime(2005, 10, 10), true, false, method);
                Assert.Fail("Contract should not be validated");
            } catch(OpenCbsContractSaveException)
            {                
            }            
            try
            {
                loan.Disbursed = false;
                loanServices.Disburse(loan, new DateTime(2005, 10, 10), true, false, method);
                loanServices.Disburse(loan, new DateTime(2005, 10, 10), true, false, method);
                Assert.Fail("Contract should not be failed due to double disbursements");
            }
            catch (OpenCbsContractSaveException)
            {
            }
        }

        [Test]
        public void DoRepaymentFromSavingsAccount()
        {
            SetApplicationSettings();

            AddDataForTestingTransaction dataHelper = new AddDataForTestingTransaction();
            int _creditId = dataHelper.AddGenericCreditContractIntoDatabase();

            IClient _client = ServicesProvider.GetInstance().GetClientServices().FindGroupByName("SCG");
            LoanManager _loanManager = new LoanManager(new User() { Id = 1 });
            Loan _loan = _loanManager.SelectLoan(_creditId, true, true, true);
            
            _loan.CreditCommiteeDate = _loan.StartDate;

            _loan.CompulsorySavings = GetSavingContract(_client);
            _loanManager.UpdateLoan(_loan);

            AddFundingLineEvent(_loan);
            LoanServices loanServices = new LoanServices(new User() { Id = 1 });

            PaymentMethod method = new PaymentMethod(1, "Cash", "Cash", false);

            _loan = loanServices.Disburse(_loan, new DateTime(2005, 10, 10), true, false, method);

            Assert.AreEqual(_loan.Disbursed, true);
            PaymentMethod paymentMethod = new PaymentMethod(1, "Savings", "", false);
            loanServices.Repay(_loan, _client, 1, new DateTime(2005, 10, 11), 100, true, 0, 0, false, 5, false, false, paymentMethod, null, false);

            Assert.IsTrue(_loan.CompulsorySavings.Events.FindAll(item => item is LoanRepaymentFromSavingEvent).Count==1);
        }

	    private void SetApplicationSettings()
	    {
	        ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.ALLOWSMULTIPLELOANS, false);
	        ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.INCREMENTALDURINGDAYOFF, false);
	        ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.ACCOUNTINGPROCESS, OAccountingProcesses.Cash);
	        ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.CALCULATIONLATEFEESDURINGPUBLICHOLIDAYS, 0);
	        ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.INTERESTS_ALSO_CREDITED_IN_FL, 1);
	        ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.DONOTSKIPWEEKENDSININSTALLMENTSDATE, 0);
	        ApplicationSettings.GetInstance("").UpdateParameter(OGeneralSettings.BAD_LOAN_DAYS, "180");
	    }

	    private void AddFundingLineEvent(Loan loan)
	    {
	        FundingLine f = loan.FundingLine;
	        f.Currency.Id = loan.Product.Currency.Id;
	        FundingLineServices fundingLineServices = new FundingLineServices(new User() { Id = 1 });

	        var ev = new FundingLineEvent
	                     {
	                         Code = "KAO",
	                         Type = OFundingLineEventTypes.Entry,
	                         CreationDate = new DateTime(2008, 1, 10),
	                         EndDate = DateTime.Now.AddDays(1),
	                         Amount = 1000,
	                         FundingLine = f,
	                         Movement = OBookingDirections.Credit,
	                         IsDeleted = false
	                     };
	        fundingLineServices.AddFundingLineEvent(ev, null);
	        f.AddEvent(ev);
	    }

	    private SavingBookContract GetSavingContract(IClient client)
	    {
	        User user = ServicesProvider.GetInstance().GetUserServices().FindAll(false)[0];
	        User.CurrentUser = user;
	        SavingBookContract savingContract = new SavingBookContract(
	            ServicesProvider.GetInstance().GetGeneralSettings(),
                user);
            client.Branch = new Branch() { Id = 1, Code = "Test" };
            
            SavingsBookProduct product = new SavingsBookProduct();
	        product.AgioFees = 0;
	        product.BalanceMin = 0;
	        product.BalanceMax = 5000;
	        product.ChequeDepositFees = 0;
	        product.ClientType = OClientTypes.All;
	        product.CloseFees = 0;
	        product.InterestRate = 0;
	        product.TransferMin = 1;
	        product.TransferMax = 5000;
	        product.WithdrawingMin = 1;
	        product.WithdrawingMax = 5000;
	        product.ReopenFees = 0;
	        product.RateWithdrawFees = 0;
	        product.TransferFeesType = OSavingsFeesType.Flat;
	        product.FlatTransferFees = 0;
	        product.FlatWithdrawFees = 0;
	        product.WithdrawFeesType = OSavingsFeesType.Flat;
            product.EntryFees = 0;
	        product.DepositFees = 0;
	        product.DepositMin = 1;
	        product.DepositMax = 5000;
	        product.ManagementFees = 0;
	        product.Code = "test";
	        product.Name = "Test";
	        product.InitialAmountMin = 0;
	        product.InitialAmountMax = 5000;
            product.Currency = new Currency(){Id=1};
	        product.InterestFrequency = OSavingInterestFrequency.EndOfMonth;
	        product.InterestBase = OSavingInterestBase.Daily;
	        product.OverdraftFees = 0;
	        product.InterBranchTransferFee.Value = 0;
            product.ManagementFeeFreq = new InstallmentType(1,"Test",30,1);
            product.AgioFeesFreq = new InstallmentType(1,"test",30,1);
	        ServicesProvider.GetInstance().GetSavingProductServices().SaveProduct(product);
            
                       
            
	        savingContract.Product = product;
	        savingContract.InterestRate = 0;
	        savingContract.FlatTransferFees = 0;
	        savingContract.FlatWithdrawFees = 0;
	        savingContract.DepositFees = 0;
	        savingContract.CloseFees = 0;
	        savingContract.ReopenFees = 0;
	        savingContract.ManagementFees = 0;
	        savingContract.AgioFees = 0;
	        savingContract.Branch = client.Branch;
	        savingContract.SavingsOfficer = user;
            savingContract.CreationDate = DateTime.Now;
	        savingContract.Code = "test";
	        savingContract.InitialAmount = 1;
	        savingContract.EntryFees = 0;

            
	        ServicesProvider.GetInstance().GetSavingServices().SaveContract(savingContract, (Client) client);
	        ServicesProvider.GetInstance().GetSavingServices().Deposit(savingContract, DateTime.Now, 4000, string.Empty,
	                                                                   user, false, OSavingsMethods.Cash, null,
	                                                                   new Teller());
	        return savingContract;
	    }
    }
}
