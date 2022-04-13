using ARES.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ARES.Modules
{
    public class Api
    {
        public string ApiKey { get; set; }

        public List<Records> GetAvatars(string query, string type, string limit, string version)
        {
            string url = "";
            string apiStart = "api";

            var amount = limit == "Max" ? "10000" : limit;

            if(ApiKey != null)
            {
                apiStart = "unlocked";
            }

            if (!string.IsNullOrEmpty(query))
            {
                switch (type)
                {
                    case "Avatar Name":
                        url = $"https://{apiStart}.ares-mod.com/records/Avatars?include=TimeDetected,AvatarID,AvatarName,AvatarDescription,AuthorID,AuthorName,PCAssetURL,QUESTAssetURL,ImageURL,ThumbnailURL,UnityVersion,Releasestatus,Tags&size={amount}&order=TimeDetected,desc&filter=AvatarName,cs,{query}";
                        break;
                    case "Avatar ID":
                        url = $"https://{apiStart}.ares-mod.com/records/Avatars?include=TimeDetected,AvatarID,AvatarName,AvatarDescription,AuthorID,AuthorName,PCAssetURL,QUESTAssetURL,ImageURL,ThumbnailURL,UnityVersion,Releasestatus,Tags&size=1&order=TimeDetected,desc&filter=AvatarID,eq,{query}";
                        break;
                    case "Author Name":
                        url = $"https://{apiStart}.ares-mod.com/records/Avatars?include=TimeDetected,AvatarID,AvatarName,AvatarDescription,AuthorID,AuthorName,PCAssetURL,QUESTAssetURL,ImageURL,ThumbnailURL,UnityVersion,Releasestatus,Tags&size={amount}&order=TimeDetected,desc&filter=AuthorName,cs,{query}";
                        break;
                    case "Author ID":
                        url = $"https://{apiStart}.ares-mod.com/records/Avatars?include=TimeDetected,AvatarID,AvatarName,AvatarDescription,AuthorID,AuthorName,PCAssetURL,QUESTAssetURL,ImageURL,ThumbnailURL,UnityVersion,Releasestatus,Tags&size={amount}&order=TimeDetected,desc&filter=AuthorID,eq,{query}";
                        break;
                }
            }
            else
            {
                url = $"https://{apiStart}.ares-mod.com/records/Avatars?include=TimeDetected,AvatarID,AvatarName,AvatarDescription,AuthorID,AuthorName,PCAssetURL,QUESTAssetURL,ImageURL,ThumbnailURL,UnityVersion,Releasestatus,Tags&size={amount}&order=TimeDetected,desc";
            }
            HttpWebRequest webReq = (HttpWebRequest)WebRequest.Create(url);     
            webReq.UserAgent = $"ARES V" + version;

            webReq.Method = "GET";

            if (ApiKey != null)
            {
                webReq.Headers.Add("X-API-Key: " + ApiKey);
            }

            HttpWebResponse webResp = (HttpWebResponse)webReq.GetResponse();        

            string jsonString;
            using (Stream stream = webResp.GetResponseStream())   //modified from your code since the using statement disposes the stream automatically when done
            {
                StreamReader reader = new StreamReader(stream, System.Text.Encoding.UTF8);
                jsonString = reader.ReadToEnd();
            }

            Avatar items = JsonConvert.DeserializeObject<Avatar>(jsonString);

            return items.records;
        }

        public List<WorldClass> GetWorlds(string query, string type)
        {
            string url = "";
            if (!string.IsNullOrEmpty(query))
            {
                switch (type)
                {
                    case "World Name":
                        url =
                            $"https://api.ares-mod.com/records/Worlds?include=TimeDetected,WorldID,WorldName,WorldDescription,AuthorID,AuthorName,PCAssetURL,ImageURL,ThumbnailURL,UnityVersion,Releasestatus,Tags&size=500&order=TimeDetected,desc&filter=WorldName,cs,{query}";
                        break;
                    case "World ID":
                        url =
                            $"https://api.ares-mod.com/records/Worlds?include=TimeDetected,WorldID,WorldName,WorldDescription,AuthorID,AuthorName,PCAssetURL,ImageURL,ThumbnailURL,UnityVersion,Releasestatus,Tags&size=500&order=TimeDetected,desc&filter=WorldID,eq,{query}";
                        break;
                }
            }
            else
            {
                url = "https://api.ares-mod.com/records/Worlds?include=TimeDetected,WorldID,WorldName,WorldDescription,AuthorID,AuthorName,PCAssetURL,ImageURL,ThumbnailURL,UnityVersion,Releasestatus,Tags&size=500&order=TimeDetected,desc";
            }
            HttpWebRequest webReq = (HttpWebRequest)WebRequest.Create(url);

            webReq.Method = "GET";

            HttpWebResponse webResp = (HttpWebResponse)webReq.GetResponse();

            Console.WriteLine(webResp.StatusCode);
            Console.WriteLine(webResp.Server);

            string jsonString;
            using (Stream stream = webResp.GetResponseStream())   //modified from your code since the using statement disposes the stream automatically when done
            {
                StreamReader reader = new StreamReader(stream, System.Text.Encoding.UTF8);
                jsonString = reader.ReadToEnd();
            }

            Worlds items = JsonConvert.DeserializeObject<Worlds>(jsonString);

            return items.records;
        }

        public Stats GetStats()
        {
            HttpWebRequest webReq = (HttpWebRequest)WebRequest.Create(string.Format("https://api.ares-mod.com/stats.php"));

            webReq.Method = "GET";

            HttpWebResponse webResp = (HttpWebResponse)webReq.GetResponse();

            Console.WriteLine(webResp.StatusCode);
            Console.WriteLine(webResp.Server);

            string jsonString;
            using (Stream stream = webResp.GetResponseStream())   //modified from your code since the using statement disposes the stream automatically when done
            {
                StreamReader reader = new StreamReader(stream, System.Text.Encoding.UTF8);
                jsonString = reader.ReadToEnd();
            }

            Stats item = JsonConvert.DeserializeObject<Stats>(jsonString);

            return item;
        }

        public RootClass GetVersions(string url)
        {
            using (WebClient webClient = new WebClient())
            {
                //Needs a useragent to be able to view images.
                webClient.Headers.Add("user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/99.0.4844.74 Safari/537.36");
                try
                {
                    string web = webClient.DownloadString(url);
                    RootClass items = JsonConvert.DeserializeObject<RootClass>(web);
                    return items;
                }
                catch
                {
                    return null;
                    //skip as its likely avatar is been yeeted from VRC servers
                }
            }

        }

        public List<Records> GetRipped(List<string> ripped)
        {
            Avatar avatarList = new Avatar { records = new List<Records>() };

            if (ripped != null)
            {
                foreach (var item in ripped.Distinct().ToList())
                {
                    var url = string.Format("https://api.ares-mod.com/records/Avatars?include=TimeDetected,AvatarID,AvatarName,AvatarDescription,AuthorID,AuthorName,PCAssetURL,QUESTAssetURL,ImageURL,ThumbnailURL,UnityVersion,Releasestatus,Tags&size=1&order=TimeDetected,desc&filter=AvatarID,eq,{0}", item);

                    HttpWebRequest webReq = (HttpWebRequest)WebRequest.Create(url);

                    webReq.Method = "GET";

                    HttpWebResponse webResp = (HttpWebResponse)webReq.GetResponse();

                    Console.WriteLine(webResp.StatusCode);
                    Console.WriteLine(webResp.Server);

                    string jsonString;
                    using (Stream stream = webResp.GetResponseStream())   //modified from your code since the using statement disposes the stream automatically when done
                    {
                        StreamReader reader = new StreamReader(stream, System.Text.Encoding.UTF8);
                        jsonString = reader.ReadToEnd();
                    }

                    Avatar items = JsonConvert.DeserializeObject<Avatar>(jsonString);

                    avatarList.records = avatarList.records.Concat(items.records).ToList();


                }
                return avatarList.records;
            }
            return null;
        }
    }
}
