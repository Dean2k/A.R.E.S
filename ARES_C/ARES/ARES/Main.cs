using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using ARES.Language;
using ARES.Models;
using ARES.Modules;
using ARES.Properties;
using MetroFramework;
using MetroFramework.Forms;
using Microsoft.Win32;
using Newtonsoft.Json;
using YamlDotNet.Core.Events;
using OpenFileDialog = System.Windows.Forms.OpenFileDialog;
using SaveFileDialog = System.Windows.Forms.SaveFileDialog;

namespace ARES
{
    public partial class Main : MetroForm
    {
        private readonly string _fileLocation =
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location).Replace(@"\GUI", @"\UserData") +
            @"\ARESConfig.json";

        private bool _apiEnabled;
        private volatile int _avatarCount;
        private List<Records> _avatarList;
        private volatile int _currentThreads;
        private List<string> _favoriteList;
        private Thread _imageThread;
        private bool _loading = true;
        private List<Records> _localAvatars;
        private List<WorldClass> _localWorlds;

        private List<string> _rippedList;
        private Thread _scanThread;
        private Records _selectedAvatar;
        private Thread _uploadThread;
        private Thread _vrcaThread;
        private int _worldCount;
        private List<WorldClass> _worldList;
        public Api ApiGrab;

        private AresConfig config;
        public CoreFunctions CoreFunctions;
        public GenerateHtml GenerateHtml;
        public HotswapConsole HotSwapConsole;
        public IniFile IniFile;
        public bool IsAvatar;
        public int LineSkip;
        public bool LoadImages;
        public bool Locked;
        public int MaxThreads = 12;
        public int ModCount;
        public int ModCountNumber;

        public int PluginCount;
        public int PluginCountNumber;
        public WorldClass SelectedWorld;
        public string UnityPath;
        public string Version = "";

        public Main()
        {
            InitializeComponent();
            StyleManager = metroStyleManager;
        }

