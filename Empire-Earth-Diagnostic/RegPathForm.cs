using Empire_Earth_Diagnostic.Properties;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace Empire_Earth_Diagnostic
{
    public partial class RegPathForm : Form
    {
        Dictionary<string, string[]> eeInstallDictionary;

        public string ResultBase { get; set; }
        public string ResultUpdUrl { get; set; }


        public RegPathForm()
        {
            InitializeComponent();
            this.Icon = Resources.eediag;
            eeInstallDictionary = new Dictionary<string, string[]>();

            if (!File.Exists("guid_dictionary"))
            {
                MessageBox.Show("Unnable to detect guid_dictionary !", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.Exit(0);
            }

            foreach (string s in File.ReadAllText("guid_dictionary").Split('\n'))
            {
                string[] splited = RemoveLineEndings(s).Split(';');
                if (splited.Length < 2)
                {
                    MessageBox.Show("Invalid entry in guid_dictionary !", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Environment.Exit(0);
                }
                string name = splited[0];
                string reg = @"Software\Microsoft\Windows\CurrentVersion\Uninstall\" + splited[1];
                string upd = splited.Length >= 3 ? splited[2] : null;
                eeInstallDictionary.Add(name, new string[] { reg, upd });
            }

            if (eeInstallDictionary.Count == 0)
            {
                MessageBox.Show("Unnable to load guid_dictionary !", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.Exit(0);
            }

            if (eeInstallDictionary.Count == 1)
            {
                ResultBase = eeInstallDictionary.First().Value[0];
                ResultUpdUrl = eeInstallDictionary.First().Value[1];
                this.Close();
            }
            else
            {
                foreach (string s in eeInstallDictionary.Keys)
                    listView1.Items.Add(s);
            }
        }

        private string RemoveLineEndings(string value)
        {
            if (String.IsNullOrEmpty(value))
            {
                return value;
            }
            string lineSeparator = ((char)0x2028).ToString();
            string paragraphSeparator = ((char)0x2029).ToString();

            return value.Replace("\r\n", string.Empty)
                        .Replace("\n", string.Empty)
                        .Replace("\r", string.Empty)
                        .Replace(lineSeparator, string.Empty)
                        .Replace(paragraphSeparator, string.Empty);
        }

        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HT_CAPTION = 0x2;
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        private void RegPathForm_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }

        private void scanButton_Click(object sender, EventArgs e)
        {
            try
            {
                ResultBase = eeInstallDictionary[listView1.SelectedItems[0].Text][0];
                ResultUpdUrl = eeInstallDictionary[listView1.SelectedItems[0].Text][1];
                this.Close();
            }
            catch
            {
                MessageBox.Show("Please select a type of installation.", "Nothing selected");
            }
        }

        private void exitButton_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }
    }
}
