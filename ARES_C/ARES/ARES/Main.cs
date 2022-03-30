using ARES.Models;
using ARES.Modules;
using Microsoft.Win32;
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
using MetroFramework.Forms;
using ARES.Properties;
using Newtonsoft.Json;
using MetroFramework;

namespace ARES
{
    public partial class Main : MetroForm
    {
        public Api ApiGrab;
        public CoreFunctions CoreFunctions;
        public IniFile iniFile;
        public GenerateHtml generateHtml;
        private List<Records> AvatarList;
        private List<WorldClass> worldList;
        private List<Records> localAvatars;
        private List<WorldClass> localWorlds;
        public bool locked;
        public Thread imageThread;
        public Thread vrcaThread;
        public Thread uploadThread;
        public Thread scanThread;
        public Thread browserThread;
        public int threadCount;
        public int maxThreads = 12;
        public volatile int currentThreads = 0;
        public volatile int avatarCount;
        public int worldCount;
        public Records selectedAvatar;
        public WorldClass selectedWorld;
        public string unityPath;
        public HotswapConsole hotswapConsole;
        public bool isAvatar;
        public bool apiEnabled;
        public bool loadImages;
        public List<string> rippedList;

        public Main()
        {
            InitializeComponent();
            this.StyleManager = metroStyleManager;
        }

        private void CleanHSB()
        {
            string programLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            killProcess("Unity Hub.exe");
            killProcess("Unity.exe");
            tryDelete(programLocation + "/HSBC.rar");
            tryDeleteDirectory(programLocation + "/HSB");
        }

        private void Main_Load(object sender, EventArgs e)
        {
            ApiGrab = new Api();
            CoreFunctions = new CoreFunctions();
            iniFile = new IniFile();
            generateHtml = new GenerateHtml();
            string version = "";

            //just incase i forgot
            mTab.SelectedIndex = 0;
            mTabMain.Show();
            txtAbout.Text = Resources.txtAbout;
            try
            {
                nmThread.Value = Environment.ProcessorCount;
            }
            catch { }
            try
            {
                Assembly assembly = Assembly.GetExecutingAssembly();
                FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);
                version = fileVersionInfo.ProductVersion;
                this.Text = "ARES V" + version;
            }
            catch { }
            cbLimit.SelectedIndex = 0;
            string filePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (!Directory.Exists(filePath + @"\Logs"))
            {
                Directory.CreateDirectory(filePath + @"\Logs");
            }

            if (File.Exists(filePath + @"\LatestLog.txt"))
            {
                File.Move(filePath + @"\LatestLog.txt", string.Format(filePath + "\\Logs\\{0}.txt", string.Format("{0:yyyy-MM-dd_HH-mm-ss-fff}", DateTime.Now)));
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
                var myFile = File.Create(filePath + @"\Ripped.txt");
                myFile.Close();
            }
            else
            {
                rippedList = new List<string>();
                foreach (string line in System.IO.File.ReadLines(filePath + @"\Ripped.txt"))
                {
                    rippedList.Add(line);
                }
            }

            if (!iniFile.KeyExists("AresVersion"))
            {
                MessageBox.Show("ARES Version not detected in INI file HSB cleaning will now begin");
                CleanHSB();
                iniFile.Write("AresVersion", version); 
            }

            if (iniFile.KeyExists("avatarOutput"))
            {
                txtAvatarOutput.Text = iniFile.Read("avatarOutput");
            }

            if (iniFile.KeyExists("worldOutput"))
            {
                txtWorldOutput.Text = iniFile.Read("worldOutput");
            }

            if (iniFile.KeyExists("avatarOutputAuto"))
            {
                toggleAvatar.Checked = Convert.ToBoolean(iniFile.Read("avatarOutputAuto"));
            }

            if (iniFile.KeyExists("worldOutputAuto"))
            {
                toggleWorld.Checked = Convert.ToBoolean(iniFile.Read("worldOutputAuto"));
            }

            if (!iniFile.KeyExists("apiEnabled"))
            {
                DialogResult dlgResult = MessageBox.Show("Enable API support?", "API", MessageBoxButtons.YesNo, MessageBoxIcon.Information);

                if (dlgResult == DialogResult.No)
                {
                    apiEnabled = false;
                    iniFile.Write("apiEnabled", "false");
                }
                else if (dlgResult == DialogResult.Yes)
                {
                    apiEnabled = true;
                    iniFile.Write("apiEnabled", "true");
                }
                else
                {
                    apiEnabled = true;
                    iniFile.Write("apiEnabled", "true");
                }
            }
            else
            {
                apiEnabled = Convert.ToBoolean(iniFile.Read("apiEnabled"));
            }

            try
            {
                lblSize.Text = ApiGrab.getStats().Total_database_size;
            }
            catch
            {
                CoreFunctions.WriteLog("Error getting API stats.", this);
            }
            cbSearchTerm.SelectedIndex = 0;
            cbVersionUnity.SelectedIndex = 0;

            if (!iniFile.KeyExists("unity"))
            {
                unitySetup();

            }
            else
            {
                unityPath = iniFile.Read("unity");
                if (!File.Exists(unityPath))
                {
                    unitySetup();
                }
            }

            if (iniFile.KeyExists("theme"))
            {
                if (iniFile.Read("theme") == "light")
                {
                    metroStyleManager.Theme = MetroThemeStyle.Light;
                }
                else
                {
                    metroStyleManager.Theme = MetroThemeStyle.Dark;
                }
            }

            if (iniFile.KeyExists("style"))
            {
                LoadStyle(iniFile.Read("style"));
            }

            string pluginCheck = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location).Replace("GUI", "");
            if (!File.Exists(pluginCheck + @"\Plugins\ARESPlugin.dll") && apiEnabled)
            {
                btnSearch.Enabled = false;
#if DEBUG
                btnSearch.Enabled = true;
                btnHotswap.Enabled = true;
#endif
            }

            if (!string.IsNullOrEmpty(unityPath))
            {
                var unitySetup = CoreFunctions.setupHSB(this);
                if (unitySetup == (true, false))
                {
                    CoreFunctions.setupUnity(unityPath, this);
                }
            }


            MessageBoxManager.Yes = "PC";
            MessageBoxManager.No = "Quest";
            MessageBoxManager.Register();

            localAvatars = CoreFunctions.getLocalAvatars(this);
            if (localAvatars.Count > 0 && apiEnabled)
            {
                uploadThread = new Thread(() => CoreFunctions.uploadToApi(localAvatars, this));
                uploadThread.Start();
            }

