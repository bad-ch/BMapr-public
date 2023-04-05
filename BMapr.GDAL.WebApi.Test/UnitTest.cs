using System;
using System.Diagnostics;
using System.Xml;
using System.Xml.Linq;
using BMapr.GDAL.WebApi.Models;
using BMapr.GDAL.WebApi.Services;
using Newtonsoft.Json;

namespace BMapr.GDAL.WebApi.Test
{
    [TestClass]
    public class UnitTest
    {
        [TestMethod]
        public void WmtsReadCapabilitiesSwisstopo()
        {
            var xmlPath = Path.GetFullPath("../../../TestFiles/WMTSCapabilities.xml");

            var content = File.ReadAllText(xmlPath);

            var contentNoNs = XmlService.RemoveAllNamespaces(content);

            //File.WriteAllText(Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.xml"), contentNoNs);

            var capabilities = XmlService.DeserializeString<Capabilities>(contentNoNs);

            Assert.IsNotNull(capabilities);
            Assert.IsTrue(capabilities.Contents.TileMatrixSet.Length == 10);
            Assert.IsTrue(capabilities.Contents.Layer.Length > 0);

            //XmlDocument doc = new XmlDocument();
            //doc.LoadXml(File.ReadAllText(xmlPath));

            //string json = JsonConvert.SerializeXmlNode(doc);

            //File.WriteAllText(Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.json"), json);

            //var capabilities = JsonConvert.DeserializeObject<Capabilities>(json);
        }

        [TestMethod]
        public void WmtsReadCapabilitiesBasemapAt()
        {
            var xmlPath = Path.GetFullPath("../../../TestFiles/WMTSCapabilities.xml");
            var content = File.ReadAllText(xmlPath);
            //var contentNoNs = XmlService.RemoveAllNamespaces(content);
            var capabilities = XmlService.DeserializeString<BMapr.GDAL.WebApi.Services.schema_v2.Capabilities>(content);

            Assert.IsNotNull(capabilities);
            Assert.IsTrue(capabilities.Contents.TileMatrixSet.Length > 0);
        }

        [TestMethod]
        public void WfsTransaction()
        {
            var xmlPath = Path.GetFullPath("../../../TestFiles/wfst_2_request.xml");
            var content = File.ReadAllText(xmlPath);
            var contentNoNs = XmlService.RemoveAllNamespaces(content);
            var contentCDATA = XmlService.SetCDATAForInserts(contentNoNs);
            var tr = XmlService.DeserializeString<BMapr.GDAL.WebApi.Services.schemaWfs110.Transaction>(contentCDATA);

            Assert.IsNotNull(tr);
            Assert.IsTrue(tr.Insert.Length == 2);
            Assert.IsTrue(tr.Update.Length == 2);
            Assert.IsTrue(tr.Delete.Length == 1);

            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(tr.Insert[0]);

            //string xpath = "myDataz/listS/sog";
            //var nodes = xmlDoc.SelectNodes(xpath);

            foreach (XmlNode xmlNode in xmlDoc.FirstChild?.ChildNodes)
            {
                Debug.Print($"{xmlNode.LocalName}: {xmlNode.InnerXml}");
            }

        }

        [TestMethod]
        public void WfsTransactionResponse()
        {
            var xmlPath = Path.GetFullPath("../../../TestFiles/wfst_2_response.xml");
            var content = File.ReadAllText(xmlPath);
            var contentNoNs = XmlService.RemoveAllNamespaces(content);
            var tr = XmlService.DeserializeString<BMapr.GDAL.WebApi.Services.schemaWfs110.TransactionResponse>(contentNoNs);

            Assert.IsNotNull(tr);
            Assert.IsTrue(tr.InsertResults.Length == 2);
            Assert.IsTrue(tr.InsertResults[1].FeatureId.fid == "Polygons.1036");
            Assert.IsTrue(tr.TransactionSummary.totalInserted == 1);
            Assert.IsTrue(tr.TransactionSummary.totalUpdated == 2);
            Assert.IsTrue(tr.TransactionSummary.totalDeleted == 3);
        }

