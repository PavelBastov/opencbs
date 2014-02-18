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
using System.ComponentModel.Composition;
using System.Drawing;
using System.Windows.Forms;
using OpenCBS.CoreDomain;
using OpenCBS.CoreDomain.Clients;
using OpenCBS.CoreDomain.Contracts.Savings;
using OpenCBS.Enums;
using OpenCBS.ExceptionsHandler;
using OpenCBS.Extensions;
using OpenCBS.GUI.Clients;
using OpenCBS.GUI.Tools;
using OpenCBS.MultiLanguageRessources;
using OpenCBS.Services;
using OpenCBS.Services.Events;
using OpenCBS.Shared;
using OpenCBS.Shared.Settings;

namespace OpenCBS.GUI.UserControl
{
	public partial class PersonUserControl : ClientControl
	{
		private Person _tempPerson;
        private bool _personSaved;

        [ImportMany(typeof(IPersonTabs), RequiredCreationPolicy = CreationPolicy.NonShared)]
        public List<IPersonTabs> Extensions { get; set; }

        public event EventHandler AddSelectedSaving;
        public event EventHandler ViewSelectedSaving;

	    private Form _mdiParent;

        public System.Windows.Forms.UserControl PanelSavings
        {
            get { return clSavingList; }
        }

        public bool ButtonAddSavingsEnabled
        {
            get { return clSavingList.ButtonAddSavingsEnabled; }
            set { clSavingList.ButtonAddSavingsEnabled = value; }
        }

        public void RemoveSavings()
        {
            tabControlEconomicInfo.TabPages.Remove(tabPageSavings);
        }

        public PersonUserControl(Person person, Form pMdiParent)
        {
           _mdiParent = pMdiParent;
            Initialization();
            _tempPerson = person;
            InitializePerson();
            InitializeGroup();
            DisplayProjects(person.Projects);
            DisplaySavings(person.Savings);
            _tempPerson.DateOfBirth = person.DateOfBirth; //new DateTime(1980,1,1);
            tabControlEconomicInfo.TabPages.Remove(tabPage1FollowUp);
            textBoxIdentificationData.ReadOnly = ServicesProvider.GetInstance().GetGeneralSettings().IsAutomaticID;
            if (ServicesProvider.GetInstance().GetGeneralSettings().IsAutomaticID)
                textBoxIdentificationData.BackColor = Color.WhiteSmoke;
        }
 
	    public bool PersonSaved
		{
			get { return _personSaved; }
		}

		public Person Person
		{
			get { return _tempPerson; }

			set
			{
				_tempPerson = value;
				if (_tempPerson == null)
				{
					ResetAllComponents();
				}
				else
				{
					InitializePerson();
                    InitializeGroup();
				}
			}
		}

        public void DisplaySavings(IEnumerable<ISavingsContract> pSavings)
        {
            clSavingList.DisplaySavings(pSavings);
        }

        public void DisplayProjects(IEnumerable<Project> pProjects)
        {
            if (ServicesProvider.GetInstance().GetGeneralSettings().UseProjects)
            {
                listViewProjects.Items.Clear();
                foreach (Project project in pProjects)
                {
                    ListViewItem item = new ListViewItem(project.Id.ToString());
                    item.Tag = project;
                    item.SubItems.Add(project.Code);
                    item.SubItems.Add(project.Name);
                    item.SubItems.Add(project.Credits.Count.ToString());
                    //item.SubItems.Add(project.Guarantees.Count.ToString());
                    listViewProjects.Items.Add(item);
                }
            }
            else
            {
                tabControlEconomicInfo.TabPages.Remove(tabPageProjects);
            }
        }

