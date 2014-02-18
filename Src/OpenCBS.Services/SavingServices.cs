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
using System.ComponentModel;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Globalization;
using OpenCBS.CoreDomain;
using OpenCBS.CoreDomain.Accounting;
using OpenCBS.CoreDomain.Clients;
using OpenCBS.CoreDomain.Contracts.Savings;
using OpenCBS.CoreDomain.Events;
using OpenCBS.CoreDomain.Events.Loan;
using OpenCBS.CoreDomain.Events.Saving;
using OpenCBS.CoreDomain.Products;
using OpenCBS.ExceptionsHandler;
using OpenCBS.ExceptionsHandler.Exceptions.SavingExceptions;
using OpenCBS.Manager.Contracts;
using OpenCBS.Manager.Events;
using OpenCBS.Shared;
using OpenCBS.Shared.Settings;
using System.Collections.Generic;
using OpenCBS.Services.Events;
using OpenCBS.CoreDomain.SearchResult;
using OpenCBS.Enums;
using System.Linq;
using OpenCBS.CoreDomain.Contracts.Savings.CalculateInterests;
using OpenCBS.CoreDomain.Contracts.Loans;

namespace OpenCBS.Services
{
    public delegate void GeneralClosureHandler(int current, int totalSavings);

	/// <summary>
	/// Description r�sum�e de SavingServices.
	/// </summary>
    [Security()]
    public class SavingServices : Services
	{
        public event GeneralClosureHandler OnSavingClosureFinished;
        private EventProcessorServices _ePS;
        private readonly SavingManager _savingManager;
		private readonly SavingEventManager _savingEventManager;
		private readonly User _user = new User();

		public SavingServices(User pUser)
		{
			_user = pUser;
			_savingManager = new SavingManager(pUser);
			_savingEventManager = new SavingEventManager(pUser);
            _ePS = new EventProcessorServices(pUser);
		}

		public SavingServices(string pTestDB)
		{
			_savingManager = new SavingManager(pTestDB);
			_savingEventManager = new SavingEventManager(pTestDB);
		}

        public SavingServices(string pTestDB, User pUser)
        {
            _user = pUser;
            _savingManager = new SavingManager(pTestDB);
            _savingEventManager = new SavingEventManager(pTestDB);
            _ePS = new EventProcessorServices(pUser, pTestDB);
        }

		public SavingServices(SavingManager pSavingManager, SavingEventManager pSavingEventManager,  User pUser)
		{
            _user = pUser;
			_savingManager = pSavingManager;
            _savingEventManager = pSavingEventManager;
		}

        public SavingServices(SavingManager pSavingManager, SavingEventManager pSavingEventManager, LoanManager pLoanManager, User pUser)
        {
            _user = pUser;
            _savingManager = pSavingManager;
            _savingEventManager = pSavingEventManager;
        }

        private static bool IsInitialAmountCorrect(ISavingsContract pSaving)
        {
            return ServicesHelper.CheckIfValueBetweenMinAndMax(pSaving.Product.InitialAmountMin, pSaving.Product.InitialAmountMax, pSaving.InitialAmount);
        }

        private static bool IsInterestRateCorrect(ISavingsContract pSaving)
		{
			if (pSaving.Product.InterestRate.HasValue)
				return pSaving.InterestRate == pSaving.Product.InterestRate.Value;
            
            return ServicesHelper.CheckIfValueBetweenMinAndMax(pSaving.Product.InterestRateMin, pSaving.Product.InterestRateMax, pSaving.InterestRate);
		}

        private static bool IsEntryFeesCorrect(ISavingsContract pSaving, OCurrency entryFees)
        {
            if (pSaving.Product.EntryFees.HasValue) return pSaving.Product.EntryFees.Value == entryFees;
            
            return ServicesHelper.CheckIfValueBetweenMinAndMax(pSaving.Product.EntryFeesMin, pSaving.Product.EntryFeesMax, entryFees);
        }

        private static bool IsWithdrawFeesCorrect(SavingBookContract pSaving)
        {
            if (pSaving.Product.WithdrawFeesType == OSavingsFeesType.Flat)
            {
                if (!pSaving.FlatWithdrawFees.HasValue)
                    return false;

                if (pSaving.Product.FlatWithdrawFees.HasValue)
                    return pSaving.FlatWithdrawFees.Value == pSaving.Product.FlatWithdrawFees;

                return ServicesHelper.CheckIfValueBetweenMinAndMax(pSaving.Product.FlatWithdrawFeesMin,
                                                                   pSaving.Product.FlatWithdrawFeesMax,
                                                                   pSaving.FlatWithdrawFees);
            }

            if (!pSaving.RateWithdrawFees.HasValue)
                return false;

            if (pSaving.Product.RateWithdrawFees.HasValue)
                return pSaving.RateWithdrawFees.Value == pSaving.Product.RateWithdrawFees.Value;
            return ServicesHelper.CheckIfValueBetweenMinAndMax(pSaving.Product.RateWithdrawFeesMin,
                                                               pSaving.Product.RateWithdrawFeesMax,
                                                               pSaving.RateWithdrawFees.Value);

        }

	    private static bool IsDepositFeesCorrect(SavingBookContract pSaving)
        {
            if (!pSaving.DepositFees.HasValue)
                return false;

            if (pSaving.Product.DepositFees.HasValue)
                return pSaving.DepositFees.Value == pSaving.Product.DepositFees;
            
            return ServicesHelper.CheckIfValueBetweenMinAndMax(pSaving.Product.DepositFeesMin, pSaving.Product.DepositFeesMax, pSaving.DepositFees);
        }

        private static bool IsCloseFeesCorrect(SavingBookContract pSaving)
        {
            if (!pSaving.CloseFees.HasValue)
                return false;

            if (pSaving.Product.CloseFees.HasValue)
                return pSaving.CloseFees.Value == pSaving.Product.CloseFees;
            else
                return ServicesHelper.CheckIfValueBetweenMinAndMax(pSaving.Product.CloseFeesMin, pSaving.Product.CloseFeesMax, pSaving.CloseFees);
        }

        private static bool IsManagementFeesCorrect(SavingBookContract pSaving)
        {
            if (!pSaving.ManagementFees.HasValue)
                return false;

            if (pSaving.Product.ManagementFees.HasValue)
                return pSaving.ManagementFees.Value == pSaving.Product.ManagementFees;

            return ServicesHelper.CheckIfValueBetweenMinAndMax(pSaving.Product.ManagementFeesMin,
                                                               pSaving.Product.ManagementFeesMax, pSaving.ManagementFees);
        }

        private static bool IsAgioFeesCorrect(SavingBookContract pSaving)
        {
            if (!pSaving.AgioFees.HasValue)
                return false;

            if (pSaving.Product.AgioFees.HasValue)
                return pSaving.AgioFees.Value == pSaving.Product.AgioFees;

            return ServicesHelper.CheckIfValueBetweenMinAndMax(pSaving.Product.AgioFeesMin, pSaving.Product.AgioFeesMax,
                                                               pSaving.AgioFees);
        }

