using System;
using System.Threading;
using System.Windows.Forms;

namespace UHFReader288MPDemo
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            using (Mutex mutex = new Mutex(true, Application.ProductName, out bool createNew))
            {
                if (createNew)
                {
                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);
                    Application.Run(new UHFReader288MP());
                }
                else
                {
                    MessageBox.Show("The application is already running ...");
                    Thread.Sleep(1000);
                    Environment.Exit(1);
                }
            }
        }
    }
}
