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

using System.Collections.Generic;
using System.Data.SqlClient;
using NUnit.Framework;
using OpenCBS.CoreDomain.Accounting;
using OpenCBS.Manager.Accounting;

namespace OpenCBS.Test.Manager.Accounting
{
    [TestFixture]
    public class TestProvisioningRuleManager : BaseManagerTest
    {
        [Test]
        public void AddProvisionningRateInDatabase()
        {
            ProvisioningRuleManager provisioningRuleManager = (ProvisioningRuleManager) container["ProvisioningRuleManager"];
            ProvisioningRate rate = new ProvisioningRate {Number = 12, NbOfDaysMin = 1000, NbOfDaysMax = 1111, ProvisioningValue = 2, ProvisioningInterest=2, ProvisioningPenalty=2};
            SqlTransaction transaction = provisioningRuleManager.GetConnection().BeginTransaction();
            provisioningRuleManager.AddProvisioningRate(rate, transaction);
            transaction.Commit();
        }

        [Test]
        public void SelectProvisioningRates()
        {
            ProvisioningRuleManager provisioningRuleManager = (ProvisioningRuleManager)container["ProvisioningRuleManager"];
            List<ProvisioningRate> list = provisioningRuleManager.SelectAllProvisioningRates();

            Assert.AreEqual(7, list.Count);

            _AssertProvisioningRule(list[0], 1, 0, 0, 0.02, 0.02, 0.02);
            _AssertProvisioningRule(list[1], 2, 1, 30, 0.1, 0.01, 0.01);
            _AssertProvisioningRule(list[2], 3, 31, 60, 0.25, 0.025, 0.025);
            _AssertProvisioningRule(list[3], 4, 61, 90, 0.5, 0.5, 0.5);
            _AssertProvisioningRule(list[4], 5, 91, 180, 0.75, 0.75, 0.75);
            _AssertProvisioningRule(list[5], 6, 181, 365, 1, 1,1);
            _AssertProvisioningRule(list[6], 7, 366, 99999, 1,1,1);
        }

        private static void _AssertProvisioningRule(ProvisioningRate pProvisioningRate, int pNumber, int pNbOfDaysMin,
                                                    int pNbOfDaysMax, double pOlb, double pInterest, double pPenalty )
        {
            Assert.AreEqual(pNumber, pProvisioningRate.Number);
            Assert.AreEqual(pNbOfDaysMin, pProvisioningRate.NbOfDaysMin);
            Assert.AreEqual(pNbOfDaysMax, pProvisioningRate.NbOfDaysMax);
            Assert.AreEqual(pOlb, pProvisioningRate.ProvisioningValue);
            Assert.AreEqual(pInterest, pProvisioningRate.ProvisioningInterest);
            Assert.AreEqual(pPenalty, pProvisioningRate.ProvisioningPenalty);
        }

        [Test]
        public void TestDeleteProvisionningRules()
        {
            ProvisioningRuleManager provisioningRuleManager = (ProvisioningRuleManager)container["ProvisioningRuleManager"];
            SqlTransaction transaction = provisioningRuleManager.GetConnection().BeginTransaction();
            provisioningRuleManager.DeleteAllProvisioningRules(transaction);
            transaction.Commit();

            List<ProvisioningRate> list = provisioningRuleManager.SelectAllProvisioningRates();

            Assert.AreEqual(0, list.Count);
        }
    }
}
