// Copyright © 2013 Open Octopus Ltd.
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
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using OpenCBS.CoreDomain.Dashboard;
using OpenCBS.Enums;
using OpenCBS.GUI.AuditTrail;
using OpenCBS.GUI.Clients;
using OpenCBS.GUI.Configuration;
using OpenCBS.GUI.Products;
using OpenCBS.GUI.Report_Browser;
using OpenCBS.GUI.UserControl;
using OpenCBS.Services;
using System.Linq;

namespace OpenCBS.GUI
{
    public partial class DashboardForm : SweetBaseForm
    {
        private Chart _portfolioChart;
        private Chart _parChart;
        private Chart _disbursementsChart;
        private Chart _olbTrendChart;

        public DashboardForm()
        {
            InitializeComponent();
        }

        private void OnLoad(object sender, EventArgs e)
        {
            var numberFormatInfo = new NumberFormatInfo
            {
                NumberGroupSeparator = " ",
                NumberDecimalSeparator = ",",
            };

            parNameColumn.AspectToStringConverter = value =>
            {
                var name = (string) value;
                return GetString(name);
            };
            parListView.RowFormatter = listViewItem =>
            {
                var portfolioLine = (PortfolioLine)listViewItem.RowObject;
                if (portfolioLine.Name == "Total")
                {
                    var font = listViewItem.Font;
                    listViewItem.Font = new Font(font.Name, font.Size, FontStyle.Bold);
                }
            };

            RefreshDashboard();
        }

        private void RefreshPortfolioPieChart(Dashboard dashboard)
        {
            if (_portfolioChart != null)
            {
                portfolioPanel.Controls.Remove(_portfolioChart);
            }
            _portfolioChart = new Chart();
            var chartArea = new ChartArea();
            _portfolioChart.ChartAreas.Add(chartArea);

            var parPercentage = 0 == dashboard.Olb ? 0 : Math.Round(100 * dashboard.Par / dashboard.Olb, 1);
            var performingPercentage = 100 - parPercentage;

            var numberFormatInfo = new NumberFormatInfo
            {
                NumberGroupSeparator = " ",
                NumberDecimalSeparator = ",",
            };

            var series = new Series();
            series.ChartType = SeriesChartType.Pie;
            var point = series.Points.Add(Convert.ToDouble(performingPercentage));
            point.LegendText = string.Format(
                "{0}: {1} %", 
                GetString("Performing"),
                performingPercentage.ToString("N1", numberFormatInfo));
            //point.Color = Color.FromArgb(28, 151, 234);
            point.Color = Color.FromArgb(72, 234, 28);

            point = series.Points.Add(Convert.ToDouble(parPercentage));
            point.LegendText = string.Format(
                "{0}: {1} %",
                GetString("PAR"),
                parPercentage.ToString("N1", numberFormatInfo));
            point.Color = Color.FromArgb(234, 28, 28);

            AddLegend(_portfolioChart);
            AddTitle(_portfolioChart, GetString("Portfolio"));

            _portfolioChart.Series.Add(series);
            _portfolioChart.Location = new Point(0, 0);
            _portfolioChart.Dock = DockStyle.Fill;

            portfolioPanel.Controls.Add(_portfolioChart);
        }

