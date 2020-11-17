using BiliDownload.Models.Xml;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using Windows.Storage;

namespace BiliDownload.Helper
{
    public class CompleteXmlHelper
    {
        static string XmlPath = ApplicationData.Current.LocalFolder.Path + "\\CompleteDownloadList.xml";
        public static string ReadXml()
        {
            var file = File.Open(XmlPath, FileMode.Open);
            string content = new StreamReader(file).ReadToEnd();
            file.Close();
            file.Dispose();
            return content;
        }
        public static void WriteXml(string content)
        {
            DeleteCompletedDownloads();
            var file = File.Create(XmlPath);
            var bytes = Encoding.UTF8.GetBytes(content);
            file.Write(bytes, 0, bytes.Count());
            file.Close();
            file.Dispose();
        }
        public static bool CheckXml()
        {
            if (!File.Exists(XmlPath))
            {
                var file = File.Create(XmlPath);
                file.Close();
                file.Dispose();
            }
            if (string.IsNullOrWhiteSpace(ReadXml())) return false;
            else return true;
        }
        private static T XmlToModel<T>(string xml)
        {
            var serializer = new XmlSerializer(typeof(T));
            return (T)serializer.Deserialize(new StringReader(xml));
        }
        public static List<CompletedXmlModel> GetCompleteDownloads()
        {
            var xmlString = ReadXml();
            if (string.IsNullOrWhiteSpace(xmlString)) return null;

            List<CompletedXmlModel> xmlList;
            xmlList = XmlToModel<CompletedXmlModelCollection>(xmlString).XmlModels.ToList();

            return xmlList;
        }
        public static void DeleteCompletedDownloads()
        {
            File.Delete(XmlPath);
        }
        public static void StoreCompletedDownloads()
        {
            var list = DownloadPage.Current.completedDownloadList;
            if (list.Count < 1) DeleteCompletedDownloads();
            else
            {
                var xmlString = CreateCompleteXmlStream(list.ToList());
                WriteXml(xmlString);
            }
        }
        public static string CreateCompleteXmlStream(List<CompletedDownloadModel> list)
        {
            var xmlModel = new CompletedXmlModelCollection();
            xmlModel.XmlModels = list.Select(c => new CompletedXmlModel()
            {
                Title = c.Title,
                Size = c.SizeNative,
                Path = c.Path
            }).ToArray();
            var serializer = new XmlSerializer(typeof(CompletedXmlModelCollection));
            StringBuilder builder = new StringBuilder();
            serializer.Serialize(new StringWriter(builder), xmlModel);
            return builder.ToString();
        }
    }
}