		private void InitializeUserControlsAddress()
		{
			addressUserControlFirst = new AddressUserControl();
            addressUserControlFirst.TextBoxHomePhoneText = MultiLanguageStrings.GetString(Ressource.PersonUserControl, "Homephone.Text");          
            addressUserControlFirst.TextBoxPersonalPhoneText = MultiLanguageStrings.GetString(Ressource.PersonUserControl, "Personalphone.Text");
			addressUserControlFirst.Dock = DockStyle.Fill;
			groupBoxFirstAddress.Controls.Add(addressUserControlFirst);

            addressUserControlSecondaryAddress = new AddressUserControl();
            addressUserControlSecondaryAddress.TextBoxHomePhoneText =
                MultiLanguageStrings.GetString(Ressource.PersonUserControl, "Businesscellphone.Text");
            addressUserControlSecondaryAddress.TextBoxPersonalPhoneText =
                MultiLanguageStrings.GetString(Ressource.PersonUserControl, "Businessphone.Text");
			addressUserControlSecondaryAddress.Dock = DockStyle.Fill;
			groupBoxSecondaryAddress.Controls.Add(addressUserControlSecondaryAddress);
		}

       private void Initialization()
		{
            InitializeComponent();
		    ApplicationSettings generalSettings = ServicesProvider.GetInstance().GetGeneralSettings();
            textBoxNationality.Text = generalSettings.Country;

            //InitializeSecurity();
			InitializeUserControlsAddress();
            _InitializeCombobox();
			_personSaved = false;
			_tempPerson = new Person();
            _tempPerson.Nationality = generalSettings.Country;
			InitializeSex();
		    dateTimePickerDateOfBirth.Value = DateTime.Now;
            _tempPerson.DateOfBirth = DateTime.Now; 
            _tempPerson.LoanCycle = 0;
		    InitPrintButton();

            foreach (Branch branch in User.CurrentUser.Branches)
            {
                cbBranch.Items.Add(branch);
            }
		}

        private void InitializeGroup()
        {
            var membership = ServicesProvider.GetInstance().GetClientServices().GetGroupMembership(_tempPerson.Id);
            listViewGroup.Items.Clear();
            foreach (var line in membership)
            {
                var item = new ListViewItem(line.Name);
                item.SubItems.Add(line.Type);
                item.SubItems.Add(line.EstablishedAt.ToShortDateString());
                item.SubItems.Add(line.JoinedAt.ToShortDateString());
                if (line.LeftAt.HasValue) item.SubItems.Add(line.LeftAt.Value.ToShortDateString());
                item.Tag = new object[]
                {
                    line.Id,
                    "SG" == line.Type ? OClientTypes.Group : OClientTypes.Village
                };
                listViewGroup.Items.Add(item);
            }
        }

        private void _InitializeCombobox()
        {
            _InitializeSponsor();
            _InitializeSponsor2();            
       }

	    private void _DisplayAllGroups(List<IClient> listAllGroups)
	    {
            //tabControlEconomicInfo.TabPages.Remove(tabPageGroupMember);
            if (listAllGroups.Count != 0)
	        {
	            //tabControlEconomicInfo.TabPages.Add(tabPageGroupMember);
                listViewGroup.Items.Clear();
                foreach (IClient client in listAllGroups)
                {
                    ListViewItem item = new ListViewItem(client.Name) { Tag = client };
                    if (client is Group)
                    {
                        Group group = (Group) client;
                        item.SubItems.Add("SG");
                        if (group.EstablishmentDate.HasValue) item.SubItems.Add(group.EstablishmentDate.Value.ToShortDateString()); else item.SubItems.Add("-");

                        List<Member> members = ServicesProvider.GetInstance().GetClientServices().FindGroupHistoryPersons(group.Id);
                        //string joinedDate = "-";
                        //string leftDate = "-";
                        foreach (Member member in members)
                        {
                            if (member.Tiers.Id == _tempPerson.Id)
                            {
                                item.SubItems.Add(member.JoinedDate.ToShortDateString());
                                if (member.LeftDate.HasValue) 
                                    item.SubItems.Add(member.LeftDate.Value.ToShortDateString()); 
                                else item.SubItems.Add("-");

                                listViewGroup.Items.Add(item);                                
                                break;
                            }
                        }
                        //item.SubItems.Add(joinedDate);
                        //item.SubItems.Add(leftDate);
                    }
                    else if (client is Village)
                    {
                        Village village = (Village) client;
                        item.SubItems.Add("NSG");
                        if (village.EstablishmentDate.HasValue) item.SubItems.Add(village.EstablishmentDate.Value.ToShortDateString()); else item.SubItems.Add("-");

                        List<VillageMember> members = ServicesProvider.GetInstance().GetClientServices().FindVillageHistoryPersons(village.Id);
                        //string joinedDate = "-";
                        //string leftDate = "-";
                        foreach (VillageMember member in members)
                        {
                            if (member.Tiers.Id == _tempPerson.Id)
                            {
                                item.SubItems.Add(member.JoinedDate.ToShortDateString());
                                if (member.LeftDate.HasValue) 
                                    item.SubItems.Add(member.LeftDate.Value.ToShortDateString()); 
                                else item.SubItems.Add("-");

                                listViewGroup.Items.Add(item);                                
                                break;
                            }
                        }
                        //item.SubItems.Add(joinedDate);
                        //item.SubItems.Add(leftDate);
                    }
                }
            }
	    }