            localWorlds = CoreFunctions.getLocalWorlds(this);
            if (localWorlds.Count > 0 && apiEnabled)
            {
                CoreFunctions.uploadToApiWorld(localWorlds, this);
            }
            try
            {
                CoreFunctions.WriteLog("Fetching unity sources", this);
                scanThread = new Thread(() => ScanPackage.DownloadOnlineSourcesOnStartup(this));
                scanThread.Start();
            }
            catch { }
        }

        private void unitySetup()
        {
            string unityPath = unityRegistry();
            if (unityPath != null)
            {
                DialogResult dlgResult = MessageBox.Show(string.Format("Possible unity path found, Location: '{0}' is this correct?", unityPath + @"\Editor\Unity.exe"), "Unity", MessageBoxButtons.YesNo);
                if (dlgResult == DialogResult.Yes)
                {
                    if (File.Exists(unityPath + @"\Editor\Unity.exe"))
                    {
                        iniFile.Write("unity", unityPath + @"\Editor\Unity.exe");
                        MessageBox.Show("Leave the command window open it will close by itself after the unity setup is complete");
                    }
                    else
                    {
                        MessageBox.Show("Someone didn't check because that file doesn't exist!");
                        selectFile();
                    }
                }
                else
                {
                    MessageBox.Show("Please select unity.exe, after doing this leave the command window open it will close by itself after setup is complete");
                    selectFile();
                }
            }
            else
            {
                MessageBox.Show("Please select unity.exe, after doing this leave the command window open it will close by itself after setup is complete");
                selectFile();
            }
        }

        private void selectFile()
        {
            var fileContent = string.Empty;
            var filePath = string.Empty;

            using (System.Windows.Forms.OpenFileDialog openFileDialog = new System.Windows.Forms.OpenFileDialog())
            {
                openFileDialog.InitialDirectory = "c:\\";
                openFileDialog.Filter = "Unity (Unity.exe)|Unity.exe";
                openFileDialog.RestoreDirectory = true;
                openFileDialog.Title = "Select Unity exe";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    //Get the path of specified file
                    filePath = openFileDialog.FileName;
                }
            }
            unityPath = filePath;
            iniFile.Write("unity", filePath);
        }

        private string unityRegistry()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Unity Technologies\Installer\Unity"))
                {
                    if (key != null)
                    {
                        Object o = key.GetValue("Location x64");
                        if (o != null)
                        {
                            return o.ToString();
                        }
                    }
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        private string selectFileVrca()
        {
            var fileContent = string.Empty;
            var filePath = string.Empty;

            using (System.Windows.Forms.OpenFileDialog openFileDialog = new System.Windows.Forms.OpenFileDialog())
            {
                openFileDialog.InitialDirectory = "c:\\";
                openFileDialog.Filter = "vrc* files (*.vrc*)|*.vrc*";
                openFileDialog.RestoreDirectory = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    //Get the path of specified file
                    filePath = openFileDialog.FileName;
                }
            }
            return filePath;
        }

        private void btnSearch_Click(object sender, EventArgs e)
        {
            if (!locked)
            {
                maxThreads = Convert.ToInt32(nmThread.Value);
                loadImages = chkLoadImages.Checked;
                flowAvatars.Controls.Clear();

                if (!cbSearchTerm.Text.Contains("World"))
                {
                    List<Records> avatars = ApiGrab.getAvatars(txtSearchTerm.Text, cbSearchTerm.Text, cbLimit.Text);
                    AvatarList = avatars;
                    if (chkPC.Checked)
                    {
                        AvatarList = AvatarList.Where(x => x.PCAssetURL.Trim().ToLower() != "none").ToList();
                    }
                    if (chkQuest.Checked)
                    {
                        AvatarList = AvatarList.Where(x => x.QUESTAssetURL.Trim().ToLower() != "none").ToList();
                    }
                    if (chkPublic.Checked == true && chkPrivate.Checked == false)
                    {
                        AvatarList = AvatarList.Where(x => x.Releasestatus.ToLower().Trim() == "public").ToList();
                    }
                    if (chkPublic.Checked == false && chkPrivate.Checked == true)
                    {
                        AvatarList = AvatarList.Where(x => x.Releasestatus.ToLower().Trim() == "private").ToList();
                    }
                    avatarCount = AvatarList.Count();
                    lblAvatarCount.Text = avatarCount.ToString();
                    locked = true;
                    isAvatar = true;
                    imageThread = new Thread(new ThreadStart(GetImages));
                    imageThread.Start();
                }
                else
                {
                    List<WorldClass> worlds = ApiGrab.getWorlds(txtSearchTerm.Text, cbSearchTerm.Text);
                    worldList = worlds;
                    worldCount = worlds.Count();
                    lblAvatarCount.Text = worldCount.ToString();
                    locked = true;
                    isAvatar = false;
                    imageThread = new Thread(new ThreadStart(GetImagesWorld));
                    imageThread.Start();
                }
            }
            else
            {
                MetroMessageBox.Show(this, "Still loading last search", "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void GetImages()
        {
            try
            {
                foreach (var item in AvatarList)
                {
                    while (currentThreads >= maxThreads)
                    {
                        Thread.Sleep(50);
                    }
                    currentThreads++;
                    var t = new Thread(() => MultiGetImages(item));
                    t.Start();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            locked = false;
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
                Panel groupBox = new Panel { Size = new Size(150, 150), BackColor = Color.Transparent };
                PictureBox avatarImage = new PictureBox { SizeMode = PictureBoxSizeMode.StretchImage, Size = new Size(148, 146) };
                PictureBox ripped = new PictureBox { SizeMode = PictureBoxSizeMode.StretchImage, Size = new Size(148, 146) };
                string questPc = "";
                if (item.PCAssetURL.ToLower() != "none")
                {
                    questPc += "{PC}";
                }
                if (item.QUESTAssetURL.ToLower() != "none")
                {
                    questPc += "{Quest}";
                }
                Label label = new Label { Text = "Avatar Name: " + item.AvatarName + " [" + questPc + "]", BackColor = Color.Transparent, ForeColor = Color.Red, Size = new Size(148, 146) };
                Bitmap bitmap = null;
                if (loadImages)
                {
                    bitmap = CoreFunctions.loadImage(item.ThumbnailURL, chkNoImages.Checked);
                }

                if (bitmap != null || !loadImages)
                {
                    avatarImage.Image = bitmap;
                    label.Name = item.AvatarID;
                    label.Click += LoadInfo;
                    groupBox.Controls.Add(avatarImage);
                    if (rippedList.Contains(item.AvatarID))
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
                    ContextMenu cm = new ContextMenu();
                    cm.MenuItems.Add("Hotswap", new EventHandler(btnHotswap_Click));
                    cm.MenuItems.Add("Extract", new EventHandler(btnExtractVRCA_Click));
                    cm.MenuItems.Add("Download", new EventHandler(btnDownload_Click));
                    label.ContextMenu = cm;

                    if (flowAvatars.InvokeRequired)
                    {
                        flowAvatars.Invoke((MethodInvoker)delegate
                        {
                            flowAvatars.Controls.Add(groupBox);
                        });
                    }
                }
                else
                {
                    avatarCount--;
                    if (lblAvatarCount.InvokeRequired)
                    {
                        lblAvatarCount.Invoke((MethodInvoker)delegate
                        {
                            lblAvatarCount.Text = avatarCount.ToString();
                        });
                    }
                }
                currentThreads--;
            }
            catch
            {
                currentThreads--;
            }
        }

        public void GetImagesWorld()
        {
            try
            {
                foreach (var item in worldList)
                {
                    Panel groupBox = new Panel { Size = new Size(150, 150), BackColor = Color.Transparent };
                    PictureBox avatarImage = new PictureBox { SizeMode = PictureBoxSizeMode.StretchImage, Size = new Size(148, 146) };
                    Label label = new Label { Text = "World Name: " + item.WorldName, BackColor = Color.Transparent, ForeColor = Color.Red, Size = new Size(148, 146) };
                    Bitmap bitmap = null;
                    if (loadImages)
                    {
                        bitmap = CoreFunctions.loadImage(item.ThumbnailURL, chkNoImages.Checked);
                    }

                    if (bitmap != null || !loadImages)
                    {
                        avatarImage.Image = bitmap;
                        label.Name = item.WorldID;
                        //avatarImage.Click += LoadInfo;
                        label.Click += LoadInfoWorld;
                        groupBox.Controls.Add(avatarImage);
                        groupBox.Controls.Add(label);
                        label.Parent = avatarImage;
                        if (flowAvatars.InvokeRequired)
                        {
                            flowAvatars.Invoke((MethodInvoker)delegate
                            {
                                flowAvatars.Controls.Add(groupBox);
                            });
                        }
                    }
                    else
                    {
                        worldCount--;
                        if (lblAvatarCount.InvokeRequired)
                        {
                            lblAvatarCount.Invoke((MethodInvoker)delegate
                            {
                                lblAvatarCount.Text = worldCount.ToString();
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            locked = false;
        }

        private void LoadInfo(object sender, EventArgs e)
        {
            var img = (Label)sender;
            selectedAvatar = AvatarList.Find(x => x.AvatarID == img.Name);
            txtAvatarInfo.Text = CoreFunctions.SetAvatarInfo(selectedAvatar);

            Bitmap bitmap; bitmap = CoreFunctions.loadImage(selectedAvatar.ImageURL, chkNoImages.Checked);

            if (bitmap != null)
            {
                selectedImage.Image = bitmap;
            }
            if (selectedAvatar.PCAssetURL != "None")
            {
                try
                {
                    string[] version = selectedAvatar.PCAssetURL.Split('/');
                    string urlCheck = selectedAvatar.PCAssetURL.Replace(version[6] + "/" + version[7] + "/file", version[6]);
                    RootClass versionList = ApiGrab.getVersions(urlCheck);
                    nmPcVersion.Value = Convert.ToInt32(versionList.versions.LastOrDefault().version);
                }
                catch { nmPcVersion.Value = 1; }
            }
            else
            {
                nmPcVersion.Value = 0;
            }
            if (selectedAvatar.QUESTAssetURL != "None")
            {
                try
                {
                    string[] version = selectedAvatar.QUESTAssetURL.Split('/');
                    string urlCheck = selectedAvatar.QUESTAssetURL.Replace(version[6] + "/" + version[7] + "/file", version[6]);
                    RootClass versionList = ApiGrab.getVersions(urlCheck);
                    nmQuestVersion.Value = Convert.ToInt32(versionList.versions.LastOrDefault().version);
                }
                catch { nmQuestVersion.Value = 1; }
            }
            else
            {
                nmQuestVersion.Value = 0;
            }
        }

        private void LoadInfoWorld(object sender, EventArgs e)
        {
            var img = (Label)sender;
            selectedWorld = worldList.Find(x => x.WorldID == img.Name);
            txtAvatarInfo.Text = CoreFunctions.SetWorldInfo(selectedWorld);

            Bitmap bitmap; bitmap = CoreFunctions.loadImage(selectedWorld.ImageURL, chkNoImages.Checked);

            if (bitmap != null)
            {
                selectedImage.Image = bitmap;
            }
            if (selectedWorld.PCAssetURL != "None")
            {
                string[] version = selectedWorld.PCAssetURL.Split('/');
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
                if (selectedAvatar != null)
                {
                    if (txtAvatarInfo.Text.Contains("avtr_") && selectedAvatar.AvatarID.Contains("avtr_"))
                    {
                        System.Windows.Forms.SaveFileDialog savefile = new System.Windows.Forms.SaveFileDialog();
                        string fileName = "custom.vrca";
                        savefile.Filter = "VRCA files (*.vrca)|*.vrca";
                        savefile.FileName = fileName;

                        if (savefile.ShowDialog() == DialogResult.OK)
                        {
                            fileName = savefile.FileName;
                        }
                        if (!downloadVRCA(fileName))
                        {
                            return;
                        }
                    }
                }

                if (selectedWorld != null)
                {
                    if (txtAvatarInfo.Text.Contains("wrld_") && selectedWorld.WorldID.Contains("wrld_"))
                    {
                        System.Windows.Forms.SaveFileDialog savefile = new System.Windows.Forms.SaveFileDialog();
                        string fileName = "custom.VRCW";
                        savefile.Filter = "VRCW files (*.VRCW)|*.VRCW";
                        savefile.FileName = fileName;

                        if (savefile.ShowDialog() == DialogResult.OK)
                        {
                            fileName = savefile.FileName;
                        }
                        if (!downloadVRCW(fileName))
                        {
                            return;
                        }
                    }
                }
            }
            else
            {
                MetroMessageBox.Show(this, "Please select an avatar or world first.", "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private bool downloadVRCW(string fileName = "custom.vrcw")
        {
            string filePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (fileName == "custom.vrcw")
            {
                fileName = filePath + @"\custom.vrcw";
            }

            string[] version = selectedWorld.PCAssetURL.Split('/');
            version[7] = nmPcVersion.Value.ToString();

            downloadFile(string.Join("/", version), fileName);
            return true;
        }

        private bool downloadVRCA(string fileName = "custom.vrca")
        {
            string filePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (fileName == "custom.vrca")
            {
                fileName = filePath + @"\custom.vrca";
            }
            if (selectedAvatar.AuthorName != "VRCA")
            {
                if (selectedAvatar.PCAssetURL != "None" && selectedAvatar.QUESTAssetURL != "None")
                {
                    DialogResult dlgResult = MessageBox.Show("Select which version to download", "VRCA Select", MessageBoxButtons.YesNo, MessageBoxIcon.Information);

                    if (dlgResult == DialogResult.No)
                    {
                        if (selectedAvatar.QUESTAssetURL != "None")
                        {
                            string[] version = selectedAvatar.QUESTAssetURL.Split('/');
                            version[7] = nmQuestVersion.Value.ToString();
                            downloadFile(string.Join("/", version), fileName);
                        }
                        else
                        {
                            MetroMessageBox.Show(this, "Quest version doesn't exist", "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return false;
                        }
                    }
                    else if (dlgResult == DialogResult.Yes)
                    {
                        if (selectedAvatar.PCAssetURL != "None")
                        {
                            string[] version = selectedAvatar.PCAssetURL.Split('/');
                            version[7] = nmPcVersion.Value.ToString();
                            downloadFile(string.Join("/", version), fileName);
                        }
                        else
                        {
                            MetroMessageBox.Show(this, "PC version doesn't exist", "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return false;
                        }
                    }
                    else
                    {
                        return false;
                    }
                }
                else if (selectedAvatar.PCAssetURL != "None")
                {
                    string[] version = selectedAvatar.PCAssetURL.Split('/');
                    version[7] = nmPcVersion.Value.ToString();
                    downloadFile(string.Join("/", version), fileName);
                }
                else if (selectedAvatar.QUESTAssetURL != "None")
                {
                    string[] version = selectedAvatar.QUESTAssetURL.Split('/');
                    version[7] = nmQuestVersion.Value.ToString();
                    downloadFile(string.Join("/", version), fileName);
                }
                else { return false; }
            }
            else
            {
                downloadFile(selectedAvatar.PCAssetURL, fileName);
            }

            return true;
        }

        private void btnExtractVRCA_Click(object sender, EventArgs e)
        {
            if (selectedAvatar != null && isAvatar)
            {
                if (!downloadVRCA())
                {
                    return;
                }

                FolderBrowserDialog folderDlg = new FolderBrowserDialog
                {
                    ShowNewFolderButton = true
                };
                // Show the FolderBrowserDialog.
                DialogResult result = DialogResult.OK;
                if (!toggleAvatar.Checked || txtAvatarOutput.Text == "")
                {
                    result = folderDlg.ShowDialog();
                }
                else
                {
                    folderDlg.SelectedPath = txtAvatarOutput.Text;
                }
                if (result == DialogResult.OK || (toggleAvatar.Checked && txtAvatarOutput.Text != ""))
                {
                    string unityVersion = cbVersionUnity.Text + "DLL";
                    string filePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                    string avatarName = Encoding.ASCII.GetString(
                    Encoding.Convert(
                        Encoding.UTF8,
                        Encoding.GetEncoding(
                            Encoding.ASCII.EncodingName,
                            new EncoderReplacementFallback(string.Empty),
                            new DecoderExceptionFallback()
                            ),
                        Encoding.UTF8.GetBytes(selectedAvatar.AvatarName)
                        )
                    );
                    avatarName += "-ARES";
                    char[] invalidFileNameChars = Path.GetInvalidFileNameChars();
                    string folderExtractLocation = folderDlg.SelectedPath + @"\" + new string(avatarName.Where(ch => !invalidFileNameChars.Contains(ch)).ToArray());
                    if (!Directory.Exists(folderExtractLocation))
                    {
                        Directory.CreateDirectory(folderExtractLocation);
                    }
                    string commands = string.Format("/C AssetRipperConsole.exe \"{2}\" \"{3}\\AssetRipperConsole_win64\\{0}\" -o \"{1}\" -q ", unityVersion, folderExtractLocation, filePath + @"\custom.vrca", filePath);

                    Process p = new Process();
                    ProcessStartInfo psi = new ProcessStartInfo
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
                        Directory.Move(folderExtractLocation + @"\Assets\Shader", folderExtractLocation + @"\Assets\.Shader");
                    }
                    catch { }
                    if (selectedAvatar.AvatarID != "VRCA")
                    {
                        File.AppendAllText(filePath + @"\Ripped.txt", selectedAvatar.AvatarID + "\n");
                        rippedList.Add(selectedAvatar.AvatarID);
                    }
                }
            }
            else if (selectedWorld != null && !isAvatar)
            {
                if (!downloadVRCW())
                {
                    return;
                }

                FolderBrowserDialog folderDlg = new FolderBrowserDialog
                {
                    ShowNewFolderButton = true
                };
                // Show the FolderBrowserDialog.
                DialogResult result = DialogResult.OK;
                if (!toggleWorld.Checked || txtWorldOutput.Text == "")
                {
                    result = folderDlg.ShowDialog();
                }
                else
                {
                    folderDlg.SelectedPath = txtWorldOutput.Text;
                }
                if (result == DialogResult.OK || (toggleWorld.Checked && txtWorldOutput.Text != ""))
                {
                    string unityVersion = cbVersionUnity.Text + "DLL";
                    string filePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                    string worldName = Encoding.ASCII.GetString(
                    Encoding.Convert(
                        Encoding.UTF8,
                        Encoding.GetEncoding(
                            Encoding.ASCII.EncodingName,
                            new EncoderReplacementFallback(string.Empty),
                            new DecoderExceptionFallback()
                            ),
                        Encoding.UTF8.GetBytes(selectedWorld.WorldName)
                        )
                    );
                    worldName += "-ARES";
                    char[] invalidFileNameChars = Path.GetInvalidFileNameChars();
                    string folderExtractLocation = folderDlg.SelectedPath + @"\" + new string(worldName.Where(ch => !invalidFileNameChars.Contains(ch)).ToArray());
                    if (!Directory.Exists(folderExtractLocation))
                    {
                        Directory.CreateDirectory(folderExtractLocation);
                    }
                    string commands = string.Format("/C AssetRipperConsole.exe \"{2}\" \"{3}\\AssetRipperConsole_win64\\{0}\" -o \"{1}\" -q ", unityVersion, folderExtractLocation, filePath + @"\custom.vrcw", filePath);

                    Process p = new Process();
                    ProcessStartInfo psi = new ProcessStartInfo
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
                        Directory.Move(folderExtractLocation + @"\Assets\Shader", folderExtractLocation + @"\Assets\.Shader");
                    }
                    catch { }
                }
            }
            else
            {
                MetroMessageBox.Show(this, "Please select an avatar or world first.", "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void downloadFile(string url, string saveName)
        {
            using (var client = new WebClient())
            {
                try
                {
                    client.Headers.Add("user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/99.0.4844.74 Safari/537.36");
                    client.DownloadFile(url, saveName);
                }
                catch (Exception ex)
                {
                    if (ex.Message == "(404) Not Found")
                    {
                        MetroMessageBox.Show(this, "Version doesn't exist or file has been deleted from VRChat servers", "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void btnStopSearch_Click(object sender, EventArgs e)
        {
            if (imageThread != null)
            {
                imageThread.Abort();
                locked = false;
            }
        }

        private void btnSearchLocal_Click(object sender, EventArgs e)
        {
            List<Records> localRecords = localAvatars;
            if (!locked)
            {
                loadImages = chkLoadImages.Checked;
                flowAvatars.Controls.Clear();
                List<Records> avatars = null;

                if (chkPC.Checked)
                {
                    localRecords = localRecords.Where(x => x.PCAssetURL.Trim().ToLower() != "none").ToList();
                }
                if (chkQuest.Checked)
                {
                    localRecords = localRecords.Where(x => x.QUESTAssetURL.Trim().ToLower() != "none").ToList();
                }
                if (chkPublic.Checked == true && chkPrivate.Checked == false)
                {
                    localRecords = localRecords.Where(x => x.Releasestatus.ToLower().Trim() == "public").ToList();
                }
                if (chkPublic.Checked == false && chkPrivate.Checked == true)
                {
                    localRecords = localRecords.Where(x => x.Releasestatus.ToLower().Trim() == "private").ToList();
                }
                if (cbSearchTerm.Text == "Avatar Name" && txtSearchTerm.Text != "")
                {
                    localRecords = localRecords.Where(x => x.AvatarName.Contains(txtSearchTerm.Text)).ToList();
                }
                if (cbSearchTerm.Text == "Avatar ID" && txtSearchTerm.Text != "")
                {
                    localRecords = localRecords.Where(x => String.Equals(x.AvatarID, txtSearchTerm.Text, StringComparison.CurrentCultureIgnoreCase)).ToList();
                }
                if (cbSearchTerm.Text == "Author Name" && txtSearchTerm.Text != "")
                {
                    localRecords = localRecords.Where(x => x.AuthorName.Contains(txtSearchTerm.Text)).ToList();
                }
                if (cbSearchTerm.Text == "Author ID" && txtSearchTerm.Text != "")
                {
                    localRecords = localRecords.Where(x => String.Equals(x.AuthorID, txtSearchTerm.Text, StringComparison.CurrentCultureIgnoreCase)).ToList();
                }
                avatars = localRecords;

                avatars = avatars.OrderByDescending(x => x.TimeDetected).ToList();

                if (cbLimit.Text != "Max")
                {
                    avatars = avatars.Take(Convert.ToInt32(cbLimit.Text)).ToList();
                }

                AvatarList = avatars;
                avatarCount = avatars.Count();
                lblAvatarCount.Text = avatarCount.ToString();
                locked = true;
                isAvatar = true;
                imageThread = new Thread(new ThreadStart(GetImages));
                imageThread.Start();
            }
            else
            {
                MetroMessageBox.Show(this, "Still loading last search", "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnHotswap_Click(object sender, EventArgs e)
        {
            if (vrcaThread != null)
            {
                if (vrcaThread.IsAlive)
                {
                    MetroMessageBox.Show(this, "Hotswap is still busy with previous request", "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }
            if (selectedAvatar != null)
            {
                if (!downloadVRCA())
                {
                    return;
                }
                hotswapConsole = new HotswapConsole();
                hotswapConsole.Show();
                vrcaThread = new Thread(new ThreadStart(hotswap));
                vrcaThread.Start();
            }
            else
            {
                MetroMessageBox.Show(this, "Please select an avatar first.", "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void hotswap()
        {
            string filePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string fileDecompressed = filePath + @"\decompressed.vrca";
            string fileDecompressed2 = filePath + @"\decompressed1.vrca";
            string fileDecompressedFinal = filePath + @"\finalDecompressed.vrca";
            string fileDummy = filePath + @"\dummy.vrca";
            string fileTarget = filePath + @"\target.vrca";
            string tempFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData).Replace("\\Roaming", "");
            string unityVRCA = tempFolder + "\\Local\\Temp\\DefaultCompany\\HSB\\custom.vrca";
            string regexId = @"avtr_[\w]{8}-[\w]{4}-[\w]{4}-[\w]{4}-[\w]{12}";
            string regexPrefabId = @"prefab-id-v1_avtr_[\w]{8}-[\w]{4}-[\w]{4}-[\w]{4}-[\w]{12}_[\d]{10}\.prefab";
            string regexCab = @"CAB-[\w]{32}";
            string regexUnity = @"20[\d]{2}\.[\d]\.[\d]{2}f[\d]";
            Regex AvatarIdRegex = new Regex(regexId);
            Regex AvatarPrefabIdRegex = new Regex(regexPrefabId);
            Regex AvatarCabRegex = new Regex(regexCab);
            Regex UnityRegex = new Regex(regexUnity);

            tryDelete(fileDecompressed);
            tryDelete(fileDecompressed2);
            tryDelete(fileDecompressedFinal);
            tryDelete(fileDummy);
            tryDelete(fileTarget);

            try
            {
                File.Copy(unityVRCA, fileDummy);
            }
            catch
            {
                MessageBox.Show("Make sure you've started the build and publish on unity", "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                if (hotswapConsole.InvokeRequired)
                {
                    hotswapConsole.Invoke((MethodInvoker)delegate
                    {
                        hotswapConsole.Close();
                    });
                }
                return;
            }

            try
            {
                HotSwap.DecompressToFileStr(fileDummy, fileDecompressed, hotswapConsole);
            }
            catch (Exception ex)
            {
                CoreFunctions.WriteLog(string.Format("{0}", ex.Message), this);
                MessageBox.Show("Error decompressing VRCA file", "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                if (hotswapConsole.InvokeRequired)
                {
                    hotswapConsole.Invoke((MethodInvoker)delegate
                    {
                        hotswapConsole.Close();
                    });
                }
                return;
            }
            MatchModel matchModelNew = getMatches(fileDecompressed, AvatarIdRegex, AvatarCabRegex, UnityRegex, AvatarPrefabIdRegex);

            try
            {
                HotSwap.DecompressToFileStr(filePath + @"\custom.vrca", fileDecompressed2, hotswapConsole);
            }
            catch (Exception ex)
            {
                CoreFunctions.WriteLog(string.Format("{0}", ex.Message), this);
                MessageBox.Show("Error decompressing VRCA file", "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                if (hotswapConsole.InvokeRequired)
                {
                    hotswapConsole.Invoke((MethodInvoker)delegate
                    {
                        hotswapConsole.Close();
                    });
                }
                return;
            }

            MatchModel matchModelOld = getMatches(fileDecompressed2, AvatarIdRegex, AvatarCabRegex, UnityRegex, AvatarPrefabIdRegex);
            if (matchModelOld.UnityVersion == null)
            {
                DialogResult dialogResult = MessageBox.Show("Possible risky hotswap detected", "Risky Upload", MessageBoxButtons.OKCancel, MessageBoxIcon.Error);
                if (dialogResult == DialogResult.Cancel)
                {
                    if (hotswapConsole.InvokeRequired)
                    {
                        hotswapConsole.Invoke((MethodInvoker)delegate
                        {
                            hotswapConsole.Close();
                        });
                    }
                    return;
                }
            }

            getReadyForCompress(fileDecompressed2, fileDecompressedFinal, matchModelOld, matchModelNew);

            try
            {
                HotSwap.CompressBundle(fileDecompressedFinal, fileTarget, hotswapConsole);
            }
            catch (Exception ex)
            {
                CoreFunctions.WriteLog(string.Format("{0}", ex.Message), this);
                MessageBox.Show("Error compressing VRCA file");
                if (hotswapConsole.InvokeRequired)
                {
                    hotswapConsole.Invoke((MethodInvoker)delegate
                    {
                        hotswapConsole.Close();
                    });
                }
                return;
            }
            try
            {
                File.Copy(fileTarget, unityVRCA, true);
            }
            catch { }

            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = new FileInfo(fileTarget).Length;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }

            string compressedSize = string.Format("{0:0.##} {1}", len, sizes[order]);

            len = new FileInfo(fileDecompressed2).Length;
            order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }

            string uncompressedSize = string.Format("{0:0.##} {1}", len, sizes[order]);
            CoreFunctions.WriteLog(string.Format("Successfully hotswapped avatar"), this);
            if (selectedAvatar != null)
            {
                if (selectedAvatar.AvatarID == "VRCA")
                {
                    imageSave();
                }
                else { selectedImage.Image.Save(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\HSB\HSB\Assets\ARES SMART\Resources\ARESLogoTex.png", ImageFormat.Png); }
            }
            if (selectedWorld != null)
            {
                if (selectedWorld.WorldID == "VRCA")
                {
                    imageSave();
                }
                else { selectedImage.Image.Save(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\HSB\HSB\Assets\ARES SMART\Resources\ARESLogoTex.png", ImageFormat.Png); }
            }


            tryDelete(fileDecompressed);
            tryDelete(fileDecompressed2);
            tryDelete(fileDecompressedFinal);
            tryDelete(fileDummy);
            tryDelete(fileTarget);

            if (hotswapConsole.InvokeRequired)
            {
                hotswapConsole.Invoke((MethodInvoker)delegate
                {
                    hotswapConsole.Close();
                });
            }

            MessageBox.Show(string.Format("Got file sizes, comp:{0}, decomp:{1}", compressedSize, uncompressedSize), "Info", MessageBoxButtons.OK, MessageBoxIcon.Information); ;
            File.AppendAllText(filePath + @"\Ripped.txt", matchModelOld.AvatarId + "\n");
            rippedList.Add(matchModelOld.AvatarId);
        }

        private void imageSave()
        {
            using (WebClient webClient = new WebClient())
            {
                byte[] data = webClient.DownloadData("https://source.unsplash.com/random/1200x900?sig=incrementingIdentifier");
                using (MemoryStream mem = new MemoryStream(data))
                {
                    using (var yourImage = Image.FromStream(mem))
                    {
                        yourImage.Save(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\HSB\HSB\Assets\ARES SMART\Resources\ARESLogoTex.png", ImageFormat.Png);
                    }
                }

            }
        }

        private MatchModel getMatches(string file, Regex avatarId, Regex avatarCab, Regex unityVersion, Regex avatarAssetId)
        {
            MatchCollection avatarIdMatch = null;
            MatchCollection avatarAssetIdMatch = null;
            MatchCollection avatarCabMatch = null;
            MatchCollection unityMatch = null;
            int unityCount = 0;

            foreach (string line in File.ReadLines(file))
            {
                var tempId = avatarId.Matches(line);
                var tempAssetId = avatarAssetId.Matches(line);
                var tempCab = avatarCab.Matches(line);
                var tempUnity = unityVersion.Matches(line);
                if (tempAssetId.Count > 0)
                {
                    avatarAssetIdMatch = tempAssetId;
                }
                if (tempId.Count > 0)
                {
                    avatarIdMatch = tempId;
                }
                if (tempCab.Count > 0)
                {
                    avatarCabMatch = tempCab;
                }
                if (tempUnity.Count > 0)
                {
                    unityMatch = tempUnity;
                    unityCount++;
                }
            }
            if (avatarAssetIdMatch == null)
            {
                avatarAssetIdMatch = avatarIdMatch;
            }

            MatchModel matchModel = new MatchModel
            {
                AvatarId = avatarIdMatch[0].Value,
                AvatarCab = avatarCabMatch[0].Value,
                AvatarAssetId = avatarAssetIdMatch[0].Value
            };

            if (unityMatch != null)
            {
                matchModel.UnityVersion = unityMatch[0].Value;
            }
            return matchModel;
        }

        private void getReadyForCompress(string oldFile, string newFile, MatchModel old, MatchModel newModel)
        {
            var enc = Encoding.GetEncoding(28591);
            using (StreamReaderOver vReader = new StreamReaderOver(oldFile, enc))
            {
                using (StreamWriter vWriter = new StreamWriter(newFile, false, enc))
                {
                    while (!vReader.EndOfStream)
                    {
                        string vLine = vReader.ReadLine();
                        string replace = checkAndReplaceLine(vLine, old, newModel);
                        vWriter.Write(replace);
                    }
                }
            }
        }

        private string checkAndReplaceLine(string line, MatchModel old, MatchModel newModel)
        {
            string edited = line;
            if (edited.Contains(old.AvatarAssetId))
            {
                edited = edited.Replace(old.AvatarAssetId, newModel.AvatarAssetId);
            }
            if (edited.Contains(old.AvatarId))
            {
                edited = edited.Replace(old.AvatarId, newModel.AvatarId);
            }
            if (edited.Contains(old.AvatarCab))
            {
                edited = edited.Replace(old.AvatarCab, newModel.AvatarCab);
            }
            if (old.UnityVersion != null)
            {
                if (edited.Contains(old.UnityVersion))
                {
                    edited = edited.Replace(old.UnityVersion, newModel.UnityVersion);
                }
            }
            return edited;
        }

        private void hotswapRepair()
        {
            string filePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string fileDecompressed2 = filePath + @"\decompressed1.vrca";

            tryDelete(fileDecompressed2);

            try
            {
                HotSwap.DecompressToFileStr(filePath + @"\custom.vrca", fileDecompressed2, hotswapConsole);
            }
            catch (Exception ex)
            {
                CoreFunctions.WriteLog(string.Format("{0}", ex.Message), this);
                MetroMessageBox.Show(this, "Error decompressing VRCA file", "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string oldId = getFileString(fileDecompressed2, @"(avtr_[\w\d]{8}-[\w\d]{4}-[\w\d]{4}-[\w\d]{4}-[\w\d]{12})");
            string oldCab = getFileString(fileDecompressed2, @"(CAB-[\w\d]{32})");

            hotswapConsole.Close();

            txtSearchTerm.Text = oldId;
            cbSearchTerm.SelectedIndex = 2;
            btnSearch.PerformClick();

            txtAvatarInfo.Text += Environment.NewLine + "Avatar Id from VRCA: " + oldId + Environment.NewLine + "CAB Id from VRCA: " + oldCab;
            CoreFunctions.WriteLog(string.Format("Repaired VRCA file"), this);
        }

        private string getFileString(string file, string searchRegexString)
        {
            string line;
            string lineReturn = null;

            System.IO.StreamReader fileOpen =
                new System.IO.StreamReader(file);

            while ((line = fileOpen.ReadLine()) != null)
            {
                lineReturn = Regex.Match(line, searchRegexString).Value;
                if (!string.IsNullOrEmpty(lineReturn))
                {
                    break;
                }
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
                    CoreFunctions.WriteLog(string.Format("Deleted file {0}", location), this);
                }
            }
            catch (Exception ex)
            {
                CoreFunctions.WriteLog(string.Format("{0}", ex.Message), this);
            }
        }

        private void tryDeleteDirectory(string location, bool showExceptions = true)
        {
            try
            {
                Directory.Delete(location, true);
                CoreFunctions.WriteLog(string.Format("Deleted file {0}", location), this);
            }
            catch (Exception ex)
            {
                if (showExceptions)
                {
                    CoreFunctions.WriteLog(string.Format("{0}", ex.Message), this);
                }
            }
        }

        private void btnUnity_Click(object sender, EventArgs e)
        {
            string tempFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData).Replace("\\Roaming", "");
            string unityTemp = "\\Local\\Temp\\DefaultCompany\\HSB";
            string unityTemp2 = "\\LocalLow\\Temp\\DefaultCompany\\HSB";

            tryDeleteDirectory(tempFolder + unityTemp, false);
            tryDeleteDirectory(tempFolder + unityTemp2, false);

            var unitySetup = CoreFunctions.setupHSB(this);
            if (unitySetup == (true, false))
            {
                CoreFunctions.setupUnity(unityPath, this);
                CoreFunctions.openUnityPreSetup(unityPath, this);
            }
            else if (unitySetup == (true, true))
            {
                CoreFunctions.openUnityPreSetup(unityPath, this);
            }
            btnHotswap.Enabled = true;
        }

        private void btnLoadVRCA_Click(object sender, EventArgs e)
        {
            selectedImage.ImageLocation = "https://github.com/Dean2k/A.R.E.S/releases/latest/download/ARESLogo.png";
            string file = selectFileVrca();
            if (Path.GetExtension(file).ToLower() == ".vrca")
            {
                isAvatar = true;
                selectedAvatar = new Records
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
                txtAvatarInfo.Text = CoreFunctions.SetAvatarInfo(selectedAvatar);
            }
            else
            {
                isAvatar = false;
                selectedWorld = new WorldClass
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
                txtAvatarInfo.Text = CoreFunctions.SetWorldInfo(selectedWorld);
            }
        }

        private void btnRepair_Click(object sender, EventArgs e)
        {
            if (vrcaThread != null)
            {
                if (vrcaThread.IsAlive)
                {
                    MetroMessageBox.Show(this, "Hotswap is still busy with previous request", "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }
            if (selectedAvatar != null)
            {
                if (selectedAvatar.AuthorName == "VRCA")
                {
                    if (!downloadVRCA())
                    {
                        return;
                    }
                    hotswapConsole = new HotswapConsole();
                    hotswapConsole.Show();
                    vrcaThread = new Thread(new ThreadStart(hotswapRepair));
                    vrcaThread.Start();
                }
                else
                {
                    MetroMessageBox.Show(this, "Please load a VRCA file first", "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                MetroMessageBox.Show(this, "Please select an avatar first.", "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnVrcaSearch_Click(object sender, EventArgs e)
        {
            if (vrcaThread != null)
            {
                if (vrcaThread.IsAlive)
                {
                    MetroMessageBox.Show(this, "VRCA search (Hotswap) is still busy with previous request", "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }
            if (selectedAvatar != null)
            {
                if (selectedAvatar.AuthorName == "VRCA")
                {
                    if (!downloadVRCA())
                    {
                        return;
                    }
                    mTabMain.Show();
                    mTab.SelectedIndex = 0;
                    hotswapConsole = new HotswapConsole();
                    hotswapConsole.Show();
                    hotswapRepair();
                }
                else
                {
                    MetroMessageBox.Show(this, "Please load a VRCA file first", "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                MetroMessageBox.Show(this, "Please select an avatar first.", "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnBrowserView_Click(object sender, EventArgs e)
        {
            if (AvatarList != null)
            {
                generateHtml.GenerateHtmlPage(AvatarList);
                Process.Start("avatars.html");
            }
        }

        private void btnCopy_Click(object sender, EventArgs e)
        {
            if (isAvatar)
            {
                if (cbCopy.Text == "Time Dectected")
                {
                    Clipboard.SetText(selectedAvatar.TimeDetected);
                    MetroMessageBox.Show(this, "information copied to clipboard.", "Copied", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                if (cbCopy.Text == "Avatar ID")
                {
                    Clipboard.SetText(selectedAvatar.AvatarID);
                    MetroMessageBox.Show(this, "information copied to clipboard.", "Copied", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                if (cbCopy.Text == "Avatar Name")
                {
                    Clipboard.SetText(selectedAvatar.AvatarName);
                    MetroMessageBox.Show(this, "information copied to clipboard.", "Copied", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                if (cbCopy.Text == "Avatar Description")
                {
                    Clipboard.SetText(selectedAvatar.AvatarDescription);
                    MetroMessageBox.Show(this, "information copied to clipboard.", "Copied", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                if (cbCopy.Text == "Author ID")
                {
                    Clipboard.SetText(selectedAvatar.AuthorID);
                    MetroMessageBox.Show(this, "information copied to clipboard.", "Copied", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                if (cbCopy.Text == "Author Name")
                {
                    Clipboard.SetText(selectedAvatar.AuthorName);
                    MetroMessageBox.Show(this, "information copied to clipboard.", "Copied", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                if (cbCopy.Text == "PC Asset URL")
                {
                    Clipboard.SetText(selectedAvatar.PCAssetURL);
                    MetroMessageBox.Show(this, "information copied to clipboard.", "Copied", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                if (cbCopy.Text == "Quest Asset URL")
                {
                    Clipboard.SetText(selectedAvatar.QUESTAssetURL);
                    MetroMessageBox.Show(this, "information copied to clipboard.", "Copied", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                if (cbCopy.Text == "Image URL")
                {
                    Clipboard.SetText(selectedAvatar.ImageURL);
                    MetroMessageBox.Show(this, "information copied to clipboard.", "Copied", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                if (cbCopy.Text == "Thumbnail URL")
                {
                    Clipboard.SetText(selectedAvatar.ThumbnailURL);
                    MetroMessageBox.Show(this, "information copied to clipboard.", "Copied", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                if (cbCopy.Text == "Unity Version")
                {
                    Clipboard.SetText(selectedAvatar.UnityVersion);
                    MetroMessageBox.Show(this, "information copied to clipboard.", "Copied", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                if (cbCopy.Text == "Release Status")
                {
                    Clipboard.SetText(selectedAvatar.Releasestatus);
                    MetroMessageBox.Show(this, "information copied to clipboard.", "Copied", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                if (cbCopy.Text == "Tags")
                {
                    Clipboard.SetText(selectedAvatar.Tags);
                    MetroMessageBox.Show(this, "information copied to clipboard.", "Copied", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            else
            {
                MetroMessageBox.Show(this, "Only works for avatars atm.", "Copied", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void btnApi_Click(object sender, EventArgs e)
        {
            if (apiEnabled)
            {
                btnSearch.Enabled = false;
                apiEnabled = false;
                btnApi.Text = "Enable API";
                iniFile.Write("apiEnabled", "false");
            }
            else if (!apiEnabled)
            {
                btnSearch.Enabled = true;
                apiEnabled = true;
                btnApi.Text = "Disable API";
                iniFile.Write("apiEnabled", "true");
            }
        }

        private void Ares_Close(object sender, FormClosedEventArgs e)
        {
            if (imageThread != null)
            {
                imageThread.Abort();
            }
            if (vrcaThread != null)
            {
                vrcaThread.Abort();
            }
            if (uploadThread != null)
            {
                uploadThread.Abort();
            }
            Thread.Sleep(2000);
        }

        private void btnScan_Click(object sender, EventArgs e)
        {
            if (scanThread.IsAlive)
            {
                MetroMessageBox.Show(this, "Still downloading hashes of files good & bad, please try again later", "Busy", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (Directory.Exists(ScanPackage.UnityTemp))
            {
                tryDeleteDirectory(ScanPackage.UnityTemp);
            }

            Directory.CreateDirectory(ScanPackage.UnityTemp);

            string packageSelected = selectPackage();
            string outpath = "";

            if (!string.IsNullOrEmpty(packageSelected))
            {
                outpath = PackageExtractor.ExtractPackage(packageSelected, ScanPackage.UnityTemp);
            }

            (int, int, int) scanCount = ScanPackage.CheckFiles(this);

            if (scanCount.Item3 > 0)
            {
                MessageBox.Show("Bad files were detected please select a new location for cleaned UnityPackage");
                string fileLocation = createPackage();
                var blank = new string[0];
                var rootDir = "Assets/";
                var pack = Package.FromDirectory(outpath, fileLocation, true, blank, blank);
                pack.GeneratePackage(rootDir);
            }
            else
            {
                MessageBox.Show("No Bad files were detected");
            }

            MessageBox.Show(string.Format("Bad files detected {0}, Safe files detected {1}, Unknown files detected {2}", scanCount.Item3, scanCount.Item1, scanCount.Item2));


        }

        private string selectPackage()
        {
            var fileContent = string.Empty;
            var filePath = string.Empty;

            using (System.Windows.Forms.OpenFileDialog openFileDialog = new System.Windows.Forms.OpenFileDialog())
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

                    using (StreamReader reader = new StreamReader(fileStream))
                    {
                        fileContent = reader.ReadToEnd();
                    }
                }
            }
            return filePath;
        }

        private string createPackage()
        {
            using (var sfd = new System.Windows.Forms.SaveFileDialog())
            {
                sfd.Filter = ".unitypackage files (*.unitypackage)|*.unitypackage";
                sfd.FilterIndex = 2;

                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    return sfd.FileName;
                }
            }
            return null;
        }

        private void btnHsbClean_Click(object sender, EventArgs e)
        {
            CleanHSB();
        }

        private void killProcess(string processName)
        {
            try
            {
                Process.Start("taskkill", "/F /IM \"" + processName + "\"");
                Console.WriteLine("Killed Process: " + processName);
                CoreFunctions.WriteLog(string.Format("Killed Process", processName), this);
            }
            catch { }
        }

        private void panel3_Paint(object sender, PaintEventArgs e)
        {

        }

        private void btnRipped_Click(object sender, EventArgs e)
        {


            if (!locked)
            {
                maxThreads = Convert.ToInt32(nmThread.Value);
                loadImages = chkLoadImages.Checked;
                flowAvatars.Controls.Clear();
                List<Records> avatars = ApiGrab.getRipped(rippedList);
                AvatarList = avatars;
                if (chkPC.Checked)
                {
                    AvatarList = AvatarList.Where(x => x.PCAssetURL.Trim().ToLower() != "none").ToList();
                }
                if (chkQuest.Checked)
                {
                    AvatarList = AvatarList.Where(x => x.QUESTAssetURL.Trim().ToLower() != "none").ToList();
                }
                if (chkPublic.Checked == true && chkPrivate.Checked == false)
                {
                    AvatarList = AvatarList.Where(x => x.Releasestatus.ToLower().Trim() == "public").ToList();
                }
                if (chkPublic.Checked == false && chkPrivate.Checked == true)
                {
                    AvatarList = AvatarList.Where(x => x.Releasestatus.ToLower().Trim() == "private").ToList();
                }
                avatarCount = AvatarList.Count();
                lblAvatarCount.Text = avatarCount.ToString();
                locked = true;
                isAvatar = true;
                imageThread = new Thread(new ThreadStart(GetImages));
                imageThread.Start();
            }
            else
            {
                MessageBox.Show("Still loading last search");
            }
        }

        private void btnUnityLoc_Click(object sender, EventArgs e)
        {
            selectFile();
        }

        private void mTabSettings_Click(object sender, EventArgs e)
        {

        }

        private AresConfig config;
        private string fileLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location).Replace(@"\GUI", @"\UserData") + @"\ARESConfig.json";
        private bool loading = true;

        private void mTab_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (mTab.SelectedIndex == 1)
            {
                LoadConfig();
                if (config != null)
                {
                    SetCheckBoxes();
                }
                else
                {
                    ConfigBox.Visible = false;
                }
                loading = false;
            }
        }

        private void LoadConfig()
        {
            try
            {
                string json = File.ReadAllText(fileLocation);
                config = JsonConvert.DeserializeObject<AresConfig>(json);
            }
            catch { }
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
            if (!loading)
            {
                string json = JsonConvert.SerializeObject(config);
                File.WriteAllText(fileLocation, json);
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
            iniFile.Write("theme", "light");
        }

        private void btnDark_Click(object sender, EventArgs e)
        {
            metroStyleManager.Theme = MetroThemeStyle.Dark;
            iniFile.Write("theme", "dark");
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
                    iniFile.Write("style", style);
                    break;

                case "White":
                    metroStyleManager.Style = MetroColorStyle.White;
                    iniFile.Write("style", style);
                    break;

                case "Silver":
                    metroStyleManager.Style = MetroColorStyle.Silver;
                    iniFile.Write("style", style);
                    break;

                case "Green":
                    metroStyleManager.Style = MetroColorStyle.Green;
                    iniFile.Write("style", style);
                    break;

                case "Blue":
                    metroStyleManager.Style = MetroColorStyle.Blue;
                    iniFile.Write("style", style);
                    break;

                case "Lime":
                    metroStyleManager.Style = MetroColorStyle.Lime;
                    iniFile.Write("style", style);
                    break;

                case "Teal":
                    metroStyleManager.Style = MetroColorStyle.Teal;
                    iniFile.Write("style", style);
                    break;

                case "Orange":
                    metroStyleManager.Style = MetroColorStyle.Orange;
                    iniFile.Write("style", style);
                    break;

                case "Brown":
                    metroStyleManager.Style = MetroColorStyle.Brown;
                    iniFile.Write("style", style);
                    break;

                case "Pink":
                    metroStyleManager.Style = MetroColorStyle.Pink;
                    iniFile.Write("style", style);
                    break;

                case "Magenta":
                    metroStyleManager.Style = MetroColorStyle.Magenta;
                    iniFile.Write("style", style);
                    break;

                case "Purple":
                    metroStyleManager.Style = MetroColorStyle.Purple;
                    iniFile.Write("style", style);
                    break;

                case "Red":
                    metroStyleManager.Style = MetroColorStyle.Red;
                    iniFile.Write("style", style);
                    break;

                case "Yellow":
                    metroStyleManager.Style = MetroColorStyle.Yellow;
                    iniFile.Write("style", style);
                    break;

                default:
                    metroStyleManager.Style = MetroColorStyle.Default;
                    iniFile.Write("style", "Default");
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
            FolderBrowserDialog folderDlg = new FolderBrowserDialog
            {
                ShowNewFolderButton = true
            };
            DialogResult result = folderDlg.ShowDialog();
            if (result == DialogResult.OK)
            {
                txtAvatarOutput.Text = folderDlg.SelectedPath;
                iniFile.Write("avatarOutput", folderDlg.SelectedPath);
            }
        }

        private void btnWorldOut_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog folderDlg = new FolderBrowserDialog
            {
                ShowNewFolderButton = true
            };
            DialogResult result = folderDlg.ShowDialog();
            if (result == DialogResult.OK)
            {
                txtWorldOutput.Text = folderDlg.SelectedPath;
                iniFile.Write("worldOutput", folderDlg.SelectedPath);
            }
        }

        private void toggleAvatar_CheckedChanged(object sender, EventArgs e)
        {
            iniFile.Write("avatarOutputAuto", toggleAvatar.Checked.ToString());
        }

        private void toggleWorld_CheckedChanged(object sender, EventArgs e)
        {
            iniFile.Write("worldOutputAuto", toggleWorld.Checked.ToString());
        }

        public int PluginCount = 0;
        public int ModCount = 0;
        public int PluginCountNumber = 0;
        public int ModCountNumber = 0;
        public int lineSkip = 0;

        private void btnCleanLog_Click(object sender, EventArgs e)
        {
            PluginCount = 0;
            ModCount = 0;
            PluginCountNumber = 0;
            ModCountNumber = 0;
            string melonLog = MelonLogLocation();
            string newMelonLog = SaveLogLocation();
            if (melonLog != null && newMelonLog != null)
            {
                melonLogClean(melonLog, newMelonLog);
                finalCleanup(newMelonLog);
                MetroMessageBox.Show(this, "Log file has been cleaned", "Log Cleaned", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void finalCleanup(string logLocation)
        {
            int newPlugin = PluginCountNumber - PluginCount;
            int newMod = ModCountNumber - ModCount;
            string text = File.ReadAllText(logLocation);
            text = text.Replace(PluginCountNumber + " Plugin Loaded", newPlugin.ToString() + " Plugin Loaded");
            text = text.Replace(ModCountNumber + " Mods Loaded", newMod.ToString() + " Mods Loaded");
            File.WriteAllText(logLocation, text);
        }

        private void melonLogClean(string oldFile, string cleanFile)
        {
            var enc = Encoding.UTF8;
            using (StreamReader vReader = new StreamReader(oldFile, enc))
            {
                using (StreamWriter vWriter = new StreamWriter(cleanFile, false, enc))
                {
                    while (!vReader.EndOfStream)
                    {
                        string vLine = vReader.ReadLine();
                        string replace = checkAndReplaceLine(vLine);
                        if (replace != null && lineSkip == 0)
                        {
                            vWriter.WriteLine(replace);
                        }
                        if(lineSkip > 0)
                        {
                            lineSkip--;
                        }
                    }
                }
            }
        }

        private string checkAndReplaceLine(string line)
        {
            if (line.Contains("Plugin Loaded"))
            {
                try
                {
                    string resultString = Regex.Match(line, @"\d+ Plugin Loaded").Value;
                    PluginCountNumber = Convert.ToInt32(Regex.Match(resultString, @"\d+").Value);
                }
                catch { }
            }
            if (line.Contains("Mods Loaded"))
            {
                try
                {
                    string resultString = Regex.Match(line, @"\d+ Mods Loaded").Value;
                    ModCountNumber = Convert.ToInt32(Regex.Match(resultString, @"\d+").Value);
                }
                catch { }
            }
            if (line.Contains("ARES Manager"))
            {
                PluginCount++;
                lineSkip = 4;
                return null;
            }
            if(line.Contains("A.R.E.S Logger v"))
            {
                ModCount++;
                lineSkip = 4;
                return null;
            }
            if (line.Contains("ReModCE_ARES"))
            {
                ModCount++;
                lineSkip = 4;
                return null;
            }
            if (line.Contains("A.R.E.S"))
            {
                return null;
            }
            if (line.Contains("ARES"))
            {
                return null;
            }
            return line;
        }

        private string SaveLogLocation()
        {
            System.Windows.Forms.SaveFileDialog savefile = new System.Windows.Forms.SaveFileDialog();
            string fileName = "MelonLog.log";
            savefile.Filter = "log files (*.log)|*.log";
            savefile.FileName = fileName;
            savefile.Title = "Select new cleaned melonlog location";

            if (savefile.ShowDialog() == DialogResult.OK)
            {
                fileName = savefile.FileName;
                return fileName;
            }
            return null;
        }

        private string MelonLogLocation()
        {
            var fileContent = string.Empty;
            var filePath = string.Empty;

            using (System.Windows.Forms.OpenFileDialog openFileDialog = new System.Windows.Forms.OpenFileDialog())
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

    }
}