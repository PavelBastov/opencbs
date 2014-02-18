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
using OpenCBS.Enums;

namespace OpenCBS.Shared.Settings
{
    public class ParamChangedEventArgs : EventArgs
    {
        private readonly string _key = string.Empty;
        private readonly object _value = null;

        public ParamChangedEventArgs(string pKey, object pValue)
        {
            _key = pKey;
            _value = pValue;
        }

        public string Key
        {
            get { return _key; }
        }

        public object Value
        {
            get { return _value; }
        }
    }

    public delegate void ParamChangedEventHandler(object sender, ParamChangedEventArgs e);

    [Serializable]
    public class ApplicationSettings
    {
        private static readonly IDictionary<string, ApplicationSettings> _generalsSettings = new Dictionary<string, ApplicationSettings>();

        public event ParamChangedEventHandler ParamChanged;

        private void FillDefaultParamList()
        {
            _defaultParamList.Add(OGeneralSettings.COUNTRY, "Not set");
            _defaultParamList.Add(OGeneralSettings.WEEKENDDAY1, 6);
            _defaultParamList.Add(OGeneralSettings.WEEKENDDAY2, 0);
            _defaultParamList.Add(OGeneralSettings.PAYFIRSTINSTALLMENTREALVALUE, 1);
            _defaultParamList.Add(OGeneralSettings.CITYMANDATORY, 1);
            _defaultParamList.Add(OGeneralSettings.AUTOMATIC_ID, 0);
            _defaultParamList.Add(OGeneralSettings.GROUPMINMEMBERS, 2);
            _defaultParamList.Add(OGeneralSettings.GROUPMAXMEMBERS, 10);
            _defaultParamList.Add(OGeneralSettings.VILLAGEMINMEMBERS, 0);
            _defaultParamList.Add(OGeneralSettings.VILLAGEMAXMEMBERS, 10);
            _defaultParamList.Add(OGeneralSettings.CLIENT_AGE_MIN, 18);
            _defaultParamList.Add(OGeneralSettings.CLIENT_AGE_MAX, 100);
            _defaultParamList.Add(OGeneralSettings.MAX_GUARANTOR_AMOUNT, 2000000000);
            _defaultParamList.Add(OGeneralSettings.MAX_LOANS_COVERED, 1);
            _defaultParamList.Add(OGeneralSettings.CITYOPENVALUE, 1);
            _defaultParamList.Add(OGeneralSettings.ACCOUNTINGPROCESS, 1);
            _defaultParamList.Add(OGeneralSettings.LATEDAYSAFTERACCRUALCEASES, null);
            _defaultParamList.Add(OGeneralSettings.ALLOWSMULTIPLELOANS, 0);
            _defaultParamList.Add(OGeneralSettings.ALLOWSMULTIPLEGROUPS, 0);
            _defaultParamList.Add(OGeneralSettings.CALCULATIONLATEFEESDURINGPUBLICHOLIDAYS, 1);
            _defaultParamList.Add(OGeneralSettings.NAME_FORMAT, "L U");
            _defaultParamList.Add(OGeneralSettings.DONOTSKIPWEEKENDSININSTALLMENTSDATE, 0);
            _defaultParamList.Add(OGeneralSettings.INCREMENTALDURINGDAYOFF, 1);
            _defaultParamList.Add(OGeneralSettings.CONTRACT_CODE_TEMPLATE, "BC/YY/PC-LC/ID");
            _defaultParamList.Add(OGeneralSettings.USEPROJECTS, 0);
            _defaultParamList.Add(OGeneralSettings.INTERESTS_ALSO_CREDITED_IN_FL, 0);
            _defaultParamList.Add(OGeneralSettings.ENFORCE_ID_PATTERN, 0);
            _defaultParamList.Add(OGeneralSettings.ID_WILD_CHAR_CHECK, 0);
            _defaultParamList.Add(OGeneralSettings.ID_PATTERN, "[A-Z]{2}[0-9]{7}");
            _defaultParamList.Add(OGeneralSettings.SAVINGS_CODE_TEMPLATE, "IC/BC/CS/ID");
            _defaultParamList.Add(OGeneralSettings.IMF_CODE, "IMF");
            _defaultParamList.Add(OGeneralSettings.MAX_NUMBER_INSTALLMENT, "MAX_NUMBER_INSTALLMENT");
            _defaultParamList.Add(OGeneralSettings.PENDING_SAVINGS_MODE, "NONE");
            _defaultParamList.Add(OGeneralSettings.BAD_LOAN_DAYS, "180");
            _defaultParamList.Add(OGeneralSettings.VAT_RATE, 0);
            _defaultParamList.Add(OGeneralSettings.CEASE_LAIE_DAYS, 1000);
            _defaultParamList.Add(OGeneralSettings.USE_TELLER_MANAGEMENT, 0);
            _defaultParamList.Add(OGeneralSettings.CONSOLIDATION_MODE, 0);
            _defaultParamList.Add(OGeneralSettings.INTEREST_RATE_DECIMAL_PLACES, 2);
            _defaultParamList.Add(OGeneralSettings.STOP_WRITEOFF_PENALTY, 0);
            _defaultParamList.Add(OGeneralSettings.MODIFY_ENTRY_FEE, 0);
            _defaultParamList.Add(OGeneralSettings.USE_MANDATORY_SAVING_ACCOUNT, 0);
            _defaultParamList.Add(OGeneralSettings.USE_DAILY_ACCRUAL_OF_PENALTY, 0);
        }