        private static bool IsTransferFeesCorrect(SavingBookContract pSaving)
        {
            if (pSaving.Product.TransferFeesType == OSavingsFeesType.Flat)
            {
                if (!pSaving.FlatTransferFees.HasValue)
                    return false;

                if (pSaving.Product.FlatTransferFees.HasValue)
                    return pSaving.FlatTransferFees.Value == pSaving.Product.FlatTransferFees;

                return ServicesHelper.CheckIfValueBetweenMinAndMax(pSaving.Product.FlatTransferFeesMin,
                                                                   pSaving.Product.FlatTransferFeesMax,
                                                                   pSaving.FlatTransferFees);
            }

                if (!pSaving.RateTransferFees.HasValue)
                    return false;

                if (pSaving.Product.RateTransferFees.HasValue)
                    return pSaving.RateTransferFees.Value == pSaving.Product.RateTransferFees.Value;

            return ServicesHelper.CheckIfValueBetweenMinAndMax(pSaving.Product.RateTransferFeesMin,
                                                               pSaving.Product.RateTransferFeesMax,
                                                               pSaving.RateTransferFees.Value);
        }

        private static bool IsProductCorrect(ISavingsContract pSaving)
        {
            return pSaving.Product != null;
        }

        private static bool IsSavingBalanceCorrect(ISavingsContract pSaving, OCurrency initialAmount)
        {
            // Check balance simulation
            return ServicesHelper.CheckIfValueBetweenMinAndMax(pSaving.Product.BalanceMin, pSaving.Product.BalanceMax, initialAmount);
        }

        private static bool IsSavingBalanceCorrect(ISavingsContract pSaving)
		{
			// Check balance simulation
			return ServicesHelper.CheckIfValueBetweenMinAndMax(pSaving.Product.BalanceMin, pSaving.Product.BalanceMax, pSaving.GetBalance());
		}

        private static bool IsDepositAmountCorrect(OCurrency pDepositAmount, ISavingsContract pSaving, OSavingsMethods savingsMethod)
		{
			// Check Deposit event amount
            if (savingsMethod == OSavingsMethods.Cheque)
            {
                SavingsBookProduct sbp = (SavingsBookProduct) pSaving.Product;
                return ServicesHelper.CheckIfValueBetweenMinAndMax(sbp.ChequeDepositMin, sbp.ChequeDepositMax, pDepositAmount);
            }
            
            return (ServicesHelper.CheckIfValueBetweenMinAndMax(pSaving.Product.DepositMin, pSaving.Product.DepositMax, pDepositAmount));
		}

        private static decimal CheckVirtualBalance(SavingBookContract saving, OCurrency totalAmount)
        {
            // Virtual balance
            saving.Loans = ServicesProvider.GetInstance().GetSavingServices().SelectLoansBySavingsId(saving.Id);
            decimal totalAmountPercentage = 0;

            if ((saving.Loans != null && (saving.Loans.Count > 0)))
            {
                decimal balance = saving.GetBalance().Value;
                foreach (Loan assosiatedLoan in saving.Loans)
                {
                    if (assosiatedLoan.ContractStatus == OContractStatus.Active)
                    {
                        if (assosiatedLoan.CompulsorySavingsPercentage != null)
                            totalAmountPercentage += (assosiatedLoan.Amount.Value * (decimal)assosiatedLoan.CompulsorySavingsPercentage) / 100;
                    }
                }

                if ((balance - totalAmountPercentage) < totalAmount)
                {
                    return totalAmountPercentage;
                }
            }

            // should be 0 in case we can do operation
            return 0;
        }

        private static bool IsWithdrawAmountCorrect(OCurrency pWithdrawAmount, ISavingsContract pSaving)
		{
			// Check Withdraw event amount
			return (ServicesHelper.CheckIfValueBetweenMinAndMax(pSaving.Product.WithdrawingMin, pSaving.Product.WithdrawingMax, pWithdrawAmount));
		}

        private static bool IsTransferAmountCorrect(OCurrency pTransferAmount, ISavingsContract pSaving)
        {
            //Check Transfer amount
            return (ServicesHelper.CheckIfValueBetweenMinAndMax(pSaving.Product.TransferMin, pSaving.Product.TransferMax, pTransferAmount));
        }

        public bool IsCompuslorySavingsValid(Loan pLoan, SavingBookContract pSaving, Client pClient)
        {
            if (pLoan.Product.Currency.Id != pSaving.Product.Currency.Id)
                throw new OpenCbsSavingException(OpenCbsSavingExceptionEnum.LoanHasNotSameCurrency);

            //if (saving.Id == 0) 
            if (new ClientServices(_user).FindTiersByContractId(pLoan.Id).Id != pClient.Id)
                    throw new OpenCbsSavingException(OpenCbsSavingExceptionEnum.LoanHasNotSameClient);

            //if (loan.Id == 0) 
            if (new ClientServices(_user).FindTiersBySavingsId(pSaving.Id).Id != pClient.Id)
                    throw new OpenCbsSavingException(OpenCbsSavingExceptionEnum.LoanHasNotSameClient);

            return true;
        }

        public List<SavingEvent> RepayLoanFromSaving(Loan loan,
                                                     RepaymentEvent repaymentEvent, 
                                                     ISavingsContract savingsContract, 
                                                     DateTime date, 
                                                     OCurrency amount, 
                                                     string description, 
                                                     SqlTransaction sqlTransaction)
        {
            if (savingsContract.GetBalance() - amount < 0)
                throw new OpenCbsSavingException(OpenCbsSavingExceptionEnum.WithdrawAmountIsInvalid);
            
            User user = User.CurrentUser;
            Teller teller = Teller.CurrentTeller;

            if (date.Hour == 0
                && date.Minute == 0
                && date.Second == 0)
            {
                date = new DateTime(date.Year, date.Month, date.Day, TimeProvider.Now.Hour, TimeProvider.Now.Minute,
                                    TimeProvider.Now.Second);
            }

            ISavingsContract savingSimulation = (ISavingsContract) savingsContract.Clone();
            savingSimulation.RepayLoanFromSaving(loan, repaymentEvent.Id, date, amount, description, user, teller);

            List<SavingEvent> events = savingsContract.RepayLoanFromSaving(loan, repaymentEvent.Id, date, amount,
                                                                           description, user, teller);
            foreach (SavingEvent savingEvent in events)
            {
                _ePS.FireEvent(savingEvent, savingsContract, sqlTransaction);
            }
            
            return events;
        }

        public List<SavingEvent> LoanDisbursement(ISavingsContract savings, Loan loan, DateTime date, string description, User user, bool disableFees)
        {
            using(SqlConnection conn =  _savingManager.GetConnection())
            using (SqlTransaction sqlTransaction = conn.BeginTransaction())
            {
                try
                {
                    bool isPending = false;
                    OSavingsMethods savingsMethod = OSavingsMethods.Cash;

                    int? pendingEventId = null;
                    Teller teller = Teller.CurrentTeller;

                    if (date.Hour == 0 && date.Minute == 0 && date.Second == 0)
                        date = new DateTime(date.Year, date.Month, date.Day, TimeProvider.Now.Hour,
                                            TimeProvider.Now.Minute,
                                            TimeProvider.Now.Second);

                    ISavingsContract savingSimulation = (ISavingsContract) savings.Clone();

                    savingSimulation.LoanDisbursement(loan, date, description, user, disableFees, isPending,
                                                      savingsMethod,
                                                      pendingEventId, teller);

                    List<SavingEvent> events = savings.LoanDisbursement(loan, date, description,
                                                                        user, false, isPending, savingsMethod,
                                                                        pendingEventId, teller);

                    foreach (SavingEvent savingEvent in events)
                    {
                        _ePS.FireEvent(savingEvent, savings, sqlTransaction);
                    }

                    // Change overdraft state
                    if (savings is SavingBookContract)
                    {
                        if (savings.GetBalance() > 0)
                        {
                            ((SavingBookContract) savings).InOverdraft = false;
                            UpdateOverdraftStatus(savings.Id, false);
                        }
                    }

                    sqlTransaction.Commit();
                    return events;
                }
                catch (Exception)
                {
                    sqlTransaction.Rollback();
                    throw;
                }
            }
        }

