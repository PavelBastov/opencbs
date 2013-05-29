
using System.Windows.Forms;
using OpenCBS.GUI.UserControl;
using OpenCBS.Enums;
using OpenCBS.Services;
using OpenCBS.CoreDomain;

namespace OpenCBS.GUI
{
    partial class LotrasmicMainWindowForm
    {
        private System.Windows.Forms.ToolStripMenuItem mnuClients;
        private System.Windows.Forms.ToolStripMenuItem mnuContracts;
        private System.Windows.Forms.ToolStripMenuItem mnuAccounting;
        private System.Windows.Forms.ToolStripMenuItem mnuNewClient;
        private System.Windows.Forms.ToolStripMenuItem mnuSearchClient;
        private System.Windows.Forms.ToolStripMenuItem mnuSearchContract;
        private System.Windows.Forms.ToolStripMenuItem mnuChartOfAccounts;
        private System.ComponentModel.IContainer components;
        private System.Windows.Forms.MenuStrip mainMenu;
        private System.Windows.Forms.ToolStripMenuItem mnuWindow;
        private System.Windows.Forms.ToolStripMenuItem mnuNewPerson;
        private System.Windows.Forms.ToolStripMenuItem mnuNewGroup;
        private System.Windows.Forms.ToolStripMenuItem mnuPackages;
        private System.Windows.Forms.ImageList imageListAlert;
        private System.Windows.Forms.ToolStripMenuItem mnuConfiguration;
        private System.Windows.Forms.ToolStripMenuItem mnuDomainOfApplication;
        private System.Windows.Forms.ToolStripMenuItem menuItemExportTransaction;
        private System.Windows.Forms.ToolStripMenuItem menuItemExchangeRate;
        private System.Windows.Forms.Timer timer;
        private System.Windows.Forms.ToolStripMenuItem menuItemSetting;
        private System.Windows.Forms.ToolStripMenuItem mnuHelp;
        private System.Windows.Forms.ToolStripMenuItem menuItemApplicationDate;
        private System.Windows.Forms.ToolStripMenuItem menuItemAdvancedSettings;
        private System.Windows.Forms.ToolStripMenuItem mnuDatamanagement;
        private System.Windows.Forms.ToolStripMenuItem menuItemDatabaseControlPanel;
        private System.Windows.Forms.ToolStripMenuItem menuItemDatabaseMaintenance;
        private ToolStripSeparator toolStripSeparatorConfig1;
        private ToolStripSeparator toolStripSeparatorConfig2;
        private ToolStripSeparator toolStripSeparatorConfig3;
        private StatusStrip mainStatusBar;
        private CollapsibleSplitter splitter6;
        private ToolStripLabel toolBarLblVersion;
        private ToolStripStatusLabel mainStatusBarLblUserName;
        private ToolStripStatusLabel mainStatusBarLblDate;
        private ToolStripStatusLabel mainStatusBarLblUpdateVersion;
        private ToolStripStatusLabel toolStripStatusLblBranchCode;
        private ToolStripMenuItem toolStripMenuItemAccountView;
        private ToolStripSeparator toolStripSeparator2;
        private ToolStripMenuItem menuItemLocations;
        private ToolStripMenuItem toolStripMenuItemFundingLines;
        private ToolStripMenuItem languagesToolStripMenuItem;
        private ToolStripMenuItem frenchToolStripMenuItem;
        private ToolStripMenuItem englishToolStripMenuItem;
        private ToolStripMenuItem russianToolStripMenuItem;
        private ToolStripMenuItem toolStripMenuItemInstallmentTypes;
        private ToolStripSeparator toolStripSeparator3;


