using ARES.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
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

            if (ApiKey != null)
            {
                apiStart = "unlocked";
            }

            if (!string.IsNullOrEmpty(query))
            {
                switch (type)
                {
                    case "Avatar Name":
                        url = $"https://{apiStart}.ares-mod.com/records/Avatars?include=Created,TimeDetected,AvatarID,AvatarName,AvatarDescription,AuthorID,AuthorName,PCAssetURL,QUESTAssetURL,ImageURL,ThumbnailURL,UnityVersion,Releasestatus,Tags,Pin,PinCode&size={amount}&order=Created,desc&filter=AvatarName,cs,{query}";
                        break;
                    case "Avatar ID":
                        url = $"https://{apiStart}.ares-mod.com/records/Avatars?include=Created,TimeDetected,AvatarID,AvatarName,AvatarDescription,AuthorID,AuthorName,PCAssetURL,QUESTAssetURL,ImageURL,ThumbnailURL,UnityVersion,Releasestatus,Tags,Pin,PinCode&size=1&order=Created,desc&filter=AvatarID,eq,{query}";
                        break;
                    case "Author Name":
                        url = $"https://{apiStart}.ares-mod.com/records/Avatars?include=Created,TimeDetected,AvatarID,AvatarName,AvatarDescription,AuthorID,AuthorName,PCAssetURL,QUESTAssetURL,ImageURL,ThumbnailURL,UnityVersion,Releasestatus,Tags,Pin,PinCode&size={amount}&order=Created,desc&filter=AuthorName,cs,{query}";
                        break;
                    case "Author ID":
                        url = $"https://{apiStart}.ares-mod.com/records/Avatars?include=Created,TimeDetected,AvatarID,AvatarName,AvatarDescription,AuthorID,AuthorName,PCAssetURL,QUESTAssetURL,ImageURL,ThumbnailURL,UnityVersion,Releasestatus,Tags,Pin,PinCode&size={amount}&order=Created,desc&filter=AuthorID,eq,{query}";
                        break;
                }
            }
            else
            {
                url = $"https://{apiStart}.ares-mod.com/records/Avatars?include=Created,TimeDetected,AvatarID,AvatarName,AvatarDescription,AuthorID,AuthorName,PCAssetURL,QUESTAssetURL,ImageURL,ThumbnailURL,UnityVersion,Releasestatus,Tags,Pin,PinCode&size={amount}&order=Created,desc";
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

        public List<Comments> GetComments(string avatarId, string version)
        {
            string url = $"https://api.ares-mod.com/records/AvatarComments?order=id&filter=AvatarId,cs,{avatarId}";

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

            RootComment items = JsonConvert.DeserializeObject<RootComment>(jsonString);

            return items.records;
        }

        public List<WorldClass> GetWorlds(string query, string type, string version)
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
            webReq.UserAgent = $"ARES V" + version;

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

        public Stats GetStats(string version)
        {
            HttpWebRequest webReq = (HttpWebRequest)WebRequest.Create(string.Format("https://api.ares-mod.com/statsV2.php"));

            webReq.Method = "GET";
            webReq.UserAgent = $"ARES V" + version;

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

        public List<Records> GetRipped(List<string> ripped, string version)
        {
            Avatar avatarList = new Avatar { records = new List<Records>() };

            if (ripped != null)
            {
                foreach (var item in ripped.Distinct().ToList())
                {

                    string url = "";
                    string apiStart = "api";

                    if (ApiKey != null)
                    {
                        apiStart = "unlocked";
                    }

                    url = $"https://{apiStart}.ares-mod.com/records/Avatars?include=TimeDetected,AvatarID,AvatarName,AvatarDescription,AuthorID,AuthorName,PCAssetURL,QUESTAssetURL,ImageURL,ThumbnailURL,UnityVersion,Releasestatus,Tags&size=1&order=TimeDetected,desc&filter=AvatarID,eq,{item}";

                    HttpWebRequest webReq = (HttpWebRequest)WebRequest.Create(url);

                    if (ApiKey != null)
                    {
                        webReq.Headers.Add("X-API-Key: " + ApiKey);
                    }

                    webReq.Method = "GET";
                    webReq.UserAgent = $"ARES V" + version;

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

        public void AddComment(Comments comment, string version)
        {
            var httpWebRequest = (HttpWebRequest)WebRequest.Create("https://api.ares-mod.com/records/AvatarComments");
            httpWebRequest.ContentType = "application/json";
            httpWebRequest.UserAgent = $"ARES V" + version;
            httpWebRequest.Method = "POST";
            string jsonPost = JsonConvert.SerializeObject(comment);
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
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public DateTime GetNistTime()
        {
            var myHttpWebRequest = (HttpWebRequest)WebRequest.Create("http://www.microsoft.com");
            var response = myHttpWebRequest.GetResponse();
            string todaysDates = response.Headers["date"];
            return DateTime.ParseExact(todaysDates,
                "ddd, dd MMM yyyy HH:mm:ss 'GMT'",
                CultureInfo.InvariantCulture.DateTimeFormat,
                DateTimeStyles.AssumeUniversal);
        }
    }
}   