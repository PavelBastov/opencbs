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
using System.Drawing;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using OpenCBS.CoreDomain;
using OpenCBS.Enums;
using OpenCBS.Services;
using OpenCBS.MultiLanguageRessources;

namespace OpenCBS.GUI
{
    /// <summary>
    /// Summary description for AddressUserControl.
    /// </summary>
    public partial class AddressUserControl : System.Windows.Forms.UserControl
    {
        private District _district;
        private string _city;
        private Province _province;
        private string _comments;
        private string _homePhone;
        private string _personalPhone;
        private string _email;
        private string _zipCode;
        private string _homeType;

        public AddressUserControl()
        {
            _Initialization();
        }
        
        public District District
        {
            get { return _district; }
            set
            {
                _district = value;
                _SetValue(_district, _city, _comments, _homePhone,_personalPhone,_zipCode,_email,_homeType);
            }
        }

        public string City
        {
            get { return _city; }
            set
            {
                _city = value;
                _SetValue(_district, _city, _comments, _homePhone, _personalPhone, _zipCode, _email, _homeType);
            }
        }

        public string Comments
        {
            get { return _comments; }
            set
            {
                _comments = value;
                _SetValue(_district, _city, _comments, _homePhone, _personalPhone, _zipCode, _email, _homeType);
            }
        }

        public string HomePhone
        {
            get { return _homePhone; }
            set
            {
                _homePhone= value;
                _SetValue(_district, _city, _comments, _homePhone, _personalPhone, _zipCode, _email, _homeType);
            }
        }

        public string PersonalPhone
        {
            get { return _personalPhone; }
            set
            {
                _personalPhone = value;
                _SetValue(_district, _city, _comments, _homePhone, _personalPhone, _zipCode, _email, _homeType);
            }
        }

        public string Email
        {
            get { return _email; }
            set
            {
                _email = value;
                _SetValue(_district, _city, _comments, _homePhone, _personalPhone, _zipCode, _email, _homeType);
            }
        }

        public string ZipCode
        {
            get { return _zipCode; }
            set
            {
                _zipCode = value;
                _SetValue(_district, _city, _comments, _homePhone, _personalPhone, _zipCode, _email, _homeType);
            }
        }

        public string HomeType
        {
            get { return _homeType; }
            set
            {
                _homeType = value;
                _SetValue(_district, _city, _comments, _homePhone, _personalPhone, _zipCode, _email, _homeType);
            }
        }

        public string TextBoxHomePhoneText
        {
            get { return labelHomePhone.Text; }
            set { labelHomePhone.Text = value; }
        }

        public string TextBoxPersonalPhoneText
        {
            get { return labelPersonalPhone.Text; }
            set { labelPersonalPhone.Text = value; }
        }
        
        private void _SetValue(District pDistrict, string pCity, 
            string pComments,string pHomePhone,string pPersonalPhone,string pZipCode, string pEmail,string pHomeType)
        {
            if (pDistrict != null)
            {
                comboBoxDistrict.Text = pDistrict.Name;
                comboBoxProvince.Text = pDistrict.Province.Name;
            }
            //InitializeActionButton();
            textBoxCity.Text = pCity;
            tbAddress.Text = pComments;
            textBoxHomePhone.Text = pHomePhone;
            textBoxPersonalPhone.Text = pPersonalPhone;
            textBoxEMail.Text = pEmail;
            tbZipCode.Text = pZipCode;
            comboBoxHomeType.Text = pHomeType;
        }

        private void _Initialization()
        {
            InitializeComponent();
            _province = new Province();
            _district = null;
            _city = null;
            _comments = null;
            _homePhone = string.Empty;
            _personalPhone = string.Empty;
            _email = string.Empty;
            _zipCode = string.Empty;
            _homeType = string.Empty;
            _InitializeProvince();
            _InitializeDistricts(); 
            _InitializeHomeType();
            textBoxCity_AutoCompleteCustomSourceChanged();
        }

