﻿using FACS01.Utilities;
using MetroFramework;
using MetroFramework.Forms;
using Microsoft.VisualBasic;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using Newtonsoft.Json;
using SARS.Models;
using SARS.Modules;
using SARS.Properties;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Policy;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Forms;
using VRChatAPI;
using VRChatAPI.Models;

namespace SARS
{
    public partial class AvatarSystem : MetroForm
    {
        private ShrekApi shrekApi;
        public List<Avatar> avatars;
        public ConfigSave<Config> configSave;
        public ConfigSave<List<Avatar>> rippedList;
        public ConfigSave<List<Avatar>> favList;
        private HotswapConsole hotSwapConsole;
        private Thread _vrcaThread;
        private string userAgent = "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/104.0.5112.79 Safari/537.36";
        private string vrcaLocation = "";
        private VRChatAPI.VRChatApiClient VrChat;
        private string SystemName;
        private static int LatestHsbVersion = 2;
        private RootClass avatarVersionPc;
        private RootClass avatarVersionQuest;

        public AvatarSystem()
        {
            InitializeComponent();
            StyleManager = metroStyleManager1;
        }

        private void AvatarSystem_Load(object sender, EventArgs e)
        {
            typeof(DataGridView).InvokeMember("DoubleBuffered", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.SetProperty, null, avatarGrid, new object[] { true });
            string filePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            ServicePointManager.ServerCertificateValidationCallback = delegate
            {
                return true;
            };
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13;
            if (filePath.ToLower().Contains("\\local\\temp"))
            {
                MessageBox.Show("EXTRACT THE PROGRAM FIRST");
                Close();
            }
            var assembly = Assembly.GetExecutingAssembly();
            var fileVersionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);
            SystemName = "Shrek Avatar Recovery System (S.A.R.S) V" + fileVersionInfo.ProductVersion;
            this.Text = SystemName;
            txtAbout.Text = Resources.About;
            cbSearchTerm.SelectedIndex = 0;
            cbLimit.SelectedIndex = 3;
            try
            {
                configSave = new ConfigSave<Config>(filePath + "\\config.cfg");
            }
            catch
            {
                MessageBox.Show("Error with config file, settings reset");
                File.Delete(filePath + "\\config.cfg");
                Console.WriteLine("Error with config");
            }

            try
            {
                rippedList = new ConfigSave<List<Avatar>>(filePath + "\\rippedNew.cfg");
            }
            catch
            {
                MessageBox.Show("Error with ripped file, ripped list reset");
                File.Delete(filePath + "\\rippedNew.cfg");
                rippedList = new ConfigSave<List<Avatar>>(filePath + "\\rippedNew.cfg");
                Console.WriteLine("Error with ripped list");
            }

            try
            {
                favList = new ConfigSave<List<Avatar>>(filePath + "\\favNew.cfg");
            }
            catch
            {
                MessageBox.Show("Error with favorites file, favorites list reset");
                File.Delete(filePath + "\\favNew.cfg");
                favList = new ConfigSave<List<Avatar>>(filePath + "\\favNew.cfg");
                Console.WriteLine("Error with fav list");
            }

            tabControl.SelectedIndex = 0;
            try
            {
                LoadSettings();
            }
            catch { Console.WriteLine("Error loading settings"); }
            if (string.IsNullOrEmpty(configSave.Config.HotSwapName))
            {
                int randomAmount = RandomFunctions.random.Next(12);
                configSave.Config.HotSwapName = RandomFunctions.RandomString(randomAmount);
                configSave.Save();
            }
            shrekApi = new ShrekApi();

            MessageBoxManager.Yes = "PC";
            MessageBoxManager.No = "Quest";
            MessageBoxManager.Register();
        }

        private void GetLatestVersion()
        {
            System.Net.WebClient wc = new System.Net.WebClient();
            byte[] raw = wc.DownloadData("https://ares-mod.com/Latest.txt");

            string version = System.Text.Encoding.UTF8.GetString(raw);
            if (Assembly.GetExecutingAssembly().GetName().Version.ToString() != version)
            {
                MessageBox.Show($"You are running an out of date version of SARS please update to stay secure\nYour Version: {Assembly.GetExecutingAssembly().GetName().Version.ToString()}\nLatest Version: {version}");
            }
        }

        private void GetClientVersion()
        {
            System.Net.WebClient wc = new System.Net.WebClient();
            byte[] raw = wc.DownloadData("https://ares-mod.com/Version.txt");

            txtClientVersion.Text = System.Text.Encoding.UTF8.GetString(raw);
            configSave.Config.ClientVersion = txtClientVersion.Text;

            raw = wc.DownloadData("https://ares-mod.com/VersionUpdated.txt");

            configSave.Config.ClientVersionLastUpdated = Convert.ToDateTime(System.Text.Encoding.UTF8.GetString(raw));
            configSave.Save();
        }

        private void LoadSettings()
        {
            txtApiKey.Text = configSave.Config.ApiKey;
            cbThemeColour.Text = configSave.Config.ThemeColor;
            if (configSave.Config.LightMode)
            {
                metroStyleManager1.Theme = MetroThemeStyle.Light;
            }
            GetClientVersion();
            GetLatestVersion();

            if (string.IsNullOrEmpty(configSave.Config.UnityLocation))
            {
                UnitySetup();
            }

            if (string.IsNullOrEmpty(configSave.Config.MacAddress))
            {
                Random rnd = new Random();
                configSave.Config.MacAddress = EasyHash.GetSHA1String(new byte[] { (byte)rnd.Next(254), (byte)rnd.Next(254), (byte)rnd.Next(254), (byte)rnd.Next(254), (byte)rnd.Next(254) });
                configSave.Save();
            }
            if (string.IsNullOrEmpty(configSave.Config.ReuploaderMacAddress))
            {
                Random rnd = new Random();
                configSave.Config.ReuploaderMacAddress = EasyHash.GetSHA1String(new byte[] { (byte)rnd.Next(254), (byte)rnd.Next(254), (byte)rnd.Next(254), (byte)rnd.Next(254), (byte)rnd.Next(254) });
                configSave.Save();
            }
            if (!string.IsNullOrEmpty(configSave.Config.PreSelectedAvatarLocation))
            {
                txtAvatarOutput.Text = configSave.Config.PreSelectedAvatarLocation;
            }
            if (!string.IsNullOrEmpty(configSave.Config.PreSelectedWorldLocation))
            {
                txtAvatarOutput.Text = configSave.Config.PreSelectedWorldLocation;
            }

            if (configSave.Config.PreSelectedAvatarLocationChecked != null)
            {
                toggleAvatar.Checked = configSave.Config.PreSelectedAvatarLocationChecked;
            }

            if (configSave.Config.PreSelectedWorldLocation != null)
            {
                toggleWorld.Checked = configSave.Config.PreSelectedWorldLocationChecked;
            }

            chkTls10.Checked = configSave.Config.Tls10;
            chkTls11.Checked = configSave.Config.Tls11;
            chkTls12.Checked = configSave.Config.Tls12;
            chkTls13.Checked = configSave.Config.Tls13;
            chkCustomApi.Checked = configSave.Config.CustomApiUse;

            if (!string.IsNullOrEmpty(configSave.Config.CustomApi))
            {
                txtCustomApi.Text = configSave.Config.CustomApi;
            }

            chkAltApi.Checked = configSave.Config.AltAPI;

            VrChat = new VRChatApiClient(15, configSave.Config.MacAddress);
            var check = VrChat.CustomApiUser.LoginWithExistingSession(configSave.Config.AuthKey, configSave.Config.TwoFactor);
            if (check == null)
            {
                MessageBox.Show("VRChat credentials expired, please relogin");
                DeleteLoginInfo();
            }
        }

        private void UnitySetup()
        {
            var unityPath = UnityRegistry();
            if (unityPath != null)
            {
                var dlgResult =
                    MessageBox.Show(
                        $"Possible unity path found, Location: '{unityPath + @"\Editor\Unity.exe"}' is this correct?",
                        "Unity", MessageBoxButtons.YesNo);
                if (dlgResult == DialogResult.Yes)
                {
                    if (File.Exists(unityPath + @"\Editor\Unity.exe"))
                    {
                        configSave.Config.UnityLocation = unityPath + @"\Editor\Unity.exe";
                        configSave.Save();
                        MessageBox.Show(
                            "Leave the command window open it will close by itself after the unity setup is complete");
                    }
                    else
                    {
                        MessageBox.Show("Someone didn't check because that file doesn't exist!");
                        SelectFile();
                    }
                }
                else
                {
                    MessageBox.Show(
                        "Please select unity.exe, after doing this leave the command window open it will close by itself after setup is complete");
                    SelectFile();
                }
            }
            else
            {
                MessageBox.Show(
                    "Please select unity.exe, after doing this leave the command window open it will close by itself after setup is complete");
                SelectFile();
            }
        }

