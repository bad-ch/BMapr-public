using System.Drawing;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Http.HttpResults;
using OSGeo.MapServer;

namespace BMapr.GDAL.WebApi.Services
{
    public class RawWmsService
    {
        private mapObj _mapObj { get; set; }

        public RawWmsService(mapObj mapObj)
        {
            _mapObj = mapObj;
        }

        public byte[] GetMap(string layers, string epsg, double xmin, double ymin, double xmax, double ymax, int width, int height, string mimeType)
        {
            var layerNames = layers.Split(',');

            for (int i = 0; i < _mapObj.numlayers; i++)
            {
                var layer = _mapObj.getLayer(i);

                layer.status = 0;

                if (layerNames.Contains(layer.name))
                {
                    layer.status = 1;
                }
            }

            _mapObj.setProjection(epsg);

            SetExtent(xmin,ymin,xmax,ymax, width,height);
            return DrawImageAsBytes(mimeType, width, height);
        }

        private void SetExtent(double xMin, double yMin, double xMax, double yMax, int width, int height)
        {
            _mapObj.width = width;
            _mapObj.height = height;
            _mapObj.setExtent(xMin, yMin, xMax, yMax);
        }

        private Image DrawImage(string mimeType, int width, int height)
        {
            var content = DrawImageAsBytes(mimeType, width, height);
            var memoryStream = new MemoryStream(content);
            return Image.FromStream(memoryStream);
        }

        private byte[] DrawImageAsBytes(string mimeType, int width, int height)
        {
            outputFormatObj outputFormat = null;

            _mapObj.width = width;
            _mapObj.height = height;

            switch (mimeType.ToLower())
            {
                case "image/png":

                    outputFormat = new outputFormatObj("AGG/PNG", "png");
                    outputFormat.transparent = 1;
                    outputFormat.imagemode = Convert.ToInt32(OSGeo.MapServer.MS_IMAGEMODE.MS_IMAGEMODE_RGBA);

                    break;

                case "image/jpg":
                case "image/jpeg":

                    outputFormat = new outputFormatObj("AGG/JPEG", "jpg");
                    outputFormat.transparent = 1;
                    outputFormat.imagemode = Convert.ToInt32(OSGeo.MapServer.MS_IMAGEMODE.MS_IMAGEMODE_RGBA);

                    break;

                default:

                    return null;
            }

            _mapObj.setOutputFormat(outputFormat);

            imageObj imageObj;

            try
            {
                imageObj = _mapObj.draw();
            }
            catch (Exception ex)
            {
                return null;
            }

            return imageObj.getBytes();
        }
    }
}
