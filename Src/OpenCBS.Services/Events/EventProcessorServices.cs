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
using OpenCBS.CoreDomain;
using OpenCBS.CoreDomain.Clients;
using OpenCBS.CoreDomain.Contracts.Loans;
using OpenCBS.CoreDomain.Events;
using OpenCBS.CoreDomain.Events.Loan;
using OpenCBS.CoreDomain.Events.Saving;
using OpenCBS.CoreDomain.Events.Teller;
using OpenCBS.CoreDomain.FundingLines;
using OpenCBS.Manager.Accounting;
using System.Data.SqlClient;
using OpenCBS.Manager.Clients;
using OpenCBS.Manager.Contracts;
using OpenCBS.Manager.Events;
using OpenCBS.Manager.Products;
using OpenCBS.Shared;
using OpenCBS.CoreDomain.Contracts.Savings;

namespace OpenCBS.Services.Events
{
    [Serializable]
	public class EventProcessorServices
	{
        private readonly User _user = new User();
		private readonly EventManager _eventManagement;
        private readonly SavingEventManager _savingEventManagement;
        private readonly AccountingTransactionManager _movementSetManagement;
        private readonly LoanManager _loanManager;
        private readonly Accounting.AccountingServices _accountingServices;
        private readonly LoanProductManager _packageManager;
        private readonly ClientManager _clientManagement;
        private IEventProcessor _eP;

        public EventProcessorServices(User pUser,string testDB)
        {
            _user = pUser;

            _eventManagement = new EventManager(testDB);
            _savingEventManagement = new SavingEventManager(testDB);
            _movementSetManagement = new AccountingTransactionManager(testDB);
            _loanManager = new LoanManager(testDB);
            _packageManager = new LoanProductManager(testDB);
            _clientManagement = new ClientManager(testDB);
            _accountingServices = new Accounting.AccountingServices(testDB);

            _InitializeEventProcessor();
        }

        public EventProcessorServices(User pUser)
        {
            _user = pUser;
            _eventManagement = new EventManager(_user);
            _savingEventManagement = new SavingEventManager(_user);
            _movementSetManagement = new AccountingTransactionManager(_user);
            _loanManager = new LoanManager(_user);
            _packageManager = new LoanProductManager(_user);
            _accountingServices = new Accounting.AccountingServices(_user);
            _clientManagement = new ClientManager(_user, false, false);
            _InitializeEventProcessor();
        }

		public EventProcessorServices(EventManager eventManagement) 
		{
			_eventManagement = eventManagement;
            _InitializeEventProcessor();
		}

        public EventProcessorServices(SavingEventManager savingEventManagement, AccountingTransactionManager movementSetManagement, AccountManager accountManager)
        {
            _savingEventManagement = savingEventManagement;
            _movementSetManagement = movementSetManagement;
            _InitializeEventProcessor();
        }
		
		public EventProcessorServices(EventManager eventManagement,AccountingTransactionManager movementSetManagement,LoanManager loanManager,
            AccountManager accountManagement)
		{
			_eventManagement = eventManagement;
			_movementSetManagement = movementSetManagement;
			_loanManager = loanManager;
            _InitializeEventProcessor();
		}

        private void _InitializeEventProcessor()
        {
             
        }