        public List<SavingEvent> Deposit(ISavingsContract saving, DateTime dateTime, OCurrency depositAmount,
            string description, User user, bool isPending, OSavingsMethods savingsMethod, int? pendingEventId, Teller teller)
        {
            using (SqlConnection conn = _savingManager.GetConnection())
            using (SqlTransaction sqlTransaction = conn.BeginTransaction())
            {
                try
                {
                    if (!IsDepositAmountCorrect(depositAmount, saving, savingsMethod))
                        throw new OpenCbsSavingException(OpenCbsSavingExceptionEnum.DepositAmountIsInvalid);

                    ISavingsContract savingSimulation = (ISavingsContract) saving.Clone();
                        // Create a fake Saving object

                    // Do deposit to the fake Saving object
                    savingSimulation.Deposit(depositAmount, dateTime, description, user, false, isPending, savingsMethod,
                                             pendingEventId, teller);

                    if (!IsSavingBalanceCorrect(savingSimulation)) // Check balance simulation
                        throw new OpenCbsSavingException(OpenCbsSavingExceptionEnum.BalanceIsInvalid);

                    List<SavingEvent> events = saving.Deposit(depositAmount, dateTime, description, user, false,
                                                              isPending,
                                                              savingsMethod, pendingEventId, teller);

                    foreach (SavingEvent savingEvent in events)
                    {
                        _ePS.FireEvent(savingEvent, saving, sqlTransaction);
                    }

                    // Change overdraft state
                    if (saving is SavingBookContract)
                    {
                        if (saving.GetBalance() > 0)
                        {
                            ((SavingBookContract) saving).InOverdraft = false;
                            UpdateOverdraftStatus(saving.Id, false);
                        }
                    }
                    sqlTransaction.Commit();
                    return events;
                }
                catch (Exception)
                {
                    sqlTransaction.Rollback();
                    throw;
                }
            }
        }

          public void SpecialOperation(ISavingsContract saving, DateTime pDate, OCurrency amount,
              string description, User pUser, OSavingsMethods savingsMethod, bool isCredit, Booking booking)
       {
           if (booking == null)
               throw new OpenCbsBookingException(OpenCbsBookingExceptionsEnum.BookingIsEmpty);

           booking.Amount = amount;
           booking.Description = description;
           booking.ExchangeRate = 1;
           booking.Date = TimeProvider.Now;
           booking.Currency = saving.Product.Currency;
           booking.User = User.CurrentUser;
           
           SavingEvent e;

           if (isCredit)
           {
               e = SpecialOperationCredit(saving, pDate, amount, description,
                                   User.CurrentUser, savingsMethod);
           }
           else
           {
               e = SpecialOperationDebit(saving, pDate, amount, description,
                                   User.CurrentUser, savingsMethod);
           }

           booking.EventId = e.Id;
           ServicesProvider.GetInstance().GetAccountingServices().BookManualEntry(booking, User.CurrentUser);
       }

          private SavingCreditOperationEvent SpecialOperationCredit(ISavingsContract pSaving, DateTime pDate, OCurrency creditAmount,
           string pDescription, User pUser, OSavingsMethods savingsMethod)
          {
              using (SqlConnection conn = _savingManager.GetConnection())
              using (SqlTransaction sqlTransaction = conn.BeginTransaction())
              {
                  try
                  {
                      //// Create a fake Saving object
                      ISavingsContract savingSimulation = (ISavingsContract) pSaving.Clone();
                      // Do deposit to the fake Saving object
                      savingSimulation.SpecialOperationCredit(creditAmount, pDate, pDescription, pUser);
                      // Check balance simulation
                      if (!IsSavingBalanceCorrect(savingSimulation))
                          throw new OpenCbsSavingException(OpenCbsSavingExceptionEnum.BalanceIsInvalid);

                      SavingCreditOperationEvent events = pSaving.SpecialOperationCredit(creditAmount, pDate,
                                                                                         pDescription, pUser);

                      _ePS.FireEvent(events, pSaving, sqlTransaction);
                      sqlTransaction.Commit();
                      return events;
                  }
                  catch (Exception)
                  {
                      sqlTransaction.Rollback();
                      throw;
                  }
              }
          }

          public SavingDebitOperationEvent SpecialOperationDebit(ISavingsContract pSaving, DateTime pDate, OCurrency debitAmount,
                string pDescription, User pUser, OSavingsMethods savingsMethod)
          {
              using (SqlConnection conn = _savingManager.GetConnection())
              using (SqlTransaction sqlTransaction = conn.BeginTransaction())
              {
                  try
                  {
                      if (pSaving is SavingBookContract)
                      {
                          decimal vBalance = CheckVirtualBalance((SavingBookContract) pSaving, debitAmount);

                          if (vBalance > 0)
                          {
                              List<string> messages = new List<string>
                                                          {
                                                              ServicesHelper.ConvertDecimalToString(
                                                                  ((SavingBookContract) pSaving).GetBalance().Value),
                                                              ServicesHelper.ConvertDecimalToString(vBalance),
                                                              ((SavingBookContract) pSaving).Loans.Count.ToString(),
                                                              ServicesHelper.ConvertDecimalToString(
                                                                  ((SavingBookContract) pSaving).GetBalance().Value -
                                                                  vBalance)
                                                          };
                              throw new OpenCbsSavingException(
                                  OpenCbsSavingExceptionEnum.BalanceOnCurrentSavingAccountForTransfer, messages);
                          }
                      }

                      //// Create a fake Saving object
                      ISavingsContract savingSimulation = (ISavingsContract) pSaving.Clone();
                      // Do deposit to the fake Saving object
                      savingSimulation.SpecialOperationDebit(debitAmount, pDate, pDescription, pUser);
                      // Check balance simulation
                      if (!IsSavingBalanceCorrect(savingSimulation))
                          throw new OpenCbsSavingException(OpenCbsSavingExceptionEnum.BalanceIsInvalid);

                      SavingDebitOperationEvent events = pSaving.SpecialOperationDebit(debitAmount, pDate, pDescription,
                                                                                        pUser);

                      _ePS.FireEvent(events, pSaving, sqlTransaction);

                      // Change overdraft state
                      if (pSaving is SavingBookContract)
                      {
                          if (pSaving.GetBalance() > 0)
                          {
                              ((SavingBookContract) pSaving).InOverdraft = false;
                              UpdateOverdraftStatus(pSaving.Id, false);
                          }
                      }
                      sqlTransaction.Commit();
                      return events;
                  }
                  catch (Exception)
                  {
                      sqlTransaction.Rollback();
                      throw;
                  }
              }
          }