        private void SelectFile()
        {
            var filePath = string.Empty;

            using (var openFileDialog = new OpenFileDialog())
            {
                openFileDialog.InitialDirectory = "c:\\";
                openFileDialog.Filter = "Unity (Unity.exe)|Unity.exe";
                openFileDialog.RestoreDirectory = true;
                openFileDialog.Title = "Select Unity exe";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                    //Get the path of specified file
                    filePath = openFileDialog.FileName;
            }

            configSave.Config.UnityLocation = filePath;
            configSave.Save();
        }

        private string UnityRegistry()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Unity Technologies\Installer\Unity"))
                {
                    if (key == null) return null;
                    var o = key.GetValue("Location x64");
                    if (o != null) return o.ToString();
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        [DllImport("user32.dll")]
        private static extern int SendMessage(IntPtr hWnd, Int32 wMsg, bool wParam, Int32 lParam);

        private const int WM_SETREDRAW = 11;

        private void btnSearch_Click(object sender, EventArgs e)
        {
            string limit = cbLimit.Text;
            DateTime? before = null;
            DateTime? after = null;
            if (limit == "Max")
            {
                limit = "10000";
            }
            if (limit == "")
            {
                limit = "500";
            }
            if (chkBefore.Checked)
            {
                before = dtBefore.Value;
            }
            if (chkAfter.Checked)
            {
                after = dtAfter.Value;
            }
            AvatarSearch avatarSearch = new AvatarSearch { Key = configSave.Config.ApiKey, Amount = Convert.ToInt32(limit), PrivateAvatars = chkPrivate.Checked, PublicAvatars = chkPublic.Checked, ContainsSearch = chkContains.Checked, DebugMode = true, PcAvatars = chkPC.Checked, QuestAvatars = chkQuest.Checked };
            if (cbSearchTerm.Text == "Avatar Name")
            {
                avatarSearch.AvatarName = txtSearchTerm.Text;
            }
            else if (cbSearchTerm.Text == "Author Name")
            {
                avatarSearch.AuthorName = txtSearchTerm.Text;
            }
            else if (cbSearchTerm.Text == "Avatar ID")
            {
                avatarSearch.AvatarId = txtSearchTerm.Text;
            }
            else if (cbSearchTerm.Text == "Author ID")
            {
                avatarSearch.AuthorId = txtSearchTerm.Text;
            }
            else if (cbSearchTerm.Text == "World Name")
            {
            }
            else if (cbSearchTerm.Text == "World ID")
            {
            }
            string customApi = "";
            if (chkCustomApi.Checked)
            {
                customApi = txtCustomApi.Text;
            }
            avatars = shrekApi.AvatarSearch(avatarSearch, chkAltApi.Checked, customApi);

            avatarGrid.Rows.Clear();
            if (avatars != null)
            {
                SendMessage(avatarGrid.Handle, WM_SETREDRAW, false, 0);
                LoadData();
                SendMessage(avatarGrid.Handle, WM_SETREDRAW, true, 0);
                avatarGrid.Refresh();
                LoadImages();
            }
        }

        private void LoadData(bool local = false)
        {
            Bitmap bitmap2 = null;
            try
            {
                if (local)
                {
                    bitmap2 = new Bitmap(Resources.No_Image);
                }
                else
                {
                    bitmap2 = new Bitmap(Resources.download);
                }
            }
            catch { }

            avatarGrid.AllowUserToAddRows = true;
            avatarGrid.RowHeadersWidthSizeMode = DataGridViewRowHeadersWidthSizeMode.DisableResizing;
            for (int i = 0; i < avatars.Count; i++)
            {
                try
                {
                    DataGridViewRow row = (DataGridViewRow)avatarGrid.Rows[0].Clone();

                    row.Cells[0].Value = bitmap2;
                    row.Cells[1].Value = avatars[i].avatar.avatarName;
                    row.Cells[2].Value = avatars[i].avatar.authorName;
                    row.Cells[3].Value = avatars[i].avatar.avatarId;
                    row.Cells[4].Value = avatars[i].avatar.recordCreated;
                    row.Cells[5].Value = avatars[i].avatar.thumbnailUrl;
                    if (rippedList.Config.Any(x => x.avatar.avatarId == avatars[i].avatar.avatarId))
                    {
                        row.Cells[6].Value = true;
                    }
                    if (favList.Config.Any(x => x.avatar.avatarId == avatars[i].avatar.avatarId))
                    {
                        row.Cells[7].Value = true;
                    }
                    avatarGrid.Rows.Add(row);
                }
                catch { }
            }
            avatarGrid.AllowUserToAddRows = false;
            int count = avatarGrid.Rows.Count;

            lblAvatarCount.Text = (count).ToString();
        }

        private void LoadImages()
        {
            ThreadPool.QueueUserWorkItem(delegate
            {
                for (int i = 0; i < avatarGrid.Rows.Count; i++)
                {
                    try
                    {
                        if (avatarGrid.Rows[i] != null)
                        {
                            if (avatarGrid.Rows[i].Cells[5].Value != null)
                            {
                                if (!string.IsNullOrEmpty(avatarGrid.Rows[i].Cells[5].Value.ToString().Trim()))
                                {
                                    try
                                    {
                                        HttpWebRequest myRequest = (HttpWebRequest)WebRequest.Create(avatarGrid.Rows[i].Cells[5].Value.ToString());
                                        myRequest.Method = "GET";
                                        myRequest.UserAgent = userAgent;
                                        using (HttpWebResponse myResponse = (HttpWebResponse)myRequest.GetResponse())
                                        {
                                            if (myResponse.StatusCode == HttpStatusCode.OK)
                                            {
                                                Bitmap bmp = new Bitmap(myResponse.GetResponseStream());
                                                avatarGrid.Rows[i].Cells[0].Value = bmp;
                                            }
                                            else
                                            {
                                                Bitmap bmp = new Bitmap(Resources.No_Image);
                                                avatarGrid.Rows[i].Cells[0].Value = bmp;
                                            }
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine(ex.ToString());
                                        try
                                        {
                                            Bitmap bmp = new Bitmap(Resources.No_Image);
                                            avatarGrid.Rows[i].Cells[0].Value = bmp;
                                        }
                                        catch { }
                                    }
                                }
                            }
                        }
                    }
                    catch { }
                }
            });
        }

        private void btnViewDetails_Click(object sender, EventArgs e)
        {
            foreach (DataGridViewRow row in avatarGrid.SelectedRows)
            {
                Avatar info = avatars.FirstOrDefault(x => x.avatar.avatarId == row.Cells[3].Value);
                Avatar_Info avatar = new Avatar_Info();
                avatar.txtAvatarInfo.Text = SetAvatarInfo(info);
                avatar._selectedAvatar = info;
                avatar.Show();
            }
        }

        public string SetAvatarInfo(Avatar avatar)
        {
            string avatarString = $"Time Detected: {avatar.avatar.recordCreated} {Environment.NewLine}" +
                $"Avatar ID: {avatar.avatar.avatarId} {Environment.NewLine}" +
                $"Avatar Name: {avatar.avatar.avatarName} {Environment.NewLine}" +
                $"Avatar Description {avatar.avatar.avatarDescription} {Environment.NewLine}" +
                $"Author ID: {avatar.avatar.authorId} {Environment.NewLine}" +
                $"Author Name: {avatar.avatar.authorName} {Environment.NewLine}" +
                $"PC Asset URL: {avatar.avatar.pcAssetUrl} {Environment.NewLine}" +
                $"Quest Asset URL: {avatar.avatar.questAssetUrl} {Environment.NewLine}" +
                $"Image URL: {avatar.avatar.imageUrl} {Environment.NewLine}" +
                $"Thumbnail URL: {avatar.avatar.thumbnailUrl} {Environment.NewLine}" +
                $"Unity Version: {avatar.avatar.unityVersion} {Environment.NewLine}" +
                $"Release Status: {avatar.avatar.releaseStatus} {Environment.NewLine}" +
                $"Tags: {String.Join(", ", avatar.tags.Select(p => p.ToString()).ToArray())}";
            return avatarString;
        }

        private void btnBrowserView_Click(object sender, EventArgs e)
        {
            if (avatars != null)
            {
                GenerateHtml.GenerateHtmlPage(avatars);
                Process.Start("avatars.html");
            }
        }

        private void metroTabPage2_Click(object sender, EventArgs e)
        {
        }

        private void btnRipped_Click(object sender, EventArgs e)
        {
            avatars = rippedList.Config;
            avatarGrid.Rows.Clear();
            LoadData();
            LoadImages();
        }

        private void btnSearchFavorites_Click(object sender, EventArgs e)
        {
            avatars = favList.Config;
            avatarGrid.Rows.Clear();
            LoadData();
            LoadImages();
        }

        private void btnToggleFavorite_Click(object sender, EventArgs e)
        {
            foreach (DataGridViewRow row in avatarGrid.SelectedRows)
            {
                try
                {
                    if (!favList.Config.Any(x => x.avatar.avatarId == row.Cells[3].Value.ToString()))
                    {
                        favList.Config.Add(avatars.FirstOrDefault(x => x.avatar.avatarId == row.Cells[3].Value.ToString()));
                        row.Cells[7].Value = "true";
                    }
                    else
                    {
                        favList.Config.Remove(avatars.FirstOrDefault(x => x.avatar.avatarId == row.Cells[3].Value.ToString()));
                        row.Cells[7].Value = "false";
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Some error" + ex.Message);
                }
            }

            favList.Save();
        }

        private void avatarGrid_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            vrcaLocation = "";
            this.Text = SystemName;
            this.Update();
            this.Refresh();
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            configSave.Config.ApiKey = txtApiKey.Text.Trim();
            configSave.Save();
            if (configSave.Config.ApiKey != "")
            {
                shrekApi.checkLogin();
            }
        }

        private void btnLight_Click(object sender, EventArgs e)
        {
            metroStyleManager1.Theme = MetroThemeStyle.Light;
            configSave.Config.LightMode = true;
            configSave.Save();
        }

        private void btnDark_Click(object sender, EventArgs e)
        {
            metroStyleManager1.Theme = MetroThemeStyle.Dark;
            configSave.Config.LightMode = false;
            configSave.Save();
        }

        private void LoadStyle(string style)
        {
            switch (style)
            {
                case "Black":
                    metroStyleManager1.Style = MetroColorStyle.Black;
                    configSave.Config.ThemeColor = style;
                    configSave.Save();
                    break;

                case "White":
                    metroStyleManager1.Style = MetroColorStyle.White;
                    configSave.Config.ThemeColor = style;
                    configSave.Save();
                    break;

                case "Silver":
                    metroStyleManager1.Style = MetroColorStyle.Silver;
                    configSave.Config.ThemeColor = style;
                    configSave.Save();
                    break;

                case "Green":
                    metroStyleManager1.Style = MetroColorStyle.Green;
                    configSave.Config.ThemeColor = style;
                    configSave.Save();
                    break;

                case "Blue":
                    metroStyleManager1.Style = MetroColorStyle.Blue;
                    configSave.Config.ThemeColor = style;
                    configSave.Save();
                    break;

                case "Lime":
                    metroStyleManager1.Style = MetroColorStyle.Lime;
                    configSave.Config.ThemeColor = style;
                    configSave.Save();
                    break;

                case "Teal":
                    metroStyleManager1.Style = MetroColorStyle.Teal;
                    configSave.Config.ThemeColor = style;
                    configSave.Save();
                    break;

                case "Orange":
                    metroStyleManager1.Style = MetroColorStyle.Orange;
                    configSave.Config.ThemeColor = style;
                    configSave.Save();
                    break;

                case "Brown":
                    metroStyleManager1.Style = MetroColorStyle.Brown;
                    configSave.Config.ThemeColor = style;
                    configSave.Save();
                    break;

                case "Pink":
                    metroStyleManager1.Style = MetroColorStyle.Pink;
                    configSave.Config.ThemeColor = style;
                    configSave.Save();
                    break;

                case "Magenta":
                    metroStyleManager1.Style = MetroColorStyle.Magenta;
                    configSave.Config.ThemeColor = style;
                    configSave.Save();
                    break;

                case "Purple":
                    metroStyleManager1.Style = MetroColorStyle.Purple;
                    configSave.Config.ThemeColor = style;
                    configSave.Save();
                    break;

                case "Red":
                    metroStyleManager1.Style = MetroColorStyle.Red;
                    configSave.Config.ThemeColor = style;
                    configSave.Save();
                    break;

                case "Yellow":
                    metroStyleManager1.Style = MetroColorStyle.Yellow;
                    configSave.Config.ThemeColor = style;
                    configSave.Save();
                    break;

                default:
                    metroStyleManager1.Style = MetroColorStyle.Default;
                    configSave.Config.ThemeColor = "Default";
                    configSave.Save();
                    break;
            }
        }

        private void cbThemeColour_SelectedIndexChanged(object sender, EventArgs e)
        {
            LoadStyle(cbThemeColour.Text);
        }

        private void btnHotswap_Click(object sender, EventArgs e)
        {
            hotSwap();
        }

        private async Task<bool> hotSwap(string customAvatarId = null, string avatarName = null, string imgFileLocation = null)
        {
            if (_vrcaThread != null)
            {
                if (_vrcaThread.IsAlive)
                {
                    MessageBox.Show("VRCA Still hotswapping please try again later");
                    return false;
                }
            }

            string fileLocation = "";
            if (vrcaLocation == "")
            {
                if (avatarGrid.SelectedRows.Count > 1)
                {
                    MessageBox.Show("Please only select 1 row at a time for hotswapping.");
                    return false;
                }
                if (avatarGrid.SelectedRows.Count < 1)
                {
                    MessageBox.Show("You must at least select one avatar to hotswap");
                    return false;
                }
                Avatar avatar = null;
                foreach (DataGridViewRow row in avatarGrid.SelectedRows)
                {
                    avatar = avatars.FirstOrDefault(x => x.avatar.avatarId == avatarGrid.SelectedRows[0].Cells[3].Value);
                    if (avatar.avatar.authorId == "Unknown Cache")
                    {
                        if (!File.Exists(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + $"\\VRCA\\{RandomFunctions.ReplaceInvalidChars(avatar.avatar.avatarName)}-{avatar.avatar.avatarId}_pc.vrca"))
                        {
                            File.Copy(avatar.avatar.pcAssetUrl, Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + $"\\VRCA\\{RandomFunctions.ReplaceInvalidChars(avatar.avatar.avatarName)}-{avatar.avatar.avatarId}_pc.vrca");
                        }
                        AvatarFunctions.pcDownload = true;
                    }
                    else
                    {
                        Download download = new Download();
                        download.Show();
                        try
                        {
                            Image myImg = (row.Cells[0].Value as Image);
                            myImg.Save(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) +
                                        $"\\{configSave.Config.HotSwapName}\\Assets\\Shrek SMART\\Resources\\shrekLogo.png", ImageFormat.Png);
                            avatar = avatars.FirstOrDefault(x => x.avatar.avatarId == row.Cells[3].Value);
                        }
                        catch { }
                        await Task.Run(() => AvatarFunctions.DownloadVrcaAsync(avatar, VrChat, configSave.Config.AuthKey, nmPcVersion.Value, nmQuestVersion.Value, configSave.Config.TwoFactor, download));
                    }
                }
                if (AvatarFunctions.pcDownload)
                {
                    fileLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + $"\\VRCA\\{RandomFunctions.ReplaceInvalidChars(avatar.avatar.avatarName)}-{avatar.avatar.avatarId}_pc.vrca";
                }
                else
                {
                    fileLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + $"\\VRCA\\{RandomFunctions.ReplaceInvalidChars(avatar.avatar.avatarName)}-{avatar.avatar.avatarId}_quest.vrca";
                }
                if (!File.Exists(fileLocation))
                {
                    return false;
                }
            }
            else
            {
                fileLocation = vrcaLocation;
            }
            hotSwapConsole = new HotswapConsole();
            hotSwapConsole.Show();

            _vrcaThread = new Thread(() => HotSwap.HotswapProcess(hotSwapConsole, this, fileLocation, customAvatarId, imgFileLocation, avatarName, null));
            _vrcaThread.Start();

            return true;
        }

        private void btnUnity_Click(object sender, EventArgs e)
        {
            var tempFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)
                .Replace("\\Roaming", "");
            var unityTemp = $"\\Local\\Temp\\DefaultCompany\\{configSave.Config.HotSwapName}";
            var unityTemp2 = $"\\LocalLow\\Temp\\DefaultCompany\\{configSave.Config.HotSwapName}";

            RandomFunctions.tryDeleteDirectory(tempFolder + unityTemp, false);
            RandomFunctions.tryDeleteDirectory(tempFolder + unityTemp2, false);

            if (configSave.Config.HsbVersion != 2)
            {
                CleanHsb();
                configSave.Config.HsbVersion = 2;
                configSave.Save();
            }

            AvatarFunctions.ExtractHSB(configSave.Config.HotSwapName);
            CopyFiles();
            RandomFunctions.OpenUnity(configSave.Config.UnityLocation, configSave.Config.HotSwapName);

            btnHotswap.Enabled = true;
        }

