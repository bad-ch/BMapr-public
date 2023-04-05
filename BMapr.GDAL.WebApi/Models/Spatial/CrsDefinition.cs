namespace BMapr.GDAL.WebApi.Models.Spatial
{
    public class CrsDefinition
    {
        /* example
            {
                "Epsg": 2056,
                "Name": "CH1903+ / LV95",
                "Deprecated": 0,
                "Type": "projected",
                "AreaName": "Europe - Liechtenstein and Switzerland",
                "AreaDescription": "Liechtenstein; Switzerland.",
                "SouthBoundLat": 45.82,
                "NorthBoundLat": 47.81,
                "WestBoundLon": 5.96,
                "EastBoundLon": 10.49,
                "Axis1Order": 1,
                "Axis1Orientation": "east",
                "Axis1Abbreviation": "E",
                "Axis1UnitName": "metre",
                "Axis1UnitType": "length",
                "Axis2Order": 2,
                "Axis2Orientation": "north",
                "Axis2Abbreviation": "N",
                "Axis2UnitName": "metre",
                "Axis2UnitType": "length",
                "CoordSysType": "Cartesian",
                "Dimension": 2,
                "Proj4Js": "+proj=somerc +lat_0=46.95240555555556 +lon_0=7.439583333333333 +k_0=1 +x_0=2600000 +y_0=1200000 +ellps=bessel +towgs84=674.374,15.056,405.346,0,0,0,0 +units=m +no_defs ",
                "MapFile": "PROJECTION\n\t\"proj=somerc\"\n\t\"lat_0=46.95240555555556\"\n\t\"lon_0=7.439583333333333\"\n\t\"k_0=1\"\n\t\"x_0=2600000\"\n\t\"y_0=1200000\"\n\t\"ellps=bessel\"\n\t\"towgs84=674.374,15.056,405.346,0,0,0,0\"\n\t\"units=m\"\n\t\"no_defs\"\nEND",
                "XMin": 2485869.5728,
                "YMin": 1076443.1884,
                "XMax": 2837076.5648,
                "YMax": 1299941.7864
            }
         */

        public int Epsg { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Deprecated { get; set; }
        public string Type { get; set; } = string.Empty;
        public string AreaName { get; set; } = string.Empty;
        public string AreaDescription { get; set; } = string.Empty;
        public double SouthBoundLat { get; set; }
        public double NorthBoundLat { get; set; }
        public double WestBoundLon { get; set; }
        public double EastBoundLon { get; set; }
        public int Axis1Order { get; set; }
        public string Axis1Orientation { get; set; } = string.Empty;
        public string Axis1Abbreviation { get; set; } = string.Empty;
        public string Axis1UnitName { get; set; } = string.Empty;
        public string Axis1UnitType { get; set; } = string.Empty;
        public int Axis2Order { get; set; }
        public string Axis2Orientation { get; set; } = string.Empty;
        public string Axis2Abbreviation { get; set; } = string.Empty;
        public string Axis2UnitName { get; set; } = string.Empty;
        public string Axis2UnitType { get; set; } = string.Empty;
        public string CoordSysType { get; set; } = string.Empty;
        public int Dimension { get; set; }
        public string Proj4Js { get; set; } = string.Empty;
        public string MapFile { get; set; } = string.Empty;
        public double XMin { get; set; }
        public double YMin { get; set; }
        public double XMax { get; set; }
        public double YMax { get; set; }
    }
}
