using System.Xml;
using BMapr.GDAL.WebApi.Models;
using BMapr.GDAL.WebApi.Models.Spatial;
using BMapr.GDAL.WebApi.Services.schemaWfs110;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace BMapr.GDAL.WebApi.Services
{
    public class WfsService
    {
        public static ActionResult Transaction(Config config, string project, string contentBody)
        {
            var mapMetadata = MapFileService.GetMapFromProject(project, config);

            if (!mapMetadata.Succesfully || string.IsNullOrEmpty(mapMetadata.Value.Key) || string.IsNullOrEmpty(mapMetadata.Value.FilePath))
            {
                //_logger.LogError("error getting map metadata");
                return new BadRequestResult();
            }

            var resultMapConfig = MapFileService.GetMapConfigFromCache(mapMetadata.Value.Key, mapMetadata.Value.FilePath, config, project);

            if (!resultMapConfig.Succesfully)
            {
                //_logger.LogError("error getting map config");
                return new BadRequestResult();
            }

            var mapConfig = resultMapConfig.Value;

            if (!mapConfig.WFST110enabled)
            {
                return new BadRequestResult();
            }

            if (mapConfig.WFST110debug)
            {
                WfsDebugService.WriteTransactioonRequest(config.DataProject(project).FullName, contentBody);
            }

            var contentNoNs = XmlService.RemoveAllNamespaces(contentBody);
            var contentCDATA = XmlService.SetCDATAForInserts(contentNoNs);
            var transaction = XmlService.DeserializeString<BMapr.GDAL.WebApi.Services.schemaWfs110.Transaction>(contentCDATA);

            if (transaction == null)
            {
                return new StatusCodeResult(500);
            }

            long featureIdCount = 0;

            FeatureServiceMsSql insertFeatureServiceMsSql = null;
            FeatureServiceMsSql updateFeatureServiceMsSql = null;
            FeatureServiceMsSql deleteFeatureServiceMsSql = null;

            MapLayerConfig layerConfig = null;
            var layerName = string.Empty;

            var featureInsertCount = 0;
            var featureListInsert = new FeatureList()
            {
                IdFieldName = string.Empty,
                Bodies = new List<FeatureBody>()
            };

            if (transaction.Insert != null)
            {
                var nextValFilePath = string.Empty;

                foreach (var insert in transaction.Insert)
                {
                    var featureBody = new FeatureBody();

                    XmlDocument xmlDoc = new XmlDocument();

                    var insertNoNs = insert.Replace("gml:", "");
                    xmlDoc.LoadXml(insertNoNs);

                    layerName = xmlDoc.FirstChild?.LocalName;
                    layerConfig = mapConfig.GetLayerConfig(layerName);
                    var tableName = string.IsNullOrEmpty(layerConfig.TableName) ? layerName : layerConfig.TableName;

                    if (featureInsertCount == 0)
                    {
                        //ini classes and config on the first item

                        featureListInsert.IdFieldName = layerConfig.Id;

                        if (layerConfig.WFSTuseMsSqlServerFeatureService)
                        {
                            insertFeatureServiceMsSql = new FeatureServiceMsSql(layerConfig.Connection, tableName, layerConfig.WFSTMsSqlServerGeography, mapConfig.WFST110debug, layerConfig.IdType);
                        }

                        if (layerConfig.IdSquenceManual)
                        {
                            var mapFileFolder = (new FileInfo(mapMetadata.Value.FilePath)).Directory?.FullName;
                            nextValFilePath = Path.Combine(mapFileFolder, $"NextVal_{layerName}");

                            if (File.Exists(nextValFilePath))
                            {
                                var content = File.ReadAllText(nextValFilePath);
                                featureIdCount = System.Convert.ToInt64(content);
                            }
                            else
                            {
                                featureIdCount = layerConfig.IdSequenceStartValue;
                            }
                        }
                    }

                    object fid;

                    if (layerConfig.IdType == "Number")
                    {
                        featureBody.Id = featureIdCount;
                        fid = featureIdCount;
                    }
                    else if (layerConfig.IdType == "Incr" || layerConfig.IdType == "AutoGuid" || layerConfig.IdType == "AutoInt" || layerConfig.IdType == "AutoBigInt")
                    {
                        featureBody.Id = null; // set by db
                        fid = null;
                    }
                    else if (layerConfig.IdType == "Guid")
                    {
                        var guid = Guid.NewGuid().ToString();
                        featureBody.Id = guid;
                        fid = guid;
                    }
                    else
                    {
                        featureBody.Id = featureIdCount.ToString();
                        fid = featureIdCount.ToString();
                    }

                    foreach (XmlNode xmlNode in xmlDoc.FirstChild?.ChildNodes)
                    {
                        if (xmlNode.LocalName.ToLower() == "geometry" || xmlNode.LocalName.ToLower() == "shape" || xmlNode.LocalName.ToLower() == "msgeometry" || xmlNode.LocalName.ToLower() == "geom" || xmlNode.LocalName.ToLower() == layerConfig.GeometryFieldName.ToLower())
                        {
                            var geometryResult = GeometryService.GetOgrGeometryFromGml(xmlNode.InnerXml);
                            var geometryWkt = GeometryService.GetStringFromOgrGeometry(geometryResult.Value, "wkt", true);
                            featureBody.Geometry = geometryWkt;
                            featureBody.Epsg = layerConfig.EPSG;
                            continue;
                        }

                        featureBody.Properties.Add(xmlNode.LocalName, xmlNode.InnerXml);
                    }

                    featureBody.Properties[layerConfig.Id] = fid;

                    featureListInsert.Bodies.Add(featureBody);
                    featureInsertCount++;
                    featureIdCount++;
                }

                if (layerConfig.WFSTuseMsSqlServerFeatureService)
                {
                    insertFeatureServiceMsSql?.Insert(featureListInsert);

                    if (layerConfig != null && layerConfig.WFSTuseMsSqlServerFeatureService)
                    {
                        if (mapConfig.WFST110debug && insertFeatureServiceMsSql != null)
                        {
                            WfsDebugService.WriteSqlTransaction(config.DataProject(project).FullName, insertFeatureServiceMsSql.SqlLog.ToString());
                        }

                        insertFeatureServiceMsSql?.Dispose();
                    }
                }
                else
                {
                    FeatureService.Insert(layerConfig.Connection, layerName, featureListInsert);
                }

                if (layerConfig.IdSquenceManual)
                {
                    File.WriteAllText(nextValFilePath, featureIdCount.ToString());
                }
            }

            var featureUpdateCount = 0;

            if (transaction.Update != null)
            {
                layerName = RemoveNamespace(transaction.Update.First().typeName);
                layerConfig = mapConfig.GetLayerConfig(layerName);
                var tableName = string.IsNullOrEmpty(layerConfig.TableName) ? layerName : layerConfig.TableName;


                if (layerConfig.WFSTuseMsSqlServerFeatureService)
                {
                    updateFeatureServiceMsSql = new FeatureServiceMsSql(layerConfig.Connection, tableName, layerConfig.WFSTMsSqlServerGeography, mapConfig.WFST110debug, layerConfig.IdType);
                }

                var featureList = new FeatureList()
                {
                    IdFieldName = layerConfig.Id,
                    Bodies = new List<FeatureBody>()
                };

                foreach (var update in transaction.Update)
                {
                    var featureBody = new FeatureBody();

                    object id;

                    if (layerConfig.IdType == "Number" || layerConfig.IdType == "Incr")
                    {
                        id = System.Convert.ToInt64(update.Filter.FeatureId.fid.Split(".")[1]);
                    }
                    else
                    {
                        id = update.Filter.FeatureId.fid.Split(".")[1];
                    }

                    featureBody.Id = id;

                    foreach (var property in update.Property)
                    {
                        var name = RemoveNamespace(property.Name);

                        if (name.ToLower() == "geometry" || name.ToLower() == "shape" || name.ToLower() == "msgeometry" || name.ToLower() == "geom" || name.ToLower() == layerConfig.GeometryFieldName.ToLower())
                        {
                            var geometryResult = GeometryService.GetOgrGeometryFromGml(property.Value.ToString());
                            var geometryWkt = GeometryService.GetStringFromOgrGeometry(geometryResult.Value, "wkt", true);
                            featureBody.Geometry = geometryWkt;
                            featureBody.Epsg = layerConfig.EPSG;
                            continue;
                        }

                        featureBody.Properties.Add(name, property.Value);
                    }

                    featureList.Bodies.Add(featureBody);
                    featureUpdateCount++;
                }

                if (layerConfig.WFSTuseMsSqlServerFeatureService)
                {
                    updateFeatureServiceMsSql?.Update(featureList);

                    if (layerConfig != null && layerConfig.WFSTuseMsSqlServerFeatureService)
                    {
                        if (mapConfig.WFST110debug && updateFeatureServiceMsSql != null)
                        {
                            WfsDebugService.WriteSqlTransaction(config.DataProject(project).FullName, updateFeatureServiceMsSql.SqlLog.ToString());
                        }

                        updateFeatureServiceMsSql?.Dispose();
                    }
                }
                else
                {
                    FeatureService.Update(layerConfig.Connection,layerName, featureList);
                }
            }

            var featureDeleteCount = 0;

            if (transaction.Delete != null)
            {
                layerName = RemoveNamespace(transaction.Delete.First().typeName);
                layerConfig = mapConfig.GetLayerConfig(layerName);
                var tableName = string.IsNullOrEmpty(layerConfig.TableName) ? layerName : layerConfig.TableName;

                if (layerConfig.WFSTuseMsSqlServerFeatureService)
                {
                    deleteFeatureServiceMsSql = new FeatureServiceMsSql(layerConfig.Connection, tableName, layerConfig.WFSTMsSqlServerGeography, mapConfig.WFST110debug, layerConfig.IdType);
                }

                var featureList = new FeatureList()
                {
                    IdFieldName = layerConfig.Id,
                    Bodies = new List<FeatureBody>()
                };

                foreach (var delete in transaction.Delete)
                {
                    foreach (var filter in delete.Filter)
                    {
                        object id;

                        if (layerConfig.IdType == "Number" || layerConfig.IdType == "Incr")
                        {
                            id = System.Convert.ToInt64(filter.fid.Split(".")[1]);
                        }
                        else
                        {
                            id = filter.fid.Split(".")[1];
                        }

                        featureList.Bodies.Add(new FeatureBody() { Id = id });
                        featureDeleteCount++;
                    }
                }

                if (layerConfig.WFSTuseMsSqlServerFeatureService)
                {
                    deleteFeatureServiceMsSql?.Delete(featureList);
                    if (layerConfig != null && layerConfig.WFSTuseMsSqlServerFeatureService)
                    {
                        if (mapConfig.WFST110debug && deleteFeatureServiceMsSql != null)
                        {
                            WfsDebugService.WriteSqlTransaction(config.DataProject(project).FullName, deleteFeatureServiceMsSql.SqlLog.ToString());
                        }

                        deleteFeatureServiceMsSql?.Dispose();
                    }
                }
                else
                {
                    FeatureService.Delete(layerConfig.Connection, layerName, featureList);
                }
            }

            var transactionResponse = new TransactionResponse();

            transactionResponse.version = "1.1.0";
            transactionResponse.TransactionSummary = new TransactionResponseTransactionSummary();
            transactionResponse.TransactionSummary.totalDeleted = featureDeleteCount;
            transactionResponse.TransactionSummary.totalUpdated = featureUpdateCount;
            transactionResponse.TransactionSummary.totalInserted = featureInsertCount;

            transactionResponse.TransactionResults = new TransactionResponseTransactionResults();

            if (featureListInsert.Bodies.Any())
            {
                var insertResult = new List<TransactionResponseFeature>();
                var prefix = layerName;

                foreach (var feature in featureListInsert.Bodies)
                {
                    var transFeature = new TransactionResponseFeature();
                    transFeature.FeatureId = new FeatureId() { fid = $"{prefix}.{feature.Id}" };
                    insertResult.Add(transFeature);
                }
                transactionResponse.InsertResults = insertResult.ToArray();
            }
            else
            {
                var transFeature = new TransactionResponseFeature();
                transFeature.FeatureId = new FeatureId() { fid = "none" };
                transactionResponse.InsertResults = (new List<TransactionResponseFeature>() { transFeature }).ToArray();
            }

            var response = XmlService.SerializeString(transactionResponse);

            if (mapConfig.WFST110debug)
            {
                WfsDebugService.WriteTransactionResponse(config.DataProject(project).FullName, response);
            }

            return new ContentResult(){Content = response, ContentType = "text/xml", StatusCode = 200};
        }

        public static byte[] AddTransactionToGetCapabilities(byte[] byteContent)
        {
            var xmlContent = System.Text.Encoding.UTF8.GetString(byteContent);

            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(xmlContent);

            var getCapabilitiy = xmlDoc.SelectSingleNode("/*[local-name() = 'WFS_Capabilities']/*[local-name() = 'OperationsMetadata'][1]/*[local-name() = 'Operation' and @name = 'GetCapabilities'][1]");
            var httpElement = getCapabilitiy.SelectSingleNode("./*[local-name() = 'DCP'][1]/*[local-name() = 'HTTP'][1]");
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

            var modifiedContent = xmlDoc.OuterXml;

            return System.Text.Encoding.UTF8.GetBytes(modifiedContent);
        }

        private static string RemoveNamespace(string value)
        {
            if (value.Count(x => x == ':') == 1)
            {
                return value.Split(':')[1];
            }
            
            if (value.Count(x => x == ':') > 1)
            {
                throw new Exception("More than one colons not allowed");
            }

            return value;
        }
    }
}
