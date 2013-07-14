namespace OpenCBS.GUI.Contracts
{
    partial class ManualScheduleForm
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
            this.pnlButtons = new System.Windows.Forms.Panel();
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnOK = new System.Windows.Forms.Button();
            this.olvSchedule = new BrightIdeasSoftware.ObjectListView();
            this.numberColumn = ((BrightIdeasSoftware.OLVColumn)(new BrightIdeasSoftware.OLVColumn()));
            this.dateColumn = ((BrightIdeasSoftware.OLVColumn)(new BrightIdeasSoftware.OLVColumn()));
            this.interestColumn = ((BrightIdeasSoftware.OLVColumn)(new BrightIdeasSoftware.OLVColumn()));
            this.principalColumn = ((BrightIdeasSoftware.OLVColumn)(new BrightIdeasSoftware.OLVColumn()));
            this.totalColumn = ((BrightIdeasSoftware.OLVColumn)(new BrightIdeasSoftware.OLVColumn()));
            this.olbColumn = ((BrightIdeasSoftware.OLVColumn)(new BrightIdeasSoftware.OLVColumn()));
            this.paidInterestColumn = ((BrightIdeasSoftware.OLVColumn)(new BrightIdeasSoftware.OLVColumn()));
            this.paidPrincipalColumn = ((BrightIdeasSoftware.OLVColumn)(new BrightIdeasSoftware.OLVColumn()));
            this.paymentDateColumn = ((BrightIdeasSoftware.OLVColumn)(new BrightIdeasSoftware.OLVColumn()));
            this.pnlButtons.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.olvSchedule)).BeginInit();
            this.SuspendLayout();
            // 
            // pnlButtons
            // 
            this.pnlButtons.Controls.Add(this.btnCancel);
            this.pnlButtons.Controls.Add(this.btnOK);
            this.pnlButtons.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.pnlButtons.Location = new System.Drawing.Point(0, 325);
            this.pnlButtons.Name = "pnlButtons";
            this.pnlButtons.Size = new System.Drawing.Size(860, 50);
            this.pnlButtons.TabIndex = 33;
            // 
            // btnCancel
            // 
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Font = new System.Drawing.Font("Arial", 9.75F);
            this.btnCancel.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.btnCancel.Location = new System.Drawing.Point(745, 13);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(103, 25);
            this.btnCancel.TabIndex = 34;
            this.btnCancel.Text = "Cancel";
            // 
            // btnOK
            // 
            this.btnOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnOK.Font = new System.Drawing.Font("Arial", 9.75F);
            this.btnOK.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.btnOK.Location = new System.Drawing.Point(626, 13);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(103, 25);
            this.btnOK.TabIndex = 33;
            this.btnOK.Text = "Ok";
            // 
            // olvSchedule
            // 
            this.olvSchedule.Alignment = System.Windows.Forms.ListViewAlignment.Default;
            this.olvSchedule.AllColumns.Add(this.numberColumn);
            this.olvSchedule.AllColumns.Add(this.dateColumn);
            this.olvSchedule.AllColumns.Add(this.interestColumn);
            this.olvSchedule.AllColumns.Add(this.principalColumn);
            this.olvSchedule.AllColumns.Add(this.totalColumn);
            this.olvSchedule.AllColumns.Add(this.olbColumn);
            this.olvSchedule.AllColumns.Add(this.paidInterestColumn);
            this.olvSchedule.AllColumns.Add(this.paidPrincipalColumn);
            this.olvSchedule.AllColumns.Add(this.paymentDateColumn);
            this.olvSchedule.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.numberColumn,
            this.dateColumn,
            this.interestColumn,
            this.principalColumn,
            this.totalColumn,
            this.olbColumn,
            this.paidInterestColumn,
            this.paidPrincipalColumn,
            this.paymentDateColumn});
            this.olvSchedule.Dock = System.Windows.Forms.DockStyle.Fill;
            this.olvSchedule.Font = new System.Drawing.Font("Arial", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.olvSchedule.FullRowSelect = true;
            this.olvSchedule.GridLines = true;
            this.olvSchedule.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
            this.olvSchedule.HeaderWordWrap = true;
            this.olvSchedule.Location = new System.Drawing.Point(0, 0);
            this.olvSchedule.MultiSelect = false;
            this.olvSchedule.Name = "olvSchedule";
            this.olvSchedule.ShowGroups = false;
            this.olvSchedule.Size = new System.Drawing.Size(860, 325);
            this.olvSchedule.TabIndex = 34;
            this.olvSchedule.UseCompatibleStateImageBehavior = false;
            this.olvSchedule.View = System.Windows.Forms.View.Details;
            this.olvSchedule.CellEditFinishing += new BrightIdeasSoftware.CellEditEventHandler(this.HandleCellEditFinishing);
            this.olvSchedule.CellEditStarting += new BrightIdeasSoftware.CellEditEventHandler(this.HandleCellEditStarting);
            // 
            // numberColumn
            // 
            this.numberColumn.AspectName = "Number";
            this.numberColumn.HeaderTextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.numberColumn.IsEditable = false;
            this.numberColumn.Text = "#";
            this.numberColumn.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.numberColumn.Width = 70;
            // 
            // dateColumn
            // 
            this.dateColumn.AspectName = "ExpectedDate";
            this.dateColumn.HeaderTextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.dateColumn.Text = "Date";
            this.dateColumn.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.dateColumn.Width = 90;
            // 
            // interestColumn
            // 
            this.interestColumn.AspectName = "InterestsRepayment";
            this.interestColumn.HeaderTextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.interestColumn.Text = "Interest";
            this.interestColumn.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.interestColumn.Width = 100;
            // 
            // principalColumn
            // 
            this.principalColumn.AspectName = "CapitalRepayment";
            this.principalColumn.HeaderTextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.principalColumn.Text = "Principal";
            this.principalColumn.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.principalColumn.Width = 100;
            // 
            // totalColumn
            // 
            this.totalColumn.AspectName = "AmountHasToPayWithInterest";
            this.totalColumn.HeaderTextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.totalColumn.IsEditable = false;
            this.totalColumn.Text = "Total";
            this.totalColumn.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.totalColumn.Width = 100;
            // 
            // olbColumn
            // 
            this.olbColumn.AspectName = "OLB";
            this.olbColumn.HeaderTextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.olbColumn.IsEditable = false;
            this.olbColumn.Text = "OLB";
            this.olbColumn.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.olbColumn.Width = 100;
            // 
            // paidInterestColumn
            // 
            this.paidInterestColumn.AspectName = "PaidInterests";
            this.paidInterestColumn.HeaderTextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.paidInterestColumn.IsEditable = false;
            this.paidInterestColumn.Text = "Paid interest";
            this.paidInterestColumn.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.paidInterestColumn.Width = 100;
            // 
            // paidPrincipalColumn
            // 
            this.paidPrincipalColumn.AspectName = "PaidCapital";
            this.paidPrincipalColumn.HeaderTextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.paidPrincipalColumn.IsEditable = false;
            this.paidPrincipalColumn.Text = "Paid principal";
            this.paidPrincipalColumn.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.paidPrincipalColumn.Width = 100;
            // 
            // paymentDateColumn
            // 
            this.paymentDateColumn.AspectName = "PaidDate";
            this.paymentDateColumn.HeaderTextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.paymentDateColumn.IsEditable = false;
            this.paymentDateColumn.Text = "Payment date";
            this.paymentDateColumn.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.paymentDateColumn.Width = 90;
            // 
            // ManualScheduleForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(860, 375);
            this.Controls.Add(this.olvSchedule);
            this.Controls.Add(this.pnlButtons);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ManualScheduleForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Manual schedule";
            this.pnlButtons.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.olvSchedule)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel pnlButtons;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Button btnOK;
        private BrightIdeasSoftware.ObjectListView olvSchedule;
        private BrightIdeasSoftware.OLVColumn numberColumn;
        private BrightIdeasSoftware.OLVColumn dateColumn;
        private BrightIdeasSoftware.OLVColumn interestColumn;
        private BrightIdeasSoftware.OLVColumn principalColumn;
        private BrightIdeasSoftware.OLVColumn totalColumn;
        private BrightIdeasSoftware.OLVColumn olbColumn;
        private BrightIdeasSoftware.OLVColumn paidInterestColumn;
        private BrightIdeasSoftware.OLVColumn paidPrincipalColumn;
        private BrightIdeasSoftware.OLVColumn paymentDateColumn;
    }
}