	    /// <summary>
	    /// Checks DepositAmount and balance simulation
	    /// </summary>
	    /// <param name="pSaving"></param>
	    /// <param name="pDate"></param>
	    /// <param name="pWithdrawAmount"></param>
	    /// <param name="pDescription"></param>
	    /// <param name="pUser"></param>
	    /// <returns></returns>
        public List<SavingEvent> Withdraw(ISavingsContract pSaving, DateTime pDate, OCurrency pWithdrawAmount, string pDescription, User pUser, Teller teller)
	    {
	        ValidateWithdrawal(pWithdrawAmount, pSaving, pDate, pDescription, pUser, teller);

	        List<SavingEvent> events = pSaving.Withdraw(pWithdrawAmount, pDate, pDescription, pUser, false, teller);
            using (SqlConnection conn = _savingManager.GetConnection())
            using (SqlTransaction sqlTransaction = conn.BeginTransaction())
	        {
	            try
	            {
	                foreach (SavingEvent savingEvent in events)
	                {
	                    _ePS.FireEvent(savingEvent, pSaving, sqlTransaction);
	                }

	                // Charge overdraft fees if the balance is negative
	                if (pSaving is SavingBookContract)
	                {
	                    if (pSaving.GetBalance() < 0 && !((SavingBookContract) pSaving).InOverdraft)
	                    {
	                        SavingEvent overdraftFeeEvent = pSaving.ChargeOverdraftFee(pDate, pUser);
	                        _ePS.FireEvent(overdraftFeeEvent, pSaving, sqlTransaction);

	                        ((SavingBookContract) pSaving).InOverdraft = true;
	                        UpdateOverdraftStatus(pSaving.Id, true);
	                    }
	                }

	                sqlTransaction.Commit();
	                return events;
	            }
	            catch (Exception)
	            {
	                sqlTransaction.Rollback();
	                throw;
	            }
	        }
	    }

	    private void ValidateWithdrawal(OCurrency pWithdrawAmount, ISavingsContract pSaving, DateTime pDate, string pDescription, User pUser, Teller teller)
	    {
	        if (!IsWithdrawAmountCorrect(pWithdrawAmount, pSaving))
	            throw new OpenCbsSavingException(OpenCbsSavingExceptionEnum.WithdrawAmountIsInvalid);

	        if (pSaving is SavingBookContract)
	        {
	            OCurrency fee;
	            if(((SavingBookContract) pSaving).RateWithdrawFees.HasValue)
	            {
	                fee = ((SavingBookContract) pSaving).RateWithdrawFees.Value*pWithdrawAmount;
	            }
	            else
	            {
	                fee = ((SavingBookContract) pSaving).FlatWithdrawFees;
	            }
	            OCurrency totalAmount = pWithdrawAmount + fee;

	            if (((SavingBookContract)pSaving).GetBalance() - totalAmount < 0)
	                CanUserMakeBalanceNegative();//Check if current user is allowed to make balance negative

	            decimal vBalance = CheckVirtualBalance((SavingBookContract) pSaving, totalAmount);
	            if (vBalance > 0)
	            {
	                List<string> messages = new List<string>
	                                            {
	                                                ServicesHelper.ConvertDecimalToString(((SavingBookContract) pSaving).GetBalance().Value),
	                                                ServicesHelper.ConvertDecimalToString(vBalance),
	                                                ((SavingBookContract) pSaving).Loans.Count.ToString(),
	                                                ServicesHelper.ConvertDecimalToString(((SavingBookContract) pSaving).GetBalance().Value - vBalance)
	                                            };

	                throw new OpenCbsSavingException(OpenCbsSavingExceptionEnum.BalanceOnCurrentSavingAccount, messages);
	            }
	        }

	        // Create a fake Saving object
	        ISavingsContract savingSimulation = (ISavingsContract)pSaving.Clone();
	        // Do withdraw in the fake Saving object
	        savingSimulation.Withdraw(pWithdrawAmount, pDate, pDescription, pUser, false, teller);
	        // Check balance simulation
	        if (!IsSavingBalanceCorrect(savingSimulation))
	            throw new OpenCbsSavingException(OpenCbsSavingExceptionEnum.BalanceIsInvalid);
	    }

	    public List<SavingEvent> Transfer(ISavingsContract from, ISavingsContract to, DateTime date, OCurrency amount, OCurrency fee, string description, User user, bool noFee)
        {
            CheckTransfer(to, from, amount, fee, date, description);
            List<SavingEvent> events = from.Transfer(to, amount, fee, date, description);
            foreach (SavingEvent e in events) 
                _ePS.FireEvent(e);

            return events;
        }

	    private void CheckTransfer(ISavingsContract to, ISavingsContract from, OCurrency amount, OCurrency fee, DateTime date, string description)
	    {
	        if (to == null)
	            throw new OpenCbsSavingException(OpenCbsSavingExceptionEnum.TransferAccountIsInvalid);

	        if (to.Status == OSavingsStatus.Closed)
	            throw new OpenCbsSavingException(OpenCbsSavingExceptionEnum.CreditTransferAccountInvalid);

	        if (from is SavingBookContract)
	        {
	            decimal vBalance = CheckVirtualBalance((SavingBookContract)from, amount + fee);

	            if (vBalance > 0)
	            {
	                List<string> messages = new List<string>
	                                            {
	                                                ServicesHelper.ConvertDecimalToString(((SavingBookContract) from).GetBalance().Value),
	                                                ServicesHelper.ConvertDecimalToString(vBalance),
	                                                ((SavingBookContract) from).Loans.Count.ToString(),
	                                                ServicesHelper.ConvertDecimalToString(((SavingBookContract) from).GetBalance().Value - vBalance)
	                                            };
	                throw new OpenCbsSavingException(
	                    OpenCbsSavingExceptionEnum.BalanceOnCurrentSavingAccountForTransfer, messages);
	            }
	        }

	        if (from.Id == to.Id)
	            throw new OpenCbsSavingException(OpenCbsSavingExceptionEnum.SavingsContractForTransferIdenticals);

	        if (from.Product.Currency.Id != to.Product.Currency.Id)
	            throw new OpenCbsSavingException(OpenCbsSavingExceptionEnum.SavingsContractForTransferNotSameCurrncy);

	        if (!IsTransferAmountCorrect(amount, from))
	                throw new OpenCbsSavingException(OpenCbsSavingExceptionEnum.TransferAmountIsInvalid);
            
            ISavingsContract fromCopy = (ISavingsContract)from.Clone();
            ISavingsContract toCopy = (ISavingsContract)to.Clone();
            fromCopy.Transfer(toCopy, amount, fee, date, description);
            if (!IsSavingBalanceCorrect(fromCopy))
                throw new OpenCbsSavingException(OpenCbsSavingExceptionEnum.BalanceIsInvalid);
	    }

	    private static bool IsCompulsorySavingBalanceOk(ISavingsContract saving, OCurrency amount)
        {
            decimal totalLoansAmount = 0;
            decimal balance = saving.GetBalance().Value;

            foreach (Loan assosiatedLoan in ((SavingBookContract)saving).Loans)
            {
                if (assosiatedLoan.ContractStatus == OContractStatus.Active)
                {
                    if (assosiatedLoan.CompulsorySavingsPercentage != null)
                        totalLoansAmount += (assosiatedLoan.Amount.Value*
                                             ((decimal) assosiatedLoan.CompulsorySavingsPercentage/100));
                }
            }

            if ((balance - amount) < totalLoansAmount)
                 return false;
            return true;
        }

