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
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Reflection;
using System.Windows.Forms;
using BrightIdeasSoftware;
using OpenCBS.CoreDomain;
using OpenCBS.CoreDomain.Alerts;
using OpenCBS.CoreDomain.Clients;
using OpenCBS.CoreDomain.Contracts.Loans;
using OpenCBS.Extensions;
using OpenCBS.Enums;
using OpenCBS.ExceptionsHandler;
using OpenCBS.GUI.Accounting;
using OpenCBS.GUI.AuditTrail;
using OpenCBS.GUI.Clients;
using OpenCBS.GUI.Configuration;
using OpenCBS.GUI.Database;
using OpenCBS.GUI.Products;
using OpenCBS.GUI.Projets;
using OpenCBS.GUI.Report_Browser;
using OpenCBS.GUI.Tools;
using OpenCBS.GUI.Contracts;
using OpenCBS.GUI.UserControl;
using OpenCBS.Reports;
using OpenCBS.Reports.Forms;
using OpenCBS.Services;
using OpenCBS.Shared;
using OpenCBS.MultiLanguageRessources;
using OpenCBS.Shared.Settings;
using Re = System.Text.RegularExpressions;
using OpenCBS.GUI.TellerManagement;

namespace OpenCBS.GUI
{
    public partial class LotrasmicMainWindowForm : SweetBaseForm
    {
        private delegate void LoadAlertsDelegate(List<Alert_v2> alerts);
        private delegate void AttachExtensionDelegate(IExtension extension);
        private List<MenuObject> _menuItems;
        private bool _showTellerFormOnClose = true;
        private bool _triggerAlertsUpdate;
        private DashboardForm pFCF; // it`s pointer for have access to FastChoiceForm component 
        private ReportLoadingProgressForm _reportLdProgressForm;

        public LotrasmicMainWindowForm()
        {
            InitializeComponent();
            _menuItems = new List<MenuObject>();
            _menuItems = Services.GetMenuItemServices().GetMenuList(OSecurityObjectTypes.MenuItem);
            LoadReports();
            LoadReprotsToolStrip();
            InitializeTracer();
            DisplayWinFormDetails();
        }

        private void InitializeTracer()
        {
            string folder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            Trace.AutoFlush = true;
            Trace.WriteLine("Application has started");
        }

        private void DisplayWinFormDetails()
        {           
            _DisplayDetails();
            InitializeContractCurrencies();
            InitAlerts();
        }

        private bool InitializeTellerManagement()
        {
            if (ServicesProvider.GetInstance().GetGeneralSettings().UseTellerManagement)
            {
                if (User.CurrentUser.UserRole.IsRoleForTeller)
                {
                    FrmOpenCloseTeller frm = new FrmOpenCloseTeller(true);
                    frm.ShowDialog();

                    if (frm.DialogResult == DialogResult.OK)
                    {
                        if (frm.Teller != null && frm.Teller.Id != 0)
                        {
                            Teller.CurrentTeller = frm.Teller;
                            //tellerManagementToolStripMenuItem.Visible = true;
                            ServicesProvider.GetInstance().GetEventProcessorServices().LogUser(OUserEvents.UserOpenTellerEvent,
                                Teller.CurrentTeller.Name + " opened", User.CurrentUser.Id);
                            ServicesProvider.GetInstance().GetEventProcessorServices().FireTellerEvent(frm.OpenOfDayAmountEvent);

                            if (frm.OpenAmountPositiveDifferenceEvent != null)
                                ServicesProvider.GetInstance().GetEventProcessorServices().FireTellerEvent(
                                    frm.OpenAmountPositiveDifferenceEvent);
                            else if (frm.OpenAmountNegativeDifferenceEvent != null)
                                ServicesProvider.GetInstance().GetEventProcessorServices().FireTellerEvent(
                                    frm.OpenAmountNegativeDifferenceEvent);
                            
                        }

                        return true;
                    }
                        return false;
                }
            }
            return true;
        }

        private void InitializeContractCurrencies()
        {
            mnuChartOfAccounts.Click += mnuChartOfAccounts_Click;
        }

        private void _DisplayDetails()
        {
            mainStatusBarLblUserName.Text = String.Format("{0} ({1})", User.CurrentUser.FirstName, User.CurrentUser.UserRole);
            toolStripStatusLblDB.Text = !TechnicalSettings.UseOnlineMode ?
                String.Format(" {0}", TechnicalSettings.DatabaseName) :
                "Online";
             
            //mainStatusBarLblUserName.ForeColor = Color.Red;

            toolBarLblVersion.Text = String.Format("OpenCBS {0}", TechnicalSettings.SoftwareVersion);
            if (TechnicalSettings.UseOnlineMode)
                menuItemDatabaseControlPanel.Visible = false;
        }

