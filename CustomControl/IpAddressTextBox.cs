using System;
using System.ComponentModel;
using System.Drawing;
using System.Net;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace CustomControl
{
    public class IpAddressTextBox : UserControl
    {
        private string IpAddress;
        private Panel pnlMain;
        private Label lbldot3;
        private Label lbldot2;
        private Label lbldot1;
        private TextBox Ip1;
        private TextBox Ip2;
        private TextBox Ip3;
        private TextBox Ip4;

        public string IpAddressStr
        {
            get
            {
                string ipString = Ip1.Text + "." + Ip2.Text + "." + Ip3.Text + "." + Ip4.Text;
                try
                {
                    IPAddress.Parse(ipString);
                }
                catch
                {
                    return "";
                }
                IpAddress = ipString;
                return IpAddress;
            }
            set
            {
                string ipString = value;
                if (string.IsNullOrEmpty(ipString))
                {
                    Ip1.Text = "";
                    Ip2.Text = "";
                    Ip3.Text = "";
                    Ip4.Text = "";
                    IpAddress = "";
                }
                else
                {
                    try
                    {
                        IPAddress.Parse(ipString);
                        string[] strArray = ipString.Split('.');
                        Ip1.Text = strArray[0];
                        Ip2.Text = strArray[1];
                        Ip3.Text = strArray[2];
                        Ip4.Text = strArray[3];
                        IpAddress = ipString;
                    }
                    catch
                    {
                        Ip1.Text = "";
                        Ip2.Text = "";
                        Ip3.Text = "";
                        Ip4.Text = "";
                        IpAddress = "";
                    }
                }
            }
        }

        public IpAddressTextBox()
        {
            InitializeComponent();
        }

        private void Ip1_TextChanged(object sender, EventArgs e)
        {
            if (Ip1.Text.Length != 3 || Ip1.Text.Length <= 0 || Ip1.SelectionLength != 0)
                return;
            if (Convert.ToInt32(Ip1.Text) > 223)
                Ip1.Text = "223";
            else if (Convert.ToInt32(Ip1.Text) < 1)
            {
                Ip1.Text = "1";
            }
            else
            {
                Ip2.Focus();
                Ip2.Select(0, Ip2.Text.Length);
            }
        }

        private void Ip2_TextChanged(object sender, EventArgs e)
        {
            if (Ip2.Text.Length != 3 || Ip2.Text.Length <= 0 || Ip2.SelectionLength != 0)
                return;
            if (Convert.ToInt32(Ip2.Text) > (int)byte.MaxValue)
            {
                Ip2.Text = "255";
            }
            else
            {
                Ip3.Focus();
                Ip3.Select(0, Ip3.Text.Length);
            }
        }

        private void Ip3_TextChanged(object sender, EventArgs e)
        {
            if (Ip3.Text.Length != 3 || Ip3.Text.Length <= 0 || Ip3.SelectionLength != 0)
                return;
            if (Convert.ToInt32(Ip3.Text) > (int)byte.MaxValue)
            {
                Ip3.Text = "255";
            }
            else
            {
                Ip4.Focus();
                Ip4.Select(0, Ip4.Text.Length);
            }
        }

        private void Ip4_TextChanged(object sender, EventArgs e)
        {
            if (Ip4.Text.Length != 3 || Ip4.Text.Length <= 0 || Ip4.SelectionLength != 0 || Convert.ToInt32(Ip4.Text) <= (int)byte.MaxValue)
                return;
            Ip4.Text = "255";
        }

        private bool CheckInput(string inputString)
        {
            bool flag = false;
            Match match = new Regex("[0-9\\s]+").Match(inputString);
            if (match.Success && match.Value == inputString)
                flag = true;
            return flag;
        }

        private void Ip1_KeyPress(object sender, KeyPressEventArgs e)
        {
            string inputString = e.KeyChar.ToString();
            if (e.KeyChar != '\b')
            {
                if (e.KeyChar == '.' && Ip1.Text.Length > 0 && Ip1.SelectionLength == 0)
                {
                    Ip2.Focus();
                    Ip2.Select(0, Ip2.Text.Length);
                }
                if (!CheckInput(inputString))
                    e.Handled = true;
            }
            OnKeyPress(e);
        }

        private void Ip2_KeyPress(object sender, KeyPressEventArgs e)
        {
            string inputString = e.KeyChar.ToString();
            if (e.KeyChar != '\b')
            {
                if (e.KeyChar == '.' && Ip2.Text.Length > 0 && Ip2.SelectionLength == 0)
                {
                    Ip3.Focus();
                    Ip3.Select(0, Ip3.Text.Length);
                }
                if (!CheckInput(inputString))
                    e.Handled = true;
            }
            OnKeyPress(e);
        }

        private void Ip3_KeyPress(object sender, KeyPressEventArgs e)
        {
            string inputString = e.KeyChar.ToString();
            if (e.KeyChar != '\b')
            {
                if (e.KeyChar == '.' && Ip3.Text.Length > 0 && Ip3.SelectionLength == 0)
                {
                    Ip4.Focus();
                    Ip4.Select(0, Ip4.Text.Length);
                }
                if (!CheckInput(inputString))
                    e.Handled = true;
            }
            OnKeyPress(e);
        }

        private void Ip4_KeyPress(object sender, KeyPressEventArgs e)
        {
            string inputString = e.KeyChar.ToString();
            if (e.KeyChar != '\b' && !CheckInput(inputString))
                e.Handled = true;
            OnKeyPress(e);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            pnlMain = new Panel();
            lbldot3 = new Label();
            lbldot2 = new Label();
            lbldot1 = new Label();
            Ip4 = new TextBox();
            Ip3 = new TextBox();
            Ip2 = new TextBox();
            Ip1 = new TextBox();
            pnlMain.SuspendLayout();
            SuspendLayout();
            pnlMain.BackColor = Color.White;
            pnlMain.BorderStyle = BorderStyle.Fixed3D;
            pnlMain.Controls.Add((Control)lbldot3);
            pnlMain.Controls.Add((Control)lbldot2);
            pnlMain.Controls.Add((Control)lbldot1);
            pnlMain.Controls.Add((Control)Ip4);
            pnlMain.Controls.Add((Control)Ip3);
            pnlMain.Controls.Add((Control)Ip2);
            pnlMain.Controls.Add((Control)Ip1);
            pnlMain.Dock = DockStyle.Fill;
            pnlMain.Location = new Point(0, 0);
            pnlMain.Name = "pnlMain";
            pnlMain.Size = new Size(121, 21);
            pnlMain.TabIndex = 1;
            lbldot3.AutoSize = true;
            lbldot3.Location = new Point(83, 4);
            lbldot3.Name = "lbldot3";
            lbldot3.Size = new Size(11, 12);
            lbldot3.TabIndex = 6;
            lbldot3.Text = ".";
            lbldot2.AutoSize = true;
            lbldot2.Location = new Point(51, 4);
            lbldot2.Name = "lbldot2";
            lbldot2.Size = new Size(11, 12);
            lbldot2.TabIndex = 5;
            lbldot2.Text = ".";
            lbldot1.AutoSize = true;
            lbldot1.Location = new Point(22, 4);
            lbldot1.Name = "lbldot1";
            lbldot1.Size = new Size(11, 12);
            lbldot1.TabIndex = 1;
            lbldot1.Text = ".";
            Ip4.BorderStyle = BorderStyle.None;
            Ip4.Location = new Point(94, 1);
            Ip4.MaxLength = 3;
            Ip4.Name = "Ip4";
            Ip4.ShortcutsEnabled = false;
            Ip4.Size = new Size(20, 14);
            Ip4.TabIndex = 3;
            Ip4.TextAlign = HorizontalAlignment.Center;
            Ip4.TextChanged += new EventHandler(Ip4_TextChanged);
            Ip4.KeyPress += new KeyPressEventHandler(Ip4_KeyPress);
            Ip3.BorderStyle = BorderStyle.None;
            Ip3.Location = new Point(62, 1);
            Ip3.MaxLength = 3;
            Ip3.Name = "Ip3";
            Ip3.ShortcutsEnabled = false;
            Ip3.Size = new Size(20, 14);
            Ip3.TabIndex = 2;
            Ip3.TextAlign = HorizontalAlignment.Center;
            Ip3.TextChanged += new EventHandler(Ip3_TextChanged);
            Ip3.KeyPress += new KeyPressEventHandler(Ip3_KeyPress);
            Ip2.BorderStyle = BorderStyle.None;
            Ip2.Location = new Point(33, 1);
            Ip2.MaxLength = 3;
            Ip2.Name = "Ip2";
            Ip2.ShortcutsEnabled = false;
            Ip2.Size = new Size(20, 14);
            Ip2.TabIndex = 1;
            Ip2.TextAlign = HorizontalAlignment.Center;
            Ip2.TextChanged += new EventHandler(Ip2_TextChanged);
            Ip2.KeyPress += new KeyPressEventHandler(Ip2_KeyPress);
            Ip1.BorderStyle = BorderStyle.None;
            Ip1.Location = new Point(0, 1);
            Ip1.MaxLength = 3;
            Ip1.Name = "Ip1";
            Ip1.ShortcutsEnabled = false;
            Ip1.Size = new Size(20, 14);
            Ip1.TabIndex = 0;
            Ip1.TextAlign = HorizontalAlignment.Center;
            Ip1.TextChanged += new EventHandler(Ip1_TextChanged);
            Ip1.KeyPress += new KeyPressEventHandler(Ip1_KeyPress);
            AutoScaleDimensions = new SizeF(6f, 12f);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add((Control)pnlMain);
            Name = "IpAddressTextBox";
            Size = new Size(121, 21);
            pnlMain.ResumeLayout(false);
            pnlMain.PerformLayout();
            ResumeLayout(false);
        }
    }
}
