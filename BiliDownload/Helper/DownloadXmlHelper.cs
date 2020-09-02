using BiliDownload.Model.Xml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using Windows.Storage;

namespace BiliDownload.Helper
{
    public static class DownloadXmlHelper
    {
        static string XmlPath = ApplicationData.Current.LocalFolder.Path + "\\CurrentDownloadList.xml";
        public static string ReadXml()
        {
            var file = File.Open(XmlPath, FileMode.Open);
            var content = new StreamReader(file).ReadToEnd();
            file.Close();
            file.Dispose();
            return content;
        }
        public static void WriteXml(string content)
        {
            DeleteCurrentDownloads();
            var file = File.Create(XmlPath);
            var bytes = Encoding.UTF8.GetBytes(content);
            file.Write(bytes, 0, bytes.Count());
            file.Close();
            file.Dispose();
        }
        public static bool CheckXml()//xml不存在就创建，当xml内容为空时，直接返回false
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
        public static List<DownloadXmlModel> GetCurrentDownloads()
        {
            var xmlString = ReadXml();
            if (string.IsNullOrWhiteSpace(xmlString)) return null;

            List<DownloadXmlModel> xmlList;
            xmlList = XmlToModel<DownloadXmlModelCollection>(xmlString).XmlModels.ToList();

            return xmlList;
        }
        private static T XmlToModel<T>(string xml)
        {
            //xml = Regex.Replace(xml, @"<\?xml*.*?>", "", RegexOptions.IgnoreCase);
            //XmlSerializer xmlSer = new XmlSerializer(typeof(T));
            //using (StringReader xmlReader = new StringReader(xml))
            //{
            //    return (T)xmlSer.Deserialize(xmlReader);
            //}
            var serializer = new XmlSerializer(typeof(T));
            return (T)serializer.Deserialize(new StringReader(xml));
        }
        public static void DeleteCurrentDownloads()
        {
            File.Delete(XmlPath);
        }
        public static void StoreCurrentDownloads()
        {
            var list = DownloadPage.Current.activeDownloadList;
            if (list?.Count < 1) return;//没有内容就不用储存了
            var doc = CreateDownloadXDocument(list.ToList()).ToStringWithDeclaration();
            WriteXml(doc);
        }
        public static XDocument CreateDownloadXDocument(List<DownloadViewModel> models)//创建Xml文档
        {
            var list = new List<XElement>();
            foreach (var model in models)
            {
                list.Add(CreateSingleDownloadXElement(model));
            }
            var xdoc = new XDocument(new XDeclaration("1.0", "utf-8", "yes"), new XElement("Downloads", list));
            return xdoc;
        }
        public static XElement CreateSingleDownloadXElement(DownloadViewModel viewModel)//单个视频创建XElement
        {
            var model = new DownloadXmlModel()
            {
                DownloadName = viewModel.DownloadName,
                Title = viewModel.Title,
                VideoUrl = viewModel.VideoUrl,
                AudioUrl = viewModel.AudioUrl,
                CacheFolderPath = viewModel.CacheFolderPath,
                PartList = new DownloadPartXmlModel[]
                {
                    new DownloadPartXmlModel()
                    {
                        OperationGuid = viewModel.PartList[0].OperationGuid,
                        Url = viewModel.PartList[0].Url,
                        CacheFilePath = viewModel.PartList[0].CacheFilePath
                    },
                    new DownloadPartXmlModel()
                    {
                        OperationGuid = viewModel.PartList[1].OperationGuid,
                        Url = viewModel.PartList[1].Url,
                        CacheFilePath = viewModel.PartList[1].CacheFilePath
                    }
                }
            };//建立xml模型，用于储存
            return DownloadXmlModelToXElement(model);
        }
        private static XElement DownloadXmlModelToXElement(DownloadXmlModel model)
        {
            var downloadXElement = new XElement("Download",
                new XElement("Name", model.DownloadName),
                new XElement("Title", model.Title),
                new XElement("VideoUrl", model.VideoUrl),
                new XElement("AudioUrl", model.AudioUrl),
                new XElement("CacheFolderPath", model.CacheFolderPath),
                new XElement("Parts",
                new XElement("Part",
                new XElement("OperationGuid", model.PartList[0].OperationGuid.ToString()),
                new XElement("Url", model.PartList[0].Url),
                new XElement("CacheFilePath", model.PartList[0].CacheFilePath)),
                 new XElement("Part",
                new XElement("OperationGuid", model.PartList[1].OperationGuid.ToString()),
                new XElement("Url", model.PartList[1].Url),
                new XElement("CacheFilePath", model.PartList[1].CacheFilePath))));
            return downloadXElement;
        }
        public static string ToStringWithDeclaration(this XDocument doc)//拓展方法，让XDocument转换为字符串时带有编码头
        {
            if (doc == null)
            {
                throw new ArgumentNullException("doc");
            }
            StringBuilder builder = new StringBuilder();
            using (TextWriter writer = new StringWriter(builder))
            {
                doc.Save(writer);
            }
            return builder.ToString();
        }
    }
}