        private void CleanHsb()
        {
            var programLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            KillProcess("Unity Hub.exe");
            KillProcess("Unity.exe");
            tryDeleteDirectory(programLocation + "/ARES");
            tryDeleteDirectory(@"C:\Users\" + Environment.UserName + @"\AppData\Local\Temp\DefaultCompany\ARES");
            tryDeleteDirectory(@"C:\Users\" + Environment.UserName + @"\AppData\LocalLow\DefaultCompany\ARES");
        }

        private void CheckIniKeys()
        {
            if (!IniFile.KeyExists("AresVersion"))
            {
                MessageBox.Show("ARES Version not detected in INI file HSB cleaning will now begin");
                CleanHsb();
                IniFile.Write("AresVersion", Version);
            }

            if (IniFile.KeyExists("avatarOutput")) txtAvatarOutput.Text = IniFile.Read("avatarOutput");

            if (IniFile.KeyExists("worldOutput")) txtWorldOutput.Text = IniFile.Read("worldOutput");

            if (IniFile.KeyExists("avatarOutputAuto"))
                toggleAvatar.Checked = Convert.ToBoolean(IniFile.Read("avatarOutputAuto"));

            if (IniFile.KeyExists("worldOutputAuto"))
                toggleWorld.Checked = Convert.ToBoolean(IniFile.Read("worldOutputAuto"));

            if (!IniFile.KeyExists("apiEnabled"))
            {
                var dlgResult = MessageBox.Show("Enable API support?", "API", MessageBoxButtons.YesNo,
                    MessageBoxIcon.Information);

                if (dlgResult == DialogResult.No)
                {
                    _apiEnabled = false;
                    IniFile.Write("apiEnabled", "false");
                }
                else if (dlgResult == DialogResult.Yes)
                {
                    _apiEnabled = true;
                    IniFile.Write("apiEnabled", "true");
                }
                else
                {
                    _apiEnabled = true;
                    IniFile.Write("apiEnabled", "true");
                }
            }
            else
            {
                _apiEnabled = Convert.ToBoolean(IniFile.Read("apiEnabled"));
            }

            if (IniFile.KeyExists("apiKey"))
            {
                txtApiKey.Text = IniFile.Read("apiKey");
                ApiGrab.ApiKey = IniFile.Read("apiKey");
            }

            if (!IniFile.KeyExists("unity"))
            {
                UnitySetup();
            }
            else
            {
                UnityPath = IniFile.Read("unity");
                if (!File.Exists(UnityPath)) UnitySetup();
            }

            if (IniFile.KeyExists("theme"))
                metroStyleManager.Theme =
                    IniFile.Read("theme") == "light" ? MetroThemeStyle.Light : MetroThemeStyle.Dark;

            if (IniFile.KeyExists("style")) LoadStyle(IniFile.Read("style"));
        }

        private void Main_Load(object sender, EventArgs e)
        {
            ApiGrab = new Api();
            CoreFunctions = new CoreFunctions();
            IniFile = new IniFile();
            GenerateHtml = new GenerateHtml();
            
            mTab.SelectedIndex = 0;
            mTabMain.Show();
            txtAbout.Text = Resources.txtAbout;
            dgCommentTable.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
            try
            {
                nmThread.Value = Environment.ProcessorCount;
            }
            catch
            {
            }

            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                var fileVersionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);
                Version = fileVersionInfo.ProductVersion;
                Text = "ARES V" + Version;
            }
            catch
            {
            }

            cbLimit.SelectedIndex = 0;
            var filePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (!Directory.Exists(filePath + @"\Logs")) Directory.CreateDirectory(filePath + @"\Logs");

            if (File.Exists(filePath + @"\LatestLog.txt"))
            {
                File.Move(filePath + @"\LatestLog.txt", string.Format(filePath + "\\Logs\\{0}.txt",
                    $"{DateTime.Now:yyyy-MM-dd_HH-mm-ss-fff}"));
                Thread.Sleep(500);
                var myFile = File.Create(filePath + @"\LatestLog.txt");
                myFile.Close();
            }
            else
            {
                var myFile = File.Create(filePath + @"\LatestLog.txt");
                myFile.Close();
            }

            if (!File.Exists(filePath + @"\Ripped.txt"))
            {
                _rippedList = new List<string>();
                var myFile = File.Create(filePath + @"\Ripped.txt");
                myFile.Close();
            }
            else
            {
                _rippedList = new List<string>();
                foreach (var line in File.ReadLines(filePath + @"\Ripped.txt")) _rippedList.Add(line);
            }

            if (!File.Exists(filePath + @"\Favorite.txt"))
            {
                _favoriteList = new List<string>();
                var myFile = File.Create(filePath + @"\Favorite.txt");
                myFile.Close();
            }
            else
            {
                _favoriteList = new List<string>();
                foreach (var line in File.ReadLines(filePath + @"\Favorite.txt")) _favoriteList.Add(line);
            }

            CheckIniKeys();

            try
            {
                Stats stats = ApiGrab.GetStats(Version);
                lblSize.Text = String.Format("{0:n0}", Convert.ToInt64(stats.Total_database_size));
                lblPublic.Text = String.Format("{0:n0}", Convert.ToInt64(stats.PublicAvatars));
                lblPrivate.Text = String.Format("{0:n0}", Convert.ToInt64(stats.PrivateAvatars));
            }
            catch
            {
                CoreFunctions.WriteLog("Error getting API stats.", this);
            }

            cbSearchTerm.SelectedIndex = 0;
            cbVersionUnity.SelectedIndex = 0;

            MessageBoxManager.Yes = "PC";
            MessageBoxManager.No = "Quest";
            MessageBoxManager.Register();

            _localAvatars = CoreFunctions.GetLocalAvatars(this);
            if (_localAvatars.Count > 0 && _apiEnabled)
            {
                _uploadThread = new Thread(() => CoreFunctions.UploadToApi(_localAvatars, this, Version));
                _uploadThread.Start();
            }

            _localWorlds = CoreFunctions.GetLocalWorlds(this);
            if (_localWorlds.Count > 0 && _apiEnabled) CoreFunctions.uploadToApiWorld(_localWorlds, this, Version);
            try
            {
                CoreFunctions.WriteLog("Fetching unity sources", this);
                _scanThread = new Thread(() => ScanPackage.DownloadOnlineSourcesOnStartup(this));
                _scanThread.Start();
            }
            catch
            {
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
                        IniFile.Write("unity", unityPath + @"\Editor\Unity.exe");
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
            var fileContent = string.Empty;
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

            UnityPath = filePath;
            IniFile.Write("unity", filePath);
        }

        private static string UnityRegistry()
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

        private string SelectFileVrca()
        {
            var fileContent = string.Empty;
            var filePath = string.Empty;

            using (var openFileDialog = new OpenFileDialog())
            {
                openFileDialog.InitialDirectory = "c:\\";
                openFileDialog.Filter = "vrc* files (*.vrc*)|*.vrc*";
                openFileDialog.RestoreDirectory = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                    //Get the path of specified file
                    filePath = openFileDialog.FileName;
            }

            return filePath;
        }

        private void btnSearch_Click(object sender, EventArgs e)
        {
            if (!Locked)
            {
                MaxThreads = Convert.ToInt32(nmThread.Value);
                LoadImages = chkLoadImages.Checked;
                flowAvatars.Controls.Clear();

                if (!cbSearchTerm.Text.Contains("World"))
                {
                    var avatars = ApiGrab.GetAvatars(txtSearchTerm.Text, cbSearchTerm.Text, cbLimit.Text, Version);
                    _avatarList = avatars;
                    if (chkPC.Checked)
                        _avatarList = _avatarList.Where(x => x.PCAssetURL?.Trim().ToLower() != "none").ToList();
                    if (chkQuest.Checked)
                        _avatarList = _avatarList.Where(x => x.QUESTAssetURL?.Trim().ToLower() != "none").ToList();
                    if (chkPublic.Checked && chkPrivate.Checked == false)
                        _avatarList = _avatarList.Where(x => x.Releasestatus.ToLower().Trim() == "public").ToList();
                    if (chkPublic.Checked == false && chkPrivate.Checked)
                        _avatarList = _avatarList.Where(x => x.Releasestatus.ToLower().Trim() == "private").ToList();
                    if (chkPin.Checked == true)
                        _avatarList = _avatarList.Where(x => x.Pin.ToLower().Trim() == "true").ToList();
                    _avatarCount = _avatarList.Count();
                    lblAvatarCount.Text = _avatarCount.ToString();
                    Locked = true;
                    IsAvatar = true;
                    _imageThread = new Thread(GetImages);
                    _imageThread.Start();
                }
                else
                {
                    var worlds = ApiGrab.GetWorlds(txtSearchTerm.Text, cbSearchTerm.Text, Version);
                    _worldList = worlds;
                    _worldCount = worlds.Count();
                    lblAvatarCount.Text = _worldCount.ToString();
                    Locked = true;
                    IsAvatar = false;
                    _imageThread = new Thread(GetImagesWorld);
                    _imageThread.Start();
                }
            }
            else
            {
                MetroMessageBox.Show(this, "Still loading last search", "ERROR", MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        public void GetImages()
        {
            try
            {
                foreach (var item in _avatarList)
                {
                    while (_currentThreads >= MaxThreads) Thread.Sleep(50);
                    _currentThreads++;
                    var t = new Thread(() => MultiGetImages(item));
                    t.Start();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            Locked = false;
        }

        private void labelAvatar_MouseDown(object sender, MouseEventArgs e)
        {
            switch (e.Button)
            {
                case MouseButtons.Right:
                    {
                        LoadInfo(sender, e);
                    }
                    break;
            }
        }

        public void MultiGetImages(Records item)
        {
            try
            {
                var groupBox = new Panel { Size = new Size(150, 150), BackColor = Color.Transparent };
                var avatarImage = new PictureBox
                { SizeMode = PictureBoxSizeMode.StretchImage, Size = new Size(148, 146) };
                var ripped = new PictureBox { SizeMode = PictureBoxSizeMode.StretchImage, Size = new Size(148, 146) };
                var questPc = "";
                if (item.PCAssetURL?.Trim().ToLower() != "none" && item.PCAssetURL != null) { questPc += "{PC}"; } else { questPc = "No key mode"; }
                if (item.QUESTAssetURL?.Trim().ToLower() != "none" && item.QUESTAssetURL != null) questPc += "{Quest}";
                var label = new Label
                {
                    Text = "Avatar Name: " + item.AvatarName + " [" + questPc + "]",
                    BackColor = Color.Transparent,
                    ForeColor = Color.Red,
                    Size = new Size(148, 146)
                };
                Bitmap bitmap = null;
                if (LoadImages) bitmap = CoreFunctions.LoadImage(item.ThumbnailURL, chkNoImages.Checked);

                if (bitmap != null || !LoadImages)
                {
                    avatarImage.Image = bitmap;
                    label.Name = item.AvatarID;
                    label.DoubleClick += DoubleClickLoad;
                    label.Click += LoadInfo;
                    groupBox.Controls.Add(avatarImage);
                    if (_rippedList.Contains(item.AvatarID))
                    {
                        ripped.Image = pbRipped.Image;

                        groupBox.Controls.Add(ripped);
                        groupBox.Controls.Add(label);
                        ripped.Parent = avatarImage;
                        label.Parent = ripped;
                    }
                    else
                    {
                        groupBox.Controls.Add(label);
                        label.Parent = avatarImage;
                    }

                    label.MouseDown += labelAvatar_MouseDown;
                    var cm = new ContextMenu();
                    cm.MenuItems.Add("Hotswap", btnHotswap_Click);
                    cm.MenuItems.Add("Extract", btnExtractVRCA_Click);
                    cm.MenuItems.Add("Download", btnDownload_Click);
                    label.ContextMenu = cm;

                    if (flowAvatars.InvokeRequired)
                        flowAvatars.Invoke((MethodInvoker)delegate { flowAvatars.Controls.Add(groupBox); });
                }
                else
                {
                    _avatarCount--;
                    if (lblAvatarCount.InvokeRequired)
                        lblAvatarCount.Invoke((MethodInvoker)delegate
                       {
                           lblAvatarCount.Text = _avatarCount.ToString();
                       });
                }

                _currentThreads--;
            }
            catch
            {
                _currentThreads--;
            }
        }

        public void GetImagesWorld()
        {
            try
            {
                foreach (var item in _worldList)
                {
                    var groupBox = new Panel { Size = new Size(150, 150), BackColor = Color.Transparent };
                    var avatarImage = new PictureBox
                    { SizeMode = PictureBoxSizeMode.StretchImage, Size = new Size(148, 146) };
                    var label = new Label
                    {
                        Text = "World Name: " + item.WorldName,
                        BackColor = Color.Transparent,
                        ForeColor = Color.Red,
                        Size = new Size(148, 146)
                    };
                    Bitmap bitmap = null;
                    if (LoadImages) bitmap = CoreFunctions.LoadImage(item.ThumbnailURL, chkNoImages.Checked);

                    if (bitmap != null || !LoadImages)
                    {
                        avatarImage.Image = bitmap;
                        label.Name = item.WorldID;
                        //avatarImage.Click += LoadInfo;
                        label.Click += LoadInfoWorld;
                        groupBox.Controls.Add(avatarImage);
                        groupBox.Controls.Add(label);
                        label.Parent = avatarImage;
                        if (flowAvatars.InvokeRequired)
                            flowAvatars.Invoke((MethodInvoker)delegate { flowAvatars.Controls.Add(groupBox); });
                    }
                    else
                    {
                        _worldCount--;
                        if (lblAvatarCount.InvokeRequired)
                            lblAvatarCount.Invoke((MethodInvoker)delegate
                           {
                               lblAvatarCount.Text = _worldCount.ToString();
                           });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            Locked = false;
        }

        private void DoubleClickLoad(object sender, EventArgs e)
        {
            if(_selectedAvatar != null)
            {
                Clipboard.SetText(_selectedAvatar.AvatarID);
            }
        }

        private void LoadInfo(object sender, EventArgs e)
        {
            var img = (Label)sender;
            if (_selectedAvatar != null)
            {
                if(_selectedAvatar.AvatarID == img.Name)
                {
                    return;
                }
            }
            
            _selectedAvatar = _avatarList.Find(x => x.AvatarID == img.Name);
            txtAvatarInfo.Text = CoreFunctions.SetAvatarInfo(_selectedAvatar);

            var bitmap = CoreFunctions.LoadImage(_selectedAvatar.ImageURL, chkNoImages.Checked);
            LoadComments();

            if (bitmap != null) selectedImage.Image = bitmap;
            if (_selectedAvatar.PCAssetURL?.Trim() != "None" && _selectedAvatar.PCAssetURL != null)
            {
                try
                {
                    var version = _selectedAvatar.PCAssetURL?.Split('/');
                    var urlCheck =
                        _selectedAvatar.PCAssetURL.Replace(version[6] + "/" + version[7] + "/file", version[6]);
                    var versionList = ApiGrab.GetVersions(urlCheck);
                    nmPcVersion.Value = Convert.ToInt32(versionList.versions.LastOrDefault().version);
                }
                catch
                {
                    nmPcVersion.Value = 1;
                }
            }
            else
            {
                nmPcVersion.Value = 0;
            }

            if (_selectedAvatar.QUESTAssetURL?.Trim() != "None" && _selectedAvatar.QUESTAssetURL != null)
            {
                try
                {
                    var version = _selectedAvatar.QUESTAssetURL.Split('/');
                    var urlCheck =
                        _selectedAvatar.QUESTAssetURL.Replace(version[6] + "/" + version[7] + "/file", version[6]);
                    var versionList = ApiGrab.GetVersions(urlCheck);
                    nmQuestVersion.Value = Convert.ToInt32(versionList.versions.LastOrDefault().version);
                }
                catch
                {
                    nmQuestVersion.Value = 1;
                }
            }
            else
            {
                nmQuestVersion.Value = 0;
            }
        }

        private void LoadInfoWorld(object sender, EventArgs e)
        {
            var img = (Label)sender;
            SelectedWorld = _worldList.Find(x => x.WorldID == img.Name);
            txtAvatarInfo.Text = CoreFunctions.SetWorldInfo(SelectedWorld);

            Bitmap bitmap;
            bitmap = CoreFunctions.LoadImage(SelectedWorld.ImageURL, chkNoImages.Checked);

            if (bitmap != null) selectedImage.Image = bitmap;
            if (SelectedWorld.PCAssetURL?.Trim() != "None")
            {
                var version = SelectedWorld.PCAssetURL.Split('/');
                nmPcVersion.Value = Convert.ToInt32(version[7]);
            }
            else
            {
                nmPcVersion.Value = 0;
            }

            nmQuestVersion.Value = 0;
        }

        private void btnDownload_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(txtAvatarInfo.Text))
            {

                if (_selectedAvatar != null)
                    if (_selectedAvatar.PCAssetURL == null)
                    {
                        MessageBox.Show("You are operating in no key mode and can't be used to download or hotswap VRCA's");
                        return;
                    }
                if (txtAvatarInfo.Text.Contains("avtr_") && _selectedAvatar.AvatarID.Contains("avtr_"))
                {
                    var saveFile = new SaveFileDialog();
                    var fileName = "custom.vrca";
                    saveFile.Filter = "VRCA files (*.vrca)|*.vrca";
                    saveFile.FileName = fileName;

                    if (saveFile.ShowDialog() == DialogResult.OK) fileName = saveFile.FileName;
                    if (!DownloadVrca(fileName)) return;
                }

                if (SelectedWorld != null)
                    if (txtAvatarInfo.Text.Contains("wrld_") && SelectedWorld.WorldID.Contains("wrld_"))
                    {
                        var saveFile = new SaveFileDialog();
                        var fileName = "custom.VRCW";
                        saveFile.Filter = "VRCW files (*.VRCW)|*.VRCW";
                        saveFile.FileName = fileName;

                        if (saveFile.ShowDialog() == DialogResult.OK) fileName = saveFile.FileName;
                        if (!DownloadVrcw(fileName)) return;
                    }
            }
            else
            {
                MetroMessageBox.Show(this, "Please select an avatar or world first.", "ERROR", MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private bool DownloadVrcw(string fileName = "custom.vrcw")
        {
            var filePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (fileName == "custom.vrcw") fileName = filePath + @"\custom.vrcw";
            if (SelectedWorld.AuthorName != "VRCW")
            {
                var version = SelectedWorld.PCAssetURL.Split('/');
                version[7] = nmPcVersion.Value.ToString();
                DownloadFile(string.Join("/", version), fileName);
            }
            else
            {
                DownloadFile(SelectedWorld.PCAssetURL, fileName);
            }

            return true;
        }

        private bool DownloadVrca(string fileName = "custom.vrca")
        {
            var filePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (fileName == "custom.vrca") fileName = filePath + @"\custom.vrca";
            if (_selectedAvatar.AuthorName != "VRCA")
            {
                if (_selectedAvatar.PCAssetURL.ToLower() != "none" && _selectedAvatar.QUESTAssetURL.ToLower() != "none" && _selectedAvatar.PCAssetURL != null && _selectedAvatar.QUESTAssetURL != null)
                {
                    var dlgResult = MessageBox.Show("Select which version to download", "VRCA Select",
                        MessageBoxButtons.YesNo, MessageBoxIcon.Information);

                    if (dlgResult == DialogResult.No)
                    {
                        if (_selectedAvatar.QUESTAssetURL.ToLower() != "none")
                        {
                            try
                            {
                                var version = _selectedAvatar.QUESTAssetURL.Split('/');
                                version[7] = nmQuestVersion.Value.ToString();
                                DownloadFile(string.Join("/", version), fileName);
                            }
                            catch { DownloadFile(_selectedAvatar.QUESTAssetURL, fileName); }
                        }
                        else
                        {
                            MetroMessageBox.Show(this, "Quest version doesn't exist", "ERROR", MessageBoxButtons.OK,
                                MessageBoxIcon.Error);
                            return false;
                        }
                    }
                    else if (dlgResult == DialogResult.Yes)
                    {
                        if (_selectedAvatar.PCAssetURL.ToLower() != "none")
                        {
                            try
                            {
                                var version = _selectedAvatar.PCAssetURL.Split('/');
                                version[7] = nmPcVersion.Value.ToString();
                                DownloadFile(string.Join("/", version), fileName);
                            }
                            catch { DownloadFile(_selectedAvatar.PCAssetURL, fileName); }
                        }
                        else
                        {
                            MetroMessageBox.Show(this, "PC version doesn't exist", "ERROR", MessageBoxButtons.OK,
                                MessageBoxIcon.Error);
                            return false;
                        }
                    }
                    else
                    {
                        return false;
                    }
                }
                else if (_selectedAvatar.PCAssetURL.ToLower() != "none" && _selectedAvatar.PCAssetURL != null)
                {
                    try
                    {
                        var version = _selectedAvatar.PCAssetURL.Split('/');
                        version[7] = nmPcVersion.Value.ToString();
                        DownloadFile(string.Join("/", version), fileName);
                    }
                    catch { DownloadFile(_selectedAvatar.PCAssetURL, fileName); }
                }
                else if (_selectedAvatar.QUESTAssetURL.ToLower() != "none" && _selectedAvatar.QUESTAssetURL != null)
                {
                    try
                    {
                        var version = _selectedAvatar.QUESTAssetURL.Split('/');
                        version[7] = nmQuestVersion.Value.ToString();
                        DownloadFile(string.Join("/", version), fileName);
                    }
                    catch { DownloadFile(_selectedAvatar.QUESTAssetURL, fileName); }
                }
                else
                {
                    return false;
                }
            }
            else
            {
                DownloadFile(_selectedAvatar.PCAssetURL, fileName);
            }

            return true;
        }

        private void btnExtractVRCA_Click(object sender, EventArgs e)
        {
            if (_selectedAvatar != null && IsAvatar)
            {
                if (!DownloadVrca()) return;

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
                    var unityVersion = cbVersionUnity.Text + "DLLnew";
                    var filePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                    var avatarName = Encoding.ASCII.GetString(
                        Encoding.Convert(
                            Encoding.UTF8,
                            Encoding.GetEncoding(
                                Encoding.ASCII.EncodingName,
                                new EncoderReplacementFallback(string.Empty),
                                new DecoderExceptionFallback()
                            ),
                            Encoding.UTF8.GetBytes(_selectedAvatar.AvatarName)
                        )
                    );
                    avatarName += "-ARES";
                    var invalidFileNameChars = Path.GetInvalidFileNameChars();
                    var folderExtractLocation = folderDlg.SelectedPath + @"\" +
                                                new string(avatarName.Where(ch => !invalidFileNameChars.Contains(ch))
                                                    .ToArray());
                    if (!Directory.Exists(folderExtractLocation)) Directory.CreateDirectory(folderExtractLocation);
                    var commands =
                        string.Format(
                            "/C AssetRipper.exe \"{2}\" \"{3}\\AssetRipperConsole_win64\\{0}\" -o \"{1}\" -q ",
                            unityVersion, folderExtractLocation, filePath + @"\custom.vrca", filePath);

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

                    tryDeleteDirectory(folderExtractLocation + @"\AssetRipper\GameAssemblies");
                    tryDeleteDirectory(folderExtractLocation + @"\Assets\Scripts");
                    try
                    {
                        Directory.Move(folderExtractLocation + @"\Assets\Shader",
                            folderExtractLocation + @"\Assets\.Shader");
                    }
                    catch
                    {
                    }

                    tryDeleteDirectory(folderExtractLocation + @"\AuxiliaryFiles");
                    tryDeleteDirectory(folderExtractLocation + @"\ExportedProject\Assets\Scripts");
                    tryDeleteDirectory(folderExtractLocation + @"\ExportedProject\AssetRipper");
                    tryDeleteDirectory(folderExtractLocation + @"\ExportedProject\ProjectSettings");
                    try
                    {
                        Directory.Move(folderExtractLocation + @"\ExportedProject\Assets\Shader",
                            folderExtractLocation + @"\ExportedProject\Assets\.Shader");
                        Directory.Move(folderExtractLocation + @"\ExportedProject\Assets\MonoScript",
                            folderExtractLocation + @"\ExportedProject\Assets\.MonoScript");

                    }
                    catch
                    {
                    }

                    if (_selectedAvatar.AvatarID != "VRCA")
                    {
                        File.AppendAllText(filePath + @"\Ripped.txt", _selectedAvatar.AvatarID + "\n");
                        _rippedList.Add(_selectedAvatar.AvatarID);
                    }
                }
            }
            else if (SelectedWorld != null && !IsAvatar)
            {
                if (!DownloadVrcw()) return;

                var folderDlg = new FolderBrowserDialog
                {
                    ShowNewFolderButton = true
                };
                // Show the FolderBrowserDialog.
                var result = DialogResult.OK;
                if (!toggleWorld.Checked || txtWorldOutput.Text == "")
                    result = folderDlg.ShowDialog();
                else
                    folderDlg.SelectedPath = txtWorldOutput.Text;
                if (result == DialogResult.OK || toggleWorld.Checked && txtWorldOutput.Text != "")
                {
                    var unityVersion = cbVersionUnity.Text + "DLLnew";
                    var filePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                    var worldName = Encoding.ASCII.GetString(
                        Encoding.Convert(
                            Encoding.UTF8,
                            Encoding.GetEncoding(
                                Encoding.ASCII.EncodingName,
                                new EncoderReplacementFallback(string.Empty),
                                new DecoderExceptionFallback()
                            ),
                            Encoding.UTF8.GetBytes(SelectedWorld.WorldName)
                        )
                    );
                    worldName += "-ARES";
                    var invalidFileNameChars = Path.GetInvalidFileNameChars();
                    var folderExtractLocation = folderDlg.SelectedPath + @"\" +
                                                new string(worldName.Where(ch => !invalidFileNameChars.Contains(ch))
                                                    .ToArray());
                    if (!Directory.Exists(folderExtractLocation)) Directory.CreateDirectory(folderExtractLocation);
                    var commands =
                        string.Format(
                            "/C AssetRipper.exe \"{2}\" \"{3}\\AssetRipperConsole_win64\\{0}\" -o \"{1}\" -q ",
                            unityVersion, folderExtractLocation, filePath + @"\custom.vrcw", filePath);

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

                    tryDeleteDirectory(folderExtractLocation + @"\AssetRipper\GameAssemblies");
                    tryDeleteDirectory(folderExtractLocation + @"\Assets\Scripts");
                    try
                    {
                        Directory.Move(folderExtractLocation + @"\Assets\Shader",
                            folderExtractLocation + @"\Assets\.Shader");
                    }
                    catch
                    {
                    }
                }
            }
            else
            {
                MetroMessageBox.Show(this, "Please select an avatar or world first.", "ERROR", MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void DownloadFile(string url, string saveName)
        {
            using (var client = new WebClient())
            {
                try
                {
                    client.Headers.Add("user-agent",
                        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/99.0.4844.74 Safari/537.36");
                    client.DownloadFile(url, saveName);
                }
                catch (Exception ex)
                {
                    if (ex.Message == "(404) Not Found")
                        MetroMessageBox.Show(this, "Version doesn't exist or file has been deleted from VRChat servers",
                            "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void btnStopSearch_Click(object sender, EventArgs e)
        {
            if (_imageThread != null)
            {
                _imageThread.Abort();
                Locked = false;
            }
        }

        private void btnSearchLocal_Click(object sender, EventArgs e)
        {
            var localRecords = _localAvatars;
            if (!Locked)
            {
                LoadImages = chkLoadImages.Checked;
                flowAvatars.Controls.Clear();
                List<Records> avatars = null;

                if (chkPC.Checked)
                    localRecords = localRecords.Where(x => x.PCAssetURL.Trim().ToLower() != "none").ToList();
                if (chkQuest.Checked)
                    localRecords = localRecords.Where(x => x.QUESTAssetURL.Trim().ToLower() != "none").ToList();
                if (chkPublic.Checked && chkPrivate.Checked == false)
                    localRecords = localRecords.Where(x => x.Releasestatus.ToLower().Trim() == "public").ToList();
                if (chkPublic.Checked == false && chkPrivate.Checked)
                    localRecords = localRecords.Where(x => x.Releasestatus.ToLower().Trim() == "private").ToList();
                if (cbSearchTerm.Text == "Avatar Name" && txtSearchTerm.Text != "")
                    localRecords = localRecords.Where(x => x.AvatarName.Contains(txtSearchTerm.Text)).ToList();
                if (cbSearchTerm.Text == "Avatar ID" && txtSearchTerm.Text != "")
                    localRecords = localRecords.Where(x =>
                            string.Equals(x.AvatarID, txtSearchTerm.Text, StringComparison.CurrentCultureIgnoreCase))
                        .ToList();
                if (cbSearchTerm.Text == "Author Name" && txtSearchTerm.Text != "")
                    localRecords = localRecords.Where(x => x.AuthorName.Contains(txtSearchTerm.Text)).ToList();
                if (cbSearchTerm.Text == "Author ID" && txtSearchTerm.Text != "")
                    localRecords = localRecords.Where(x =>
                            string.Equals(x.AuthorID, txtSearchTerm.Text, StringComparison.CurrentCultureIgnoreCase))
                        .ToList();
                avatars = localRecords;

                avatars = avatars.OrderByDescending(x => x.TimeDetected).ToList();

                if (cbLimit.Text != "Max") avatars = avatars.Take(Convert.ToInt32(cbLimit.Text)).ToList();

                _avatarList = avatars;
                _avatarCount = avatars.Count();
                lblAvatarCount.Text = _avatarCount.ToString();
                Locked = true;
                IsAvatar = true;
                _imageThread = new Thread(GetImages);
                _imageThread.Start();
            }
            else
            {
                MetroMessageBox.Show(this, "Still loading last search", "ERROR", MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void btnHotswap_Click(object sender, EventArgs e)
        {
            if (_vrcaThread != null)
                if (_vrcaThread.IsAlive)
                {
                    MetroMessageBox.Show(this, "Hotswap is still busy with previous request", "ERROR",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

            if (_selectedAvatar != null)
            {
                if (_selectedAvatar.PCAssetURL == null)
                {
                    MessageBox.Show("You are operating in no key mode and can't be used to download or hotswap VRCA's");
                    return;
                }
                if (!DownloadVrca()) return;
                HotSwapConsole = new HotswapConsole();
                HotSwapConsole.Show();
                _vrcaThread = new Thread(Hotswap);
                _vrcaThread.Start();
            }
            else
            {
                MetroMessageBox.Show(this, "Please select an avatar first.", "ERROR", MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void Hotswap()
        {
            var filePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var fileDecompressed = filePath + @"\decompressed.vrca";
            var fileDecompressed2 = filePath + @"\decompressed1.vrca";
            var fileDecompressedFinal = filePath + @"\finalDecompressed.vrca";
            var fileDummy = filePath + @"\dummy.vrca";
            var fileTarget = filePath + @"\target.vrca";
            var tempFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)
                .Replace("\\Roaming", "");
            var unityVrca = tempFolder + "\\Local\\Temp\\DefaultCompany\\ARES\\custom.vrca";
            var regexId = @"avtr_[\w]{8}-[\w]{4}-[\w]{4}-[\w]{4}-[\w]{12}";
            var regexPrefabId = @"prefab-id-v1_avtr_[\w]{8}-[\w]{4}-[\w]{4}-[\w]{4}-[\w]{12}_[\d]{10}\.prefab";
            var regexCab = @"CAB-[\w]{32}";
            var regexUnity = @"20[\d]{2}\.[\d]\.[\d]{2}f[\d]";
            var avatarIdRegex = new Regex(regexId);
            var avatarPrefabIdRegex = new Regex(regexPrefabId);
            var avatarCabRegex = new Regex(regexCab);
            var unityRegex = new Regex(regexUnity);

            tryDelete(fileDecompressed);
            tryDelete(fileDecompressed2);
            tryDelete(fileDecompressedFinal);
            tryDelete(fileDummy);
            tryDelete(fileTarget);

            try
            {
                File.Copy(unityVrca, fileDummy);
            }
            catch
            {
                MessageBox.Show("Make sure you've started the build and publish on unity", "ERROR",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                if (HotSwapConsole.InvokeRequired)
                    HotSwapConsole.Invoke((MethodInvoker)delegate { HotSwapConsole.Close(); });
                return;
            }

            try
            {
                HotSwap.DecompressToFileStr(fileDummy, fileDecompressed, HotSwapConsole);
            }
            catch (Exception ex)
            {
                CoreFunctions.WriteLog(string.Format("{0}", ex.Message), this);
                MessageBox.Show("Error decompressing VRCA file", "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                if (HotSwapConsole.InvokeRequired)
                    HotSwapConsole.Invoke((MethodInvoker)delegate { HotSwapConsole.Close(); });
                return;
            }

            var matchModelNew = getMatches(fileDecompressed, avatarIdRegex, avatarCabRegex, unityRegex,
                avatarPrefabIdRegex);

            try
            {
                HotSwap.DecompressToFileStr(filePath + @"\custom.vrca", fileDecompressed2, HotSwapConsole);
            }
            catch (Exception ex)
            {
                CoreFunctions.WriteLog(string.Format("{0}", ex.Message), this);
                MessageBox.Show("Error decompressing VRCA file", "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                if (HotSwapConsole.InvokeRequired)
                    HotSwapConsole.Invoke((MethodInvoker)delegate { HotSwapConsole.Close(); });
                return;
            }

            var matchModelOld = getMatches(fileDecompressed2, avatarIdRegex, avatarCabRegex, unityRegex,
                avatarPrefabIdRegex);
            if (matchModelOld.UnityVersion == null)
            {
                var dialogResult = MessageBox.Show("Possible risky hotswap detected", "Risky Upload",
                    MessageBoxButtons.OKCancel, MessageBoxIcon.Error);
                if (dialogResult == DialogResult.Cancel)
                {
                    if (HotSwapConsole.InvokeRequired)
                        HotSwapConsole.Invoke((MethodInvoker)delegate { HotSwapConsole.Close(); });
                    return;
                }
            }

            if (matchModelOld.UnityVersion != null)
                if (matchModelOld.UnityVersion.Contains("2017.") || matchModelOld.UnityVersion.Contains("2018."))
                {
                    var dialogResult = MessageBox.Show(
                        "Replace 2017-2018 unity version, replacing this can cause issues but not replacing it can also increase a ban chance (Press OK to replace and cancel to skip replacements)",
                        "Possible 2017-2018 unity issue", MessageBoxButtons.OKCancel, MessageBoxIcon.Error);
                    if (dialogResult == DialogResult.Cancel) matchModelOld.UnityVersion = null;
                }

            GetReadyForCompress(fileDecompressed2, fileDecompressedFinal, matchModelOld, matchModelNew);

            try
            {
                HotSwap.CompressBundle(fileDecompressedFinal, fileTarget, HotSwapConsole);
            }
            catch (Exception ex)
            {
                CoreFunctions.WriteLog(string.Format("{0}", ex.Message), this);
                MessageBox.Show("Error compressing VRCA file");
                if (HotSwapConsole.InvokeRequired)
                    HotSwapConsole.Invoke((MethodInvoker)delegate { HotSwapConsole.Close(); });
                return;
            }

            try
            {
                File.Copy(fileTarget, unityVrca, true);
            }
            catch
            {
            }

            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = new FileInfo(fileTarget).Length;
            var order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }

            var compressedSize = $"{len:0.##} {sizes[order]}";

            len = new FileInfo(fileDecompressed2).Length;
            order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }

            var uncompressedSize = $"{len:0.##} {sizes[order]}";
            CoreFunctions.WriteLog("Successfully hotswapped avatar", this);
            if (_selectedAvatar != null)
            {
                if (_selectedAvatar.AvatarID == "VRCA")
                {
                    imageSave();
                }
                else
                {
                    try
                    {
                        selectedImage.Image.Save(
                            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) +
                            @"\ARES\Assets\ARES SMART\Resources\ARESLogoTex.png", ImageFormat.Png);
                    }
                    catch
                    {
                    }
                }
            }

            if (SelectedWorld != null)
            {
                if (SelectedWorld.WorldID == "VRCA")
                {
                    imageSave();
                }
                else
                {
                    try
                    {
                        selectedImage.Image.Save(
                            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) +
                            @"\ARES\Assets\ARES SMART\Resources\ARESLogoTex.png", ImageFormat.Png);
                    }
                    catch { }
                }
            }


            tryDelete(fileDecompressed);
            tryDelete(fileDecompressed2);
            tryDelete(fileDecompressedFinal);
            tryDelete(fileDummy);
            tryDelete(fileTarget);

            if (HotSwapConsole.InvokeRequired)
                HotSwapConsole.Invoke((MethodInvoker)delegate { HotSwapConsole.Close(); });

            MessageBox.Show($"Got file sizes, comp:{compressedSize}, decomp:{uncompressedSize}", "Info",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            ;
            File.AppendAllText(filePath + @"\Ripped.txt", matchModelOld.AvatarId + "\n");
            _rippedList.Add(matchModelOld.AvatarId);
        }

        private void imageSave()
        {
            using (var webClient = new WebClient())
            {
                var data = webClient.DownloadData(
                    "https://source.unsplash.com/random/1200x900?sig=incrementingIdentifier");
                using (var mem = new MemoryStream(data))
                {
                    using (var yourImage = Image.FromStream(mem))
                    {
                        yourImage.Save(
                            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) +
                            @"\ARES\Assets\ARES SMART\Resources\ARESLogoTex.png", ImageFormat.Png);
                    }
                }
            }
        }

        private MatchModel getMatches(string file, Regex avatarId, Regex avatarCab, Regex unityVersion,
            Regex avatarAssetId)
        {
            MatchCollection avatarIdMatch = null;
            MatchCollection avatarAssetIdMatch = null;
            MatchCollection avatarCabMatch = null;
            MatchCollection unityMatch = null;
            var unityCount = 0;

            foreach (var line in File.ReadLines(file))
            {
                var tempId = avatarId.Matches(line);
                var tempAssetId = avatarAssetId.Matches(line);
                var tempCab = avatarCab.Matches(line);
                var tempUnity = unityVersion.Matches(line);
                if (tempAssetId.Count > 0) avatarAssetIdMatch = tempAssetId;
                if (tempId.Count > 0) avatarIdMatch = tempId;
                if (tempCab.Count > 0) avatarCabMatch = tempCab;
                if (tempUnity.Count > 0)
                {
                    unityMatch = tempUnity;
                    unityCount++;
                }
            }

            if (avatarAssetIdMatch == null) avatarAssetIdMatch = avatarIdMatch;

            var matchModel = new MatchModel
            {
                AvatarId = avatarIdMatch[0].Value,
                AvatarCab = avatarCabMatch[0].Value,
                AvatarAssetId = avatarAssetIdMatch[0].Value
            };

            if (unityMatch != null) matchModel.UnityVersion = unityMatch[0].Value;
            return matchModel;
        }

        private void GetReadyForCompress(string oldFile, string newFile, MatchModel old, MatchModel newModel)
        {
            var enc = Encoding.GetEncoding(28591);
            using (var vReader = new StreamReaderOver(oldFile, enc))
            {
                using (var vWriter = new StreamWriter(newFile, false, enc))
                {
                    while (!vReader.EndOfStream)
                    {
                        var vLine = vReader.ReadLine();
                        var replace = CheckAndReplaceLine(vLine, old, newModel);
                        vWriter.Write(replace);
                    }
                }
            }
        }

        private string CheckAndReplaceLine(string line, MatchModel old, MatchModel newModel)
        {
            var edited = line;
            if (edited.Contains(old.AvatarAssetId)) edited = edited.Replace(old.AvatarAssetId, newModel.AvatarAssetId);
            if (edited.Contains(old.AvatarId)) edited = edited.Replace(old.AvatarId, newModel.AvatarId);
            if (edited.Contains(old.AvatarCab)) edited = edited.Replace(old.AvatarCab, newModel.AvatarCab);
            if (old.UnityVersion != null)
                if (edited.Contains(old.UnityVersion))
                    edited = edited.Replace(old.UnityVersion, newModel.UnityVersion);
            return edited;
        }

        private void HotswapRepair()
        {
            var filePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var fileDecompressed2 = filePath + @"\decompressed1.vrca";

            tryDelete(fileDecompressed2);

            try
            {
                HotSwap.DecompressToFileStr(filePath + @"\custom.vrca", fileDecompressed2, HotSwapConsole);
            }
            catch (Exception ex)
            {
                CoreFunctions.WriteLog($"{ex.Message}", this);
                MetroMessageBox.Show(this, "Error decompressing VRCA file", "ERROR", MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            var oldId = getFileString(fileDecompressed2, @"(avtr_[\w\d]{8}-[\w\d]{4}-[\w\d]{4}-[\w\d]{4}-[\w\d]{12})");
            var oldCab = getFileString(fileDecompressed2, @"(CAB-[\w\d]{32})");

            HotSwapConsole.Close();

            txtSearchTerm.Text = oldId;
            cbSearchTerm.SelectedIndex = 2;
            btnSearch.PerformClick();

            txtAvatarInfo.Text += Environment.NewLine + "Avatar Id from VRCA: " + oldId + Environment.NewLine +
                                  "CAB Id from VRCA: " + oldCab;
            CoreFunctions.WriteLog("Repaired VRCA file", this);
        }

        private string getFileString(string file, string searchRegexString)
        {
            string line;
            string lineReturn = null;

            var fileOpen =
                new StreamReader(file);

            while ((line = fileOpen.ReadLine()) != null)
            {
                lineReturn = Regex.Match(line, searchRegexString).Value;
                if (!string.IsNullOrEmpty(lineReturn)) break;
            }

            fileOpen.Close();

            return lineReturn;
        }

        private void tryDelete(string location)
        {
            try
            {
                if (File.Exists(location))
                {
                    File.Delete(location);
                    CoreFunctions.WriteLog($"Deleted file {location}", this);
                }
            }
            catch (Exception ex)
            {
                CoreFunctions.WriteLog($"{ex.Message}", this);
            }
        }

        private void tryDeleteDirectory(string location, bool showExceptions = true)
        {
            try
            {
                Directory.Delete(location, true);
                CoreFunctions.WriteLog($"Deleted file {location}", this);
            }
            catch (Exception ex)
            {
                if (showExceptions) CoreFunctions.WriteLog($"{ex.Message}", this);
            }
        }

        private void btnUnity_Click(object sender, EventArgs e)
        {
            var tempFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)
                .Replace("\\Roaming", "");
            var unityTemp = "\\Local\\Temp\\DefaultCompany\\ARES";
            var unityTemp2 = "\\LocalLow\\Temp\\DefaultCompany\\ARES";

            tryDeleteDirectory(tempFolder + unityTemp, false);
            tryDeleteDirectory(tempFolder + unityTemp2, false);

            CoreFunctions.ExtractHSB();
            CopyFiles();
            CoreFunctions.OpenUnity(UnityPath);

            btnHotswap.Enabled = true;
        }

        private void btnLoadVRCA_Click(object sender, EventArgs e)
        {
            selectedImage.ImageLocation = "https://github.com/Dean2k/A.R.E.S/releases/latest/download/ARESLogo.png";
            var file = SelectFileVrca();
            if (Path.GetExtension(file).ToLower() == ".vrca")
            {
                IsAvatar = true;
                _selectedAvatar = new Records
                {
                    AuthorID = "VRCA",
                    AuthorName = "VRCA",
                    AvatarDescription = "VRCA",
                    ImageURL = "VRCA",
                    ThumbnailURL = "VRCA",
                    PCAssetURL = file,
                    QUESTAssetURL = file,
                    Tags = "VRCA",
                    TimeDetected = ((DateTimeOffset)DateTime.Now).ToUnixTimeSeconds().ToString(),
                    UnityVersion = "VRCA",
                    AvatarID = "VRCA",
                    AvatarName = "VRCA",
                    Releasestatus = "VRCA"
                };
                txtAvatarInfo.Text = CoreFunctions.SetAvatarInfo(_selectedAvatar);
            }
            else
            {
                IsAvatar = false;
                SelectedWorld = new WorldClass
                {
                    AuthorID = "VRCW",
                    AuthorName = "VRCW",
                    WorldDescription = "VRCW",
                    ImageURL = "VRCW",
                    ThumbnailURL = "VRCW",
                    PCAssetURL = file,
                    Tags = "VRCW",
                    TimeDetected = ((DateTimeOffset)DateTime.Now).ToUnixTimeSeconds().ToString(),
                    UnityVersion = "VRCW",
                    WorldID = "VRCW",
                    WorldName = "VRCW",
                    Releasestatus = "VRCW"
                };
                txtAvatarInfo.Text = CoreFunctions.SetWorldInfo(SelectedWorld);
            }
        }

        private void btnRepair_Click(object sender, EventArgs e)
        {
            if (_vrcaThread != null)
                if (_vrcaThread.IsAlive)
                {
                    MetroMessageBox.Show(this, "Hotswap is still busy with previous request", "ERROR",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

            if (_selectedAvatar != null)
            {
                if (_selectedAvatar.AuthorName == "VRCA")
                {
                    if (!DownloadVrca()) return;
                    HotSwapConsole = new HotswapConsole();
                    HotSwapConsole.Show();
                    _vrcaThread = new Thread(HotswapRepair);
                    _vrcaThread.Start();
                }
                else
                {
                    MetroMessageBox.Show(this, "Please load a VRCA file first", "ERROR", MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }
            else
            {
                MetroMessageBox.Show(this, "Please select an avatar first.", "ERROR", MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void btnVrcaSearch_Click(object sender, EventArgs e)
        {
            if (_vrcaThread != null)
                if (_vrcaThread.IsAlive)
                {
                    MetroMessageBox.Show(this, "VRCA search (Hotswap) is still busy with previous request", "ERROR",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

            if (_selectedAvatar != null)
            {
                if (_selectedAvatar.AuthorName == "VRCA")
                {
                    if (!DownloadVrca()) return;
                    mTabMain.Show();
                    mTab.SelectedIndex = 0;
                    HotSwapConsole = new HotswapConsole();
                    HotSwapConsole.Show();
                    HotswapRepair();
                }
                else
                {
                    MetroMessageBox.Show(this, "Please load a VRCA file first", "ERROR", MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }
            else
            {
                MetroMessageBox.Show(this, "Please select an avatar first.", "ERROR", MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void btnBrowserView_Click(object sender, EventArgs e)
        {
            if (_avatarList != null)
            {
                GenerateHtml.GenerateHtmlPage(_avatarList);
                Process.Start("avatars.html");
            }
        }

        private void btnCopy_Click(object sender, EventArgs e)
        {
            if (IsAvatar)
            {
                if (cbCopy.Text == "Time Detected")
                {
                    Clipboard.SetText(_selectedAvatar.TimeDetected);
                    MetroMessageBox.Show(this, "information copied to clipboard.", "Copied", MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }

                if (cbCopy.Text == "Avatar ID")
                {
                    Clipboard.SetText(_selectedAvatar.AvatarID);
                    MetroMessageBox.Show(this, "information copied to clipboard.", "Copied", MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }

                if (cbCopy.Text == "Avatar Name")
                {
                    Clipboard.SetText(_selectedAvatar.AvatarName);
                    MetroMessageBox.Show(this, "information copied to clipboard.", "Copied", MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }

                if (cbCopy.Text == "Avatar Description")
                {
                    Clipboard.SetText(_selectedAvatar.AvatarDescription);
                    MetroMessageBox.Show(this, "information copied to clipboard.", "Copied", MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }

                if (cbCopy.Text == "Author ID")
                {
                    Clipboard.SetText(_selectedAvatar.AuthorID);
                    MetroMessageBox.Show(this, "information copied to clipboard.", "Copied", MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }

                if (cbCopy.Text == "Author Name")
                {
                    Clipboard.SetText(_selectedAvatar.AuthorName);
                    MetroMessageBox.Show(this, "information copied to clipboard.", "Copied", MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }

                if (cbCopy.Text == "PC Asset URL")
                {
                    Clipboard.SetText(_selectedAvatar.PCAssetURL);
                    MetroMessageBox.Show(this, "information copied to clipboard.", "Copied", MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }

                if (cbCopy.Text == "Quest Asset URL")
                {
                    Clipboard.SetText(_selectedAvatar.QUESTAssetURL);
                    MetroMessageBox.Show(this, "information copied to clipboard.", "Copied", MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }

                if (cbCopy.Text == "Image URL")
                {
                    Clipboard.SetText(_selectedAvatar.ImageURL);
                    MetroMessageBox.Show(this, "information copied to clipboard.", "Copied", MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }

                if (cbCopy.Text == "Thumbnail URL")
                {
                    Clipboard.SetText(_selectedAvatar.ThumbnailURL);
                    MetroMessageBox.Show(this, "information copied to clipboard.", "Copied", MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }

                if (cbCopy.Text == "Unity Version")
                {
                    Clipboard.SetText(_selectedAvatar.UnityVersion);
                    MetroMessageBox.Show(this, "information copied to clipboard.", "Copied", MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }

                if (cbCopy.Text == "Release Status")
                {
                    Clipboard.SetText(_selectedAvatar.Releasestatus);
                    MetroMessageBox.Show(this, "information copied to clipboard.", "Copied", MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }

                if (cbCopy.Text == "Tags")
                {
                    Clipboard.SetText(_selectedAvatar.Tags);
                    MetroMessageBox.Show(this, "information copied to clipboard.", "Copied", MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
            }
            else
            {
                MetroMessageBox.Show(this, "Only works for avatars atm.", "Copied", MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
        }

        private void btnApi_Click(object sender, EventArgs e)
        {
            if (_apiEnabled)
            {
                btnSearch.Enabled = false;
                _apiEnabled = false;
                btnApi.Text = "Enable API";
                IniFile.Write("apiEnabled", "false");
            }
            else if (!_apiEnabled)
            {
                btnSearch.Enabled = true;
                _apiEnabled = true;
                btnApi.Text = "Disable API";
                IniFile.Write("apiEnabled", "true");
            }
        }

        private void Ares_Close(object sender, FormClosedEventArgs e)
        {
            _imageThread?.Abort();
            _vrcaThread?.Abort();
            _uploadThread?.Abort();
            Thread.Sleep(2000);
        }

        private void btnScan_Click(object sender, EventArgs e)
        {
            if (_scanThread.IsAlive)
            {
                MetroMessageBox.Show(this, "Still downloading hashes of files good & bad, please try again later",
                    "Busy", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (Directory.Exists(ScanPackage.UnityTemp)) tryDeleteDirectory(ScanPackage.UnityTemp);

            Directory.CreateDirectory(ScanPackage.UnityTemp);

            var packageSelected = selectPackage();
            var outPath = "";

            if (!string.IsNullOrEmpty(packageSelected))
                outPath = PackageExtractor.ExtractPackage(packageSelected, ScanPackage.UnityTemp);

            var scanCount = ScanPackage.CheckFiles(this);

            if (scanCount.Item3 > 0)
            {
                MessageBox.Show("Bad files were detected please select a new location for cleaned UnityPackage");
                var fileLocation = createPackage();
                var blank = new string[0];
                var rootDir = "Assets/";
                var pack = Package.FromDirectory(outPath, fileLocation, true, blank, blank);
                pack.GeneratePackage(rootDir);
            }
            else
            {
                MessageBox.Show("No Bad files were detected");
            }

            MessageBox.Show(
                $"Bad files detected {scanCount.Item3}, Safe files detected {scanCount.Item1}, Unknown files detected {scanCount.Item2}");
        }

        private string selectPackage()
        {
            var fileContent = string.Empty;
            var filePath = string.Empty;

            using (var openFileDialog = new OpenFileDialog())
            {
                openFileDialog.InitialDirectory = "c:\\";
                openFileDialog.Filter = ".unitypackage files (*.unitypackage)|*.unitypackage";
                openFileDialog.RestoreDirectory = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    //Get the path of specified file
                    filePath = openFileDialog.FileName;

                    //Read the contents of the file into a stream
                    var fileStream = openFileDialog.OpenFile();

                    using (var reader = new StreamReader(fileStream))
                    {
                        fileContent = reader.ReadToEnd();
                    }
                }
            }

            return filePath;
        }

        private string createPackage()
        {
            using (var sfd = new SaveFileDialog())
            {
                sfd.Filter = ".unitypackage files (*.unitypackage)|*.unitypackage";
                sfd.FilterIndex = 2;

                if (sfd.ShowDialog() == DialogResult.OK) return sfd.FileName;
            }

            return null;
        }

        private void btnHsbClean_Click(object sender, EventArgs e)
        {
            CleanHsb();
        }

        private void KillProcess(string processName)
        {
            try
            {
                Process.Start("taskkill", "/F /IM \"" + processName + "\"");
                Console.WriteLine("Killed Process: " + processName);
                CoreFunctions.WriteLog(string.Format("Killed Process", processName), this);
            }
            catch
            {
            }
        }

        private void btnRipped_Click(object sender, EventArgs e)
        {
            if (!Locked)
            {
                MaxThreads = Convert.ToInt32(nmThread.Value);
                LoadImages = chkLoadImages.Checked;
                flowAvatars.Controls.Clear();
                var avatars = ApiGrab.GetRipped(_rippedList, Version);
                _avatarList = avatars;
                if (chkPC.Checked)
                    _avatarList = _avatarList.Where(x => x.PCAssetURL?.Trim().ToLower() != "none").ToList();
                if (chkQuest.Checked)
                    _avatarList = _avatarList.Where(x => x.QUESTAssetURL?.Trim().ToLower() != "none").ToList();
                if (chkPublic.Checked && chkPrivate.Checked == false)
                    _avatarList = _avatarList.Where(x => x.Releasestatus.ToLower().Trim() == "public").ToList();
                if (chkPublic.Checked == false && chkPrivate.Checked)
                    _avatarList = _avatarList.Where(x => x.Releasestatus.ToLower().Trim() == "private").ToList();
                _avatarCount = _avatarList.Count();
                lblAvatarCount.Text = _avatarCount.ToString();
                Locked = true;
                IsAvatar = true;
                _imageThread = new Thread(GetImages);
                _imageThread.Start();
            }
            else
            {
                MessageBox.Show("Still loading last search");
            }
        }

        private void btnUnityLoc_Click(object sender, EventArgs e)
        {
            SelectFile();
        }

        private void mTabSettings_Click(object sender, EventArgs e)
        {
        }

        private void mTab_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (mTab.SelectedIndex == 1)
            {
                LoadConfig();
                if (config != null)
                    SetCheckBoxes();
                else
                    ConfigBox.Visible = false;
                _loading = false;
            }
        }

        private void LoadConfig()
        {
            try
            {
                var json = File.ReadAllText(_fileLocation);
                config = JsonConvert.DeserializeObject<AresConfig>(json);
            }
            catch
            {
            }
        }

        private void SetCheckBoxes()
        {
            cbConsoleError.Checked = config.ConsoleError;
            cbStealth.Checked = config.Stealth;
            cbHWIDSpoof.Checked = config.HWIDSpoof;
            cbLogAvatars.Checked = config.LogAvatars;
            cbLogFriendsAvatars.Checked = config.LogFriendsAvatars;
            cbLogOwnAvatars.Checked = config.LogOwnAvatars;
            cbLogPrivateAvatars.Checked = config.LogPrivateAvatars;
            cbLogPublicAvatars.Checked = config.LogPublicAvatars;
            cbLogToConsole.Checked = config.LogToConsole;
            cbLogWorlds.Checked = config.LogWorlds;
            cbCustomNameplates.Checked = config.CustomNameplates;
            cbAutoUpdate.Checked = config.AutoUpdate;
        }

        private void WriteConfig()
        {
            if (!_loading)
            {
                var json = JsonConvert.SerializeObject(config);
                File.WriteAllText(_fileLocation, json);
            }
        }

        private void cbUnlimitedFavorites_CheckedChanged(object sender, EventArgs e)
        {
            config.CustomNameplates = cbCustomNameplates.Checked;
            WriteConfig();
        }

        private void cbStealth_CheckedChanged(object sender, EventArgs e)
        {
            config.Stealth = cbStealth.Checked;
            WriteConfig();
        }

        private void cbLogAvatars_CheckedChanged(object sender, EventArgs e)
        {
            config.LogAvatars = cbLogAvatars.Checked;
            WriteConfig();
        }

        private void cbLogWorlds_CheckedChanged(object sender, EventArgs e)
        {
            config.LogWorlds = cbLogWorlds.Checked;
            WriteConfig();
        }

        private void cbLogFriendsAvatars_CheckedChanged(object sender, EventArgs e)
        {
            config.LogFriendsAvatars = cbLogFriendsAvatars.Checked;
            WriteConfig();
        }

        private void cbLogOwnAvatars_CheckedChanged(object sender, EventArgs e)
        {
            config.LogOwnAvatars = cbLogOwnAvatars.Checked;
            WriteConfig();
        }

        private void cbLogPublicAvatars_CheckedChanged(object sender, EventArgs e)
        {
            config.LogPublicAvatars = cbLogPublicAvatars.Checked;
            WriteConfig();
        }

        private void cbLogPrivateAvatars_CheckedChanged(object sender, EventArgs e)
        {
            config.LogPrivateAvatars = cbLogPrivateAvatars.Checked;
            WriteConfig();
        }

        private void cbLogToConsole_CheckedChanged(object sender, EventArgs e)
        {
            config.LogToConsole = cbLogToConsole.Checked;
            WriteConfig();
        }

        private void cbConsoleError_CheckedChanged(object sender, EventArgs e)
        {
            config.ConsoleError = cbConsoleError.Checked;
            WriteConfig();
        }

        private void cbHWIDSpoof_CheckedChanged(object sender, EventArgs e)
        {
            config.HWIDSpoof = cbHWIDSpoof.Checked;
            WriteConfig();
        }

        private void btnLight_Click(object sender, EventArgs e)
        {
            metroStyleManager.Theme = MetroThemeStyle.Light;
            IniFile.Write("theme", "light");
        }

        private void btnDark_Click(object sender, EventArgs e)
        {
            metroStyleManager.Theme = MetroThemeStyle.Dark;
            IniFile.Write("theme", "dark");
        }

        private void cbThemeColour_SelectedIndexChanged(object sender, EventArgs e)
        {
            LoadStyle(cbThemeColour.Text);
        }

        private void LoadStyle(string style)
        {
            switch (style)
            {
                case "Black":
                    metroStyleManager.Style = MetroColorStyle.Black;
                    IniFile.Write("style", style);
                    break;

                case "White":
                    metroStyleManager.Style = MetroColorStyle.White;
                    IniFile.Write("style", style);
                    break;

                case "Silver":
                    metroStyleManager.Style = MetroColorStyle.Silver;
                    IniFile.Write("style", style);
                    break;

                case "Green":
                    metroStyleManager.Style = MetroColorStyle.Green;
                    IniFile.Write("style", style);
                    break;

                case "Blue":
                    metroStyleManager.Style = MetroColorStyle.Blue;
                    IniFile.Write("style", style);
                    break;

                case "Lime":
                    metroStyleManager.Style = MetroColorStyle.Lime;
                    IniFile.Write("style", style);
                    break;

                case "Teal":
                    metroStyleManager.Style = MetroColorStyle.Teal;
                    IniFile.Write("style", style);
                    break;

                case "Orange":
                    metroStyleManager.Style = MetroColorStyle.Orange;
                    IniFile.Write("style", style);
                    break;

                case "Brown":
                    metroStyleManager.Style = MetroColorStyle.Brown;
                    IniFile.Write("style", style);
                    break;

                case "Pink":
                    metroStyleManager.Style = MetroColorStyle.Pink;
                    IniFile.Write("style", style);
                    break;

                case "Magenta":
                    metroStyleManager.Style = MetroColorStyle.Magenta;
                    IniFile.Write("style", style);
                    break;

                case "Purple":
                    metroStyleManager.Style = MetroColorStyle.Purple;
                    IniFile.Write("style", style);
                    break;

                case "Red":
                    metroStyleManager.Style = MetroColorStyle.Red;
                    IniFile.Write("style", style);
                    break;

                case "Yellow":
                    metroStyleManager.Style = MetroColorStyle.Yellow;
                    IniFile.Write("style", style);
                    break;

                default:
                    metroStyleManager.Style = MetroColorStyle.Default;
                    IniFile.Write("style", "Default");
                    break;
            }
        }

        private void btnClearLogs_Click(object sender, EventArgs e)
        {
            tryDelete(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\LatestLog.txt");
            tryDeleteDirectory(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\Logs");
        }

        private void btnClearPluginLogs_Click(object sender, EventArgs e)
        {
            tryDelete(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\Log.txt");
            tryDelete(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\LogWorld.txt");
            tryDelete(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\WorldUploaded.txt");
            tryDelete(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\AvatarUploaded.txt");
        }

        private void btnAvatarOut_Click(object sender, EventArgs e)
        {
            var folderDlg = new FolderBrowserDialog
            {
                ShowNewFolderButton = true
            };
            var result = folderDlg.ShowDialog();
            if (result == DialogResult.OK)
            {
                txtAvatarOutput.Text = folderDlg.SelectedPath;
                IniFile.Write("avatarOutput", folderDlg.SelectedPath);
            }
        }

        private void btnWorldOut_Click(object sender, EventArgs e)
        {
            var folderDlg = new FolderBrowserDialog
            {
                ShowNewFolderButton = true
            };
            var result = folderDlg.ShowDialog();
            if (result == DialogResult.OK)
            {
                txtWorldOutput.Text = folderDlg.SelectedPath;
                IniFile.Write("worldOutput", folderDlg.SelectedPath);
            }
        }

        private void toggleAvatar_CheckedChanged(object sender, EventArgs e)
        {
            IniFile.Write("avatarOutputAuto", toggleAvatar.Checked.ToString());
        }

        private void toggleWorld_CheckedChanged(object sender, EventArgs e)
        {
            IniFile.Write("worldOutputAuto", toggleWorld.Checked.ToString());
        }

        private void btnCleanLog_Click(object sender, EventArgs e)
        {
            PluginCount = 0;
            ModCount = 0;
            PluginCountNumber = 0;
            ModCountNumber = 0;
            var melonLog = MelonLogLocation();
            var newMelonLog = SaveLogLocation();
            if (melonLog != null && newMelonLog != null)
            {
                melonLogClean(melonLog, newMelonLog);
                finalCleanup(newMelonLog);
                MetroMessageBox.Show(this, "Log file has been cleaned", "Log Cleaned", MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
        }

        private void finalCleanup(string logLocation)
        {
            var newPlugin = PluginCountNumber - PluginCount;
            var newMod = ModCountNumber - ModCount;
            var text = File.ReadAllText(logLocation);
            text = text.Replace(PluginCountNumber + " Plugin Loaded", newPlugin + " Plugin Loaded");
            text = text.Replace(ModCountNumber + " Mods Loaded", newMod + " Mods Loaded");
            File.WriteAllText(logLocation, text);
        }

        private void melonLogClean(string oldFile, string cleanFile)
        {
            var enc = Encoding.UTF8;
            using (var vReader = new StreamReader(oldFile, enc))
            {
                using (var vWriter = new StreamWriter(cleanFile, false, enc))
                {
                    while (!vReader.EndOfStream)
                    {
                        var vLine = vReader.ReadLine();
                        var replace = CheckAndReplaceLine(vLine);
                        if (replace != null && LineSkip == 0) vWriter.WriteLine(replace);
                        if (LineSkip > 0) LineSkip--;
                    }
                }
            }
        }

        private string CheckAndReplaceLine(string line)
        {
            if (line.Contains("Plugin Loaded"))
                try
                {
                    var resultString = Regex.Match(line, @"\d+ Plugin Loaded").Value;
                    PluginCountNumber = Convert.ToInt32(Regex.Match(resultString, @"\d+").Value);
                }
                catch
                {
                }

            if (line.Contains("Mods Loaded"))
                try
                {
                    var resultString = Regex.Match(line, @"\d+ Mods Loaded").Value;
                    ModCountNumber = Convert.ToInt32(Regex.Match(resultString, @"\d+").Value);
                }
                catch
                {
                }

            if (line.Contains("ARES Manager"))
            {
                PluginCount++;
                LineSkip = 4;
                return null;
            }

            if (line.Contains("A.R.E.S Logger v"))
            {
                ModCount++;
                LineSkip = 4;
                return null;
            }

            if (line.Contains("ReModCE_ARES v"))
            {
                ModCount++;
                LineSkip = 4;
                return null;
            }

            if (line.Contains("A.R.E.S")) return null;
            if (line.Contains("ARES")) return null;
            return line;
        }

        private string SaveLogLocation()
        {
            var fileName = "MelonLog.log";
            var saveFile = new SaveFileDialog();
            saveFile.Filter = "log files (*.log)|*.log";
            saveFile.Title = "Select new cleaned melonlog location";
            saveFile.FileName = fileName;

            if (saveFile.ShowDialog() == DialogResult.OK)
            {
                fileName = saveFile.FileName;
                return fileName;
            }

            return null;
        }

        private string MelonLogLocation()
        {
            var fileContent = string.Empty;
            var filePath = string.Empty;

            using (var openFileDialog = new OpenFileDialog())
            {
                openFileDialog.InitialDirectory = Assembly.GetExecutingAssembly().Location;
                openFileDialog.Filter = "Melon Log (*.log)|*.log";
                openFileDialog.RestoreDirectory = true;
                openFileDialog.Title = "Select Melon loader log";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    //Get the path of specified file
                    filePath = openFileDialog.FileName;
                    return filePath;
                }
            }

            return null;
        }

        private void txtApiKey_Leave(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(txtApiKey.Text))
            {
                IniFile.Write("apiKey", txtApiKey.Text);
                ApiGrab.ApiKey = txtApiKey.Text;
            }
            else
            {
                ApiGrab.ApiKey = null;
                IniFile.DeleteKey("apiKey");
            }
        }

        private void txtApiKey_Click(object sender, EventArgs e)
        {
        }

        private void btnToggleFavorite_Click(object sender, EventArgs e)
        {
            if (_selectedAvatar == null) return;
            var filePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var fileContents = File.ReadAllText(filePath + @"\Favorite.txt");
            if (fileContents.Contains(_selectedAvatar.AvatarID))
            {
                fileContents = fileContents.Replace(_selectedAvatar.AvatarID, "");
                File.WriteAllText(filePath + @"\Favorite.txt", fileContents);
                _favoriteList.Remove(_selectedAvatar.AvatarID);
                MessageBox.Show("Removed From Favorites");
            }
            else
            {
                File.AppendAllText(filePath + @"\Favorite.txt", _selectedAvatar.AvatarID + Environment.NewLine);
                _favoriteList.Add(_selectedAvatar.AvatarID);
                MessageBox.Show("Added To Favorites");
            }
        }

        private void btnSearchFavorites_Click(object sender, EventArgs e)
        {
            if (!Locked)
            {
                MaxThreads = Convert.ToInt32(nmThread.Value);
                LoadImages = chkLoadImages.Checked;
                flowAvatars.Controls.Clear();
                var avatars = ApiGrab.GetRipped(_favoriteList, Version);
                _avatarList = avatars;
                if (chkPC.Checked)
                    _avatarList = _avatarList.Where(x => x.PCAssetURL?.Trim().ToLower() != "none").ToList();
                if (chkQuest.Checked)
                    _avatarList = _avatarList.Where(x => x.QUESTAssetURL?.Trim().ToLower() != "none").ToList();
                switch (chkPublic.Checked)
                {
                    case true when !chkPrivate.Checked:
                        _avatarList = _avatarList.Where(x => x.Releasestatus.ToLower().Trim() == "public").ToList();
                        break;
                    case false when chkPrivate.Checked:
                        _avatarList = _avatarList.Where(x => x.Releasestatus.ToLower().Trim() == "private").ToList();
                        break;
                }

                _avatarCount = _avatarList.Count();
                lblAvatarCount.Text = _avatarCount.ToString();
                Locked = true;
                IsAvatar = true;
                _imageThread = new Thread(GetImages);
                _imageThread.Start();
            }
            else
            {
                MessageBox.Show("Still loading last search");
            }
        }

        private void btnAddComment_Click(object sender, EventArgs e)
        {
            if (txtComment.Text.Length > 3)
            {
                Comments comment = new Comments
                {
                    AvatarId = _selectedAvatar.AvatarID,
                    Comment = txtComment.Text,
                    Created = ApiGrab.GetNistTime().ToString()
                };
                ApiGrab.AddComment(comment, Version);
                LoadComments();
            }
            else
            {
                MessageBox.Show("Please enter a comment over 3 characters");
            }
        }

        private void LoadComments()
        {
            List<Comments> comments = ApiGrab.GetComments(_selectedAvatar.AvatarID, Version);
            dgCommentTable.Rows.Clear();
            foreach (var comment in comments)
            {
                dgCommentTable.Rows.Add(comment.id, comment.Created, comment.Comment);
            }
        }

        private void btnResetScene_Click(object sender, EventArgs e)
        {
            CopyFiles();
        }

        public void CopyFiles()
        {
            try
            {
                var programLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                File.Copy(programLocation + @"\Template\SampleScene.unity", programLocation + @"\ARES\Assets\Scenes\SampleScene.unity", true);
                File.Copy(programLocation + @"\Template\ARESLogoTex.png", programLocation + @"\ARES\Assets\ARES SMART\Resources\ARESLogoTex.png", true);
            }
            catch { }
        }

        private void btnPreview_Click(object sender, EventArgs e)
        {
            string urlVRCA = null;
            if (_selectedAvatar.PCAssetURL == null)
            {
                MessageBox.Show("You are operating in no key mode and can't be used to download or hotswap VRCA's");
                return;
            }
            if (_selectedAvatar.PCAssetURL.ToLower() != "none")
            {
                try
                {
                    var version = _selectedAvatar.PCAssetURL.Split('/');
                    version[7] = nmPcVersion.Value.ToString();
                    urlVRCA = string.Join("/", version);
                    string commands = string.Format("\"-" + urlVRCA + "\"");

                    Process p = new Process();
                    ProcessStartInfo psi = new ProcessStartInfo
                    {
                        FileName = "AssetViewer.exe",
                        Arguments = commands,
                        WorkingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\AssetViewer\",
                        //WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden
                    };
                    p.StartInfo = psi;
                    p.Start();
                }
                catch {  }
            }
            else
            {
                MetroMessageBox.Show(this, "PC version doesn't exist", "ERROR", MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            
        }

        private void Main_Shown(object sender, EventArgs e)
        {
            Words.SetLanguage("English");
            Words.LoadTabNames(this);
            Words.LoadMainTabLang(this);
        }

        private void selectedImage_DoubleClick(object sender, EventArgs e)
        {
            if(_selectedAvatar != null)
            {
                System.Diagnostics.Process.Start(_selectedAvatar.ImageURL);
            }
        }
    }
}