using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMapr.GDAL.WebApi.Models.Spatial.Style
{
    public class StyleFill
    {
        //RGB opacity: integer[0-255], integer[0-255], integer[0-255], double[0.0-1.0]
        public double[] Color { get; set; }
    }
}