	    private void InitializeSex()
		{
			comboBoxSex.LoadGender();
		}

	

		private void InitializePerson()
		{
            if (_tempPerson.Id != 0)
            {
                buttonSave.Text = MultiLanguageStrings.GetString(Ressource.PersonUserControl, "Update");
                textBoxLastname.Text = _tempPerson.LastName;
                textBoxFirstName.Text = _tempPerson.FirstName;
                textBoxIdentificationData.Text = _tempPerson.IdentificationData;
                dateTimePickerFirstContact.Text = ServicesHelper.ConvertNullableDateTimeToString(_tempPerson.FirstContact);
                dateTimePickerFirstAppointment.Text = ServicesHelper.ConvertNullableDateTimeToString(_tempPerson.FirstAppointment);
                dateTimePickerDateOfBirth.Text = ServicesHelper.ConvertNullableDateTimeToString(_tempPerson.DateOfBirth);
                checkBoxHeadOfHousehold.Checked = _tempPerson.HouseHoldHead;
                textBoxLoanCycle.Text = _tempPerson.LoanCycle.ToString();
                textBoxFatherName.Text = _tempPerson.FatherName;
                textBoxBirthPlace.Text = _tempPerson.BirthPlace ?? String.Empty;
                textBoxNationality.Text = _tempPerson.Nationality ?? ServicesProvider.GetInstance().GetGeneralSettings().Country;
                richTextBoxCommentsActivity.Text = _tempPerson.FollowUpComment;
                
                comboBoxSex.SelectGender(_tempPerson.Sex);
                
                comboBoxSex.Text = _tempPerson.Sex.ToString();
                eacPerson.Activity = _tempPerson.Activity;
                
                comboBoxSponsor1.Text = _tempPerson.Sponsor1;
                comboBoxSponsor2.Text = _tempPerson.Sponsor2;
                textBoxSponsor1.Text = _tempPerson.Sponsor1Comment;
                textBoxSponsor2.Text = _tempPerson.Sponsor2Comment;

                addressUserControlFirst.ZipCode = _tempPerson.ZipCode;
                addressUserControlFirst.HomeType = _tempPerson.HomeType;
                addressUserControlFirst.Email = _tempPerson.Email;
                addressUserControlFirst.District = _tempPerson.District;
                addressUserControlFirst.City = _tempPerson.City;
                addressUserControlFirst.Comments = _tempPerson.Address;
                addressUserControlFirst.HomePhone = _tempPerson.HomePhone ?? String.Empty;
                addressUserControlFirst.PersonalPhone = _tempPerson.PersonalPhone ?? String.Empty;

                if (!_tempPerson.SecondaryAddressIsEmpty)
                {
                    addressUserControlSecondaryAddress.ZipCode = _tempPerson.SecondaryZipCode;
                    addressUserControlSecondaryAddress.HomeType = _tempPerson.SecondaryHomeType;
                    addressUserControlSecondaryAddress.Email = _tempPerson.SecondaryEmail;
                    addressUserControlSecondaryAddress.District = _tempPerson.SecondaryDistrict;
                    addressUserControlSecondaryAddress.City = _tempPerson.SecondaryCity;
                    addressUserControlSecondaryAddress.Comments = _tempPerson.SecondaryAddress;
                    addressUserControlSecondaryAddress.HomePhone = _tempPerson.SecondaryHomePhone ?? String.Empty;
                    addressUserControlSecondaryAddress.PersonalPhone = _tempPerson.SecondaryPersonalPhone ?? String.Empty;
                }
                //_InitializeCustomizeFields();
                DisplayProjects(_tempPerson.Projects);

                cbBranch.SelectedItem = _tempPerson.Branch;
            }
            else
            {
                _tempPerson.LoanCycle = 0;
                if (cbBranch.Items.Count > 0) cbBranch.SelectedIndex = 0;
            }
            InitializePhoto();
		}