        /// <summary>
        /// Nettoyage des ressources utilisées.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (components != null)
                {
                    components.Dispose();
                }
            }
            base.Dispose(disposing);
        }

        #region Code généré par le Concepteur Windows Form
        /// <summary>
        /// Méthode requise pour la prise en charge du concepteur - ne modifiez pas
        /// le contenu de cette méthode avec l'éditeur de code.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(LotrasmicMainWindowForm));
            this.mnuClients = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuNewClient = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuNewPerson = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuNewGroup = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuNewVillage = new System.Windows.Forms.ToolStripMenuItem();
            this.newCorporateToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuSearchClient = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuContracts = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuSearchContract = new System.Windows.Forms.ToolStripMenuItem();
            this.reasignToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuAccounting = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuChartOfAccounts = new System.Windows.Forms.ToolStripMenuItem();
            this.accountingRulesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.trialBalanceToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItemAccountView = new System.Windows.Forms.ToolStripMenuItem();
            this.manualEntriesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.standardToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.menuItemExportTransaction = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuNewclosure = new System.Windows.Forms.ToolStripMenuItem();
            this.fiscalYearToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuConfiguration = new System.Windows.Forms.ToolStripMenuItem();
            this.rolesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.menuItemAddUser = new System.Windows.Forms.ToolStripMenuItem();
            this.tellersToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.branchesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.changePasswordToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.languagesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.frenchToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.englishToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.russianToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.spanishToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.portugueseToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparatorConfig1 = new System.Windows.Forms.ToolStripSeparator();
            this.mnuPackages = new System.Windows.Forms.ToolStripMenuItem();
            this.savingProductsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.menuItemCollateralProducts = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
            this.toolStripMenuItemFundingLines = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparatorConfig2 = new System.Windows.Forms.ToolStripSeparator();
            this.mnuDomainOfApplication = new System.Windows.Forms.ToolStripMenuItem();
            this.menuItemLocations = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItemInstallmentTypes = new System.Windows.Forms.ToolStripMenuItem();
            this.miContractCode = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparatorConfig3 = new System.Windows.Forms.ToolStripSeparator();
            this.menuItemExchangeRate = new System.Windows.Forms.ToolStripMenuItem();
            this.currenciesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripSeparator();
            this.menuItemApplicationDate = new System.Windows.Forms.ToolStripMenuItem();
            this.menuItemSetting = new System.Windows.Forms.ToolStripMenuItem();
            this.menuItemAdvancedSettings = new System.Windows.Forms.ToolStripMenuItem();
            this.CustomizableFieldsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuDatamanagement = new System.Windows.Forms.ToolStripMenuItem();
            this.menuItemDatabaseControlPanel = new System.Windows.Forms.ToolStripMenuItem();
            this.menuItemDatabaseMaintenance = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuWindow = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuHelp = new System.Windows.Forms.ToolStripMenuItem();
            this.aboutMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.imageListAlert = new System.Windows.Forms.ImageList(this.components);
            this.mainMenu = new System.Windows.Forms.MenuStrip();
            this.reportsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.otherToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.mView = new System.Windows.Forms.ToolStripMenuItem();
            this.miAuditTrail = new System.Windows.Forms.ToolStripMenuItem();
            this.timer = new System.Windows.Forms.Timer(this.components);
            this.toolBarLblVersion = new System.Windows.Forms.ToolStripLabel();
            this.mainStatusBar = new System.Windows.Forms.StatusStrip();
            this.mainStatusBarLblUpdateVersion = new System.Windows.Forms.ToolStripStatusLabel();
            this.mainStatusBarLblUserName = new System.Windows.Forms.ToolStripStatusLabel();
            this.mainStatusBarLblDate = new System.Windows.Forms.ToolStripStatusLabel();
            this.toolStripStatusLblBranchCode = new System.Windows.Forms.ToolStripStatusLabel();
            this.toolStripStatusLblDB = new System.Windows.Forms.ToolStripStatusLabel();
            this.bwAlerts = new System.ComponentModel.BackgroundWorker();
            this.nIUpdateAvailable = new System.Windows.Forms.NotifyIcon(this.components);
            this.openCustomizableFileDialog = new System.Windows.Forms.OpenFileDialog();
            this.colAlerts_Address = ((BrightIdeasSoftware.OLVColumn)(new BrightIdeasSoftware.OLVColumn()));
            this.colAlerts_City = ((BrightIdeasSoftware.OLVColumn)(new BrightIdeasSoftware.OLVColumn()));
            this.colAlerts_Phone = ((BrightIdeasSoftware.OLVColumn)(new BrightIdeasSoftware.OLVColumn()));
            this.splitter6 = new OpenCBS.GUI.UserControl.CollapsibleSplitter();
            this.panelLeft = new System.Windows.Forms.Panel();
            this.olvAlerts = new BrightIdeasSoftware.ObjectListView();
            this.colAlerts_ContractCode = ((BrightIdeasSoftware.OLVColumn)(new BrightIdeasSoftware.OLVColumn()));
            this.colAlerts_Status = ((BrightIdeasSoftware.OLVColumn)(new BrightIdeasSoftware.OLVColumn()));
            this.colAlerts_Client = ((BrightIdeasSoftware.OLVColumn)(new BrightIdeasSoftware.OLVColumn()));
            this.colAlerts_LoanOfficer = ((BrightIdeasSoftware.OLVColumn)(new BrightIdeasSoftware.OLVColumn()));
            this.colAlerts_Date = ((BrightIdeasSoftware.OLVColumn)(new BrightIdeasSoftware.OLVColumn()));
            this.colAlerts_Amount = ((BrightIdeasSoftware.OLVColumn)(new BrightIdeasSoftware.OLVColumn()));
            this.colAlerts_BranchName = ((BrightIdeasSoftware.OLVColumn)(new BrightIdeasSoftware.OLVColumn()));
            this.lblTitle = new System.Windows.Forms.Label();
            this.tabFilter = new System.Windows.Forms.TableLayoutPanel();
            this.chkPostponedLoans = new System.Windows.Forms.CheckBox();
            this.tbFilter = new System.Windows.Forms.TextBox();
            this.lblFilter = new System.Windows.Forms.Label();
            this.chkLateLoans = new System.Windows.Forms.CheckBox();
            this.chkPendingLoans = new System.Windows.Forms.CheckBox();
            this.chkPendingSavings = new System.Windows.Forms.CheckBox();
            this.chkOverdraftSavings = new System.Windows.Forms.CheckBox();
            this.chkValidatedLoan = new System.Windows.Forms.CheckBox();
            this.bwUserInformation = new System.ComponentModel.BackgroundWorker();
            this.alertBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.mainMenu.SuspendLayout();
            this.mainStatusBar.SuspendLayout();
            this.panelLeft.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.olvAlerts)).BeginInit();
            this.tabFilter.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.alertBindingSource)).BeginInit();
            this.SuspendLayout();
            // 
            // mnuClients
            // 
            resources.ApplyResources(this.mnuClients, "mnuClients");
            this.mnuClients.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.mnuNewClient,
            this.mnuSearchClient});
            this.mnuClients.Name = "mnuClients";
            // 
            // mnuNewClient
            // 
            resources.ApplyResources(this.mnuNewClient, "mnuNewClient");
            this.mnuNewClient.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.mnuNewPerson,
            this.mnuNewGroup,
            this.mnuNewVillage,
            this.newCorporateToolStripMenuItem});
            this.mnuNewClient.Name = "mnuNewClient";
            // 
            // mnuNewPerson
            // 
            resources.ApplyResources(this.mnuNewPerson, "mnuNewPerson");
            this.mnuNewPerson.Name = "mnuNewPerson";
            this.mnuNewPerson.Click += new System.EventHandler(this.mnuNewPerson_Click);
            // 
            // mnuNewGroup
            // 
            resources.ApplyResources(this.mnuNewGroup, "mnuNewGroup");
            this.mnuNewGroup.Name = "mnuNewGroup";
            this.mnuNewGroup.Click += new System.EventHandler(this.mnuNewGroup_Click);
            // 
            // mnuNewVillage
            // 
            resources.ApplyResources(this.mnuNewVillage, "mnuNewVillage");
            this.mnuNewVillage.Name = "mnuNewVillage";
            this.mnuNewVillage.Click += new System.EventHandler(this.mnuNewVillage_Click);
            // 
            // newCorporateToolStripMenuItem
            // 
            resources.ApplyResources(this.newCorporateToolStripMenuItem, "newCorporateToolStripMenuItem");
            this.newCorporateToolStripMenuItem.Name = "newCorporateToolStripMenuItem";
            this.newCorporateToolStripMenuItem.Click += new System.EventHandler(this.newCorporateToolStripMenuItem_Click);
            // 
            // mnuSearchClient
            // 
            resources.ApplyResources(this.mnuSearchClient, "mnuSearchClient");
            this.mnuSearchClient.Image = global::OpenCBS.GUI.Properties.Resources.find;
            this.mnuSearchClient.Name = "mnuSearchClient";
            this.mnuSearchClient.Click += new System.EventHandler(this.mnuSearchClient_Click);
            // 
            // mnuContracts
            // 
            resources.ApplyResources(this.mnuContracts, "mnuContracts");
            this.mnuContracts.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.mnuSearchContract,
            this.reasignToolStripMenuItem});
            this.mnuContracts.Name = "mnuContracts";
            // 
            // mnuSearchContract
            // 
            resources.ApplyResources(this.mnuSearchContract, "mnuSearchContract");
            this.mnuSearchContract.Image = global::OpenCBS.GUI.Properties.Resources.find;
            this.mnuSearchContract.Name = "mnuSearchContract";
            this.mnuSearchContract.Click += new System.EventHandler(this.mnuSearchContract_Click);
            // 
            // reasignToolStripMenuItem
            // 
            resources.ApplyResources(this.reasignToolStripMenuItem, "reasignToolStripMenuItem");
            this.reasignToolStripMenuItem.Name = "reasignToolStripMenuItem";
            this.reasignToolStripMenuItem.Click += new System.EventHandler(this.reasignToolStripMenuItem_Click);
            // 
            // mnuAccounting
            // 
            resources.ApplyResources(this.mnuAccounting, "mnuAccounting");
            this.mnuAccounting.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.mnuChartOfAccounts,
            this.accountingRulesToolStripMenuItem,
            this.trialBalanceToolStripMenuItem,
            this.toolStripMenuItemAccountView,
            this.manualEntriesToolStripMenuItem,
            this.standardToolStripMenuItem,
            this.toolStripSeparator2,
            this.menuItemExportTransaction,
            this.mnuNewclosure,
            this.fiscalYearToolStripMenuItem});
            this.mnuAccounting.Name = "mnuAccounting";
            // 
            // mnuChartOfAccounts
            // 
            resources.ApplyResources(this.mnuChartOfAccounts, "mnuChartOfAccounts");
            this.mnuChartOfAccounts.Image = global::OpenCBS.GUI.Properties.Resources.page;
            this.mnuChartOfAccounts.Name = "mnuChartOfAccounts";
            // 
            // accountingRulesToolStripMenuItem
            // 
            resources.ApplyResources(this.accountingRulesToolStripMenuItem, "accountingRulesToolStripMenuItem");
            this.accountingRulesToolStripMenuItem.Name = "accountingRulesToolStripMenuItem";
            this.accountingRulesToolStripMenuItem.Click += new System.EventHandler(this.accountingRulesToolStripMenuItem_Click);
            // 
            // trialBalanceToolStripMenuItem
            // 
            resources.ApplyResources(this.trialBalanceToolStripMenuItem, "trialBalanceToolStripMenuItem");
            this.trialBalanceToolStripMenuItem.Name = "trialBalanceToolStripMenuItem";
            this.trialBalanceToolStripMenuItem.Click += new System.EventHandler(this.trialBalanceToolStripMenuItem_Click);
            // 
            // toolStripMenuItemAccountView
            // 
            resources.ApplyResources(this.toolStripMenuItemAccountView, "toolStripMenuItemAccountView");
            this.toolStripMenuItemAccountView.Image = global::OpenCBS.GUI.Properties.Resources.book;
            this.toolStripMenuItemAccountView.Name = "toolStripMenuItemAccountView";
            this.toolStripMenuItemAccountView.Click += new System.EventHandler(this.toolStripMenuItemAccountView_Click);
            // 
            // manualEntriesToolStripMenuItem
            // 
            resources.ApplyResources(this.manualEntriesToolStripMenuItem, "manualEntriesToolStripMenuItem");
            this.manualEntriesToolStripMenuItem.Name = "manualEntriesToolStripMenuItem";
            this.manualEntriesToolStripMenuItem.Click += new System.EventHandler(this.manualEntriesToolStripMenuItem_Click);
            // 
            // standardToolStripMenuItem
            // 
            resources.ApplyResources(this.standardToolStripMenuItem, "standardToolStripMenuItem");
            this.standardToolStripMenuItem.Name = "standardToolStripMenuItem";
            this.standardToolStripMenuItem.Click += new System.EventHandler(this.standardToolStripMenuItem_Click);
            // 
            // toolStripSeparator2
            // 
            resources.ApplyResources(this.toolStripSeparator2, "toolStripSeparator2");
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            // 
            // menuItemExportTransaction
            // 
            resources.ApplyResources(this.menuItemExportTransaction, "menuItemExportTransaction");
            this.menuItemExportTransaction.Name = "menuItemExportTransaction";
            this.menuItemExportTransaction.Click += new System.EventHandler(this.menuItemExportTransaction_Click);
            // 
            // mnuNewclosure
            // 
            resources.ApplyResources(this.mnuNewclosure, "mnuNewclosure");
            this.mnuNewclosure.Name = "mnuNewclosure";
            this.mnuNewclosure.Click += new System.EventHandler(this.newClosureToolStripMenuItem_Click_1);
            // 
            // fiscalYearToolStripMenuItem
            // 
            resources.ApplyResources(this.fiscalYearToolStripMenuItem, "fiscalYearToolStripMenuItem");
            this.fiscalYearToolStripMenuItem.Name = "fiscalYearToolStripMenuItem";
            this.fiscalYearToolStripMenuItem.Click += new System.EventHandler(this.fiscalYearToolStripMenuItem_Click);
            // 
            // mnuConfiguration
            // 
            resources.ApplyResources(this.mnuConfiguration, "mnuConfiguration");
            this.mnuConfiguration.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.rolesToolStripMenuItem,
            this.menuItemAddUser,
            this.tellersToolStripMenuItem,
            this.branchesToolStripMenuItem,
            this.changePasswordToolStripMenuItem,
            this.languagesToolStripMenuItem,
            this.toolStripSeparatorConfig1,
            this.mnuPackages,
            this.savingProductsToolStripMenuItem,
            this.menuItemCollateralProducts,
            this.toolStripSeparator3,
            this.toolStripMenuItemFundingLines,
            this.toolStripSeparatorConfig2,
            this.mnuDomainOfApplication,
            this.menuItemLocations,
            this.toolStripMenuItemInstallmentTypes,
            this.miContractCode,
            this.toolStripSeparatorConfig3,
            this.menuItemExchangeRate,
            this.currenciesToolStripMenuItem,
            this.toolStripMenuItem1,
            this.menuItemApplicationDate,
            this.menuItemSetting,
            this.menuItemAdvancedSettings,
            this.CustomizableFieldsToolStripMenuItem});
            this.mnuConfiguration.Name = "mnuConfiguration";
            // 
            // rolesToolStripMenuItem
            // 
            resources.ApplyResources(this.rolesToolStripMenuItem, "rolesToolStripMenuItem");
            this.rolesToolStripMenuItem.Name = "rolesToolStripMenuItem";
            this.rolesToolStripMenuItem.Click += new System.EventHandler(this.rolesToolStripMenuItem_Click);
            // 
            // menuItemAddUser
            // 
            resources.ApplyResources(this.menuItemAddUser, "menuItemAddUser");
            this.menuItemAddUser.Image = global::OpenCBS.GUI.Properties.Resources.group;
            this.menuItemAddUser.Name = "menuItemAddUser";
            this.menuItemAddUser.Click += new System.EventHandler(this.menuItemAddUser_Click);
            // 
            // tellersToolStripMenuItem
            // 
            resources.ApplyResources(this.tellersToolStripMenuItem, "tellersToolStripMenuItem");
            this.tellersToolStripMenuItem.Name = "tellersToolStripMenuItem";
            this.tellersToolStripMenuItem.Click += new System.EventHandler(this.tellersToolStripMenuItem_Click);
            // 
            // branchesToolStripMenuItem
            // 
            resources.ApplyResources(this.branchesToolStripMenuItem, "branchesToolStripMenuItem");
            this.branchesToolStripMenuItem.Name = "branchesToolStripMenuItem";
            this.branchesToolStripMenuItem.Click += new System.EventHandler(this.branchesToolStripMenuItem_Click);
            // 
            // changePasswordToolStripMenuItem
            // 
            resources.ApplyResources(this.changePasswordToolStripMenuItem, "changePasswordToolStripMenuItem");
            this.changePasswordToolStripMenuItem.Name = "changePasswordToolStripMenuItem";
            this.changePasswordToolStripMenuItem.Click += new System.EventHandler(this.changePasswordToolStripMenuItem_Click);
            // 
            // languagesToolStripMenuItem
            // 
            resources.ApplyResources(this.languagesToolStripMenuItem, "languagesToolStripMenuItem");
            this.languagesToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.frenchToolStripMenuItem,
            this.englishToolStripMenuItem,
            this.russianToolStripMenuItem,
            this.spanishToolStripMenuItem,
            this.portugueseToolStripMenuItem});
            this.languagesToolStripMenuItem.Name = "languagesToolStripMenuItem";
            this.languagesToolStripMenuItem.DropDownOpening += new System.EventHandler(this.languagesToolStripMenuItem_DropDownOpening);
            // 
            // frenchToolStripMenuItem
            // 
            resources.ApplyResources(this.frenchToolStripMenuItem, "frenchToolStripMenuItem");
            this.frenchToolStripMenuItem.Image = global::OpenCBS.GUI.Properties.Resources.fr;
            this.frenchToolStripMenuItem.Name = "frenchToolStripMenuItem";
            this.frenchToolStripMenuItem.Tag = "fr";
            this.frenchToolStripMenuItem.Click += new System.EventHandler(this.LanguageToolStripMenuItem_Click);
            // 
            // englishToolStripMenuItem
            // 
            resources.ApplyResources(this.englishToolStripMenuItem, "englishToolStripMenuItem");
            this.englishToolStripMenuItem.Image = global::OpenCBS.GUI.Properties.Resources.gb;
            this.englishToolStripMenuItem.Name = "englishToolStripMenuItem";
            this.englishToolStripMenuItem.Tag = "en-US";
            this.englishToolStripMenuItem.Click += new System.EventHandler(this.LanguageToolStripMenuItem_Click);
            // 
            // russianToolStripMenuItem
            // 
            resources.ApplyResources(this.russianToolStripMenuItem, "russianToolStripMenuItem");
            this.russianToolStripMenuItem.Image = global::OpenCBS.GUI.Properties.Resources.ru;
            this.russianToolStripMenuItem.Name = "russianToolStripMenuItem";
            this.russianToolStripMenuItem.Tag = "ru-RU";
            this.russianToolStripMenuItem.Click += new System.EventHandler(this.LanguageToolStripMenuItem_Click);
            // 
            // spanishToolStripMenuItem
            // 
            resources.ApplyResources(this.spanishToolStripMenuItem, "spanishToolStripMenuItem");
            this.spanishToolStripMenuItem.Image = global::OpenCBS.GUI.Properties.Resources.es;
            this.spanishToolStripMenuItem.Name = "spanishToolStripMenuItem";
            this.spanishToolStripMenuItem.Tag = "es-ES";
            this.spanishToolStripMenuItem.Click += new System.EventHandler(this.LanguageToolStripMenuItem_Click);
            // 
            // portugueseToolStripMenuItem
            // 
            resources.ApplyResources(this.portugueseToolStripMenuItem, "portugueseToolStripMenuItem");
            this.portugueseToolStripMenuItem.Image = global::OpenCBS.GUI.Properties.Resources.pt;
            this.portugueseToolStripMenuItem.Name = "portugueseToolStripMenuItem";
            this.portugueseToolStripMenuItem.Click += new System.EventHandler(this.LanguageToolStripMenuItem_Click);
            // 
            // toolStripSeparatorConfig1
            // 
            resources.ApplyResources(this.toolStripSeparatorConfig1, "toolStripSeparatorConfig1");
            this.toolStripSeparatorConfig1.Name = "toolStripSeparatorConfig1";
            // 
            // mnuPackages
            // 
            resources.ApplyResources(this.mnuPackages, "mnuPackages");
            this.mnuPackages.Image = global::OpenCBS.GUI.Properties.Resources.package;
            this.mnuPackages.Name = "mnuPackages";
            this.mnuPackages.Click += new System.EventHandler(this.menuItemPackages_Click);
            // 
            // savingProductsToolStripMenuItem
            // 
            resources.ApplyResources(this.savingProductsToolStripMenuItem, "savingProductsToolStripMenuItem");
            this.savingProductsToolStripMenuItem.Image = global::OpenCBS.GUI.Properties.Resources.package;
            this.savingProductsToolStripMenuItem.Name = "savingProductsToolStripMenuItem";
            this.savingProductsToolStripMenuItem.Click += new System.EventHandler(this.savingProductsToolStripMenuItem_Click);
            // 
            // menuItemCollateralProducts
            // 
            resources.ApplyResources(this.menuItemCollateralProducts, "menuItemCollateralProducts");
            this.menuItemCollateralProducts.Image = global::OpenCBS.GUI.Properties.Resources.package;
            this.menuItemCollateralProducts.Name = "menuItemCollateralProducts";
            this.menuItemCollateralProducts.Click += new System.EventHandler(this.menuItemCollateralProducts_Click);
            // 
            // toolStripSeparator3
            // 
            resources.ApplyResources(this.toolStripSeparator3, "toolStripSeparator3");
            this.toolStripSeparator3.Name = "toolStripSeparator3";
            // 
            // toolStripMenuItemFundingLines
            // 
            resources.ApplyResources(this.toolStripMenuItemFundingLines, "toolStripMenuItemFundingLines");
            this.toolStripMenuItemFundingLines.Name = "toolStripMenuItemFundingLines";
            this.toolStripMenuItemFundingLines.Click += new System.EventHandler(this.toolStripMenuItemFundingLines_Click);
            // 
            // toolStripSeparatorConfig2
            // 
            resources.ApplyResources(this.toolStripSeparatorConfig2, "toolStripSeparatorConfig2");
            this.toolStripSeparatorConfig2.Name = "toolStripSeparatorConfig2";
            // 
            // mnuDomainOfApplication
            // 
            resources.ApplyResources(this.mnuDomainOfApplication, "mnuDomainOfApplication");
            this.mnuDomainOfApplication.Name = "mnuDomainOfApplication";
            this.mnuDomainOfApplication.Click += new System.EventHandler(this.mnuDomainOfApplication_Click);
            // 
            // menuItemLocations
            // 
            resources.ApplyResources(this.menuItemLocations, "menuItemLocations");
            this.menuItemLocations.Name = "menuItemLocations";
            this.menuItemLocations.Click += new System.EventHandler(this.menuItemLocations_Click);
            // 
            // toolStripMenuItemInstallmentTypes
            // 
            resources.ApplyResources(this.toolStripMenuItemInstallmentTypes, "toolStripMenuItemInstallmentTypes");
            this.toolStripMenuItemInstallmentTypes.Name = "toolStripMenuItemInstallmentTypes";
            this.toolStripMenuItemInstallmentTypes.Click += new System.EventHandler(this.toolStripMenuItemInstallmentTypes_Click);
            // 
            // miContractCode
            // 
            resources.ApplyResources(this.miContractCode, "miContractCode");
            this.miContractCode.Name = "miContractCode";
            this.miContractCode.Click += new System.EventHandler(this.miContractCode_Click);
            // 
            // toolStripSeparatorConfig3
            // 
            resources.ApplyResources(this.toolStripSeparatorConfig3, "toolStripSeparatorConfig3");
            this.toolStripSeparatorConfig3.Name = "toolStripSeparatorConfig3";
            // 
            // menuItemExchangeRate
            // 
            resources.ApplyResources(this.menuItemExchangeRate, "menuItemExchangeRate");
            this.menuItemExchangeRate.Name = "menuItemExchangeRate";
            this.menuItemExchangeRate.Click += new System.EventHandler(this.menuItemExchangeRate_Click);
            // 
            // currenciesToolStripMenuItem
            // 
            resources.ApplyResources(this.currenciesToolStripMenuItem, "currenciesToolStripMenuItem");
            this.currenciesToolStripMenuItem.Image = global::OpenCBS.GUI.Properties.Resources.money;
            this.currenciesToolStripMenuItem.Name = "currenciesToolStripMenuItem";
            this.currenciesToolStripMenuItem.Click += new System.EventHandler(this.currenciesToolStripMenuItem_Click);
            // 
            // toolStripMenuItem1
            // 
            resources.ApplyResources(this.toolStripMenuItem1, "toolStripMenuItem1");
            this.toolStripMenuItem1.Name = "toolStripMenuItem1";
            // 
            // menuItemApplicationDate
            // 
            resources.ApplyResources(this.menuItemApplicationDate, "menuItemApplicationDate");
            this.menuItemApplicationDate.Image = global::OpenCBS.GUI.Properties.Resources.calendar;
            this.menuItemApplicationDate.Name = "menuItemApplicationDate";
            this.menuItemApplicationDate.Click += new System.EventHandler(this.OnChangeApplicationDateClick);
            // 
            // menuItemSetting
            // 
            resources.ApplyResources(this.menuItemSetting, "menuItemSetting");
            this.menuItemSetting.Image = global::OpenCBS.GUI.Properties.Resources.cog;
            this.menuItemSetting.Name = "menuItemSetting";
            this.menuItemSetting.Click += new System.EventHandler(this.menuItemSetting_Click);
            // 
            // menuItemAdvancedSettings
            // 
            resources.ApplyResources(this.menuItemAdvancedSettings, "menuItemAdvancedSettings");
            this.menuItemAdvancedSettings.Name = "menuItemAdvancedSettings";
            this.menuItemAdvancedSettings.Click += new System.EventHandler(this.menuItemAdvancedSettings_Click);
            // 
            // CustomizableFieldsToolStripMenuItem
            // 
            resources.ApplyResources(this.CustomizableFieldsToolStripMenuItem, "CustomizableFieldsToolStripMenuItem");
            this.CustomizableFieldsToolStripMenuItem.Name = "CustomizableFieldsToolStripMenuItem";
            this.CustomizableFieldsToolStripMenuItem.Click += new System.EventHandler(this.CustomizableFieldsToolStripMenuItem_Click);
            // 
            // mnuDatamanagement
            // 
            resources.ApplyResources(this.mnuDatamanagement, "mnuDatamanagement");
            this.mnuDatamanagement.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.menuItemDatabaseControlPanel,
            this.menuItemDatabaseMaintenance});
            this.mnuDatamanagement.Name = "mnuDatamanagement";
            // 
            // menuItemDatabaseControlPanel
            // 
            resources.ApplyResources(this.menuItemDatabaseControlPanel, "menuItemDatabaseControlPanel");
            this.menuItemDatabaseControlPanel.Image = global::OpenCBS.GUI.Properties.Resources.database_gear;
            this.menuItemDatabaseControlPanel.Name = "menuItemDatabaseControlPanel";
            this.menuItemDatabaseControlPanel.Click += new System.EventHandler(this.menuItemBackupData_Click);
            // 
            // menuItemDatabaseMaintenance
            // 
            resources.ApplyResources(this.menuItemDatabaseMaintenance, "menuItemDatabaseMaintenance");
            this.menuItemDatabaseMaintenance.Name = "menuItemDatabaseMaintenance";
            this.menuItemDatabaseMaintenance.Click += new System.EventHandler(this.menuItemDatabaseMaintenance_Click);
            // 
            // mnuWindow
            // 
            resources.ApplyResources(this.mnuWindow, "mnuWindow");
            this.mnuWindow.Name = "mnuWindow";
            // 
            // mnuHelp
            // 
            resources.ApplyResources(this.mnuHelp, "mnuHelp");
            this.mnuHelp.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.aboutMenuItem});
            this.mnuHelp.Name = "mnuHelp";
            // 
            // aboutMenuItem
            // 
            resources.ApplyResources(this.aboutMenuItem, "aboutMenuItem");
            this.aboutMenuItem.Name = "aboutMenuItem";
            this.aboutMenuItem.Click += new System.EventHandler(this.OnAboutMenuItemClick);
            // 
            // imageListAlert
            // 
            this.imageListAlert.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imageListAlert.ImageStream")));
            this.imageListAlert.TransparentColor = System.Drawing.Color.Transparent;
            this.imageListAlert.Images.SetKeyName(0, "money.png");
            this.imageListAlert.Images.SetKeyName(1, "time.png");
            this.imageListAlert.Images.SetKeyName(2, "");
            this.imageListAlert.Images.SetKeyName(3, "");
            this.imageListAlert.Images.SetKeyName(4, "money_dollar.png");
            // 
            // mainMenu
            // 
            resources.ApplyResources(this.mainMenu, "mainMenu");
            this.mainMenu.BackColor = System.Drawing.SystemColors.Control;
            this.mainMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.mnuClients,
            this.mnuContracts,
            this.mnuAccounting,
            this.mnuConfiguration,
            this.reportsToolStripMenuItem,
            this.mView,
            this.mnuDatamanagement,
            this.mnuWindow,
            this.mnuHelp});
            this.mainMenu.MdiWindowListItem = this.mnuWindow;
            this.mainMenu.Name = "mainMenu";
            // 
            // reportsToolStripMenuItem
            // 
            resources.ApplyResources(this.reportsToolStripMenuItem, "reportsToolStripMenuItem");
            this.reportsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.otherToolStripMenuItem});
            this.reportsToolStripMenuItem.Name = "reportsToolStripMenuItem";
            // 
            // otherToolStripMenuItem
            // 
            resources.ApplyResources(this.otherToolStripMenuItem, "otherToolStripMenuItem");
            this.otherToolStripMenuItem.Name = "otherToolStripMenuItem";
            // 
            // mView
            // 
            resources.ApplyResources(this.mView, "mView");
            this.mView.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.miAuditTrail});
            this.mView.Name = "mView";
            // 
            // miAuditTrail
            // 
            resources.ApplyResources(this.miAuditTrail, "miAuditTrail");
            this.miAuditTrail.Name = "miAuditTrail";
            this.miAuditTrail.Click += new System.EventHandler(this.eventsToolStripMenuItem_Click);
            // 
            // timer
            // 
            this.timer.Enabled = true;
            this.timer.Interval = 1000;
            this.timer.Tick += new System.EventHandler(this.timer_Tick);
            // 
            // toolBarLblVersion
            // 
            resources.ApplyResources(this.toolBarLblVersion, "toolBarLblVersion");
            this.toolBarLblVersion.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.toolBarLblVersion.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(180)))), ((int)(((byte)(156)))));
            this.toolBarLblVersion.Name = "toolBarLblVersion";
            // 
            // mainStatusBar
            // 
            resources.ApplyResources(this.mainStatusBar, "mainStatusBar");
            this.mainStatusBar.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.mainStatusBarLblUpdateVersion,
            this.mainStatusBarLblUserName,
            this.mainStatusBarLblDate,
            this.toolStripStatusLblBranchCode,
            this.toolStripStatusLblDB});
            this.mainStatusBar.Name = "mainStatusBar";
            this.mainStatusBar.RenderMode = System.Windows.Forms.ToolStripRenderMode.ManagerRenderMode;
            this.mainStatusBar.ShowItemToolTips = true;
            this.mainStatusBar.SizingGrip = false;
            // 
            // mainStatusBarLblUpdateVersion
            // 
            resources.ApplyResources(this.mainStatusBarLblUpdateVersion, "mainStatusBarLblUpdateVersion");
            this.mainStatusBarLblUpdateVersion.Name = "mainStatusBarLblUpdateVersion";
            this.mainStatusBarLblUpdateVersion.Spring = true;
            // 
            // mainStatusBarLblUserName
            // 
            resources.ApplyResources(this.mainStatusBarLblUserName, "mainStatusBarLblUserName");
            this.mainStatusBarLblUserName.BorderSides = ((System.Windows.Forms.ToolStripStatusLabelBorderSides)((((System.Windows.Forms.ToolStripStatusLabelBorderSides.Left | System.Windows.Forms.ToolStripStatusLabelBorderSides.Top) 
            | System.Windows.Forms.ToolStripStatusLabelBorderSides.Right) 
            | System.Windows.Forms.ToolStripStatusLabelBorderSides.Bottom)));
            this.mainStatusBarLblUserName.BorderStyle = System.Windows.Forms.Border3DStyle.SunkenOuter;
            this.mainStatusBarLblUserName.Image = global::OpenCBS.GUI.Properties.Resources.user_gray;
            this.mainStatusBarLblUserName.Name = "mainStatusBarLblUserName";
            // 
            // mainStatusBarLblDate
            // 
            resources.ApplyResources(this.mainStatusBarLblDate, "mainStatusBarLblDate");
            this.mainStatusBarLblDate.BorderSides = ((System.Windows.Forms.ToolStripStatusLabelBorderSides)((((System.Windows.Forms.ToolStripStatusLabelBorderSides.Left | System.Windows.Forms.ToolStripStatusLabelBorderSides.Top) 
            | System.Windows.Forms.ToolStripStatusLabelBorderSides.Right) 
            | System.Windows.Forms.ToolStripStatusLabelBorderSides.Bottom)));
            this.mainStatusBarLblDate.BorderStyle = System.Windows.Forms.Border3DStyle.SunkenOuter;
            this.mainStatusBarLblDate.Image = global::OpenCBS.GUI.Properties.Resources.calendar;
            this.mainStatusBarLblDate.Name = "mainStatusBarLblDate";
            // 
            // toolStripStatusLblBranchCode
            // 
            resources.ApplyResources(this.toolStripStatusLblBranchCode, "toolStripStatusLblBranchCode");
            this.toolStripStatusLblBranchCode.BorderSides = ((System.Windows.Forms.ToolStripStatusLabelBorderSides)((((System.Windows.Forms.ToolStripStatusLabelBorderSides.Left | System.Windows.Forms.ToolStripStatusLabelBorderSides.Top) 
            | System.Windows.Forms.ToolStripStatusLabelBorderSides.Right) 
            | System.Windows.Forms.ToolStripStatusLabelBorderSides.Bottom)));
            this.toolStripStatusLblBranchCode.BorderStyle = System.Windows.Forms.Border3DStyle.SunkenOuter;
            this.toolStripStatusLblBranchCode.Name = "toolStripStatusLblBranchCode";
            // 
            // toolStripStatusLblDB
            // 
            resources.ApplyResources(this.toolStripStatusLblDB, "toolStripStatusLblDB");
            this.toolStripStatusLblDB.BorderSides = ((System.Windows.Forms.ToolStripStatusLabelBorderSides)((((System.Windows.Forms.ToolStripStatusLabelBorderSides.Left | System.Windows.Forms.ToolStripStatusLabelBorderSides.Top) 
            | System.Windows.Forms.ToolStripStatusLabelBorderSides.Right) 
            | System.Windows.Forms.ToolStripStatusLabelBorderSides.Bottom)));
            this.toolStripStatusLblDB.BorderStyle = System.Windows.Forms.Border3DStyle.SunkenOuter;
            this.toolStripStatusLblDB.Image = global::OpenCBS.GUI.Properties.Resources.database;
            this.toolStripStatusLblDB.Name = "toolStripStatusLblDB";
            // 
            // bwAlerts
            // 
            this.bwAlerts.WorkerSupportsCancellation = true;
            this.bwAlerts.DoWork += new System.ComponentModel.DoWorkEventHandler(this.OnAlertsLoading);
            this.bwAlerts.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(this.OnAlertsLoaded);
            // 
            // nIUpdateAvailable
            // 
            this.nIUpdateAvailable.BalloonTipIcon = System.Windows.Forms.ToolTipIcon.Info;
            resources.ApplyResources(this.nIUpdateAvailable, "nIUpdateAvailable");
            this.nIUpdateAvailable.BalloonTipClicked += new System.EventHandler(this.nIUpdateAvailable_BalloonTipClicked);
            // 
            // openCustomizableFileDialog
            // 
            resources.ApplyResources(this.openCustomizableFileDialog, "openCustomizableFileDialog");
            // 
            // colAlerts_Address
            // 
            this.colAlerts_Address.AspectName = "Address";
            resources.ApplyResources(this.colAlerts_Address, "colAlerts_Address");
            this.colAlerts_Address.IsEditable = false;
            this.colAlerts_Address.IsVisible = false;
            // 
            // colAlerts_City
            // 
            this.colAlerts_City.AspectName = "City";
            resources.ApplyResources(this.colAlerts_City, "colAlerts_City");
            this.colAlerts_City.IsEditable = false;
            this.colAlerts_City.IsVisible = false;
            // 
            // colAlerts_Phone
            // 
            this.colAlerts_Phone.AspectName = "Phone";
            resources.ApplyResources(this.colAlerts_Phone, "colAlerts_Phone");
            this.colAlerts_Phone.IsEditable = false;
            this.colAlerts_Phone.IsVisible = false;
            // 
            // splitter6
            // 
            resources.ApplyResources(this.splitter6, "splitter6");
            this.splitter6.AnimationDelay = 20;
            this.splitter6.AnimationStep = 20;
            this.splitter6.BorderStyle3D = System.Windows.Forms.Border3DStyle.Flat;
            this.splitter6.ControlToHide = this.panelLeft;
            this.splitter6.ExpandParentForm = false;
            this.splitter6.Name = "splitter6";
            this.splitter6.TabStop = false;
            this.splitter6.UseAnimations = false;
            this.splitter6.VisualStyle = OpenCBS.GUI.UserControl.VisualStyles.Mozilla;
            // 
            // panelLeft
            // 
            resources.ApplyResources(this.panelLeft, "panelLeft");
            this.panelLeft.BackColor = System.Drawing.Color.Transparent;
            this.panelLeft.Controls.Add(this.olvAlerts);
            this.panelLeft.Controls.Add(this.lblTitle);
            this.panelLeft.Controls.Add(this.tabFilter);
            this.panelLeft.Name = "panelLeft";
            // 
            // olvAlerts
            // 
            resources.ApplyResources(this.olvAlerts, "olvAlerts");
            this.olvAlerts.AllColumns.Add(this.colAlerts_ContractCode);
            this.olvAlerts.AllColumns.Add(this.colAlerts_Status);
            this.olvAlerts.AllColumns.Add(this.colAlerts_Client);
            this.olvAlerts.AllColumns.Add(this.colAlerts_LoanOfficer);
            this.olvAlerts.AllColumns.Add(this.colAlerts_Date);
            this.olvAlerts.AllColumns.Add(this.colAlerts_Amount);
            this.olvAlerts.AllColumns.Add(this.colAlerts_Address);
            this.olvAlerts.AllColumns.Add(this.colAlerts_City);
            this.olvAlerts.AllColumns.Add(this.colAlerts_Phone);
            this.olvAlerts.AllColumns.Add(this.colAlerts_BranchName);
            this.olvAlerts.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.colAlerts_ContractCode,
            this.colAlerts_Status,
            this.colAlerts_Client,
            this.colAlerts_LoanOfficer,
            this.colAlerts_Date,
            this.colAlerts_Amount});
            this.olvAlerts.FullRowSelect = true;
            this.olvAlerts.GridLines = true;
            this.olvAlerts.HasCollapsibleGroups = false;
            this.olvAlerts.Name = "olvAlerts";
            this.olvAlerts.OverlayText.Text = resources.GetString("resource.Text");
            this.olvAlerts.ShowGroups = false;
            this.olvAlerts.SmallImageList = this.imageListAlert;
            this.olvAlerts.UseCompatibleStateImageBehavior = false;
            this.olvAlerts.View = System.Windows.Forms.View.Details;
            this.olvAlerts.FormatRow += new System.EventHandler<BrightIdeasSoftware.FormatRowEventArgs>(this.OnFormatAlertRow);
            this.olvAlerts.ItemsChanged += new System.EventHandler<BrightIdeasSoftware.ItemsChangedEventArgs>(this.OnAlertItemsChanged);
            this.olvAlerts.DoubleClick += new System.EventHandler(this.OnAlertDoubleClicked);
            // 
            // colAlerts_ContractCode
            // 
            this.colAlerts_ContractCode.AspectName = "ContractCode";
            resources.ApplyResources(this.colAlerts_ContractCode, "colAlerts_ContractCode");
            this.colAlerts_ContractCode.IsEditable = false;
            // 
            // colAlerts_Status
            // 
            this.colAlerts_Status.AspectName = "Status";
            resources.ApplyResources(this.colAlerts_Status, "colAlerts_Status");
            this.colAlerts_Status.IsEditable = false;
            // 
            // colAlerts_Client
            // 
            this.colAlerts_Client.AspectName = "ClientName";
            resources.ApplyResources(this.colAlerts_Client, "colAlerts_Client");
            this.colAlerts_Client.IsEditable = false;
            // 
            // colAlerts_LoanOfficer
            // 
            this.colAlerts_LoanOfficer.AspectName = "LoanOfficer";
            resources.ApplyResources(this.colAlerts_LoanOfficer, "colAlerts_LoanOfficer");
            this.colAlerts_LoanOfficer.IsEditable = false;
            // 
            // colAlerts_Date
            // 
            this.colAlerts_Date.AspectName = "Date";
            resources.ApplyResources(this.colAlerts_Date, "colAlerts_Date");
            this.colAlerts_Date.IsEditable = false;
            // 
            // colAlerts_Amount
            // 
            this.colAlerts_Amount.AspectName = "Amount";
            resources.ApplyResources(this.colAlerts_Amount, "colAlerts_Amount");
            this.colAlerts_Amount.HeaderTextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.colAlerts_Amount.IsEditable = false;
            // 
            // colAlerts_BranchName
            // 
            this.colAlerts_BranchName.AspectName = "BranchName";
            resources.ApplyResources(this.colAlerts_BranchName, "colAlerts_BranchName");
            this.colAlerts_BranchName.IsEditable = false;
            this.colAlerts_BranchName.IsVisible = false;
            // 
            // lblTitle
            // 
            resources.ApplyResources(this.lblTitle, "lblTitle");
            this.lblTitle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(81)))), ((int)(((byte)(152)))));
            this.lblTitle.ForeColor = System.Drawing.Color.White;
            this.lblTitle.Name = "lblTitle";
            // 
            // tabFilter
            // 
            resources.ApplyResources(this.tabFilter, "tabFilter");
            this.tabFilter.Controls.Add(this.chkPostponedLoans, 0, 3);
            this.tabFilter.Controls.Add(this.tbFilter, 1, 0);
            this.tabFilter.Controls.Add(this.lblFilter, 0, 0);
            this.tabFilter.Controls.Add(this.chkLateLoans, 0, 1);
            this.tabFilter.Controls.Add(this.chkPendingLoans, 0, 2);
            this.tabFilter.Controls.Add(this.chkPendingSavings, 0, 5);
            this.tabFilter.Controls.Add(this.chkOverdraftSavings, 0, 6);
            this.tabFilter.Controls.Add(this.chkValidatedLoan, 0, 4);
            this.tabFilter.Name = "tabFilter";
            // 
            // chkPostponedLoans
            // 
            resources.ApplyResources(this.chkPostponedLoans, "chkPostponedLoans");
            this.chkPostponedLoans.Checked = true;
            this.chkPostponedLoans.CheckState = System.Windows.Forms.CheckState.Checked;
            this.tabFilter.SetColumnSpan(this.chkPostponedLoans, 2);
            this.chkPostponedLoans.Name = "chkPostponedLoans";
            this.chkPostponedLoans.CheckedChanged += new System.EventHandler(this.OnAlertCheckChanged);
            // 
            // tbFilter
            // 
            resources.ApplyResources(this.tbFilter, "tbFilter");
            this.tbFilter.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(255)))), ((int)(((byte)(200)))));
            this.tbFilter.Name = "tbFilter";
            this.tbFilter.TextChanged += new System.EventHandler(this.OnFilterChanged);
            // 
            // lblFilter
            // 
            resources.ApplyResources(this.lblFilter, "lblFilter");
            this.lblFilter.Name = "lblFilter";
            // 
            // chkLateLoans
            // 
            resources.ApplyResources(this.chkLateLoans, "chkLateLoans");
            this.chkLateLoans.Checked = true;
            this.chkLateLoans.CheckState = System.Windows.Forms.CheckState.Checked;
            this.tabFilter.SetColumnSpan(this.chkLateLoans, 2);
            this.chkLateLoans.Name = "chkLateLoans";
            this.chkLateLoans.CheckedChanged += new System.EventHandler(this.OnAlertCheckChanged);
            // 
            // chkPendingLoans
            // 
            resources.ApplyResources(this.chkPendingLoans, "chkPendingLoans");
            this.chkPendingLoans.Checked = true;
            this.chkPendingLoans.CheckState = System.Windows.Forms.CheckState.Checked;
            this.tabFilter.SetColumnSpan(this.chkPendingLoans, 2);
            this.chkPendingLoans.Name = "chkPendingLoans";
            this.chkPendingLoans.CheckedChanged += new System.EventHandler(this.OnAlertCheckChanged);
            // 
            // chkPendingSavings
            // 
            resources.ApplyResources(this.chkPendingSavings, "chkPendingSavings");
            this.chkPendingSavings.Checked = true;
            this.chkPendingSavings.CheckState = System.Windows.Forms.CheckState.Checked;
            this.tabFilter.SetColumnSpan(this.chkPendingSavings, 2);
            this.chkPendingSavings.Name = "chkPendingSavings";
            this.chkPendingSavings.CheckedChanged += new System.EventHandler(this.OnAlertCheckChanged);
            // 
            // chkOverdraftSavings
            // 
            resources.ApplyResources(this.chkOverdraftSavings, "chkOverdraftSavings");
            this.chkOverdraftSavings.Checked = true;
            this.chkOverdraftSavings.CheckState = System.Windows.Forms.CheckState.Checked;
            this.tabFilter.SetColumnSpan(this.chkOverdraftSavings, 2);
            this.chkOverdraftSavings.Name = "chkOverdraftSavings";
            this.chkOverdraftSavings.CheckedChanged += new System.EventHandler(this.OnAlertCheckChanged);
            // 
            // chkValidatedLoan
            // 
            resources.ApplyResources(this.chkValidatedLoan, "chkValidatedLoan");
            this.chkValidatedLoan.Checked = true;
            this.chkValidatedLoan.CheckState = System.Windows.Forms.CheckState.Checked;
            this.tabFilter.SetColumnSpan(this.chkValidatedLoan, 2);
            this.chkValidatedLoan.Name = "chkValidatedLoan";
            this.chkValidatedLoan.CheckedChanged += new System.EventHandler(this.OnAlertCheckChanged);
            // 
            // alertBindingSource
            // 
            this.alertBindingSource.DataSource = typeof(OpenCBS.CoreDomain.Alert);
            // 
            // LotrasmicMainWindowForm
            // 
            resources.ApplyResources(this, "$this");
            this.Controls.Add(this.splitter6);
            this.Controls.Add(this.panelLeft);
            this.Controls.Add(this.mainStatusBar);
            this.Controls.Add(this.mainMenu);
            this.IsMdiContainer = true;
            this.MainMenuStrip = this.mainMenu;
            this.Name = "LotrasmicMainWindowForm";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.LotrasmicMainWindowForm_FormClosing);
            this.Load += new System.EventHandler(this.LotrasmicMainWindowForm_Load);
            this.mainMenu.ResumeLayout(false);
            this.mainMenu.PerformLayout();
            this.mainStatusBar.ResumeLayout(false);
            this.mainStatusBar.PerformLayout();
            this.panelLeft.ResumeLayout(false);
            this.panelLeft.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.olvAlerts)).EndInit();
            this.tabFilter.ResumeLayout(false);
            this.tabFilter.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.alertBindingSource)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }
        #endregion

        private ToolStripMenuItem reasignToolStripMenuItem;
        private ToolStripMenuItem newCorporateToolStripMenuItem;
        private ToolStripMenuItem savingProductsToolStripMenuItem;
        private ToolStripMenuItem mnuNewVillage;
        private ToolStripMenuItem miContractCode;
        private ToolStripMenuItem spanishToolStripMenuItem;
        private ToolStripSeparator toolStripMenuItem1;
        private System.ComponentModel.BackgroundWorker bwAlerts;
        private ToolStripMenuItem standardToolStripMenuItem;
        private ToolStripMenuItem currenciesToolStripMenuItem;
        private ToolStripMenuItem mView;
        private ToolStripMenuItem miAuditTrail;
        private Panel panelLeft;
        private Label lblTitle;
        private ToolStripMenuItem accountingRulesToolStripMenuItem;
        private NotifyIcon nIUpdateAvailable;
        private ToolStripMenuItem rolesToolStripMenuItem;
        private OpenFileDialog openCustomizableFileDialog;
        private BindingSource alertBindingSource;
        private ToolStripMenuItem menuItemCollateralProducts;
        private ToolStripMenuItem trialBalanceToolStripMenuItem;
        private BrightIdeasSoftware.ObjectListView olvAlerts;
        private BrightIdeasSoftware.OLVColumn colAlerts_ContractCode;
        private BrightIdeasSoftware.OLVColumn colAlerts_Status;
        private BrightIdeasSoftware.OLVColumn colAlerts_Client;
        private BrightIdeasSoftware.OLVColumn colAlerts_LoanOfficer;
        private BrightIdeasSoftware.OLVColumn colAlerts_Date;
        private BrightIdeasSoftware.OLVColumn colAlerts_Amount;
        private BrightIdeasSoftware.OLVColumn colAlerts_Address;
        private BrightIdeasSoftware.OLVColumn colAlerts_City;
        private BrightIdeasSoftware.OLVColumn colAlerts_Phone;
        private Label lblFilter;
        private TextBox tbFilter;
        private CheckBox chkLateLoans;
        private TableLayoutPanel tabFilter;
        private ToolStripMenuItem changePasswordToolStripMenuItem;
        private CheckBox chkPendingLoans;
        private ToolStripMenuItem manualEntriesToolStripMenuItem;
        private ToolStripMenuItem portugueseToolStripMenuItem;
        private ToolStripMenuItem branchesToolStripMenuItem;
        private ToolStripStatusLabel toolStripStatusLblDB;
        private BrightIdeasSoftware.OLVColumn colAlerts_BranchName;
        private ToolStripMenuItem CustomizableFieldsToolStripMenuItem;
        private ToolStripMenuItem menuItemAddUser;
        private CheckBox chkOverdraftSavings;
        private CheckBox chkPostponedLoans;
        private CheckBox chkPendingSavings;
        private CheckBox chkValidatedLoan;
        private ToolStripMenuItem mnuNewclosure;
        private System.ComponentModel.BackgroundWorker bwUserInformation;
        private ToolStripMenuItem fiscalYearToolStripMenuItem;
        private ToolStripMenuItem tellersToolStripMenuItem;
        private ToolStripMenuItem aboutMenuItem;
        private ToolStripMenuItem reportsToolStripMenuItem;
        private ToolStripMenuItem otherToolStripMenuItem;


    }
}
