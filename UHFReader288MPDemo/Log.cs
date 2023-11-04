using System;
using System.IO;
using System.Windows.Forms;

namespace UHFReader288MPDemo
{
    public class Log
    {
        public static string PATH;

        public static bool WriteLog(string log)
        {
            return Write(log, "Log");
        }

        public static bool WriteError(string error)
        {
            return Write(error, "Error");
        }

        public static bool WriteException(Exception exception)
        {
            if (exception.InnerException != null)
                Write("InnerException: " + exception.InnerException.ToString(), "Error");

            if (exception.Message != null)
                Write("Message: " + exception.Message.ToString(), "Error");

            if (exception.Source != null)
                Write("Source: " + exception.Source.ToString(), "Error");

            if (exception.StackTrace != null)
                Write("StackTrace :" + exception.StackTrace.ToString(), "Error");

            if (exception.TargetSite != null)
                Write("TargetSite :" + exception.TargetSite.ToString(), "Error");

            Write("-------------------------------------------------------------------------", "Error");
            return true;
        }

        private static bool Write(string text, string writeType)
        {
            StreamWriter f = null;
            try
            {
                string path = PATH + @"\Log";
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);

                path += @"\" + DateTime.Now.ToString("yyyyMMdd") + writeType + ".txt";
                if (File.Exists(path))
                    f = File.AppendText(path);
                else
                    f = File.CreateText(path);

                f.WriteLine(text);
                return true;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
                return false;
            }
            finally
            {
                f?.Close();
            }
        }
    }
}
