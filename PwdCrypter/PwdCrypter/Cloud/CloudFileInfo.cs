using System;

namespace PwdCrypter.Cloud
{
    /// <summary>
    /// Stato del file su Cloud
    /// </summary>
    public enum CloudFileStatus
    {
        FileNotFound,
        FileFound,
        FileError,
        FileDownloadError,
        FileDownloaded
    };

    /// <summary>
    /// Classe che raggruppa le informazioni di un file nel Cloud
    /// </summary>
    public class CloudFileInfo
    {
        public string DownloadUrl { get; set; }          // @microsoft.graph.downloadUrl
        public string ID { get; set; }                   // id
        public string Name { get; set; }                 // name
        public long Size { get; set; }                   // size
        public string WebUrl { get; set; }               // webUrl

        public DateTime CreatedDateTime { get; set; }       // createdDateTime
        public DateTime LastModifiedDateTime { get; set; }  // lastModifiedDateTime

        public CloudFileInfo()
        {
            DownloadUrl = "";
            CreatedDateTime = new DateTime();
            ID = "";
            LastModifiedDateTime = new DateTime();
            Name = "";
            Size = 0;
            WebUrl = "";
        }
    }
}