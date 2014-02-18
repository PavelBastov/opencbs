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
using System.Data;
using System.Collections.Generic;
using System.Data.SqlClient;
using OpenCBS.DatabaseConnection;
using OpenCBS.ExceptionsHandler.Exceptions.AccountExceptions;
using OpenCBS.Manager.Accounting;
using OpenCBS.CoreDomain;
using OpenCBS.ExceptionsHandler;
using OpenCBS.Reports;
using OpenCBS.Shared;
using OpenCBS.CoreDomain.Accounting;
using OpenCBS.Services.Currencies;
using OpenCBS.Enums;

namespace OpenCBS.Services.Accounting
{
    public class AccountingServices : ContextBoundObject
	{
		private readonly AccountingTransactionManager _movementSetManagement;
        private readonly ExchangeRateServices _exchangeRateServices;
        private readonly User _user;
     
		#region constructors

        public AccountingServices(User pUser)
        {
            _user = pUser;
            _exchangeRateServices = new ExchangeRateServices(_user);
            _movementSetManagement = new AccountingTransactionManager(pUser);
        }

        public AccountingServices(string testDb)
		{
            //_user = new User();
            _exchangeRateServices = new ExchangeRateServices(testDb);
			_movementSetManagement = new AccountingTransactionManager(testDb);
			ConnectionManager.GetInstance(testDb);
		}

		#endregion

		public void DoLoanMovement(Booking booking)
        {
            using (SqlConnection conn = _movementSetManagement.GetConnection())
            {
                SqlTransaction sqlTransac = conn.BeginTransaction();
                try
                {
                    _movementSetManagement.InsertLoanMovement(booking, sqlTransac);
                    sqlTransac.Commit();
                }
                catch (Exception)
                {
                    sqlTransac.Rollback();
                    throw;
                }
            }
        }

        public int CreateClosure(int count)
        {
           return _movementSetManagement.CreateClosure(count);
        }

        public List<AccountingClosure> SelectAccountingClosures()
        {
            return _movementSetManagement.SelectAccountingClosures();
        }

        public void DeleteClosure(int closureId)
        {
            if (!_movementSetManagement.IsDeletable(closureId))
                throw new OpenCbsBookingException(OpenCbsBookingExceptionsEnum.NotDeletableClosure);

            using (SqlConnection conn = _movementSetManagement.GetConnection())
            {
                SqlTransaction sqlTransac = conn.BeginTransaction();
                try
                {
                    _movementSetManagement.DeleteClosure(closureId, sqlTransac);
                    sqlTransac.Commit();
                }
                catch (Exception ex)
                {
                    sqlTransac.Rollback();
                    throw ex;
                }
            }
        }

        public List<Account> GetTrialBalance(DateTime pBeginDate, DateTime pEndDate, int currencyId, int pBranchId)
        {
            return _movementSetManagement.GetTrialBalance(pEndDate, pEndDate, currencyId, pBranchId);
        }

        public void DoMovements(List<Booking> bookings)
        {
            using (SqlConnection conn = _movementSetManagement.GetConnection())
            {
                using (SqlTransaction sqlTransac = conn.BeginTransaction())
                {
                    try
                    {
                        int closureId =
                            ServicesProvider.GetInstance().GetAccountingServices().CreateClosure(bookings.Count);

                        foreach (Booking booking in bookings)
                        {
                            booking.ClosureId = closureId;
                        }

                        foreach (Booking booking in bookings)
                        {
                            switch (booking.Type)
                            {
                                case OMovementType.Loan:
                                    _movementSetManagement.InsertLoanMovement(booking, sqlTransac);
                                    ServicesProvider.GetInstance().GetEventProcessorServices().ExportEvent(
                                        booking.EventId,
                                        sqlTransac);
                                    break;
                                case OMovementType.Saving:
                                    _movementSetManagement.InsertSavingMovment(booking, sqlTransac);
                                    ServicesProvider.GetInstance().GetSavingServices().MakeEventExported(
                                        booking.EventId,
                                        sqlTransac);
                                    break;
                                case OMovementType.Manual:
                                    _movementSetManagement.UpdateManualMovment(booking, sqlTransac);
                                    break;
                                case OMovementType.Teller:
                                    _movementSetManagement.InsertLoanMovement(booking, sqlTransac);
                                    ServicesProvider.GetInstance().GetEventProcessorServices().ExportTellerEvent(
                                        booking.EventId, sqlTransac);
                                    break;
                            }
                        }

                        sqlTransac.Commit();
                    }
                    catch (Exception ex)
                    {
                        sqlTransac.Rollback();
                        throw ex;
                    }
                }
            }
        }

        public void DoSavingMovement(Booking booking)
        {
            using (SqlConnection conn = _movementSetManagement.GetConnection())
            {
                SqlTransaction sqlTransac = conn.BeginTransaction();
                try
                {
                    _movementSetManagement.InsertSavingMovment(booking, sqlTransac);
                    sqlTransac.Commit();
                }
                catch (Exception)
                {
                    sqlTransac.Rollback();
                    throw;
                }                
            }
        }

        public List<Booking> SelectMovementsForReverse(List<ExchangeRate> rates, List<FiscalYear> fiscalYears)
        {
            return _movementSetManagement.SelectMovementsForReverse(rates, fiscalYears);
        }

