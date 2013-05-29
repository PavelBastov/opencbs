using System;
using System.Windows.Forms;

namespace OpenCBS.GUI.Report_Browser
{
    public partial class ReportLoadingProgressForm : Form
    {
        private DateTime _startDate;

        public ReportLoadingProgressForm()
        {
            InitializeComponent();
        }

        private void LoadingReport_Load(object sender, EventArgs e)
        {
            _startDate = DateTime.Now;
            timer.Enabled = true;
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            elapsedTimeLabel.Text = (DateTime.Now - _startDate).Seconds.ToString() + " s.";
        }
    }
}
