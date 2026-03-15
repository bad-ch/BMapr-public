namespace BMapr.GDAL.WebApi.Services
{
    public class LegendService
    {
        public static byte[] GetPointLegendDefinition(int width, int height, string style, string symbol, string label)
        {
            var mapfile = GetMapTemplate()
                .Replace("##width##", width.ToString())
                .Replace("##height##", height.ToString())
                .Replace("##type##", "POINT")
                .Replace("##feature##", "POINTS 50 50 END")
                .Replace("##style##", style)
                .Replace("##symbol##", symbol)
                .Replace("##label##", label);

            var mapserver = new Mapserver(mapfile);

            return mapserver.DrawImage("image/png",width, height, CancellationToken.None);
        }

        public static byte[] GetPolygonLegendDefinition(int width, int height, string style, string symbol, string label)
        {
            var mapfile = GetMapTemplate()
                .Replace("##width##", width.ToString())
                .Replace("##height##", height.ToString())
                .Replace("##type##", "POLYGON")
                .Replace("##feature##", "POINTS 10 10 90 10 90 90 10 90 10 10 END")
                .Replace("##style##", style)
                .Replace("##symbol##", symbol)
                .Replace("##label##", label);

            var mapserver = new Mapserver(mapfile);

            return mapserver.DrawImage("image/png", width, height, CancellationToken.None);
        }

        public static byte[] GetLineLegendDefinition(int width, int height, string style, string symbol, string label)
        {
            var mapfile = GetMapTemplate()
                .Replace("##width##", width.ToString())
                .Replace("##height##", height.ToString())
                .Replace("##type##", "LINE")
                .Replace("##feature##", "POINTS 10 10 40 90 60 30 90 90 END")
                .Replace("##style##", style)
                .Replace("##symbol##", symbol)
                .Replace("##label##", label);

            var mapserver = new Mapserver(mapfile);

            return mapserver.DrawImage("image/png", width, height, CancellationToken.None);
        }

        private static string GetMapTemplate()
        {
            return @"

                MAP
                  NAME ""inline_demo""
                  STATUS ON

                  SIZE ##width## ##height##
                  EXTENT 0 0 100 100
                  UNITS METERS

                  OUTPUTFORMAT
                    NAME ""png""
                    DRIVER ""AGG/PNG""
                    MIMETYPE ""image/png""
                    IMAGEMODE RGBA
                    EXTENSION ""png""
                    TRANSPARENT ON
                  END

                  IMAGETYPE ""png""

                  ##symbol##

                  LAYER
                    NAME ""inline_point""
                    TYPE ##type##
                    STATUS ON

                    FEATURE
                      ##feature##
                    END

                    CLASS
                      NAME ""Inline Point""
                      ##style##
                      ##label##  
                    END
                  END
                END             
            ";
        }
    }
}