        public SavingEvent CancelLastEvent(ISavingsContract saving, User user, string pDescription)
        {
            if (string.IsNullOrEmpty(pDescription))
                throw new OpenCbsSavingException(OpenCbsSavingExceptionEnum.SavingsEventCommentIsEmpty);
            
            Debug.Assert(_ePS != null, "Event processor is null");
            SavingEvent lastSavingEvent = saving.GetCancelableEvent();
            
            if (null == lastSavingEvent) return null;

            // check for compulsory
            if (saving is SavingBookContract)
            {
                if (lastSavingEvent is SavingDepositEvent)
                {
                    if (((SavingBookContract)saving).Loans != null && ((SavingBookContract)saving).Loans.Count > 0)
                    {
                        if (!IsCompulsorySavingBalanceOk(saving, lastSavingEvent.Amount))
                            throw new OpenCbsSavingException(OpenCbsSavingExceptionEnum.SavingsEventCannotBeCanceled);
                    }
                }
            }

            //reversal transaction in accounting
            if (lastSavingEvent is SavingCreditOperationEvent || lastSavingEvent is SavingDebitOperationEvent)
            {
                if (((SavingBookContract)saving).Loans != null && ((SavingBookContract)saving).Loans.Count > 0)
                {
                    if (!IsCompulsorySavingBalanceOk(saving, lastSavingEvent.Amount))
                        throw new OpenCbsSavingException(OpenCbsSavingExceptionEnum.SavingsEventCannotBeCanceled);
                }

                Booking booking = ServicesProvider.GetInstance().GetAccountingServices().SelectBookingByEventId(lastSavingEvent.Id);
                Booking reversalBooking = new Booking
                                              {
                                                  Currency = booking.Currency,
                                                  User = User.CurrentUser,
                                                  Amount = booking.Amount,
                                                  Description = booking.Description,
                                                  DebitAccount = booking.CreditAccount,
                                                  CreditAccount = booking.DebitAccount,
                                                  Date = TimeProvider.Now,
                                                  EventId = lastSavingEvent.Id,
                                                  ExchangeRate = booking.ExchangeRate,
                                                  Branch = saving.Branch
                                              };
                ServicesProvider.GetInstance().GetAccountingServices().BookManualEntry(reversalBooking, User.CurrentUser);
            }

            // Cancelling event
            lastSavingEvent.Description = pDescription;
            lastSavingEvent = saving.CancelLastEvent();
            _savingEventManager.UpdateEventDescription(lastSavingEvent.Id, pDescription);
            lastSavingEvent.CancelDate = new DateTime(
                                                        TimeProvider.Now.Year,
                                                        TimeProvider.Now.Month,
                                                        TimeProvider.Now.Day,
                                                        TimeProvider.Now.Hour,
                                                        TimeProvider.Now.Minute,
                                                        TimeProvider.Now.Second
                                                      ); 
            _ePS.CancelFireEvent(lastSavingEvent, saving.Product.Currency.Id);

            SavingEvent savingEvent = lastSavingEvent;
            if (lastSavingEvent.PendingEventId != null)
            {
                savingEvent.Code = "SPDE";
                savingEvent.Id = (int)lastSavingEvent.PendingEventId;
                _ePS.CancelFireEvent(savingEvent, saving.Product.Currency.Id);
            }

            using (var sqlTransaction = CoreDomain.DatabaseConnection.GetConnection().BeginTransaction())
            {
                ServicesProvider.GetInstance().GetContractServices().CallInterceptor(
                    new Dictionary<string, object>
                                {
                                    {"Event", lastSavingEvent},
                                    {"Deleted", true},
                                    {"SqlTransaction", sqlTransaction}
                                });
                sqlTransaction.Commit();
            }

            return savingEvent;
        }

        public void DeleteLoanDisbursementSavingsEvent(Loan loan, Event evnt)
        {
            _savingEventManager.DeleteLoanDisbursementSavingsEvent(loan.CompulsorySavings.Id, evnt.Id);
        }

        public void DeleteEvent(SavingEvent savingEvent)
        {
            _savingEventManager.DeleteEventInDatabase(savingEvent);
        }

        public void DeleteRepaymentFromSavingEvent(int loanEventId, SqlTransaction sqlTransaction)
        {
            _savingEventManager.DeleteSavingsEventByLoanEventId(loanEventId, sqlTransaction);
        }

        public List<SavingEvent> RefusePendingDeposit(OCurrency pRefuseAmount, ISavingsContract pSaving, DateTime pDate, User pUser,
            OSavingsMethods method, int? pendingEventId)
        {
            using (SqlConnection conn = _savingManager.GetConnection())
            using (SqlTransaction sqlTransaction = conn.BeginTransaction())
            {
                try
                {
                    List<SavingEvent> events = pSaving.RefusePendingDeposit(pRefuseAmount, pDate, pUser,
                                                                            "Refuse pending deposit", method,
                                                                            pendingEventId);

                    foreach (SavingEvent savingEvent in events)
                        _ePS.FireEvent(savingEvent, pSaving, sqlTransaction);

                    sqlTransaction.Commit();
                    return events;
                }
                catch (Exception)
                {
                    sqlTransaction.Rollback();
                    throw;
                }
            }
        }

	    public List<SavingEvent> Reopen(OCurrency pReopenAmount, ISavingsContract pSaving, DateTime pDate, User pUser, Client pClient)
	    {
            using (SqlConnection conn = _savingManager.GetConnection())
	        using (SqlTransaction sqlTransaction = conn.BeginTransaction())
	        {
	            try
	            {
	                List<SavingEvent> events = pSaving.Reopen(pReopenAmount, pDate, pUser, "Reopen savings account", false);

	                foreach (SavingEvent savingEvent in events)
	                    _ePS.FireEvent(savingEvent, pSaving, sqlTransaction);

	                _savingManager.UpdateStatus(pSaving.Id, pSaving.Status, pSaving.ClosedDate);
	                sqlTransaction.Commit();
	                return events;
	            }
	            catch (Exception)
	            {
	                sqlTransaction.Rollback();
	                throw;
	            }
	        }
	    }

	    public List<SavingEvent> CloseAndWithdraw(ISavingsContract saving, DateTime date, User user, OCurrency withdrawAmount, 
                                                 bool isDesactivateFees, Teller teller)
	    {
	        OCurrency balance = SimulateCloseAccount(saving, date, user, isDesactivateFees, teller).GetBalance(date);
	       
	        if (balance != withdrawAmount)
	        {
	            throw new OpenCbsSavingException(OpenCbsSavingExceptionEnum.WithdrawAmountIsInvalid);
	        }

	        List<SavingEvent> events = saving.Withdraw(withdrawAmount, date, "Withdraw savings", user, true,
	                                                    Teller.CurrentTeller);
	        events.AddRange(saving.Close(date, user, "Close savings contract", isDesactivateFees, teller, false));
            using (SqlConnection conn = _savingManager.GetConnection())
            using (SqlTransaction sqlTransaction = conn.BeginTransaction())
	        {
	            try
	            {
	                foreach (SavingEvent savingEvent in events)
	                    _ePS.FireEvent(savingEvent, saving, sqlTransaction);

	                if (saving.ClosedDate != null)
	                    _savingManager.UpdateStatus(saving.Id, saving.Status, saving.ClosedDate.Value);

	                sqlTransaction.Commit();
	                return events;
	            }
	            catch (Exception)
	            {
	                sqlTransaction.Rollback();
	                throw;
	            }
	        }
	    }