		public void ResetAllComponents()
		{
			_tempPerson = new Person();
			addressUserControlFirst.ResetAllComponents();
			addressUserControlSecondaryAddress.ResetAllComponents();
			textBoxFirstName.Text = string.Empty;
			textBoxLastname.Text = string.Empty;
			pictureBox.Image = null;
            dateTimePickerDateOfBirth.Value = new DateTime(1980, 1, 1);
            dateTimePickerFirstContact.Value = TimeProvider.Today;
			textBoxIdentificationData.Text = string.Empty;
			
			checkBoxHeadOfHousehold.Checked = false;
			richTextBoxCommentsActivity.Text = string.Empty;

		    eacPerson.Activity = null;
            textBoxBirthPlace.Clear();
            
            textBoxNationality.Clear();
        }

		private void RecoverDatasFromUserControlsAddress()
		{
            _tempPerson.ZipCode = addressUserControlFirst.ZipCode;
            _tempPerson.HomeType = addressUserControlFirst.HomeType;
            _tempPerson.Email = addressUserControlFirst.Email;
			_tempPerson.District = addressUserControlFirst.District;
			_tempPerson.City = addressUserControlFirst.City;
			_tempPerson.Address = addressUserControlFirst.Comments;
            _tempPerson.HomePhone = addressUserControlFirst.HomePhone;
            _tempPerson.PersonalPhone = addressUserControlFirst.PersonalPhone;

            _tempPerson.SecondaryZipCode = addressUserControlSecondaryAddress.ZipCode;
            _tempPerson.SecondaryHomeType = addressUserControlSecondaryAddress.HomeType;
            _tempPerson.SecondaryEmail = addressUserControlSecondaryAddress.Email;
			_tempPerson.SecondaryDistrict = addressUserControlSecondaryAddress.District;
			_tempPerson.SecondaryCity = addressUserControlSecondaryAddress.City;
			_tempPerson.SecondaryAddress = addressUserControlSecondaryAddress.Comments;
            _tempPerson.SecondaryHomePhone = addressUserControlSecondaryAddress.HomePhone;
            _tempPerson.SecondaryPersonalPhone = addressUserControlSecondaryAddress.PersonalPhone;
		}

        private void RecoverDataFromCombobox()
        {
            _tempPerson.Sponsor1 = comboBoxSponsor1.Text;
            _tempPerson.Sponsor2 = comboBoxSponsor2.Text;            
        }