        private void _InitializeHomeType()
        {
            comboBoxHomeType.Items.Clear();
            List<string> list = ServicesProvider.GetInstance().GetClientServices().FindAllSetUpFields(OSetUpFieldTypes.HousingLocation);
            foreach (string s in list)
            {
                comboBoxHomeType.Items.Add(s);
            }
        }

        private void _InitializeProvince()
        {
            comboBoxProvince.Items.Clear();
            List<Province> provinceList = ServicesProvider.GetInstance().GetLocationServices().GetProvinces();
            foreach (Province pro in provinceList)
            {
                comboBoxProvince.Items.Add(pro);
            }
            Province selectProvince = new Province{Name =MultiLanguageStrings.GetString(Ressource.AddressUserControl,"all.Text")};
            comboBoxProvince.Items.Add(selectProvince);
            comboBoxProvince.SelectedItem = selectProvince;
            _province = new Province();
        }

        private void _InitializeDistricts()
        {
            comboBoxDistrict.Items.Clear();

            District selectDistrict = new District{Name = MultiLanguageStrings.GetString(Ressource.AddressUserControl,"selectDistrict.Text")};
            comboBoxDistrict.Items.Add(selectDistrict);

            List<District> districtList = ServicesProvider.GetInstance().GetLocationServices().FindDistrict(_province);
            foreach (District dis in districtList)
            {
                comboBoxDistrict.Items.Add(dis);
            }
            comboBoxDistrict.SelectedItem = selectDistrict;
            //_district = null;
        }

        private void _SelectProvince()
        {
            if (_district == null || _district.Id == 0)
            {
                return;
            }
            foreach (Province selectedProvince in comboBoxProvince.Items)
            {
                if (selectedProvince.Name != _district.Province.Name)
                {
                    continue;
                }
                comboBoxProvince.SelectedItem = selectedProvince;
                _province = selectedProvince;
                break;
            }
        }

        private void _SelectDistrict()
        {
            if (_city == null)
            {
                return;
            }
            var districtOfCity = ServicesProvider.GetInstance().GetLocationServices().FindDistrictByCityName(_city);
            foreach (District selectedDistrict in comboBoxDistrict.Items)
            {
                if (selectedDistrict.Id != districtOfCity.Id)
                {
                    continue;
                }
                comboBoxDistrict.SelectedItem = selectedDistrict;
                _district = selectedDistrict;
                break;
            }
        }

        private void comboBoxDistrict_SelectionChangeCommitted(object sender, System.EventArgs e)
        {
            _district = (District)comboBoxDistrict.SelectedItem;
            District selectDistrict = new District { Name = MultiLanguageStrings.GetString(Ressource.AddressUserControl, "selectDistrict.Text") };
            if (_district.Name == selectDistrict.Name)
                _district = null;
            _SelectProvince();
            textBoxCity.Text = String.Empty;
            _city = null;
            //InitializeActionButton();
            textBoxCity_AutoCompleteCustomSourceChanged();
        }

        private void textBoxCity_TextChanged(object sender, System.EventArgs e)
        {
            _city = ServicesHelper.CheckTextBoxText(textBoxCity.Text);
            textBoxCity_AutoCompleteCustomSourceChanged();
        }

        private void textBoxComments_TextChanged(object sender, System.EventArgs e)
        {
            _comments = ServicesHelper.CheckTextBoxText(tbAddress.Text);
        }

        private void comboBoxProvince_SelectionChangeCommitted(object sender, System.EventArgs e)
        {
            _province = (Province)comboBoxProvince.SelectedItem;
            //Province selectProvince = new Province { Name = MultiLanguageStrings.GetString(Ressource.AddressUserControl, "all.Text") };
            //if (_province.Name == selectProvince.Name)
            //    _province = new Province();
            textBoxCity.Text = String.Empty;
            _city = null;
            _district = null;
            _InitializeDistricts();
            textBoxCity_AutoCompleteCustomSourceChanged();
        }

        //private void InitializeActionButton()
        //{
            //if (ServicesProvider.GetInstance().GetGeneralSettings().IsCityAnOpenValue)
            //{
            //    buttonSave.Visible = false;
            //    textBoxCity.Enabled = true;
            //}
            //else
            //{
            //    textBoxCity.Enabled = false;
            //    if (_district != null)
            //        buttonSave.Visible = _district.Id != 0;
            //}
        //}