        private void _InitializeUserRights()
        {
            foreach (ToolStripMenuItem mi in MainMenuStrip.Items)
            {
                Role role = User.CurrentUser.UserRole;
                MenuObject foundMo = GetMenuObject(mi.Name);

                if (foundMo != null)
                {
                    mi.Enabled = role.IsMenuAllowed(foundMo);
                    mi.Tag = foundMo;
                    InitializeMenuChildren(mi, role);
                }
            }
        }

        private void InitializeMenuChildren(ToolStripMenuItem pMenuItem, Role pRole)
        {
            if (!pMenuItem.HasDropDownItems)
            {
                return;
            }
            foreach (Object tsmi in pMenuItem.DropDownItems)
            {
                if (! (tsmi is ToolStripMenuItem))
                    continue;

                ToolStripMenuItem tsmiMenu = (ToolStripMenuItem)tsmi;
                
                MenuObject foundMO = GetMenuObject(tsmiMenu.Name);
                bool isAllowed = foundMO == null || pRole.IsMenuAllowed(foundMO);
                tsmiMenu.Enabled = isAllowed;

                if (!(tsmiMenu.Tag is IExtension))
                {
                    tsmiMenu.Tag = foundMO;
                }
                
                InitializeMenuChildren(tsmiMenu, pRole);
            }
        }

        private MenuObject GetMenuObject(string pText)
        {
            MenuObject foundObject = _menuItems.Find(item => item == pText.Trim());
            return foundObject;
        }

        private void _CheckMenu(string mnuName, bool mnuAct)
        {
        }

        private void SetActiveMenuItem(ToolStripMenuItem tsmi_menu)
        {
            if (!tsmi_menu.HasDropDownItems)
            {
                return;
            }
            foreach (Object mnu in tsmi_menu.DropDownItems)
            {
                if (mnu is ToolStripMenuItem)
                {
                    _CheckMenu(((ToolStripMenuItem)mnu).Name, ((ToolStripMenuItem)mnu).Enabled);
                    SetActiveMenuItem((ToolStripMenuItem)mnu);
                }
            }
        }


        private void DisplayFastChoiceForm()
        {
            DashboardForm fastChoiceForm = new DashboardForm {MdiParent = this};
            fastChoiceForm.Show();
            pFCF = fastChoiceForm;
                       
            foreach (Object tsmi in MainMenuStrip.Items)
            {
                if (!(tsmi is ToolStripMenuItem))
                    continue;
                ToolStripMenuItem tsmi_menu = (ToolStripMenuItem)tsmi;
                SetActiveMenuItem(tsmi_menu);
            }

        }

        public void InitializePersonForm()
        {
            ClientForm personForm = new ClientForm(OClientTypes.Person, this, false) {MdiParent = this};
            personForm.Show();
        }

        public void InitializeCorporateForm()
        {
            ClientForm corporateForm = new ClientForm(OClientTypes.Corporate,this,false) {MdiParent = this};
            corporateForm.Show();
        }
        public void InitializeCorporateForm(Corporate corporate, Project project)
        {
            ClientForm corporateForm = new ClientForm(corporate, this) {MdiParent = this};
            if (project != null)
                corporateForm.DisplayUserControl_ViewProject(project, null);

            corporateForm.Show();
        }

        public void InitializePersonForm(Person person, Project project)
        {
            ClientForm personForm = new ClientForm(person, this)
                                        {
                                            MdiParent = this,
                                            Text = string.Format("{0} [{1}]", MultiLanguageStrings.GetString(Ressource.ClientForm, "Person.Text"), person.Name)
                                        };
            if (project != null)
                personForm.DisplayUserControl_ViewProject(project, null);
            personForm.Show();
        }

        public void InitializeGroupForm()
        {
            ClientForm personForm = new ClientForm(OClientTypes.Group, this, false) {MdiParent = this};
            personForm.Show();
        }

        public void InitializeVillageForm()
        {
            NonSolidaryGroupForm frm = new NonSolidaryGroupForm() {MdiParent = this};
            frm.Show();
        }

        public void InitializeVillageForm(Village village)
        {
            NonSolidaryGroupForm frm = new NonSolidaryGroupForm(village) {MdiParent = this};
            frm.Show();
        }

        public void InitializeGroupForm(Group group, Project project)
        {
            ClientForm personForm = new ClientForm(group, this)
                                        {
                                            MdiParent = this,
                                            Text = string.Format("{0} [{1}]", MultiLanguageStrings.GetString(Ressource.ClientForm, "Group.Text"), group.Name)
                                        };
            if (project != null)
                    personForm.DisplayUserControl_ViewProject(project, null);
                personForm.Show();
        }

        public void InitializeSearchClientForm()
        {
            SearchClientForm searchClientForm = SearchClientForm.GetInstance(this);
            searchClientForm.BringToFront();
            searchClientForm.WindowState = FormWindowState.Normal;
            searchClientForm.Show();
        }

