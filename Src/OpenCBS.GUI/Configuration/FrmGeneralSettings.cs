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
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using OpenCBS.CoreDomain.Online;
using OpenCBS.Enums;
using OpenCBS.GUI.UserControl;
using OpenCBS.Services.Accounting;
using OpenCBS.ExceptionsHandler;
using OpenCBS.Services;
using OpenCBS.Shared;
using OpenCBS.MultiLanguageRessources;
using OpenCBS.CoreDomain.Accounting;
using System.Linq;

namespace OpenCBS.GUI.Configuration
{
    /// <summary>
    /// Summary description for GeneralSettings.
    /// </summary>
    public class FrmGeneralSettings : Form
    {
        #region Controls

        private System.Windows.Forms.Button buttonCancel;
        private ProvisioningRate pR;
        private LoanScaleRate lR;
        private DictionaryEntry entry;
        private IContainer components;
        private GroupBox groupBox1;
        private SplitContainer splitContainer1;
        private System.Windows.Forms.Button butImport;
        private System.Windows.Forms.Button butExport;
        private TabControl tabControlGeneralSettings;
        private TabPage tabPage1;
        private ListView lvGeneralParameters;
        private ColumnHeader columnHeader5;
        private ColumnHeader columnHeader6;
        private GroupBox groupBox3;
        private ComboBox cbxValue;
        private CheckedListBox clbxPendingSavings;
        private TextBox textBoxGeneralParameterValue;
        private TextBox textBoxGeneralParameterName;
        private Label label9;
        private Label label10;
        private System.Windows.Forms.Button buttonUpdate;
        private GroupBox groupBoxValue;
        private ComboBox comboBoxSavings;
        private RadioButton radioButtonNo;
        private RadioButton radioButtonYes;
        private TabPage tabPageProvioningRules;
        private ListView listViewProvisioningRules;
        private ColumnHeader columnHeader1;
        private ColumnHeader columnHeader2;
        private ColumnHeader columnHeader3;
        private ColumnHeader columnHeader4;
        private GroupBox groupBoxAddUser;
        private Label label4;
        private System.Windows.Forms.Button buttonOK;
        private Label label2;
        private Label label1;
        private TextBox textBoxProvisioningOlb;
        private TextBox textBoxNbOfDaysMax;
        private TextBox textBoxNbOfDaysMin;
        private Label labelPassword;
        private Label labelUsername;
        private TabPage tabPage2;
        private SplitContainer splitContainer2;
        private SplitContainer splitContainer3;
        private ListView listViewPublicHolidays;
        private ColumnHeader columnHeader7;
        private ColumnHeader columnHeader8;
        private GroupBox groupBox5;
        private System.Windows.Forms.Button buttonPublicHolidayDelete;
        private GroupBox groupBox4;
        private DateTimePicker dateTimePickerPublicHoliday;
        private TextBox textBoxPublicHolidayDescription;
        private Label label5;
        private Label label6;
        private System.Windows.Forms.Button buttonPublicHolidaysSave;
        private TabPage tabPageLoanScale;
        private GroupBox groupBox2;
        private System.Windows.Forms.Button buttonDeleteScale;
        private System.Windows.Forms.Button buttonSaveScale;
        private TextBox textBoxMaxScale;
        private TextBox textBoxMinScale;
        private Label labelMaxExter;
        private Label labelMinExter;
        private Label labelScaleMax;
        private Label labelScaleMin;
        private ListView listViewLoanScale;
        private ColumnHeader columnHeader9;
        private ColumnHeader columnHeader10;
        private ColumnHeader columnHeader11;
        private ColumnHeader columnHeader12;
        private ColumnHeader columnHeader13;
        private TextBox textBoxProvisioningPenalty;
        private Label label7;
        private TextBox textBoxProvisioningInterest;
        private Label label3;
        private OpenFileDialog openFileDialog;
        #endregion
        public FrmGeneralSettings()
        {
            pR = new ProvisioningRate();
            lR = new LoanScaleRate();
            InitializeComponent();
            InitializeListViewProvisioningTables();
            InitializeListViewGeneralParameters();
            InitializeListViewPublicHolidays();
            InitializeListViewLoanScaleTables();
            InitializeListBoxPendingSavings();
        }

        private void InitializeListViewPublicHolidays()
        {
            listViewPublicHolidays.Items.Clear();

            Dictionary<DateTime, string> table = ServicesProvider.GetInstance().GetNonWorkingDate().PublicHolidays;

            foreach (KeyValuePair<DateTime, string> entry in table)
            {
                ListViewItem listViewItem = new ListViewItem((entry.Key).ToShortDateString());
                listViewItem.SubItems.Add(entry.Value);
                listViewItem.Tag = entry;
                listViewPublicHolidays.Items.Add(listViewItem);
            }
        }

        private void InitializeListViewGeneralParameters()
        {
            lvGeneralParameters.Items.Clear();
            Hashtable settings = ServicesProvider.GetInstance().GetGeneralSettings().DbParamList;
            ArrayList keys = new ArrayList();
            keys.AddRange(settings.Keys);
            keys.Sort();
            foreach (object key in keys)
            {
                if (key.ToString() == OGeneralSettings.CONTRACT_CODE_TEMPLATE)
                    continue;
                DictionaryEntry val = new DictionaryEntry(key, settings[key]);
                ListViewItem listViewItem = new ListViewItem(val.Key.ToString());
                if (val.Value != null)
                {
                    if (val.Key.ToString() == OGeneralSettings.CITYMANDATORY ||
                        val.Key.ToString() == OGeneralSettings.PAYFIRSTINSTALLMENTREALVALUE ||
                        val.Key.ToString() == OGeneralSettings.CITYOPENVALUE ||
                        val.Key.ToString() == OGeneralSettings.ALLOWSMULTIPLELOANS ||
                        val.Key.ToString() == OGeneralSettings.ALLOWSMULTIPLEGROUPS ||
                        val.Key.ToString() == OGeneralSettings.OLBBEFOREREPAYMENT ||
                        val.Key.ToString() == OGeneralSettings.CALCULATIONLATEFEESDURINGPUBLICHOLIDAYS ||
                        val.Key.ToString() == OGeneralSettings.DONOTSKIPWEEKENDSININSTALLMENTSDATE ||
                        val.Key.ToString() == OGeneralSettings.USEPROJECTS ||
                        val.Key.ToString() == OGeneralSettings.ENFORCE_ID_PATTERN||
                        val.Key.ToString() == OGeneralSettings.ID_WILD_CHAR_CHECK ||
                        val.Key.ToString() == OGeneralSettings.INCREMENTALDURINGDAYOFF ||
                        val.Key.ToString() == OGeneralSettings.INTERESTS_ALSO_CREDITED_IN_FL ||
                        val.Key.ToString() == OGeneralSettings.USE_TELLER_MANAGEMENT ||
                        val.Key.ToString() == OGeneralSettings.CONSOLIDATION_MODE ||
                        val.Key.ToString() == OGeneralSettings.AUTOMATIC_ID ||
                        val.Key.ToString() == OGeneralSettings.STOP_WRITEOFF_PENALTY ||
                        val.Key.ToString() == OGeneralSettings.MODIFY_ENTRY_FEE)
                    {
                        listViewItem.SubItems.Add(val.Value.ToString().Trim() == "1" ? "True" : "False");
                    }
                    else if(val.Key.ToString() == OGeneralSettings.ACCOUNTINGPROCESS)
                    {
                        listViewItem.SubItems.Add(val.Value.ToString().Trim() == "1" ? "Cash" : "Accrual");
                    }
                    else if (val.Key.ToString() == OGeneralSettings.REAL_EXPECTED_AMOUNT)
                    {
                        listViewItem.SubItems.Add(val.Value.ToString().Trim() == "1" ? "Accrued interests" : "Accrued interests + principal");
                    }
                    else
                        listViewItem.SubItems.Add(val.Value.ToString());
                }
                else
                    listViewItem.SubItems.Add("-");

                listViewItem.Tag = val;
                lvGeneralParameters.Items.Add(listViewItem);
            }
        }

        private void InitializeListBoxPendingSavings()
        {
            clbxPendingSavings.Items.Clear();
            foreach (var item in Enum.GetNames(typeof(OSavingsMethods)))
                clbxPendingSavings.Items.Add(MultiLanguageStrings.GetString(Ressource.CreditContractRepayForm, item + ".Text"));
        }

