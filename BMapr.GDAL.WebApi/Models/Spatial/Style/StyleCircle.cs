using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMapr.GDAL.WebApi.Models.Spatial.Style
{
    public class StyleCircle
    {
        public double Radius { get; set; }
        public StyleFill Fill { get; set; }
        public StyleStroke Stroke { get; set; }
    }
}
