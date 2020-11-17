using System;
using System.Xml.Serialization;

namespace BiliDownload.Models.Xml
{
    [XmlRoot(Namespace = "", ElementName = "Downloads")]
    public class DownloadXmlModelCollection
    {
        [XmlElement(ElementName = "Download")]
        public DownloadXmlModel[] XmlModels { get; set; }
    }
    [XmlRoot("Download")]
    public class DownloadXmlModel // 单个下载任务的xml模型
    {
        [XmlElement("Name")]
        public string DownloadName { get; set; }
        [XmlElement("Title")]
        public string Title { get; set; }
        [XmlElement("VideoUrl")]
        public string VideoUrl { get; set; }
        [XmlElement("AudioUrl")]
        public string AudioUrl { get; set; }
        [XmlElement("CacheFolderPath")]
        public string CacheFolderPath { get; set; }
        [XmlArray("Parts"), XmlArrayItem("Part")]
        public DownloadPartXmlModel[] PartList { get; set; }
    }
    [XmlRoot("Part")]
    public class DownloadPartXmlModel
    {
        [XmlElement("OperationGuid")]
        public Guid OperationGuid { get; set; }
        [XmlElement("Url")]
        public string Url { get; set; }
        [XmlElement("CacheFilePath")]
        public string CacheFilePath { get; set; }
    }
}
