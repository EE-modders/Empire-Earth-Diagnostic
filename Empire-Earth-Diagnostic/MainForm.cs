using Empire_Earth_Diagnostic.Properties;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management;
using System.Net;
using System.Security.Principal;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace Empire_Earth_Diagnostic
{
    public partial class MainForm : Form
    {

        RegistryKey key;
        string base_reg;
        string upd_url;

        public MainForm(string base_reg, string upd_url = null)
        {
            InitializeComponent();
            this.Icon = Resources.eediag;
            versionLabel.Text = "Diagnostic v" + Application.ProductVersion;

            this.base_reg = base_reg;
            this.upd_url = upd_url;

            if (string.IsNullOrWhiteSpace(base_reg))
            {
                MessageBox.Show("Unnable to read the linked reg path!", "Reg path was empty");
                Environment.Exit(0);
            }

            if (KeyExists(Registry.LocalMachine, base_reg))
            {
                key = Registry.LocalMachine;
                if (!IsElevated())
                    RestartAdmin();
            }
            else if (KeyExists(Registry.CurrentUser, base_reg))
            {
                key = Registry.CurrentUser;
            }
            else
            {
                MessageBox.Show("Unnable to detect Empire Earth installation, did you used the Community Setup?", "Unnable to detect installation!");
                Environment.Exit(0);
            }


            regLabel.Text = key.Name;

            using (var objOS = new ManagementObjectSearcher("SELECT * FROM Win32_OperatingSystem"))
            {
                foreach (ManagementObject objMgmt in objOS.Get())
                {
                    windowsLabel.Text = objMgmt.Properties["Caption"].Value.ToString().Replace("Microsoft ", string.Empty)
                        + " " + Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE");
                }
            }

            setupNameLabel.Text = key.OpenSubKey(base_reg).GetValue("DisplayName").ToString();
            installDateLabel.Text = DateTime.ParseExact(key.OpenSubKey(base_reg).GetValue("InstallDate").ToString(),
                "yyyyMMdd", CultureInfo.InvariantCulture).ToString("dd/MM/yyyy");

            tasksLabel.Text = key.OpenSubKey(base_reg).GetValue("Inno Setup: Selected Tasks").ToString();
            componentsLabel.Text = key.OpenSubKey(base_reg).GetValue("Inno Setup: Selected Components").ToString();

            RegistryKey cdkey = key.OpenSubKey(@"Software\Sierra\CDKeys");
            if (cdkey == null)
                cdkey = key.OpenSubKey(@"Software\WOW6432Node\Sierra\CDKeys");

            if (cdkey != null)
            {
                try
                {
                    if (cdkey.GetValue("EmpireEarth").ToString().Length > 0)
                    {
                        eeCDLlabel.ForeColor = Color.Green;
                        eeCDLlabel.Text = "OK!";
                    }
                }
                catch
                {
                    eeCDLlabel.ForeColor = Color.Red;
                    eeCDLlabel.Text = "N/A!";
                }

                try
                {
                    if (cdkey.GetValue("EEAOC").ToString().Length > 0)
                    {
                        aocCDLabel.ForeColor = Color.Green;
                        aocCDLabel.Text = "OK!";
                    }
                }
                catch
                {
                    aocCDLabel.ForeColor = Color.Red;
                    aocCDLabel.Text = "N/A!";
                }
            }

            if (!string.IsNullOrWhiteSpace(upd_url))
            {
                using (WebClient webClient = new WebClient())
                {
                    try
                    {
                        string remoteSetupVersion = webClient.DownloadString(upd_url + "/setup.version");
                        if (Version.Parse(remoteSetupVersion) <=
                            Version.Parse(Regex.Split(key.OpenSubKey(base_reg).GetValue("DisplayName").ToString(), "Setup v")[1]))
                        {
                            setupUpdLabel.ForeColor = Color.Green;
                            setupUpdLabel.Text = "OK!";
                        }
                        else
                        {
                            updateButton.Visible = true;
                            setupUpdLabel.ForeColor = Color.Red;
                            setupUpdLabel.Text = "KO!";
                        }
                    }
                    catch
                    {
                        setupUpdLabel.ForeColor = Color.Gray;
                        setupUpdLabel.Text = "N/A";
                    }

                    try
                    {
                        string remoteGameVersion = webClient.DownloadString(upd_url + "/game.version");
                        if (Version.Parse(remoteGameVersion) <=
                            Version.Parse(key.OpenSubKey(base_reg).GetValue("DisplayVersion").ToString()))
                        {
                            gameUpdLabel.ForeColor = Color.Green;
                            gameUpdLabel.Text = "OK!";
                        }
                        else
                        {
                            updateButton.Visible = true;
                            gameUpdLabel.ForeColor = Color.Red;
                            gameUpdLabel.Text = "KO!";
                        }
                    }
                    catch
                    {
                        gameUpdLabel.ForeColor = Color.Gray;
                        gameUpdLabel.Text = "N/A";
                    }
                }
            } else
            {
                setupUpdLabel.ForeColor = Color.Gray;
                gameUpdLabel.Text = "N/A";
                gameUpdLabel.ForeColor = Color.Gray;
                setupUpdLabel.Text = "N/A";
            }

            string install_path_ee = key.OpenSubKey(base_reg).GetValue("Inno Setup: App Path").ToString() + @"\Empire Earth";
            string install_path_aoc = key.OpenSubKey(base_reg).GetValue("Inno Setup: App Path").ToString() + @"\Empire Earth - The Art of Conquest";
            Crc32 crcObj = new Crc32();
            string hash = string.Empty;

            if (File.Exists(install_path_ee + @"\Empire Earth.exe"))
            {
                using (FileStream fs = File.Open(install_path_ee + @"\Empire Earth.exe", FileMode.Open))
                    foreach (byte b in crcObj.ComputeHash(fs)) hash += b.ToString("x2").ToLower();

                eeCRCLabel.Text = hash.ToUpper();

                if (Directory.Exists(install_path_ee + @"\Data\Textures"))
                    eeTexturesLabel.Text = Directory.EnumerateFiles(install_path_ee + @"\Data\Textures").ToList().Count.ToString();
                if (Directory.Exists(install_path_ee + @"\Data\Models"))
                {
                    List<string> boring_edit = Directory.EnumerateFiles(install_path_ee + @"\Data\Models").ToList();
                    if (boring_edit.Contains(install_path_ee + @"\Data\Models\amb_rock.cem"))
                        boring_edit.Remove(install_path_ee + @"\Data\Models\amb_rock.cem");
                    eeModelsLabel.Text = boring_edit.Count.ToString();
                }
                if (Directory.Exists(install_path_ee + @"\Data\db"))
                    eeDBLabel.Text = Directory.EnumerateFiles(install_path_ee + @"\Data\db").ToList().Count.ToString();
            }
            else
            {
                eeCRCLabel.ForeColor = Color.Red;
                eeCRCLabel.Text = "N/A";
            }

            hash = string.Empty;
            if (File.Exists(install_path_aoc + @"\EE-AoC.exe"))
            {
                using (FileStream fs = File.Open(install_path_aoc + @"\EE-AoC.exe", FileMode.Open))
                    foreach (byte b in crcObj.ComputeHash(fs)) hash += b.ToString("x2").ToLower();

                aocCRCLabel.Text = hash.ToUpper();

                if (Directory.Exists(install_path_aoc + @"\Data\Textures"))
                    aocTexturesLabel.Text = Directory.EnumerateFiles(install_path_aoc + @"\Data\Textures").ToList().Count.ToString();
                if (Directory.Exists(install_path_aoc + @"\Data\Models"))
                {
                    List<string> boring_edit = Directory.EnumerateFiles(install_path_aoc + @"\Data\Models").ToList();
                    if (boring_edit.Contains(install_path_aoc + @"\Data\Models\amb_rock.cem"))
                        boring_edit.Remove(install_path_aoc + @"\Data\Models\amb_rock.cem");
                    aocModelsLabel.Text = boring_edit.Count.ToString();
                }
                if (Directory.Exists(install_path_aoc + @"\Data\db"))
                    aocDBLabel.Text = Directory.EnumerateFiles(install_path_aoc + @"\Data\db").ToList().Count.ToString();
            }
            else
            {
                aocCRCLabel.ForeColor = Color.Red;
                aocCRCLabel.Text = "N/A";
            }

            using (var searcher = new ManagementObjectSearcher("select * from Win32_VideoController"))
            {
                foreach (ManagementObject obj in searcher.Get())
                {
                    GPULabel.Text = obj["Name"].ToString();
                }
            }

            key.Close();
        }

        private void RestartAdmin()
        {
            using (Process configTool = new Process())
            {
                configTool.StartInfo.FileName = Process.GetCurrentProcess().MainModule.FileName;
                configTool.StartInfo.WorkingDirectory = Path.GetDirectoryName(configTool.StartInfo.FileName);
                configTool.StartInfo.Arguments = "\"" + base_reg + "\"";
                configTool.StartInfo.Arguments += string.IsNullOrWhiteSpace(upd_url) ? string.Empty : " \"" + upd_url + "\"";
                configTool.StartInfo.Verb = "runas";
                try
                {
                    configTool.Start();
                }
                catch { };
                Environment.Exit(0);
            }
        }

        private bool IsElevated()
        {
            return new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);
        }

        private bool KeyExists(RegistryKey baseKey, string subKeyName)
        {
            RegistryKey ret = baseKey.OpenSubKey(subKeyName);

            return ret != null;
        }

        private void creditLabel_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Salut !", "You find me !");
        }

        private void exitButton_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void rescanButton_Click(object sender, EventArgs e)
        {
            using (Process configTool = new Process())
            {
                configTool.StartInfo.FileName = Process.GetCurrentProcess().MainModule.FileName;
                configTool.StartInfo.WorkingDirectory = Path.GetDirectoryName(configTool.StartInfo.FileName);
                configTool.StartInfo.Arguments = "\"" + base_reg + "\"";
                configTool.StartInfo.Arguments += string.IsNullOrWhiteSpace(upd_url) ? string.Empty : " \"" + upd_url + "\"";
                try
                {
                    configTool.Start();
                }
                catch { };
                Environment.Exit(0);
            }
        }

        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HT_CAPTION = 0x2;
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        private void Form1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }

        private void gameDirButton_Click(object sender, EventArgs e)
        {
            Process.Start("explorer.exe", key.OpenSubKey(base_reg).GetValue("Inno Setup: App Path").ToString());
            key.Close();
        }
    }
}
