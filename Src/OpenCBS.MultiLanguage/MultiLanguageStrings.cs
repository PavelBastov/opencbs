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

using System.Collections;
using System.Resources;

namespace OpenCBS.MultiLanguageRessources
{
    public enum Ressource
    {
        FrmSplash,
        AccountView,
        AddGuarantorForm,
        FrmAddLoanProduct,
        AddressUserControl,
        ApplicationExceptionHandler,
        AuditTrailView,
        BadLoanRepaymentForm,
        CashPrevisionForm,
        CashReceiptForm,
        ChartOfAccountsForm,
        Common,
        ContractReschedulingForm,
        CreditContractDisbursmentForm,
        CreditContractForm,
        CreditContractRepayForm,
        DateTimeUserControl,
        ElemMvtUserControl,
        ExchangeRateForm,
        ExportBookingsForm,
        FastChoiceForm,
        ReassingContract,
        FrmActivity,
        FrmFundingLine,
        FrmFundingLineEvent,
        FrmAdvancedSettings,
        FrmApplicationSetup,
        FrmBackup,
        FrmBranchConsolidations,
        FrmCollateralsCud,
        FrmCollateralsR,
        FrmDatabaseSettings,
        FrmGlobalConsolidations,
        FrmReportViewer,
        FrmRestore,
        FrmContractBookings,
        GeneralSettings,
        GroupForm,
        GroupUserControl,
        InternalReportsForm,
        LotrasmicMainWindowForm,
        ManualEntryForm,
        MonthlyClosureForm,
        NewExportConso,
        PackagesForm,
        PasswordForm,
        PasswordFormOnline,
        ClientForm,
        PersonUserControl,
        SearchClientForm,
        SearchCreditContractForm,
        StringRes,
        UserForm,
        GuaranteeContractForm,
        DatabaseMaitenance,
        CorporateUserControl,
        FrmCustomReportWizard,
        VillageForm,
        FrmCurrencyType,
        FrmAddSavingProduct,
        FrmAddSavingEvent,
        StandardBooking,
        AccountingRule,
        FrmExportSage,
        FrmRoles,
        CustomizableExport,
        ShowPictureForm,
        LoanSharesForm,
        FrmOpenCloseTeller,
        CreditScoringForm,
        ClientControl,
        MyInforamtionForm
    }

	/// <summary>
	/// Summary description for RessourcesManager.
	/// </summary>
	public static class  MultiLanguageStrings
	{
        public static string STRING_MESSAGES_RES_NAME = "stringRes";
        public static string ERROR_MESSAGES_RES_NAME = "errorRes";

        static Hashtable _ressourceManagers = new Hashtable();

        public static string GetString(Ressource pRessourceName, string pName)
        {
            ResourceManager rm = (ResourceManager) _ressourceManagers[pRessourceName];
            if (rm == null)
            {
                rm = new ResourceManager("OpenCBS.MultiLanguageRessources.resx." + pRessourceName, typeof(MultiLanguageStrings).Assembly);
                _ressourceManagers.Add(pRessourceName, rm);
            }
            return rm.GetString(pName);
        }

        public static string GetString(string res, string key)
        {
            ResourceManager rm = (ResourceManager)_ressourceManagers[res];
            if (rm == null)
            {
                rm = new ResourceManager("OpenCBS.MultiLanguageRessources.resx." + res, typeof(MultiLanguageStrings).Assembly);
                _ressourceManagers.Add(res, rm);
            }
            return rm.GetString(key);
        }

        public static void Reset()
        {
            _ressourceManagers.Clear();
        }
	}
}
