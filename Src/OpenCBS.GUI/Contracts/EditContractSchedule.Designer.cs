using OpenCBS.GUI.UserControl;

namespace OpenCBS.GUI.Contracts
{
    partial class EditContractSchedule
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

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(EditContractSchedule));
            this.splitContainer = new System.Windows.Forms.SplitContainer();
            this.pnlRounding = new System.Windows.Forms.Panel();
            this.chxAutomaticCalculation = new System.Windows.Forms.CheckBox();
            this.rbtnRoundTo10 = new System.Windows.Forms.RadioButton();
            this.rbtnInitialSchedule = new System.Windows.Forms.RadioButton();
            this.rbtnRoundTo5 = new System.Windows.Forms.RadioButton();
            this.textBox = new System.Windows.Forms.TextBox();
            this.dateTimePicker = new System.Windows.Forms.DateTimePicker();
            this.lvSchedule = new OpenCBS.GUI.UserControl.ListViewEx();
            this.columnHeader1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader2 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader3 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader4 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader5 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader6 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.pnlButtons = new System.Windows.Forms.Panel();
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnOK = new System.Windows.Forms.Button();
            this.splitContainer.Panel1.SuspendLayout();
            this.splitContainer.Panel2.SuspendLayout();
            this.splitContainer.SuspendLayout();
            this.pnlRounding.SuspendLayout();
            this.pnlButtons.SuspendLayout();
            this.SuspendLayout();
            // 
            // splitContainer
            // 
            resources.ApplyResources(this.splitContainer, "splitContainer");
            this.splitContainer.Name = "splitContainer";
            // 
            // splitContainer.Panel1
            // 
            resources.ApplyResources(this.splitContainer.Panel1, "splitContainer.Panel1");
            this.splitContainer.Panel1.Controls.Add(this.pnlRounding);
            this.splitContainer.Panel1.Controls.Add(this.textBox);
            this.splitContainer.Panel1.Controls.Add(this.dateTimePicker);
            this.splitContainer.Panel1.Controls.Add(this.lvSchedule);
            // 
            // splitContainer.Panel2
            // 
            resources.ApplyResources(this.splitContainer.Panel2, "splitContainer.Panel2");
            this.splitContainer.Panel2.Controls.Add(this.pnlButtons);
            // 
            // pnlRounding
            // 
            resources.ApplyResources(this.pnlRounding, "pnlRounding");
            this.pnlRounding.Controls.Add(this.chxAutomaticCalculation);
            this.pnlRounding.Controls.Add(this.rbtnRoundTo10);
            this.pnlRounding.Controls.Add(this.rbtnInitialSchedule);
            this.pnlRounding.Controls.Add(this.rbtnRoundTo5);
            this.pnlRounding.Name = "pnlRounding";
            // 
            // chxAutomaticCalculation
            // 
            resources.ApplyResources(this.chxAutomaticCalculation, "chxAutomaticCalculation");
            this.chxAutomaticCalculation.Name = "chxAutomaticCalculation";
            this.chxAutomaticCalculation.CheckedChanged += new System.EventHandler(this.cbxAutomaticCalculation_CheckedChanged);
            // 
            // rbtnRoundTo10
            // 
            resources.ApplyResources(this.rbtnRoundTo10, "rbtnRoundTo10");
            this.rbtnRoundTo10.Name = "rbtnRoundTo10";
            this.rbtnRoundTo10.CheckedChanged += new System.EventHandler(this.rbtnRoundTo10_CheckedChanged);
            // 
            // rbtnInitialSchedule
            // 
            resources.ApplyResources(this.rbtnInitialSchedule, "rbtnInitialSchedule");
            this.rbtnInitialSchedule.Checked = true;
            this.rbtnInitialSchedule.Name = "rbtnInitialSchedule";
            this.rbtnInitialSchedule.TabStop = true;
            this.rbtnInitialSchedule.CheckedChanged += new System.EventHandler(this.rbtnInitialSchedule_CheckedChanged);
            // 
            // rbtnRoundTo5
            // 
            resources.ApplyResources(this.rbtnRoundTo5, "rbtnRoundTo5");
            this.rbtnRoundTo5.Name = "rbtnRoundTo5";
            this.rbtnRoundTo5.CheckedChanged += new System.EventHandler(this.rbtnRoundTo5_CheckedChanged);
            // 
            // textBox
            // 
            resources.ApplyResources(this.textBox, "textBox");
            this.textBox.Name = "textBox";
            this.textBox.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.textBox_KeyPress);
            // 
            // dateTimePicker
            // 
            resources.ApplyResources(this.dateTimePicker, "dateTimePicker");
            this.dateTimePicker.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dateTimePicker.Name = "dateTimePicker";
            // 
            // lvSchedule
            // 
            resources.ApplyResources(this.lvSchedule, "lvSchedule");
            this.lvSchedule.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1,
            this.columnHeader2,
            this.columnHeader3,
            this.columnHeader4,
            this.columnHeader5,
            this.columnHeader6});
            this.lvSchedule.DoubleClickActivation = false;
            this.lvSchedule.FullRowSelect = true;
            this.lvSchedule.GridLines = true;
            this.lvSchedule.Name = "lvSchedule";
            this.lvSchedule.UseCompatibleStateImageBehavior = false;
            this.lvSchedule.View = System.Windows.Forms.View.Details;
            this.lvSchedule.SubItemClicked += new OpenCBS.GUI.UserControl.SubItemEventHandler(this.lvSchedule_SubItemClicked);
            this.lvSchedule.SubItemEndEditing += new OpenCBS.GUI.UserControl.SubItemEndEditingEventHandler(this.lvSchedule_SubItemEndEditing);
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
            // columnHeader5
            // 
            resources.ApplyResources(this.columnHeader5, "columnHeader5");
            // 
            // columnHeader6
            // 
            resources.ApplyResources(this.columnHeader6, "columnHeader6");
            // 
            // pnlButtons
            // 
            resources.ApplyResources(this.pnlButtons, "pnlButtons");
            this.pnlButtons.Controls.Add(this.btnCancel);
            this.pnlButtons.Controls.Add(this.btnOK);
            this.pnlButtons.Name = "pnlButtons";
            // 
            // btnCancel
            // 
            resources.ApplyResources(this.btnCancel, "btnCancel");
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // btnOK
            // 
            resources.ApplyResources(this.btnOK, "btnOK");
            this.btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnOK.Name = "btnOK";
            this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
            // 
            // EditContractSchedule
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.splitContainer);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Name = "EditContractSchedule";
            this.splitContainer.Panel1.ResumeLayout(false);
            this.splitContainer.Panel1.PerformLayout();
            this.splitContainer.Panel2.ResumeLayout(false);
            this.splitContainer.ResumeLayout(false);
            this.pnlRounding.ResumeLayout(false);
            this.pnlRounding.PerformLayout();
            this.pnlButtons.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.SplitContainer splitContainer;
        private System.Windows.Forms.TextBox textBox;
        private System.Windows.Forms.DateTimePicker dateTimePicker;
        private OpenCBS.GUI.UserControl.ListViewEx lvSchedule;
        private System.Windows.Forms.ColumnHeader columnHeader1;
        private System.Windows.Forms.ColumnHeader columnHeader2;
        private System.Windows.Forms.ColumnHeader columnHeader3;
        private System.Windows.Forms.ColumnHeader columnHeader4;
        private System.Windows.Forms.ColumnHeader columnHeader5;
        private System.Windows.Forms.ColumnHeader columnHeader6;
        private System.Windows.Forms.Panel pnlButtons;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.Panel pnlRounding;
        private System.Windows.Forms.RadioButton rbtnRoundTo10;
        private System.Windows.Forms.RadioButton rbtnRoundTo5;
        private System.Windows.Forms.RadioButton rbtnInitialSchedule;
        private System.Windows.Forms.CheckBox chxAutomaticCalculation;

    }
}