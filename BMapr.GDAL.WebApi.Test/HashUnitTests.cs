using BMapr.GDAL.WebApi.Models.Tracking;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BMapr.GDAL.WebApi.Services;

namespace BMapr.GDAL.WebApi.Test
{
    [TestClass]
    public class HashUnitTests
    {
        [TestMethod]
        public void HashString()
        {
            var test = "Dies ist ein Test";

            var md5 = HashService.CreateFromStringMd5(test);

            Assert.AreEqual(md5, "6cddeb6a2f0582c82dee9a38e3f035d7");
        }

        [TestMethod]
        public void HashFile()
        {
            var mapPath = Path.GetFullPath("../../../TestFiles/bb.map");

            var md5 = HashService.CreateFromStringMd5(mapPath);

            Assert.AreEqual(md5, "a5e04b3d82bfa92b7c0243c26c5519ea");
        }
    }
}
