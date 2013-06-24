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
using System.Windows.Forms;
using OpenCBS.CoreDomain;
using OpenCBS.Services;

namespace OpenCBS.GUI
{
    /// <summary>
    /// Summary description for CityForm.
    /// </summary>
    public class CityForm : Form
    {
        private ListView listViewCity;
        private ColumnHeader columnHeader1;
        private string city;

        /// <summary>
        /// Required designer variable.
        /// </summary>
        private Container components = null;

        public CityForm()
        {
            InitializeComponent();
            InitializeCities();
        }

        public CityForm(int districtId)
        {
            InitializeComponent();
            InitializeCities(districtId);
        }

        public string City
        {
            get { return city; }
        }

        private void InitializeCities(int districtId)
        {
            List<City> cities = ServicesProvider.GetInstance().GetLocationServices().FindCityByDistrictId(districtId);
            listViewCity.Items.Clear();
            foreach (City cityObject in cities)
            {
                ListViewItem listViewItem = new ListViewItem(cityObject.Name);
                listViewCity.Items.Add(listViewItem);
            }
        }

        private void InitializeCities()
        {
            List<City> cities = ServicesProvider.GetInstance().GetLocationServices().GetCities();
            listViewCity.Items.Clear();
            foreach (City cityObject in cities)
            {
                ListViewItem listViewItem = new ListViewItem(cityObject.Name);
                listViewCity.Items.Add(listViewItem);
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(CityForm));
            this.listViewCity = new System.Windows.Forms.ListView();
            this.columnHeader1 = new System.Windows.Forms.ColumnHeader();
            this.SuspendLayout();
            // 
            // listViewCity
            // 
            this.listViewCity.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1});
            resources.ApplyResources(this.listViewCity, "listViewCity");
            this.listViewCity.FullRowSelect = true;
            this.listViewCity.GridLines = true;
            this.listViewCity.Name = "listViewCity";
            this.listViewCity.UseCompatibleStateImageBehavior = false;
            this.listViewCity.View = System.Windows.Forms.View.Details;
            this.listViewCity.DoubleClick += new System.EventHandler(this.listViewCity_DoubleClick);
            // 
            // columnHeader1
            // 
            resources.ApplyResources(this.columnHeader1, "columnHeader1");
            // 
            // CityForm
            // 
            resources.ApplyResources(this, "$this");
            this.Controls.Add(this.listViewCity);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Name = "CityForm";
            this.ResumeLayout(false);

        }

        #endregion

        private void listViewCity_DoubleClick(object sender, EventArgs e)
        {
            city = listViewCity.SelectedItems[0].Text;
            Close();
        }
    }
}
