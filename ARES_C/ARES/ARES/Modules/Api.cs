using ARES.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
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

        public List<Records> getAvatars(string query, string type, string limit)
        {
            string url = "";
            string amount;
            string apiStart = "api";

            if (limit == "Max")
            {
                amount = "10000";
            }
            else
            {
                amount = limit;
            }

            if(ApiKey != null)
            {
                apiStart = "unlocked";
            }

            if (!string.IsNullOrEmpty(query))
            {
                if (type == "Avatar Name")
                {
                    url = $"https://{apiStart}.ares-mod.com/records/Avatars?include=TimeDetected,AvatarID,AvatarName,AvatarDescription,AuthorID,AuthorName,PCAssetURL,QUESTAssetURL,ImageURL,ThumbnailURL,UnityVersion,Releasestatus,Tags&size={amount}&order=TimeDetected,desc&filter=AvatarName,cs,{query}";
                }
                if (type == "Avatar ID")
                {
                    url = $"https://{apiStart}.ares-mod.com/records/Avatars?include=TimeDetected,AvatarID,AvatarName,AvatarDescription,AuthorID,AuthorName,PCAssetURL,QUESTAssetURL,ImageURL,ThumbnailURL,UnityVersion,Releasestatus,Tags&size=1&order=TimeDetected,desc&filter=AvatarID,eq,{query}";
                }
                if (type == "Author Name")
                {
                    url = $"https://{apiStart}.ares-mod.com/records/Avatars?include=TimeDetected,AvatarID,AvatarName,AvatarDescription,AuthorID,AuthorName,PCAssetURL,QUESTAssetURL,ImageURL,ThumbnailURL,UnityVersion,Releasestatus,Tags&size={amount}&order=TimeDetected,desc&filter=AuthorName,cs,{query}";
                }
                if (type == "Author ID")
                {
                    url = $"https://{apiStart}.ares-mod.com/records/Avatars?include=TimeDetected,AvatarID,AvatarName,AvatarDescription,AuthorID,AuthorName,PCAssetURL,QUESTAssetURL,ImageURL,ThumbnailURL,UnityVersion,Releasestatus,Tags&size={amount}&order=TimeDetected,desc&filter=AuthorID,eq,{query}";
                }
            }
            else
            {
                url = $"https://{apiStart}.ares-mod.com/records/Avatars?include=TimeDetected,AvatarID,AvatarName,AvatarDescription,AuthorID,AuthorName,PCAssetURL,QUESTAssetURL,ImageURL,ThumbnailURL,UnityVersion,Releasestatus,Tags&size={amount}&order=TimeDetected,desc";
            }
            HttpWebRequest WebReq = (HttpWebRequest)WebRequest.Create(url);

            WebReq.Method = "GET";

            if (ApiKey != null)
            {
                WebReq.Headers.Add("X-API-Key: " + ApiKey);
            }

            HttpWebResponse WebResp = (HttpWebResponse)WebReq.GetResponse();        

            string jsonString;
            using (Stream stream = WebResp.GetResponseStream())   //modified from your code since the using statement disposes the stream automatically when done
            {
                StreamReader reader = new StreamReader(stream, System.Text.Encoding.UTF8);
                jsonString = reader.ReadToEnd();
            }

            Avatar items = JsonConvert.DeserializeObject<Avatar>(jsonString);

            return items.records;
        }

        public List<WorldClass> getWorlds(string query, string type)
        {
            string url = "";
            if (!string.IsNullOrEmpty(query))
            {
                if (type == "World Name")
                {
                    url = string.Format("https://api.ares-mod.com/records/Worlds?include=TimeDetected,WorldID,WorldName,WorldDescription,AuthorID,AuthorName,PCAssetURL,ImageURL,ThumbnailURL,UnityVersion,Releasestatus,Tags&size=500&order=TimeDetected,desc&filter=WorldName,cs,{0}", query);
                }
                if (type == "World ID")
                {
                    url = string.Format("https://api.ares-mod.com/records/Worlds?include=TimeDetected,WorldID,WorldName,WorldDescription,AuthorID,AuthorName,PCAssetURL,ImageURL,ThumbnailURL,UnityVersion,Releasestatus,Tags&size=500&order=TimeDetected,desc&filter=WorldID,eq,{0}", query);
                }
            }
            else
            {
                url = string.Format("https://api.ares-mod.com/records/Worlds?include=TimeDetected,WorldID,WorldName,WorldDescription,AuthorID,AuthorName,PCAssetURL,ImageURL,ThumbnailURL,UnityVersion,Releasestatus,Tags&size=500&order=TimeDetected,desc");
            }
            HttpWebRequest WebReq = (HttpWebRequest)WebRequest.Create(url);

            WebReq.Method = "GET";

            HttpWebResponse WebResp = (HttpWebResponse)WebReq.GetResponse();

            Console.WriteLine(WebResp.StatusCode);
            Console.WriteLine(WebResp.Server);

            string jsonString;
            using (Stream stream = WebResp.GetResponseStream())   //modified from your code since the using statement disposes the stream automatically when done
            {
                StreamReader reader = new StreamReader(stream, System.Text.Encoding.UTF8);
                jsonString = reader.ReadToEnd();
            }

            Worlds items = JsonConvert.DeserializeObject<Worlds>(jsonString);

            return items.records;
        }

        public Stats getStats()
        {
            HttpWebRequest WebReq = (HttpWebRequest)WebRequest.Create(string.Format("https://api.ares-mod.com/stats.php"));

            WebReq.Method = "GET";

            HttpWebResponse WebResp = (HttpWebResponse)WebReq.GetResponse();

            Console.WriteLine(WebResp.StatusCode);
            Console.WriteLine(WebResp.Server);

            string jsonString;
            using (Stream stream = WebResp.GetResponseStream())   //modified from your code since the using statement disposes the stream automatically when done
            {
                StreamReader reader = new StreamReader(stream, System.Text.Encoding.UTF8);
                jsonString = reader.ReadToEnd();
            }

            Stats item = JsonConvert.DeserializeObject<Stats>(jsonString);

            return item;
        }

        public RootClass getVersions(string url)
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

        public List<Records> getRipped(List<string> ripped)
        {
            string url = "";
            string amount;

            Avatar avatarList = new Avatar { records = new List<Records>() };

            if (ripped != null)
            {
                foreach (var item in ripped.Distinct().ToList())
                {
                    url = string.Format("https://api.ares-mod.com/records/Avatars?include=TimeDetected,AvatarID,AvatarName,AvatarDescription,AuthorID,AuthorName,PCAssetURL,QUESTAssetURL,ImageURL,ThumbnailURL,UnityVersion,Releasestatus,Tags&size=1&order=TimeDetected,desc&filter=AvatarID,eq,{0}", item);

                    HttpWebRequest WebReq = (HttpWebRequest)WebRequest.Create(url);

                    WebReq.Method = "GET";

                    HttpWebResponse WebResp = (HttpWebResponse)WebReq.GetResponse();

                    Console.WriteLine(WebResp.StatusCode);
                    Console.WriteLine(WebResp.Server);

                    string jsonString;
                    using (Stream stream = WebResp.GetResponseStream())   //modified from your code since the using statement disposes the stream automatically when done
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