        #region Internal stuff

        //private static ApplicationSettings _theUniqueInstance;
        private Hashtable _dbParamList;
        private readonly Hashtable _defaultParamList;
        
        public static void SuppressRemotingInfos(string pMd5)
        {
            if (_generalsSettings.ContainsKey(pMd5))
            {
                _generalsSettings.Remove(pMd5);
            }
        }

        private ApplicationSettings()
        {
            _dbParamList = new Hashtable();
            _defaultParamList = new Hashtable();
            FillDefaultParamList();
        }


        public static ApplicationSettings GetInstance(string pMd5)
        {
            if (!_generalsSettings.ContainsKey(pMd5))
                _generalsSettings.Add(pMd5, new ApplicationSettings());
            return _generalsSettings[pMd5];
        }

        public void AddParameter(string key, object val)
        {
            if (val is bool)
                if (((bool)val)) _dbParamList.Add(key, "1"); else _dbParamList.Add(key, "0");

            else
                _dbParamList.Add(key, val);
        }

        private void FireParamChanged(ParamChangedEventArgs e)
        {
            if (ParamChanged != null)
            {
                ParamChanged(this, e);
            }
        }

        public void UpdateParameter(string key, object value)
        {
            if (value is bool)
            {
                if (((bool)value))
                {
                    _dbParamList[key] = "1";
                } 
                else
                {
                    _dbParamList[key] = "0";
                }
            }

            else
            {
                _dbParamList[key] = value;
            }
            var args = new ParamChangedEventArgs(key, value);
            FireParamChanged(args);
        }

        public void DeleteAllParameters()
        {
            _dbParamList = new Hashtable();
        }

        public Hashtable DbParamList
        {
            get { return _dbParamList; }
        }

        public Hashtable DefaultParamList
        {
            get { return _defaultParamList; }
        }

        public object GetSpecificParameter(string name)
        {
            if (_dbParamList.ContainsKey(name))
                return _dbParamList[name];
            return null;
        }

        #endregion

        public bool UseTellerManagement
        {
            get { return GetSpecificParameter(OGeneralSettings.USE_TELLER_MANAGEMENT).ToString() == "1"; }
        }

        public string IDPattern
        {
            get
            {
                 return (string)GetSpecificParameter(OGeneralSettings.ID_PATTERN);
            }
        }
        public bool IsAllowMultipleLoans
        {
            get
            {
                return GetSpecificParameter(OGeneralSettings.ALLOWSMULTIPLELOANS).ToString() == "1";
            }
        }

        public bool IsAllowMultipleGroups
        {
            get
            {
                return GetSpecificParameter(OGeneralSettings.ALLOWSMULTIPLEGROUPS).ToString() == "1";
            }
        }

        public bool UseProjects
        {
            get { return GetSpecificParameter(OGeneralSettings.USEPROJECTS).ToString() == "1"; }
        }

        public bool IsCalculationLateFeesDuringHolidays
        {
            get
            {
                return GetSpecificParameter(OGeneralSettings.CALCULATIONLATEFEESDURINGPUBLICHOLIDAYS).ToString() == "1";
            }
        }
 
