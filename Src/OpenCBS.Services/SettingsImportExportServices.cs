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
using OpenCBS.CoreDomain;
using OpenCBS.CoreDomain.Products;
using OpenCBS.Enums;
using OpenCBS.Shared;
using System.Xml.Serialization;
using OpenCBS.CoreDomain.Accounting;
using OpenCBS.Shared.Settings;

namespace OpenCBS.Services
{
    #region DTOs
    [Serializable]
    public class Setting
    {
        public Setting() {}
        public Setting(string pName, string pValue)
        {
            Name = pName;
            Value = pValue;
        }
        public string Name{ get; set;}
        public string Value { get; set; }
        //public LoanProduct Package;
    }

    [Serializable]
    public class SettingGroup
    {
        public SettingGroup() {}
        public SettingGroup(string pName)
        {
            Name = pName;
        }

        public void Add(Setting pSetting)
        {
            _settings.Add(pSetting);
        }
        [XmlAttributeAttribute("Name")]	
        public string Name{ get; set;}
        private List<Setting> _settings = new List<Setting>();
        [XmlArray("Setting"), XmlArrayItem("Setting", typeof(Setting))]
        public Setting[] Settings
        {
            get
            {
                Setting[] setting = new Setting[_settings.Count];
                for (int i = 0; i < _settings.Count; i++)
                {
                    setting[i] = _settings[i];
                }
                return setting;
            }
            set
            {
                foreach (Setting setting in value)
                {
                    _settings.Add(setting);
                }
            }
        }
    }

    [Serializable]
    public class Settings
    {
        private List<SettingGroup> _Groups = new List<SettingGroup>();
        public void Add(SettingGroup pGroup)
        {
            _Groups.Add(pGroup);
        }
        [XmlArray("SettingGroup"), XmlArrayItem("SettingGroup", typeof(SettingGroup))]
        public SettingGroup[] Groups
        {
            get
            {
                SettingGroup[] groups = new SettingGroup[_Groups.Count];
                for (int i = 0; i < _Groups.Count; i++ )
                {
                    groups[i] = _Groups[i];
                }
                return groups;
            }

            set
            {
                foreach (SettingGroup group in value)
                {
                    _Groups.Add(group);
                }
            }
        }

        public Setting MatchNames(SettingGroup pGroup, Setting pSetting)
        {
            foreach (SettingGroup group in _Groups)
            {
                if (group.Name == pGroup.Name)
                {
                    foreach (Setting s in group.Settings)
                    {
                        if (s.Name == pSetting.Name) return s;
                    }
                }
            }
            return null;
        }
    }

    #endregion

    public class SettingsImportExportServices : MarshalByRefObject
    {
        public const string GENERAL_PARAMETERS = "GeneralParameters";
        public const string PROVISIONING_RULES = "ProvisioningRules";
        public const string PUBLIC_HOLIDAYS = "PublicHolidays";
        public const string PACKAGES = "Packages";
        private readonly User _user;

        public SettingsImportExportServices(User pUser)
        {
            _user = pUser;
        }