		private void SavePerson()
		{
			_personSaved = false;
			RecoverDatasFromUserControlsAddress();
		    RecoverDataFromCombobox();

		    _tempPerson.FirstContact = dateTimePickerFirstContact.Value;
            _tempPerson.FirstAppointment = dateTimePickerFirstAppointment.Value;
		    _tempPerson.Branch = (Branch) cbBranch.SelectedItem;

		    try
            {
                bool save = 0 == _tempPerson.Id;
                if (_tempPerson.FirstName != null)
                    _tempPerson.FirstName = _tempPerson.FirstName.Trim();
                if (_tempPerson.LastName != null)
                    _tempPerson.LastName = _tempPerson.LastName.Trim();
                if (_tempPerson.FatherName != null)
                    _tempPerson.FatherName = _tempPerson.FatherName.Trim();
                if (_tempPerson.IdentificationData != null)
                    _tempPerson.IdentificationData = _tempPerson.IdentificationData.Trim();

                string result = ServicesProvider
                    .GetInstance()
                    .GetClientServices()
                    .SavePerson(ref _tempPerson, (tx, id) =>
                    {
                        foreach (var extension in Extensions) extension.Save(_tempPerson, tx);
                    });

                EventProcessorServices es = ServicesProvider.GetInstance().GetEventProcessorServices();
                es.LogClientSaveUpdateEvent(_tempPerson, save);

                if (result != string.Empty)
                    MessageBox.Show(result, "", MessageBoxButtons.OK, MessageBoxIcon.Information);
                
                _personSaved = true;
                buttonSave.Text = MultiLanguageStrings.GetString(Ressource.PersonUserControl, "Update");

                var formatInfo = new NameFormatInfo();
                var settings = ServicesProvider.GetInstance().GetGeneralSettings();
                var fnFormat = @"{0:" + settings.FirstNameFormat + @"}";
                var lnFormat = @"{0:" + settings.LastNameFormat + @"}";
                textBoxFirstName.Text = string.Format(_tempPerson.FirstName);
                textBoxLastname.Text = string.Format(_tempPerson.LastName);
                if (_tempPerson.FatherName != null)
                    textBoxFatherName.Text = string.Format(_tempPerson.FatherName);
                textBoxIdentificationData.Text = string.Format(_tempPerson.IdentificationData);
                //EnableDocuments();
                ResetImagesFlags();

                if (ApplicationSettings.GetInstance(User.CurrentUser.Md5).UseMandatorySavingAccount &&
                    Person.Savings.Count == 0)
                {
                    Person.Savings.Add(
                        ServicesProvider.GetInstance().GetSavingServices().AddAndActivateDefaultSavingAccount(Person));
                    DisplaySavings(Person.Savings);
                }
            }
            catch (Exception ex)
            {
                new frmShowError(CustomExceptionHandler.ShowExceptionText(ex)).ShowDialog();
            }
		}

        private void ResetImagesFlags()
        {
            _tempPerson.IsImage2Added = false;
            _tempPerson.IsImage2Deleted = false;
            _tempPerson.IsImage2Updated = false;
            _tempPerson.IsImageAdded = false;
            _tempPerson.IsImageDeleted = false;
            _tempPerson.IsImageUpdated = false;
        }

	    private void checkBoxHeadOfHousehold_CheckedChanged(object sender, EventArgs e)
		{
			_tempPerson.HouseHoldHead = checkBoxHeadOfHousehold.Checked;
		}

		private void dateTimePickerDateOfBirth_ValueChanged(object sender, EventArgs e)
		{
            if (dateTimePickerDateOfBirth.Value.Date > DateTime.Now.Date)
                dateTimePickerDateOfBirth.Value = DateTime.Now;

			_tempPerson.DateOfBirth = dateTimePickerDateOfBirth.Value.Date;

            int year = DateTime.Now.Year - dateTimePickerDateOfBirth.Value.Year;

            if ((DateTime.Now.Month - dateTimePickerDateOfBirth.Value.Month == 0 && DateTime.Now.Day - dateTimePickerDateOfBirth.Value.Day < 0)
                || DateTime.Now.Month - dateTimePickerDateOfBirth.Value.Month < 0)
                --year;

            labelRangeOfAge.Text = year
                   + @" " + MultiLanguageStrings.GetString(Ressource.PersonUserControl, "Age"); 
        }

		private void textBoxLastname_TextChanged(object sender, EventArgs e)
		{
			_tempPerson.LastName = ServicesHelper.CheckTextBoxText(textBoxLastname.Text);
		}

		private void textBoxFirstName_TextChanged(object sender, EventArgs e)
		{
			_tempPerson.FirstName = ServicesHelper.CheckTextBoxText(textBoxFirstName.Text);
		}

		private void comboBoxSex_SelectionChangeCommitted(object sender, EventArgs e)
		{
		    _tempPerson.Sex = comboBoxSex.GetGender();
		}

