using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ARES.Models
{
    public class Records
    {
        public string TimeDetected { get; set; }
        public string AvatarID { get; set; }
        public string AvatarName { get; set; }
        public string AvatarDescription { get; set; }
        public string AuthorName { get; set; }

        public string AuthorID { get; set; }
        public string PCAssetURL { get; set; }
        public string QUESTAssetURL { get; set; }
        public string ImageURL { get; set; }
        public string ThumbnailURL { get; set; }
        public string UnityVersion { get; set; }
        public string Releasestatus { get; set; }
        public string Tags { get; set; }
        public string Pin { get; set; }
        public string PinCode { get; set; }
    }

    public class Avatar
    {
        public List<Records> records { get; set; }
    }
}
