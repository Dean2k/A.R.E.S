using ARES.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Reflection;
using System.Windows.Forms;

namespace ARES.Modules
{
    // Thanks to FACs for his repos and scan methods
    public class ScanPackage
    {
        public static string FileLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        public static string MainFolder = FileLocation + @"\Safe Import";
        public static string SafeImport_Safe = MainFolder + @"\Safe Files";
        public static string SafeImport_Bad = MainFolder + @"\Unsafe Files";
        public static string UnityTemp = MainFolder + @"\UnityExtract";

        public static string[] safeFiles;
        public static string[] badFiles;


        public static void WriteLog(string logText, Main main)
        {
            string logBuilder = string.Format("{0:yy/MM/dd H:mm:ss} | {1} \n", DateTime.Now, logText);
            if (main.txtConsole.InvokeRequired)
            {
                main.txtConsole.Invoke((MethodInvoker)delegate
                {
                    main.txtConsole.Text += logBuilder + Environment.NewLine;
                });
            }
            else
            {
                main.txtConsole.Text += logBuilder + Environment.NewLine;
            }
            File.AppendAllText("LatestLog.txt", logBuilder);
        }

        public static void ReloadDatabase(Main main)
        {
            if (!Directory.Exists(SafeImport_Safe))
                Directory.CreateDirectory(SafeImport_Safe);

            if (!Directory.Exists(SafeImport_Bad))
                Directory.CreateDirectory(SafeImport_Bad);

            string[] SafePaths = Directory.GetFiles(SafeImport_Safe, "*.txt", SearchOption.AllDirectories);
            string[] UnsafePaths = Directory.GetFiles(SafeImport_Bad, "*.txt", SearchOption.AllDirectories);

            List<string> safepathslist = new List<string>();
            foreach (string f in SafePaths)
            {
                string file = f.Replace("\\", "/");
                var lines = File.ReadLines(file);
                foreach (var line in lines)
                {
                    if (line.StartsWith("#") || String.IsNullOrWhiteSpace(line)) continue;
                    string line2 = line.Trim();
                    if (!safepathslist.Contains(line2)) safepathslist.Add(line2);
                }
            }
            safeFiles = safepathslist.ToArray();

            List<string> unsafepathslist = new List<string>();
            foreach (string f in UnsafePaths)
            {
                string file = f.Replace("\\", "/");
                var lines = File.ReadLines(file);
                foreach (var line in lines)
                {
                    if (line.StartsWith("#") || String.IsNullOrWhiteSpace(line)) continue;
                    string line2 = line.Trim();
                    if (!unsafepathslist.Contains(line2)) unsafepathslist.Add(line2);
                }
            }
            badFiles = unsafepathslist.ToArray();

            WriteLog($"Database loaded with {safeFiles.Length} allowed hashes and {badFiles.Length} not allowed hashes.\n", main);
        }

        public static void DownloadOnlineSourcesOnStartup(Main main)
        {
            if (!Directory.Exists(MainFolder))
            {
                Directory.CreateDirectory(MainFolder);
            }

            DownloadGitHubContent_Single(main: main);
            ReloadDatabase(main);
        }

        public static (int, int, int) CheckFiles(Main main)
        {
            string[] CSPaths = Directory.GetFiles(UnityTemp, "*.cs", SearchOption.AllDirectories);
            string[] DLLPaths = Directory.GetFiles(UnityTemp, "*.dll", SearchOption.AllDirectories);

            string[] CombinedStrings = CSPaths.Concat(DLLPaths).ToArray();

            List<string> arrayToList = new List<string>();

            foreach (var item in CombinedStrings)
            {
                arrayToList.Add(item);
            }

            return CheckSafeUnsafeFiles(arrayToList, main);
        }

        public static string SHA256CheckSum(string filePath)
        {
            using (SHA256 SHA256 = SHA256.Create())
            {
                using (FileStream fileStream = File.OpenRead(filePath))
                    return BitConverter.ToString(SHA256.ComputeHash(fileStream)).Replace("-", "").ToLowerInvariant();
            }
        }

        public static (int,int,int) CheckSafeUnsafeFiles(List<string> importeds, Main main)
        {
            importeds.Sort();
            List<(string, string)> safeFiles = new List<(string, string)>();
            List<(string, string)> badFilesShouldDelete = new List<(string, string)>();
            List<(string, string)> unknownFiles = new List<(string, string)>();

            foreach (string file in importeds)
            {
                string fileHash = SHA256CheckSum(file);
                if (ScanPackage.badFiles.Contains(fileHash))
                {
                    badFilesShouldDelete.Add((file, fileHash));
                }
                else if (ScanPackage.safeFiles.Contains(fileHash))
                {
                    safeFiles.Add((file, fileHash));
                }
                else unknownFiles.Add((file, fileHash));
            }

            if (safeFiles.Count > 0)
            {
                string output = "";
                foreach (var f in safeFiles)
                {
                    output += f.Item1 + " | Hash: " + f.Item2 + "\n";
                }
                WriteLog($"Allowed scripts ({safeFiles.Count}):\n" + output, main);
            }

            if (unknownFiles.Count > 0)
            {
                string output = "";
                foreach (var f in unknownFiles)
                {
                    output += f.Item1 + " | Hash: " + f.Item2 + "\n";
                }
                WriteLog($"Unknown scripts ({unknownFiles.Count}):\n" + output, main);
            }

            if (badFilesShouldDelete.Count > 0)
            {
                string output = "";
                foreach (var f in badFilesShouldDelete)
                {
                    output += f.Item1 + " | Hash: " + f.Item2 + "\n";
                    File.Delete(f.Item1);
                    if (File.Exists(f.Item1 + ".meta"))
                    {
                        File.Delete(f.Item1 + ".meta");
                    }
                }
                WriteLog($"Not allowed scripts ({badFilesShouldDelete.Count}). They will be deleted:\n" + output, main);
            }
            return (safeFiles.Count, unknownFiles.Count, badFilesShouldDelete.Count);
        }