        private void CopyFiles()
        {
            try
            {
                var programLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                File.Copy(programLocation + @"\Template\SampleScene.unity", programLocation + $"\\{configSave.Config.HotSwapName}\\Assets\\Scenes\\SampleScene.unity", true);
                File.Copy(programLocation + @"\Template\shrekLogo.png", programLocation + $"\\{configSave.Config.HotSwapName}\\Assets\\Shrek SMART\\Resources\\shrekLogo.png", true);
            }
            catch { }
        }

        private void avatarGrid_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.Value == null || e.RowIndex == -1)
                return;

            if (avatarGrid.Columns[e.ColumnIndex].AutoSizeMode != DataGridViewAutoSizeColumnMode.None)
            {
            }

            var s = e.Graphics.MeasureString(e.Value.ToString(), new Font("Segoe UI", 11, FontStyle.Regular, GraphicsUnit.Pixel));
            if (e.Value.ToString().Length / (double)avatarGrid.Columns[e.ColumnIndex].Width >= .189)
            {
                SolidBrush backColorBrush;
                if (avatarGrid.SelectedRows[0].Index == e.RowIndex)
                    backColorBrush = new SolidBrush(e.CellStyle.SelectionBackColor);
                else
                    backColorBrush = new SolidBrush(e.CellStyle.BackColor);

                using (backColorBrush)
                {
                    e.Graphics.FillRectangle(backColorBrush, e.CellBounds);
                    e.Graphics.DrawString(e.Value.ToString(), avatarGrid.Font, Brushes.Black, e.CellBounds, StringFormat.GenericDefault);
                    //avatarGrid.Rows[e.RowIndex].Height = System.Convert.ToInt32((s.Height * Math.Ceiling(s.Width / (double)avatarGrid.Columns[e.ColumnIndex].Width)));
                    e.Handled = true;
                }
            }
        }

        private void btnResetScene_Click(object sender, EventArgs e)
        {
            CopyFiles();
        }

        private void btnHsbClean_Click(object sender, EventArgs e)
        {
            CleanHsb();
        }

        private void CleanHsb()
        {
            var programLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            RandomFunctions.KillProcess("Unity Hub.exe");
            RandomFunctions.KillProcess("Unity.exe");
            RandomFunctions.tryDeleteDirectory(programLocation + $"\\{configSave.Config.HotSwapName}");
            RandomFunctions.tryDeleteDirectory(@"C:\Users\" + Environment.UserName + $"\\AppData\\Local\\Temp\\DefaultCompany\\{configSave.Config.HotSwapName}");
            RandomFunctions.tryDeleteDirectory(@"C:\Users\" + Environment.UserName + $"\\AppData\\LocalLow\\DefaultCompany\\{configSave.Config.HotSwapName}");
        }

        private void btnLoadVRCA_Click(object sender, EventArgs e)
        {
            vrcaLocation = SelectFileVrca();
        }

        private string SelectFileVrca()
        {
            var filePath = string.Empty;

            using (var openFileDialog = new OpenFileDialog())
            {
                openFileDialog.InitialDirectory = "c:\\";
                openFileDialog.Filter = "vrca files (*.vrca)|*.vrca";
                openFileDialog.RestoreDirectory = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    //Get the path of specified file
                    filePath = openFileDialog.FileName;
                    this.Text = SystemName + " | VRCA FILE LOADED";
                    this.Update();
                    this.Refresh();
                }
            }

            return filePath;
        }

        private void avatarGrid_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            vrcaLocation = "";
            this.Text = SystemName;
            this.Update();
            this.Refresh();
            if (avatarGrid.SelectedRows.Count == 1)
            {
                Avatar info = avatars.FirstOrDefault(x => x.avatar.avatarId == avatarGrid.SelectedRows[0].Cells[3].Value.ToString());
                var versions = AvatarFunctions.GetVersion(info.avatar.pcAssetUrl, info.avatar.questAssetUrl, configSave.Config.AuthKey, configSave.Config.TwoFactor, VrChat);
                avatarVersionPc = versions.Item3;
                avatarVersionQuest = versions.Item4;
                if (avatarVersionPc != null)
                {
                    nmPcVersion.Maximum = versions.Item1;
                    nmPcVersion.Value = versions.Item1;
                    txtAvatarSizePc.Text = FormatSize(avatarVersionPc.versions.FirstOrDefault(x => x.version == nmPcVersion.Value).file.sizeInBytes);
                }
                if (avatarVersionQuest != null)
                {
                    nmQuestVersion.Maximum = versions.Item2;
                    nmQuestVersion.Value = versions.Item2;
                    txtAvatarSizeQuest.Text = FormatSize(avatarVersionQuest.versions.FirstOrDefault(x => x.version == nmQuestVersion.Value).file.sizeInBytes);
                }
                else
                {
                    nmQuestVersion.Maximum = 1;
                    txtAvatarSizeQuest.Text = "0MB";
                }
            }
        }

        // Load all suffixes in an array
        private static readonly string[] suffixes =
        { "Bytes", "KB", "MB", "GB", "TB", "PB" };

        public string FormatSize(Int64 bytes)
        {
            try
            {
                int counter = 0;
                decimal number = (decimal)bytes;
                while (Math.Round(number / 1024) >= 1)
                {
                    number = number / 1024;
                    counter++;
                }
                return string.Format("{0:n1}{1}", number, suffixes[counter]);
            }
            catch
            {
                return "0MB";
            }
        }

        private void DeleteLoginInfo()
        {
            configSave.Config.UserId = null;
            configSave.Config.AuthKey = null;
            configSave.Config.TwoFactor = null;
            configSave.Save();

            try
            {
                _ = VrChat.CustomApiUser.Logout().Result;
            }
            catch
            {
            }
            try
            {
                File.Delete("auth.txt");
            }
            catch { }

            try
            {
                File.Delete("2fa.txt");
            }
            catch { }
        }

        private void btnSaveVRC_Click(object sender, EventArgs e)
        {
            try
            {
                DeleteLoginInfo();
            }
            catch
            {
            }
            if (txtVRCUsername.Text != "" && txtVRCPassword.Text != "")
            {
                try
                {
                    _ = VrChat.CustomApiUser.Login(txtVRCUsername.Text, txtVRCPassword.Text, CustomApiUser.VerifyTwoFactorAuthCode).Result;
                }
                catch (Exception ex)
                {
                    if (ex.Message == "Couldn't verify 2FA code")
                    {
                        MessageBox.Show(ex.Message);
                    }
                }
                if (string.IsNullOrEmpty(StaticValues.UserId))
                {
                    MessageBox.Show("Login Failed (Did VRChat ask about a new location in emails?)");
                }
                else
                {
                    configSave.Config.UserId = StaticValues.UserId;
                    configSave.Config.AuthKey = StaticValues.AuthKey;
                    configSave.Config.TwoFactor = StaticValues.TwoFactor;
                    configSave.Save();
                    MessageBox.Show("Login Successful");
                }
            }
        }

        private void btnDownload_Click(object sender, EventArgs e)
        {
            Download();
        }

        private async Task<bool> Download()
        {
            if (string.IsNullOrEmpty(configSave.Config.UserId))
            {
                MessageBox.Show("Please Login with an alt first.");
                return false;
            }
            if (avatarGrid.SelectedRows.Count > 1)
            {
                Download download = new Download();
                download.Show();
                Avatar avatar = null;
                foreach (DataGridViewRow row in avatarGrid.SelectedRows)
                {
                    avatar = avatars.FirstOrDefault(x => x.avatar.avatarId == row.Cells[3].Value);
                    await Task.Run(() => AvatarFunctions.DownloadVrcaAsync(avatar, VrChat, configSave.Config.AuthKey, 0, 0, configSave.Config.TwoFactor, download));
                }
            }
            else
            {
                Avatar avatar = null;
                foreach (DataGridViewRow row in avatarGrid.SelectedRows)
                {
                    Download download = new Download();
                    download.Show();
                    avatar = avatars.FirstOrDefault(x => x.avatar.avatarId == row.Cells[3].Value);
                    await Task.Run(() => AvatarFunctions.DownloadVrcaAsync(avatar, VrChat, configSave.Config.AuthKey, nmPcVersion.Value, nmQuestVersion.Value, configSave.Config.TwoFactor, download));
                }
            }
            return true;
        }

        private async void btnExtractVRCA_Click(object sender, EventArgs e)
        {
            if (avatarGrid.SelectedRows.Count == 1 || vrcaLocation != "")
            {
                string avatarFile;
                Avatar avatar = null;
                if (vrcaLocation == "")
                {
                    Download download = new Download();
                    download.Show();
                    avatar = avatars.FirstOrDefault(x => x.avatar.avatarId == avatarGrid.SelectedRows[0].Cells[3].Value);
                    if (await Task.Run(() => AvatarFunctions.DownloadVrcaAsync(avatar, VrChat, configSave.Config.AuthKey, nmPcVersion.Value, nmQuestVersion.Value, configSave.Config.TwoFactor, download)) == false) return;
                    avatarFile = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + $"\\VRCA\\{avatar.avatar.avatarName}-{avatar.avatar.avatarId}_pc.vrca";
                }
                else
                {
                    avatarFile = vrcaLocation;
                }

                if (File.Exists(avatarFile) && File.Exists(avatarFile.Replace("_pc", "_quest")))
                {
                    var dlgResult = MessageBox.Show("Select which version to extract", "VRCA Select",
                       MessageBoxButtons.YesNo, MessageBoxIcon.Information);

                    if (dlgResult == DialogResult.No)
                    {
                        avatarFile = avatarFile.Replace("_pc", "_quest");
                    }
                }
                else
                {
                    if (!File.Exists(avatarFile))
                    {
                        if (File.Exists(avatarFile.Replace("_pc", "_quest")))
                        {
                            avatarFile = avatarFile.Replace("_pc", "_quest");
                        }
                        else
                        {
                            MessageBox.Show("Something went wrong with avatar file location, either it failed to download or the file doesn't exist");
                            return;
                        }
                    }
                }

                var folderDlg = new FolderBrowserDialog
                {
                    ShowNewFolderButton = true
                };
                // Show the FolderBrowserDialog.
                var result = DialogResult.OK;
                if (!toggleAvatar.Checked || txtAvatarOutput.Text == "")
                    result = folderDlg.ShowDialog();
                else
                    folderDlg.SelectedPath = txtAvatarOutput.Text;
                if (result == DialogResult.OK || toggleAvatar.Checked && txtAvatarOutput.Text != "")
                {
                    var filePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                    var invalidFileNameChars = Path.GetInvalidFileNameChars();
                    var folderExtractLocation = folderDlg.SelectedPath + @"\" + Path.GetFileNameWithoutExtension(avatarFile);
                    if (!Directory.Exists(folderExtractLocation)) Directory.CreateDirectory(folderExtractLocation);
                    var commands =
                        string.Format(
                            "/C AssetRipper.exe \"{1}\" -o \"{0}\" -q ",
                             folderExtractLocation, avatarFile);

                    var p = new Process();
                    var psi = new ProcessStartInfo
                    {
                        FileName = "CMD.EXE",
                        Arguments = commands,
                        WorkingDirectory = filePath + @"\AssetRipperConsole_win64"
                    };
                    p.StartInfo = psi;
                    p.Start();
                    p.WaitForExit();

                    RandomFunctions.tryDeleteDirectory(folderExtractLocation + @"\AssetRipper\GameAssemblies", false);
                    try
                    {
                        Directory.Move(folderExtractLocation + @"\Assets\Shader",
                            folderExtractLocation + @"\Assets\.Shader");
                    }
                    catch
                    {
                    }
                    try
                    {
                        Directory.Move(folderExtractLocation + @"\Assets\Scripts",
                            folderExtractLocation + @"\Assets\.Scripts");
                    }
                    catch
                    {
                    }
                    RandomFunctions.tryDeleteDirectory(folderExtractLocation + @"\AuxiliaryFiles", false);
                    RandomFunctions.tryDeleteDirectory(folderExtractLocation + @"\ExportedProject\AssetRipper", false);
                    RandomFunctions.tryDeleteDirectory(folderExtractLocation + @"\ExportedProject\ProjectSettings", false);
                    try
                    {
                        Directory.Move(folderExtractLocation + @"\ExportedProject\Assets\Shader",
                            folderExtractLocation + @"\ExportedProject\Assets\.Shader");
                        Directory.Move(folderExtractLocation + @"\ExportedProject\Assets\Scripts",
                            folderExtractLocation + @"\ExportedProject\Assets\.Scripts");
                        Directory.Move(folderExtractLocation + @"\ExportedProject\Assets\MonoScript",
                            folderExtractLocation + @"\ExportedProject\Assets\.MonoScript");
                    }
                    catch
                    {
                    }
                    FixVRC3Scripts fixVRC3Scripts = new FixVRC3Scripts();
                    fixVRC3Scripts.FixScripts(folderExtractLocation);

                    if (vrcaLocation == "")
                    {
                        rippedList.Config.Add(avatar);
                        rippedList.Save();
                    }
                }
            }
            else
            {
                MetroMessageBox.Show(this, "Please select an avatar or world first.", "ERROR", MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void btnUnityLoc_Click(object sender, EventArgs e)
        {
            SelectFile();
        }

        private void btnPreview_Click(object sender, EventArgs e)
        {
            Preview();
        }

        private async Task<bool> Preview()
        {
            string fileLocation;
            if (avatarGrid.SelectedRows.Count > 0)
            {
                foreach (DataGridViewRow row in avatarGrid.SelectedRows)
                {
                    Avatar avatar = avatars.FirstOrDefault(x => x.avatar.avatarId == row.Cells[3].Value);
                    if (avatar.avatar.authorId == "Unknown Cache")
                    {
                        if (!File.Exists(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + $"\\VRCA\\{RandomFunctions.ReplaceInvalidChars(avatar.avatar.avatarName)}-{avatar.avatar.avatarId}_pc.vrca"))
                        {
                            File.Copy(avatar.avatar.pcAssetUrl, Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + $"\\VRCA\\{RandomFunctions.ReplaceInvalidChars(avatar.avatar.avatarName)}-{avatar.avatar.avatarId}_pc.vrca");
                        }
                        fileLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + $"\\VRCA\\{RandomFunctions.ReplaceInvalidChars(avatar.avatar.avatarName)}-{avatar.avatar.avatarId}_pc.vrca";
                        try
                        {
                            string commands = string.Format($"\"{fileLocation}\"");
                            Console.WriteLine(commands);
                            Process p = new Process();
                            ProcessStartInfo psi = new ProcessStartInfo
                            {
                                FileName = "AssetViewer.exe",
                                Arguments = commands,
                                WorkingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\NewestViewer\",
                            };
                            p.StartInfo = psi;
                            p.Start();
                        }
                        catch (Exception ex) { Console.WriteLine(ex.Message); }
                        return true;
                    }
                }
            }
            if (string.IsNullOrEmpty(configSave.Config.UserId) && vrcaLocation == "")
            {
                MessageBox.Show("Please Login with an alt first.");
                return false;
            }

            if (vrcaLocation == "")
            {
                if (avatarGrid.SelectedRows.Count > 1)
                {
                    MessageBox.Show("Please only select 1 row at a time for hotswapping.");
                    return false;
                }
                if(avatarGrid.SelectedRows.Count == 0)
                {
                    MessageBox.Show("Please select an avatar first");
                    return false;
                }
                bool downloaded = false;
                Avatar avatar = null;
                foreach (DataGridViewRow row in avatarGrid.SelectedRows)
                {
                    Download download = new Download();
                    download.Show();
                    avatar = avatars.FirstOrDefault(x => x.avatar.avatarId == row.Cells[3].Value);
                    await Task.Run(() => AvatarFunctions.DownloadVrcaAsync(avatar, VrChat, configSave.Config.AuthKey, nmPcVersion.Value, nmQuestVersion.Value, configSave.Config.TwoFactor, download));
                }
                fileLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + $"\\VRCA\\{RandomFunctions.ReplaceInvalidChars(avatar.avatar.avatarName)}-{avatar.avatar.avatarId}_pc.vrca";
            }
            else
            {
                if (string.IsNullOrEmpty(vrcaLocation))
                {
                    MessageBox.Show("Please select an avatar first or load an VRCA file");
                    return false;
                }
                fileLocation = vrcaLocation;
            }

            if (File.Exists(fileLocation) && File.Exists(fileLocation.Replace("_pc", "_quest")))
            {
                var dlgResult = MessageBox.Show("Select which version to extract", "VRCA Select",
                   MessageBoxButtons.YesNo, MessageBoxIcon.Information);

                if (dlgResult == DialogResult.No)
                {
                    fileLocation = fileLocation.Replace("_pc", "_quest");
                }
            }
            else
            {
                if (!File.Exists(fileLocation))
                {
                    if (File.Exists(fileLocation.Replace("_pc", "_quest")))
                    {
                        fileLocation = fileLocation.Replace("_pc", "_quest");
                    }
                    else
                    {
                        MessageBox.Show("Avatar file doesn't exist");
                        return false;
                    }
                }
            }

            try
            {
                string commands = string.Format($"\"{fileLocation}\"");
                Console.WriteLine(commands);
                Process p = new Process();
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "AssetViewer.exe",
                    Arguments = commands,
                    WorkingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\NewestViewer\",
                };
                p.StartInfo = psi;
                p.Start();
            }
            catch (Exception ex) { Console.WriteLine(ex.Message); }
            return true;
        }

        private void tabControl_SelectedIndexChanged(object sender, EventArgs e)
        {
        }

        private void btnCheck_Click(object sender, EventArgs e)
        {
            if (configSave.Config.AuthKey != null)
            {
                var check = VrChat.CustomApiUser.LoginWithExistingSession(configSave.Config.AuthKey, configSave.Config.TwoFactor);
                if (check == null)
                {
                    MessageBox.Show("VRChat credentials expired, please relogin");
                    DeleteLoginInfo();
                }
                else
                {
                    MessageBox.Show("Token Works :D");
                }
            }
            else
            {
                MessageBox.Show("Login First");
            }
        }

        private void btnAvatarOut_Click(object sender, EventArgs e)
        {
            var folderDlg = new CommonOpenFileDialog { IsFolderPicker = true, InitialDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) };
            var result = folderDlg.ShowDialog();
            if (result == CommonFileDialogResult.Ok)
            {
                txtAvatarOutput.Text = folderDlg.FileName;
                configSave.Config.PreSelectedAvatarLocation = folderDlg.FileName;
                configSave.Save();
            }
        }

        private void btnWorldOut_Click(object sender, EventArgs e)
        {
            var folderDlg = new CommonOpenFileDialog { IsFolderPicker = true, InitialDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) };
            var result = folderDlg.ShowDialog();
            if (result == CommonFileDialogResult.Ok)
            {
                txtWorldOutput.Text = folderDlg.FileName;
                configSave.Config.PreSelectedWorldLocation = folderDlg.FileName;
                configSave.Save();
            }
        }

        private void toggleAvatar_CheckedChanged(object sender, EventArgs e)
        {
            configSave.Config.PreSelectedAvatarLocationChecked = toggleAvatar.Checked;
            configSave.Save();
        }

        private void toggleWorld_CheckedChanged(object sender, EventArgs e)
        {
            configSave.Config.PreSelectedWorldLocationChecked = toggleWorld.Checked;
            configSave.Save();
        }

        private void btn2FA_Click(object sender, EventArgs e)
        {
            Process.Start("https://support.google.com/accounts/answer/1066447?hl=en&ref_topic=2954345");
        }

        private void chkAltApi_CheckedChanged(object sender, EventArgs e)
        {
            configSave.Config.AltAPI = chkAltApi.Checked;
            configSave.Save();
        }

        private void btnUnityLoc_Click_1(object sender, EventArgs e)
        {
            SelectFile();
        }

        private void chkTls13_CheckedChanged(object sender, EventArgs e)
        {
            if (chkTls13.Checked)
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls13;
                chkTls10.Checked = false;
                chkTls11.Checked = false;
                chkTls12.Checked = false;
            }
            configSave.Config.Tls13 = chkTls13.Checked;
            configSave.Save();
        }

        private void chkTls12_CheckedChanged(object sender, EventArgs e)
        {
            if (chkTls12.Checked)
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                chkTls10.Checked = false;
                chkTls11.Checked = false;
                chkTls13.Checked = false;
            }
            configSave.Config.Tls12 = chkTls12.Checked;
            configSave.Save();
        }

        private void chkTls11_CheckedChanged(object sender, EventArgs e)
        {
            if (chkTls11.Checked)
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls11;
                chkTls10.Checked = false;
                chkTls13.Checked = false;
                chkTls12.Checked = false;
            }
            configSave.Config.Tls11 = chkTls11.Checked;
            configSave.Save();
        }

        private void chkTls10_CheckedChanged(object sender, EventArgs e)
        {
            if (chkTls10.Checked)
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls;
                chkTls13.Checked = false;
                chkTls11.Checked = false;
                chkTls12.Checked = false;
            }
            configSave.Config.Tls10 = chkTls10.Checked;
            configSave.Save();
        }

        private void btnCustomSave_Click(object sender, EventArgs e)
        {
            configSave.Config.CustomApi = txtCustomApi.Text;
            configSave.Save();
        }

        private void chkCustomApi_CheckedChanged(object sender, EventArgs e)
        {
            configSave.Config.CustomApiUse = chkCustomApi.Checked;
            configSave.Save();
        }

        private void nmPcVersion_ValueChanged(object sender, EventArgs e)
        {
            if (avatarVersionPc != null && nmPcVersion.Value > 0)
            {
                txtAvatarSizePc.Text = FormatSize(avatarVersionPc.versions.FirstOrDefault(x => x.version == nmPcVersion.Value).file.sizeInBytes);
            }
        }

        private void nmQuestVersion_ValueChanged(object sender, EventArgs e)
        {
            if (avatarVersionQuest != null && nmQuestVersion.Value > 0)
            {
                txtAvatarSizeQuest.Text = FormatSize(avatarVersionQuest.versions.FirstOrDefault(x => x.version == nmQuestVersion.Value).file.sizeInBytes);
            }
        }

        private void btnScanCacheFolder_Click(object sender, EventArgs e)
        {
            string cachePath = $"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}Low\\VRChat\\VRChat\\Cache-WindowsPlayer";

            CommonOpenFileDialog dialog = new CommonOpenFileDialog { IsFolderPicker = true };
            dialog.Title = "Select your VrChat Cache folder called Cache-WindowsPlayer";

            if (Directory.Exists(cachePath))
            {
                dialog.InitialDirectory = cachePath;
            }

            if (dialog.ShowDialog() != CommonFileDialogResult.Ok)
            {
                MessageBox.Show("No Folder selected");
                return;
            }

            cachePath = dialog.FileName;
            CacheMessages.Enabled = true;
            CacheScanner.ScanCache(cachePath);
        }

        private void btnReupload_Click(object sender, EventArgs e)
        {
            //if (_vrcaThread != null)
            //{
            //    if (_vrcaThread.IsAlive)
            //    {
            //        MessageBox.Show("VRCA Still hotswapping please try again later");
            //        return;
            //    }
            //}
            //if (File.Exists("target_pc.vrca"))
            //{
            //    File.Delete("target_pc.vrca");
            //}
            //if (File.Exists("target_quest.vrca"))
            //{
            //    File.Delete("target_quest.vrca");
            //}
            //if (configSave.Config.ReuploaderUserId == null)
            //{
            //    MessageBox.Show("Login with VRChat account first");
            //    return;
            //}

            //ReuploaderVrChat._userId = configSave.Config.UserId;
            //ReuploaderVrChat._authCookie = configSave.Config.ReuploaderAuthKey;
            //ReuploaderVrChat._twoFactor = configSave.Config.ReuploaderTwoFactor;
            //string avatarName = SetUpAvatarName();
            //if (string.IsNullOrEmpty(avatarName))
            //{
            //    MessageBox.Show("Please enter an avatar name");
            //    return;
            //}

            //string imgFileLocation = FileFitler("png", "png files (*.png)|*.png");

            //if (string.IsNullOrEmpty(imgFileLocation))
            //{
            //    MessageBox.Show("Please Select A image File");
            //    return;
            //}

            //string avatarId = $"avtr_{Guid.NewGuid()}";

            //BaseUploader._avatarName = avatarName;

            ////try
            ////{
            ////    ReuploaderVrChat.VRChatApiUser.Logout();
            ////}
            ////catch { }

            //ReUploader(avatarId, avatarName, imgFileLocation);
        }

        //private async Task<bool> ReUploader(string avatarId, string avatarName, string imgFileLocation)
        //{
        //string fileLocation = "";
        //string questLocation = "";

        //if (vrcaLocation == "")
        //{
        //    if (avatarGrid.SelectedRows.Count > 1)
        //    {
        //        MessageBox.Show("Please only select 1 row at a time for hotswapping.");
        //        return false;
        //    }
        //    if (avatarGrid.SelectedRows.Count < 1)
        //    {
        //        MessageBox.Show("Please only select 1 row at a time for hotswapping.");
        //        return false;
        //    }
        //    Avatar avatar = new Avatar();
        //    foreach (DataGridViewRow row in avatarGrid.SelectedRows)
        //    {
        //        try
        //        {
        //            avatar = avatars.FirstOrDefault(x => x.avatar.avatarId == row.Cells[3].Value);
        //        }
        //        catch { }
        //        if (avatar.avatar.authorId == "Unknown Cache")
        //        {
        //            if (!File.Exists(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + $"\\VRCA\\{RandomFunctions.ReplaceInvalidChars(avatar.avatar.avatarName)}-{avatar.avatar.avatarId}_pc.vrca"))
        //            {
        //                File.Copy(avatar.avatar.pcAssetUrl, Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + $"\\VRCA\\{RandomFunctions.ReplaceInvalidChars(avatar.avatar.avatarName)}-{avatar.avatar.avatarId}_pc.vrca");
        //            }
        //            fileLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + $"\\VRCA\\{RandomFunctions.ReplaceInvalidChars(avatar.avatar.avatarName)}-{avatar.avatar.avatarId}_pc.vrca";
        //            AvatarFunctions.pcDownload = true;
        //        }
        //        else
        //        {
        //            Download download = new Download();
        //            download.Show();
        //            try
        //            {
        //                avatar = avatars.FirstOrDefault(x => x.avatar.avatarId == row.Cells[3].Value);
        //                await Task.Run(() => AvatarFunctions.DownloadVrcaAsync(avatar, VrChat, configSave.Config.AuthKey, nmPcVersion.Value, nmQuestVersion.Value, configSave.Config.TwoFactor, download, true));
        //            }
        //            catch (Exception ex)
        //            {
        //                Console.WriteLine(ex.ToString());
        //            }

        //            fileLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + $"\\VRCA\\{RandomFunctions.ReplaceInvalidChars(avatar.avatar.avatarName)}-{avatar.avatar.avatarId}_pc.vrca";
        //            questLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + $"\\VRCA\\{RandomFunctions.ReplaceInvalidChars(avatar.avatar.avatarName)}-{avatar.avatar.avatarId}_quest.vrca";
        //        }
        //    }
        //    if (!File.Exists(fileLocation))
        //    {
        //        return false;
        //    }
        //}
        //else
        //{
        //    fileLocation = vrcaLocation;
        //}

        //hotSwapConsole = new HotswapConsole();
        //hotSwapConsole.Show();

        //await Task.Run(() => HotSwap.HotswapProcess(hotSwapConsole, this, fileLocation, avatarId, imgFileLocation, avatarName, null));

        //var filePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        //var fileTarget = filePath + @"\target.vrca";

        //if (File.Exists(fileTarget))
        //{
        //    File.Move(fileTarget, fileTarget.Replace(".vrca", "_pc.vrca"));

        //    if (!string.IsNullOrEmpty(questLocation))
        //    {
        //        fileLocation = questLocation;
        //        if (File.Exists(fileLocation))
        //        {
        //            hotSwapConsole = new HotswapConsole();
        //            hotSwapConsole.Show();
        //            await Task.Run(() => HotSwap.HotswapProcess(hotSwapConsole, this, questLocation, avatarId, imgFileLocation, avatarName, null));
        //            File.Move(fileTarget, fileTarget.Replace(".vrca", "_quest.vrca"));
        //        }
        //        else
        //        {
        //            questLocation = null;
        //        }
        //    }

        //    if (ReuploaderVrChat != null)
        //    {
        //        ReuploaderVrChat._vrChatApiUser = ReuploaderVrChat._vrchatLoginHandle.LoginWithExistingSession(ReuploaderVrChat._authCookie, ReuploaderVrChat._twoFactor).Result;
        //        StoredValues.VrchatApiClient = ReuploaderVrChat;
        //        if (!string.IsNullOrEmpty(questLocation))
        //        {
        //            _ = BaseUploader.UploadAvatarAsync(avatarName, fileTarget.Replace(".vrca", "_pc.vrca"), fileTarget.Replace(".vrca", "_quest.vrca"), imgFileLocation, avatarId);
        //        }
        //        else
        //        {
        //            _ = BaseUploader.UploadAvatarAsync(avatarName, fileTarget.Replace(".vrca", "_pc.vrca"), null, imgFileLocation, avatarId);
        //        }
        //        MessageBox.Show($"Avatar Uploaded, Avatar ID: {avatarId}");
        //    }
        //    RandomFunctions.tryDelete(fileTarget.Replace(".vrca", "_pc.vrca"));
        //    RandomFunctions.tryDelete(fileTarget.Replace(".vrca", "_quest.vrca"));
        //    return true;
        //}
        //return false;
        //}

        private string FileFitler(string defaultExt, string filter)
        {
            OpenFileDialog imgFile = new OpenFileDialog
            {
                InitialDirectory = @"C:\",
                Title = "Browse Files",

                CheckFileExists = true,
                CheckPathExists = true,

                DefaultExt = defaultExt,
                Filter = filter,
                FilterIndex = 2,
                RestoreDirectory = true,

                ReadOnlyChecked = true,
                ShowReadOnly = true
            };
            if (imgFile.ShowDialog() == DialogResult.OK)
            {
                return imgFile.FileName;
            }
            else
            {
                return null;
            }
        }

        private static string SetUpAvatarName()
        {
            string input = Interaction.InputBox("Enter Avatar Name", "Avatar Name", "");
            return input;
        }

        private void btnReLogin_Click(object sender, EventArgs e)
        {
            //try
            //{
            //    DeleteLoginInfo();
            //}
            //catch
            //{
            //}
            //if (txtReUsername.Text != "" && txtRePassword.Text != "")
            //{
            //    _ = ReuploaderVrChat._vrchatLoginHandle.Login(txtReUsername.Text, txtRePassword.Text, VRChatLoginHandle.VerifyTwoFactorAuthCode).Result;
            //    if (string.IsNullOrEmpty(ReuploaderVrChat._userId))
            //    {
            //        MessageBox.Show("Error, Unable to login: invalid credentials");
            //        return;
            //    }
            //    else
            //    {
            //        configSave.Config.ReuploaderUserId = ReuploaderVrChat._userId;
            //        configSave.Config.ReuploaderAuthKey = ReuploaderVrChat._authCookie;
            //        configSave.Config.ReuploaderTwoFactor = ReuploaderVrChat._twoFactor;
            //        configSave.Save();
            //        MessageBox.Show("Login Successful");
            //    }
            //}
        }

        private void chkReuploaderEnabled_CheckedChanged(object sender, EventArgs e)
        {
            if (chkReuploaderEnabled.Checked)
            {
                btnReupload.Enabled = true;
            }
            else
            {
                btnReupload.Enabled = false;
            }
        }

        private void CacheMessages_Tick(object sender, EventArgs e)
        {
            if (CacheScanner.messages.Count == 0) return;
            lock (CacheScanner.messages)
            {
                foreach (var message in CacheScanner.messages)
                {
                    txtCacheScannerLog.Text = message + txtCacheScannerLog.Text;
                }
                CacheScanner.messages.Clear();
            }
        }

        private void btnLoadResults_Click(object sender, EventArgs e)
        {
            List<Avatar> list = new List<Avatar>();
            foreach (var item in CacheScanner.avatarIds)
            {
                Avatar avatar = new Avatar { tags = new List<string>(), avatar = new AvatarDetails { thumbnailUrl = "https://ares-mod.com/avatars/Image_not_available.png", imageUrl = "https://ares-mod.com/avatars/Image_not_available.png", pcAssetUrl = item.FileLocation, avatarId = item.AvatarId, avatarName = "From cache no names", avatarDescription = "Avatar is from the game cache no names are located", recordCreated = item.AvatarDetected, releaseStatus = "????", unityVersion = "????", questAssetUrl = "None", authorName = "Unknown", authorId = "Unknown Cache" } };
                list.Add(avatar);
            }
            avatars = list;
            avatarGrid.Rows.Clear();
            if (avatars != null)
            {
                SendMessage(avatarGrid.Handle, WM_SETREDRAW, false, 0);
                LoadData(true);
                SendMessage(avatarGrid.Handle, WM_SETREDRAW, true, 0);
                avatarGrid.Refresh();
            }
        }

        private void txtClientVersion_TextChanged(object sender, EventArgs e)
        {
            StaticValues.ClientVersion = txtClientVersion.Text;
        }

        private void btnNewPreview_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(configSave.Config.ApiKey))
            {
                SARS sars = new SARS();
                sars.Show();
            }
            else
            {
                MessageBox.Show("Please enter API Key first");
            }
        }

        private VRChatCacheResult GetDetails(string avatarId)
        {
            using (WebClient webClient = new WebClient())
            {
                try
                {
                    webClient.BaseAddress = "https://api.vrchat.cloud";
                    webClient.Headers.Add("Accept", $"*/*");
                    webClient.Headers.Add("Cookie", $"auth={configSave.Config.AuthKey}; twoFactorAuth={configSave.Config.TwoFactor}");
                    webClient.Headers.Add("X-MacAddress", StaticValues.MacAddress);
                    webClient.Headers.Add("X-Client-Version",
                            StaticValues.ClientVersion);
                    webClient.Headers.Add("X-Platform",
                            "standalonewindows");
                    webClient.Headers.Add("user-agent",
                            "VRC.Core.BestHTTP");
                    webClient.Headers.Add("X-Unity-Version",
                            "2019.4.40f1");
                    string jsonString = webClient.DownloadString(new Uri($"https://api.vrchat.cloud/api/1/avatars/{avatarId}"));
                    VRChatCacheResult vrChatCacheResult = JsonConvert.DeserializeObject<VRChatCacheResult>(jsonString);
                    return vrChatCacheResult;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
            return null;
        }

        private void UploadCacheResult(VRChatCacheResult model)
        {
            AvatarDetailsSend avatarDetails = new AvatarDetailsSend
            {
                authorId = model.authorId,
                authorName = model.authorName,
                imageUrl = model.imageUrl,
                avatarDescription = model.description,
                avatarId = model.id,
                avatarName = model.name,
                recordCreated = DateTime.Now,
                releaseStatus = model.releaseStatus,
                thumbnailUrl = model.thumbnailImageUrl,
                tags = String.Join(",",model.tags)
            };
            if (model.unityPackages != null)
            {
                if (model.unityPackages.FirstOrDefault(x => x.platform.ToLower() == "standalonewindows") != null)
                {
                    avatarDetails.pcAssetUrl = model.unityPackages.FirstOrDefault(x => x.platform.ToLower() == "standalonewindows").assetUrl;
                    avatarDetails.unityVersion = model.unityPackages.FirstOrDefault(x => x.platform.ToLower() == "standalonewindows").unityVersion;
                }
                if (model.unityPackages.FirstOrDefault(x => x.platform.ToLower() == "android") != null)
                {
                    avatarDetails.questAssetUrl = model.unityPackages.FirstOrDefault(x => x.platform.ToLower() == "android").assetUrl;
                    avatarDetails.unityVersion = model.unityPackages.FirstOrDefault(x => x.platform.ToLower() == "android").unityVersion;
                } else
                {
                    avatarDetails.questAssetUrl = "";
                }
            }
            if (string.IsNullOrEmpty(avatarDetails.tags))
            {
                avatarDetails.tags = "None";
            }

            if (!string.IsNullOrEmpty(avatarDetails.pcAssetUrl) || !string.IsNullOrEmpty(avatarDetails.questAssetUrl))
            {
                try
                {
                    string apiUrl = "https://unlocked.modvrc.com/Avatar/AddModel";
                    var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiUrl);
                    httpWebRequest.ContentType = "application/json";
                    httpWebRequest.Method = "POST";
                    httpWebRequest.UserAgent = $"SARS" + Assembly.GetExecutingAssembly().GetName().Version.ToString();
                    string jsonPost = JsonConvert.SerializeObject(avatarDetails);
                    using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
                    {
                        streamWriter.Write(jsonPost);
                    }
                    try
                    {
                        var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                        using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                        {
                            var result = streamReader.ReadToEnd();
                            bool avatarResponse = JsonConvert.DeserializeObject<bool>(result);
                            if (avatarResponse)
                            {
                                Console.WriteLine("Avatar Added");
                            }
                            else if (!avatarResponse)
                            {
                                Console.WriteLine("Avatar already on API");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Unknown Error: {ex.Message}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
                try
                {
                    if (avatarDetails.questAssetUrl != "")
                    {
                        using (var client = new WebClient())
                        {
                            string url = $"https://unlocked.modvrc.com/Avatar/AddQuestSideCheat?questUrl={System.Uri.EscapeDataString(avatarDetails.questAssetUrl)}&avatarId={System.Uri.EscapeDataString(avatarDetails.avatarId)}";
                            var response = client.DownloadString(url);
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
            }
        }

        private void btnParseImages_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(configSave.Config.AuthKey))
            {
                MessageBox.Show("Requires VRChat login details");
                return;
            }
            ThreadPool.QueueUserWorkItem(delegate
            {
                for (int i = 0; i < avatarGrid.Rows.Count; i++)
                {
                    try
                    {
                        if (avatarGrid.Rows[i] != null)
                        {
                            if (avatarGrid.Rows[i].Cells[3].Value != null)
                            {
                                VRChatCacheResult vRChatCacheResult = GetDetails(avatarGrid.Rows[i].Cells[3].Value.ToString());
                                if (vRChatCacheResult != null)
                                {
                                    avatarGrid.Rows[i].Cells[1].Value = vRChatCacheResult.name;
                                    avatarGrid.Rows[i].Cells[2].Value = vRChatCacheResult.authorName;
                                    avatarGrid.Rows[i].Cells[5].Value = vRChatCacheResult.imageUrl;
                                    try
                                    {
                                        UploadCacheResult(vRChatCacheResult);
                                    }
                                    catch (Exception ex) { Console.WriteLine(ex.ToString()); }
                                    if (avatarGrid.Rows[i].Cells[5].Value != null)
                                    {
                                        if (!string.IsNullOrEmpty(avatarGrid.Rows[i].Cells[5].Value.ToString().Trim()))
                                        {
                                            try
                                            {
                                                HttpWebRequest myRequest = (HttpWebRequest)WebRequest.Create(avatarGrid.Rows[i].Cells[5].Value.ToString());
                                                myRequest.Method = "GET";
                                                myRequest.UserAgent = userAgent;
                                                using (HttpWebResponse myResponse = (HttpWebResponse)myRequest.GetResponse())
                                                {
                                                    if (myResponse.StatusCode == HttpStatusCode.OK)
                                                    {
                                                        Bitmap bmp = new Bitmap(myResponse.GetResponseStream());
                                                        avatarGrid.Rows[i].Cells[0].Value = bmp;
                                                    }
                                                    else
                                                    {
                                                        Bitmap bmp = new Bitmap(Resources.No_Image);
                                                        avatarGrid.Rows[i].Cells[0].Value = bmp;
                                                    }
                                                }
                                            }
                                            catch (Exception ex)
                                            {
                                                Console.WriteLine(ex.ToString());
                                                try
                                                {
                                                    Bitmap bmp = new Bitmap(Resources.No_Image);
                                                    avatarGrid.Rows[i].Cells[0].Value = bmp;
                                                }
                                                catch (Exception exc) { Console.WriteLine(exc.Message); }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch { }
                }
                MessageBox.Show("Finished Getting avatar information");
            });
        }
    }
}