        private void RefreshParPieChart(Dashboard dashboard)
        {
            if (_parChart != null)
            {
                parPanel.Controls.Remove(_parChart);
            }
            if (0 == dashboard.Par) return;

            _parChart = new Chart();
            var chartArea = new ChartArea();
            _parChart.ChartAreas.Add(chartArea);

            var values = dashboard.
                PortfolioLines.
                Where(line => line.Name.StartsWith("PAR")).
                Select(line => line.Amount).
                ToArray();
            var legends = dashboard.
                PortfolioLines.
                Where(line => line.Name.StartsWith("PAR")).
                Select(line => GetString(line.Name)).
                ToArray();

            var colors = new[]
            {
                Color.FromArgb(234, 200, 28),
                Color.FromArgb(234, 160, 28),
                Color.FromArgb(234, 120, 28),
                Color.FromArgb(234, 80, 28),
                Color.FromArgb(234, 40, 28),
                Color.FromArgb(234, 0, 28),
            };

            var series = new Series();
            series.ChartType = SeriesChartType.Pie;
            for (var i = 0; i < 6; i++)
            {
                var value = Math.Round(0 == dashboard.Par ? 0 : 100 * values[i] / dashboard.Par, 1);
                var point = series.Points.Add(Convert.ToDouble(value));
                var numberFormat = Math.Round(value) == value ? "N0" : "N1";
                point.LegendText = string.Format("{0}: {1}%", legends[i], value.ToString(numberFormat));
                point.Color = colors[i];
            }

            AddLegend(_parChart);
            AddTitle(_parChart, GetString("PAR"));

            _parChart.Series.Add(series);
            _parChart.Location = new Point(0, 0);
            _parChart.Dock = DockStyle.Fill;

            parPanel.Controls.Add(_parChart);
        }

        private void RefreshDisbursementsChart(Dashboard dashboard)
        {
            if (_disbursementsChart != null)
            {
                disbursementsPanel.Controls.Remove(_disbursementsChart);
            }
            _disbursementsChart = new Chart();
            var chartArea = new ChartArea();
            chartArea.AxisX.LabelStyle.Font = chartArea.AxisY.LabelStyle.Font = new Font("Arial", 7f);
            chartArea.AxisX.IsLabelAutoFit = chartArea.AxisY.IsLabelAutoFit = false;
            _disbursementsChart.ChartAreas.Add(chartArea);

            var series = new Series();
            foreach (var actionStat in dashboard.ActionStats)
            {
                var point = series.Points.Add(actionStat.NumberDisbursed);
                point.AxisLabel = actionStat.Date.ToString("dd.MM");
                point.Color = Color.FromArgb(28, 151, 234);
            }

            _disbursementsChart.Series.Add(series);
            _disbursementsChart.Dock = DockStyle.Fill;
            AddTitle(_disbursementsChart, GetString("NumberOfDisbursements"));

            disbursementsPanel.Controls.Add(_disbursementsChart);
        }

        private void RefreshOlbTrendChart(Dashboard dashboard)
        {
            if (_olbTrendChart != null)
            {
                olbTrendPanel.Controls.Remove(_olbTrendChart);
            }
            _olbTrendChart = new Chart();
            var chartArea = new ChartArea();
            chartArea.AxisX.LabelStyle.Font = chartArea.AxisY.LabelStyle.Font = new Font("Arial", 7f);
            chartArea.AxisX.IsLabelAutoFit = chartArea.AxisY.IsLabelAutoFit = false;
            _olbTrendChart.ChartAreas.Add(chartArea);

            var series = new Series();
            series.ChartType = SeriesChartType.Line;
            series.BorderWidth = 3;
            series.MarkerStyle = MarkerStyle.Circle;
            double value = 0;
            var point = series.Points.Add(value);
            point.AxisLabel = dashboard.ActionStats[0].Date.AddDays(-1).ToString("dd.MM");
            foreach (var actionStat in dashboard.ActionStats)
            {
                value += Convert.ToDouble(actionStat.OlbGrowth);
                point = series.Points.Add(value);
                point.AxisLabel = actionStat.Date.ToString("dd.MM");
            }

            _olbTrendChart.Series.Add(series);
            _olbTrendChart.Dock = DockStyle.Fill;
            AddTitle(_olbTrendChart, GetString("OlbTrend"));

            olbTrendPanel.Controls.Add(_olbTrendChart);
        }

        private void AddLegend(Chart chart)
        {
            var legend = new Legend
            {
                Docking = Docking.Right,
                Alignment = StringAlignment.Center,
                Font = new Font("Arial", 7f),
            };
            chart.Legends.Add(legend);
        }

        private void AddTitle(Chart chart, string text)
        {
            var title = new Title
            {
                Text = text,
                Font = new Font("Arial", 7f, FontStyle.Bold),
                ForeColor = Color.FromArgb(45, 45, 48),
                Docking = Docking.Bottom,
            };
            chart.Titles.Add(title);
        }

