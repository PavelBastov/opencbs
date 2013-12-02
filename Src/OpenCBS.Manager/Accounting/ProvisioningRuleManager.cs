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
using OpenCBS.CoreDomain;
using OpenCBS.CoreDomain.Accounting;

namespace OpenCBS.Manager.Accounting
{
    public class ProvisioningRuleManager : Manager
    {
        public ProvisioningRuleManager(string pTestDB) : base(pTestDB) { }
        public ProvisioningRuleManager(User pUser) : base(pUser) { }


        public void AddProvisioningRate(ProvisioningRate pR, SqlTransaction sqlTransac)
        {
            const string sqlText = @"INSERT INTO ProvisioningRules(
                                        id,
                                        number_of_days_min, 
                                        number_of_days_max, 
                                         provisioning_value,
                                       provisioning_interest,
                                       provisioning_penalty)
                                    VALUES(
                                      @number,
                                      @numberOfDaysMin, 
                                      @numberOfDaysMax, 
                                      @ProvisioningOlb,
                                      @ProvisioningInterest,
                                      @ProvisioningPenalty) 
                                   SELECT SCOPE_IDENTITY()";

            using (OpenCbsCommand insert = new OpenCbsCommand(sqlText, sqlTransac.Connection, sqlTransac))
            {
                SetProvisioningRate(insert, pR);
                insert.ExecuteScalar();
            }
        }

        private static void SetProvisioningRate(OpenCbsCommand octCommand, ProvisioningRate pProvisionningRate)
        {
            octCommand.AddParam("@number", pProvisionningRate.Number);
            octCommand.AddParam("@numberOfDaysMin", pProvisionningRate.NbOfDaysMin);
            octCommand.AddParam("@numberOfDaysMax", pProvisionningRate.NbOfDaysMax);
            octCommand.AddParam("@ProvisioningOlb", pProvisionningRate.ProvisioningValue);
            octCommand.AddParam("@ProvisioningInterest", pProvisionningRate.ProvisioningInterest);
            octCommand.AddParam("@ProvisioningPenalty", pProvisionningRate.ProvisioningPenalty);
        }

        ///// <summary>
        ///// This method Fill the instance of the ProvisioningTable object accessed by singleton
        ///// </summary>
        public List<ProvisioningRate> SelectAllProvisioningRates()
        {
            List<ProvisioningRate> list = new List<ProvisioningRate>();

            const string sqlText = @"SELECT 
                                       id,
                                       number_of_days_min, 
                                       number_of_days_max, 
                                       provisioning_value,
                                       provisioning_interest,
                                       provisioning_penalty   
                                     FROM ProvisioningRules";
            using (SqlConnection conn = GetConnection())
            {
                using (OpenCbsCommand select = new OpenCbsCommand(sqlText, conn))
                {
                    using (OpenCbsReader reader = select.ExecuteReader())
                    {
                        if (reader.Empty) return list;
                        while (reader.Read())
                        {
                            list.Add(GetProvisionningRate(reader));
                        }
                        return list;
                    }
                }
            }
        }

        private static ProvisioningRate GetProvisionningRate(OpenCbsReader reader)
        {
            return new ProvisioningRate
                       {
                           Number = reader.GetInt("id"),
                           NbOfDaysMin = reader.GetInt("number_of_days_min"),
                           NbOfDaysMax = reader.GetInt("number_of_days_max"),
                           ProvisioningValue = reader.GetDouble("provisioning_value"),
                           ProvisioningInterest = reader.GetDouble("provisioning_interest"),
                           ProvisioningPenalty = reader.GetDouble("provisioning_penalty")
                       };
        }

        public void DeleteAllProvisioningRules(SqlTransaction sqlTransac)
        {
            const string sqlText = "DELETE FROM ProvisioningRules";
            using (OpenCbsCommand delete = new OpenCbsCommand(sqlText, sqlTransac.Connection, sqlTransac))
            {
                delete.ExecuteNonQuery();
            }
        }
    }
}
