using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using OpenCBS.ExceptionsHandler;
using OpenCBS.GUI.Clients;
using OpenCBS.GUI.Tools;
using OpenCBS.Enums;

namespace OpenCBS.GUI.UserControl
{
    partial class CorporateUserControl
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(CorporateUserControl));
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.lvContacts = new System.Windows.Forms.ListView();
            this.columnHeaderName = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeaderPhone = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.btnAddContact = new System.Windows.Forms.Button();
            this.btnSelectContact = new System.Windows.Forms.Button();
            this.buttonDelete = new System.Windows.Forms.Button();
            this.tabControlCorporate = new System.Windows.Forms.TabControl();
            this.tabPageAddress = new System.Windows.Forms.TabPage();
            this.groupBoxAddress = new System.Windows.Forms.GroupBox();
            this.tabPageContacts = new System.Windows.Forms.TabPage();
            this.tableLayoutPanel3 = new System.Windows.Forms.TableLayoutPanel();
            this.tabPageSavings = new System.Windows.Forms.TabPage();
            this.savingsListUserControl1 = new OpenCBS.GUI.UserControl.SavingsListUserControl();
            this.tabPageCustomizableFields = new System.Windows.Forms.TabPage();
            this.cbBranch = new System.Windows.Forms.ComboBox();
            this.groupBoxCorporate = new System.Windows.Forms.GroupBox();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.labelLastname = new System.Windows.Forms.Label();
            this.eacCorporate = new OpenCBS.GUI.UserControl.EconomicActivityControl();
            this.lblEconomicActivity = new System.Windows.Forms.Label();
            this.labelDateCrate = new System.Windows.Forms.Label();
            this.labelSigle = new System.Windows.Forms.Label();
            this.labelCorpCycle = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.textBoxCorpLoanCycle = new System.Windows.Forms.TextBox();
            this.textBoxLastNameCorporate = new System.Windows.Forms.TextBox();
            this.dateTimePickerDateOfCreate = new System.Windows.Forms.DateTimePicker();
            this.textBoxSigle = new System.Windows.Forms.TextBox();
            this.textBoxSmallNameCorporate = new System.Windows.Forms.TextBox();
            this.labelSmallNameCorporate = new System.Windows.Forms.Label();
            this.linkLabelChangePhoto2 = new System.Windows.Forms.LinkLabel();
            this.linkLabelChangePhoto = new System.Windows.Forms.LinkLabel();
            this.pictureBox2 = new System.Windows.Forms.PictureBox();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            this.buttonCancel = new System.Windows.Forms.Button();
            this.buttonSave = new System.Windows.Forms.Button();
            this.btnPrint = new OpenCBS.GUI.UserControl.PrintButton();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.tabControlCorporate.SuspendLayout();
            this.tabPageAddress.SuspendLayout();
            this.tabPageContacts.SuspendLayout();
            this.tableLayoutPanel3.SuspendLayout();
            this.tabPageSavings.SuspendLayout();
            this.groupBoxCorporate.SuspendLayout();
            this.tableLayoutPanel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.flowLayoutPanel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // splitContainer1
            // 
            resources.ApplyResources(this.splitContainer1, "splitContainer1");
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.lvContacts);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.groupBox3);
            // 
            // lvContacts
            // 
            this.lvContacts.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeaderName,
            this.columnHeaderPhone});
            resources.ApplyResources(this.lvContacts, "lvContacts");
            this.lvContacts.FullRowSelect = true;
            this.lvContacts.GridLines = true;
            this.lvContacts.MultiSelect = false;
            this.lvContacts.Name = "lvContacts";
            this.lvContacts.UseCompatibleStateImageBehavior = false;
            this.lvContacts.View = System.Windows.Forms.View.Details;
            this.lvContacts.DoubleClick += new System.EventHandler(this.ViewMember);
            // 
            // columnHeaderName
            // 
            resources.ApplyResources(this.columnHeaderName, "columnHeaderName");
            // 
            // columnHeaderPhone
            // 
            resources.ApplyResources(this.columnHeaderPhone, "columnHeaderPhone");
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.btnAddContact);
            this.groupBox3.Controls.Add(this.btnSelectContact);
            this.groupBox3.Controls.Add(this.buttonDelete);
            resources.ApplyResources(this.groupBox3, "groupBox3");
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.TabStop = false;
            // 
            // btnAddContact
            // 
            resources.ApplyResources(this.btnAddContact, "btnAddContact");
            this.btnAddContact.Name = "btnAddContact";
            this.btnAddContact.Click += new System.EventHandler(this.BtnAddContactClick);
            // 
            // btnSelectContact
            // 
            resources.ApplyResources(this.btnSelectContact, "btnSelectContact");
            this.btnSelectContact.Name = "btnSelectContact";
            this.btnSelectContact.Click += new System.EventHandler(this.BtnSelectContactClick);
            // 
            // buttonDelete
            // 
            resources.ApplyResources(this.buttonDelete, "buttonDelete");
            this.buttonDelete.Name = "buttonDelete";
            this.buttonDelete.Click += new System.EventHandler(this.ButtonDeleteClick1);
            // 
            // tabControlCorporate
            // 
            this.tabControlCorporate.Controls.Add(this.tabPageAddress);
            this.tabControlCorporate.Controls.Add(this.tabPageContacts);
            this.tabControlCorporate.Controls.Add(this.tabPageSavings);
            this.tabControlCorporate.Controls.Add(this.tabPageCustomizableFields);
            resources.ApplyResources(this.tabControlCorporate, "tabControlCorporate");
            this.tabControlCorporate.Multiline = true;
            this.tabControlCorporate.Name = "tabControlCorporate";
            this.tabControlCorporate.SelectedIndex = 0;
            this.tabControlCorporate.DoubleClick += new System.EventHandler(this.ViewMember);
            // 
            // tabPageAddress
            // 
            resources.ApplyResources(this.tabPageAddress, "tabPageAddress");
            this.tabPageAddress.Controls.Add(this.groupBoxAddress);
            this.tabPageAddress.Name = "tabPageAddress";
            // 
            // groupBoxAddress
            // 
            resources.ApplyResources(this.groupBoxAddress, "groupBoxAddress");
            this.groupBoxAddress.Name = "groupBoxAddress";
            this.groupBoxAddress.TabStop = false;
            // 
            // tabPageContacts
            // 
            resources.ApplyResources(this.tabPageContacts, "tabPageContacts");
            this.tabPageContacts.Controls.Add(this.tableLayoutPanel3);
            this.tabPageContacts.Name = "tabPageContacts";
            // 
            // tableLayoutPanel3
            // 
            resources.ApplyResources(this.tableLayoutPanel3, "tableLayoutPanel3");
            this.tableLayoutPanel3.Controls.Add(this.splitContainer1, 0, 0);
            this.tableLayoutPanel3.Name = "tableLayoutPanel3";
            // 
            // tabPageSavings
            // 
            resources.ApplyResources(this.tabPageSavings, "tabPageSavings");
            this.tabPageSavings.Controls.Add(this.savingsListUserControl1);
            this.tabPageSavings.Name = "tabPageSavings";
            // 
            // savingsListUserControl1
            // 
            this.savingsListUserControl1.ButtonAddSavingsEnabled = true;
            this.savingsListUserControl1.ClientType = OpenCBS.Enums.OClientTypes.Corporate;
            resources.ApplyResources(this.savingsListUserControl1, "savingsListUserControl1");
            this.savingsListUserControl1.Name = "savingsListUserControl1";
            this.savingsListUserControl1.AddSelectedSaving += new System.EventHandler(this.SavingsListUserControl1AddSelectedSaving);
            this.savingsListUserControl1.ViewSelectedSaving += new System.EventHandler(this.SavingsListUserControl1ViewSelectedSaving);
            // 
            // tabPageCustomizableFields
            // 
            resources.ApplyResources(this.tabPageCustomizableFields, "tabPageCustomizableFields");
            this.tabPageCustomizableFields.Name = "tabPageCustomizableFields";
            // 
            // cbBranch
            // 
            resources.ApplyResources(this.cbBranch, "cbBranch");
            this.cbBranch.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbBranch.FormattingEnabled = true;
            this.cbBranch.Name = "cbBranch";
            // 
            // groupBoxCorporate
            // 
            this.groupBoxCorporate.Controls.Add(this.tableLayoutPanel1);
            this.groupBoxCorporate.Controls.Add(this.linkLabelChangePhoto2);
            this.groupBoxCorporate.Controls.Add(this.linkLabelChangePhoto);
            this.groupBoxCorporate.Controls.Add(this.pictureBox2);
            this.groupBoxCorporate.Controls.Add(this.pictureBox1);
            resources.ApplyResources(this.groupBoxCorporate, "groupBoxCorporate");
            this.groupBoxCorporate.Name = "groupBoxCorporate";
            this.groupBoxCorporate.TabStop = false;
            // 
            // tableLayoutPanel1
            // 
            resources.ApplyResources(this.tableLayoutPanel1, "tableLayoutPanel1");
            this.tableLayoutPanel1.Controls.Add(this.labelLastname, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.eacCorporate, 2, 6);
            this.tableLayoutPanel1.Controls.Add(this.lblEconomicActivity, 0, 6);
            this.tableLayoutPanel1.Controls.Add(this.labelDateCrate, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.labelSigle, 0, 2);
            this.tableLayoutPanel1.Controls.Add(this.cbBranch, 1, 5);
            this.tableLayoutPanel1.Controls.Add(this.labelCorpCycle, 0, 4);
            this.tableLayoutPanel1.Controls.Add(this.label3, 0, 5);
            this.tableLayoutPanel1.Controls.Add(this.textBoxCorpLoanCycle, 1, 4);
            this.tableLayoutPanel1.Controls.Add(this.textBoxLastNameCorporate, 1, 0);
            this.tableLayoutPanel1.Controls.Add(this.dateTimePickerDateOfCreate, 1, 1);
            this.tableLayoutPanel1.Controls.Add(this.textBoxSigle, 1, 2);
            this.tableLayoutPanel1.Controls.Add(this.textBoxSmallNameCorporate, 1, 3);
            this.tableLayoutPanel1.Controls.Add(this.labelSmallNameCorporate, 0, 3);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            // 
            // labelLastname
            // 
            resources.ApplyResources(this.labelLastname, "labelLastname");
            this.labelLastname.BackColor = System.Drawing.Color.Transparent;
            this.labelLastname.Name = "labelLastname";
            // 
            // eacCorporate
            // 
            this.eacCorporate.Activity = null;
            resources.ApplyResources(this.eacCorporate, "eacCorporate");
            this.eacCorporate.Name = "eacCorporate";
            this.eacCorporate.EconomicActivityChange += new System.EventHandler<OpenCBS.GUI.UserControl.EconomicActivtyEventArgs>(this.EacCorporateEconomicActivityChange);
            // 
            // lblEconomicActivity
            // 
            resources.ApplyResources(this.lblEconomicActivity, "lblEconomicActivity");
            this.lblEconomicActivity.BackColor = System.Drawing.Color.Transparent;
            this.lblEconomicActivity.Name = "lblEconomicActivity";
            // 
            // labelDateCrate
            // 
            resources.ApplyResources(this.labelDateCrate, "labelDateCrate");
            this.labelDateCrate.BackColor = System.Drawing.Color.Transparent;
            this.labelDateCrate.Name = "labelDateCrate";
            // 
            // labelSigle
            // 
            resources.ApplyResources(this.labelSigle, "labelSigle");
            this.labelSigle.BackColor = System.Drawing.Color.Transparent;
            this.labelSigle.Name = "labelSigle";
            // 
            // labelCorpCycle
            // 
            resources.ApplyResources(this.labelCorpCycle, "labelCorpCycle");
            this.labelCorpCycle.BackColor = System.Drawing.Color.Transparent;
            this.labelCorpCycle.Name = "labelCorpCycle";
            // 
            // label3
            // 
            resources.ApplyResources(this.label3, "label3");
            this.label3.BackColor = System.Drawing.Color.Transparent;
            this.label3.Name = "label3";
            // 
            // textBoxCorpLoanCycle
            // 
            resources.ApplyResources(this.textBoxCorpLoanCycle, "textBoxCorpLoanCycle");
            this.textBoxCorpLoanCycle.Name = "textBoxCorpLoanCycle";
            // 
            // textBoxLastNameCorporate
            // 
            this.textBoxLastNameCorporate.BackColor = System.Drawing.Color.White;
            resources.ApplyResources(this.textBoxLastNameCorporate, "textBoxLastNameCorporate");
            this.textBoxLastNameCorporate.Name = "textBoxLastNameCorporate";
            // 
            // dateTimePickerDateOfCreate
            // 
            resources.ApplyResources(this.dateTimePickerDateOfCreate, "dateTimePickerDateOfCreate");
            this.dateTimePickerDateOfCreate.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dateTimePickerDateOfCreate.Name = "dateTimePickerDateOfCreate";
            this.dateTimePickerDateOfCreate.Value = new System.DateTime(2006, 5, 15, 0, 0, 0, 0);
            // 
            // textBoxSigle
            // 
            this.textBoxSigle.BackColor = System.Drawing.Color.White;
            resources.ApplyResources(this.textBoxSigle, "textBoxSigle");
            this.textBoxSigle.Name = "textBoxSigle";
            // 
            // textBoxSmallNameCorporate
            // 
            resources.ApplyResources(this.textBoxSmallNameCorporate, "textBoxSmallNameCorporate");
            this.textBoxSmallNameCorporate.Name = "textBoxSmallNameCorporate";
            // 
            // labelSmallNameCorporate
            // 
            resources.ApplyResources(this.labelSmallNameCorporate, "labelSmallNameCorporate");
            this.labelSmallNameCorporate.BackColor = System.Drawing.Color.Transparent;
            this.labelSmallNameCorporate.Name = "labelSmallNameCorporate";
            // 
            // linkLabelChangePhoto2
            // 
            resources.ApplyResources(this.linkLabelChangePhoto2, "linkLabelChangePhoto2");
            this.linkLabelChangePhoto2.Cursor = System.Windows.Forms.Cursors.Hand;
            this.linkLabelChangePhoto2.Name = "linkLabelChangePhoto2";
            this.linkLabelChangePhoto2.TabStop = true;
            this.linkLabelChangePhoto2.Click += new System.EventHandler(this.PictureBox1Click);
            // 
            // linkLabelChangePhoto
            // 
            resources.ApplyResources(this.linkLabelChangePhoto, "linkLabelChangePhoto");
            this.linkLabelChangePhoto.Name = "linkLabelChangePhoto";
            this.linkLabelChangePhoto.TabStop = true;
            this.linkLabelChangePhoto.Click += new System.EventHandler(this.PictureBox1Click);
            // 
            // pictureBox2
            // 
            resources.ApplyResources(this.pictureBox2, "pictureBox2");
            this.pictureBox2.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            this.pictureBox2.Cursor = System.Windows.Forms.Cursors.Hand;
            this.pictureBox2.Name = "pictureBox2";
            this.pictureBox2.TabStop = false;
            this.pictureBox2.Click += new System.EventHandler(this.PictureBox1Click);
            // 
            // pictureBox1
            // 
            resources.ApplyResources(this.pictureBox1, "pictureBox1");
            this.pictureBox1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            this.pictureBox1.Cursor = System.Windows.Forms.Cursors.Hand;
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.TabStop = false;
            this.pictureBox1.Click += new System.EventHandler(this.PictureBox1Click);
            // 
            // flowLayoutPanel1
            // 
            resources.ApplyResources(this.flowLayoutPanel1, "flowLayoutPanel1");
            this.flowLayoutPanel1.Controls.Add(this.buttonCancel);
            this.flowLayoutPanel1.Controls.Add(this.buttonSave);
            this.flowLayoutPanel1.Controls.Add(this.btnPrint);
            this.flowLayoutPanel1.Name = "flowLayoutPanel1";
            // 
            // buttonCancel
            // 
            resources.ApplyResources(this.buttonCancel, "buttonCancel");
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.Click += new System.EventHandler(this.ButtonCancelClick);
            // 
            // buttonSave
            // 
            resources.ApplyResources(this.buttonSave, "buttonSave");
            this.buttonSave.Name = "buttonSave";
            this.buttonSave.Click += new System.EventHandler(this.SaveCorporate);
            // 
            // btnPrint
            // 
            resources.ApplyResources(this.btnPrint, "btnPrint");
            this.btnPrint.AttachmentPoint = OpenCBS.Reports.AttachmentPoint.CorporateDetails;
            this.btnPrint.Name = "btnPrint";
            this.btnPrint.ReportInitializer = null;
            this.btnPrint.UseVisualStyleBackColor = true;
            this.btnPrint.Visibility = OpenCBS.Reports.Visibility.Group;
            // 
            // CorporateUserControl
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.Controls.Add(this.tabControlCorporate);
            this.Controls.Add(this.flowLayoutPanel1);
            this.Controls.Add(this.groupBoxCorporate);
            this.Name = "CorporateUserControl";
            resources.ApplyResources(this, "$this");
            this.Load += new System.EventHandler(this.CorporateUserControlLoad);
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            this.splitContainer1.ResumeLayout(false);
            this.groupBox3.ResumeLayout(false);
            this.tabControlCorporate.ResumeLayout(false);
            this.tabPageAddress.ResumeLayout(false);
            this.tabPageContacts.ResumeLayout(false);
            this.tableLayoutPanel3.ResumeLayout(false);
            this.tabPageSavings.ResumeLayout(false);
            this.groupBoxCorporate.ResumeLayout(false);
            this.groupBoxCorporate.PerformLayout();
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.flowLayoutPanel1.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBoxCorporate;
        private System.Windows.Forms.TextBox textBoxSigle;
        private System.Windows.Forms.DateTimePicker dateTimePickerDateOfCreate;
        private System.Windows.Forms.Label labelDateCrate;
        private System.Windows.Forms.TextBox textBoxLastNameCorporate;
        private System.Windows.Forms.Label labelLastname;
        private System.Windows.Forms.Label labelSigle;
        private System.Windows.Forms.TextBox textBoxSmallNameCorporate;
        private System.Windows.Forms.Label labelSmallNameCorporate;
        protected System.Windows.Forms.TabControl tabControlCorporate;
        private System.Windows.Forms.TabPage tabPageAddress;
        private System.Windows.Forms.TabPage tabPageContacts;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel3;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.ListView lvContacts;
        private System.Windows.Forms.ColumnHeader columnHeaderName;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.Button buttonDelete;
        private System.Windows.Forms.GroupBox groupBoxAddress;
        private System.Windows.Forms.TabPage tabPageSavings;
        private SavingsListUserControl savingsListUserControl1;
        private System.Windows.Forms.LinkLabel linkLabelChangePhoto2;
        private System.Windows.Forms.LinkLabel linkLabelChangePhoto;
        public System.Windows.Forms.PictureBox pictureBox2;
        public System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
        private System.Windows.Forms.ComboBox cbBranch;
        private System.Windows.Forms.Label label3;
        private TabPage tabPageCustomizableFields;
        private System.Windows.Forms.Button buttonSave;
        private System.Windows.Forms.Button buttonCancel;
        private Label labelCorpCycle;
        private TextBox textBoxCorpLoanCycle;
        private PrintButton btnPrint;
        private TableLayoutPanel tableLayoutPanel1;
        private Label lblEconomicActivity;
        private EconomicActivityControl eacCorporate;
        private System.Windows.Forms.Button btnAddContact;
        private System.Windows.Forms.Button btnSelectContact;
        private ColumnHeader columnHeaderPhone;

    }
}