        public void InitializeSearchCreditContractForm()
        {
            SearchCreditContractForm searchCreditContractForm = SearchCreditContractForm.GetInstance(this);
            searchCreditContractForm.BringToFront();
            searchCreditContractForm.WindowState = FormWindowState.Normal;
            searchCreditContractForm.Show();
        }

        private void InitializeSearchProject()
        {
            SearchProjectForm searchProjectForm = SearchProjectForm.GetInstance(this);
            searchProjectForm.BringToFront();
            searchProjectForm.WindowState = FormWindowState.Normal;
            searchProjectForm.Show();
        }

        public void InitializeCreditContractForm(IClient pClient, int pContractId)
        {
            /*
             * This code is for loading compulsory savings. Compulsory savings are being 
             * loaded here because in LoanManager class SavingsManager trigers problems.
             * Ruslan Kazakov
             */
            
            if (pClient.Projects != null)
                foreach (Project project in pClient.Projects)
                    if (project.Credits != null)
                        foreach (Loan loan in project.Credits)
                            loan.CompulsorySavings = ServicesProvider.GetInstance().GetSavingServices().GetSavingForLoan(loan.Id, true);
            ClientForm personForm = new ClientForm(pClient, pContractId, this) {MdiParent = this};
            personForm.Show();
        }

        public void InitializeSavingContractForm(IClient client, int savingId)
        {
            switch (client.Type)
            {
                case OClientTypes.Person:
                    {
                        var personForm = new ClientForm((Person)client, this)
                        {
                            MdiParent = this,
                            Text = string.Format("{0} [{1}]", MultiLanguageStrings.GetString(
                            Ressource.ClientForm, "Person.Text"), 
                            ((Person)client).Name)
                        };
                        personForm.DisplaySaving(savingId, client);
                        personForm.Show();
                        break;
                    }
                case OClientTypes.Group:
                    {
                        var personForm = new ClientForm((Group)client, this)
                        {
                            MdiParent = this,
                            Text = string.Format("{0} [{1}]", MultiLanguageStrings.GetString(Ressource.ClientForm, "Group.Text"), ((Group)client).Name)
                        };
                        personForm.DisplaySaving(savingId, client);
                        personForm.Show();
                        break;
                    }
                case OClientTypes.Village:
                    {
                        var frm = new NonSolidaryGroupForm((Village)client) { MdiParent = this };
                        frm.Show();
                        break;
                    }
                case OClientTypes.Corporate:
                    {
                        var corporateForm = new ClientForm((Corporate)client, this) { MdiParent = this };
                        corporateForm.DisplaySaving(savingId, client);
                        corporateForm.Show();
                        break;
                    }
            }
        }

        public void InitializeChartOfAccountsForm(int pCurrencyId)
        {
            var chartOfAccountsForm = new ChartOfAccountsForm(pCurrencyId) { MdiParent = this };
            chartOfAccountsForm.Show();
        }

        private void InitializeCollateralProductsForm()
        {
            var collateralProductsForm = new FrmAvalaibleCollateralProducts { MdiParent = this };
            collateralProductsForm.Show();
        }

        private void InitializePackagesForm()
        {
            var packagesForm = new FrmAvalaibleLoanProducts { MdiParent = this };
            packagesForm.Show();
        }

        private void InitializeSavingProductsForm()
        {
            var frmSavingProductsForm = new FrmAvailableSavingProducts { MdiParent = this };
            frmSavingProductsForm.Show();
        }

        private void InitializeReassingContractsForm()
        {
            var reassingForm = new ReassignContractsForm { MdiParent = this };
            reassingForm.Show();
        }

        private static void InitializeDomainOfApplicationForm()
        {
            var doaf = new FrmEconomicActivity();
            //doaf.Show();
            doaf.ShowDialog();
        }

        public void SetInfoMessage(string pMessage)
        {
        }

        private void mnuNewPerson_Click(object sender, EventArgs e)
        {
            InitializePersonForm();
        }

        private void mnuNewGroup_Click(object sender, EventArgs e)
        {
            InitializeGroupForm();
        }

        private void mnuSearchClient_Click(object sender, EventArgs e)
        {
            InitializeSearchClientForm();
        }

        private void mnuChartOfAccounts_Click(object sender, EventArgs e)
        {
            InitializeChartOfAccountsForm(ServicesProvider.GetInstance().GetCurrencyServices().GetPivot().Id);
        }

        private void menuItemPackages_Click(object sender, EventArgs e)
        {
            InitializePackagesForm();
        }

        private void savingProductsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            InitializeSavingProductsForm();
        }

        private void mnuDomainOfApplication_Click(object sender, EventArgs e)
        {
            InitializeDomainOfApplicationForm();
        }

        private void menuItemExportTransaction_Click(object sender, EventArgs e)
        {
            Form exportTransactions = new ExportBookingsForm { MdiParent = this };
            
            exportTransactions.Show();
        }
        private void menuItemExchangeRate_Click(object sender, System.EventArgs e)
        {
            ExchangeRateForm exchangeRate = new ExchangeRateForm();
            exchangeRate.Show();
        }

