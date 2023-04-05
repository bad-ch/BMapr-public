export var LayerType;
(function (LayerType) {
    LayerType["WMTS"] = "WMTS";
    LayerType["WMS"] = "WMS";
    LayerType["WFS"] = "WFS";
    LayerType["Vector"] = "Vector";
})(LayerType || (LayerType = {}));
export var VectorSourceType;
(function (VectorSourceType) {
    VectorSourceType["GeoJSON"] = "GeoJSON";
    VectorSourceType["KML"] = "KML";
    VectorSourceType["KMZ"] = "KMZ";
    VectorSourceType["GPX"] = "GPX";
    VectorSourceType["GML212"] = "GML212";
    VectorSourceType["GML311"] = "GML311";
    VectorSourceType["GML312"] = "GML312";
    VectorSourceType["WKT"] = "WKT";
    VectorSourceType["CSV"] = "CSV";
})(VectorSourceType || (VectorSourceType = {}));
export var MimeType;
(function (MimeType) {
    MimeType["GeoJSON"] = "application/json";
    MimeType["KML"] = "application/vnd.google-earth.kml+xml";
    MimeType["KMZ"] = "application/vnd.google-earth.kmz";
    MimeType["GPX"] = "application/gpx+xml";
    MimeType["GML212"] = "text/xml; subtype=gml/2.1.2";
    MimeType["GML311"] = "text/xml; subtype=gml/3.1.1";
    MimeType["GML312"] = "text/xml; subtype=gml/3.1.2";
    MimeType["WKT"] = "text/plain";
    MimeType["CSV"] = "text/csv";
})(MimeType || (MimeType = {}));
export var OgcOutputFormatType;
(function (OgcOutputFormatType) {
    OgcOutputFormatType["JSON"] = "JSON";
    OgcOutputFormatType["GML2"] = "GML2";
    OgcOutputFormatType["GML3"] = "GML3";
})(OgcOutputFormatType || (OgcOutputFormatType = {}));
export var DisplayMode;
(function (DisplayMode) {
    DisplayMode["On"] = "On";
    DisplayMode["Off"] = "Off";
    DisplayMode["ByScale"] = "ByScale";
})(DisplayMode || (DisplayMode = {}));
export var TextAlign;
(function (TextAlign) {
    TextAlign["Left"] = "left";
    TextAlign["Right"] = "right";
    TextAlign["Center"] = "center";
    TextAlign["Start"] = "start";
    TextAlign["End"] = "end";
})(TextAlign || (TextAlign = {}));
export var TextBaseLine;
(function (TextBaseLine) {
    TextBaseLine["Bottom"] = "bottom";
    TextBaseLine["Top"] = "top";
    TextBaseLine["Middle"] = "middle";
    TextBaseLine["Alphabetic"] = "alphabetic";
    TextBaseLine["Hanging"] = "hanging";
    TextBaseLine["Ideographic"] = "ideographic";
})(TextBaseLine || (TextBaseLine = {}));
export var AnchorUnitType;
(function (AnchorUnitType) {
    AnchorUnitType["Fraction"] = "fraction";
    AnchorUnitType["Pixels"] = "pixels";
})(AnchorUnitType || (AnchorUnitType = {}));
//# sourceMappingURL=client.js.map