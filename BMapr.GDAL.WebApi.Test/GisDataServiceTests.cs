using BMapr.GDAL.WebApi.Services;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMapr.GDAL.WebApi.Test
{
    [TestClass]
    public class GisDataServiceTests
    {
        [TestMethod]
        public void ConvertShapeToFlatgeobuf()
        {
            var filePath = Path.GetFullPath("../../../TestFiles/02_DKM500_STRASSE_ANNO.shp");
            var filePathTarget = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.fgb");
            var gisDataService = GisDataService.Convert(filePath, filePathTarget, 2056, 2056);
            Assert.IsTrue(gisDataService);
        }
    }
}
