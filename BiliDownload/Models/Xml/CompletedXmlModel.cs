using System.Xml.Serialization;

namespace BiliDownload.Models.Xml
{
    [XmlRoot(Namespace = "", ElementName = "Files")]
    public class CompletedXmlModelCollection
    {
        [XmlElement(ElementName = "File")]
        public CompletedXmlModel[] XmlModels { get; set; }
    }
    [XmlRoot("File")]
    public class CompletedXmlModel
    {
        [XmlElement("Title")]
        public string Title { get; set; }
        [XmlElement("Size")]
        public ulong Size { get; set; }
        [XmlElement("Path")]
        public string Path { get; set; }
    }
}
