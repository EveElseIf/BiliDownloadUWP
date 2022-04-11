using BiliDownload.Components;
using BiliDownload.Models.Xml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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
            return File.ReadAllText(XmlPath);
        }
        public static void WriteXml(string content)
        {
            DeleteCurrentDownloads();
            File.WriteAllText(XmlPath, content);
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
            var serializer = new XmlSerializer(typeof(T));
            return (T)serializer.Deserialize(new StringReader(xml));
        }
        public static void DeleteCurrentDownloads()
        {
            File.Delete(XmlPath);
        }
        public static void StoreCurrentDownloads()
        {
            var list = new List<BiliDashDownload>();
            foreach (var item in DownloadPage.Current.activeDownloadList)
            {
                list.Add(item as BiliDashDownload);
            }
            if (list?.Count < 1) return;//没有内容就不用储存了
            var doc = CreateDownloadXDocument(list).ToStringWithDeclaration();
            WriteXml(doc);
        }
        public static XDocument CreateDownloadXDocument(List<BiliDashDownload> models)//创建Xml文档
        {
            var list = new List<XElement>();
            foreach (var model in models)
            {
                list.Add(CreateSingleDownloadXElement(model));
            }
            var xdoc = new XDocument(new XDeclaration("1.0", "utf-8", "yes"), new XElement("Downloads", list));
            return xdoc;
        }
        public static XElement CreateSingleDownloadXElement(BiliDashDownload viewModel)//单个视频创建XElement
        {
            var model = new DownloadXmlModel()
            {
                DownloadName = viewModel.DownloadName,
                Title = viewModel.Title,
                Bv = viewModel.Bv,
                Cid = viewModel.Cid,
                Quality = viewModel.Quality,
                CacheFolderPath = viewModel.CacheFolder.Path,
                PartList = new DownloadPartXmlModel[]
                {
                    new DownloadPartXmlModel()
                    {
                        TaskGuid = viewModel.PartList[0].TaskGuid,
                        RestoreModelJson = viewModel.PartList[0].TaskRestoreModelJson
                    },
                    new DownloadPartXmlModel()
                    {
                        TaskGuid = viewModel.PartList[1].TaskGuid,
                        RestoreModelJson = viewModel.PartList[1].TaskRestoreModelJson
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
                new XElement("Bv", model.Bv),
                new XElement("Cid", model.Cid),
                new XElement("Quality", model.Quality),
                new XElement("CacheFolderPath", model.CacheFolderPath),
                new XElement("Parts",
                new XElement("Part",
                new XElement("TaskGuid", model.PartList[0].TaskGuid.ToString()),
                new XElement("RestoreModelJson", model.PartList[0].RestoreModelJson)),
                new XElement("Part",
                new XElement("TaskGuid", model.PartList[1].TaskGuid.ToString()),
                new XElement("RestoreModelJson", model.PartList[1].RestoreModelJson))));
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
