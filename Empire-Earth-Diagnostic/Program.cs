using System;
using System.Windows.Forms;

namespace Empire_Earth_Diagnostic
{
    static class Program
    {
        public static Logging Logging = null;

        /// <summary>
        /// Point d'entrée principal de l'application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            Logging = new Logging();
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler((sender, e) =>
            {
                Logging.Log("Diagnostic Unhandled Exception !", (Exception)e.ExceptionObject);
                MessageBox.Show("Unhandled Exception !\nPlease contact EnergyCube to report the error.", "Unhandled Exception", MessageBoxButtons.OK);
            });
            Logging.Log("Started EE Diagnostic");

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            if (args.Length == 0)
            {
                RegPathForm reg;
                Application.Run(reg = new RegPathForm());
                Application.Run(new MainForm(reg.ResultBase, reg.ResultUpdUrl));
            }
            else if (args.Length == 1)
            {
                if (args[0].StartsWith("Software", StringComparison.InvariantCultureIgnoreCase))
                    Application.Run(new MainForm(args[0]));
                else
                    Application.Run(new MainForm(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\" + args[0]));
            }
            else if (args.Length == 2)
            {
                Application.Run(new MainForm(args[0], args[1]));
            }
        }
    }
}
