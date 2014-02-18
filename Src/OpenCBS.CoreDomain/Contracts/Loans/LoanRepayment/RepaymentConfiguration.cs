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
using OpenCBS.CoreDomain.Accounting;
using OpenCBS.CoreDomain.Clients;
using OpenCBS.CoreDomain.Contracts.Savings;
using OpenCBS.Shared;

namespace OpenCBS.CoreDomain.Contracts.Loans.LoanRepayment
{
    public class RepaymentConfiguration
    {
        public IClient Client { get; set; }
        public Loan Loan { get; set; }
        public ISavingsContract Saving { get; set; }
        public int InstallmentNumber { get; set; }
        public DateTime Date { get; set; }
        public OCurrency Amount { get; set; }
        public bool DisableFees { get; set; }
        public OCurrency ManualFeesAmount { get; set; }
        public OCurrency ManualCommission { get; set; }
        public bool DisableInterests { get; set; }
        public OCurrency ManualInterestsAmount { get; set; }
        public bool KeepExpectedInstallment { get; set; }
        public bool ProportionPayment { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public string Comment { get; set; }
        public bool IsPending { get; set; }
    }
}
