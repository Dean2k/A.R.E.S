﻿using ARES.Models;
using ARES.Properties;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ARES.Modules
{
    public class CoreFunctions
    {

        public string SetAvatarInfo(Records avatar)
        {
            string avatarString =
                $"Time Detected: {GetDate(avatar.TimeDetected)} {Environment.NewLine}Avatar Pin: {avatar.PinCode} {Environment.NewLine}Avatar ID: {avatar.AvatarID} {Environment.NewLine}Avatar Name: {avatar.AvatarName} {Environment.NewLine}Avatar Description {avatar.AvatarDescription} {Environment.NewLine}Author ID: {avatar.AuthorID} {Environment.NewLine}Author Name: {avatar.AuthorName} {Environment.NewLine}PC Asset URL: {avatar.PCAssetURL} {Environment.NewLine}Quest Asset URL: {avatar.QUESTAssetURL} {Environment.NewLine}Image URL: {avatar.ImageURL} {Environment.NewLine}Thumbnail URL: {avatar.ThumbnailURL} {Environment.NewLine}Unity Version: {avatar.UnityVersion} {Environment.NewLine}Release Status: {avatar.Releasestatus} {Environment.NewLine}Tags: {avatar.Tags}";
            return avatarString;
        }

        public string SetWorldInfo(WorldClass avatar)
        {
            string avatarString =
                $"Time Detected: {GetDate(avatar.TimeDetected)} {Environment.NewLine}World ID: {avatar.WorldID} {Environment.NewLine}World Name: {avatar.WorldName} {Environment.NewLine}World Description {avatar.WorldDescription} {Environment.NewLine}Author ID: {avatar.AuthorID} {Environment.NewLine}Author Name: {avatar.AuthorName} {Environment.NewLine}PC Asset URL: {avatar.PCAssetURL} {Environment.NewLine}Image URL: {avatar.ImageURL} {Environment.NewLine}Thumbnail URL: {avatar.ThumbnailURL} {Environment.NewLine}Unity Version: {avatar.UnityVersion} {Environment.NewLine}Release Status: {avatar.Releasestatus} {Environment.NewLine}Tags: {avatar.Tags}";
            return avatarString;
        }

        public Bitmap LoadImage(string url, bool loadBroken)
        {
            using (WebClient webClient = new WebClient())
            {
                //Needs a useragent to be able to view images.
                webClient.Headers.Add("user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/99.0.4844.74 Safari/537.36");
                try
                {
                    Stream stream = webClient.OpenRead(url);
                    Bitmap bitmap; bitmap = new Bitmap(stream);
                    stream.Close();
                    stream.Dispose();
                    return bitmap;
                }
                catch
                {
                    return loadBroken ? Resources.No_Image : null;
                    //skip as its likely avatar is been yeeted from VRC servers
                    //avatarImage.Load(CoreFunctions.ErrorImage);
                }

            }
        }

        public string GetDate(string unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            try
            {
                double time = Convert.ToDouble(unixTimeStamp);
                DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
                dateTime = dateTime.AddSeconds(time).ToLocalTime();
                return dateTime.ToString();
            } catch
            {
                return DateTime.UtcNow.ToString();
            }
        }

        public List<Records> GetLocalAvatars(Main main)
        {
            if (File.Exists("Log.txt"))
            {
                string contents = File.ReadAllText(@"Log.txt");
                string pattern = "Time Detected:(.*)\r\nAvatar ID:(.*)\r\nAvatar Name:(.*)\r\nAvatar Description:(.*)\r\nAuthor ID:(.*)\r\nAuthor Name:(.*)\r\nPC Asset URL:(.*)\r\nQuest Asset URL:(.*)\r\nImage URL:(.*)\r\nThumbnail URL:(.*)\r\nUnity Version:(.*)\r\nRelease Status:(.*)\r\nTags:(.*)";
                string[] logRecords = Regex.Matches(contents, pattern).Cast<Match>().Select(m => m.Value).ToArray();


                List<Records> list = logRecords.Select(item => item.Split('\n'))
                    .Select(lineItem => new Records
                    {
                        TimeDetected = lineItem[0].Split(':')[1].Replace("\r", ""),
                        AvatarID = lineItem[1].Split(':')[1].Replace("\r", ""),
                        AvatarName = lineItem[2].Split(':')[1].Replace("\r", ""),
                        AvatarDescription = lineItem[3].Split(':')[1].Replace("\r", ""),
                        AuthorID = lineItem[4].Split(':')[1].Replace("\r", ""),
                        AuthorName = lineItem[5].Split(':')[1].Replace("\r", ""),
                        PCAssetURL = string.Join("", lineItem[6].Split(':').Skip(1)).Replace("\r", "").Replace("https", "https:"),
                        QUESTAssetURL = string.Join("", lineItem[7].Split(':').Skip(1)).Replace("\r", "").Replace("https", "https:"),
                        ImageURL = string.Join("", lineItem[8].Split(':').Skip(1)).Replace("\r", "").Replace("https", "https:"),
                        ThumbnailURL = string.Join("", lineItem[9].Split(':').Skip(1)).Replace("\r", "").Replace("https", "https:"),
                        UnityVersion = lineItem[10].Split(':')[1].Replace("\r", ""),
                        Releasestatus = lineItem[11].Split(':')[1].Replace("\r", ""),
                        Tags = lineItem[12].Split(':')[1].Replace("\r", ""),
                        Pin = "false",
                        PinCode = "None"
                    })
                    .ToList();
                WriteLog("Loaded Local Avatars", main);
                return list;
            }
            return new List<Records>();
        }

        public List<WorldClass> GetLocalWorlds(Main main)
        {
            if (File.Exists("LogWorld.txt"))
            {
                List<WorldClass> list = new List<WorldClass>();
                string contents = File.ReadAllText(@"LogWorld.txt");
                string pattern = "Time Detected:(.*)\r\nWorld ID:(.*)\r\nWorld Name:(.*)\r\nWorld Description:(.*)\r\nAuthor ID:(.*)\r\nAuthor Name:(.*)\r\nPC Asset URL:(.*)\r\nImage URL:(.*)\r\nThumbnail URL:(.*)\r\nUnity Version:(.*)\r\nRelease Status:(.*)\r\nTags:(.*)";
                string[] logRecords = Regex.Matches(contents, pattern).Cast<Match>().Select(m => m.Value).ToArray();


                foreach (var item in logRecords)
                {
                    string[] lineItem = item.Split('\n');
                    WorldClass records = new WorldClass
                    {
                        TimeDetected = lineItem[0].Split(':')[1].Replace("\r", ""),
                        WorldID = lineItem[1].Split(':')[1].Replace("\r", ""),
                        WorldName = lineItem[2].Split(':')[1].Replace("\r", ""),
                        WorldDescription = lineItem[3].Split(':')[1].Replace("\r", ""),
                        AuthorID = lineItem[4].Split(':')[1].Replace("\r", ""),
                        AuthorName = lineItem[5].Split(':')[1].Replace("\r", ""),
                        PCAssetURL = string.Join("", lineItem[6].Split(':').Skip(1)).Replace("\r", "").Replace("https", "https:"),
                        ImageURL = string.Join("", lineItem[7].Split(':').Skip(1)).Replace("\r", "").Replace("https", "https:"),
                        ThumbnailURL = string.Join("", lineItem[8].Split(':').Skip(1)).Replace("\r", "").Replace("https", "https:"),
                        UnityVersion = lineItem[9].Split(':')[1].Replace("\r", ""),
                        Releasestatus = lineItem[10].Split(':')[1].Replace("\r", ""),
                        Tags = lineItem[11].Split(':')[1].Replace("\r", "")
                    };
                    list.Add(records);
                }
                WriteLog(string.Format("Loaded Local Worlds"), main);
                return list;
            }
            return new List<WorldClass>();
        }

        public (bool, bool) SetupHsb(Main main)
        {
            if (File.Exists("HSBC.rar"))
            {
                return (true, true);
            }
            try
            {
                TryDelete(main);
                string filePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                string commands = string.Format("/C UnRAR.exe x HSB.rar HSB -id[c,d,n,p,q] -O+");

                Process p = new Process();
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "CMD.EXE",
                    Arguments = commands,
                    WorkingDirectory = filePath,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
            };
                p.StartInfo = psi;
                p.Start();
                p.WaitForExit();
                return (true, false);
            }
            catch
            {
                return (false, false);
            }
        }

        public bool SetupUnity(string unityPath, Main main)
        {
            string filePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            try
            {
                string commands = string.Format("/C \"{0}\" -ProjectPath HSB", unityPath);

                Process p = new Process();
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "CMD.EXE",
                    Arguments = commands,
                    WorkingDirectory = filePath,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };
                p.StartInfo = psi;
                p.Start();
                p.WaitForExit();
            }
            catch { return false; }

            KillProcess("Unity Hub.exe", main);
            KillProcess("Unity.exe", main);

            try
            {
                File.Copy(filePath + @"\HSB\Assets\ARES SMART\Resources\.CurrentLayout-default.dwlt", filePath + @"\HSB\Library\CurrentLayout-default.dwlt", true);
            }
            catch { }

            try
            {

                string commands = string.Format("/C Rar.exe a HSBC.rar HSB");
                Process p = new Process();
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "CMD.EXE",
                    Arguments = commands,
                    WorkingDirectory = filePath,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };
                p.StartInfo = psi;
                p.Start();
                p.WaitForExit();

                TryDelete(main);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
            return true;
        }

        private void TryDelete(Main main)
        {
            try
            {
                KillProcess("Unity Hub.exe", main);
                KillProcess("Unity.exe", main);
                if (Directory.Exists("HSB"))
                {
                    Directory.Delete("HSB", true);
                }
                Directory.CreateDirectory("HSB");
            }
            catch { }
        }

        public bool OpenUnityPreSetup(string unityPath, Main main)
        {

            TryDelete(main);
            try
            {
                string filePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                string commands = string.Format("/C UnRAR.exe x HSBC.rar HSB -id[c,d,n,p,q] -O+");

                Process p = new Process();
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "CMD.EXE",
                    Arguments = commands,
                    WorkingDirectory = filePath,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };
                p.StartInfo = psi;
                p.Start();
                p.WaitForExit();

                if (Directory.Exists("HSB/HSB"))
                {
                    commands = string.Format("/C \"{0}\" -ProjectPath HSB/HSB", unityPath);

                    p = new Process();
                    psi = new ProcessStartInfo
                    {
                        FileName = "CMD.EXE",
                        Arguments = commands,
                        WorkingDirectory = filePath,
                    };
                    p.StartInfo = psi;
                    p.Start();
                    //p.WaitForExit();
                }
                return false;
            }
            catch { return false; }
        }

        private void KillProcess(string processName, Main main)
        {
            try
            {
                Process.Start("taskkill", "/F /IM \"" + processName + "\"");
                Console.WriteLine("Killed Process: " + processName);
                WriteLog(string.Format("Killed Process", processName), main);
            }
            catch { }
        }

        public void UploadToApi(List<Records> avatars, Main main)
        {
            string filePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string uploadedFile = filePath + @"\AvatarUploaded.txt";           
            if (!File.Exists(uploadedFile))
            {
                var myFile = File.Create(uploadedFile);
                myFile.Close();
            }
            Thread.Sleep(500);
            foreach (var item in avatars)
            {
                if (!HasAvatarId(uploadedFile, item.AvatarID))
                {
                    var httpWebRequest = (HttpWebRequest)WebRequest.Create("https://api.ares-mod.com/records/Avatars");
                    httpWebRequest.ContentType = "application/json";
                    httpWebRequest.Method = "POST";
                    string jsonPost = JsonConvert.SerializeObject(item);
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
                        }
                        File.AppendAllText(uploadedFile, item.AvatarID + Environment.NewLine);
                        WriteLog($"Avatar: {item.AvatarID} uploaded to API", main);
                    }
                    catch (Exception ex)
                    {
                        if (ex.Message.Contains("(409) Conflict"))
                        {
                            File.AppendAllText(uploadedFile, item.AvatarID + Environment.NewLine);
                            WriteLog($"Avatar: {item.AvatarID} already on API", main);
                        }
                    }
                    Console.WriteLine(item.AvatarID);
                }
            }
        }

        public void uploadToApiWorld(List<WorldClass> worlds, Main main)
        {

            string uploadedFile = "WorldUploaded.txt";
            if (!File.Exists(uploadedFile))
            {
                var myFile = File.Create(uploadedFile);
                myFile.Close();
            }
            Thread.Sleep(500);
            foreach (var item in worlds)
            {
                if (!HasAvatarId(uploadedFile, item.WorldID))
                {
                    var httpWebRequest = (HttpWebRequest)WebRequest.Create("https://api.ares-mod.com/records/Worlds");
                    httpWebRequest.ContentType = "application/json";
                    httpWebRequest.Method = "POST";
                    string jsonPost = JsonConvert.SerializeObject(item);
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
                        }
                        File.AppendAllText(uploadedFile, item.WorldID + Environment.NewLine);
                        WriteLog(string.Format("World: {0} Uploaded to API", item.WorldID), main);
                    }
                    catch (Exception ex)
                    {
                        if (ex.Message.Contains("(409) Conflict"))
                        {
                            File.AppendAllText(uploadedFile, item.WorldID + Environment.NewLine);
                            WriteLog(string.Format("World: {0} already on API", item.WorldID), main);
                        }
                    }
                    Console.WriteLine(item.WorldID);
                }
            }
        }

        public bool HasAvatarId(string avatarFile, string avatarId)
        {
            var lines = File.ReadLines(avatarFile);
            foreach (var line in lines)
            {
                if (line.Contains(avatarId))
                {
                    return true;
                }
            }

            return false;
        }

        public void WriteLog(string logText, Main main)
        {
            string logBuilder = string.Format("{0:yy/MM/dd H:mm:ss} | {1} \n", DateTime.Now, logText);
            if (main.txtConsole.InvokeRequired)
            {
                main.txtConsole.Invoke((MethodInvoker)delegate
                {
                    main.txtConsole.Text += logBuilder + Environment.NewLine;
                });
            } else
            {
                main.txtConsole.Text += logBuilder + Environment.NewLine;
            }
            File.AppendAllText("LatestLog.txt", logBuilder);
        }
    }
}
