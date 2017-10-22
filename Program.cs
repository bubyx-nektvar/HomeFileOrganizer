using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HomeFileOrganizer
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            bool adminReq = false;
            AppDomain.CurrentDomain.SetPrincipalPolicy(PrincipalPolicy.WindowsPrincipal);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException);
            Form1 f = null;
            do
            {
                f= new Form1();
                try {
                        Application.Run(f);
                 }
                catch (System.Security.SecurityException e)
                {
                    MessageBox.Show("You don't have enought premission to do this action. Please start application with admin premission and repeate your request.");
                    adminReq = true;
                }
                finally
                {
                    f.Close();
                }
            }
            while (f.DialogResult == DialogResult.Retry);
            if (adminReq) {
                var pi = new ProcessStartInfo();
                pi.FileName = "HomeFileOrganizer.exe";
                pi.Verb = "runas";
                pi.UseShellExecute = true;
                Process.Start(pi);
                }
        }
    }
}
