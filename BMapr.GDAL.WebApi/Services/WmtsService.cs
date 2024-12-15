using System.Web;
using BMapr.GDAL.WebApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using OSGeo.OGR;

namespace BMapr.GDAL.WebApi.Services
{
    /// <summary>
    /// Convert WMTS getTile requests to WMS getMap request
    /// </summary>
    public class WmtsService
    {
        private static ILogger<WmtsService> _logger;

        public static ActionResult HandleRequest(string project, string queryString, Config config)
        {
            var nameValueCollection = HttpUtility.ParseQueryString(queryString);
            var parameters = nameValueCollection.AllKeys.ToDictionary(key => key.ToLower(), value => nameValueCollection[value]);

            // todo improve parameter handling
            // var parametersGetTileMandatory = new List<string>() { "service", "request", "version", "layer", "style", "format", "tilematrixset", "tilematrix", "tilerow", "tilecol" };
            // var parametersGetCapabilitiesMandatory = new List<string>() { "service", "request", "version" };

            if (!parameters.ContainsKey("service") || parameters["service"].ToLower() != "wmts" || !parameters.ContainsKey("version") || parameters["version"].ToLower() != "1.0.0")
            {
                return new BadRequestObjectResult("service or version parameter wrong");
            }

            var projectPath = Path.Combine(config.DataProjects.FullName, project);

            try
            {
                if (parameters.ContainsKey("request") && parameters["request"].ToLower() == "getcapabilities")
                {
                    return WmtsService.WmtsGetCapabilities(parameters["service"], parameters["request"], parameters["version"], projectPath, config, project);
                }

                if (parameters.ContainsKey("request") && parameters["request"].ToLower() == "gettile")
                {
                    var capabilitiesFilePath = Path.Combine(projectPath, "wmts.xml");
                    var content = System.IO.File.ReadAllText(capabilitiesFilePath);
                    var contentNoNs = XmlService.RemoveAllNamespacesWMTS(content);
                    var capabilities = XmlService.DeserializeString<Capabilities>(contentNoNs);
                    var format = parameters["format"];

                    if (format.ToLower() == "jpg" || format.ToLower() == "jpeg")
                    {
                        format = "image/jpeg";
                    }
                    if (format.ToLower() == "png")
                    {
                        format = "image/png";
                    }
                    if (format.ToLower() == "gif")
                    {
                        format = "image/gif";
                    }

                    return WmtsService.GetWmsRequestFromWmtsGetTile(
                        config,
                        project,
                        capabilities,
                        parameters["service"],
                        parameters["request"],
                        parameters["version"],
                        parameters["layer"],
                        parameters["style"],
                        format,
                        parameters["tilematrixset"],
                        parameters["tilematrix"],
                        Convert.ToInt32(parameters["tilerow"]),
                        Convert.ToInt32(parameters["tilecol"])
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"WMTS Service error, {project}, {ex.Message}");
                throw;
            }

            return new BadRequestObjectResult("invalid WMTS request");
        }

        public static ActionResult WmtsGetCapabilities(string service, string request, string version, string projectPath, Config config, string project)
        {
            if (service.ToLower() != "wmts")
            {
                return new BadRequestObjectResult("InvalidParameterValue");
            }

            if (request.ToLower() != "getcapabilities")
            {
                return new BadRequestObjectResult("InvalidParameterValue");
            }

            if (version.ToLower() != "1.0.0")
            {
                return new BadRequestObjectResult("InvalidParameterValue");
            }

            var capabilitiesFile = Path.Combine(projectPath, "wmts.xml");

            if (!Directory.Exists(projectPath) || !File.Exists(capabilitiesFile))
            {
                return new BadRequestObjectResult("WMTS get capabilities file is not available");
            }

            var content = File.ReadAllText(capabilitiesFile);

            // todo debug
            // content = content.Replace("https://ogc.dev.local", "https://localhost:7090");

            content = content.Replace("#host#", config.Host);
            content = content.Replace("#project#", project);

            return new FileContentResult(System.Text.Encoding.UTF8.GetBytes(content), "text/xml");
        }

        public static ActionResult GetWmsRequestFromWmtsGetTile(Config config, string project, Capabilities capabilities, string service, string request, string version, string layer, string style, string format, string tileMatrixSet, string tileMatrix, int tileRow, int tileCol)
        {
            //SERVICE=WMTS&REQUEST=GetTile&VERSION=version&Layer=&Style=&Format=&TileMatrixSet=&TileMatrix=&TileRow=&TileCol=

            if (service.ToLower()!="wmts")
            {
                return new BadRequestObjectResult("InvalidParameterValue, service");
            }

            if (request.ToLower() != "gettile")
            {
                return new BadRequestObjectResult("InvalidParameterValue, request");
            }

            if (version.ToLower() != "1.0.0")
            {
                return new BadRequestObjectResult("InvalidParameterValue, version");
            }

            var layerFound = capabilities.Contents.Layer.FirstOrDefault(x => x.Identifier == layer);

            if (layerFound == null)
            {
                return new BadRequestObjectResult("InvalidParameterValue, layer");
            }

            if (layerFound.Style.Identifier != style)
            {
                return new BadRequestObjectResult("InvalidParameterValue, layer style");
            }

            if (layerFound.Format != format)
            {
                return new BadRequestObjectResult("InvalidParameterValue, layer format");
            }

            var tileMatrixSetFound = capabilities.Contents.TileMatrixSet.FirstOrDefault(x => (string)x.Items[0] == tileMatrixSet);

            if (tileMatrixSetFound == null)
            {
                return new BadRequestObjectResult("InvalidParameterValue, tileMatrixSet");
            }

            var crs = tileMatrixSetFound.Items[1].ToString();

            if (crs == null || !crs.ToLower().Contains("epsg"))
            {
                return new BadRequestObjectResult("InvalidParameterValue, crs not EPSG");
            }

            var epsgCode = System.Convert.ToInt32(crs.Split(':').ToList().Last());

            var tileMatrixList = tileMatrixSetFound.Items.Skip(2).Select(x=> (TileMatrix)x).ToList();
            var tileMatrixFound = tileMatrixList.FirstOrDefault(x => x.Identifier == tileMatrix);

            if (tileMatrixFound == null)
            {
                return new BadRequestObjectResult("InvalidParameterValue, tileMatrix");
            }

            if (tileCol > System.Convert.ToInt32(tileMatrixFound.MatrixWidth) || tileRow > System.Convert.ToInt32(tileMatrixFound.MatrixHeight))
            {
                return new BadRequestObjectResult("TileOutOfRange, tileMatrix");
            }

            var meterPerUnit = 1m; //relation to CRS for epsg 3857 it's 1, for example 2056 we don't know the value exactly

            var originX = System.Convert.ToDecimal(tileMatrixFound.TopLeftCorner.Split(' ')[0]);
            var originY = System.Convert.ToDecimal(tileMatrixFound.TopLeftCorner.Split(' ')[1]);
            var scaleDenominator = tileMatrixFound.ScaleDenominator;
            var tileWidth = ((decimal)System.Convert.ToInt32(tileMatrixFound.TileWidth)) * scaleDenominator * 0.00028m * meterPerUnit;
            var tileHeight = ((decimal)System.Convert.ToInt32(tileMatrixFound.TileHeight)) * scaleDenominator * 0.00028m * meterPerUnit;

            var xMin = originX + (tileWidth * tileCol);
            var yMin = originY - (tileHeight * (tileRow + 1)); // todo check

            var xMax = originX + (tileWidth * (tileCol + 1)); // todo check
            var yMax = originY - (tileHeight * tileRow);

            var queryString = $"SERVICE=WMS&VERSION=1.1.1&REQUEST=GetMap&LAYERS={layer}&SRS=EPSG:{epsgCode}&BBOX={xMin},{yMin},{xMax},{yMax}&WIDTH={tileMatrixFound.TileWidth}&HEIGHT={tileMatrixFound.TileHeight}&STYLES=&FORMAT={layerFound.Format}&TRANSPARENT=true";

            var response = OgcService.Process( OgcService.RequestType.Get, queryString, "", config, project);

            //todo cache and save result

            return response;
        }
    }
}