        public List<Booking> SelectClosureMovments(int closureId)
        {
            return _movementSetManagement.SelectClosureMovments(closureId);
        }

        public OCurrency GetAccountBalance(int accountId, int currencyId, int contractId, int? mode, int toSumParent, int branchId)
        {
            return _movementSetManagement.GetAccountBalance(accountId, currencyId, contractId, mode, toSumParent, branchId);
        }

        public OCurrency GetAccountCategoryBalance(int categoryId, int currencyId, int contractId, int? mode)
        {
            return _movementSetManagement.GetAccountCategoryBalance(categoryId, currencyId, contractId, mode);
        }

        public void BookManualEntry(Booking booking, User currentUser)
		{
            using (SqlConnection conn = _movementSetManagement.GetConnection())
            {
                SqlTransaction sqlTransac = conn.BeginTransaction();
                try
                {
                    if (booking.Amount <= 0)
                        throw new OpenCbsAccountException(OpenCbsAccountExceptionsEnum.IncorrectAmountFormat);

                    if (booking.DebitAccount.Id == booking.CreditAccount.Id)
                    {
                        throw new OpenCbsAccountException(OpenCbsAccountExceptionsEnum.EqualAccounts);
                    }

                    _movementSetManagement.InsertManualMovment(booking, sqlTransac);
                    sqlTransac.Commit();
                }
                catch (Exception ex)
                {
                    sqlTransac.Rollback();
                    throw ex;
                }
            }
		}

		public AccountingTransaction FindMovementSet(int movementSetId, SqlTransaction pSqlTransaction)
		{
            return null;
		}

        public List<Booking> SelectMovements(bool all, List<ExchangeRate> rates, List<FiscalYear> fiscalYears)
        {
            return _movementSetManagement.SelectManualMovements(all, rates, fiscalYears);
        }

        public Booking SelectProvisionMovments(int contractId, List<ExchangeRate> rates, List<FiscalYear> fiscalYears)
        {
            return _movementSetManagement.SelectProvisionMovments(contractId,rates, fiscalYears);
        }
        
        public Booking SelectBookingByEventId(int eventId)
        {
            return _movementSetManagement.SelectBookingByEventId(eventId);
        }

        public ExchangeRate FindExchangeRate(DateTime pDate, Currency pCurrency)
        {
            ExchangeRate exchangeRate = null;
            Currency pivot = new CurrencyServices(_user).GetPivot();

            if (!pivot.Equals(pCurrency))
            {
                if (new CurrencyServices(_user).FindAllCurrencies().Count > 1)
                {
                    exchangeRate = _exchangeRateServices.SelectExchangeRate(pDate.Date, pCurrency);
                    if (exchangeRate == null)
                        throw new OpenCbsExchangeRateException(OpenCbsExchangeRateExceptionEnum.ExchangeRateIsNull);
                }
            }
            else
            {
                exchangeRate = new ExchangeRate
                                   {
                                       Currency = pCurrency,
                                       Date = pDate,
                                       Rate = 1
                                   };
            }
            return exchangeRate;
        }

        public ExchangeRate FindLatestExchangeRate(DateTime pDate,Currency pCurrency)
        {

            ExchangeRate exchangeRate = null;
             Currency pivot = new CurrencyServices(_user).GetPivot();
            if (!pivot.Equals(pCurrency))
            {
                if (new CurrencyServices(_user).FindAllCurrencies().Count > 1)
                {
                    double rate = _exchangeRateServices.GetMostRecentlyRate(pDate,pCurrency);
                    
                    exchangeRate = new ExchangeRate
                                       {
                                           Currency = pCurrency,
                                           Date = pDate,
                                           Rate = rate
                                       };
                }
            }
            else
            {
                exchangeRate = new ExchangeRate
                {
                    Currency = pCurrency,
                    Date = pDate,
                    Rate = 1
                };
            }
            return exchangeRate;
        }

        public OCurrency ConvertAmountToExternalCurrency(OCurrency amount, ExchangeRate exchangeRate)
		{
			return amount * 1 / exchangeRate.Rate;
		}

        public List<string> SelectExportAccountingProcNames()
        {
            return _movementSetManagement.SelectExportAccountingProcNames();
        }

        public Dictionary<string, string> SelectExportAccountingProcParams(string procName)
        {
            return _movementSetManagement.SelectExportAccountingProcParams(procName);
        }

        public DataTable FindElementaryMvtsToExport(string procName, List<ReportParamV2> procParams, out DataTable idTable)
        {
            return _movementSetManagement.SelectBookingsToExport(procName, procParams, out idTable);
        }

		public void UpdateElementaryMvtExportedValue(string idSet, int transactionType)
        {
            _movementSetManagement.UpdateSelectedBooking(idSet, true, transactionType);
        }

        public BookingToViewStock FindAllBookings(Account pAccount, DateTime pBeginDate, DateTime pEndDate, int currencyId ,OBookingTypes pBookingType, int pBranchId)
        {
            return _movementSetManagement.SelectBookings(pAccount, pBeginDate, pEndDate, currencyId, pBookingType,
                                                         pBranchId);
        }
	}
}