        private void OpenClientForm(OClientTypes clientType)
        {
            var parent = Application.OpenForms[0];
            var form = new ClientForm(clientType, parent, false) { MdiParent = parent };
            form.Show();
        }

        private void OnNewIndividualClientLinkLabelClick(object sender, LinkLabelLinkClickedEventArgs e)
        {
            OpenClientForm(OClientTypes.Person);
        }

        private void OnNewSolidarityGroupLinkLabelLinkClick(object sender, LinkLabelLinkClickedEventArgs e)
        {
            OpenClientForm(OClientTypes.Group);
        }

        private void OnNewNonSolidairtyGroupLinkLabelLinkClick(object sender, LinkLabelLinkClickedEventArgs e)
        {
            var form = new NonSolidaryGroupForm { MdiParent = Application.OpenForms[0] };
            form.Show();
        }

        private void OnCorporateClientLinkLabelLinkClick(object sender, LinkLabelLinkClickedEventArgs e)
        {
            OpenClientForm(OClientTypes.Corporate);
        }

        private void RefreshParTable(Dashboard dashboard)
        {
            parListView.SetObjects(dashboard.PortfolioLines);
        }

        private void RefreshDashboard()
        {
            var us = ServicesProvider.GetInstance().GetUserServices();
            var dashboard = us.GetDashboard();

            RefreshPortfolioPieChart(dashboard);
            RefreshParPieChart(dashboard);
            RefreshParTable(dashboard);
            RefreshDisbursementsChart(dashboard);
            RefreshOlbTrendChart(dashboard);
        }

        private void OnSearchClientClick(object sender, LinkLabelLinkClickedEventArgs e)
        {
            var searchForm = SearchClientForm.GetInstance(this);
            searchForm.BringToFront();
            searchForm.WindowState = FormWindowState.Normal;
            searchForm.Show();
        }

        private void OnSearchContractClick(object sender, LinkLabelLinkClickedEventArgs e)
        {
            var searchForm = SearchCreditContractForm.GetInstance(this);
            searchForm.BringToFront();
            searchForm.WindowState = FormWindowState.Normal;
            searchForm.Show();
        }

        private void OnConfigureLoanProductsClick(object sender, LinkLabelLinkClickedEventArgs e)
        {
            var loanProductsForm = new FrmAvalaibleLoanProducts { MdiParent = Application.OpenForms[0] };
            loanProductsForm.Show();
        }

        private void OnConfigureSavingsProductsClick(object sender, LinkLabelLinkClickedEventArgs e)
        {
            var savingsProductsForm = new FrmAvailableSavingProducts { MdiParent = Application.OpenForms[0] };
            savingsProductsForm.Show();
        }

        private void OnConfigureCollateralProducts(object sender, LinkLabelLinkClickedEventArgs e)
        {
            var collateralProductsForm = new FrmAvalaibleCollateralProducts { MdiParent = Application.OpenForms[0] };
            collateralProductsForm.Show();
        }

        private void OnConfigureSettingsClick(object sender, LinkLabelLinkClickedEventArgs e)
        {
            var settingsForm = new FrmGeneralSettings();
            settingsForm.ShowDialog();
        }

        private void OnConfigurePermissionsClick(object sender, LinkLabelLinkClickedEventArgs e)
        {
            var rolesForm = new FrmRoles(Application.OpenForms[0]) { MdiParent = Application.OpenForms[0] };
            rolesForm.Show();

        }

        private void OnAuditTrailClick(object sender, LinkLabelLinkClickedEventArgs e)
        {
            var auditTrailForm = new AuditTrailForm { MdiParent = Application.OpenForms[0] };
            auditTrailForm.Show();
        }

        private void OnReportsClick(object sender, LinkLabelLinkClickedEventArgs e)
        {
            var frm = new ReportBrowserForm { MdiParent = Application.OpenForms[0] };
            frm.Show();
        }

        private void OnRefreshLinkLabelClick(object sender, LinkLabelLinkClickedEventArgs e)
        {
            RefreshDashboard();
        }

        private void aboutLabel_Click(object sender, EventArgs e)
        {

        }

        private void parListView_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
    }
}
