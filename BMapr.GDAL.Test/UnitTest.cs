using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Xsl;
using BMapr.GDAL;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using OSGeo.GDAL;
using OSGeo.MapServer;
using OSGeo.OGR;
using OSGeo.OSR;

namespace GdalTest
{
    [TestClass]
    public class GdalTesting
    {
        public GdalTesting ()
        {
            GdalConfiguration.ExternalPath = Path.GetFullPath(@"..\..\..\BMapr.GDAL.Native\output\");
        }

        [TestMethod]
        public void GdalTest()
        {
            var plattform = IntPtr.Size == 4 ? "x86" : "x64";
            Assert.AreEqual("x64", plattform);

            GdalConfiguration.ConfigureGdal();
            GdalConfiguration.ConfigureOgr();
            OSGeo.OGR.Ogr.GetDriverCount();

            Console.WriteLine(string.Join(", ", GdalConfiguration.GetDriversGdal()));
            Console.WriteLine(string.Join(", ", GdalConfiguration.GetDriversOgr()));

            var version = GdalConfiguration.GetVersionGdal();
        }

        [TestMethod]
        public void OpenTif()
        {
            GdalConfiguration.ConfigureGdal();

            var dataset = Gdal.Open(Path.GetFullPath("../../TestFiles/test2.tif"), Access.GA_ReadOnly);

            Assert.IsNotNull(dataset);
        }

        [TestMethod]
        public void OpenShape()
        {
            GdalConfiguration.ConfigureOgr();

            var datasource = Ogr.Open(Path.GetFullPath("../../TestFiles/test3.shp"), 0);

            Assert.IsNotNull(datasource);

            var layer = datasource.GetLayerByIndex(0);

            Assert.IsNotNull(layer);
        }

        [TestMethod]
        public void UseSpatialRef()
        {
            GdalConfiguration.ConfigureOgr();

            var spatialReference = new OSGeo.OSR.SpatialReference("");

            string wkt = "";
            spatialReference.ImportFromEPSG(2056);
            spatialReference.ExportToWkt(out wkt, new string[]{});

            Assert.IsFalse(string.IsNullOrEmpty(wkt));
            Assert.IsTrue(wkt.Contains("LV95"));
        }

        [TestMethod]
        public void UseMapserver()
        {
            GdalConfiguration.ConfigureGdal();
            GdalConfiguration.ConfigureOgr();

            var map = new mapObj(Path.GetFullPath("../../TestFiles/test1.map"));

            outputFormatObj outputFormat = null;

            outputFormat = new outputFormatObj("AGG/PNG", "png");
            outputFormat.transparent = 1;
            outputFormat.imagemode = Convert.ToInt32(OSGeo.MapServer.MS_IMAGEMODE.MS_IMAGEMODE_RGBA);

            map.width = 517;
            map.height = 327;
            map.setExtent(-12000.000 ,241000.000, - 11000.000 ,242000.000);

            map.setOutputFormat(outputFormat);

            imageObj imageObj = null;

            try
            {
                imageObj = map.draw();
            }
            catch (Exception ex)
            {
                Assert.Fail($"mapserver does not draw, {ex.Message}");
            }

            Assert.IsTrue(imageObj.getBytes().Length > 0);

            var memoryStream = new MemoryStream(imageObj.getBytes());

            var file = new FileStream(Path.Combine(Path.GetTempPath(), String.Format("MapserverExportImage_{0}.png", Guid.NewGuid())), FileMode.Create, FileAccess.Write); //for visual control
            memoryStream.WriteTo(file);
            file.Close();
            memoryStream.Close();
        }

        [TestMethod]
        public void UseMapserverWfs()
        {
            GdalConfiguration.ConfigureGdal();
            GdalConfiguration.ConfigureOgr();

            var map = new mapObj(Path.GetFullPath("../../TestFiles/wfs.map"));

            outputFormatObj outputFormat = null;

            outputFormat = new outputFormatObj("AGG/PNG", "png");
            outputFormat.transparent = 1;
            outputFormat.imagemode = Convert.ToInt32(OSGeo.MapServer.MS_IMAGEMODE.MS_IMAGEMODE_RGBA);

            map.width = 1600;
            map.height = 1200;
            map.setExtent(-64789, 241896 , -40754, 256245);

            map.setOutputFormat(outputFormat);

            imageObj imageObj = null;

            try
            {
                imageObj = map.draw();
            }
            catch (Exception ex)
            {
                Assert.Fail($"mapserver does not draw, {ex.Message}");
            }

            Assert.IsTrue(imageObj.getBytes().Length > 0);

            var memoryStream = new MemoryStream(imageObj.getBytes());

            var filePath = Path.Combine(Path.GetTempPath(), String.Format("MapserverExportImageWFS_{0}.png", Guid.NewGuid()));

            Console.Write(filePath);

            var file = new FileStream(filePath, FileMode.Create, FileAccess.Write); //for visual control
            memoryStream.WriteTo(file);
            file.Close();
            memoryStream.Close();
        }

        [TestMethod]
        public void GetVersionInfo()
        {
            GdalConfiguration.ConfigureGdal();
            GdalConfiguration.ConfigureOgr();

            var gdalInfo = GdalConfiguration.GetVersionGdal();
            var ogrInfo = GdalConfiguration.GetVersionOgr();
            var msInfo = GdalConfiguration.GetVersionMapserver();
            var msOptions = GdalConfiguration.GetCompiledOptionsMapserver();

            Assert.IsFalse(string.IsNullOrEmpty(gdalInfo));
            Assert.IsFalse(string.IsNullOrEmpty(ogrInfo));
            Assert.IsFalse(string.IsNullOrEmpty(msInfo));
            Assert.IsTrue(msOptions.Count > 0);
        }

        [TestMethod]
        public void SaveOverviews()
        {
            GdalConfiguration.ConfigureGdal();

            var dataset = Gdal.Open(Path.GetFullPath("../../TestFiles/test4_overviewInternal.tif"), Access.GA_ReadOnly);

            //image has 6 overviews
            var fineOverview = dataset.GetRasterBand(1).GetOverview(0).GetDataset();
            var notfineOverview = dataset.GetRasterBand(1).GetOverview(5).GetDataset();

            String[] options = null;
            Gdal.GetDriverByName("PNG").CreateCopy(Path.Combine(Path.GetTempPath(), String.Format("ExportOverviewFine_{0}.png", Guid.NewGuid())), fineOverview, 0, options, null, null);
            Gdal.GetDriverByName("PNG").CreateCopy(Path.Combine(Path.GetTempPath(), String.Format("ExportOverviewNotFine_{0}.png", Guid.NewGuid())), notfineOverview, 0, options, null, null);
            Gdal.GetDriverByName("PNG").CreateCopy(Path.Combine(Path.GetTempPath(), String.Format("ExportOriginal_{0}.png", Guid.NewGuid())), dataset, 0, options, null, null);
        }

        [TestMethod]
        public void ImportKML()
        {
            GdalConfiguration.ConfigureOgr();

            //var filepath = Path.GetFullPath("../../TestFiles/mayrhofenahorn.kml");
            //var driver = Ogr.GetDriverByName("KML");
            //var filepath = Path.GetFullPath("../../TestFiles/test.dxf");
            //var driver = Ogr.GetDriverByName("DXF");
            var filepath = Path.GetFullPath("../../TestFiles/test3.SHP");
            var driver = Ogr.GetDriverByName("Esri Shapefile");

            var datasource = driver.Open(filepath, 0);

            var layerCount = datasource.GetLayerCount();
            var epsgSource = 31255;
            var epsgTarget = 4326;

            for (int layerIndex = 0; layerIndex < layerCount; layerIndex++)
            {
                var layer = datasource.GetLayerByIndex(layerIndex);
                var layername = layer.GetName();

                var featureCount = layer.GetFeatureCount(1);
                var features = new List<object>();

                for (int featureIndex = 0; featureIndex < featureCount; featureIndex++)
                {
                    string geometryWkt;
                    string geometryWktTarget;

                    var feature = layer.GetFeature(featureIndex);
                    var definition = feature.GetDefnRef();
                    var geometry = feature.GetGeometryRef();

                    var envelope = new Envelope();
                    geometry.GetEnvelope(envelope);

                    //todo check for CRS in correct range, otherwise break procedure or continue ?

                    var result = geometry.ExportToWkt(out geometryWkt);
                    var geomType = geometry.GetGeometryType().ToString();

                    var style = feature.GetStyleString();
                    var fieldCount = feature.GetFieldCount();
                    var fields = new List<object>();

                    var spatialRefSource = new OSGeo.OSR.SpatialReference("");
                    spatialRefSource.ImportFromEPSG(epsgSource);
                    spatialRefSource.SetAxisMappingStrategy(AxisMappingStrategy.OAMS_TRADITIONAL_GIS_ORDER); //important change !!

                    var spatialRefTarget = new OSGeo.OSR.SpatialReference("");
                    spatialRefTarget.ImportFromEPSG(epsgTarget);
                    spatialRefTarget.SetAxisMappingStrategy(AxisMappingStrategy.OAMS_TRADITIONAL_GIS_ORDER); //important change !!

                    var transformation = Osr.CreateCoordinateTransformation(spatialRefSource, spatialRefTarget, new CoordinateTransformationOptions(){});

                    var status = geometry.Transform(transformation);
                    var resultTarget = geometry.ExportToWkt(out geometryWktTarget);

                    fields.Add(new { field = "geometryType", type = "geometryType", value = geomType });

                    for (int fieldIndex = 0; fieldIndex < fieldCount; fieldIndex++)
                    {
                        var fielddef = definition.GetFieldDefn(fieldIndex);

                        var type = (int)fielddef.GetFieldType();
                        var typeStr = fielddef.GetTypeName();
                        var name = fielddef.GetName();

                        switch (type)
                        {
                            case 4:     //string
                            case 6:     //widestring

                                fields.Add(new { field = name, type = typeStr, value = feature.GetFieldAsString(fieldIndex) });
                                break;

                            case 5:     //stringlist
                            case 7:     //wide stringlist

                                fields.Add(new { field = name, type = typeStr, value = feature.GetFieldAsStringList(fieldIndex) });
                                break;

                            case 0:     //int

                                fields.Add(new { field = name, type = typeStr, value = feature.GetFieldAsInteger(fieldIndex) });
                                break;

                            case 2:     //real

                                fields.Add(new { field = name, type = typeStr, value = feature.GetFieldAsDouble(fieldIndex) });
                                break;

                            case 9:      //date
                            case 10:     //time
                            case 11:     //datetime

                                int year; int month; int day; int hour; int minute; float second; int tz;

                                feature.GetFieldAsDateTime(fieldIndex, out year, out month, out day, out hour, out minute, out second, out tz);
                                fields.Add(new { field = name, type = typeStr, value = new DateTime(year, month, day, hour, minute, (int)Math.Round(second)) });
                                break;

                            case 12:    //int64

                                fields.Add(new { field = name, type = typeStr, value = feature.GetFieldAsInteger64(fieldIndex) });
                                break;

                            default:

                                fields.Add(new { field = name, type = typeStr, value = feature.GetFieldAsString(fieldIndex) });
                                break;

                        }
                    }

                    features.Add(new
                    {
                        geometryWktTarget = geometryWktTarget,
                        geometryWkt = geometryWkt,
                        fields = fields
                    });
                }

                var featuresOutput = JsonConvert.SerializeObject(features);

            }            
        }

        [TestMethod]
        public void ProcessRequestGet_GetCapabilities()
        {
            GdalConfiguration.ConfigureGdal();
            GdalConfiguration.ConfigureOgr();

            OWSRequest req = new OWSRequest();

            req.setParameter("SERVICE", "WFS");
            req.setParameter("VERSION", "1.0.0");
            req.setParameter("REQUEST", "GetCapabilities");

            var map = new mapObj(Path.GetFullPath(@"..\..\Testfiles\wfs.map"));

            mapscript.msIO_installStdoutToBuffer();

            int result = map.OWSDispatch(req);

            Assert.IsTrue(result == 0);

            var header = mapscript.msIO_stripStdoutBufferContentType();
            var content = mapscript.msIO_getStdoutBufferBytes();

            mapscript.msIO_resetHandlers();

            Assert.IsTrue(header == "text/xml; charset=UTF-8");
            Assert.IsTrue(content.Length > 0);
        }

        [TestMethod]
        public void ProcessRequestPost_GetCapabilities()
        {
            GdalConfiguration.ConfigureGdal();
            GdalConfiguration.ConfigureOgr();

            OWSRequest req = new OWSRequest();

            req.type = MS_REQUEST_TYPE.MS_POST_REQUEST;
            req.contenttype = "text/xml";
            req.postrequest = @"<?xml version=""1.0"" encoding=""UTF-8""?><GetCapabilities service=""WFS"" version=""2.0.0"" xmlns=""http://www.opengis.net/wfs/2.0"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:schemaLocation=""http://www.opengis.net/wfs/2.0 http://schemas.opengis.net/wfs/2.0/wfs.xsd""/>";

            var map = new mapObj(Path.GetFullPath(@"..\..\Testfiles\wfs.map"));

            mapscript.msIO_installStdoutToBuffer();

            int result = map.OWSDispatch(req);

            Assert.IsTrue(result == 0);

            var header = mapscript.msIO_stripStdoutBufferContentType();
            var content = mapscript.msIO_getStdoutBufferBytes();

            mapscript.msIO_resetHandlers();

            Assert.IsTrue(header == "text/xml; charset=UTF-8");
            Assert.IsTrue(content.Length > 0);
        }

        [TestMethod]
        public void LoadXmlMapFile()
        {
            GdalConfiguration.ConfigureGdal();
            GdalConfiguration.ConfigureOgr();

            //var map = new mapObj(Path.GetFullPath(@"..\..\Testfiles\mapfile-sections.xml"));

            var xslt = File.ReadAllText(Path.GetFullPath(@"..\..\Testfiles\mapfile.xsl"));
            var mapFileXml = XDocument.Load(Path.GetFullPath(@"..\..\Testfiles\mapfile-sections.xml"));

            using (var stringReader = new StringReader(xslt))
            {
                using (XmlReader xsltReader = XmlReader.Create(stringReader))
                {
                    var transformer = new XslCompiledTransform();
                    transformer.Load(xsltReader);
                    using (XmlReader oldDocumentReader = mapFileXml.CreateReader())
                    {
                        XmlWriterSettings settings = new XmlWriterSettings();
                        settings.ConformanceLevel = ConformanceLevel.Fragment;
                        settings.CheckCharacters = false;

                        using (TextWriter newDocumentWriter = File.CreateText(Path.Combine(Path.GetTempPath(),$"{Guid.NewGuid()}.map")))
                        {
                            transformer.Transform(oldDocumentReader, new XsltArgumentList(), newDocumentWriter);
                        }
                    }
                }
            }
        }
    }
}
