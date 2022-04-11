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
        [XmlElement("Bv")]
        public string Bv { get; set; }
        [XmlElement("Cid")]
        public long Cid { get; set; }
        [XmlElement("Quality")]
        public int Quality { get; set; }
        [XmlElement("CacheFolderPath")]
        public string CacheFolderPath { get; set; }
        [XmlArray("Parts"), XmlArrayItem("Part")]
        public DownloadPartXmlModel[] PartList { get; set; }
    }
    [XmlRoot("Part")]
    public class DownloadPartXmlModel
    {
        [XmlElement("OperationGuid")]
        public Guid TaskGuid { get; set; }
        [XmlElement("RestoreModelJson")]
        public string RestoreModelJson { get; set; }
    }
}
