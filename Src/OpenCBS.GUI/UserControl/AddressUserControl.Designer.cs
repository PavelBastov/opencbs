

namespace OpenCBS.GUI
{
    public partial class AddressUserControl
    {
        private System.Windows.Forms.ComboBox comboBoxDistrict;
        private System.Windows.Forms.Label labelDistrict;
        private OpenCBS.GUI.UserControl.TextBoxLimit tbAddress;
        private System.Windows.Forms.Label labelComments;
        private OpenCBS.GUI.UserControl.TextBoxLimit textBoxCity;
        private System.Windows.Forms.Label labelCity;
        private System.Windows.Forms.Label labelProvince;

        private System.Windows.Forms.ComboBox comboBoxProvince;
        private System.Windows.Forms.Button buttonSave;
        private System.Windows.Forms.Label labelHomePhone;
        private System.Windows.Forms.Label labelPersonalPhone;
        private OpenCBS.GUI.UserControl.TextBoxLimit textBoxHomePhone;
        private OpenCBS.GUI.UserControl.TextBoxLimit textBoxPersonalPhone;
        private OpenCBS.GUI.UserControl.TextBoxLimit textBoxEMail;
        private System.Windows.Forms.Label labelEMail;
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.Container components = null;

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

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AddressUserControl));
            this.comboBoxDistrict = new System.Windows.Forms.ComboBox();
            this.labelDistrict = new System.Windows.Forms.Label();
            this.labelComments = new System.Windows.Forms.Label();
            this.labelCity = new System.Windows.Forms.Label();
            this.labelProvince = new System.Windows.Forms.Label();
            this.comboBoxProvince = new System.Windows.Forms.ComboBox();
            this.buttonSave = new System.Windows.Forms.Button();
            this.labelHomePhone = new System.Windows.Forms.Label();
            this.labelPersonalPhone = new System.Windows.Forms.Label();
            this.labelEMail = new System.Windows.Forms.Label();
            this.labelZipCode = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.comboBoxHomeType = new System.Windows.Forms.ComboBox();
            this.tbZipCode = new OpenCBS.GUI.UserControl.TextBoxLimit();
            this.textBoxEMail = new OpenCBS.GUI.UserControl.TextBoxLimit();
            this.textBoxPersonalPhone = new OpenCBS.GUI.UserControl.TextBoxLimit();
            this.textBoxHomePhone = new OpenCBS.GUI.UserControl.TextBoxLimit();
            this.textBoxCity = new OpenCBS.GUI.UserControl.TextBoxLimit();
            this.tbAddress = new OpenCBS.GUI.UserControl.TextBoxLimit();
            this.SuspendLayout();
            // 
            // comboBoxDistrict
            // 
            this.comboBoxDistrict.BackColor = System.Drawing.Color.White;
            this.comboBoxDistrict.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            resources.ApplyResources(this.comboBoxDistrict, "comboBoxDistrict");
            this.comboBoxDistrict.Name = "comboBoxDistrict";
            this.comboBoxDistrict.SelectionChangeCommitted += new System.EventHandler(this.comboBoxDistrict_SelectionChangeCommitted);
            // 
            // labelDistrict
            // 
            resources.ApplyResources(this.labelDistrict, "labelDistrict");
            this.labelDistrict.BackColor = System.Drawing.Color.Transparent;
            this.labelDistrict.Name = "labelDistrict";
            // 
            // labelComments
            // 
            resources.ApplyResources(this.labelComments, "labelComments");
            this.labelComments.BackColor = System.Drawing.Color.Transparent;
            this.labelComments.Name = "labelComments";
            // 
            // labelCity
            // 
            resources.ApplyResources(this.labelCity, "labelCity");
            this.labelCity.BackColor = System.Drawing.Color.Transparent;
            this.labelCity.Name = "labelCity";
            // 
            // labelProvince
            // 
            resources.ApplyResources(this.labelProvince, "labelProvince");
            this.labelProvince.BackColor = System.Drawing.Color.Transparent;
            this.labelProvince.Name = "labelProvince";
            // 
            // comboBoxProvince
            // 
            this.comboBoxProvince.BackColor = System.Drawing.Color.White;
            this.comboBoxProvince.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            resources.ApplyResources(this.comboBoxProvince, "comboBoxProvince");
            this.comboBoxProvince.Name = "comboBoxProvince";
            this.comboBoxProvince.Sorted = true;
            this.comboBoxProvince.SelectionChangeCommitted += new System.EventHandler(this.comboBoxProvince_SelectionChangeCommitted);
            // 
            // buttonSave
            // 
            resources.ApplyResources(this.buttonSave, "buttonSave");
            this.buttonSave.Name = "buttonSave";
            this.buttonSave.Click += new System.EventHandler(this.buttonSave_Click);
            // 
            // labelHomePhone
            // 
            resources.ApplyResources(this.labelHomePhone, "labelHomePhone");
            this.labelHomePhone.Name = "labelHomePhone";
            // 
            // labelPersonalPhone
            // 
            resources.ApplyResources(this.labelPersonalPhone, "labelPersonalPhone");
            this.labelPersonalPhone.Name = "labelPersonalPhone";
            // 
            // labelEMail
            // 
            resources.ApplyResources(this.labelEMail, "labelEMail");
            this.labelEMail.Name = "labelEMail";
            // 
            // labelZipCode
            // 
            resources.ApplyResources(this.labelZipCode, "labelZipCode");
            this.labelZipCode.Name = "labelZipCode";
            // 
            // label1
            // 
            resources.ApplyResources(this.label1, "label1");
            this.label1.Name = "label1";
            // 
            // comboBoxHomeType
            // 
            this.comboBoxHomeType.BackColor = System.Drawing.Color.White;
            this.comboBoxHomeType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            resources.ApplyResources(this.comboBoxHomeType, "comboBoxHomeType");
            this.comboBoxHomeType.Name = "comboBoxHomeType";
            this.comboBoxHomeType.SelectionChangeCommitted += new System.EventHandler(this.comboBoxHomeType_SelectionChangeCommitted);
            // 
            // tbZipCode
            // 
            resources.ApplyResources(this.tbZipCode, "tbZipCode");
            this.tbZipCode.Name = "tbZipCode";
            this.tbZipCode.TextChanged += new System.EventHandler(this.textBoxHomeType_TextChanged);
            // 
            // textBoxEMail
            // 
            resources.ApplyResources(this.textBoxEMail, "textBoxEMail");
            this.textBoxEMail.Name = "textBoxEMail";
            this.textBoxEMail.TextChanged += new System.EventHandler(this.textBoxEMail_TextChanged);
            // 
            // textBoxPersonalPhone
            // 
            resources.ApplyResources(this.textBoxPersonalPhone, "textBoxPersonalPhone");
            this.textBoxPersonalPhone.Name = "textBoxPersonalPhone";
            this.textBoxPersonalPhone.TextChanged += new System.EventHandler(this.textBoxPersonalPhone_TextChanged);
            // 
            // textBoxHomePhone
            // 
            resources.ApplyResources(this.textBoxHomePhone, "textBoxHomePhone");
            this.textBoxHomePhone.Name = "textBoxHomePhone";
            this.textBoxHomePhone.TextChanged += new System.EventHandler(this.textBoxHomePhone_TextChanged);
            // 
            // textBoxCity
            // 
            resources.ApplyResources(this.textBoxCity, "textBoxCity");
            this.textBoxCity.Name = "textBoxCity";
            this.textBoxCity.TextChanged += new System.EventHandler(this.textBoxCity_TextChanged);
            // 
            // tbAddress
            // 
            this.tbAddress.AcceptsReturn = true;
            resources.ApplyResources(this.tbAddress, "tbAddress");
            this.tbAddress.Name = "tbAddress";
            this.tbAddress.TextChanged += new System.EventHandler(this.textBoxComments_TextChanged);
            // 
            // AddressUserControl
            // 
            resources.ApplyResources(this, "$this");
            this.Controls.Add(this.comboBoxHomeType);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.tbZipCode);
            this.Controls.Add(this.labelZipCode);
            this.Controls.Add(this.labelEMail);
            this.Controls.Add(this.textBoxEMail);
            this.Controls.Add(this.textBoxPersonalPhone);
            this.Controls.Add(this.textBoxHomePhone);
            this.Controls.Add(this.labelPersonalPhone);
            this.Controls.Add(this.labelHomePhone);
            this.Controls.Add(this.buttonSave);
            this.Controls.Add(this.textBoxCity);
            this.Controls.Add(this.comboBoxDistrict);
            this.Controls.Add(this.labelDistrict);
            this.Controls.Add(this.tbAddress);
            this.Controls.Add(this.labelComments);
            this.Controls.Add(this.labelCity);
            this.Controls.Add(this.comboBoxProvince);
            this.Controls.Add(this.labelProvince);
            this.Name = "AddressUserControl";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        private System.Windows.Forms.Label labelZipCode;
        private OpenCBS.GUI.UserControl.TextBoxLimit tbZipCode;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox comboBoxHomeType;
    }
}
