using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BMapr.GDAL.WebApi.Models.Tracking;
using Newtonsoft.Json;

namespace BMapr.GDAL.WebApi.Test
{
    [TestClass]
    public class TrackingUnitTests
    {
        [TestMethod]
        public void DesLocationFile()
        {
            var locationPath = Path.GetFullPath("../../../TestFiles/owntracks_location.json");
            var locationContent = File.ReadAllText(locationPath);

            var location = JsonConvert.DeserializeObject<Location>(locationContent);
            
            Assert.IsNotNull(location);
            Assert.AreEqual(location.Longitude, 7.9945401);
        }
    }
}