        public static void DownloadGitHubContent_Single(Main main, string gitname = "Purple420/Hashes-of-Safe-Scripts")
        {
            string json; GitHub_content[] contents;
            List<(string, string)> safes = new List<(string, string)>();
            List<(string, string)> unsafes = new List<(string, string)>();

            string safeurl = $"https://api.github.com/repos/{gitname}/contents/Safe%20Files";
            try
            {
                using (MyWebClient wc = new MyWebClient())
                {
                    json = wc.DownloadString(new Uri(safeurl));
                }
                contents = JsonConvert.DeserializeObject<GitHub_content[]>(json);
                foreach (var cont in contents)
                {
                    if (cont.type == "file" && cont.name.EndsWith(".txt"))
                    {
                        safes.Add((cont.name, cont.download_url));
                    }
                }
            }
            catch (WebException e)
            {
                WriteLog($"An error occurred while fetching Github page {gitname} (Safe Files). Internet down? Webpage down?\n" + e.Message, main);
            }
            catch (NotSupportedException e)
            {
                WriteLog($"A Not Supported Exception occurred while fetching Github page {gitname} (Safe Files).\n" + e.Message,main);
            }
            catch (Exception e)
            {
                WriteLog($"An Exception occurred while processing Github page {gitname} (Safe Files).\n" + e.Message, main);
            }

            string unsafeurl = $"https://api.github.com/repos/{gitname}/contents/Unsafe%20Files";
            try
            {
                using (MyWebClient wc = new MyWebClient())
                {
                    json = wc.DownloadString(new Uri(unsafeurl));
                }
                contents = JsonConvert.DeserializeObject<GitHub_content[]>(json);
                foreach (var cont in contents)
                {
                    if (cont.type == "file" && cont.name.EndsWith(".txt"))
                    {
                        unsafes.Add((cont.name, cont.download_url));
                    }
                }
            }
            catch (WebException e)
            {
                WriteLog($"An error occurred while fetching Github page {gitname} (Unsafe Files). Internet down? Webpage down?\n" + e.Message, main);
            }
            catch (NotSupportedException e)
            {
                WriteLog($"A Not Supported Exception occurred while fetching Github page {gitname} (Unsafe Files).\n" + e.Message, main);
            }
            catch (Exception e)
            {
                WriteLog($"An Exception occurred while processing Github page {gitname} (Unsafe Files).\n" + e.Message, main);
            }

            int downloadCount = safes.Count + unsafes.Count;
            string safefolder = SafeImport_Safe + "/" + gitname.Replace("/", " ");
            string unsafefolder = SafeImport_Bad + "/" + gitname.Replace("/", " ");
            if (Directory.Exists(safefolder)) Directory.Delete(safefolder, true);
            if (Directory.Exists(unsafefolder)) Directory.Delete(unsafefolder, true);
            if (!(downloadCount > 0))
            {
                WriteLog($"There were no Safe or Unsafe entries to download from the Github page {gitname}.\n", main);
                return;
            }

            Directory.CreateDirectory(safefolder);
            Directory.CreateDirectory(unsafefolder);
            float index = 0;
            foreach (var dd in safes)
            {
                try
                {
                    using (MyWebClient wc = new MyWebClient())
                    {
                        wc.DownloadFile(dd.Item2, safefolder + "/" + dd.Item1);
                    }
                }
                catch
                {
                    WriteLog($"Failed to download Allowed Hashes from GitHub {gitname}, {dd.Item1}\n", main);
                }
                index++;
            }
            foreach (var dd in unsafes)
            {
                try
                {
                    using (MyWebClient wc = new MyWebClient())
                    {
                        wc.DownloadFile(dd.Item2, unsafefolder + "/" + dd.Item1);
                    }
                }
                catch
                {
                    WriteLog($"Failed to download Not Allowed Hashes from GitHub {gitname}, {dd.Item1}\n", main);
                }
                index++;
            }
        }
    }
    public class MyWebClient : WebClient
    {
        public MyWebClient() : base()
        {
            this.Headers.Add("User-Agent: Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/99.0.4844.74 Safari/537.36");
            this.Headers.Add("Cache-Control", "no-cache");
            this.Headers.Add("Cache-Control", "no-store");
            this.Headers.Add("Pragma", "no-cache");
            this.Headers.Add("Expires", "-1");
        }
    }
}
