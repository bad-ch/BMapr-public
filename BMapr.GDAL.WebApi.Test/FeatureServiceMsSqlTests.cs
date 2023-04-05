using System.Xml;
using BMapr.GDAL.WebApi.Models;
using BMapr.GDAL.WebApi.Models.Spatial;
using BMapr.GDAL.WebApi.Services;
using Newtonsoft.Json;

namespace BMapr.GDAL.WebApi.Test
{
    [TestClass]
    public class FeatureServiceMsSqlTests
    {
        [TestMethod]
        public void GetFields()
        {
            var featureService = new FeatureServiceMsSql("server=;database=;User Id=sa;Password=", "bb_new");
            Assert.IsTrue(featureService.Fields.Count == 6);
            featureService.Dispose();
        }

        [TestMethod]
        public void CreateFeatureSqlServer()
        {
            var featureList = new FeatureList()
            {
                IdFieldName = "fid",
                Bodies = new List<FeatureBody>()
                {
                    new FeatureBody()
                    {
                        Properties = new Dictionary<string, object>(){{ "objektart", $"New** {Guid.NewGuid()}" } },
                        Geometry = "POLYGON((2600520 1150191, 2715764 1186190,2511651 1138667,2600520 1150191))",
                        Epsg = 2056
                    },
                    new FeatureBody()
                    {
                        Properties = new Dictionary<string, object>(){{ "objektart", $"New**** {Guid.NewGuid()}" } },
                        Geometry = "POLYGON((2511651 1138667,2600520 1150191, 2626895 1174666,2511651 1138667))",
                        Epsg = 2056
                    }
                }
            };

            var featureService = new FeatureServiceMsSql("server=;database=;User Id=sa;Password=", "bb_new");
            featureService.Insert(featureList);
        }

    }
}