        private void InitializeListViewProvisioningTables()
        {
            listViewProvisioningRules.Items.Clear();
            var listViewItemRes = new ListViewItem();

            List<ProvisioningRate> pr = CoreDomainProvider.GetInstance().GetProvisioningTable().ProvisioningRates;
            
            foreach (ProvisioningRate provisioningRate in CoreDomainProvider.GetInstance().GetProvisioningTable().ProvisioningRates)
            {
                var listViewItem = new ListViewItem(provisioningRate.Number.ToString());

                if (provisioningRate.NbOfDaysMin == -1)
                {
                    listViewItem.Tag = provisioningRate;
                    listViewItem.SubItems.Add("rescheduled");
                    listViewItem.SubItems.Add("-");
                    
                    //listViewItem.SubItems.Add((Math.Round(provisioningRate.Rate, 2) * 100).ToString());
                    listViewItem.SubItems.Add((provisioningRate.ProvisioningValue * 100).ToString());
                    listViewItem.SubItems.Add((provisioningRate.ProvisioningInterest * 100).ToString());
                    listViewItem.SubItems.Add((provisioningRate.ProvisioningPenalty * 100).ToString());
                    listViewItemRes = listViewItem;
                }
                else
                {
                    listViewItem.Tag = provisioningRate;
                    listViewItem.SubItems.Add(provisioningRate.NbOfDaysMin.ToString());
                    listViewItem.SubItems.Add(provisioningRate.NbOfDaysMax.ToString());
                    //listViewItem.SubItems.Add((Math.Round(provisioningRate.Rate, 2)*100).ToString());
                    listViewItem.SubItems.Add((provisioningRate.ProvisioningValue * 100).ToString());
                    listViewItem.SubItems.Add((provisioningRate.ProvisioningInterest * 100).ToString());
                    listViewItem.SubItems.Add((provisioningRate.ProvisioningPenalty * 100).ToString());
                    listViewProvisioningRules.Items.Add(listViewItem);
                }
            }

            // adding restructured item
            listViewProvisioningRules.Items.Add(listViewItemRes);
        }