        public int? LateDaysAfterAccrualCeases
        {
            get {
                return GetSpecificParameter(OGeneralSettings.LATEDAYSAFTERACCRUALCEASES) == null || Convert.ToString(GetSpecificParameter(OGeneralSettings.LATEDAYSAFTERACCRUALCEASES)) == ""
                           ? 0
                           : Convert.ToInt32(GetSpecificParameter(OGeneralSettings.LATEDAYSAFTERACCRUALCEASES));
            }
        }

        public int GroupMinMembers
        {
            get { return Convert.ToInt32(GetSpecificParameter(OGeneralSettings.GROUPMINMEMBERS)); }
        }

        public int GroupMaxMembers
        {
            get { return Convert.ToInt32(GetSpecificParameter(OGeneralSettings.GROUPMAXMEMBERS)); }
        }

        public  int ClientAgeMin
        {
            get { return Convert.ToInt32(GetSpecificParameter(OGeneralSettings.CLIENT_AGE_MIN)); }
        }

        public int ClientAgeMax
        {
            get { return Convert.ToInt32(GetSpecificParameter(OGeneralSettings.CLIENT_AGE_MAX)); }
        }

        public int MaxLoansCovered
        {
            get { return Convert.ToInt32(GetSpecificParameter(OGeneralSettings.MAX_LOANS_COVERED)); }
        }

        public  int MaxGuarantorAmount
        {
            get { return Convert.ToInt32(GetSpecificParameter(OGeneralSettings.MAX_GUARANTOR_AMOUNT)); }    
        }

        public int VillageMinMembers
        {
            get { return Convert.ToInt32(GetSpecificParameter(OGeneralSettings.VILLAGEMINMEMBERS)); }
        }

        public int VillageMaxMembers
        {
            get { return Convert.ToInt32(GetSpecificParameter(OGeneralSettings.VILLAGEMAXMEMBERS)); }
        }

        public OAccountingProcesses AccountingProcesses
        {
            get
            {
                return (OAccountingProcesses)Convert.ToInt32(GetSpecificParameter(OGeneralSettings.ACCOUNTINGPROCESS));
            }
        }

        public bool IsCityMandatory
        {
            get { return GetSpecificParameter(OGeneralSettings.CITYMANDATORY).ToString() == "1"; }
        }

        public bool IsAutomaticID
        {
            get { return GetSpecificParameter(OGeneralSettings.AUTOMATIC_ID).ToString() == "1"; }
        }

        public bool IsStopWriteOffPenalty
        {
            get { return GetSpecificParameter(OGeneralSettings.STOP_WRITEOFF_PENALTY).ToString() == "1"; }
        }

        public bool ModifyEntryFee
        {
            get { return GetSpecificParameter(OGeneralSettings.MODIFY_ENTRY_FEE).ToString() == "1"; }
        }

        public bool PayFirstInterestRealValue
        {
            get
            {
                return GetSpecificParameter(OGeneralSettings.PAYFIRSTINSTALLMENTREALVALUE).ToString() == "1";
            }
        }

        public bool IsCityAnOpenValue
        {
            get
            {
                return GetSpecificParameter(OGeneralSettings.CITYOPENVALUE).ToString() == "1";
            }
        }

        public bool IsOlbBeforeRepayment
        {
            get
            {
                return GetSpecificParameter(OGeneralSettings.OLBBEFOREREPAYMENT).ToString() == "1";
            }
        }

        public string Country
        {
            get
            {
                return GetSpecificParameter(OGeneralSettings.COUNTRY).ToString();
            }
        }

        public int ReportTimeout
        {
            get { return Convert.ToInt32(GetSpecificParameter(OGeneralSettings.REPORT_TIMEOUT)); }
        }

        public string MfiName
        {
            get
            {
                return (string)GetSpecificParameter(OGeneralSettings.MFI_NAME);
            }
        }

        public int MaxNumberInstallment
        {
            get
            {
                return Convert.ToInt32(GetSpecificParameter(OGeneralSettings.MAX_NUMBER_INSTALLMENT));
            }
        }

        /// <summary>
        /// 0 = sunday, 1 = monday...
        /// </summary>
        public int CeaseLateDays
        {
            get
            {
                return Convert.ToInt32(GetSpecificParameter(OGeneralSettings.CEASE_LAIE_DAYS));
            }
        }