        private void mnuSearchContract_Click(object sender, System.EventArgs e)
        {
            InitializeSearchCreditContractForm();
        }

        private void menuItemAddUser_Click(object sender, System.EventArgs e)
        {
            UserForm userForm = new UserForm {MdiParent = this};
            userForm.Show();
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            mainStatusBarLblDate.ForeColor = TimeProvider.IsUsingSystemTime ? Color.Black : Color.Red;
            mainStatusBarLblDate.Text = String.Format("{0} {1}", TimeProvider.Today.ToString("dd/MM/yyyy"),
                                                      TimeProvider.Now.ToLongTimeString());
        }

        private void menuItemSetting_Click(object sender, EventArgs e)
        {
            FrmGeneralSettings generalSettings = new FrmGeneralSettings();
            generalSettings.ShowDialog();
        }

        private void OnAboutMenuItemClick(object sender, EventArgs e)
        {
            AboutForm aboutForm = new AboutForm();
            aboutForm.ShowDialog();
        }

        private void OnChangeApplicationDateClick(object sender, EventArgs e)
        {
            ApplicationDate frm = new ApplicationDate
            {
                Today = TimeProvider.Today
            };

            if (frm.ShowDialog() != DialogResult.OK) return;
            if (TimeProvider.Today == frm.Today) return;

            TimeProvider.SetToday(frm.Today);
            ReloadAlerts();
        }

        private void menuItemAdvancedSettings_Click(object sender, System.EventArgs e)
        {
            Form form = new FrmUserSettings();
            form.ShowDialog();
            if (form.DialogResult == DialogResult.OK)
            {
                MessageBox.Show(MultiLanguageStrings.GetString(Ressource.LotrasmicMainWindowForm, "advancedSettingsChanged.Text"));
                Restart.LaunchRestarter();
            }
        }

        private void menuItemBackupData_Click(object sender, EventArgs e)
        {
            FrmDatabaseSettings frmDatabaseSettings = new FrmDatabaseSettings(FrmDatabaseSettingsEnum.SqlServerSettings, false, true);
            frmDatabaseSettings.ShowDialog();
        }

        private void menuItemDatabaseMaintenance_Click(object sender, EventArgs e)
        {
            Form form = new frmDatabaseMaintenance();
            form.ShowDialog();
        }

        private void FillDropDownMenuWithLanguages()
        {
            string currentLanguage = UserSettings.Language;

            frenchToolStripMenuItem.Checked = (currentLanguage == "fr");
            russianToolStripMenuItem.Checked = (currentLanguage == "ru-RU");
            englishToolStripMenuItem.Checked = (currentLanguage == "en-US");
            spanishToolStripMenuItem.Checked = (currentLanguage == "es-ES");
        }

        private void _InitializeStandardBookings()
        {
            StandardBooking standardBooking = new StandardBooking {MdiParent = this};
            standardBooking.Show();
        }

        private void toolStripMenuItemAccountView_Click(object sender, EventArgs e)
        {
            AccountView accountView = new AccountView {MdiParent = this};
            accountView.Show();
        }

        private void menuItemLocations_Click(object sender, EventArgs e)
        {
            Form frm = new FrmLocations();
            frm.ShowDialog();
        }

        private void toolStripMenuItemFundingLines_Click(object sender, EventArgs e)
        {
            Form frm = new FrmFundingLine {MdiParent = this};
            frm.Show();
        }

        private void RestartApplication(string language)
        {
            UserSettings.SetUserLanguage(language);
            UserSettings.Language = language;
            ServicesProvider.GetInstance().GetEventProcessorServices().LogUser(
                OUserEvents.UserCloseTellerEvent,
                OUserEvents.UserCloseTellerDescription,
                User.CurrentUser.Id);

            MessageBox.Show(MultiLanguageStrings.GetString(Ressource.LotrasmicMainWindowForm, "advancedSettingsChanged.Text"));
            Restart.LaunchRestarter();
        }

        private void LanguageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string language = sender == frenchToolStripMenuItem ? "fr" :
                    (sender == russianToolStripMenuItem ? "ru-RU" :
                    (sender == englishToolStripMenuItem ? "en-US" :
                    (sender == spanishToolStripMenuItem ? "es-ES" : "pt")));