        private void InitializeListViewLoanScaleTables()
        {
            listViewLoanScale.Items.Clear();
            
            foreach (LoanScaleRate lR in CoreDomainProvider.GetInstance().GetLoanScaleTable().LoanScaleRates)
            {
                ListViewItem listViewItem = new ListViewItem(lR.Number.ToString());
                
                listViewItem.Tag = lR;
                listViewItem.SubItems.Add(lR.ScaleMin.ToString());
                listViewItem.SubItems.Add(lR.ScaleMax.ToString());
                listViewLoanScale.Items.Add(listViewItem);
            }

        }
        /// <summary>
        /// Clean up any resources being used.
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

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FrmGeneralSettings));
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.tabControlGeneralSettings = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.lvGeneralParameters = new System.Windows.Forms.ListView();
            this.columnHeader5 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader6 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.textBoxGeneralParameterName = new System.Windows.Forms.TextBox();
            this.label9 = new System.Windows.Forms.Label();
            this.label10 = new System.Windows.Forms.Label();
            this.buttonUpdate = new System.Windows.Forms.Button();
            this.groupBoxValue = new System.Windows.Forms.GroupBox();
            this.radioButtonNo = new System.Windows.Forms.RadioButton();
            this.radioButtonYes = new System.Windows.Forms.RadioButton();
            this.comboBoxSavings = new System.Windows.Forms.ComboBox();
            this.clbxPendingSavings = new System.Windows.Forms.CheckedListBox();
            this.cbxValue = new System.Windows.Forms.ComboBox();
            this.textBoxGeneralParameterValue = new System.Windows.Forms.TextBox();
            this.tabPageProvioningRules = new System.Windows.Forms.TabPage();
            this.listViewProvisioningRules = new System.Windows.Forms.ListView();
            this.columnHeader1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader2 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader3 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader4 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader12 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader13 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.groupBoxAddUser = new System.Windows.Forms.GroupBox();
            this.textBoxProvisioningPenalty = new System.Windows.Forms.TextBox();
            this.label7 = new System.Windows.Forms.Label();
            this.textBoxProvisioningInterest = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.buttonOK = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.textBoxProvisioningOlb = new System.Windows.Forms.TextBox();
            this.textBoxNbOfDaysMax = new System.Windows.Forms.TextBox();
            this.textBoxNbOfDaysMin = new System.Windows.Forms.TextBox();
            this.labelPassword = new System.Windows.Forms.Label();
            this.labelUsername = new System.Windows.Forms.Label();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.splitContainer2 = new System.Windows.Forms.SplitContainer();
            this.splitContainer3 = new System.Windows.Forms.SplitContainer();
            this.listViewPublicHolidays = new System.Windows.Forms.ListView();
            this.columnHeader7 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader8 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.groupBox5 = new System.Windows.Forms.GroupBox();
            this.buttonPublicHolidayDelete = new System.Windows.Forms.Button();
            this.groupBox4 = new System.Windows.Forms.GroupBox();
            this.dateTimePickerPublicHoliday = new System.Windows.Forms.DateTimePicker();
            this.textBoxPublicHolidayDescription = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.buttonPublicHolidaysSave = new System.Windows.Forms.Button();
            this.tabPageLoanScale = new System.Windows.Forms.TabPage();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.buttonDeleteScale = new System.Windows.Forms.Button();
            this.buttonSaveScale = new System.Windows.Forms.Button();
            this.textBoxMaxScale = new System.Windows.Forms.TextBox();
            this.textBoxMinScale = new System.Windows.Forms.TextBox();
            this.labelMaxExter = new System.Windows.Forms.Label();
            this.labelMinExter = new System.Windows.Forms.Label();
            this.labelScaleMax = new System.Windows.Forms.Label();
            this.labelScaleMin = new System.Windows.Forms.Label();
            this.listViewLoanScale = new System.Windows.Forms.ListView();
            this.columnHeader9 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader10 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader11 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.butImport = new System.Windows.Forms.Button();
            this.butExport = new System.Windows.Forms.Button();
            this.buttonCancel = new System.Windows.Forms.Button();
            this.openFileDialog = new System.Windows.Forms.OpenFileDialog();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.tabControlGeneralSettings.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.groupBoxValue.SuspendLayout();
            this.tabPageProvioningRules.SuspendLayout();
            this.groupBoxAddUser.SuspendLayout();
            this.tabPage2.SuspendLayout();
            this.splitContainer2.Panel1.SuspendLayout();
            this.splitContainer2.Panel2.SuspendLayout();
            this.splitContainer2.SuspendLayout();
            this.splitContainer3.Panel1.SuspendLayout();
            this.splitContainer3.Panel2.SuspendLayout();
            this.splitContainer3.SuspendLayout();
            this.groupBox5.SuspendLayout();
            this.groupBox4.SuspendLayout();
            this.tabPageLoanScale.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // splitContainer1
            // 
            resources.ApplyResources(this.splitContainer1, "splitContainer1");
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.tabControlGeneralSettings);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.groupBox1);
            // 
            // tabControlGeneralSettings
            // 
            this.tabControlGeneralSettings.Controls.Add(this.tabPage1);
            this.tabControlGeneralSettings.Controls.Add(this.tabPageProvioningRules);
            this.tabControlGeneralSettings.Controls.Add(this.tabPage2);
            this.tabControlGeneralSettings.Controls.Add(this.tabPageLoanScale);
            resources.ApplyResources(this.tabControlGeneralSettings, "tabControlGeneralSettings");
            this.tabControlGeneralSettings.Multiline = true;
            this.tabControlGeneralSettings.Name = "tabControlGeneralSettings";
            this.tabControlGeneralSettings.SelectedIndex = 0;
            this.tabControlGeneralSettings.SizeMode = System.Windows.Forms.TabSizeMode.Fixed;
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.lvGeneralParameters);
            this.tabPage1.Controls.Add(this.groupBox3);
            resources.ApplyResources(this.tabPage1, "tabPage1");
            this.tabPage1.Name = "tabPage1";
            // 
            // lvGeneralParameters
            // 
            this.lvGeneralParameters.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader5,
            this.columnHeader6});
            resources.ApplyResources(this.lvGeneralParameters, "lvGeneralParameters");
            this.lvGeneralParameters.FullRowSelect = true;
            this.lvGeneralParameters.GridLines = true;
            this.lvGeneralParameters.MultiSelect = false;
            this.lvGeneralParameters.Name = "lvGeneralParameters";
            this.lvGeneralParameters.UseCompatibleStateImageBehavior = false;
            this.lvGeneralParameters.View = System.Windows.Forms.View.Details;
            this.lvGeneralParameters.Click += new System.EventHandler(this.listViewGeneralParameters_Click);
            // 
            // columnHeader5
            // 
            resources.ApplyResources(this.columnHeader5, "columnHeader5");
            // 
            // columnHeader6
            // 
            resources.ApplyResources(this.columnHeader6, "columnHeader6");
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.textBoxGeneralParameterName);
            this.groupBox3.Controls.Add(this.label9);
            this.groupBox3.Controls.Add(this.label10);
            this.groupBox3.Controls.Add(this.buttonUpdate);
            this.groupBox3.Controls.Add(this.groupBoxValue);
            this.groupBox3.Controls.Add(this.clbxPendingSavings);
            this.groupBox3.Controls.Add(this.cbxValue);
            this.groupBox3.Controls.Add(this.textBoxGeneralParameterValue);
            resources.ApplyResources(this.groupBox3, "groupBox3");
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.TabStop = false;
            this.groupBox3.Enter += new System.EventHandler(this.groupBox3_Enter);
            // 
            // textBoxGeneralParameterName
            // 
            resources.ApplyResources(this.textBoxGeneralParameterName, "textBoxGeneralParameterName");
            this.textBoxGeneralParameterName.Name = "textBoxGeneralParameterName";
            // 
            // label9
            // 
            resources.ApplyResources(this.label9, "label9");
            this.label9.BackColor = System.Drawing.Color.Transparent;
            this.label9.Name = "label9";
            // 
            // label10
            // 
            resources.ApplyResources(this.label10, "label10");
            this.label10.BackColor = System.Drawing.Color.Transparent;
            this.label10.Name = "label10";
            // 
            // buttonUpdate
            // 
            resources.ApplyResources(this.buttonUpdate, "buttonUpdate");
            this.buttonUpdate.Name = "buttonUpdate";
            this.buttonUpdate.Click += new System.EventHandler(this.buttonUpdate_Click);
            // 
            // groupBoxValue
            // 
            this.groupBoxValue.Controls.Add(this.radioButtonNo);
            this.groupBoxValue.Controls.Add(this.radioButtonYes);
            this.groupBoxValue.Controls.Add(this.comboBoxSavings);
            resources.ApplyResources(this.groupBoxValue, "groupBoxValue");
            this.groupBoxValue.Name = "groupBoxValue";
            this.groupBoxValue.TabStop = false;
            // 
            // radioButtonNo
            // 
            resources.ApplyResources(this.radioButtonNo, "radioButtonNo");
            this.radioButtonNo.Name = "radioButtonNo";
            this.radioButtonNo.CheckedChanged += new System.EventHandler(this.radioButtonNo_CheckedChanged);
            // 
            // radioButtonYes
            // 
            this.radioButtonYes.Checked = true;
            resources.ApplyResources(this.radioButtonYes, "radioButtonYes");
            this.radioButtonYes.Name = "radioButtonYes";
            this.radioButtonYes.TabStop = true;
            // 
            // comboBoxSavings
            // 
            this.comboBoxSavings.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxSavings.FormattingEnabled = true;
            this.comboBoxSavings.Items.AddRange(new object[] {
            resources.GetString("comboBoxSavings.Items"),
            resources.GetString("comboBoxSavings.Items1")});
            resources.ApplyResources(this.comboBoxSavings, "comboBoxSavings");
            this.comboBoxSavings.Name = "comboBoxSavings";
            this.comboBoxSavings.SelectionChangeCommitted += new System.EventHandler(this.comboBoxSavings_SelectionChangeCommitted);
            // 
            // clbxPendingSavings
            // 
            this.clbxPendingSavings.CheckOnClick = true;
            this.clbxPendingSavings.FormattingEnabled = true;
            this.clbxPendingSavings.Items.AddRange(new object[] {
            resources.GetString("clbxPendingSavings.Items")});
            resources.ApplyResources(this.clbxPendingSavings, "clbxPendingSavings");
            this.clbxPendingSavings.Name = "clbxPendingSavings";
            this.clbxPendingSavings.Leave += new System.EventHandler(this.checkedListBoxPendingSavings_Leave);
            // 
            // cbxValue
            // 
            this.cbxValue.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbxValue.FormattingEnabled = true;
            this.cbxValue.Items.AddRange(new object[] {
            resources.GetString("cbxValue.Items"),
            resources.GetString("cbxValue.Items1")});
            resources.ApplyResources(this.cbxValue, "cbxValue");
            this.cbxValue.Name = "cbxValue";
            this.cbxValue.SelectionChangeCommitted += new System.EventHandler(this.comboBoxValue_SelectionChangeCommitted);
            // 
            // textBoxGeneralParameterValue
            // 
            resources.ApplyResources(this.textBoxGeneralParameterValue, "textBoxGeneralParameterValue");
            this.textBoxGeneralParameterValue.Name = "textBoxGeneralParameterValue";
            this.textBoxGeneralParameterValue.TextChanged += new System.EventHandler(this.textBoxGeneralParameterValue_TextChanged);
            // 
            // tabPageProvioningRules
            // 
            this.tabPageProvioningRules.Controls.Add(this.listViewProvisioningRules);
            this.tabPageProvioningRules.Controls.Add(this.groupBoxAddUser);
            resources.ApplyResources(this.tabPageProvioningRules, "tabPageProvioningRules");
            this.tabPageProvioningRules.Name = "tabPageProvioningRules";
            // 
            // listViewProvisioningRules
            // 
            this.listViewProvisioningRules.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1,
            this.columnHeader2,
            this.columnHeader3,
            this.columnHeader4,
            this.columnHeader12,
            this.columnHeader13});
            resources.ApplyResources(this.listViewProvisioningRules, "listViewProvisioningRules");
            this.listViewProvisioningRules.FullRowSelect = true;
            this.listViewProvisioningRules.GridLines = true;
            this.listViewProvisioningRules.Name = "listViewProvisioningRules";
            this.listViewProvisioningRules.UseCompatibleStateImageBehavior = false;
            this.listViewProvisioningRules.View = System.Windows.Forms.View.Details;
            this.listViewProvisioningRules.Click += new System.EventHandler(this.listViewProvisioningRules_Click);
            // 
            // columnHeader1
            // 
            resources.ApplyResources(this.columnHeader1, "columnHeader1");
            // 
            // columnHeader2
            // 
            resources.ApplyResources(this.columnHeader2, "columnHeader2");
            // 
            // columnHeader3
            // 
            resources.ApplyResources(this.columnHeader3, "columnHeader3");
            // 
            // columnHeader4
            // 
            resources.ApplyResources(this.columnHeader4, "columnHeader4");
            // 
            // columnHeader12
            // 
            resources.ApplyResources(this.columnHeader12, "columnHeader12");
            // 
            // columnHeader13
            // 
            resources.ApplyResources(this.columnHeader13, "columnHeader13");
            // 
            // groupBoxAddUser
            // 
            this.groupBoxAddUser.Controls.Add(this.textBoxProvisioningPenalty);
            this.groupBoxAddUser.Controls.Add(this.label7);
            this.groupBoxAddUser.Controls.Add(this.textBoxProvisioningInterest);
            this.groupBoxAddUser.Controls.Add(this.label3);
            this.groupBoxAddUser.Controls.Add(this.label4);
            this.groupBoxAddUser.Controls.Add(this.buttonOK);
            this.groupBoxAddUser.Controls.Add(this.label2);
            this.groupBoxAddUser.Controls.Add(this.label1);
            this.groupBoxAddUser.Controls.Add(this.textBoxProvisioningOlb);
            this.groupBoxAddUser.Controls.Add(this.textBoxNbOfDaysMax);
            this.groupBoxAddUser.Controls.Add(this.textBoxNbOfDaysMin);
            this.groupBoxAddUser.Controls.Add(this.labelPassword);
            this.groupBoxAddUser.Controls.Add(this.labelUsername);
            resources.ApplyResources(this.groupBoxAddUser, "groupBoxAddUser");
            this.groupBoxAddUser.Name = "groupBoxAddUser";
            this.groupBoxAddUser.TabStop = false;
            // 
            // textBoxProvisioningPenalty
            // 
            resources.ApplyResources(this.textBoxProvisioningPenalty, "textBoxProvisioningPenalty");
            this.textBoxProvisioningPenalty.Name = "textBoxProvisioningPenalty";
            this.textBoxProvisioningPenalty.TextChanged += new System.EventHandler(this.textBoxProvisioningPenalty_TextChanged);
            this.textBoxProvisioningPenalty.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.textBoxProvisioningInterest_KeyPress);
            // 
            // label7
            // 
            resources.ApplyResources(this.label7, "label7");
            this.label7.BackColor = System.Drawing.Color.Transparent;
            this.label7.Name = "label7";
            // 
            // textBoxProvisioningInterest
            // 
            resources.ApplyResources(this.textBoxProvisioningInterest, "textBoxProvisioningInterest");
            this.textBoxProvisioningInterest.Name = "textBoxProvisioningInterest";
            this.textBoxProvisioningInterest.TextChanged += new System.EventHandler(this.textBoxProvisioningInterest_TextChanged);
            this.textBoxProvisioningInterest.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.textBoxProvisioningInterest_KeyPress);
            // 
            // label3
            // 
            resources.ApplyResources(this.label3, "label3");
            this.label3.BackColor = System.Drawing.Color.Transparent;
            this.label3.Name = "label3";
            // 
            // label4
            // 
            resources.ApplyResources(this.label4, "label4");
            this.label4.BackColor = System.Drawing.Color.Transparent;
            this.label4.Name = "label4";
            // 
            // buttonOK
            // 
            resources.ApplyResources(this.buttonOK, "buttonOK");
            this.buttonOK.Name = "buttonOK";
            this.buttonOK.Click += new System.EventHandler(this.buttonOK_Click);
            // 
            // label2
            // 
            resources.ApplyResources(this.label2, "label2");
            this.label2.BackColor = System.Drawing.Color.Transparent;
            this.label2.Name = "label2";
            // 
            // label1
            // 
            resources.ApplyResources(this.label1, "label1");
            this.label1.BackColor = System.Drawing.Color.Transparent;
            this.label1.Name = "label1";
            // 
            // textBoxProvisioningOlb
            // 
            resources.ApplyResources(this.textBoxProvisioningOlb, "textBoxProvisioningOlb");
            this.textBoxProvisioningOlb.Name = "textBoxProvisioningOlb";
            this.textBoxProvisioningOlb.TextChanged += new System.EventHandler(this.textBoxProvisioning_TextChanged);
            this.textBoxProvisioningOlb.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.textBoxProvisioning_KeyPress);
            // 
            // textBoxNbOfDaysMax
            // 
            resources.ApplyResources(this.textBoxNbOfDaysMax, "textBoxNbOfDaysMax");
            this.textBoxNbOfDaysMax.Name = "textBoxNbOfDaysMax";
            this.textBoxNbOfDaysMax.TextChanged += new System.EventHandler(this.textBoxNbOfDaysMax_TextChanged);
            this.textBoxNbOfDaysMax.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.textBoxNbOfDaysMax_KeyPress);
            // 
            // textBoxNbOfDaysMin
            // 
            resources.ApplyResources(this.textBoxNbOfDaysMin, "textBoxNbOfDaysMin");
            this.textBoxNbOfDaysMin.Name = "textBoxNbOfDaysMin";
            this.textBoxNbOfDaysMin.TextChanged += new System.EventHandler(this.textBoxNbOfDaysMin_TextChanged);
            this.textBoxNbOfDaysMin.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.textBoxNbOfDaysMin_KeyPress);
            // 
            // labelPassword
            // 
            resources.ApplyResources(this.labelPassword, "labelPassword");
            this.labelPassword.BackColor = System.Drawing.Color.Transparent;
            this.labelPassword.Name = "labelPassword";
            // 
            // labelUsername
            // 
            resources.ApplyResources(this.labelUsername, "labelUsername");
            this.labelUsername.BackColor = System.Drawing.Color.Transparent;
            this.labelUsername.Name = "labelUsername";
            // 
            // tabPage2
            // 
            this.tabPage2.Controls.Add(this.splitContainer2);
            resources.ApplyResources(this.tabPage2, "tabPage2");
            this.tabPage2.Name = "tabPage2";
            // 
            // splitContainer2
            // 
            resources.ApplyResources(this.splitContainer2, "splitContainer2");
            this.splitContainer2.FixedPanel = System.Windows.Forms.FixedPanel.Panel2;
            this.splitContainer2.Name = "splitContainer2";
            // 
            // splitContainer2.Panel1
            // 
            this.splitContainer2.Panel1.Controls.Add(this.splitContainer3);
            // 
            // splitContainer2.Panel2
            // 
            this.splitContainer2.Panel2.Controls.Add(this.groupBox4);
            // 
            // splitContainer3
            // 
            resources.ApplyResources(this.splitContainer3, "splitContainer3");
            this.splitContainer3.FixedPanel = System.Windows.Forms.FixedPanel.Panel2;
            this.splitContainer3.Name = "splitContainer3";
            // 
            // splitContainer3.Panel1
            // 
            this.splitContainer3.Panel1.Controls.Add(this.listViewPublicHolidays);
            // 
            // splitContainer3.Panel2
            // 
            this.splitContainer3.Panel2.Controls.Add(this.groupBox5);
            // 
            // listViewPublicHolidays
            // 
            this.listViewPublicHolidays.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader7,
            this.columnHeader8});
            resources.ApplyResources(this.listViewPublicHolidays, "listViewPublicHolidays");
            this.listViewPublicHolidays.FullRowSelect = true;
            this.listViewPublicHolidays.GridLines = true;
            this.listViewPublicHolidays.MultiSelect = false;
            this.listViewPublicHolidays.Name = "listViewPublicHolidays";
            this.listViewPublicHolidays.UseCompatibleStateImageBehavior = false;
            this.listViewPublicHolidays.View = System.Windows.Forms.View.Details;
            this.listViewPublicHolidays.Click += new System.EventHandler(this.listViewPublicHolidays_Click);
            // 
            // columnHeader7
            // 
            resources.ApplyResources(this.columnHeader7, "columnHeader7");
            // 
            // columnHeader8
            // 
            resources.ApplyResources(this.columnHeader8, "columnHeader8");
            // 
            // groupBox5
            // 
            this.groupBox5.Controls.Add(this.buttonPublicHolidayDelete);
            resources.ApplyResources(this.groupBox5, "groupBox5");
            this.groupBox5.Name = "groupBox5";
            this.groupBox5.TabStop = false;
            // 
            // buttonPublicHolidayDelete
            // 
            resources.ApplyResources(this.buttonPublicHolidayDelete, "buttonPublicHolidayDelete");
            this.buttonPublicHolidayDelete.Name = "buttonPublicHolidayDelete";
            this.buttonPublicHolidayDelete.Click += new System.EventHandler(this.buttonPublicHolidayDelete_Click);
            // 
            // groupBox4
            // 
            this.groupBox4.Controls.Add(this.dateTimePickerPublicHoliday);
            this.groupBox4.Controls.Add(this.textBoxPublicHolidayDescription);
            this.groupBox4.Controls.Add(this.label5);
            this.groupBox4.Controls.Add(this.label6);
            this.groupBox4.Controls.Add(this.buttonPublicHolidaysSave);
            resources.ApplyResources(this.groupBox4, "groupBox4");
            this.groupBox4.Name = "groupBox4";
            this.groupBox4.TabStop = false;
            // 
            // dateTimePickerPublicHoliday
            // 
            this.dateTimePickerPublicHoliday.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            resources.ApplyResources(this.dateTimePickerPublicHoliday, "dateTimePickerPublicHoliday");
            this.dateTimePickerPublicHoliday.Name = "dateTimePickerPublicHoliday";
            this.dateTimePickerPublicHoliday.Value = new System.DateTime(2008, 10, 7, 0, 0, 0, 0);
            this.dateTimePickerPublicHoliday.ValueChanged += new System.EventHandler(this.dateTimePickerPublicHoliday_ValueChanged);
            // 
            // textBoxPublicHolidayDescription
            // 
            resources.ApplyResources(this.textBoxPublicHolidayDescription, "textBoxPublicHolidayDescription");
            this.textBoxPublicHolidayDescription.Name = "textBoxPublicHolidayDescription";
            this.textBoxPublicHolidayDescription.TextChanged += new System.EventHandler(this.textBoxPublicHolidayDescription_TextChanged);
            // 
            // label5
            // 
            resources.ApplyResources(this.label5, "label5");
            this.label5.BackColor = System.Drawing.Color.Transparent;
            this.label5.Name = "label5";
            // 
            // label6
            // 
            resources.ApplyResources(this.label6, "label6");
            this.label6.BackColor = System.Drawing.Color.Transparent;
            this.label6.Name = "label6";
            // 
            // buttonPublicHolidaysSave
            // 
            resources.ApplyResources(this.buttonPublicHolidaysSave, "buttonPublicHolidaysSave");
            this.buttonPublicHolidaysSave.Name = "buttonPublicHolidaysSave";
            this.buttonPublicHolidaysSave.Click += new System.EventHandler(this.buttonPublicHolidaysSave_Click);
            // 
            // tabPageLoanScale
            // 
            this.tabPageLoanScale.Controls.Add(this.groupBox2);
            this.tabPageLoanScale.Controls.Add(this.listViewLoanScale);
            resources.ApplyResources(this.tabPageLoanScale, "tabPageLoanScale");
            this.tabPageLoanScale.Name = "tabPageLoanScale";
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.buttonDeleteScale);
            this.groupBox2.Controls.Add(this.buttonSaveScale);
            this.groupBox2.Controls.Add(this.textBoxMaxScale);
            this.groupBox2.Controls.Add(this.textBoxMinScale);
            this.groupBox2.Controls.Add(this.labelMaxExter);
            this.groupBox2.Controls.Add(this.labelMinExter);
            this.groupBox2.Controls.Add(this.labelScaleMax);
            this.groupBox2.Controls.Add(this.labelScaleMin);
            resources.ApplyResources(this.groupBox2, "groupBox2");
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.TabStop = false;
            // 
            // buttonDeleteScale
            // 
            resources.ApplyResources(this.buttonDeleteScale, "buttonDeleteScale");
            this.buttonDeleteScale.Name = "buttonDeleteScale";
            this.buttonDeleteScale.Click += new System.EventHandler(this.buttonDeleteScale_Click);
            // 
            // buttonSaveScale
            // 
            resources.ApplyResources(this.buttonSaveScale, "buttonSaveScale");
            this.buttonSaveScale.Name = "buttonSaveScale";
            this.buttonSaveScale.Click += new System.EventHandler(this.buttonSaveScale_Click);
            // 
            // textBoxMaxScale
            // 
            resources.ApplyResources(this.textBoxMaxScale, "textBoxMaxScale");
            this.textBoxMaxScale.Name = "textBoxMaxScale";
            this.textBoxMaxScale.TextChanged += new System.EventHandler(this.textBoxMaxScale_TextChanged);
            // 
            // textBoxMinScale
            // 
            resources.ApplyResources(this.textBoxMinScale, "textBoxMinScale");
            this.textBoxMinScale.Name = "textBoxMinScale";
            this.textBoxMinScale.TextChanged += new System.EventHandler(this.textBoxMinScale_TextChanged);
            // 
            // labelMaxExter
            // 
            resources.ApplyResources(this.labelMaxExter, "labelMaxExter");
            this.labelMaxExter.BackColor = System.Drawing.Color.Transparent;
            this.labelMaxExter.Name = "labelMaxExter";
            // 
            // labelMinExter
            // 
            resources.ApplyResources(this.labelMinExter, "labelMinExter");
            this.labelMinExter.BackColor = System.Drawing.Color.Transparent;
            this.labelMinExter.Name = "labelMinExter";
            // 
            // labelScaleMax
            // 
            resources.ApplyResources(this.labelScaleMax, "labelScaleMax");
            this.labelScaleMax.BackColor = System.Drawing.Color.Transparent;
            this.labelScaleMax.Name = "labelScaleMax";
            // 
            // labelScaleMin
            // 
            resources.ApplyResources(this.labelScaleMin, "labelScaleMin");
            this.labelScaleMin.BackColor = System.Drawing.Color.Transparent;
            this.labelScaleMin.Name = "labelScaleMin";
            // 
            // listViewLoanScale
            // 
            this.listViewLoanScale.AutoArrange = false;
            this.listViewLoanScale.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader9,
            this.columnHeader10,
            this.columnHeader11});
            resources.ApplyResources(this.listViewLoanScale, "listViewLoanScale");
            this.listViewLoanScale.FullRowSelect = true;
            this.listViewLoanScale.GridLines = true;
            this.listViewLoanScale.Name = "listViewLoanScale";
            this.listViewLoanScale.UseCompatibleStateImageBehavior = false;
            this.listViewLoanScale.View = System.Windows.Forms.View.Details;
            this.listViewLoanScale.Click += new System.EventHandler(this.listViewLoanScale_Click);
            // 
            // columnHeader9
            // 
            resources.ApplyResources(this.columnHeader9, "columnHeader9");
            // 
            // columnHeader10
            // 
            resources.ApplyResources(this.columnHeader10, "columnHeader10");
            // 
            // columnHeader11
            // 
            resources.ApplyResources(this.columnHeader11, "columnHeader11");
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.butImport);
            this.groupBox1.Controls.Add(this.butExport);
            this.groupBox1.Controls.Add(this.buttonCancel);
            resources.ApplyResources(this.groupBox1, "groupBox1");
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.TabStop = false;
            // 
            // butImport
            // 
            resources.ApplyResources(this.butImport, "butImport");
            this.butImport.Name = "butImport";
            this.butImport.Click += new System.EventHandler(this.butImport_Click);
            // 
            // butExport
            // 
            resources.ApplyResources(this.butExport, "butExport");
            this.butExport.Name = "butExport";
            this.butExport.Click += new System.EventHandler(this.butExport_Click);
            // 
            // buttonCancel
            // 
            resources.ApplyResources(this.buttonCancel, "buttonCancel");
            this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.Click += new System.EventHandler(this.buttonCancel_Click);
            // 
            // openFileDialog
            // 
            this.openFileDialog.FileName = "openFileDialog1";
            // 
            // FrmGeneralSettings
            // 
            resources.ApplyResources(this, "$this");
            this.Controls.Add(this.splitContainer1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Name = "FrmGeneralSettings";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.FrmGeneralSettings_FormClosed);
            this.Load += new System.EventHandler(this.FrmGeneralSettings_Load);
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            this.splitContainer1.ResumeLayout(false);
            this.tabControlGeneralSettings.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            this.groupBoxValue.ResumeLayout(false);
            this.tabPageProvioningRules.ResumeLayout(false);
            this.groupBoxAddUser.ResumeLayout(false);
            this.groupBoxAddUser.PerformLayout();
            this.tabPage2.ResumeLayout(false);
            this.splitContainer2.Panel1.ResumeLayout(false);
            this.splitContainer2.Panel2.ResumeLayout(false);
            this.splitContainer2.ResumeLayout(false);
            this.splitContainer3.Panel1.ResumeLayout(false);
            this.splitContainer3.Panel2.ResumeLayout(false);
            this.splitContainer3.ResumeLayout(false);
            this.groupBox5.ResumeLayout(false);
            this.groupBox4.ResumeLayout(false);
            this.groupBox4.PerformLayout();
            this.tabPageLoanScale.ResumeLayout(false);
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.groupBox1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        void textBoxMinScale_TextChanged(object sender, EventArgs e)
        {
            if (textBoxMinScale.Text != "")
            {
                try
                {
                    textBoxMinScale.Text = Convert.ToInt32(textBoxMinScale.Text.Replace(",", "")).ToString();
                    lR.ScaleMin = Convert.ToInt32(textBoxMinScale.Text);
                }
                catch
                {
                    MessageBox.Show("Invalid minimum amount format!");
                    textBoxMinScale.Focus();
                }
            }
        }

        void textBoxMaxScale_TextChanged(object sender, EventArgs e)
        {
            if (textBoxMaxScale.Text != "")
            {
                try
                {
                    textBoxMaxScale.Text = Convert.ToInt32(textBoxMaxScale.Text.Replace(",", "")).ToString();
                    lR.ScaleMax = Convert.ToInt32(textBoxMaxScale.Text);
                }
                catch
                {
                    MessageBox.Show("Invalid maximum amount format!");
                    textBoxMaxScale.Focus();
                }
            }
        }

     
        #endregion

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void textBoxProvisioning_TextChanged(object sender, EventArgs e)
        {
            try
            {
                pR.ProvisioningValue = Convert.ToDouble(textBoxProvisioningOlb.Text) / 100;
            }
            catch (FormatException ex)
            {
                MessageBox.Show("Invalid format: " + ex.Message);
            }

        }

        private void textBoxProvisioning_KeyPress(object sender, KeyPressEventArgs e)
        {
            const char Delete = (char) 8;
            char decimalSeparator = Convert.ToChar(CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator);
            e.Handled = !Char.IsDigit(e.KeyChar) && e.KeyChar != Delete && e.KeyChar != decimalSeparator;
        }

        private void listViewProvisioningRules_Click(object sender, EventArgs e)
        {
            try
            {
                pR = (ProvisioningRate) listViewProvisioningRules.SelectedItems[0].Tag;
                if (pR.NbOfDaysMin == -1)
                {
                    textBoxNbOfDaysMin.Enabled = false;
                    textBoxNbOfDaysMax.Enabled = false;
                }
                else
                {
                    textBoxNbOfDaysMin.Enabled = true;
                    textBoxNbOfDaysMax.Enabled = true;
                }
                InitializeProvisioningRate();
            } catch
            {
                MessageBox.Show(@"Please, select proper row from the list.");
            }
        }
       
        private void InitializeProvisioningRate()
        {
            groupBoxAddUser.Enabled = true;

            if (pR.NbOfDaysMin == -1)
            {
                textBoxProvisioningOlb.Text = (pR.ProvisioningValue * 100).ToString();
                textBoxProvisioningInterest.Text = (pR.ProvisioningInterest * 100).ToString();
                textBoxProvisioningPenalty.Text = (pR.ProvisioningPenalty * 100).ToString();
                textBoxNbOfDaysMin.Text = @"rest.";
                textBoxNbOfDaysMax.Text = @"rest.";
            }
            else
            {
                textBoxNbOfDaysMin.Text = pR.NbOfDaysMin.ToString();
                textBoxNbOfDaysMax.Text = pR.NbOfDaysMax.ToString();
                textBoxProvisioningOlb.Text = (pR.ProvisioningValue * 100).ToString();
                textBoxProvisioningInterest.Text = (pR.ProvisioningInterest * 100).ToString();
                textBoxProvisioningPenalty.Text = (pR.ProvisioningPenalty * 100).ToString();
            }
        }

        private void buttonSaveScale_Click(object sender, EventArgs e)
        {
            try
            {
                if (lR.Number == 0)
                    ServicesProvider.GetInstance().GetChartOfAccountsServices().AddLoanScale(lR);
                ServicesProvider.GetInstance().GetChartOfAccountsServices().UpdateLoanScaleTableInstance();
                InitializeListViewLoanScaleTables();
            }
            catch(Exception ex)
            {
                new frmShowError(CustomExceptionHandler.ShowExceptionText(ex)).ShowDialog();
            }
        }
        
        private void buttonOK_Click(object sender, EventArgs e)
        {
            try
            {
                if (pR.Number == 0) 
                    ServicesProvider.GetInstance().GetChartOfAccountsServices().AddProvisioningRate(pR);

                ServicesProvider.GetInstance().GetChartOfAccountsServices().UpdateProvisioningTableInstance();
                InitializeListViewProvisioningTables();
            }
            catch (Exception ex)
            {
                new frmShowError(CustomExceptionHandler.ShowExceptionText(ex)).ShowDialog();
            }
        }

        private void listViewGeneralParameters_Click(object sender, EventArgs e)
        {
            entry = (DictionaryEntry)lvGeneralParameters.SelectedItems[0].Tag;
            InitializeControls();
            InitializeGeneralParameterValue();
        }

        private void InitializeControls()
        {
            if (entry.Key.ToString() == OGeneralSettings.CITYMANDATORY ||
                entry.Key.ToString() == OGeneralSettings.PAYFIRSTINSTALLMENTREALVALUE ||
                entry.Key.ToString() == OGeneralSettings.CITYOPENVALUE ||
                entry.Key.ToString() == OGeneralSettings.ALLOWSMULTIPLELOANS ||
                entry.Key.ToString() == OGeneralSettings.ALLOWSMULTIPLEGROUPS ||
                entry.Key.ToString() == OGeneralSettings.OLBBEFOREREPAYMENT ||
                entry.Key.ToString() == OGeneralSettings.CALCULATIONLATEFEESDURINGPUBLICHOLIDAYS ||
                entry.Key.ToString() == OGeneralSettings.DONOTSKIPWEEKENDSININSTALLMENTSDATE ||
                entry.Key.ToString() == OGeneralSettings.USEPROJECTS ||
                entry.Key.ToString() == OGeneralSettings.ENFORCE_ID_PATTERN ||
                entry.Key.ToString() == OGeneralSettings.ID_WILD_CHAR_CHECK ||
                entry.Key.ToString() == OGeneralSettings.INCREMENTALDURINGDAYOFF ||
                entry.Key.ToString() == OGeneralSettings.INTERESTS_ALSO_CREDITED_IN_FL ||
                entry.Key.ToString() == OGeneralSettings.USE_TELLER_MANAGEMENT ||
                entry.Key.ToString() == OGeneralSettings.CONSOLIDATION_MODE ||
                entry.Key.ToString() == OGeneralSettings.AUTOMATIC_ID ||
                entry.Key.ToString() == OGeneralSettings.STOP_WRITEOFF_PENALTY ||
                entry.Key.ToString() == OGeneralSettings.MODIFY_ENTRY_FEE)
            {
                groupBoxValue.Visible = true;
                cbxValue.Visible = false;
                comboBoxSavings.Visible = false;
                textBoxGeneralParameterValue.Visible = false;
                clbxPendingSavings.Visible = false;
            }
            else if(entry.Key.ToString() == OGeneralSettings.ACCOUNTINGPROCESS)
            {
                cbxValue.Items.Clear();
                cbxValue.Items.Add("Accrual");
                cbxValue.Items.Add("Cash");

                groupBoxValue.Visible = false;
                comboBoxSavings.Visible = false;
                textBoxGeneralParameterValue.Visible = false;
                clbxPendingSavings.Visible = false;
                
                cbxValue.Enabled = true;
                cbxValue.Visible = true;
                cbxValue.Width = 150;
            }
            else if (entry.Key.ToString() == OGeneralSettings.REAL_EXPECTED_AMOUNT)
            {
                cbxValue.Visible = true;
                cbxValue.Items.Clear();
                cbxValue.Items.Add("Accrued interests");
                cbxValue.Items.Add("Accrued interests + principal");

                groupBoxValue.Visible = false;
                comboBoxSavings.Visible = false;
                textBoxGeneralParameterValue.Visible = false;
                clbxPendingSavings.Visible = false;
            }
            else if (entry.Key.ToString() == OGeneralSettings.SAVINGS_CODE_TEMPLATE)
            {
                groupBoxValue.Visible = false;
                cbxValue.Visible = false;
                comboBoxSavings.Visible = true;
                textBoxGeneralParameterValue.Visible = false;
            }
            else if (entry.Key.ToString() == OGeneralSettings.PENDING_SAVINGS_MODE)
            {
                groupBoxValue.Visible = false;
                cbxValue.Visible = false;
                comboBoxSavings.Visible = false;
                textBoxGeneralParameterValue.Visible = false;
                clbxPendingSavings.Visible = true;
            }
            else
            {
                groupBoxValue.Visible = false;
                cbxValue.Visible = false;
                comboBoxSavings.Visible = false;
                clbxPendingSavings.Visible = false;
                textBoxGeneralParameterValue.Visible = true;
            }
            
        }

        private void InitializeGeneralParameterValue()
        {
            textBoxGeneralParameterName.Text = entry.Key.ToString();
            if (entry.Key.ToString() == OGeneralSettings.LATEDAYSAFTERACCRUALCEASES ||
                entry.Key.ToString() == OGeneralSettings.MFI_NAME)
            {
                textBoxGeneralParameterValue.Text = entry.Value == null ? "-" : entry.Value.ToString();
                textBoxGeneralParameterValue.Enabled = true;
            }
            else if (entry.Value == null)
            {
                textBoxGeneralParameterValue.Text = String.Empty;
                textBoxGeneralParameterValue.Enabled = false;
            }
            else if (entry.Key.ToString() == OGeneralSettings.CITYMANDATORY ||
                 entry.Key.ToString() == OGeneralSettings.PAYFIRSTINSTALLMENTREALVALUE ||
                 entry.Key.ToString() == OGeneralSettings.CITYOPENVALUE ||
                 entry.Key.ToString() == OGeneralSettings.ALLOWSMULTIPLELOANS||
                 entry.Key.ToString() == OGeneralSettings.ALLOWSMULTIPLEGROUPS ||
                 entry.Key.ToString() == OGeneralSettings.OLBBEFOREREPAYMENT ||
                 entry.Key.ToString() == OGeneralSettings.CALCULATIONLATEFEESDURINGPUBLICHOLIDAYS ||
                 entry.Key.ToString() == OGeneralSettings.DONOTSKIPWEEKENDSININSTALLMENTSDATE ||
                 entry.Key.ToString() == OGeneralSettings.USEPROJECTS ||
                 entry.Key.ToString() == OGeneralSettings.ENFORCE_ID_PATTERN ||
                 entry.Key.ToString() == OGeneralSettings.ID_WILD_CHAR_CHECK || 
                 entry.Key.ToString() == OGeneralSettings.INCREMENTALDURINGDAYOFF ||
                 entry.Key.ToString() == OGeneralSettings.INTERESTS_ALSO_CREDITED_IN_FL ||
                 entry.Key.ToString() == OGeneralSettings.USE_TELLER_MANAGEMENT ||
                 entry.Key.ToString() == OGeneralSettings.CONSOLIDATION_MODE ||
                 entry.Key.ToString() == OGeneralSettings.AUTOMATIC_ID ||
                 entry.Key.ToString() == OGeneralSettings.STOP_WRITEOFF_PENALTY ||
                 entry.Key.ToString() == OGeneralSettings.MODIFY_ENTRY_FEE)
            {
                radioButtonYes.Checked = entry.Value.ToString() == "1";
                radioButtonNo.Checked = entry.Value.ToString() == "0";
            }
            else if(entry.Key.ToString() == OGeneralSettings.ACCOUNTINGPROCESS)
            {
                cbxValue.Text = entry.Value.ToString() == "1" ? "Cash" : "Accrual";
            }
            else if (entry.Key.ToString() == OGeneralSettings.SAVINGS_CODE_TEMPLATE)
            {
                comboBoxSavings.Text = entry.Value.ToString();
            }
            else if (entry.Key.ToString() == OGeneralSettings.PENDING_SAVINGS_MODE)
            {
                foreach (int checkedIndice in clbxPendingSavings.CheckedIndices)
                {
                    clbxPendingSavings.SetItemChecked(checkedIndice, false);
                }

                foreach (var item in entry.Value.ToString().Split(','))
                {
                    string str = MultiLanguageStrings.GetString(Ressource.FrmAddSavingProduct, item + ".Text");
                    if (str == string.Empty)
                    {
                        int index = clbxPendingSavings.FindStringExact(str);
                        if (index != -1)
                            clbxPendingSavings.SetItemChecked(index, true);
                    }
                }
            }
            else
            {
                textBoxGeneralParameterValue.Text = entry.Value.ToString();
            }
        }


        private void buttonUpdate_Click(object sender, EventArgs e)
        {
            ServicesProvider.GetInstance().GetGeneralSettings().UpdateParameter(entry.Key.ToString(), entry.Value);
            ServicesProvider.GetInstance().GetApplicationSettingsServices().UpdateSelectedParameter(entry.Key.ToString(), entry.Value);
            InitializeListViewGeneralParameters();
        }

        private void textBoxGeneralParameterValue_TextChanged(object sender, EventArgs e)
        {
            string entryKey = entry.Key.ToString();
            try
            {
                if (entryKey == OGeneralSettings.IMF_CODE)
                {
                    // Branch name can only contain chars
                    string value = textBoxGeneralParameterValue.Text;
                    for (int i = 0; i < value.Length; i++)
                    {
                        char c = value[i];
                        if (
                            !(((c >= 'a') && (c <= 'z')) || ((c >= 'A') && (c <= 'Z')) || ((c >= '0') && (c <= '9')) ||
                              (c == '_')))
                        {
                            throw new GeneralSettingException(GeneralSettingEnumException.OnlyChar);
                        }
                        if (i == value.Length - 1)
                            entry.Value = textBoxGeneralParameterValue.Text;
                    }
                }

                else if (entryKey == OGeneralSettings.GROUPMINMEMBERS ||
                         entryKey == OGeneralSettings.GROUPMAXMEMBERS ||
                         entryKey == OGeneralSettings.VILLAGEMINMEMBERS ||
                         entryKey == OGeneralSettings.VILLAGEMAXMEMBERS ||
                         entryKey == OGeneralSettings.WEEKENDDAY1 ||
                         entryKey == OGeneralSettings.WEEKENDDAY2 || 
                         entryKey == OGeneralSettings.CEASE_LAIE_DAYS ||
                         entryKey == OGeneralSettings.CLIENT_AGE_MIN ||
                         entryKey == OGeneralSettings.CLIENT_AGE_MAX ||
                         entryKey == OGeneralSettings.MAX_LOANS_COVERED ||
                         entryKey == OGeneralSettings.MAX_GUARANTOR_AMOUNT ||
                         entryKey == OGeneralSettings.INTEREST_RATE_DECIMAL_PLACES)
                {
                    if (textBoxGeneralParameterValue.Text != String.Empty)
                    {
                        try
                        {
                            entry.Value = Convert.ToInt32(textBoxGeneralParameterValue.Text);
                        }
                        catch
                        {
                            throw new GeneralSettingException(GeneralSettingEnumException.OnlyInt);
                        }
                    }
                    else
                        entry.Value = null;

                }
                else if (entryKey == OGeneralSettings.LATEDAYSAFTERACCRUALCEASES)
                {
                    if (textBoxGeneralParameterValue.Text == @"-")
                        entry.Value = null;
                    else
                    {
                        try
                        {
                            entry.Value = Convert.ToInt32(textBoxGeneralParameterValue.Text);
                        }
                        catch
                        {
                            textBoxGeneralParameterValue.Text = @"-";
                            entry.Value = null;
                            throw new GeneralSettingException(GeneralSettingEnumException.OnlyIntAndUnderscore);
                        }
                    }
                }
                else
                {
                    entry.Value = textBoxGeneralParameterValue.Text;
                }
            }
            catch (Exception ex)
            {
                new frmShowError(CustomExceptionHandler.ShowExceptionText(ex)).ShowDialog();
                if (entryKey != OGeneralSettings.LATEDAYSAFTERACCRUALCEASES)
                {
                    textBoxGeneralParameterValue.Text = String.Empty;
                    entry.Value = String.Empty;
                }
            }
        }

        private void radioButtonNo_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButtonYes.Checked)
                entry.Value = true;
            else
                entry.Value = false;
        }

        private void InitializeHoliday()
        {
            groupBox4.Enabled = true;

            dateTimePickerPublicHoliday.Value = (DateTime)entry.Key;
            textBoxPublicHolidayDescription.Text = entry.Value.ToString();
        }

        private void listViewPublicHolidays_Click(object sender, EventArgs e)
        {
            KeyValuePair<DateTime, string> valuePair = (KeyValuePair<DateTime, string>) listViewPublicHolidays.SelectedItems[0].Tag;
            entry = new DictionaryEntry(valuePair.Key, valuePair.Value); ;
            InitializeHoliday();
        }

        private void dateTimePickerPublicHoliday_ValueChanged(object sender, EventArgs e)
        {
            entry.Key = dateTimePickerPublicHoliday.Value.Date;
        }

        private void textBoxPublicHolidayDescription_TextChanged(object sender, EventArgs e)
        {
            entry.Value = textBoxPublicHolidayDescription.Text;
            entry.Key = dateTimePickerPublicHoliday.Value.Date;
        }

        private void buttonPublicHolidaysSave_Click(object sender, EventArgs e)
        {
            PublicHolidaysWaitingForm waitingForm;
            DialogResult result;
            if (entry.Value != null)
            {
                if (ServicesProvider.GetInstance().GetNonWorkingDate().PublicHolidays.ContainsKey((DateTime)entry.Key))
                {
                    result = MessageBox.Show(MultiLanguageStrings.GetString(Ressource.GeneralSettings, "dateAlreadyExist.Text"), "",
                                             MessageBoxButtons.YesNo,
                                             MessageBoxIcon.Information);
                    if (result == DialogResult.Yes)
                    {
                        ServicesProvider.GetInstance().GetNonWorkingDate().PublicHolidays[(DateTime)entry.Key] = (string)entry.Value;
                        ServicesProvider.GetInstance().GetApplicationSettingsServices().UpdateNonWorkingDate(entry);
                        ServicesProvider.GetInstance().GetApplicationSettingsServices().FillNonWorkingDate();
                    }
                }
                else
                {
                    //display dialog
                    waitingForm = new PublicHolidaysWaitingForm();
                    result = waitingForm.ShowDialog();

                    ServicesProvider.GetInstance().GetNonWorkingDate().PublicHolidays.Add((DateTime)entry.Key, (string)entry.Value);
                    ServicesProvider.GetInstance().GetApplicationSettingsServices().AddNonWorkingDate(entry);
                    ServicesProvider.GetInstance().GetApplicationSettingsServices().FillNonWorkingDate();

                    if (result == DialogResult.Yes)
                    {
                        waitingForm.UpdateInstallmentsDate();
                    }
                }

                InitializeListViewPublicHolidays();
            }
            else
            {
                MessageBox.Show(MultiLanguageStrings.GetString(Ressource.GeneralSettings, "emptyPHDescription.Text"), "",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information);
            } 
        }

        private void buttonPublicHolidayDelete_Click(object sender, EventArgs e)
        {
            if (listViewPublicHolidays.SelectedItems.Count != 0)
            {
                ServicesProvider.GetInstance().GetNonWorkingDate().PublicHolidays.Remove((DateTime)entry.Key);
                ServicesProvider.GetInstance().GetApplicationSettingsServices().DeleteNonWorkingDate(entry);
               
                PublicHolidaysWaitingForm waitingForm;
               
                DialogResult result;

                if (entry.Value != null)
                {
                    waitingForm = new PublicHolidaysWaitingForm();
                    result = waitingForm.ShowDialog();
                    if (result == DialogResult.Yes)
                    {
                        waitingForm.UpdateInstallmentsDate((DateTime)entry.Key, ServicesProvider.GetInstance().GetContractServices().GetListOfInstallmentsOnDate((DateTime)entry.Key));
                    }
                }
            }
            else
                MessageBox.Show(MultiLanguageStrings.GetString(Ressource.GeneralSettings, "selectADate.Text"), "", MessageBoxButtons.OK, MessageBoxIcon.Information);

            InitializeListViewPublicHolidays();
        }

        private void butExport_Click(object sender, EventArgs e)
        {
            Form frm = new FrmSettingsImportExport();
            frm.ShowDialog();
        }

        private void butImport_Click(object sender, EventArgs e)
        {
            openFileDialog.Filter = @"OpenCBS Settings files|*.Settings";
            openFileDialog.FileName = @"OpenCBS.Settings";
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                string fileName = openFileDialog.FileName;
                Form frm = new FrmSettingsImportExport(fileName);
                frm.ShowDialog();
            }
        }

        private void listViewLoanScale_Click(object sender, EventArgs e)
        {
            lR = (LoanScaleRate)listViewLoanScale.SelectedItems[0].Tag;
            InitializeLoanScaleRate();
        }

        private void InitializeLoanScaleRate()
        {
            groupBox2.Enabled = true;
            textBoxMinScale.Text = lR.ScaleMin.ToString();
            textBoxMaxScale.Text = lR.ScaleMax.ToString();
        }

        private void comboBoxValue_SelectionChangeCommitted(object sender, EventArgs e)
        {
            entry.Value = cbxValue.SelectedItem.ToString() == "Cash" ? 1: 2;
        }

        private void FrmGeneralSettings_Load(object sender, EventArgs e)
        {
            dateTimePickerPublicHoliday.Value = TimeProvider.Now;
            cbxValue.Location = new Point(96, 71);
        }

        private void buttonDeleteScale_Click(object sender, EventArgs e)
        {
            try
            {
                ChartOfAccountsServices coas = ServicesProvider.GetInstance().GetChartOfAccountsServices();
                coas.DeleteLoanScale(lR);
                coas.UpdateLoanScaleTableInstance();
                InitializeListViewLoanScaleTables();
            }
            catch (Exception ex)
            {
                new frmShowError(CustomExceptionHandler.ShowExceptionText(ex)).ShowDialog();
            }
        }

        private void comboBoxSavings_SelectionChangeCommitted(object sender, EventArgs e)
        {
            entry.Value = comboBoxSavings.SelectedItem.ToString();
        }

        private void FrmGeneralSettings_FormClosed(object sender, FormClosedEventArgs e)
        {

        }

        private void checkedListBoxPendingSavings_Leave(object sender, EventArgs e)
        {
            List<string> selected = new List<string>();
            foreach (string checkedItem in clbxPendingSavings.CheckedItems)
            {
                selected.Add(Enum.GetNames(typeof(OPaymentMethods)).First(item =>
                    checkedItem == MultiLanguageStrings.GetString(Ressource.FrmAddSavingProduct, item + ".Text")));
            }
            entry.Value = string.Join(",", selected.ToArray());
            if (string.IsNullOrEmpty(entry.Value.ToString()))
                entry.Value = "NONE";
        }

        private void groupBox3_Enter(object sender, EventArgs e)
        {

        }

        private void textBoxNbOfDaysMin_TextChanged(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(textBoxNbOfDaysMin.Text) && textBoxNbOfDaysMin.Text != "rest.")
                pR.NbOfDaysMin = ServicesHelper.ConvertStringToInt32(textBoxNbOfDaysMin.Text);
        }

        private void textBoxNbOfDaysMax_TextChanged(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(textBoxNbOfDaysMax.Text) && textBoxNbOfDaysMax.Text != "rest.")
                pR.NbOfDaysMax = ServicesHelper.ConvertStringToInt32(textBoxNbOfDaysMax.Text);
        }

        private void textBoxNbOfDaysMin_KeyPress(object sender, KeyPressEventArgs e)
        {
            const char Delete = (char)8;
            e.Handled = !Char.IsDigit(e.KeyChar) && e.KeyChar != Delete;
        }

        private void textBoxNbOfDaysMax_KeyPress(object sender, KeyPressEventArgs e)
        {
            const char Delete = (char)8;
            e.Handled = !Char.IsDigit(e.KeyChar) && e.KeyChar != Delete;
        }

        private void textBoxProvisioningInterest_TextChanged(object sender, EventArgs e)
        {
            try
            {
                pR.ProvisioningInterest = Convert.ToDouble(textBoxProvisioningInterest.Text) / 100;
            }
            catch (FormatException ex)
            {
                MessageBox.Show("Invalid format: " + ex.Message);
            }
        }

        private void textBoxProvisioningInterest_KeyPress(object sender, KeyPressEventArgs e)
        {
            const char Delete = (char)8;
            char decimalSeparator = Convert.ToChar(CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator);
            e.Handled = !Char.IsDigit(e.KeyChar) && e.KeyChar != Delete && e.KeyChar != decimalSeparator;
        }

        private void textBoxProvisioningPenalty_TextChanged(object sender, EventArgs e)
        {
            try
            {
                pR.ProvisioningPenalty = Convert.ToDouble(textBoxProvisioningPenalty.Text) / 100;
            }
            catch (FormatException ex)
            {
                MessageBox.Show("Invalid format: " + ex.Message);
            }
        }

        

    }
}