        public void FireEvent(Event e, Loan contract, SqlTransaction sqlTransac)
		{
            e.IsFired = true;

			if(e is LoanDisbursmentEvent)
			{
                LoanDisbursmentOrigination((LoanDisbursmentEvent)e, contract, sqlTransac);
			}
            else if (e is TrancheEvent)
            {
                TrancheEventOrigination((TrancheEvent)e, contract, sqlTransac);
            }
			else if (e is RescheduleLoanEvent)
			{
                ReschedulingOfALoanOrigination((RescheduleLoanEvent)e, contract, sqlTransac);
			}
            else if (e is RepaymentEvent)
            {
                LoanRepaymentOrigination((RepaymentEvent)e, contract, sqlTransac);
            }
            else if (e is OverdueEvent)
            {
                OverdueEventOrigination((OverdueEvent)e, contract, sqlTransac);
            }
            else if (e is ProvisionEvent)
            {
                ProvisionEventOrigination((ProvisionEvent)e, contract, sqlTransac);
            }
            else if (e is WriteOffEvent)
            {
                WriteOffOrigination((WriteOffEvent)e, contract, sqlTransac);
            }
            else if (e is AccruedInterestEvent)
            {
                LoanInterestAccruingOrigination((AccruedInterestEvent)e, contract, sqlTransac);
            }
            else if (e is LoanValidationEvent)
            {
                LoanValidationOrigination((LoanValidationEvent)e, contract, sqlTransac);
            }
            else if (e is CreditInsuranceEvent)
            {
                CreditInsuranceOrigination((CreditInsuranceEvent) e, contract, sqlTransac);
            }
            else if (e is LoanCloseEvent)
            {
                LoanCloseOrigination((LoanCloseEvent) e, contract, sqlTransac);
            }
            else if (e is ManualScheduleChangeEvent)
            {
                ManualScheduleChangeOrigination((ManualScheduleChangeEvent) e, contract, sqlTransac);
            }
		}

        public void FireSavingBlockEvent(SavingBlockCompulsarySavingsEvent savingBlockEvent, int contracId, SqlTransaction sqlTransac)
        {
            _savingEventManagement.Add(savingBlockEvent, contracId, sqlTransac);
        }

        public void FireSavingUnblockEvent(SavingUnblockCompulsorySavingsEvent savingUnblockEvent, int contractId, int loanRepayEventId, SqlTransaction sqlTransac)
        {
            savingUnblockEvent.LoanEventId = loanRepayEventId;
            _savingEventManagement.Add(savingUnblockEvent, contractId, sqlTransac);
        }

        private void CreditInsuranceOrigination(CreditInsuranceEvent insuranceEvent, Loan contract, SqlTransaction sqlTransaction)
        {
            insuranceEvent.Id =
                _eventManagement.AddLoanEventHead(insuranceEvent, contract.Id, sqlTransaction);
            _eventManagement.AddCreditInsuranceEvent(insuranceEvent, sqlTransaction);
        }

        private void LoanValidationOrigination(LoanValidationEvent pLoanValidationEvent, Loan pContract, SqlTransaction sqlTransac)
        {
            _eventManagement.AddLoanEvent(pLoanValidationEvent, pContract.Id, sqlTransac);
        }

        private void LoanCloseOrigination(LoanCloseEvent pLoanCloseEvent, Loan pContract, SqlTransaction sqlTransac)
        {
            _eventManagement.AddLoanEvent(pLoanCloseEvent, pContract.Id, sqlTransac);
        }

        public void FireEvent(SavingEvent e, ISavingsContract pSaving, SqlTransaction sqlTransac)
        {
            e.IsFired = true;
            SavingEventOrigination(e, pSaving, sqlTransac);
        }

