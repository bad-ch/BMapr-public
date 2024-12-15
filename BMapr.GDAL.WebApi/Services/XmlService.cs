using Newtonsoft.Json.Linq;
using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace BMapr.GDAL.WebApi.Services
{
    public class XmlService
    {
        public static void Serialize<T>(T myObj, string filename)
        {
            //todo error handling

            var mySerializer = new XmlSerializer(typeof(T));
            TextWriter myWriter = new StreamWriter(filename);
            mySerializer.Serialize(myWriter, myObj);
            myWriter.Close();
        }

        public static string SerializeString<T>(T value)
        {
            if (value == null)
            {
                return string.Empty;
            }
            try
            {
                XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
                ns.Add("wfs", "http://www.opengis.net/wfs");

                var xmlSerializer = new XmlSerializer(typeof(T));
                var stringWriter = new Utf8StringWriter();
                using (var writer = XmlWriter.Create(stringWriter))
                {
                    xmlSerializer.Serialize(writer, value, ns);
                    return stringWriter.ToString();
                }
            }
            catch (Exception ex)
            {
                throw new Exception("An error occurred", ex);
            }
        }

        public static T Deserialize<T>(string filename)
        {
            if (!File.Exists(filename))
            {
                return default(T);
            }

            try
            {
                var mySerializer = new XmlSerializer(typeof(T));
                TextReader myReader = new StreamReader(filename);
                var newObject = (T)mySerializer.Deserialize(myReader);
                myReader.Close();
                return newObject;
            }
            catch (Exception ex)
            {
                return default(T);
            }
        }

        public static T DeserializeString<T>(string content)
        {
            var serializer = new XmlSerializer(typeof(T));
            T result;

            using (TextReader reader = new StringReader(content))
            {
                result = (T)serializer.Deserialize(reader);
            }

            return result;
        }

        public static string RemoveAllNamespaces(string xmlDocument)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xmlDocument);
            XmlNode newNode = doc.DocumentElement;
            var result = RemoveAllNamespaces2(newNode);

            return result.OuterXml;
        }

        public static string RemoveAllNamespacesWMTS(string xmlDocument)
        {
            XElement xmlDocumentWithoutNs = RemoveAllNamespaces(XElement.Parse(xmlDocument));
            return xmlDocumentWithoutNs.ToString();
        }

        public static string SetCDATAForInserts(string xmlDocument)
        {
            xmlDocument = xmlDocument.Replace("<Insert>", "<Insert><![CDATA[");
            xmlDocument = xmlDocument.Replace("</Insert>", "]]></Insert>");
            return xmlDocument;
        }

        //Core recursion function
        private static XElement RemoveAllNamespaces(XElement xmlDocument)
        {
            if (!xmlDocument.HasElements)
            {
                XElement xElement = new XElement(xmlDocument.Name.LocalName);
                xElement.Value = xmlDocument.Value;

                foreach (XAttribute attribute in xmlDocument.Attributes())
                {
                    XAttribute attributeNoNs = new XAttribute(attribute.Name.LocalName, attribute.Value);
                    xElement.Add(attributeNoNs);
                }

                return xElement;
            }
            return new XElement(xmlDocument.Name.LocalName, xmlDocument.Elements().Select(el => RemoveAllNamespaces(el)));
        }

        public static XmlNode RemoveAllNamespaces2(XmlNode documentElement)
        {
            var xmlnsPattern = "\\s+xmlns\\s*(:\\w)?\\s*=\\s*\\\"(?<url>[^\\\"]*)\\\"";
            var outerXml = documentElement.OuterXml;
            var matchCol = Regex.Matches(outerXml, xmlnsPattern);
            foreach (var match in matchCol)
                outerXml = outerXml.Replace(match.ToString(), "");

            var result = new XmlDocument();
            result.LoadXml(outerXml);

            return result;
        }

        public XmlNode Strip(XmlNode documentElement)
        {
            var namespaceManager = new XmlNamespaceManager(documentElement.OwnerDocument.NameTable);
            foreach (var nspace in namespaceManager.GetNamespacesInScope(XmlNamespaceScope.All))
            {
                namespaceManager.RemoveNamespace(nspace.Key, nspace.Value);
            }

            return documentElement;
        }
    }

    public class Utf8StringWriter : StringWriter
    {
        public override Encoding Encoding
        {
            get { return Encoding.UTF8; }
        }
    }
}