		private void buttonSave_Click(object sender, EventArgs e)
		{
			SavePerson();
			if (ButtonSaveClick != null)
				ButtonSaveClick(this, e);
		}

	    private void buttonCancel_Click(object sender, EventArgs e)
		{
			if (ButtonCancelClick != null)
				ButtonCancelClick(this, e);
		}

		
		private void PersonUserControl_Load(object sender, EventArgs e)
		{
            Tabs = tabControlEconomicInfo;
            Client = _tempPerson;
            InitDocuments();

			groupBoxFirstAddress.Size = new Size(tabPageAddress.Width/2, tabPageAddress.Height);
		    
            if (!ServicesProvider.GetInstance().GetGeneralSettings().UseProjects)
                ButtonAddProjectClick(buttonViewProject, null);

		    LoadExtensions();
		}

        private void LoadExtensions()
        {
            foreach (var extension in Extensions)
            {
                var pages = extension.GetTabPages(_tempPerson);
                if (pages == null) continue;
                tabControlEconomicInfo.TabPages.AddRange(pages);
            }
        }

	    private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
		    int photoSubId;
            if (((LinkLabel)sender).Name == "changePhotoLinkLabel")
                photoSubId = 0;//it means that we want to get 1st picture
            else
                photoSubId = 1;//it means that we want to get 2nd picture
            ShowPictureForm pictureForm = new ShowPictureForm(Person, photoSubId, this);
            pictureForm.ShowDialog();
		}

		public void InitializePhoto()
		{
		    PicturesServices ps = ServicesProvider.GetInstance().GetPicturesServices();
            if (!ps.IsEnabled())
            {
                pictureBox.Visible = false;
                pictureBox2.Visible = false;
                changePhotoLinkLabel.Visible = false;
                changePhotoLinkLabel2.Visible = false;
                return;
            }

            int photoSubId = 0;//it means that we want to get 1st picture
            pictureBox.Image = ps.GetPicture(_tempPerson.Id, photoSubId, true);
		    pictureBox.SizeMode = PictureBoxSizeMode.Zoom;
		    photoSubId = 1;//it means that we want to get 2nd picture
            pictureBox2.Image = ps.GetPicture(_tempPerson.Id, photoSubId, true);
		    pictureBox2.SizeMode = PictureBoxSizeMode.Zoom;
		}

        private void textBoxLoanCycle_TextChanged(object sender, EventArgs e)
        {
            int? loanCycle = ServicesHelper.ConvertStringToNullableInt32(textBoxLoanCycle.Text);
            if (!loanCycle.HasValue)
            {
                textBoxLoanCycle.BackColor = Color.Red;
                _tempPerson.LoanCycle = 0;
            }
            else
            {
                textBoxLoanCycle.BackColor = Color.White;
                _tempPerson.LoanCycle = loanCycle.Value;
            }
        }

        private void pictureBox_Click(object sender, EventArgs e)
        {
            int photoSubId=0;
            if (sender is PictureBox)
            {
                if (((PictureBox)sender).Name == "pictureBox2")
                    photoSubId = 1;
            }
            else if (sender is LinkLabel)
                if (((LinkLabel)sender).Name == "changePhotoLinkLabel2")
                    photoSubId = 1;

            ShowPictureForm showPictureForm = new ShowPictureForm(_tempPerson, photoSubId, this);
            showPictureForm.ShowDialog();
        }

        private void listViewGroup_DoubleClick(object sender, EventArgs e)
        {
            var data = (object[]) listViewGroup.SelectedItems[0].Tag;
            var clientId = (int) data[0];
            var clientType = (OClientTypes) data[1];

            var client = ServicesProvider.GetInstance().GetClientServices().FindTiers(clientId, clientType);
            if (client is Group)
            {
                var form = new ClientForm((Group)client, _mdiParent);
                form.ShowDialog();
            }
            else if (client is Village)
            {
                var form = new NonSolidaryGroupForm((Village)client);
                form.ShowDialog();
            }
        }

        private void textBoxFatherName_TextChanged(object sender, EventArgs e)
        {
            _tempPerson.FatherName = ServicesHelper.CheckTextBoxText(textBoxFatherName.Text);
        }

