using System;
using System.Collections.Generic;

namespace ARES.Models
{
    public class FileUrl
    {
        public string fileName { get; set; }
        public string url { get; set; }
        public string md5 { get; set; }
        public int sizeInBytes { get; set; }
        public string status { get; set; }
        public string category { get; set; }
        public string uploadId { get; set; }
    }

    public class Delta
    {
        public string fileName { get; set; }
        public string url { get; set; }
        public string md5 { get; set; }
        public int sizeInBytes { get; set; }
        public string status { get; set; }
        public string category { get; set; }
        public string uploadId { get; set; }
    }

    public class Signature
    {
        public string fileName { get; set; }
        public string url { get; set; }
        public string md5 { get; set; }
        public int sizeInBytes { get; set; }
        public string status { get; set; }
        public string category { get; set; }
        public string uploadId { get; set; }
    }

    public class Version
    {
        public int version { get; set; }
        public string status { get; set; }
        public DateTime created_at { get; set; }
        public FileUrl file { get; set; }
        public Delta delta { get; set; }
        public Signature signature { get; set; }
    }

    public class RootClass
    {
        public string id { get; set; }
        public string name { get; set; }
        public string ownerId { get; set; }
        public string mimeType { get; set; }
        public string extension { get; set; }
        public List<object> tags { get; set; }
        public List<Version> versions { get; set; }
    }

}
