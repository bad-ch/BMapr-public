using BMapr.GDAL.WebApi.Models;
using OSGeo.OGR;
using OSGeo.OSR;
using System.Text.RegularExpressions;

namespace BMapr.GDAL.WebApi.Services
{
    public class GeometryService
    {
        public static Result<Geometry> GetOgrGeometryFromWkt(string inGeometry, int precision = 5)
        {
            GdalConfiguration.ConfigureOgr();

            var result = new Result<Geometry>();

            try
            {
                var geometryChangedPrecision = ChangePrecision(inGeometry, precision);
                var geometry = Ogr.CreateGeometryFromWkt(ref geometryChangedPrecision, new SpatialReference(""));
                result.Value = geometry;
                result.Succesfully = true;
                return  result;
            }
            catch (Exception ex)
            {
                result.Exceptions.Add(ex);
            }

            return result;
        }

        public static Result<Geometry> GetOgrGeometryFromGml(string inGeometry)
        {
            GdalConfiguration.ConfigureOgr();

            var result = new Result<Geometry>();

            try
            {
                var geometry = Ogr.CreateGeometryFromGML(inGeometry);
                result.Value = geometry;
                result.Succesfully = true;
                return result;
            }
            catch (Exception ex)
            {
                result.Exceptions.Add(ex);
            }

            return result;
        }

        public static string GetStringFromOgrGeometry(Geometry geometry, string format, bool checkValidation = true)
        {
            if (!geometry.IsValid() && checkValidation)
            {
                return string.Empty;
            }

            switch (format.ToLower())
            {
                case "geojson":
                    string[] options = null;
                    return geometry.ExportToJson(options);

                case "gml":
                    return geometry.ExportToGML();

                case "wkt":
                    geometry.ExportToWkt(out var geometryString);
                    return geometryString;

                case "kml":
                    return geometry.ExportToKML(string.Empty);
            }

            return string.Empty;
        }

        private static string ChangePrecision(string original, int precision)
        {
            return Regex.Replace(original, @"\d+[\.]?\d*", m => PrecisionString(m.ToString(), precision)); ;
        }

        private static string PrecisionString(string value, int precision)
        {
            double valueDouble;
            Double.TryParse(value, out valueDouble);
            var template = "{0}";
            return String.Format(template, Math.Round(valueDouble, precision));
        }

        public static Result<Geometry> InvertGeometry(Geometry geometry)
        {
            GdalConfiguration.ConfigureOgr();

            var result = new Result<Geometry>();

            try
            {
                var geometryCloned = geometry.Clone();
                geometryCloned.SwapXY();
                result.Value = geometryCloned;
                result.Succesfully = true;
                return result;
            }
            catch (Exception ex)
            {
                result.Exceptions.Add(ex);
            }

            return result;
        }

        public static Result<string> InvertWktGeometry(string wktGeometry)
        {
            GdalConfiguration.ConfigureOgr();

            var result = new Result<string>();

            try
            {
                var geometry = GetOgrGeometryFromWkt(wktGeometry);
                var geometryInverted = InvertGeometry(geometry.Value);

                result.Value = GetStringFromOgrGeometry(geometryInverted.Value, "wkt");
                result.Succesfully = true;
                return result;
            }
            catch (Exception ex)
            {
                result.Exceptions.Add(ex);
            }

            return result;
        }
    }
}
