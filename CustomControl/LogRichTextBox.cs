using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace CustomControl
{
    public class LogRichTextBox : RichTextBox
    {
        private IContainer components;

        protected override void Dispose(bool disposing)
        {
            if (disposing && this.components != null)
                this.components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.components = (IContainer)new System.ComponentModel.Container();
        }

        public LogRichTextBox()
        {
            this.InitializeComponent();
        }

        protected override void OnTextChanged(EventArgs e)
        {
            base.OnTextChanged(e);
            this.Select(this.TextLength, 0);
            this.ScrollToCaret();
        }

        public void AppendTextEx(string strText, Color clAppend)
        {
            int textLength = this.TextLength;
            if (textLength != 0)
                this.AppendText(Environment.NewLine + DateTime.Now.ToString() + " " + strText);
            else
                this.AppendText(DateTime.Now.ToString() + " " + strText);
            this.Select(textLength, this.TextLength - textLength);
            this.SelectionColor = clAppend;
        }
    }
}