            if (ServicesProvider.GetInstance().GetGeneralSettings().UseTellerManagement)
            {
                if (User.CurrentUser.UserRole.IsRoleForTeller)
                {
                    if (Teller.CurrentTeller != null && Teller.CurrentTeller.Id != 0)
                    {
                        FrmOpenCloseTeller frm = new FrmOpenCloseTeller(false);
                        frm.ShowDialog();

                        if (frm.DialogResult == DialogResult.OK)
                        {
                            _showTellerFormOnClose = false;
                            Teller.CurrentTeller = null;
                            ServicesProvider.GetInstance().GetEventProcessorServices().FireTellerEvent(
                                                                                    frm.CloseOfDayAmountEvent);
                            if (frm.CloseAmountNegativeDifferenceEvent != null)
                                ServicesProvider.GetInstance().GetEventProcessorServices().FireTellerEvent(
                                    frm.CloseAmountNegativeDifferenceEvent);
                            else if (frm.CloseAmountPositiveDifferenceEvent != null)
                                ServicesProvider.GetInstance().GetEventProcessorServices().FireTellerEvent(
                                    frm.CloseAmountPositiveDifferenceEvent);
                            RestartApplication(language);
                        }
                    }
                }
                else RestartApplication(language);
            }
            else RestartApplication(language);
        }

        private void languagesToolStripMenuItem_DropDownOpening(object sender, EventArgs e)
        {
            FillDropDownMenuWithLanguages();
        }

        private void toolStripMenuItemInstallmentTypes_Click(object sender, EventArgs e)
        {
            FrmInstallmentTypes frmInstallmentTypes = new FrmInstallmentTypes();
            frmInstallmentTypes.ShowDialog();
        }

        private void reasignToolStripMenuItem_Click(object sender, EventArgs e)
        {
            InitializeReassingContractsForm();
        }

        private void newCorporateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            InitializeCorporateForm();
        }

        public void InitializePersonForm(Person member)
        {
            throw new NotImplementedException();
        }

        private void mnuNewVillage_Click(object sender, EventArgs e)
        {
            InitializeVillageForm();
        }

        private void miContractCode_Click(object sender, EventArgs e)
        {
            ContractCodeForm frm = new ContractCodeForm();
            if (DialogResult.OK == frm.ShowDialog())
            {
                ServicesProvider.GetInstance().GetGeneralSettings().UpdateParameter(OGeneralSettings.CONTRACT_CODE_TEMPLATE, frm.code);
                ServicesProvider.GetInstance().GetApplicationSettingsServices().UpdateSelectedParameter(OGeneralSettings.CONTRACT_CODE_TEMPLATE, frm.code);
            }
        }

        #region Alerts
        private void InitAlerts()
        {
            colAlerts_Status.AspectToStringConverter = delegate(object value)
            {
                OContractStatus status = (OContractStatus) value;
                string key = string.Format("Status{0}", status);
                return GetString(key);
            };

            colAlerts_Date.AspectToStringConverter = delegate(object value)
            {
                DateTime date = (DateTime)value;
                return date.ToShortDateString();
            };

            colAlerts_Amount.AspectToStringConverter = delegate(object value)
            {
                OCurrency amount = (OCurrency) value;
                return amount.GetFormatedValue(true);
            };

            colAlerts_ContractCode.ImageGetter = delegate(object value)
            {
                Alert_v2 alert = (Alert_v2) value;
                return alert.ImageIndex;
            };

            byte[] state = UserSettings.GetAlertState();
            if (state != null)
                olvAlerts.RestoreState(state);

            _triggerAlertsUpdate = false;
            chkLateLoans.Checked = UserSettings.GetShowLateLoans();
            chkPendingLoans.Checked = UserSettings.GetShowPendingLoans();
            chkPendingSavings.Checked = UserSettings.GetShowPendingSavings();
            chkOverdraftSavings.Checked = UserSettings.GetShowOverdraftSavings();
            chkValidatedLoan.Checked = UserSettings.GetValidatedLoans();
            chkPostponedLoans.Checked = UserSettings.GetPostponedLoans();
            _triggerAlertsUpdate = true;
        }

        private void UpdateAlertsTitle()
        {
            string t = GetString("Alerts");
            lblTitle.Text = string.Format(t, olvAlerts.Items.Count);
        }

        public void ReloadAlertsSync()
        {
            if (!UserSettings.GetLoadAlerts()) return;

            LoanServices ls = ServicesProvider.GetInstance().GetContractServices();
            ls.ClearAlerts();
            olvAlerts.SetObjects(null);
            lblTitle.Text = GetString("AlertsLoading");
            tabFilter.Enabled = false;

            List<Alert_v2> alerts = ls.FindAlerts(chkLateLoans.Checked, chkPendingLoans.Checked,
                                                  chkPostponedLoans.Checked, chkOverdraftSavings.Checked, chkPendingSavings.Checked, chkValidatedLoan.Checked);
            LoadAlerts(alerts);
            tabFilter.Enabled = true;
        }

        public void ReloadAlerts()
        {
            ReloadAlerts(true);
        }

        public void ReloadAlerts(bool clear)
        {
            if (!UserSettings.GetLoadAlerts()) return;

            LoanServices ls = ServicesProvider.GetInstance().GetContractServices();
            if (clear)
                ls.ClearAlerts();
            olvAlerts.SetObjects(null);
            lblTitle.Text = GetString("AlertsLoading");
            tabFilter.Enabled = false;
            bwAlerts.RunWorkerAsync();
        }

        private void LoadAlerts(List<Alert_v2> alerts)
        {
            if (InvokeRequired)
            {
                Invoke(new LoadAlertsDelegate(LoadAlerts), new object[] {alerts});
                return;
            }

            olvAlerts.SetObjects(alerts);
        }

        private void OnFilterChanged(object sender, EventArgs e)
        {
            string filter = tbFilter.Text.ToLower();
            if (string.IsNullOrEmpty(filter))
            {
                olvAlerts.UseFiltering = false;
                UpdateAlertsTitle();
                return;
            }

            olvAlerts.UseFiltering = true;
            olvAlerts.ModelFilter = new ModelFilter(delegate(object x)
            {
                Alert_v2 alert = (Alert_v2)x;
                return alert.ContractCode.ToLower().Contains(filter)
                       || alert.ClientName.ToLower().Contains(filter)
                       || alert.LoanOfficer.Name.ToLower().Contains(filter);
            });
            UpdateAlertsTitle();
        }

        private void OnFormatAlertRow(object sender, FormatRowEventArgs e)
        {
            Alert_v2 alert = (Alert_v2) e.Model;
            e.Item.BackColor = alert.BackColor;
        }

        private void OnAlertItemsChanged(object sender, ItemsChangedEventArgs e)
        {
            UpdateAlertsTitle();
        }

        private void OnAlertDoubleClicked(object sender, EventArgs e)
        {
            if (null == olvAlerts.SelectedObject) return;

            Alert_v2 alert = (Alert_v2) olvAlerts.SelectedObject;

            IClient client;
            switch (alert.Kind)
            {
                case AlertKind.Loan:
                    client = ServicesProvider.GetInstance().GetClientServices().FindTiersByContractId(alert.Id);
                    InitializeCreditContractForm(client, alert.Id);
                    break;

                case AlertKind.Saving:
                    client = ServicesProvider.GetInstance().GetClientServices().FindTiersBySavingsId(alert.Id);
                    InitializeSavingContractForm(client, alert.Id);
                    break;

                default:
                    Debug.Fail("Cannot be here.");
                    break;
            }
        }

        private void OnAlertsLoading(object sender, DoWorkEventArgs e)
        {
            LoanServices ls = ServicesProvider.GetInstance().GetContractServices();
            List<Alert_v2> alerts = ls.FindAlerts(chkLateLoans.Checked, chkPendingLoans.Checked,
                                                  chkPostponedLoans.Checked, chkOverdraftSavings.Checked,
                                                  chkPendingSavings.Checked, chkValidatedLoan.Checked);
            LoadAlerts(alerts);
        }


        private void OnAlertsLoaded(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                Debug.WriteLine(e.Error.Message);
            }
            tabFilter.Enabled = true;
        }


        private void OnAlertCheckChanged(object sender, EventArgs e)
        {
            if (!_triggerAlertsUpdate) return;
            UserSettings.SetShowLateLoans(chkLateLoans.Checked);
            UserSettings.SetShowPendingLoans(chkPendingLoans.Checked);
            UserSettings.SetShowPendingSavings(chkPendingSavings.Checked);
            UserSettings.SetShowOverdraftSavings(chkOverdraftSavings.Checked);
            UserSettings.SetShowValidatedLoans(chkValidatedLoan.Checked);
            UserSettings.SetShowPostponedLoans(chkPostponedLoans.Checked);
            ReloadAlerts(false);
        }


        private void OnAlertsVisibleChanged(object sender, EventArgs e)
        {
            UserSettings.SetLoadAlerts(panelLeft.Visible);
            ReloadAlerts();
        }

        private void OnAlertsSizeChanged(object sender, EventArgs e)
        {
            UserSettings.SetAlertsWidth(panelLeft.Width);
        }
        #endregion Alerts

        private void LotrasmicMainWindowForm_Load(object sender, EventArgs e)
        {
            UserSettings.Language = UserSettings.GetUserLanguage();
            if (InitializeTellerManagement())
            {
                LogUser();
                _LoadExtensions();                
                panelLeft.Visible = UserSettings.GetLoadAlerts();
                panelLeft.Width = UserSettings.GetAlertsWidth();
                if (panelLeft.Visible) ReloadAlerts();
                panelLeft.VisibleChanged += OnAlertsVisibleChanged;
                panelLeft.SizeChanged += OnAlertsSizeChanged;
            }
            else
            {
                Environment.Exit(0);
            }
        }

        private void LoadReprotsToolStrip()
        {
            ToolStripMenuItem i;
            IComparer<Report> comparer = new ReportAbcComparer();
            List<Report> re = new List<Report>(ReportService.GetInstance().GetReportsByTag("Main",true,Flag.Standard));
            re.Sort(comparer);
            foreach (Report report in re)
            {
                i = new ToolStripMenuItem(report.Title) {Tag = report.Name};
                i.Click += new System.EventHandler(this.activeLoansToolStripMenuItem_Click);
                reportsToolStripMenuItem.DropDownItems.Add(i);
            }

            i = reportsToolStripMenuItem.DropDownItems[0] as ToolStripMenuItem;
            reportsToolStripMenuItem.DropDownItems.RemoveAt(0);
            re = new List<Report>(ReportService.GetInstance().GetReportsByTag("Main", false, Flag.Standard));
            re.Sort(comparer);
            foreach (Report report in re)
            {
                ToolStripMenuItem jItem = new ToolStripMenuItem(report.Title) {Tag = report.Name};
                jItem.Click += new System.EventHandler(this.activeLoansToolStripMenuItem_Click);
                i.DropDownItems.Add(jItem);
            }
            reportsToolStripMenuItem.DropDownItems.Add(i);
        }

        private void LogUser()
        {
            ServicesProvider.GetInstance().GetEventProcessorServices().LogUser(OUserEvents.UserLogInEvent,
                OUserEvents.UserLoginDescription, User.CurrentUser.Id);
        }

        private static void LoadReports()
        {
           bwReportLoader_DoWork(null, null);
        }

        private void _LoadExtensions()
        {
            BackgroundWorker bwExtensionLoader = new BackgroundWorker();
            bwExtensionLoader.DoWork += LoadExtensions;
            bwExtensionLoader.RunWorkerCompleted += OnExtensionsLoaded;
            Trace.WriteLine("Started analyzing extensions");
            bwExtensionLoader.RunWorkerAsync();
        }

        private void OnExtensionsLoaded(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                ExceptionStatus exceptionStatus = CustomExceptionHandler.ShowExceptionText(e.Error, "LoadExtensionError.Text");
                new frmShowError(exceptionStatus).ShowDialog();
            }
            _InitializeUserRights();
            DisplayFastChoiceForm();
        }

        private void AddExtensionToMenu(IExtension extension)
        {
            if (null == extension) return;

            try
            {
                if (InvokeRequired)
                {
                    Invoke(new AttachExtensionDelegate(AddExtensionToMenu), new object[] {extension});
                    return;
                }

                IMenu menu = extension.QueryInterface(typeof(IMenu)) as IMenu;
                if (null == menu) return;

                foreach (ExtensionMenuItem extensionItem in menu.GetItems())
                {
                    var items = mainMenu.Items.Find(extensionItem.InsertAfter, true);
                    if (0 == items.Length) return;

                    ToolStripMenuItem item = (ToolStripMenuItem)items[0];
                    ToolStripMenuItem ownerItem = (ToolStripMenuItem)item.OwnerItem;
  
                    if (null == ownerItem)
                    {
                        int index = mainMenu.Items.IndexOf(item);
                        mainMenu.Items.Insert(index + 1, extensionItem.MenuItem);
                    }
                    else
                    {
                        int index = ownerItem.DropDownItems.IndexOf(item);
                        ownerItem.DropDownItems.Insert(index + 1, extensionItem.MenuItem);
                    }
                }
            }
            catch(Exception ex)
            {
                new frmShowError(CustomExceptionHandler.ShowExceptionText(ex)).ShowDialog();
            }
        }

        private static void bwReportLoader_DoWork(object sender, DoWorkEventArgs e)
        {
            ReportService rs = ReportService.GetInstance();
            rs.LoadReports();
        }

        private void LoadExtensions(object sender, DoWorkEventArgs e)
        {
            
            foreach (IExtension ext in Extension.Instance.Extensions)
            {
                AddExtensionToMenu(ext);
            }
        }

        private void standardToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _InitializeStandardBookings();
        }

        private void nIUpdateAvailable_BalloonTipClicked(object sender, EventArgs e)
        {
            string url = nIUpdateAvailable.Tag.ToString();
            try
            {
                Process.Start("firefox.exe", url);
            }
            catch
            {
                try
                {
                    Process.Start("chrome.exe", url);
                }
                catch
                {
                    try
                    {
                        Process.Start("iexplore.exe", url);
                    }
                    catch
                    {
                    }
                }
            }
            nIUpdateAvailable.Visible = false;
        }

        private void currenciesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FrmCurrencyType _frmCurrency = new FrmCurrencyType();
            _frmCurrency.Show();
            InitializeContractCurrencies();
        }

        private void eventsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AuditTrailForm trailForm = new AuditTrailForm { MdiParent = this };
            trailForm.Show();
        }

        private void LotrasmicMainWindowForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (ServicesProvider.GetInstance().GetGeneralSettings().UseTellerManagement)
            {
                if (User.CurrentUser.UserRole.IsRoleForTeller)
                {
                    if (_showTellerFormOnClose)
                    {
                        e.Cancel = false;

                        if (Teller.CurrentTeller != null && Teller.CurrentTeller.Id != 0) 
                            if (!CloseTeller()) 
                                e.Cancel = true;
                    }
                }
            }
            UserSettings.SetAlertState(olvAlerts.SaveState());
            try
            {
                ServicesProvider.GetInstance().GetEventProcessorServices().LogUser(OUserEvents.UserLogOutEvent, 
                    OUserEvents.UserLogoutDescription, User.CurrentUser.Id);
            }
            catch {}
        }

        private void accountingRulesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FrmAccountingRules frmAccountingRules = new FrmAccountingRules { MdiParent = this };
            frmAccountingRules.Show();
        }

        private void rolesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FrmRoles rolesForm = new FrmRoles(this) { MdiParent = this };
            rolesForm.Show();
        }

        private void menuItemCollateralProducts_Click(object sender, EventArgs e)
        {
            InitializeCollateralProductsForm();
        }

        private void trialBalanceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AccountTrialBalance accountTrialBalance = new AccountTrialBalance { MdiParent = this };
            accountTrialBalance.Show();
        }

        private void changePasswordToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PasswordForm pswdForm = new PasswordForm(User.CurrentUser);
            if (DialogResult.OK != pswdForm.ShowDialog()) return;
            User.CurrentUser.Password = pswdForm.NewPassword;
            ServicesProvider.GetInstance().GetUserServices().SaveUser(User.CurrentUser);
            Notify("passwordChanged");
        }

        private void manualEntriesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ManualEntries accountView = new ManualEntries { MdiParent = this };
            accountView.Show();
        }

        private void branchesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            BranchesForm frm = new BranchesForm {MdiParent = this};
            frm.Show();
        }

        private void closeTellerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!CloseTeller())
                MessageBox.Show(MultiLanguageStrings.GetString(Ressource.FrmOpenCloseTeller, "noOpenTellersText"));
        }

        private bool CloseTeller()
        {
            if (Teller.CurrentTeller != null)
            {
                FrmOpenCloseTeller frm = new FrmOpenCloseTeller(false);
                frm.ShowDialog();
                if (frm.DialogResult == DialogResult.OK)
                {
                    string desc = Teller.CurrentTeller.Name + " closed";
                    Teller.CurrentTeller = null;
                    ServicesProvider.GetInstance().GetEventProcessorServices().LogUser(
                                                                        OUserEvents.UserCloseTellerEvent, 
                                                                        desc, 
                                                                        User.CurrentUser.Id);
                    ServicesProvider.GetInstance().GetEventProcessorServices().FireTellerEvent(frm.CloseOfDayAmountEvent);
                    if (frm.CloseAmountNegativeDifferenceEvent != null)
                        ServicesProvider.GetInstance().GetEventProcessorServices().FireTellerEvent(
                            frm.CloseAmountNegativeDifferenceEvent);
                    else if (frm.CloseAmountPositiveDifferenceEvent != null)
                        ServicesProvider.GetInstance().GetEventProcessorServices().FireTellerEvent(
                            frm.CloseAmountPositiveDifferenceEvent);
                    return true;
                }
            }
            
            return false;
        }

        private void CustomizableFieldsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CustomizableFieldsForm frm = new CustomizableFieldsForm { MdiParent = this };
            frm.Show();
        }

        private void newClosureToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            AccountingClosureForm frm = new AccountingClosureForm { MdiParent = this };
            frm.Show();
        }

        private void fiscalYearToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FiscalYear fiscalYear = new FiscalYear() { MdiParent = this };
            fiscalYear.Show();
        }

        private void tellersToolStripMenuItem_Click(object sender, EventArgs e)
        {
            TellersForm frm = new TellersForm() {MdiParent = this};
            frm.Show();
        }

        private void activeLoansToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                var reportName = (sender as ToolStripMenuItem).Tag.ToString();
                var report = ReportService.GetInstance().GetReportByName(reportName);
                var reportParamsForm = new ReportParamsForm(report.Params, report.Title);

                if (reportParamsForm.ShowDialog() != DialogResult.OK) return;

                var progressForm = new ReportLoadingProgressForm();
                progressForm.Show();

                var bw = new BackgroundWorker
                {
                    WorkerReportsProgress = true,
                    WorkerSupportsCancellation = true,
                };
                bw.DoWork += (obj, args) =>
                {
                    ReportService.GetInstance().LoadReport(report);
                    bw.ReportProgress(100);
                };
                bw.RunWorkerCompleted += (obj, args) =>
                {
                    progressForm.Close();
                    if (args.Error != null)
                    {
                        Fail(args.Error.Message);
                        return;
                    }
                    if (args.Cancelled) return;

                    report.OpenCount++;
                    report.SaveOpenCount();
                    var reportViewer = new ReportViewerForm(report);
                    reportViewer.Show();
                };
                bw.RunWorkerAsync(report);
            }
            catch (Exception ex)
            {
                Fail(ex.Message);
            }
        }
    }
}