        public void ResetAllComponents()
        {
            _InitializeDistricts();
            _InitializeProvince();
            _InitializeHomeType();
            textBoxCity.Text = string.Empty;
            tbAddress.Text = string.Empty;
            textBoxHomePhone.Text = string.Empty;
            textBoxPersonalPhone.Text = string.Empty;
            tbZipCode.Text = string.Empty;
            textBoxEMail.Text = string.Empty;

        }

        private void buttonSave_Click(object sender, System.EventArgs e)
        {
            CityForm city = new CityForm(_district, _province);
            city.ShowDialog();
            _city = textBoxCity.Text = city.City;
            _SelectDistrict();
            _SelectProvince();
            _InitializeDistricts();
            _SelectDistrict();
            //var districtOfCity = ServicesProvider.GetInstance().GetLocationServices().FindDistrictByCityName(_city);
            //foreach (District selectedDistrict in comboBoxDistrict.Items)
            //{
            //    if (selectedDistrict.Id != districtOfCity.Id)
            //    {
            //        continue;
            //    }
            //    comboBoxDistrict.SelectedItem = selectedDistrict;
            //    _district = selectedDistrict;
            //    break;
            //}
            textBoxCity_AutoCompleteCustomSourceChanged();
        }

        private void textBoxHomePhone_TextChanged(object sender, EventArgs e)
        {
            _homePhone = ServicesHelper.CheckTextBoxText(textBoxHomePhone.Text);
        }

        private void textBoxPersonalPhone_TextChanged(object sender, EventArgs e)
        {
            _personalPhone = ServicesHelper.CheckTextBoxText(textBoxPersonalPhone.Text);
        }

        private void textBoxHomeType_TextChanged(object sender, EventArgs e)
        {
            string zipCode = ServicesHelper.CheckTextBoxText(tbZipCode.Text);
            if(string.IsNullOrEmpty(zipCode))
            {
                tbZipCode.BackColor = Color.White;
                _zipCode = zipCode;
                return;
            }
            Regex regex = new Regex("^[0-9a-zA-Z]{1,10}$");
            if (regex.IsMatch(zipCode))
            {
               _zipCode = zipCode;
                tbZipCode.BackColor = Color.White;
            }
            else
            {
                _zipCode = null;
                tbZipCode.BackColor = Color.Red;
            }
        }

        private void textBoxEMail_TextChanged(object sender, EventArgs e)
        {
            _email = ServicesHelper.CheckTextBoxText(textBoxEMail.Text);
        }

        private void comboBoxHomeType_DropDown(object sender, EventArgs e)
        {
            _InitializeHomeType();
        }

        private void comboBoxHomeType_SelectionChangeCommitted(object sender, EventArgs e)
        {
            _homeType = ServicesHelper.CheckTextBoxText(comboBoxHomeType.SelectedItem.ToString());
        }

        public bool ExtraVisible
        {
            set
            {
                comboBoxHomeType.Visible = value;
                label1.Visible = value;
                textBoxHomePhone.Visible = value;
                labelHomePhone.Visible = value;
                textBoxPersonalPhone.Visible = value;
                labelPersonalPhone.Visible = value;
                textBoxEMail.Visible = value;
                labelEMail.Visible = value;
            }
        }

        private void textBoxCity_AutoCompleteCustomSourceChanged()
        {
            textBoxCity.AutoCompleteCustomSource.Clear();
            var data = new AutoCompleteStringCollection();
            List<City> cities =
                ServicesProvider.GetInstance()
                                .GetLocationServices()
                                .FindCitiesByDistrictOrProvince(_district, _province);
            foreach (City cityObject in cities)
                data.Add(cityObject.Name);
            textBoxCity.AutoCompleteCustomSource = data;
        }

        private void FindDistrictProvince()
        {
            _city = textBoxCity.Text;
            _SelectDistrict();
            _SelectProvince();
        }

        private void textBoxCity_Leave(object sender, EventArgs e)
        {
            FindDistrictProvince();
        }
    }
}