	    public List<SavingEvent> CloseAndTransfer(ISavingsContract from, ISavingsContract to, DateTime date, User pUser,
            OCurrency amount, bool pIsDesactivateFees, Teller teller)
        {
            if (to.Status == OSavingsStatus.Closed)
                throw new OpenCbsSavingException(OpenCbsSavingExceptionEnum.CreditTransferAccountInvalid);

            if (from.Id == to.Id)
                throw new OpenCbsSavingException(OpenCbsSavingExceptionEnum.SavingsContractForTransferIdenticals);

            if (from.Product.Currency.Id != to.Product.Currency.Id)
                throw new OpenCbsSavingException(OpenCbsSavingExceptionEnum.SavingsContractForTransferNotSameCurrncy);

            OCurrency balance = SimulateCloseAccount(from, date, pUser, pIsDesactivateFees, teller).GetBalance(date);
            if (from is SavingBookContract && !pIsDesactivateFees) balance -= ((SavingBookContract)from).CloseFees;

            if (balance != amount)
                throw new OpenCbsSavingException(OpenCbsSavingExceptionEnum.TransferAmountIsInvalid);

            List<SavingEvent> events = new List<SavingEvent>();
            events.AddRange(from.Transfer(to, amount, 0, date, "Closing transfer"));
            events.AddRange(from.Close(date, pUser, "Close savings contract", pIsDesactivateFees, teller, false));
            foreach (SavingEvent e in events)
                _ePS.FireEvent(e);
            
            if (from.ClosedDate != null)
                _savingManager.UpdateStatus(from.Id, from.Status, from.ClosedDate.Value);

            return events;
        }

        public List<SavingEvent> Closure(ISavingsContract saving, DateTime dateTime, User user)
        {
            // Check if closure has been already run today
            List<SavingEvent> events = _savingEventManager.SelectEvents(saving.Id, saving.Product);
            SavingEvent closureEvent =
                events.Where(item => item is SavingClosureEvent).OrderByDescending(item => item.Date).FirstOrDefault();

            if (closureEvent != null && closureEvent.Date == dateTime)
                return null;

            bool inOverdraft = ((SavingBookContract) saving).InOverdraft;

            ((SavingBookContract) saving).AccountAtMaturityEvent += SavingServicesAccountAtMaturity;

            List<SavingEvent> savingEvents = saving.Closure(dateTime, user);
            using (SqlConnection conn = _savingManager.GetConnection())
            using (SqlTransaction sqlTransaction = conn.BeginTransaction())
            {
                try
                {
                    foreach (SavingEvent savingEvent in savingEvents)
                    {
                        if (((SavingBookContract) saving).InOverdraft != inOverdraft)
                        {
                            ((SavingBookContract) saving).InOverdraft = inOverdraft;
                            UpdateOverdraftStatus(saving.Id, inOverdraft);
                        }
                        _ePS.FireEvent(savingEvent, saving, sqlTransaction);
                    }

                    _savingManager.UpdateNextMaturityForSavingBook(saving.Id, ((SavingBookContract) saving).NextMaturity);
                    sqlTransaction.Commit();
                    return events;
                }
                catch (Exception)
                {
                    sqlTransaction.Rollback();
                    throw;
                }
            }
        }

	    public void PostEvents(List<ISavingsContract> savings, List<SavingEvent> savingEvents)
	    {
	        // Check if closure has been already run today
            using (SqlConnection conn = _savingManager.GetConnection())
            using (SqlTransaction sqlTransaction = conn.BeginTransaction())
	        {
	            try
	            {
	                if (savingEvents.Count > 0)
	                {
	                    foreach (ISavingsContract saving in savings)
	                    {
	                        bool inOverdraft = ((SavingBookContract) saving).InOverdraft;

	                        ((SavingBookContract) saving).AccountAtMaturityEvent += SavingServicesAccountAtMaturity;

	                        foreach (SavingEvent savingEvent in savingEvents)
	                        {
	                            if (((SavingBookContract) saving).InOverdraft != inOverdraft)
	                            {
	                                ((SavingBookContract) saving).InOverdraft = inOverdraft;
	                                UpdateOverdraftStatus(saving.Id, inOverdraft);
	                            }

	                            if (savingEvent.ContracId == saving.Id)
	                                _ePS.FireEvent(savingEvent, saving, sqlTransaction);
	                        }

	                        _savingManager.UpdateNextMaturityForSavingBook(saving.Id,
	                                                                       ((SavingBookContract) saving).NextMaturity);
	                    }
	                }
	                sqlTransaction.Commit();
	            }
	            catch (Exception)
	            {
	                sqlTransaction.Rollback();
	                throw;
	            }
	        }
	    }

	    private void SavingServicesAccountAtMaturity(ISavingsContract savingContract, DateTime date, User user)
        {
            if (savingContract.Rollover == OSavingsRollover.None)
                CloseAndTransfer(
                                    savingContract, 
                                    savingContract.TransferAccount, 
                                    date, 
                                    user, 
                                    savingContract.GetBalance(date), 
                                    true, 
                                    Teller.CurrentTeller);
            
            if (savingContract.Rollover == OSavingsRollover.Principal)
            {
                DateTime lastMaturity = DateCalculationStrategy.GetLastMaturity(date,
                                                                                savingContract.Product.Periodicity,
                                                                                savingContract.NumberOfPeriods);
                OCurrency interests = savingContract.Events.Where(
                    item => item is SavingInterestsPostingEvent && item.Date > lastMaturity && item.Date <= date).
                    Sum(item => item.Amount.Value);
                if (decimal.Parse(interests.GetFormatedValue(savingContract.Product.Currency.UseCents)) == 0m)
                    return;

                // TODO: replace the fee of zero with a meaningful value
                
                savingContract.Events.AddRange(
                    Transfer(savingContract, savingContract.TransferAccount, date, interests, 0, "Transfer interests", user, true));
            }
        }

        /// <summary>
        /// This method returns true when operation is allowed
        /// </summary>
        /// <returns></returns>
        public bool CanUserMakeBalanceNegative()
        {
            //this method will work only if user has special rights
            return true;
        }

        /// <summary>
        /// This method returns true when operation is allowed
        /// </summary>
        /// <returns></returns>
        public bool AllowOperationsDuringPendingDeposit()
        {
            //this method will work only if user has special rights
            return true;
        }

        /// <summary>
        /// This method returns true when operation is allowed
        /// </summary>
        /// <returns></returns>
        public bool AllowSettingSavingsOperationsFeesManually()
        {
            return true;
        }

        public List<ISavingsContract> SelectActiveContracts()
        {
            List<ISavingsContract> savingsContracts = _savingManager.SelectAllActive();
            return savingsContracts;
        }