        [TestMethod]
        public void WfsModifyGetCapabilities()
        {
            var xmlPath = Path.GetFullPath("../../../TestFiles/WfsGetCapabilities110.xml");
            var content = File.ReadAllText(xmlPath);

            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(content);

            var getCapabilitiy = xmlDoc.SelectSingleNode("/*[local-name() = 'WFS_Capabilities']/*[local-name() = 'OperationsMetadata'][1]/*[local-name() = 'Operation' and @name = 'GetCapabilities'][1]");
            var httpElement = getCapabilitiy.SelectSingleNode("./*[local-name() = 'DCP'][1]/*[local-name() = 'HTTP'][1]"); ///*[local-name() = 'Get'][1]
            var url = httpElement.FirstChild.Attributes.GetNamedItem("xlink:href").Value;
            
            var operationMetadata = xmlDoc.SelectSingleNode("/*[local-name() = 'WFS_Capabilities']/*[local-name() = 'OperationsMetadata'][1]");
            var lastOperationChild = xmlDoc.SelectSingleNode("/*[local-name() = 'WFS_Capabilities']/*[local-name() = 'OperationsMetadata'][1]/*[local-name() = 'Operation'][last()]");

            var post = xmlDoc.CreateElement("Post", lastOperationChild.NamespaceURI);
            post.SetAttribute("type", "http://www.w3.org/1999/xlink", "simple");
            post.SetAttribute("href", "http://www.w3.org/1999/xlink", url);

            var http = xmlDoc.CreateElement("HTTP", lastOperationChild.NamespaceURI);
            http.AppendChild(post);

            var dcp = xmlDoc.CreateElement("DCP", lastOperationChild.NamespaceURI);
            dcp.AppendChild(http);

            var operation = xmlDoc.CreateElement("Operation", lastOperationChild.NamespaceURI);
            operation.SetAttribute("name", "Transaction");

            operation.AppendChild(dcp);

            operationMetadata.InsertAfter(operation, lastOperationChild);

            var operations = xmlDoc.SelectSingleNode("/*[local-name() = 'WFS_Capabilities']/*[local-name() = 'FeatureTypeList'][1]/*[local-name() = 'Operations'][1]");

            if (operations != null)
            {
                var insert = xmlDoc.CreateElement("Operation", operations.NamespaceURI);
                insert.InnerText = "Insert";
                var update = xmlDoc.CreateElement("Operation", operations.NamespaceURI);
                update.InnerText = "Update";
                var delete = xmlDoc.CreateElement("Operation", operations.NamespaceURI);
                delete.InnerText = "Delete";

                operations.AppendChild(insert);
                operations.AppendChild(update);
                operations.AppendChild(delete);
            }

            var test = xmlDoc.OuterXml;
        }

        [TestMethod]
        public void CreateAliasTable()
        {
            var aliasTable = new Dictionary<string, string>();

            aliasTable.Add("Test", "a75b6b64-14ff-4d78-901a-fff31ef8a5a4");
            var content = JsonConvert.SerializeObject(aliasTable);

            File.WriteAllText(Path.Combine(Path.GetTempPath(),$"aliasTable_{Guid.NewGuid()}.xml"), content);
        }

        [TestMethod]
        public void UrlFile()
        {
            var urlFile = new WebRequestParameter()
            {
                Accept = "*",
                AllowAutoRedirect = true,
                BodyContent = "",
                BodyContentType = "",
                Headers = new Dictionary<string, string>(){{"Authorization","Bearer chfwjhgrhgfjhwqrfkwjf"}},
                Method = WebRequestService.MethodType.Get,
                TimeOut = 120000,
                Url = "https://www.webb.ch"
            };

            var content = JsonConvert.SerializeObject(urlFile);

            File.WriteAllText(Path.Combine(Path.GetTempPath(), $"urlFile_{Guid.NewGuid()}.json"), content);
        }

        [TestMethod]
        public void Regex()
        {
            var results = TextService.GetContentBetween("morenonxmldata<tag1>0002</tag1>morenonxmldata<tag2>abc</tag2><tag1>0003</tag1><tag1>0004</tag1>asd", "<tag1>", @"<\/tag1>");

            Assert.IsTrue(results.Count == 3);
            Assert.IsTrue(results[2] == "0004");
        }

        [TestMethod]
        public void Regex2()
        {
            var results = TextService.GetContentBetween("morenonxmldata/*0002*/morenonxmldata<tag2>abc</tag2><tag1>0003</tag1><tag1>0004</tag1>asd", @"\/\*", @"\*\/");

            Assert.IsTrue(results.Count == 1);
            Assert.IsTrue(results[0] == "0002");
        }

        [TestMethod]
        public void RegexMap()
        {
            var mapPath = Path.GetFullPath("../../../TestFiles/bb.map");
            var mapContent = File.ReadAllText(mapPath);

            var result = TextService.GetContentBetweenSimple(mapContent, @"###start-server-config###", @"###end-server-config###");

            Assert.IsTrue(!string.IsNullOrEmpty(result));

            var mapConfig = JsonConvert.DeserializeObject<MapConfig>(result);

            Assert.IsNotNull(mapConfig);
            //Assert.IsTrue(mapConfig.IdSequenceStartValue == 32000);
        }
    }
}