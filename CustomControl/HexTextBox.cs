using System;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace CustomControl
{
    public class HexTextBox : TextBox
    {
        private IContainer resources;

        protected override void Dispose(bool disposing)
        {
            if (disposing && resources != null)
                resources.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            resources = (IContainer)new System.ComponentModel.Container();
        }

        public HexTextBox()
        {
            InitializeComponent();
        }

        protected override void OnKeyPress(KeyPressEventArgs e)
        {
            string inputString = e.KeyChar.ToString();
            if (e.KeyChar != '\b')
            {
                if (!CheckInput(inputString))
                    e.Handled = true;
            }
            else
            {
                int selectionStart = SelectionStart;
                if (selectionStart > 0 && Text[selectionStart - 1] == ' ')
                {
                    Text = Text.Remove(selectionStart - 1);
                    SelectionStart = selectionStart - 1;
                }
            }
            base.OnKeyPress(e);
        }

        private bool CheckInput(string inputString)
        {
            bool flag = false;
            Match match = new Regex("[a-fA-F0-9.\\s]+").Match(inputString);
            if (match.Success && match.Value == inputString)
                flag = true;
            return flag;
        }

        protected override void OnTextChanged(EventArgs e)
        {
            base.OnTextChanged(e);
            int selectionStart = SelectionStart;
            if (selectionStart <= 1 ||
                MaxLength == selectionStart ||
                Text[selectionStart - 1] == ' ' ||
                Text[selectionStart - 2] == ' ')
                return;
            Text = Text.Substring(0, selectionStart) + " " + Text.Substring(selectionStart);
            SelectionStart = selectionStart + 1;
        }
    }
}