        private void buttonAddProject_Click(object sender, EventArgs e)
        {
            if (ButtonAddProjectClick != null)
                ButtonAddProjectClick(this, e);
            DisplayProjects(_tempPerson.Projects);
        }

        private void _ViewProject(EventArgs e)
        {
            if (listViewProjects.SelectedItems.Count != 0)
            {
                Project project = (Project)listViewProjects.SelectedItems[0].Tag;
                if (ViewProject != null)
                    ViewProject(project, e);
            }
        }

	    private void buttonViewProject_Click(object sender, EventArgs e)
        {
            _ViewProject(e);
        }

        private void listViewProjects_DoubleClick(object sender, EventArgs e)
        {
            _ViewProject(e);
        }

        private void textBoxBirthPlace_TextChanged(object sender, EventArgs e)
        {
            _tempPerson.BirthPlace = ServicesHelper.CheckTextBoxText(textBoxBirthPlace.Text);
        }

       
        private void richTextBoxCommentsActivity_TextChanged(object sender, EventArgs e)
        {
            _tempPerson.FollowUpComment = ServicesHelper.CheckTextBoxText(richTextBoxCommentsActivity.Text);
        }

	   private void _InitializeSponsor()
        {
            comboBoxSponsor1.Items.Clear();
            List<string> list = ServicesProvider.GetInstance().GetClientServices().FindAllSetUpFields(OSetUpFieldTypes.Sponsor1);
            foreach(string s in list)
            {
                comboBoxSponsor1.Items.Add(s);
            }
        }
        private void _InitializeSponsor2()
        {
            comboBoxSponsor2.Items.Clear();
            List<string> list = ServicesProvider.GetInstance().GetClientServices().FindAllSetUpFields(OSetUpFieldTypes.Sponsor2);
            foreach (string s in list)
            {
                comboBoxSponsor2.Items.Add(s);
            }
        }

        private void textBoxSponsor1_TextChanged(object sender, EventArgs e)
        {
            _tempPerson.Sponsor1Comment = textBoxSponsor1.Text;
        }

        private void textBoxSponsor2_TextChanged(object sender, EventArgs e)
        {
            _tempPerson.Sponsor2Comment = textBoxSponsor2.Text;
        }

        
        public void HideSavings()
        {
            tabControlEconomicInfo.TabPages.Remove(tabPageSavings);
        }

        private void tabControlEconomicInfo_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void textBoxIdentificationData_Leave(object sender, EventArgs e)
        {
            try
            {
                if (textBoxIdentificationData.Text != String.Empty)
                    if (ServicesProvider.GetInstance().GetClientServices().CheckIfIdentificationDataAlreadyExists(
                        textBoxIdentificationData.Text, _tempPerson.Id))
                            throw new OpenCbsTiersSaveException(OpenCbsTiersSaveExceptionEnum.IdentificationDataAlreadyUsed);
                _tempPerson.IdentificationData = ServicesHelper.CheckTextBoxText(textBoxIdentificationData.Text);
            }
            catch (Exception ex)
            {
                new frmShowError(CustomExceptionHandler.ShowExceptionText(ex)).ShowDialog();
            }
        }

        private void savingsListUserControl_AddSelectedSaving(object sender, EventArgs e)
        {
            if (AddSelectedSaving != null)
                AddSelectedSaving(sender, e);
        }

        private void savingsListUserControl_ViewSelectedSaving(object sender, EventArgs e)
        {
            if (ViewSelectedSaving != null)
                ViewSelectedSaving(sender, e);
        }

        public void SyncLoanCycle()
        {
            textBoxLoanCycle.Text = _tempPerson.LoanCycle.ToString();
        }

        private void InitPrintButton()
        {
            btnPrint.ReportInitializer = report => report.SetParamValue("person_id", _tempPerson.Id);
            btnPrint.LoadReports();
        }

        private void EacPersonActivityChange(object sender, EconomicActivtyEventArgs e)
        {
            if (_tempPerson != null) _tempPerson.Activity = eacPerson.Activity;
        }
	}
}