        public Settings GetCurrentSettings(bool pIncludePackages)
        {
            Settings settings = new Settings();
            // General parameters
            SettingGroup generalParameters = new SettingGroup(GENERAL_PARAMETERS);
            settings.Add(generalParameters);
            ApplicationSettings gp = ApplicationSettings.GetInstance(_user.Md5);
            
           
            generalParameters.Add(new Setting(OGeneralSettings.ACCOUNTINGPROCESS, ((int) gp.AccountingProcesses).ToString()));
            generalParameters.Add(new Setting(OGeneralSettings.ALLOWSMULTIPLELOANS, gp.IsAllowMultipleLoans.ToString()));
            generalParameters.Add(new Setting(OGeneralSettings.ALLOWSMULTIPLEGROUPS, gp.IsAllowMultipleGroups.ToString()));

            generalParameters.Add(new Setting(OGeneralSettings.BAD_LOAN_DAYS, gp.BadLoanDays.ToString()));

            generalParameters.Add(new Setting(OGeneralSettings.CALCULATIONLATEFEESDURINGPUBLICHOLIDAYS, gp.IsCalculationLateFeesDuringHolidays.ToString()));
            generalParameters.Add(new Setting(OGeneralSettings.CEASE_LAIE_DAYS, gp.CeaseLateDays.ToString()));
            generalParameters.Add(new Setting(OGeneralSettings.CITYMANDATORY, gp.IsCityMandatory.ToString()));
            generalParameters.Add(new Setting(OGeneralSettings.CITYOPENVALUE, gp.IsCityAnOpenValue.ToString()));
            generalParameters.Add(new Setting(OGeneralSettings.CONSOLIDATION_MODE, gp.ConsolidationMode.ToString()));
            generalParameters.Add(new Setting(OGeneralSettings.CONTRACT_CODE_TEMPLATE, gp.ContractCodeTemplate));
            generalParameters.Add(new Setting(OGeneralSettings.COUNTRY, gp.Country));
            generalParameters.Add(new Setting(OGeneralSettings.AUTOMATIC_ID, gp.IsAutomaticID.ToString()));

            generalParameters.Add(new Setting(OGeneralSettings.DONOTSKIPWEEKENDSININSTALLMENTSDATE, gp.DoNotSkipNonWorkingDays.ToString())); 
            
            generalParameters.Add(new Setting(OGeneralSettings.ENFORCE_ID_PATTERN, gp.EnforceIDPattern.ToString()));

            generalParameters.Add(new Setting(OGeneralSettings.GROUPMINMEMBERS, gp.GroupMinMembers.ToString()));
            generalParameters.Add(new Setting(OGeneralSettings.GROUPMAXMEMBERS, gp.GroupMaxMembers.ToString()));

            generalParameters.Add(new Setting(OGeneralSettings.VILLAGEMINMEMBERS, gp.VillageMinMembers.ToString()));
            generalParameters.Add(new Setting(OGeneralSettings.VILLAGEMAXMEMBERS, gp.VillageMaxMembers.ToString()));

            generalParameters.Add(new Setting(OGeneralSettings.CLIENT_AGE_MIN, gp.ClientAgeMin.ToString()));
            generalParameters.Add(new Setting(OGeneralSettings.CLIENT_AGE_MAX, gp.ClientAgeMax.ToString()));

            generalParameters.Add(new Setting(OGeneralSettings.MAX_LOANS_COVERED, gp.MaxLoansCovered.ToString()));
            generalParameters.Add(new Setting(OGeneralSettings.MAX_GUARANTOR_AMOUNT, gp.MaxGuarantorAmount.ToString()));

            generalParameters.Add(new Setting(OGeneralSettings.ID_WILD_CHAR_CHECK, gp.IDWildCharCheck.ToString()));
            generalParameters.Add(new Setting(OGeneralSettings.ID_PATTERN, gp.IDPattern));
            generalParameters.Add(new Setting(OGeneralSettings.INTERESTS_ALSO_CREDITED_IN_FL, gp.InterestsCreditedInFL.ToString()));
            generalParameters.Add(new Setting(OGeneralSettings.INCREMENTALDURINGDAYOFF, gp.IsIncrementalDuringDayOff.ToString()));
            generalParameters.Add(new Setting(OGeneralSettings.IMF_CODE, gp.ImfCode));

            generalParameters.Add(new Setting(OGeneralSettings.LATEDAYSAFTERACCRUALCEASES, gp.LateDaysAfterAccrualCeases.ToString()));

            generalParameters.Add(new Setting(OGeneralSettings.MAX_NUMBER_INSTALLMENT, gp.MaxNumberInstallment.ToString())); ;
            generalParameters.Add(new Setting(OGeneralSettings.MFI_NAME, gp.MfiName));

            generalParameters.Add(new Setting(OGeneralSettings.NAME_FORMAT, gp.FirstNameFormat));

            generalParameters.Add(new Setting(OGeneralSettings.OLBBEFOREREPAYMENT, gp.IsOlbBeforeRepayment.ToString()));

            generalParameters.Add(new Setting(OGeneralSettings.PAYFIRSTINSTALLMENTREALVALUE, gp.PayFirstInterestRealValue.ToString()));
            generalParameters.Add(new Setting(OGeneralSettings.PENDING_SAVINGS_MODE, gp.PendingSavingsMode));

            generalParameters.Add(new Setting(OGeneralSettings.SAVINGS_CODE_TEMPLATE, gp.SavingsCodeTemplate));

            generalParameters.Add(new Setting(OGeneralSettings.USE_TELLER_MANAGEMENT, gp.UseTellerManagement.ToString()));
            generalParameters.Add(new Setting(OGeneralSettings.USEPROJECTS, gp.UseProjects.ToString()));

            generalParameters.Add(new Setting(OGeneralSettings.VAT_RATE, gp.VatRate.ToString()));

            generalParameters.Add(new Setting(OGeneralSettings.WEEKENDDAY1, gp.WeekEndDay1.ToString()));
            generalParameters.Add(new Setting(OGeneralSettings.WEEKENDDAY2, gp.WeekEndDay2.ToString()));
            
            generalParameters.Add(new Setting(OGeneralSettings.INTEREST_RATE_DECIMAL_PLACES, Convert.ToString(gp.InterestRateDecimalPlaces)));
            generalParameters.Add(new Setting(OGeneralSettings.STOP_WRITEOFF_PENALTY, Convert.ToString(gp.IsStopWriteOffPenalty.ToString())));
            generalParameters.Add(new Setting(OGeneralSettings.MODIFY_ENTRY_FEE, Convert.ToString(gp.ModifyEntryFee.ToString())));

            // Provisioning Rules
            SettingGroup provisioningRules = new SettingGroup(PROVISIONING_RULES);
            settings.Add(provisioningRules);
            foreach (ProvisioningRate prate in ProvisionTable.GetInstance(_user).ProvisioningRates)
            {
                provisioningRules.Add(new Setting(prate.NbOfDaysMin + "-" + prate.NbOfDaysMax, prate.ProvisioningValue.ToString()));
            }

            // Public Holidays
            SettingGroup publicHolidays = new SettingGroup(PUBLIC_HOLIDAYS);
            settings.Add(publicHolidays);
            foreach (DateTime entry in ServicesProvider.GetInstance().GetNonWorkingDate().PublicHolidays.Keys)
            {
                publicHolidays.Add(new Setting(ServicesProvider.GetInstance().GetNonWorkingDate().PublicHolidays[entry], (entry).ToString("dd/MM/yyyy")));
            }
            //if (pIncludePackages)
            //{
            //    // Packages
            //    SettingGroup packages = new SettingGroup(PACKAGES);
            //    settings.Add(packages);
            //    List<LoanProduct> allPackages = new ProductServices(_user).FindAllPackages(false, OClientTypes.Both);
            //    foreach (LoanProduct package in allPackages)
            //    {
            //        Setting p = new Setting(package.Name, null);
            //        //p.Package = package;
            //        packages.Add(p);
            //    }
            //}
            return settings;            
        }
        public void ApplySettings(Settings pSettings)
        {
            foreach (SettingGroup group in pSettings.Groups)
            {
                switch (group.Name)
                {
                    case GENERAL_PARAMETERS:
                        ApplyGeneralParameters(group);
                        break;

                    case PROVISIONING_RULES:
                        ApplyProvisioningRules(group);
                        break;

                    case PUBLIC_HOLIDAYS:
                        ApplyPublicHoliday(group);
                        break;  
                        
                    case PACKAGES:
                        ApplyPackages(group);
                        break;
                }
            }
        }
        private void ApplyPackages(SettingGroup group)
        {
            throw new Exception("The method or operation is not implemented.");
        }
        private void ApplyPublicHoliday(SettingGroup group)
        {
            ApplicationSettingsServices service = new ApplicationSettingsServices(_user);
            foreach (Setting s in group.Settings)
            {
                DictionaryEntry entry = new DictionaryEntry(s.Value, s.Name);
                service.DeleteNonWorkingDate(entry);
                service.AddNonWorkingDate(entry);
            }
        }
        private void ApplyGeneralParameters(SettingGroup group)
        {
            ApplicationSettingsServices service = new ApplicationSettingsServices(_user);
            foreach (Setting s in group.Settings)
            {
                service.UpdateSelectedParameter(s.Name, s.Value);
            }
        }
        private void ApplyProvisioningRules(SettingGroup group)
        {
            foreach (Setting s in group.Settings)
            {
                foreach (ProvisioningRate rate in ProvisionTable.GetInstance(_user).ProvisioningRates)
                {
                    if(rate.NbOfDaysMin + "-" + rate.NbOfDaysMax == s.Name)
                    {
                        rate.ProvisioningValue = Convert.ToDouble(s.Value);
                    }
                }
            }
            ServicesProvider.GetInstance().GetChartOfAccountsServices().UpdateProvisioningTableInstance();
        }
    }
}
