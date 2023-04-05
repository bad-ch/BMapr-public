using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMapr.GDAL.WebApi.Models.Spatial.Style
{
    public class StyleIcon
    {
        public string Src { get; set; }
        public double[] Anchor { get; set; }
        public double ImageSize { get; set; }
        public double Scale { get; set; }
    }
}
