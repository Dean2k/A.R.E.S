using SharpCompress.Common;
using SharpCompress.Readers;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;

namespace ARES.UPDATER
{
    internal class Program
    {
        private static string fileLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        private static string guiLocation = fileLocation + @"\GUI\ARES.exe";
        private static bool guiDownloaded;
        private static int timeout = 7200000;

        private static void Main(string[] args)
        {
            if (File.Exists("VRChat.exe"))
            {
                Console.WriteLine("This updater is about to close any running instances of\nARES, Unity, Unity Hub, Hotswaps and AssetRipper sessions!\nPlease save your work in your unity projects if they are open before\npressing enter to continue");
                while (Console.ReadKey().Key != ConsoleKey.Enter) { }
                killProcess("ARES.exe");
                killProcess("HOTSWAP.exe");
                killProcess("Unity Hub.exe");
                killProcess("Unity.exe");
                killProcess("AssetRipperConsole.exe");
                if (!Directory.Exists(fileLocation + @"\GUI"))
                {
                    Directory.CreateDirectory(fileLocation + @"\GUI");
                    Console.WriteLine("Updating ARES");
                    startGuiDownload();
                }

                if (guiDownloaded)
                {
                    extractGUI();
                    return;
                }

                string application = SHA256CheckSum(guiLocation);
                string latestString = GetHashLatestAsync("https://raw.githubusercontent.com/Dean2k/A.R.E.S/main/VersionHashes/ARESGUI.txt");

                if (application.ToLower() != latestString.ToLower())
                {
                    Console.WriteLine("Updating ARES");
                    startGuiDownload();
                    extractGUI();
                }
                startARES();
            }
            else
            {
                Console.WriteLine("The updater is not currently in the VRChat folder, please place it\nalongside your 'VRChat.exe' file for optimal preformance!\nYou can just hit enter to close meh!");
                while (Console.ReadKey().Key != ConsoleKey.Enter) { }
            }
            
        }

        private static void startGuiDownload()
        {
            FileDownloader fileDownloader = new FileDownloader("https://github.com/Dean2k/A.R.E.S/releases/latest/download/GUI.rar", fileLocation + @"\GUI.rar");
            fileDownloader.StartDownload(timeout, "GUI.rar");
            guiDownloaded = true;
        }

        private static void extractGUI()
        {
            try
            {
                using (Stream stream = File.OpenRead(fileLocation + @"\GUI.rar"))
                {
                    var reader = ReaderFactory.Open(stream);
                    while (reader.MoveToNextEntry())
                    {
                        if (!reader.Entry.IsDirectory)
                        {
                            Console.WriteLine(reader.Entry.Key);
                            reader.WriteEntryToDirectory(fileLocation + @"\GUI", new ExtractionOptions() { ExtractFullPath = true, Overwrite = true });
                        }
                    }
                }
            } catch (Exception ex)
            {
                Console.WriteLine("Extraction Error: " + ex.Message);
            }
            
        }

        private static void startARES()
        {
            try
            {
                string commands = string.Format("/C ARES.exe");

                Process p = new Process();
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "CMD.EXE",
                    Arguments = commands,
                    WorkingDirectory = fileLocation + @"\GUI\",
                    WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden
                };
                p.StartInfo = psi;
                p.Start();
            }
            catch { }
        }

        private static void killProcess(string processName)
        {
            try
            {
                Process.Start("taskkill", "/F /IM \"" + processName + "\"");
                Console.WriteLine("Killed Process: " + processName);
            }
            catch { }
        }

        private static string SHA256CheckSum(string filePath)
        {
            try
            {
                using (SHA256 SHA256 = SHA256Managed.Create())
                {
                    using (FileStream fileStream = File.OpenRead(filePath))
                        return BitConverter.ToString(SHA256.ComputeHash(fileStream)).Replace("-", "");
                }
            }
            catch { return "0"; }
        }

        private static string GetHashLatestAsync(string url)
        {
            string result;
            using (HttpClient client = new HttpClient())
            {
                result = client.GetStringAsync(url).Result;
            }
            return result;
        }
    }
}