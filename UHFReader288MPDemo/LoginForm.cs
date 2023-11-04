using System;
using System.Windows.Forms;

namespace UHFReader288MPDemo
{
    public partial class LoginForm : Form
    {
        public string UserName
        {
            get
            {
                return txtUsername.Text.Trim();
            }
        }

        public string Password
        {
            get
            {
                return txtPwd.Text.Trim();
            }
        }

        public LoginForm()
        {
            InitializeComponent();
        }

        private void btnLogin_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
        }
    }
}