        public void FireTellerEvent(Event e)
        {
            using (SqlConnection connection = _eventManagement.GetConnection())
            using (SqlTransaction transaction = connection.BeginTransaction())
            {
                try
                {
                    _eventManagement.AddTellerEvent((TellerEvent) e, transaction);
                    transaction.Commit();
                }
                catch(Exception)
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

       public void FireEvent(SavingEvent e)
        {
            Debug.Assert(e.Target != null, "Saving event's target is null");
            e.IsFired = true;
            using (SqlConnection conn = _savingEventManagement.GetConnection())
            using (SqlTransaction t = conn.BeginTransaction())
            {
                try
                {
                    SavingEventOrigination(e, e.Target, t);
                    t.Commit();
                }
                catch (Exception)
                {
                    t.Rollback();
                    throw;
                }
            }
        }

        private void SavingEventOrigination(SavingEvent savingEvent, ISavingsContract savingsContract, SqlTransaction sqlTransac)
        {
            _savingEventManagement.Add(savingEvent, savingsContract.Id, sqlTransac);
        }

        private void LoanInterestAccruingOrigination(AccruedInterestEvent loanInterestAccruingEvent, Loan loanContract, SqlTransaction sqlTransac)
        {
            _eventManagement.AddLoanEvent(loanInterestAccruingEvent, loanContract.Id, sqlTransac);
        }

		public void CancelFireEvent(Event e, SqlTransaction sqlTransac, Loan loanContract, int currencyId)
		{
           e.CancelDate = new DateTime(
                                            TimeProvider.Now.Year,
                                            TimeProvider.Now.Month,
                                            TimeProvider.Now.Day,
                                            TimeProvider.Now.Hour,
                                            TimeProvider.Now.Minute,
                                            TimeProvider.Now.Second
                                        );
           CancelEventOrigination(e, sqlTransac);
		}

        public void CancelFireEvent(SavingEvent savingEvent, int currencyId)
        {
            _savingEventManagement.DeleteEventInDatabase(savingEvent);
        }

        private void LoanRepaymentOrigination(RepaymentEvent repaymentEvent, Loan loanContract, SqlTransaction sqlTransac)
		{
            int parentId = -1;
            foreach (RepaymentEvent loanEvent in loanContract.Events.GetLoanRepaymentEvents())
            {
                if (loanEvent.IsFired) continue;
                
                loanEvent.User = repaymentEvent.User;
                loanEvent.TellerId = repaymentEvent.TellerId;
                loanEvent.IsFired = true;

                if (parentId > 0)
                    loanEvent.ParentId = parentId;

                loanEvent.Id = _eventManagement.AddLoanEventHead(loanEvent, loanContract.Id, sqlTransac);
                
                if (parentId < 0)
                {
                    parentId = loanEvent.Id;
                    repaymentEvent.Id = parentId;
                }
                
                _eventManagement.AddLoanEvent(loanEvent, loanContract.Id, sqlTransac);
            } 
		}

        private void LoanDisbursmentOrigination(LoanDisbursmentEvent loanDisbursmentEvent, Loan loanContract, SqlTransaction sqlTransac)
		{
           int eventId = _eventManagement.AddLoanEvent(loanDisbursmentEvent, loanContract.Id, sqlTransac);
           loanDisbursmentEvent.Id = eventId;
           if (loanDisbursmentEvent.Commissions != null)
           {
               foreach (LoanEntryFeeEvent entryFeeEvent in loanDisbursmentEvent.Commissions)
               {
                   if (entryFeeEvent.Fee == 0) continue;
                   entryFeeEvent.DisbursementEventId = eventId;
                   entryFeeEvent.User = loanDisbursmentEvent.User;
                   entryFeeEvent.TellerId = loanDisbursmentEvent.TellerId;

                   if (loanContract.CompulsorySavings != null) 
                       entryFeeEvent.Comment = loanContract.CompulsorySavings.Code;

                   int feeEventId = _eventManagement.AddLoanEventHead(entryFeeEvent, loanContract.Id, sqlTransac);
                   entryFeeEvent.Id = feeEventId;
                   if (entryFeeEvent.Fee != 0) _eventManagement.AddLoanEntryFeesEvent(entryFeeEvent, sqlTransac);
               }
           }
		}

        private void CancelEventOrigination(Event e, SqlTransaction sqlTransac)
		{
			_eventManagement.DeleteLoanEvent(e, sqlTransac);
		}

        private void TrancheEventOrigination(TrancheEvent trancheEvent, Loan pContract, SqlTransaction sqlTransac)
        {
            _eventManagement.AddLoanEvent(trancheEvent, pContract.Id, sqlTransac);
        }

        private void ReschedulingOfALoanOrigination(RescheduleLoanEvent rescheduleLoanEvent, Loan pContract, SqlTransaction sqlTransac)
		{
            _eventManagement.AddLoanEvent(rescheduleLoanEvent, pContract.Id, sqlTransac);
		}

        private void ProvisionEventOrigination(ProvisionEvent provisionEvent, Loan loanContract, SqlTransaction sqlTransac)
        {
            _eventManagement.AddLoanEvent(provisionEvent, loanContract.Id, sqlTransac);
        }
        
        private void OverdueEventOrigination(OverdueEvent overdueEvent, Loan loanContract, SqlTransaction sqlTransac)
        {
            _eventManagement.AddLoanEvent(overdueEvent, loanContract.Id, sqlTransac);
        }

        private void WriteOffOrigination(WriteOffEvent writeOffEvent, Loan loanContract, SqlTransaction sqlTransac)
		{
            _eventManagement.AddLoanEvent(writeOffEvent, loanContract.Id, sqlTransac);
		}
        
        private void ManualScheduleChangeOrigination(ManualScheduleChangeEvent manualScheduleChangeEvent,
                                                     Loan loanContract, SqlTransaction sqlTransac)
        {
            _eventManagement.AddLoanEvent(manualScheduleChangeEvent, loanContract.Id, sqlTransac);
        }

        public void FireFundingLineEvent(Event e, FundingLine fundingLine, SqlTransaction sqlTransac)
        {
            e.IsFired = true;
            FundingLineEvent fL = e as FundingLineEvent;
            //_movementSetManagement.AddTransaction(mS, sqlTransac);
        }

        public EventStock SelectEvents(string eventType, int userId, DateTime beginDate, DateTime endDate)
        {
            return _eventManagement.SelectEvents(eventType, userId, beginDate, endDate);
        }

        public void LogUser(string eventCode, string eventDescription, int userId)
        {
            _eventManagement.WriteLog(eventCode, eventDescription, userId);
        }

        public List<EventType> SelectEventTypes()
        {
            return _eventManagement.SelectEventTypes();
        }

        public List<EventType> SelectEventTypesForAccounting()
        {
            return _eventManagement.SelectEventTypesForAccounting();
        }

        public EventStock SelectEventsForClosure(DateTime beginDate, DateTime endDate, Branch branch)
        {
            EventStock eventStock = _eventManagement.SelectEventsForClosure(beginDate, endDate, branch);
            return eventStock;
        }

        public List<TellerEvent> GetTellerEventsForClosure(DateTime beginDate, DateTime endDate)
        {
            return _eventManagement.SelectTellerEventsForClosure(beginDate, endDate); 
        }

        public List<EventAttribute> SelectEventAttributes(string eventType)
        {
            return _eventManagement.SelectEventAttributes(eventType);
        }

        public List<AuditTrailEvent> SelectAuditTrailEvents(AuditTrailFilter filter)
        {
            return _eventManagement.SelectAuditTrailEvents(filter);
        }

        public void ExportEvent(int eventId)
        {
            using (SqlConnection conn = _movementSetManagement.GetConnection())
            {
                using (SqlTransaction sqlTransac = conn.BeginTransaction())
                {
                    try
                    {
                        _eventManagement.ExportEvent(eventId, sqlTransac);
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

        public void ExportTellerEvent(int eventId, SqlTransaction transaction)
        {
            _eventManagement.ExportTellerEvent(eventId, transaction);
        }

        public void ExportEvent(int eventId, SqlTransaction transaction)
        {
            _eventManagement.ExportEvent(eventId, transaction);
        }

        public void LogClientSaveUpdateEvent(IClient client, bool save)
        {
            _eventManagement.LogClientSaveUpdateEvent(client.ToString(), save, User.CurrentUser.Id);
        }

        public void UpdateCommentForLoanEvent(Event evnt, SqlTransaction sqlTransac)
        {
            _eventManagement.UpdateCommentForLoanEvent(evnt, sqlTransac);
        }

        public bool IsEventExported(int id)
        {
            return _eventManagement.IsEventExported(id);
        }
	}
}