        public void GeneralClosure(DateTime dateTime, User user)
        {
            List<ISavingsContract> listSavingTmp = _savingManager.SelectAll();
            listSavingTmp = listSavingTmp.Where(item => item.Status != OSavingsStatus.Closed).ToList();
            List<ISavingsContract> listSaving =
                new List<ISavingsContract>(listSavingTmp.OfType<SavingBookContract>().Cast<ISavingsContract>());

            for (int i = 0; i < listSaving.Count; i++)
            {
                List<SavingEvent> list = Closure(listSaving[i], dateTime, user);
                if (OnSavingClosureFinished != null && list != null)
                    OnSavingClosureFinished(i + 1, listSaving.Count);
            }
        }

        public List<SavingEvent> SelectEventsForClosure(DateTime beginDate, DateTime endDate, Branch branch)
        {
            //set the date to the end of day to select all events for the day
            endDate.AddHours(23);
            endDate.AddMinutes(59);
            endDate.AddSeconds(59);

            List<SavingEvent> savingEvents = _savingEventManager.SelectEventsForClosure(beginDate, endDate, branch);

            return savingEvents;
        }

        public void MakeEventExported(int savingEventId)
        {
            using (SqlConnection conn = _savingEventManager.GetConnection())
            {
                using (SqlTransaction sqlTransac = conn.BeginTransaction())
                {
                    try
                    {
                        _savingEventManager.MakeEventExported(savingEventId, sqlTransac);
                        sqlTransac.Commit();
                    }
                    catch (Exception)
                    {
                        sqlTransac.Rollback();
                        throw;
                    }
                }
            }
        }

        public void MakeEventExported(int savingEventId, SqlTransaction transaction)
        {
            _savingEventManager.MakeEventExported(savingEventId, transaction);
        }

        public void ChangePendingEventStatus(int savingEventId, bool isPending)
        {
            _savingEventManager.ChangePendingEventStatus(savingEventId, isPending);
        }
        
        public SavingBookContract GetSaving(int savingId)
        {
            SavingBookContract saving = _savingManager.Select(savingId);
            if (saving != null)
            {
                UserServices us = ServicesProvider.GetInstance().GetUserServices();
                saving.SavingsOfficer = us.Find(saving.SavingsOfficer.Id);
            }
            return saving;
        }

        public ISavingsContract GetSaving(string savingCode)
        {
            ISavingsContract saving = _savingManager.Select(savingCode);
            if (saving != null)
            {
                UserServices us = ServicesProvider.GetInstance().GetUserServices();
                saving.SavingsOfficer = us.Find(saving.SavingsOfficer.Id);
            }
            return saving;
        }

        public SavingBookContract GetSavingForLoan(int loanId, bool loadLoans)
        {
            SavingBookContract saving = _savingManager.SelectSavingsByLoanId(loanId, loadLoans);
            if (saving != null)
            {
                UserServices us = ServicesProvider.GetInstance().GetUserServices();
                saving.SavingsOfficer = us.Find(saving.SavingsOfficer.Id);
            }
            return saving;
        }

        public ISavingsContract SimulateCloseAccount(ISavingsContract saving, DateTime date, User user, bool isDesactivateFees, Teller teller)
        {
            ISavingsContract savingSimulation = (ISavingsContract) saving.Clone();
            savingSimulation.SimulateClose(date, user, "Close savings contract", isDesactivateFees, teller);

            return savingSimulation;
        }

        public List<ISavingsContract> GetAllSavings(string pSavingCode)
        {
            List<ISavingsContract> savings = _savingManager.SelectAll(pSavingCode);
            UserServices us = ServicesProvider.GetInstance().GetUserServices();
            foreach (ISavingsContract saving in savings)
            {
                saving.SavingsOfficer = us.Find(saving.SavingsOfficer.Id);
            }
            return savings;
        }

        public List<ISavingsContract> GetSavingsByClientId(int clientId)
        {
            return _savingManager.SelectSavings(clientId);
        }

	    public int GetSavingCount(Client pClient)
        {
            if (pClient == null) throw new OpenCbsTiersSaveException(OpenCbsTiersSaveExceptionEnum.TiersIsNull);
            
            return _savingManager.GetNumberOfSavings(pClient.Id);
        }

        public List<Loan> SelectLoansBySavingsId(int savingsId)
        {
            return _savingManager.SelectLoansBySavingsId(savingsId);
        }

        public bool ValidateSavingsContract(ISavingsContract saving, Client client)
        {
            if (!IsProductCorrect(saving))
                throw new OpenCbsSavingException(OpenCbsSavingExceptionEnum.ProductIsInvalid);
            if (!IsInterestRateCorrect(saving))
                throw new OpenCbsSavingException(OpenCbsSavingExceptionEnum.InterestRateIsInvalid);
            if (!IsWithdrawFeesCorrect((SavingBookContract)saving))
                throw new OpenCbsSavingException(OpenCbsSavingExceptionEnum.WithdrawFeesIsInvalid);

            if (!IsTransferFeesCorrect((SavingBookContract)saving))
                throw new OpenCbsSavingException(OpenCbsSavingExceptionEnum.TransferFeesIsInvalid);

            if (!IsDepositFeesCorrect((SavingBookContract)saving))
                throw new OpenCbsSavingException(OpenCbsSavingExceptionEnum.DepositFeesIsInvalid);

            if (!IsCloseFeesCorrect((SavingBookContract)saving))
                throw new OpenCbsSavingException(OpenCbsSavingExceptionEnum.CloseFeesIsInvalid);

            if (!IsManagementFeesCorrect((SavingBookContract)saving))
                throw new OpenCbsSavingException(OpenCbsSavingExceptionEnum.ManagementFeesIsInvalid);

            if (!IsAgioFeesCorrect((SavingBookContract)saving))
                throw new OpenCbsSavingException(OpenCbsSavingExceptionEnum.AgioFeesIsInvalid);

            if (((SavingBookContract)saving).Loans != null)
            {
                //IsLoanValid((Saving)saving, pClient);

                //if (!_IsLoanAmountCorrect((Saving)saving, ((Saving)saving).Loan))
                //    throw new OpenCbsSavingException(OpenCbsSavingExceptionEnum.InitialAmountIsInvalid);
            }
            else
            {
                if (!IsInitialAmountCorrect(saving))
                    throw new OpenCbsSavingException(OpenCbsSavingExceptionEnum.InitialAmountIsInvalid);
            }
            if (((SavingBookContract)saving).UseTermDeposit)
            {
                if (((SavingBookContract)saving).Rollover != OSavingsRollover.PrincipalAndInterests)
                {
                    if (((SavingBookContract)saving).TransferAccount == null)
                        throw new OpenCbsSavingException(OpenCbsSavingExceptionEnum.TransferAccountIsInvalid);
                }

            }
            
            return true;
        }

