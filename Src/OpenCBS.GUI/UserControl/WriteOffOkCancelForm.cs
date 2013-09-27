using System;
using System.Windows.Forms;

namespace OpenCBS.GUI.UserControl
{
    public partial class WriteOffOkCancelForm : Form
    {
        public int WriteOffMethodId { get; set; }
        public WriteOffOkCancelForm()
        {
            InitializeComponent();
            reserveComboBox.SelectedIndex = 0;
        }

        private void reserveComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            WriteOffMethodId = reserveComboBox.SelectedIndex;
        }

        public DialogResult ShowDialogWrappe(out int writeOffMethodId)
        {
            var dialogRet = ShowDialog();
            writeOffMethodId = WriteOffMethodId;
            return dialogRet;
        }
    }
}
