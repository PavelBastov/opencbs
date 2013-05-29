

namespace OpenCBS.GUI
{
    partial class DashboardForm
    {
        private System.ComponentModel.IContainer components;

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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(DashboardForm));
            this.generalInfoPanel = new System.Windows.Forms.Panel();
            this.infoPanel = new System.Windows.Forms.Panel();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.tableLayoutPanel3 = new System.Windows.Forms.TableLayoutPanel();
            this.disbursementsPanel = new System.Windows.Forms.Panel();
            this.olbTrendPanel = new System.Windows.Forms.Panel();
            this.label2 = new System.Windows.Forms.Label();
            this.riskTableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
            this.parListView = new BrightIdeasSoftware.ObjectListView();
            this.parNameColumn = ((BrightIdeasSoftware.OLVColumn)(new BrightIdeasSoftware.OLVColumn()));
            this.parAmountColumn = ((BrightIdeasSoftware.OLVColumn)(new BrightIdeasSoftware.OLVColumn()));
            this.parQuantityColumn = ((BrightIdeasSoftware.OLVColumn)(new BrightIdeasSoftware.OLVColumn()));
            this.tableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
            this.portfolioPanel = new System.Windows.Forms.Panel();
            this.parPanel = new System.Windows.Forms.Panel();
            this.label1 = new System.Windows.Forms.Label();
            this.topBarPanel = new System.Windows.Forms.TableLayoutPanel();
            this.refreshLinkLabel = new System.Windows.Forms.LinkLabel();
            this.smallLogoPictureBox = new System.Windows.Forms.PictureBox();
            this.aboutLabel = new System.Windows.Forms.Label();
            this.quickLinksPanel = new System.Windows.Forms.Panel();
            this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            this.operateLabel = new System.Windows.Forms.Label();
            this.searchClientLabel = new System.Windows.Forms.LinkLabel();
            this.searchContractLabel = new System.Windows.Forms.LinkLabel();
            this.newIndividualClientLinkLabel = new System.Windows.Forms.LinkLabel();
            this.newSolidarityGroupLinkLabel = new System.Windows.Forms.LinkLabel();
            this.newNonSolidairtyGroupLinkLabel = new System.Windows.Forms.LinkLabel();
            this.newCorporateClientLinkLabel = new System.Windows.Forms.LinkLabel();
            this.configureLabel = new System.Windows.Forms.Label();
            this.configureLoanProductsLabel = new System.Windows.Forms.LinkLabel();
            this.configureSavingsProductsLabel = new System.Windows.Forms.LinkLabel();
            this.configureCollateralProducts = new System.Windows.Forms.LinkLabel();
            this.configureSettingsLabel = new System.Windows.Forms.LinkLabel();
            this.configurePermissionsLabel = new System.Windows.Forms.LinkLabel();
            this.controlLabel = new System.Windows.Forms.Label();
            this.auditTrailLabel = new System.Windows.Forms.LinkLabel();
            this.activeLoansLink = new System.Windows.Forms.LinkLabel();
            this.parAnalysisLink = new System.Windows.Forms.LinkLabel();
            this.delinquentLoansLink = new System.Windows.Forms.LinkLabel();
            this.disbursementsLink = new System.Windows.Forms.LinkLabel();
            this.generalInfoPanel.SuspendLayout();
            this.infoPanel.SuspendLayout();
            this.tableLayoutPanel1.SuspendLayout();
            this.tableLayoutPanel3.SuspendLayout();
            this.riskTableLayoutPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.parListView)).BeginInit();
            this.tableLayoutPanel2.SuspendLayout();
            this.topBarPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.smallLogoPictureBox)).BeginInit();
            this.quickLinksPanel.SuspendLayout();
            this.flowLayoutPanel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // generalInfoPanel
            // 
            this.generalInfoPanel.Controls.Add(this.infoPanel);
            this.generalInfoPanel.Controls.Add(this.quickLinksPanel);
            resources.ApplyResources(this.generalInfoPanel, "generalInfoPanel");
            this.generalInfoPanel.Name = "generalInfoPanel";
            // 
            // infoPanel
            // 
            this.infoPanel.BackColor = System.Drawing.Color.White;
            this.infoPanel.Controls.Add(this.tableLayoutPanel1);
            this.infoPanel.Controls.Add(this.label2);
            this.infoPanel.Controls.Add(this.riskTableLayoutPanel);
            this.infoPanel.Controls.Add(this.label1);
            this.infoPanel.Controls.Add(this.topBarPanel);
            resources.ApplyResources(this.infoPanel, "infoPanel");
            this.infoPanel.Name = "infoPanel";
            // 
            // tableLayoutPanel1
            // 
            resources.ApplyResources(this.tableLayoutPanel1, "tableLayoutPanel1");
            this.tableLayoutPanel1.Controls.Add(this.tableLayoutPanel3, 0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            // 
            // tableLayoutPanel3
            // 
            resources.ApplyResources(this.tableLayoutPanel3, "tableLayoutPanel3");
            this.tableLayoutPanel3.Controls.Add(this.disbursementsPanel, 0, 0);
            this.tableLayoutPanel3.Controls.Add(this.olbTrendPanel, 1, 0);
            this.tableLayoutPanel3.Name = "tableLayoutPanel3";
            // 
            // disbursementsPanel
            // 
            resources.ApplyResources(this.disbursementsPanel, "disbursementsPanel");
            this.disbursementsPanel.Name = "disbursementsPanel";
            // 
            // olbTrendPanel
            // 
            resources.ApplyResources(this.olbTrendPanel, "olbTrendPanel");
            this.olbTrendPanel.Name = "olbTrendPanel";
            // 
            // label2
            // 
            resources.ApplyResources(this.label2, "label2");
            this.label2.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(48)))));
            this.label2.Name = "label2";
            // 
            // riskTableLayoutPanel
            // 
            resources.ApplyResources(this.riskTableLayoutPanel, "riskTableLayoutPanel");
            this.riskTableLayoutPanel.Controls.Add(this.parListView, 1, 0);
            this.riskTableLayoutPanel.Controls.Add(this.tableLayoutPanel2, 0, 0);
            this.riskTableLayoutPanel.Name = "riskTableLayoutPanel";
            // 
            // parListView
            // 
            this.parListView.AllColumns.Add(this.parNameColumn);
            this.parListView.AllColumns.Add(this.parAmountColumn);
            this.parListView.AllColumns.Add(this.parQuantityColumn);
            this.parListView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.parNameColumn,
            this.parAmountColumn,
            this.parQuantityColumn});
            resources.ApplyResources(this.parListView, "parListView");
            this.parListView.FullRowSelect = true;
            this.parListView.GridLines = true;
            this.parListView.HasCollapsibleGroups = false;
            this.parListView.Name = "parListView";
            this.parListView.ShowGroups = false;
            this.parListView.UseCompatibleStateImageBehavior = false;
            this.parListView.View = System.Windows.Forms.View.Details;
            // 
            // parNameColumn
            // 
            this.parNameColumn.AspectName = "Name";
            resources.ApplyResources(this.parNameColumn, "parNameColumn");
            // 
            // parAmountColumn
            // 
            this.parAmountColumn.AspectName = "Amount";
            this.parAmountColumn.HeaderTextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            resources.ApplyResources(this.parAmountColumn, "parAmountColumn");
            // 
            // parQuantityColumn
            // 
            this.parQuantityColumn.AspectName = "Quantity";
            this.parQuantityColumn.HeaderTextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            resources.ApplyResources(this.parQuantityColumn, "parQuantityColumn");
            // 
            // tableLayoutPanel2
            // 
            resources.ApplyResources(this.tableLayoutPanel2, "tableLayoutPanel2");
            this.tableLayoutPanel2.Controls.Add(this.portfolioPanel, 0, 0);
            this.tableLayoutPanel2.Controls.Add(this.parPanel, 1, 0);
            this.tableLayoutPanel2.Name = "tableLayoutPanel2";
            // 
            // portfolioPanel
            // 
            resources.ApplyResources(this.portfolioPanel, "portfolioPanel");
            this.portfolioPanel.Name = "portfolioPanel";
            // 
            // parPanel
            // 
            resources.ApplyResources(this.parPanel, "parPanel");
            this.parPanel.Name = "parPanel";
            // 
            // label1
            // 
            resources.ApplyResources(this.label1, "label1");
            this.label1.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(48)))));
            this.label1.Name = "label1";
            // 
            // topBarPanel
            // 
            resources.ApplyResources(this.topBarPanel, "topBarPanel");
            this.topBarPanel.Controls.Add(this.refreshLinkLabel, 2, 0);
            this.topBarPanel.Controls.Add(this.smallLogoPictureBox, 0, 0);
            this.topBarPanel.Controls.Add(this.aboutLabel, 1, 0);
            this.topBarPanel.Name = "topBarPanel";
            // 
            // refreshLinkLabel
            // 
            this.refreshLinkLabel.ActiveLinkColor = System.Drawing.Color.FromArgb(((int)(((byte)(98)))), ((int)(((byte)(192)))), ((int)(((byte)(255)))));
            resources.ApplyResources(this.refreshLinkLabel, "refreshLinkLabel");
            this.refreshLinkLabel.LinkColor = System.Drawing.Color.FromArgb(((int)(((byte)(28)))), ((int)(((byte)(151)))), ((int)(((byte)(234)))));
            this.refreshLinkLabel.Name = "refreshLinkLabel";
            this.refreshLinkLabel.TabStop = true;
            this.refreshLinkLabel.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.OnRefreshLinkLabelClick);
            // 
            // smallLogoPictureBox
            // 
            resources.ApplyResources(this.smallLogoPictureBox, "smallLogoPictureBox");
            this.smallLogoPictureBox.Image = global::OpenCBS.GUI.Properties.Resources.SmallLogo;
            this.smallLogoPictureBox.Name = "smallLogoPictureBox";
            this.smallLogoPictureBox.TabStop = false;
            // 
            // aboutLabel
            // 
            resources.ApplyResources(this.aboutLabel, "aboutLabel");
            this.aboutLabel.Name = "aboutLabel";
            // 
            // quickLinksPanel
            // 
            this.quickLinksPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(48)))));
            this.quickLinksPanel.Controls.Add(this.flowLayoutPanel1);
            resources.ApplyResources(this.quickLinksPanel, "quickLinksPanel");
            this.quickLinksPanel.Name = "quickLinksPanel";
            // 
            // flowLayoutPanel1
            // 
            this.flowLayoutPanel1.Controls.Add(this.operateLabel);
            this.flowLayoutPanel1.Controls.Add(this.searchClientLabel);
            this.flowLayoutPanel1.Controls.Add(this.searchContractLabel);
            this.flowLayoutPanel1.Controls.Add(this.newIndividualClientLinkLabel);
            this.flowLayoutPanel1.Controls.Add(this.newSolidarityGroupLinkLabel);
            this.flowLayoutPanel1.Controls.Add(this.newNonSolidairtyGroupLinkLabel);
            this.flowLayoutPanel1.Controls.Add(this.newCorporateClientLinkLabel);
            this.flowLayoutPanel1.Controls.Add(this.configureLabel);
            this.flowLayoutPanel1.Controls.Add(this.configureLoanProductsLabel);
            this.flowLayoutPanel1.Controls.Add(this.configureSavingsProductsLabel);
            this.flowLayoutPanel1.Controls.Add(this.configureCollateralProducts);
            this.flowLayoutPanel1.Controls.Add(this.configureSettingsLabel);
            this.flowLayoutPanel1.Controls.Add(this.configurePermissionsLabel);
            this.flowLayoutPanel1.Controls.Add(this.controlLabel);
            this.flowLayoutPanel1.Controls.Add(this.auditTrailLabel);
            this.flowLayoutPanel1.Controls.Add(this.activeLoansLink);
            this.flowLayoutPanel1.Controls.Add(this.parAnalysisLink);
            this.flowLayoutPanel1.Controls.Add(this.delinquentLoansLink);
            this.flowLayoutPanel1.Controls.Add(this.disbursementsLink);
            resources.ApplyResources(this.flowLayoutPanel1, "flowLayoutPanel1");
            this.flowLayoutPanel1.Name = "flowLayoutPanel1";
            // 
            // operateLabel
            // 
            resources.ApplyResources(this.operateLabel, "operateLabel");
            this.operateLabel.ForeColor = System.Drawing.Color.White;
            this.operateLabel.Name = "operateLabel";
            // 
            // searchClientLabel
            // 
            this.searchClientLabel.ActiveLinkColor = System.Drawing.Color.FromArgb(((int)(((byte)(98)))), ((int)(((byte)(192)))), ((int)(((byte)(255)))));
            resources.ApplyResources(this.searchClientLabel, "searchClientLabel");
            this.searchClientLabel.LinkColor = System.Drawing.Color.FromArgb(((int)(((byte)(28)))), ((int)(((byte)(151)))), ((int)(((byte)(234)))));
            this.searchClientLabel.Name = "searchClientLabel";
            this.searchClientLabel.TabStop = true;
            this.searchClientLabel.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.OnSearchClientClick);
            // 
            // searchContractLabel
            // 
            this.searchContractLabel.ActiveLinkColor = System.Drawing.Color.FromArgb(((int)(((byte)(98)))), ((int)(((byte)(192)))), ((int)(((byte)(255)))));
            resources.ApplyResources(this.searchContractLabel, "searchContractLabel");
            this.searchContractLabel.LinkColor = System.Drawing.Color.FromArgb(((int)(((byte)(28)))), ((int)(((byte)(151)))), ((int)(((byte)(234)))));
            this.searchContractLabel.Name = "searchContractLabel";
            this.searchContractLabel.TabStop = true;
            this.searchContractLabel.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.OnSearchContractClick);
            // 
            // newIndividualClientLinkLabel
            // 
            this.newIndividualClientLinkLabel.ActiveLinkColor = System.Drawing.Color.FromArgb(((int)(((byte)(98)))), ((int)(((byte)(192)))), ((int)(((byte)(255)))));
            resources.ApplyResources(this.newIndividualClientLinkLabel, "newIndividualClientLinkLabel");
            this.newIndividualClientLinkLabel.LinkColor = System.Drawing.Color.FromArgb(((int)(((byte)(28)))), ((int)(((byte)(151)))), ((int)(((byte)(234)))));
            this.newIndividualClientLinkLabel.Name = "newIndividualClientLinkLabel";
            this.newIndividualClientLinkLabel.TabStop = true;
            this.newIndividualClientLinkLabel.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.OnNewIndividualClientLinkLabelClick);
            // 
            // newSolidarityGroupLinkLabel
            // 
            this.newSolidarityGroupLinkLabel.ActiveLinkColor = System.Drawing.Color.FromArgb(((int)(((byte)(98)))), ((int)(((byte)(192)))), ((int)(((byte)(255)))));
            resources.ApplyResources(this.newSolidarityGroupLinkLabel, "newSolidarityGroupLinkLabel");
            this.newSolidarityGroupLinkLabel.LinkColor = System.Drawing.Color.FromArgb(((int)(((byte)(28)))), ((int)(((byte)(151)))), ((int)(((byte)(234)))));
            this.newSolidarityGroupLinkLabel.Name = "newSolidarityGroupLinkLabel";
            this.newSolidarityGroupLinkLabel.TabStop = true;
            this.newSolidarityGroupLinkLabel.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.OnNewSolidarityGroupLinkLabelLinkClick);
            // 
            // newNonSolidairtyGroupLinkLabel
            // 
            this.newNonSolidairtyGroupLinkLabel.ActiveLinkColor = System.Drawing.Color.FromArgb(((int)(((byte)(98)))), ((int)(((byte)(192)))), ((int)(((byte)(255)))));
            resources.ApplyResources(this.newNonSolidairtyGroupLinkLabel, "newNonSolidairtyGroupLinkLabel");
            this.newNonSolidairtyGroupLinkLabel.LinkColor = System.Drawing.Color.FromArgb(((int)(((byte)(28)))), ((int)(((byte)(151)))), ((int)(((byte)(234)))));
            this.newNonSolidairtyGroupLinkLabel.Name = "newNonSolidairtyGroupLinkLabel";
            this.newNonSolidairtyGroupLinkLabel.TabStop = true;
            this.newNonSolidairtyGroupLinkLabel.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.OnNewNonSolidairtyGroupLinkLabelLinkClick);
            // 
            // newCorporateClientLinkLabel
            // 
            this.newCorporateClientLinkLabel.ActiveLinkColor = System.Drawing.Color.FromArgb(((int)(((byte)(98)))), ((int)(((byte)(192)))), ((int)(((byte)(255)))));
            resources.ApplyResources(this.newCorporateClientLinkLabel, "newCorporateClientLinkLabel");
            this.newCorporateClientLinkLabel.LinkColor = System.Drawing.Color.FromArgb(((int)(((byte)(28)))), ((int)(((byte)(151)))), ((int)(((byte)(234)))));
            this.newCorporateClientLinkLabel.Name = "newCorporateClientLinkLabel";
            this.newCorporateClientLinkLabel.TabStop = true;
            this.newCorporateClientLinkLabel.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.OnCorporateClientLinkLabelLinkClick);
            // 
            // configureLabel
            // 
            resources.ApplyResources(this.configureLabel, "configureLabel");
            this.configureLabel.ForeColor = System.Drawing.Color.White;
            this.configureLabel.Name = "configureLabel";
            // 
            // configureLoanProductsLabel
            // 
            this.configureLoanProductsLabel.ActiveLinkColor = System.Drawing.Color.FromArgb(((int)(((byte)(98)))), ((int)(((byte)(192)))), ((int)(((byte)(255)))));
            resources.ApplyResources(this.configureLoanProductsLabel, "configureLoanProductsLabel");
            this.configureLoanProductsLabel.LinkColor = System.Drawing.Color.FromArgb(((int)(((byte)(28)))), ((int)(((byte)(151)))), ((int)(((byte)(234)))));
            this.configureLoanProductsLabel.Name = "configureLoanProductsLabel";
            this.configureLoanProductsLabel.TabStop = true;
            this.configureLoanProductsLabel.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.OnConfigureLoanProductsClick);
            // 
            // configureSavingsProductsLabel
            // 
            this.configureSavingsProductsLabel.ActiveLinkColor = System.Drawing.Color.FromArgb(((int)(((byte)(98)))), ((int)(((byte)(192)))), ((int)(((byte)(255)))));
            resources.ApplyResources(this.configureSavingsProductsLabel, "configureSavingsProductsLabel");
            this.configureSavingsProductsLabel.LinkColor = System.Drawing.Color.FromArgb(((int)(((byte)(28)))), ((int)(((byte)(151)))), ((int)(((byte)(234)))));
            this.configureSavingsProductsLabel.Name = "configureSavingsProductsLabel";
            this.configureSavingsProductsLabel.TabStop = true;
            this.configureSavingsProductsLabel.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.OnConfigureSavingsProductsClick);
            // 
            // configureCollateralProducts
            // 
            this.configureCollateralProducts.ActiveLinkColor = System.Drawing.Color.FromArgb(((int)(((byte)(98)))), ((int)(((byte)(192)))), ((int)(((byte)(255)))));
            resources.ApplyResources(this.configureCollateralProducts, "configureCollateralProducts");
            this.configureCollateralProducts.LinkColor = System.Drawing.Color.FromArgb(((int)(((byte)(28)))), ((int)(((byte)(151)))), ((int)(((byte)(234)))));
            this.configureCollateralProducts.Name = "configureCollateralProducts";
            this.configureCollateralProducts.TabStop = true;
            this.configureCollateralProducts.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.OnConfigureCollateralProducts);
            // 
            // configureSettingsLabel
            // 
            this.configureSettingsLabel.ActiveLinkColor = System.Drawing.Color.FromArgb(((int)(((byte)(98)))), ((int)(((byte)(192)))), ((int)(((byte)(255)))));
            resources.ApplyResources(this.configureSettingsLabel, "configureSettingsLabel");
            this.configureSettingsLabel.LinkColor = System.Drawing.Color.FromArgb(((int)(((byte)(28)))), ((int)(((byte)(151)))), ((int)(((byte)(234)))));
            this.configureSettingsLabel.Name = "configureSettingsLabel";
            this.configureSettingsLabel.TabStop = true;
            this.configureSettingsLabel.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.OnConfigureSettingsClick);
            // 
            // configurePermissionsLabel
            // 
            this.configurePermissionsLabel.ActiveLinkColor = System.Drawing.Color.FromArgb(((int)(((byte)(98)))), ((int)(((byte)(192)))), ((int)(((byte)(255)))));
            resources.ApplyResources(this.configurePermissionsLabel, "configurePermissionsLabel");
            this.configurePermissionsLabel.LinkColor = System.Drawing.Color.FromArgb(((int)(((byte)(28)))), ((int)(((byte)(151)))), ((int)(((byte)(234)))));
            this.configurePermissionsLabel.Name = "configurePermissionsLabel";
            this.configurePermissionsLabel.TabStop = true;
            this.configurePermissionsLabel.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.OnConfigurePermissionsClick);
            // 
            // controlLabel
            // 
            resources.ApplyResources(this.controlLabel, "controlLabel");
            this.controlLabel.ForeColor = System.Drawing.Color.White;
            this.controlLabel.Name = "controlLabel";
            // 
            // auditTrailLabel
            // 
            this.auditTrailLabel.ActiveLinkColor = System.Drawing.Color.FromArgb(((int)(((byte)(98)))), ((int)(((byte)(192)))), ((int)(((byte)(255)))));
            resources.ApplyResources(this.auditTrailLabel, "auditTrailLabel");
            this.auditTrailLabel.LinkColor = System.Drawing.Color.FromArgb(((int)(((byte)(28)))), ((int)(((byte)(151)))), ((int)(((byte)(234)))));
            this.auditTrailLabel.Name = "auditTrailLabel";
            this.auditTrailLabel.TabStop = true;
            this.auditTrailLabel.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.OnAuditTrailClick);
            // 
            // activeLoansLink
            // 
            this.activeLoansLink.ActiveLinkColor = System.Drawing.Color.FromArgb(((int)(((byte)(98)))), ((int)(((byte)(192)))), ((int)(((byte)(255)))));
            resources.ApplyResources(this.activeLoansLink, "activeLoansLink");
            this.activeLoansLink.LinkColor = System.Drawing.Color.FromArgb(((int)(((byte)(28)))), ((int)(((byte)(151)))), ((int)(((byte)(234)))));
            this.activeLoansLink.Name = "activeLoansLink";
            this.activeLoansLink.TabStop = true;
            this.activeLoansLink.Tag = "Active_Loans.zip";
            this.activeLoansLink.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.OnReportsClick);
            // 
            // parAnalysisLink
            // 
            this.parAnalysisLink.ActiveLinkColor = System.Drawing.Color.FromArgb(((int)(((byte)(98)))), ((int)(((byte)(192)))), ((int)(((byte)(255)))));
            resources.ApplyResources(this.parAnalysisLink, "parAnalysisLink");
            this.parAnalysisLink.LinkColor = System.Drawing.Color.FromArgb(((int)(((byte)(28)))), ((int)(((byte)(151)))), ((int)(((byte)(234)))));
            this.parAnalysisLink.Name = "parAnalysisLink";
            this.parAnalysisLink.TabStop = true;
            this.parAnalysisLink.Tag = "PAR_Analysis.zip";
            this.parAnalysisLink.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.OnReportsClick);
            // 
            // delinquentLoansLink
            // 
            this.delinquentLoansLink.ActiveLinkColor = System.Drawing.Color.FromArgb(((int)(((byte)(98)))), ((int)(((byte)(192)))), ((int)(((byte)(255)))));
            resources.ApplyResources(this.delinquentLoansLink, "delinquentLoansLink");
            this.delinquentLoansLink.LinkColor = System.Drawing.Color.FromArgb(((int)(((byte)(28)))), ((int)(((byte)(151)))), ((int)(((byte)(234)))));
            this.delinquentLoansLink.Name = "delinquentLoansLink";
            this.delinquentLoansLink.TabStop = true;
            this.delinquentLoansLink.Tag = "Delinquent_Loans.zip";
            this.delinquentLoansLink.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.OnReportsClick);
            // 
            // disbursementsLink
            // 
            this.disbursementsLink.ActiveLinkColor = System.Drawing.Color.FromArgb(((int)(((byte)(98)))), ((int)(((byte)(192)))), ((int)(((byte)(255)))));
            resources.ApplyResources(this.disbursementsLink, "disbursementsLink");
            this.disbursementsLink.LinkColor = System.Drawing.Color.FromArgb(((int)(((byte)(28)))), ((int)(((byte)(151)))), ((int)(((byte)(234)))));
            this.disbursementsLink.Name = "disbursementsLink";
            this.disbursementsLink.TabStop = true;
            this.disbursementsLink.Tag = "Disbursments.zip";
            this.disbursementsLink.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.OnReportsClick);
            // 
            // DashboardForm
            // 
            resources.ApplyResources(this, "$this");
            this.ControlBox = false;
            this.Controls.Add(this.generalInfoPanel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Name = "DashboardForm";
            this.ShowInTaskbar = false;
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.Load += new System.EventHandler(this.OnLoad);
            this.generalInfoPanel.ResumeLayout(false);
            this.infoPanel.ResumeLayout(false);
            this.infoPanel.PerformLayout();
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel3.ResumeLayout(false);
            this.riskTableLayoutPanel.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.parListView)).EndInit();
            this.tableLayoutPanel2.ResumeLayout(false);
            this.topBarPanel.ResumeLayout(false);
            this.topBarPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.smallLogoPictureBox)).EndInit();
            this.quickLinksPanel.ResumeLayout(false);
            this.flowLayoutPanel1.ResumeLayout(false);
            this.flowLayoutPanel1.PerformLayout();
            this.ResumeLayout(false);

        }
        #endregion

        private System.Windows.Forms.Panel generalInfoPanel;
        private System.Windows.Forms.Panel quickLinksPanel;
        private System.Windows.Forms.Panel infoPanel;
        private System.Windows.Forms.Label operateLabel;
        private System.Windows.Forms.LinkLabel newIndividualClientLinkLabel;
        private System.Windows.Forms.LinkLabel newNonSolidairtyGroupLinkLabel;
        private System.Windows.Forms.LinkLabel newSolidarityGroupLinkLabel;
        private System.Windows.Forms.LinkLabel newCorporateClientLinkLabel;
        private System.Windows.Forms.TableLayoutPanel riskTableLayoutPanel;
        private BrightIdeasSoftware.ObjectListView parListView;
        private BrightIdeasSoftware.OLVColumn parNameColumn;
        private BrightIdeasSoftware.OLVColumn parAmountColumn;
        private BrightIdeasSoftware.OLVColumn parQuantityColumn;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel2;
        private System.Windows.Forms.Panel portfolioPanel;
        private System.Windows.Forms.Panel parPanel;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel3;
        private System.Windows.Forms.Panel disbursementsPanel;
        private System.Windows.Forms.Panel olbTrendPanel;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
        private System.Windows.Forms.LinkLabel searchClientLabel;
        private System.Windows.Forms.LinkLabel searchContractLabel;
        private System.Windows.Forms.Label configureLabel;
        private System.Windows.Forms.LinkLabel configureLoanProductsLabel;
        private System.Windows.Forms.LinkLabel configureSavingsProductsLabel;
        private System.Windows.Forms.LinkLabel configureCollateralProducts;
        private System.Windows.Forms.LinkLabel configureSettingsLabel;
        private System.Windows.Forms.LinkLabel configurePermissionsLabel;
        private System.Windows.Forms.Label controlLabel;
        private System.Windows.Forms.LinkLabel auditTrailLabel;
        private System.Windows.Forms.LinkLabel activeLoansLink;
        private System.Windows.Forms.TableLayoutPanel topBarPanel;
        private System.Windows.Forms.LinkLabel refreshLinkLabel;
        private System.Windows.Forms.PictureBox smallLogoPictureBox;
        private System.Windows.Forms.Label aboutLabel;
        private System.Windows.Forms.LinkLabel parAnalysisLink;
        private System.Windows.Forms.LinkLabel delinquentLoansLink;
        private System.Windows.Forms.LinkLabel disbursementsLink;
    }
}