        public void FirstDeposit(ISavingsContract saving, OCurrency initialAmount, DateTime creationDate, 
                                 OCurrency entryFees, User user, Teller teller)
        {
            if (!IsEntryFeesCorrect(saving, entryFees))
                throw new OpenCbsSavingException(OpenCbsSavingExceptionEnum.EntryFeesIsInvalid);

            if (!IsSavingBalanceCorrect(saving, initialAmount))
                throw new OpenCbsSavingException(OpenCbsSavingExceptionEnum.BalanceIsInvalid);
            using(SqlConnection conn = _savingManager.GetConnection())
            using (SqlTransaction sqlTransaction = conn.BeginTransaction())
            {
                try
                {
                    saving.FirstDeposit(initialAmount, creationDate, entryFees, user, teller);

                    if (_ePS != null)
                    {
                        foreach (SavingEvent savingEvent in saving.Events)
                        {
                            _ePS.FireEvent(savingEvent, saving, sqlTransaction);
                        }
                    }

                    saving.Status = OSavingsStatus.Active;
                    _savingManager.UpdateStatus(saving.Id, saving.Status, null);
                    sqlTransaction.Commit();
                }
                catch (Exception)
                {
                    sqlTransaction.Rollback();
                    throw;
                }
            }
        }


	    public void CheckIfTransferAccountHasWrongCurrency(ISavingsContract currentContract, SavingSearchResult transferAccount)
        {
            if (currentContract.Product.Currency.Id != transferAccount.CurrencyId)
                throw new OpenCbsSavingException(OpenCbsSavingExceptionEnum.TransferAccountHasWrongCurrency);
        }

        public int SaveContract(ISavingsContract saving, Client client)
        {
            return SaveContract(saving, client, null);
        }

        public int SaveContract(ISavingsContract saving, Client client, Action<SqlTransaction, int> action)
        {
            if (client == null)
                throw new OpenCbsTiersSaveException(OpenCbsTiersSaveExceptionEnum.TiersIsNull);

            ValidateSavingsContract(saving, client);

            int savingsCount = _savingManager.GetNumberOfSavings(client.Id);

            saving.GenerateSavingCode(
                client,
                savingsCount,
                ApplicationSettings.GetInstance(_user.Md5).SavingsCodeTemplate,
                ApplicationSettings.GetInstance(_user.Md5).ImfCode,
                client.Branch.Code
                );

            saving.Status = OSavingsStatus.Pending;

            if (((SavingBookContract) saving).UseTermDeposit)
            {
                ((SavingBookContract) saving).NextMaturity =
                    DateCalculationStrategy.GetNextMaturity(
                        saving.CreationDate,
                        ((SavingBookContract) saving).Product.Periodicity,
                        ((SavingBookContract) saving).NumberOfPeriods);
            }

            using (SqlConnection connection = _savingManager.GetConnection())
            using (SqlTransaction sqlTransaction = connection.BeginTransaction())
            {
                try
                {

                    saving.Id = _savingManager.Add(saving, client, sqlTransaction);
                    string code = saving.Code + '/' + saving.Id.ToString(CultureInfo.InvariantCulture);
                    _savingManager.UpdateSavingContractCode(saving.Id, code, sqlTransaction);
                    if (action != null) action(sqlTransaction, saving.Id);
                    sqlTransaction.Commit();

                    return saving.Id;
                }
                catch (Exception)
                {
                    sqlTransaction.Rollback();
                    throw;
                }
            }
        }

	    public int GetLastSavingsId()
        {
            return _savingManager.GetLastSavingsId();
        }

        public void UpdateContract(SavingBookContract pSaving, Client pClient)
        {
            SavingBookContract oldSavings = (SavingBookContract)_savingManager.Select(pSaving.Id);

            foreach (Loan loan in oldSavings.Loans)
            {
                if (loan != null && (loan.ContractStatus == OContractStatus.Active || loan.ContractStatus == OContractStatus.Validated))
                    throw new OpenCbsSavingException(OpenCbsSavingExceptionEnum.CompulsorySavingsLinkedToLoan);

                if (loan != null)
                    if (new ClientServices(_user).FindTiersByContractId(loan.Id).Id != new ClientServices(_user).FindTiersBySavingsId(pSaving.Id).Id)
                        throw new OpenCbsSavingException(OpenCbsSavingExceptionEnum.LoanHasNotSameClient);
            }
        }

        public List<SavingSearchResult> FindContracts(int pCurrentlyPage, out int pNumbersTotalPage, 
                                                        out int pNumberOfRecords, 
                                                        string pQuery, 
                                                        bool all,
                                                        bool activeContractsOnly)
        {
            pNumberOfRecords = _savingManager.GetNumberSavingContract(pQuery, all, activeContractsOnly);
            List<SavingSearchResult> list = _savingManager.SearchSavingContractByCritere(pCurrentlyPage, pQuery, all, activeContractsOnly);
            UserServices us = ServicesProvider.GetInstance().GetUserServices();
            foreach (SavingSearchResult ssr in list)
            {
                ssr.LoanOfficer = us.Find(ssr.LoanOfficer.Id);
            }

            pNumbersTotalPage = (pNumberOfRecords / 20) + 1;
            return list;
        }

        public KeyValuePair<int, string>[] SelectClientSavingBookCodes(int tiersId, int? currencyId)
        {
            return _savingManager.SelectClientSavingBookCodes(tiersId, currencyId);
        }

        public void UpdateOverdraftStatus(int pSavingId, bool inOverdraft)
        {
            _savingManager.UpdateOverdraftStatus(pSavingId, inOverdraft);
        }
        
        public void UpdateInitialData(int pSavingId, OCurrency initialAmount, OCurrency entryFees)
        {
            _savingManager.UpdateInitialData(pSavingId, initialAmount, entryFees);
        }

        public static void SavingSelected(SavingsContract saving)
        {
            Debug.Assert(saving != null, "Client is null.");
            Debug.Assert(saving.Branch != null, "Branch is null.");
            BranchService bs = ServicesProvider.GetInstance().GetBranchService();
            saving.Branch = bs.FindById(saving.Branch.Id);
        }

        public void PerformBackDateOperations(DateTime date)
        {
            return;
        }

        public void PerformFutureDateOperations(DateTime date)
        {
            return;
        }

        public ISavingsContract AddAndActivateDefaultSavingAccount(IClient client)
        {
            var productList = ServicesProvider.GetInstance()
                                               .GetSavingProductServices()
                                               .FindAllSavingsProducts(false, OClientTypes.All);
            var products = from item in productList where item.Code == "default" select item;
            if (!products.Any()) return null;
            var saving = new SavingBookContract(
                ServicesProvider.GetInstance().GetGeneralSettings(),
                User.CurrentUser,
                TimeProvider.Today,
                (SavingsBookProduct) products.First(),
                client)
                {
                    InterestRate = 0,
                    InitialAmount = 0,
                    EntryFees = 0,
                    DepositFees = 0,
                    ChequeDepositFees = 0,
                    CloseFees = 0,
                    ManagementFees = 0,
                    OverdraftFees = 0,
                    AgioFees = 0,
                    ReopenFees = 0,
                    FlatInterBranchTransferFee = 0,
                    RateInterBranchTransferFee = 0,
                    FlatWithdrawFees = 0,
                    RateWithdrawFees = 0,
                    FlatTransferFees = 0,
                    RateTransferFees = 0,
                    Code = client.Id.ToString(CultureInfo.InvariantCulture),
                    SavingsOfficer = User.CurrentUser
                };

            saving.Id = SaveContract(saving, (Client) client);

            FirstDeposit(saving, 0, TimeProvider.Today, saving.EntryFees, User.CurrentUser, Teller.CurrentTeller);

            return saving;
        }
    }
}