        public int WeekEndDay1
        {
            get
            {
                return Convert.ToInt32(GetSpecificParameter(OGeneralSettings.WEEKENDDAY1));
            }
        }
        /// <summary>
        /// 0 = sunday, 1 = monday...
        /// </summary>
        public int WeekEndDay2
        {
            get
            {
                return Convert.ToInt32(GetSpecificParameter(OGeneralSettings.WEEKENDDAY2));
            }
        }

        public int InterestRateDecimalPlaces
        {
            get
            {
                return Convert.ToInt32(GetSpecificParameter(OGeneralSettings.INTEREST_RATE_DECIMAL_PLACES));
            }
        }

        public bool InterestsCreditedInFL
        {
            get
            {
                return GetSpecificParameter(OGeneralSettings.INTERESTS_ALSO_CREDITED_IN_FL).ToString() == "1";
            }
        }
        public bool IDWildCharCheck
        {
            get 
            {
                return GetSpecificParameter(OGeneralSettings.ID_WILD_CHAR_CHECK).ToString() == "1";
            }
        }
        public bool EnforceIDPattern
        {
            get
            {
                return GetSpecificParameter(OGeneralSettings.ENFORCE_ID_PATTERN).ToString() == "1";
            }
        }
        public bool DoNotSkipNonWorkingDays
        {
            get
            {
                return GetSpecificParameter(OGeneralSettings.DONOTSKIPWEEKENDSININSTALLMENTSDATE).ToString() == "1";
            }
        }

        public bool IsIncrementalDuringDayOff
        {
            get
            {
                return GetSpecificParameter(OGeneralSettings.INCREMENTALDURINGDAYOFF).ToString() == "1";
            }
        }

        public string FirstNameFormat
        {
            get
            {
                var format = (string) GetSpecificParameter(OGeneralSettings.NAME_FORMAT);
                var retval = "L";
                if (!string.IsNullOrEmpty(format))
                {
                    var items = format.Trim().Split(' ');
                    if (items.Length >= 1)
                    {
                        retval = items[0];
                    }
                }
                return retval;
            }
        }

        public string LastNameFormat
        {
            get
            {
                var format = (string) GetSpecificParameter(OGeneralSettings.NAME_FORMAT);
                var retval = "U";
                if (!string.IsNullOrEmpty(format))
                {
                    var items = format.Trim().Split();
                    if (2 == items.Length)
                    {
                        retval = items[1];
                    }
                }
                return retval;
            }
        }

        public string ContractCodeTemplate
        {
            get { return (string)GetSpecificParameter(OGeneralSettings.CONTRACT_CODE_TEMPLATE); }
        }

        public string SavingsCodeTemplate
        {
            get { return (string)GetSpecificParameter(OGeneralSettings.SAVINGS_CODE_TEMPLATE); }
        }

        public string ImfCode
        {
            get { return (string)GetSpecificParameter(OGeneralSettings.IMF_CODE); }
        }

        public string PendingSavingsMode
        {
            get { return GetSpecificParameter(OGeneralSettings.PENDING_SAVINGS_MODE).ToString(); }
        }

        public int BadLoanDays
        {
            get { return Convert.ToInt32(GetSpecificParameter(OGeneralSettings.BAD_LOAN_DAYS).ToString()); }
        }

        public int VatRate
        {
            get
            {
                object value = GetSpecificParameter(OGeneralSettings.VAT_RATE);
                return Convert.ToInt32(value);
            }
        }

        public int GetDaysInAYear(int year)
        {
            int days = 0;
            for (int i = 1; i <= 12; i++)
            {
                days += DateTime.DaysInMonth(year, i);
            }
            return days;
        }

        public bool ConsolidationMode
        {
            get
            {
                return "1" == GetSpecificParameter(OGeneralSettings.CONSOLIDATION_MODE).ToString();
            }
        }

        public bool UseMandatorySavingAccount
        {
            get { return GetSpecificParameter(OGeneralSettings.USE_MANDATORY_SAVING_ACCOUNT).ToString() == "1"; }
        }

        public bool UseDailyAccrualOfPenalty
        {
            get { return GetSpecificParameter(OGeneralSettings.USE_DAILY_ACCRUAL_OF_PENALTY).ToString() == "1"; }
        }
    }
}
