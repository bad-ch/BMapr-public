using System.Xml;
using BMapr.GDAL.WebApi.Models;
using BMapr.GDAL.WebApi.Models.Spatial;
using BMapr.GDAL.WebApi.Services;
using Newtonsoft.Json;

namespace BMapr.GDAL.WebApi.Test
{
    [TestClass]
    public class FeatureServiceUnitTest
    {
        [TestMethod]
        public void UpdateFeature()
        {
            var filePath = Path.GetFullPath("../../../TestFiles/02_DKM500_STRASSE_ANNO.shp");

            //ID FEATUREID, Name TEXTSTRNG
            var featureList = new FeatureList()
            {
                IdFieldName = "FEATUREID",
                Bodies = new List<FeatureBody>()
                {
                    new FeatureBody()
                    {
                        Id = 50534,
                        Properties = new Dictionary<string, object>(){{ "TEXTSTRING", Guid.NewGuid().ToString() } }
                    },
                    new FeatureBody()
                    {
                        Id = 79283,
                        Properties = new Dictionary<string, object>(){{ "TEXTSTRING", $"** {Guid.NewGuid()}" } }
                    }
                }
            };

            FeatureService.Update(filePath, "02_DKM500_STRASSE_ANNO", featureList);
        }

        [TestMethod]
        public void CreateFeature()
        {
            var filePath = Path.GetFullPath("../../../TestFiles/02_DKM500_STRASSE_ANNO.shp");

            //ID FEATUREID, Name TEXTSTRNG
            var featureList = new FeatureList()
            {
                IdFieldName = "FEATUREID",
                Bodies = new List<FeatureBody>()
                {
                    new FeatureBody()
                    {
                        Id = 5,
                        Properties = new Dictionary<string, object>(){{ "TEXTSTRING", Guid.NewGuid().ToString() }, {"FEATUREID", 5} },
                        Geometry = "LINESTRING(2600520 1150191, 2715764 1186190)"
                    },
                    new FeatureBody()
                    {
                        Id= 6,
                        Properties = new Dictionary<string, object>(){{ "TEXTSTRING", $"** {Guid.NewGuid()}" }, {"FEATUREID", 6} },
                        Geometry = "LINESTRING(2511651 1138667,2626895 1174666)"
                    }
                }
            };

            FeatureService.Insert(filePath, "02_DKM500_STRASSE_ANNO", featureList);
        }


        [TestMethod]
        public void DeleteFeature()
        {
            var filePath = Path.GetFullPath("../../../TestFiles/02_DKM500_STRASSE_ANNO.shp");

            //ID FEATUREID, Name TEXTSTRNG
            var featureList = new FeatureList()
            {
                IdFieldName = "FEATUREID",
                Bodies = new List<FeatureBody>()
                {
                    new FeatureBody()
                    {
                        Id = 5,
                        Properties = new Dictionary<string, object>(){{ "TEXTSTRING", Guid.NewGuid().ToString() } },
                        Geometry = "LINESTRING(2600520 1150191, 2715764 1186190)"
                    },
                    new FeatureBody()
                    {
                        Id= 6,
                        Properties = new Dictionary<string, object>(){{ "TEXTSTRING", $"** {Guid.NewGuid()}" } },
                        Geometry = "LINESTRING(2511651 1138667,2626895 1174666)"
                    }
                }
            };

            FeatureService.Delete(filePath, "02_DKM500_STRASSE_ANNO", featureList);
        }

        [TestMethod]
        public void CreateFeatureSqlServer()
        {
            //ID FEATUREID, Name TEXTSTRNG
            var featureList = new FeatureList()
            {
                IdFieldName = "fid",
                Bodies = new List<FeatureBody>()
                {
                    new FeatureBody()
                    {
                        Id = 5,
                        Properties = new Dictionary<string, object>(){{ "objektart", Guid.NewGuid().ToString() }, {"id", 5} },
                        Geometry = "POLYGON((2600520 1150191, 2715764 1186190,2511651 1138667))"
                    },
                    new FeatureBody()
                    {
                        Id= 6,
                        Properties = new Dictionary<string, object>(){{ "objektart", $"** {Guid.NewGuid()}" }, {"id", 6} },
                        Geometry = "POLYGON((2511651 1138667,2600520 1150191, 2626895 1174666))"
                    }
                }
            };

            FeatureService.Insert("MSSQL:server=;database=;tables=;UID=sa;PWD=", "bb_new", featureList);
        }

        [TestMethod]
        public void EditFeatureSqlServer()
        {
            //ID FEATUREID, Name TEXTSTRNG
            var featureList = new FeatureList()
            {
                IdFieldName = "id",
                Bodies = new List<FeatureBody>()
                {
                    new FeatureBody()
                    {
                        Id = 5,
                        Properties = new Dictionary<string, object>(){{ "objektart", DateTime.Now.ToString("O") }, {"id", 5} },
                        Geometry = "POLYGON((2600520 1150191, 2715764 1186190,2511651 1138667))"
                    },
                    new FeatureBody()
                    {
                        Id= 6,
                        Properties = new Dictionary<string, object>(){{ "objektart", DateTime.Now.ToString("O") }, {"id", 6} },
                        Geometry = "POLYGON((2511651 1138667,2600520 1150191, 2626895 1174666))"
                    }
                }
            };

            FeatureService.Update("MSSQL:server=;database=;tables=;UID=sa;PWD=", "bb_new", featureList);
        }

        [TestMethod]
        public void CreateFeaturePostgis()
        {
            //ID FEATUREID, Name TEXTSTRNG
            var featureList = new FeatureList()
            {
                IdFieldName = "FEATUREID",
                Bodies = new List<FeatureBody>()
                {
                    new FeatureBody()
                    {
                        Id = 5,
                        Properties = new Dictionary<string, object>(){{ "objektart", Guid.NewGuid().ToString() }, {"id", 5} },
                        Geometry = "MULTIPOLYGON(((2600520 1150191, 2715764 1186190,2511651 1138667)))"
                    },
                    new FeatureBody()
                    {
                        Id= 6,
                        Properties = new Dictionary<string, object>(){{ "objektart", $"** {Guid.NewGuid()}" }, {"id", 6} },
                        Geometry = "MULTIPOLYGON(((2511651 1138667,2600520 1150191, 2626895 1174666)))"
                    }
                }
            };

            FeatureService.Insert("PG: dbname='' host='localhost' port='5432' user='postgres' password=''", "bb", featureList);

        }

        [TestMethod]
        public void EditFeaturePostgis()
        {
            //ID FEATUREID, Name TEXTSTRNG
            var featureList = new FeatureList()
            {
                IdFieldName = "id",
                Bodies = new List<FeatureBody>()
                {
                    new FeatureBody()
                    {
                        Id = 11448,
                        Properties = new Dictionary<string, object>(){{ "objektart", "123456"} },
                        Geometry = "MULTIPOLYGON(((2600520 1150191, 2715764 1186190,2511651 1138667)))"
                    },
                }
            };

            FeatureService.Update("PG: dbname='' host='localhost' port='5432' user='postgres' password=''", "bb", featureList);

        }
    }
}