// MapFile.cs
// MapServer 8.6 mapfile model + parser + writer (focused on LAYER/CLASS/STYLE/LABEL completeness)
// Notes:
//  * Comprehensive properties for CLASS, STYLE, LABEL per MapServer 8.6 docs.
//  * Unknown directives preserved via Attributes bags for forward-compatibility.
//  * Color fields support RGB triplets OR hex/rgba strings OR attribute/expression strings.
//  * Added support for FEATURE blocks (captured and re-emitted) and INCLUDE expansion when parsing from files.
//
// References used while aligning keys (MapServer 8.6 docs):
//  - CLASS: https://mapserver.github.io/mapfile/class.html
//  - STYLE: https://mapserver.github.io/mapfile/style.html (may redirect/localize)
//  - LABEL: https://mapserver.github.io/mapfile/label.html

using System.Globalization;
using System.Text;

namespace BMapr.GDAL.WebApi.Services
{
    #region Model classes

    public class MapObj
    {
        public string? Name { get; set; }
        public string? Status { get; set; }
        public double[]? Extent { get; set; }
        public int[]? Size { get; set; }
        public string? Units { get; set; }

        public string? ShapePath { get; set; }
        public string? SymbolSet { get; set; }
        public string? FontSet { get; set; }
        public string? ImageType { get; set; }
        public int[]? ImageColor { get; set; }
        public string? ImageColorHex { get; set; }
        public string? TemplatePattern { get; set; }
        public string? DataPattern { get; set; }
        public int? Resolution { get; set; }
        public int? DefResolution { get; set; }
        public int? MaxSize { get; set; }
        public double? Angle { get; set; }
        public string? Debug { get; set; }
        public string? Transparent { get; set; }

        public List<KeyValuePair<string,string>> Config { get; } = new();

        public List<string> Projection { get; } = new();
        public WebObj Web { get; set; } = new WebObj();
        public List<OutputFormatObj> OutputFormats { get; } = new();
        public List<LayerObj> Layers { get; } = new();

        public Dictionary<string, List<string[]>> Attributes { get; } = new(StringComparer.OrdinalIgnoreCase);

        #region Parse / Write API
        public static MapObj Parse(string text) => MapfileParser.Parse(text, baseDir: null);
        public static MapObj ParseFile(string path)
        {
            var text = File.ReadAllText(path);
            var baseDir = Path.GetDirectoryName(Path.GetFullPath(path));
            return MapfileParser.Parse(text, baseDir);
        }
        public string ToMapfileString(){ var sw = new StringWriter(); Write(sw); return sw.ToString(); }
        public void WriteToFile(string path){ using var fs = File.Create(path); using var sw = new StreamWriter(fs, new UTF8Encoding(false)); Write(sw);}        
        #endregion

        public void Write(TextWriter w)
        {
            w.WriteLine("MAP");
            if (!string.IsNullOrWhiteSpace(Name)) MapfileSerializer.WriteKeyValues(w, 1, "NAME", MapfileSerializer.Quote(Name!));
            if (!string.IsNullOrWhiteSpace(Status)) MapfileSerializer.WriteKeyValues(w, 1, "STATUS", Status!);
            if (Extent != null && Extent.Length==4) MapfileSerializer.WriteKeyValues(w, 1, "EXTENT", Extent.Select(d=>d.ToString(CultureInfo.InvariantCulture)).ToArray());
            if (Size != null && Size.Length==2) MapfileSerializer.WriteKeyValues(w, 1, "SIZE", Size.Select(d=>d.ToString(CultureInfo.InvariantCulture)).ToArray());
            if (!string.IsNullOrWhiteSpace(Units)) MapfileSerializer.WriteKeyValues(w, 1, "UNITS", Units!);

            if (!string.IsNullOrWhiteSpace(ImageType)) MapfileSerializer.WriteKeyValues(w, 1, "IMAGETYPE", MapfileSerializer.MaybeQuote(ImageType!));
            if (ImageColor != null && ImageColor.Length==3) MapfileSerializer.WriteKeyValues(w, 1, "IMAGECOLOR", ImageColor.Select(i=>i.ToString(CultureInfo.InvariantCulture)).ToArray());
            else if (!string.IsNullOrWhiteSpace(ImageColorHex)) MapfileSerializer.WriteKeyValues(w, 1, "IMAGECOLOR", MapfileSerializer.Quote(ImageColorHex!));

            if (!string.IsNullOrWhiteSpace(ShapePath)) MapfileSerializer.WriteKeyValues(w, 1, "SHAPEPATH", MapfileSerializer.Quote(ShapePath!));
            if (!string.IsNullOrWhiteSpace(SymbolSet)) MapfileSerializer.WriteKeyValues(w, 1, "SYMBOLSET", MapfileSerializer.Quote(SymbolSet!));
            if (!string.IsNullOrWhiteSpace(FontSet)) MapfileSerializer.WriteKeyValues(w, 1, "FONTSET", MapfileSerializer.Quote(FontSet!));

            if (Resolution.HasValue) MapfileSerializer.WriteKeyValues(w, 1, "RESOLUTION", Resolution.Value.ToString(CultureInfo.InvariantCulture));
            if (DefResolution.HasValue) MapfileSerializer.WriteKeyValues(w, 1, "DEFRESOLUTION", DefResolution.Value.ToString(CultureInfo.InvariantCulture));
            if (MaxSize.HasValue) MapfileSerializer.WriteKeyValues(w, 1, "MAXSIZE", MaxSize.Value.ToString(CultureInfo.InvariantCulture));
            if (Angle.HasValue) MapfileSerializer.WriteKeyValues(w, 1, "ANGLE", Angle.Value.ToString(CultureInfo.InvariantCulture));

            if (!string.IsNullOrWhiteSpace(TemplatePattern)) MapfileSerializer.WriteKeyValues(w, 1, "TEMPLATEPATTERN", MapfileSerializer.Quote(TemplatePattern!));
            if (!string.IsNullOrWhiteSpace(DataPattern)) MapfileSerializer.WriteKeyValues(w, 1, "DATAPATTERN", MapfileSerializer.Quote(DataPattern!));

            if (!string.IsNullOrWhiteSpace(Debug)) MapfileSerializer.WriteKeyValues(w, 1, "DEBUG", Debug!);
            if (!string.IsNullOrWhiteSpace(Transparent)) MapfileSerializer.WriteKeyValues(w, 1, "TRANSPARENT", Transparent!);

            foreach (var kv in Config) MapfileSerializer.WriteKeyValues(w, 1, "CONFIG", MapfileSerializer.MaybeQuote(kv.Key), MapfileSerializer.MaybeQuote(kv.Value));

            MapfileSerializer.WriteAttributes(w, 1, Attributes);

            if (Projection.Count>0){ MapfileSerializer.WriteIndent(w,1); w.WriteLine("PROJECTION"); foreach(var l in Projection){ MapfileSerializer.WriteIndent(w,2); w.WriteLine(MapfileSerializer.Quote(l)); } MapfileSerializer.WriteIndent(w,1); w.WriteLine("END"); }
            if (!Web.IsEmpty) Web.Write(w,1);
            foreach (var of in OutputFormats) of.Write(w,1);
            foreach (var layer in Layers) layer.Write(w,1);
            w.WriteLine("END");
        }
    }

    public class WebObj
    {
        public Dictionary<string,string> Metadata { get; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string,List<string[]>> Attributes { get; } = new(StringComparer.OrdinalIgnoreCase);
        public bool IsEmpty => Metadata.Count==0 && Attributes.Count==0;
        public void Write(TextWriter w, int indent)
        {
            MapfileSerializer.WriteIndent(w, indent); w.WriteLine("WEB");
            MapfileSerializer.WriteAttributes(w, indent+1, Attributes);
            if (Metadata.Count>0){ MapfileSerializer.WriteIndent(w, indent+1); w.WriteLine("METADATA"); foreach(var kv in Metadata){ MapfileSerializer.WriteIndent(w, indent+2); w.WriteLine($"{MapfileSerializer.Quote(kv.Key)} {MapfileSerializer.Quote(kv.Value)}"); } MapfileSerializer.WriteIndent(w, indent+1); w.WriteLine("END"); }
            MapfileSerializer.WriteIndent(w, indent); w.WriteLine("END");
        }
    }

    public class OutputFormatObj
    {
        public string? Name { get; set; }
        public string? Driver { get; set; }
        public string? MimeType { get; set; }
        public string? Extension { get; set; }
        public string? ImageMode { get; set; }
        public string? Transparent { get; set; }
        public Dictionary<string,List<string[]>> Attributes { get; } = new(StringComparer.OrdinalIgnoreCase);
        public void Write(TextWriter w, int indent)
        {
            MapfileSerializer.WriteIndent(w, indent); w.WriteLine("OUTPUTFORMAT");
            if (!string.IsNullOrWhiteSpace(Name)) MapfileSerializer.WriteKeyValues(w, indent+1, "NAME", MapfileSerializer.MaybeQuote(Name!));
            if (!string.IsNullOrWhiteSpace(Driver)) MapfileSerializer.WriteKeyValues(w, indent+1, "DRIVER", MapfileSerializer.MaybeQuote(Driver!));
            if (!string.IsNullOrWhiteSpace(MimeType)) MapfileSerializer.WriteKeyValues(w, indent+1, "MIMETYPE", MapfileSerializer.Quote(MimeType!));
            if (!string.IsNullOrWhiteSpace(Extension)) MapfileSerializer.WriteKeyValues(w, indent+1, "EXTENSION", MapfileSerializer.MaybeQuote(Extension!));
            if (!string.IsNullOrWhiteSpace(ImageMode)) MapfileSerializer.WriteKeyValues(w, indent+1, "IMAGEMODE", ImageMode!);
            if (!string.IsNullOrWhiteSpace(Transparent)) MapfileSerializer.WriteKeyValues(w, indent+1, "TRANSPARENT", Transparent!);
            MapfileSerializer.WriteAttributes(w, indent+1, Attributes);
            MapfileSerializer.WriteIndent(w, indent); w.WriteLine("END");
        }
    }

    public class LayerObj
    {
        public string? Name { get; set; }
        public string? Type { get; set; }
        public string? Status { get; set; }
        public string? Data { get; set; }
        public string? ConnectionType { get; set; }
        public string? Connection { get; set; }

        public string? ClassItem { get; set; }
        public string? LabelItem { get; set; }
        public string? ClassGroup { get; set; }
        public string? Group { get; set; }

        public double? MinScaleDenom { get; set; }
        public double? MaxScaleDenom { get; set; }
        public double? MinScale { get; set; }
        public double? MaxScale { get; set; }
        public double? MinGeoWidth { get; set; }
        public double? MaxGeoWidth { get; set; }
        public double? SymbolScaleDenom { get; set; }

        public double[]? Extent { get; set; }
        public string? Units { get; set; }

        public double? LabelMinScaleDenom { get; set; }
        public double? LabelMaxScaleDenom { get; set; }
        public string? LabelRequires { get; set; }
        public string? LabelCache { get; set; }

        public string? Debug { get; set; }
        public string? Encoding { get; set; }
        public string? Filter { get; set; }
        public string? FilterItem { get; set; }
        public int? MinFeatureSize { get; set; }
        public int? MaxFeatures { get; set; }
        public string? Mask { get; set; }
        public string? StyleItem { get; set; }
        public string? GeomTransform { get; set; }
        public string? PostLabelCache { get; set; }
        public string? Requires { get; set; }
        public string? Transform { get; set; }
        public double? Tolerance { get; set; }
        public string? ToleranceUnits { get; set; }
        public string? Header { get; set; }
        public string? Footer { get; set; }
        public string? Template { get; set; }

        public int[]? OffsiteColor { get; set; }
        public string? OffsiteHex { get; set; }
        public int? Opacity { get; set; }
        public string? OpacityKeyword { get; set; }

        public string? TileIndex { get; set; }
        public string? TileItem { get; set; }
        public string? TileFilter { get; set; }
        public string? TileFilterItem { get; set; }

        public List<string> Projection { get; } = new();
        public Dictionary<string,string> Metadata { get; } = new(StringComparer.OrdinalIgnoreCase);
        public List<ClassObj> Classes { get; } = new();
        public List<JoinObj> Joins { get; } = new();

        public List<string> Processing { get; } = new();
        public Dictionary<string,string> Validation { get; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string,string> ConnectionOptions { get; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, List<string[]>> Identify { get; } = new(StringComparer.OrdinalIgnoreCase);

        public List<FeatureObj> Features { get; } = new(); // FEATURE blocks

        public Dictionary<string,List<string[]>> Attributes { get; } = new(StringComparer.OrdinalIgnoreCase);

        public void Write(TextWriter w, int indent)
        {
            MapfileSerializer.WriteIndent(w, indent); w.WriteLine("LAYER");
            if (!string.IsNullOrWhiteSpace(Name)) MapfileSerializer.WriteKeyValues(w, indent+1, "NAME", MapfileSerializer.Quote(Name!));
            if (!string.IsNullOrWhiteSpace(Type)) MapfileSerializer.WriteKeyValues(w, indent+1, "TYPE", Type!);
            if (!string.IsNullOrWhiteSpace(Status)) MapfileSerializer.WriteKeyValues(w, indent+1, "STATUS", Status!);
            if (!string.IsNullOrWhiteSpace(Data)) MapfileSerializer.WriteKeyValues(w, indent+1, "DATA", MapfileSerializer.Quote(Data!));
            if (!string.IsNullOrWhiteSpace(ConnectionType)) MapfileSerializer.WriteKeyValues(w, indent+1, "CONNECTIONTYPE", ConnectionType!);
            if (!string.IsNullOrWhiteSpace(Connection)) MapfileSerializer.WriteKeyValues(w, indent+1, "CONNECTION", MapfileSerializer.Quote(Connection!));

            if (!string.IsNullOrWhiteSpace(Group)) MapfileSerializer.WriteKeyValues(w, indent+1, "GROUP", MapfileSerializer.Quote(Group!));
            if (!string.IsNullOrWhiteSpace(ClassGroup)) MapfileSerializer.WriteKeyValues(w, indent+1, "CLASSGROUP", MapfileSerializer.Quote(ClassGroup!));
            if (!string.IsNullOrWhiteSpace(ClassItem)) MapfileSerializer.WriteKeyValues(w, indent+1, "CLASSITEM", MapfileSerializer.Quote(ClassItem!));
            if (!string.IsNullOrWhiteSpace(LabelItem)) MapfileSerializer.WriteKeyValues(w, indent+1, "LABELITEM", MapfileSerializer.Quote(LabelItem!));

            if (MinScaleDenom.HasValue) MapfileSerializer.WriteKeyValues(w, indent+1, "MINSCALEDENOM", MinScaleDenom.Value.ToString(CultureInfo.InvariantCulture));
            if (MaxScaleDenom.HasValue) MapfileSerializer.WriteKeyValues(w, indent+1, "MAXSCALEDENOM", MaxScaleDenom.Value.ToString(CultureInfo.InvariantCulture));
            if (MinScale.HasValue) MapfileSerializer.WriteKeyValues(w, indent+1, "MINSCALE", MinScale.Value.ToString(CultureInfo.InvariantCulture));
            if (MaxScale.HasValue) MapfileSerializer.WriteKeyValues(w, indent+1, "MAXSCALE", MaxScale.Value.ToString(CultureInfo.InvariantCulture));
            if (MinGeoWidth.HasValue) MapfileSerializer.WriteKeyValues(w, indent+1, "MINGEOWIDTH", MinGeoWidth.Value.ToString(CultureInfo.InvariantCulture));
            if (MaxGeoWidth.HasValue) MapfileSerializer.WriteKeyValues(w, indent+1, "MAXGEOWIDTH", MaxGeoWidth.Value.ToString(CultureInfo.InvariantCulture));
            if (SymbolScaleDenom.HasValue) MapfileSerializer.WriteKeyValues(w, indent+1, "SYMBOLSCALEDENOM", SymbolScaleDenom.Value.ToString(CultureInfo.InvariantCulture));

            if (Extent!=null && Extent.Length==4) MapfileSerializer.WriteKeyValues(w, indent+1, "EXTENT", Extent.Select(d=>d.ToString(CultureInfo.InvariantCulture)).ToArray());
            if (!string.IsNullOrWhiteSpace(Units)) MapfileSerializer.WriteKeyValues(w, indent+1, "UNITS", Units!);

            if (LabelMinScaleDenom.HasValue) MapfileSerializer.WriteKeyValues(w, indent+1, "LABELMINSCALEDENOM", LabelMinScaleDenom.Value.ToString(CultureInfo.InvariantCulture));
            if (LabelMaxScaleDenom.HasValue) MapfileSerializer.WriteKeyValues(w, indent+1, "LABELMAXSCALEDENOM", LabelMaxScaleDenom.Value.ToString(CultureInfo.InvariantCulture));
            if (!string.IsNullOrWhiteSpace(LabelRequires)) MapfileSerializer.WriteKeyValues(w, indent+1, "LABELREQUIRES", MapfileSerializer.Quote(LabelRequires!));
            if (!string.IsNullOrWhiteSpace(LabelCache)) MapfileSerializer.WriteKeyValues(w, indent+1, "LABELCACHE", LabelCache!);

            if (!string.IsNullOrWhiteSpace(Debug)) MapfileSerializer.WriteKeyValues(w, indent+1, "DEBUG", Debug!);
            if (!string.IsNullOrWhiteSpace(Encoding)) MapfileSerializer.WriteKeyValues(w, indent+1, "ENCODING", MapfileSerializer.Quote(Encoding!));
            if (!string.IsNullOrWhiteSpace(Filter)) MapfileSerializer.WriteKeyValues(w, indent+1, "FILTER", MapfileSerializer.Quote(Filter!));
            if (!string.IsNullOrWhiteSpace(FilterItem)) MapfileSerializer.WriteKeyValues(w, indent+1, "FILTERITEM", MapfileSerializer.Quote(FilterItem!));
            if (MinFeatureSize.HasValue) MapfileSerializer.WriteKeyValues(w, indent+1, "MINFEATURESIZE", MinFeatureSize.Value.ToString(CultureInfo.InvariantCulture));
            if (MaxFeatures.HasValue) MapfileSerializer.WriteKeyValues(w, indent+1, "MAXFEATURES", MaxFeatures.Value.ToString(CultureInfo.InvariantCulture));
            if (!string.IsNullOrWhiteSpace(Mask)) MapfileSerializer.WriteKeyValues(w, indent+1, "MASK", MapfileSerializer.Quote(Mask!));
            if (!string.IsNullOrWhiteSpace(StyleItem)) MapfileSerializer.WriteKeyValues(w, indent+1, "STYLEITEM", StyleItem!);
            if (!string.IsNullOrWhiteSpace(GeomTransform)) MapfileSerializer.WriteKeyValues(w, indent+1, "GEOMTRANSFORM", MapfileSerializer.Quote(GeomTransform!));
            if (!string.IsNullOrWhiteSpace(PostLabelCache)) MapfileSerializer.WriteKeyValues(w, indent+1, "POSTLABELCACHE", PostLabelCache!);
            if (!string.IsNullOrWhiteSpace(Requires)) MapfileSerializer.WriteKeyValues(w, indent+1, "REQUIRES", MapfileSerializer.Quote(Requires!));
            if (!string.IsNullOrWhiteSpace(Transform)) MapfileSerializer.WriteKeyValues(w, indent+1, "TRANSFORM", Transform!);
            if (Tolerance.HasValue) MapfileSerializer.WriteKeyValues(w, indent+1, "TOLERANCE", Tolerance.Value.ToString(CultureInfo.InvariantCulture));
            if (!string.IsNullOrWhiteSpace(ToleranceUnits)) MapfileSerializer.WriteKeyValues(w, indent+1, "TOLERANCEUNITS", ToleranceUnits!);

            if (!string.IsNullOrWhiteSpace(Header)) MapfileSerializer.WriteKeyValues(w, indent+1, "HEADER", MapfileSerializer.Quote(Header!));
            if (!string.IsNullOrWhiteSpace(Footer)) MapfileSerializer.WriteKeyValues(w, indent+1, "FOOTER", MapfileSerializer.Quote(Footer!));
            if (!string.IsNullOrWhiteSpace(Template)) MapfileSerializer.WriteKeyValues(w, indent+1, "TEMPLATE", MapfileSerializer.Quote(Template!));

            if (OffsiteColor!=null && OffsiteColor.Length==3) MapfileSerializer.WriteKeyValues(w, indent+1, "OFFSITE", OffsiteColor.Select(i=>i.ToString(CultureInfo.InvariantCulture)).ToArray());
            else if (!string.IsNullOrWhiteSpace(OffsiteHex)) MapfileSerializer.WriteKeyValues(w, indent+1, "OFFSITE", MapfileSerializer.Quote(OffsiteHex!));

            if (Opacity.HasValue) MapfileSerializer.WriteKeyValues(w, indent+1, "OPACITY", Opacity.Value.ToString(CultureInfo.InvariantCulture));
            else if (!string.IsNullOrWhiteSpace(OpacityKeyword)) MapfileSerializer.WriteKeyValues(w, indent+1, "OPACITY", OpacityKeyword!);

            if (!string.IsNullOrWhiteSpace(TileIndex)) MapfileSerializer.WriteKeyValues(w, indent+1, "TILEINDEX", MapfileSerializer.Quote(TileIndex!));
            if (!string.IsNullOrWhiteSpace(TileItem)) MapfileSerializer.WriteKeyValues(w, indent+1, "TILEITEM", MapfileSerializer.Quote(TileItem!));
            if (!string.IsNullOrWhiteSpace(TileFilter)) MapfileSerializer.WriteKeyValues(w, indent+1, "TILEFILTER", MapfileSerializer.Quote(TileFilter!));
            if (!string.IsNullOrWhiteSpace(TileFilterItem)) MapfileSerializer.WriteKeyValues(w, indent+1, "TILEFILTERITEM", MapfileSerializer.Quote(TileFilterItem!));

            foreach (var kv in ConnectionOptions) MapfileSerializer.WriteKeyValues(w, indent+1, "CONNECTIONOPTIONS", MapfileSerializer.Quote(kv.Key), MapfileSerializer.Quote(kv.Value));
            foreach (var p in Processing) MapfileSerializer.WriteKeyValues(w, indent+1, "PROCESSING", MapfileSerializer.Quote(p));

            if (Validation.Count>0){ MapfileSerializer.WriteIndent(w,indent+1); w.WriteLine("VALIDATION"); foreach(var kv in Validation){ MapfileSerializer.WriteIndent(w,indent+2); w.WriteLine($"{MapfileSerializer.Quote(kv.Key)} {MapfileSerializer.Quote(kv.Value)}"); } MapfileSerializer.WriteIndent(w,indent+1); w.WriteLine("END"); }

            if (Identify.Count>0){ MapfileSerializer.WriteIndent(w,indent+1); w.WriteLine("IDENTIFY"); MapfileSerializer.WriteAttributes(w, indent+1, Identify); MapfileSerializer.WriteIndent(w,indent+1); w.WriteLine("END"); }

            foreach (var j in Joins) j.Write(w, indent+1);

            if (Metadata.Count>0){ MapfileSerializer.WriteIndent(w,indent+1); w.WriteLine("METADATA"); foreach(var kv in Metadata){ MapfileSerializer.WriteIndent(w,indent+2); w.WriteLine($"{MapfileSerializer.Quote(kv.Key)} {MapfileSerializer.Quote(kv.Value)}"); } MapfileSerializer.WriteIndent(w,indent+1); w.WriteLine("END"); }

            // FEATURE blocks (if any)
            foreach (var f in Features) f.Write(w, indent+1);

            foreach (var c in Classes) c.Write(w, indent+1);

            if (Projection.Count>0){ MapfileSerializer.WriteIndent(w,indent+1); w.WriteLine("PROJECTION"); foreach(var l in Projection){ MapfileSerializer.WriteIndent(w,indent+2); w.WriteLine(MapfileSerializer.Quote(l)); } MapfileSerializer.WriteIndent(w,indent+1); w.WriteLine("END"); }

            MapfileSerializer.WriteAttributes(w, indent+1, Attributes);
            MapfileSerializer.WriteIndent(w, indent); w.WriteLine("END");
        }
    }

    public class JoinObj
    {
        public string? Name { get; set; }
        public string? Table { get; set; }
        public string? From { get; set; }
        public string? To { get; set; }
        public string? Type { get; set; }
        public string? Template { get; set; }
        public Dictionary<string,List<string[]>> Attributes { get; } = new(StringComparer.OrdinalIgnoreCase);
        public void Write(TextWriter w, int indent){ MapfileSerializer.WriteIndent(w,indent); w.WriteLine("JOIN"); if(!string.IsNullOrWhiteSpace(Name)) MapfileSerializer.WriteKeyValues(w,indent+1,"NAME",MapfileSerializer.Quote(Name!)); if(!string.IsNullOrWhiteSpace(Table)) MapfileSerializer.WriteKeyValues(w,indent+1,"TABLE",MapfileSerializer.Quote(Table!)); if(!string.IsNullOrWhiteSpace(From)) MapfileSerializer.WriteKeyValues(w,indent+1,"FROM",MapfileSerializer.Quote(From!)); if(!string.IsNullOrWhiteSpace(To)) MapfileSerializer.WriteKeyValues(w,indent+1,"TO",MapfileSerializer.Quote(To!)); if(!string.IsNullOrWhiteSpace(Type)) MapfileSerializer.WriteKeyValues(w,indent+1,"TYPE",Type!); if(!string.IsNullOrWhiteSpace(Template)) MapfileSerializer.WriteKeyValues(w,indent+1,"TEMPLATE",MapfileSerializer.Quote(Template!)); MapfileSerializer.WriteAttributes(w, indent+1, Attributes); MapfileSerializer.WriteIndent(w,indent); w.WriteLine("END"); }
    }

    // ===== FEATURE =====
    public class FeatureObj
    {
        // We store the inner lines of the FEATURE block as-is to preserve formatting/content (POINTS/ITEMS/WKT etc.).
        public List<string> InnerLines { get; } = new();
        public Dictionary<string,List<string[]>> Attributes { get; } = new(StringComparer.OrdinalIgnoreCase);

        public void Write(TextWriter w, int indent)
        {
            MapfileSerializer.WriteIndent(w, indent); w.WriteLine("FEATURE");
            // Write inner lines exactly as captured (no extra indentation to avoid double-indenting)
            foreach (var line in InnerLines)
            {
                w.WriteLine(line);
            }
            MapfileSerializer.WriteIndent(w, indent); w.WriteLine("END");
        }
    }

    // ===== CLASS, STYLE, LABEL =====
    public class ClassObj
    {
        public string? Name { get; set; }      // NAME
        public string? Title { get; set; }     // TITLE (legend label)
        public string? Status { get; set; }    // STATUS on|off
        public string? Group { get; set; }     // GROUP (class grouping)
        public string? Expression { get; set; }// EXPRESSION
        public string? Text { get; set; }      // TEXT (string|expression)
        public string? Template { get; set; }  // TEMPLATE
        public string? KeyImage { get; set; }  // KEYIMAGE
        public double? MinScaleDenom { get; set; } // MINSCALEDENOM
        public double? MaxScaleDenom { get; set; } // MAXSCALEDENOM
        public int? MinFeatureSize { get; set; }    // MINFEATURESIZE
        public string? Debug { get; set; }     // DEBUG 0..5|on|off
        public bool? Fallback { get; set; }    // FALLBACK true|false (8.6)

        public List<StyleObj> Styles { get; } = new();
        public List<LabelObj> Labels { get; } = new();
        public LeaderObj? Leader { get; set; } // optional LEADER block

        public Dictionary<string,string> Metadata { get; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string,string> Validation { get; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string,List<string[]>> Attributes { get; } = new(StringComparer.OrdinalIgnoreCase);

        public void Write(TextWriter w, int indent)
        {
            MapfileSerializer.WriteIndent(w,indent); w.WriteLine("CLASS");
            if (!string.IsNullOrWhiteSpace(Name)) MapfileSerializer.WriteKeyValues(w, indent+1, "NAME", MapfileSerializer.Quote(Name!));
            if (!string.IsNullOrWhiteSpace(Title)) MapfileSerializer.WriteKeyValues(w, indent+1, "TITLE", MapfileSerializer.Quote(Title!));
            if (!string.IsNullOrWhiteSpace(Status)) MapfileSerializer.WriteKeyValues(w, indent+1, "STATUS", Status!);
            if (!string.IsNullOrWhiteSpace(Group)) MapfileSerializer.WriteKeyValues(w, indent+1, "GROUP", MapfileSerializer.Quote(Group!));
            if (!string.IsNullOrWhiteSpace(Expression)) MapfileSerializer.WriteKeyValues(w, indent+1, "EXPRESSION", MapfileSerializer.Quote(Expression!));
            if (!string.IsNullOrWhiteSpace(Text)) MapfileSerializer.WriteKeyValues(w, indent+1, "TEXT", MapfileSerializer.Quote(Text!));
            if (!string.IsNullOrWhiteSpace(Template)) MapfileSerializer.WriteKeyValues(w, indent+1, "TEMPLATE", MapfileSerializer.Quote(Template!));
            if (!string.IsNullOrWhiteSpace(KeyImage)) MapfileSerializer.WriteKeyValues(w, indent+1, "KEYIMAGE", MapfileSerializer.Quote(KeyImage!));
            if (MinScaleDenom.HasValue) MapfileSerializer.WriteKeyValues(w, indent+1, "MINSCALEDENOM", MinScaleDenom.Value.ToString(CultureInfo.InvariantCulture));
            if (MaxScaleDenom.HasValue) MapfileSerializer.WriteKeyValues(w, indent+1, "MAXSCALEDENOM", MaxScaleDenom.Value.ToString(CultureInfo.InvariantCulture));
            if (MinFeatureSize.HasValue) MapfileSerializer.WriteKeyValues(w, indent+1, "MINFEATURESIZE", MinFeatureSize.Value.ToString(CultureInfo.InvariantCulture));
            if (!string.IsNullOrWhiteSpace(Debug)) MapfileSerializer.WriteKeyValues(w, indent+1, "DEBUG", Debug!);
            if (Fallback.HasValue) MapfileSerializer.WriteKeyValues(w, indent+1, "FALLBACK", Fallback.Value ? "TRUE" : "FALSE");

            if (Metadata.Count>0){ MapfileSerializer.WriteIndent(w, indent+1); w.WriteLine("METADATA"); foreach(var kv in Metadata){ MapfileSerializer.WriteIndent(w, indent+2); w.WriteLine($"{MapfileSerializer.Quote(kv.Key)} {MapfileSerializer.Quote(kv.Value)}"); } MapfileSerializer.WriteIndent(w, indent+1); w.WriteLine("END"); }
            if (Validation.Count>0){ MapfileSerializer.WriteIndent(w, indent+1); w.WriteLine("VALIDATION"); foreach(var kv in Validation){ MapfileSerializer.WriteIndent(w, indent+2); w.WriteLine($"{MapfileSerializer.Quote(kv.Key)} {MapfileSerializer.Quote(kv.Value)}"); } MapfileSerializer.WriteIndent(w, indent+1); w.WriteLine("END"); }

            foreach (var s in Styles) s.Write(w, indent+1);
            foreach (var l in Labels) l.Write(w, indent+1);
            Leader?.Write(w, indent+1);

            MapfileSerializer.WriteAttributes(w, indent+1, Attributes);
            MapfileSerializer.WriteIndent(w,indent); w.WriteLine("END");
        }
    }

    public class LeaderObj
    {
        public int? GridStep { get; set; }     // GRIDSTEP
        public int? MaxDistance { get; set; }  // MAXDISTANCE
        public List<StyleObj> Styles { get; } = new();
        public Dictionary<string,List<string[]>> Attributes { get; } = new(StringComparer.OrdinalIgnoreCase);
        public void Write(TextWriter w, int indent)
        {
            MapfileSerializer.WriteIndent(w, indent); w.WriteLine("LEADER");
            if (GridStep.HasValue) MapfileSerializer.WriteKeyValues(w, indent+1, "GRIDSTEP", GridStep.Value.ToString(CultureInfo.InvariantCulture));
            if (MaxDistance.HasValue) MapfileSerializer.WriteKeyValues(w, indent+1, "MAXDISTANCE", MaxDistance.Value.ToString(CultureInfo.InvariantCulture));
            foreach (var s in Styles) s.Write(w, indent+1);
            MapfileSerializer.WriteAttributes(w, indent+1, Attributes);
            MapfileSerializer.WriteIndent(w, indent); w.WriteLine("END");
        }
    }

    public class StyleObj
    {
        // COLOR (RGB/hex/attr)
        public int[]? Color { get; set; }
        public string? ColorHex { get; set; }
        public string? ColorAttr { get; set; }

        public int[]? OutlineColor { get; set; }
        public string? OutlineColorHex { get; set; }
        public string? OutlineColorAttr { get; set; }
        public double? OutlineWidth { get; set; }

        public string? Symbol { get; set; }

        public string? Angle { get; set; } // numeric/AUTO/[attr]
        public double? Width { get; set; }
        public double? Size { get; set; }
        public string? SizeExpr { get; set; } // [attr] or expression

        public double? MinScaleDenom { get; set; }
        public double? MaxScaleDenom { get; set; }
        public double? MinWidth { get; set; }
        public double? MaxWidth { get; set; }
        public double? MinSize { get; set; }
        public double? MaxSize { get; set; }

        public double? Gap { get; set; }
        public double? InitialGap { get; set; }

        public string? OffsetX { get; set; }  // allow attribute binding
        public string? OffsetY { get; set; }
        public string? PolarOffsetR { get; set; }
        public string? PolarOffsetA { get; set; }

        public string? LineCap { get; set; }
        public string? LineJoin { get; set; }
        public int? LineJoinMaxSize { get; set; }

        public List<double> Pattern { get; } = new();

        public string? GeomTransform { get; set; }
        public string? Opacity { get; set; } // numeric or [attr]

        public string? RangeItem { get; set; }
        public (int[]? rgb,string? hex) ColorRangeLow;
        public (int[]? rgb,string? hex) ColorRangeHigh;
        public (double? low,double? high) DataRange;

        public Dictionary<string,List<string[]>> Attributes { get; } = new(StringComparer.OrdinalIgnoreCase);

        public void Write(TextWriter w, int indent)
        {
            MapfileSerializer.WriteIndent(w, indent); w.WriteLine("STYLE");
            // COLOR
            if (Color != null && Color.Length==3) MapfileSerializer.WriteKeyValues(w, indent+1, "COLOR", Color.Select(i=>i.ToString(CultureInfo.InvariantCulture)).ToArray());
            else if (!string.IsNullOrWhiteSpace(ColorHex)) MapfileSerializer.WriteKeyValues(w, indent+1, "COLOR", MapfileSerializer.Quote(ColorHex!));
            else if (!string.IsNullOrWhiteSpace(ColorAttr)) MapfileSerializer.WriteKeyValues(w, indent+1, "COLOR", ColorAttr!);

            if (OutlineColor != null && OutlineColor.Length==3) MapfileSerializer.WriteKeyValues(w, indent+1, "OUTLINECOLOR", OutlineColor.Select(i=>i.ToString(CultureInfo.InvariantCulture)).ToArray());
            else if (!string.IsNullOrWhiteSpace(OutlineColorHex)) MapfileSerializer.WriteKeyValues(w, indent+1, "OUTLINECOLOR", MapfileSerializer.Quote(OutlineColorHex!));
            else if (!string.IsNullOrWhiteSpace(OutlineColorAttr)) MapfileSerializer.WriteKeyValues(w, indent+1, "OUTLINECOLOR", OutlineColorAttr!);
            if (OutlineWidth.HasValue) MapfileSerializer.WriteKeyValues(w, indent+1, "OUTLINEWIDTH", OutlineWidth.Value.ToString(CultureInfo.InvariantCulture));

            if (!string.IsNullOrWhiteSpace(Symbol)) MapfileSerializer.WriteKeyValues(w, indent+1, "SYMBOL", MapfileSerializer.MaybeQuote(Symbol!));
            if (!string.IsNullOrWhiteSpace(Angle)) MapfileSerializer.WriteKeyValues(w, indent+1, "ANGLE", Angle!);
            if (Width.HasValue) MapfileSerializer.WriteKeyValues(w, indent+1, "WIDTH", Width.Value.ToString(CultureInfo.InvariantCulture));
            if (Size.HasValue) MapfileSerializer.WriteKeyValues(w, indent+1, "SIZE", Size.Value.ToString(CultureInfo.InvariantCulture));
            else if (!string.IsNullOrWhiteSpace(SizeExpr)) MapfileSerializer.WriteKeyValues(w, indent+1, "SIZE", SizeExpr!);

            if (MinScaleDenom.HasValue) MapfileSerializer.WriteKeyValues(w, indent+1, "MINSCALEDENOM", MinScaleDenom.Value.ToString(CultureInfo.InvariantCulture));
            if (MaxScaleDenom.HasValue) MapfileSerializer.WriteKeyValues(w, indent+1, "MAXSCALEDENOM", MaxScaleDenom.Value.ToString(CultureInfo.InvariantCulture));
            if (MinWidth.HasValue) MapfileSerializer.WriteKeyValues(w, indent+1, "MINWIDTH", MinWidth.Value.ToString(CultureInfo.InvariantCulture));
            if (MaxWidth.HasValue) MapfileSerializer.WriteKeyValues(w, indent+1, "MAXWIDTH", MaxWidth.Value.ToString(CultureInfo.InvariantCulture));
            if (MinSize.HasValue) MapfileSerializer.WriteKeyValues(w, indent+1, "MINSIZE", MinSize.Value.ToString(CultureInfo.InvariantCulture));
            if (MaxSize.HasValue) MapfileSerializer.WriteKeyValues(w, indent+1, "MAXSIZE", MaxSize.Value.ToString(CultureInfo.InvariantCulture));

            if (Gap.HasValue) MapfileSerializer.WriteKeyValues(w, indent+1, "GAP", Gap.Value.ToString(CultureInfo.InvariantCulture));
            if (InitialGap.HasValue) MapfileSerializer.WriteKeyValues(w, indent+1, "INITIALGAP", InitialGap.Value.ToString(CultureInfo.InvariantCulture));

            if (!string.IsNullOrWhiteSpace(OffsetX) || !string.IsNullOrWhiteSpace(OffsetY))
            {
                MapfileSerializer.WriteKeyValues(w, indent+1, "OFFSET", (OffsetX??"0"), (OffsetY??"0"));
            }
            if (!string.IsNullOrWhiteSpace(PolarOffsetR) || !string.IsNullOrWhiteSpace(PolarOffsetA))
            {
                MapfileSerializer.WriteKeyValues(w, indent+1, "POLAROFFSET", (PolarOffsetR??"0"), (PolarOffsetA??"0"));
            }

            if (!string.IsNullOrWhiteSpace(LineCap)) MapfileSerializer.WriteKeyValues(w, indent+1, "LINECAP", LineCap!);
            if (!string.IsNullOrWhiteSpace(LineJoin)) MapfileSerializer.WriteKeyValues(w, indent+1, "LINEJOIN", LineJoin!);
            if (LineJoinMaxSize.HasValue) MapfileSerializer.WriteKeyValues(w, indent+1, "LINEJOINMAXSIZE", LineJoinMaxSize.Value.ToString(CultureInfo.InvariantCulture));

            if (Pattern.Count>0)
            {
                MapfileSerializer.WriteKeyValues(w, indent+1, "PATTERN");
                MapfileSerializer.WriteIndent(w, indent+2);
                for (int i=0;i<Pattern.Count;i++)
                {
                    if (i>0) w.Write(' ');
                    w.Write(Pattern[i].ToString(CultureInfo.InvariantCulture));
                }
                w.WriteLine();
                MapfileSerializer.WriteIndent(w, indent+1); w.WriteLine("END");
            }

            if (!string.IsNullOrWhiteSpace(GeomTransform)) MapfileSerializer.WriteKeyValues(w, indent+1, "GEOMTRANSFORM", MapfileSerializer.Quote(GeomTransform!));
            if (!string.IsNullOrWhiteSpace(Opacity)) MapfileSerializer.WriteKeyValues(w, indent+1, "OPACITY", Opacity!);

            if (!string.IsNullOrWhiteSpace(RangeItem)) MapfileSerializer.WriteKeyValues(w, indent+1, "RANGEITEM", MapfileSerializer.Quote(RangeItem!));
            if ((ColorRangeLow.rgb!=null||!string.IsNullOrWhiteSpace(ColorRangeLow.hex)) && (ColorRangeHigh.rgb!=null||!string.IsNullOrWhiteSpace(ColorRangeHigh.hex)))
            {
                string low = ColorRangeLow.rgb!=null? string.Join(' ', ColorRangeLow.rgb.Select(i=>i.ToString(CultureInfo.InvariantCulture))) : MapfileSerializer.Quote(ColorRangeLow.hex!);
                string high = ColorRangeHigh.rgb!=null? string.Join(' ', ColorRangeHigh.rgb.Select(i=>i.ToString(CultureInfo.InvariantCulture))) : MapfileSerializer.Quote(ColorRangeHigh.hex!);
                MapfileSerializer.WriteKeyValues(w, indent+1, "COLORRANGE", low, high);
            }
            if (DataRange.low.HasValue && DataRange.high.HasValue)
            {
                MapfileSerializer.WriteKeyValues(w, indent+1, "DATARANGE", DataRange.low.Value.ToString(CultureInfo.InvariantCulture), DataRange.high.Value.ToString(CultureInfo.InvariantCulture));
            }

            MapfileSerializer.WriteAttributes(w, indent+1, Attributes);
            MapfileSerializer.WriteIndent(w, indent); w.WriteLine("END");
        }
    }

    public class LabelObj
    {
        public string? Align { get; set; }            // ALIGN left|center|right|[attr]
        public string? Angle { get; set; }            // ANGLE auto|auto2|follow|deg|[attr]
        public int? Buffer { get; set; }              // BUFFER

        public int[]? Color { get; set; }
        public string? ColorHex { get; set; }
        public string? ColorAttr { get; set; }

        public int[]? OutlineColor { get; set; }
        public string? OutlineColorHex { get; set; }
        public string? OutlineColorAttr { get; set; }
        public int? OutlineWidth { get; set; }

        public string? Font { get; set; }             // FONT name or [attr]
        public string? Type { get; set; }             // TYPE bitmap|truetype
        public string? Size { get; set; }             // SIZE int|keyword|[attr]|(expr)

        public double? MaxOverlapAngle { get; set; }
        public double? MinScaleDenom { get; set; }
        public double? MaxScaleDenom { get; set; }
        public int? MinDistance { get; set; }
        public string? MinFeatureSize { get; set; }   // int|auto
        public int? MinSize { get; set; }
        public int? MaxSize { get; set; }

        public string? OffsetX { get; set; }          // OFFSET x y (supports [attr])
        public string? OffsetY { get; set; }

        public string? Position { get; set; }         // POSITION ul|...|auto|[attr]
        public string? Priority { get; set; }         // PRIORITY int|[attr]|(expr)
        public string? Partials { get; set; }         // true|false
        public string? Force { get; set; }            // true|false|group

        public int? RepeatDistance { get; set; }
        public int[]? ShadowColor { get; set; }       // optional
        public string? ShadowColorHex { get; set; }
        public string? ShadowSizeX { get; set; }
        public string? ShadowSizeY { get; set; }

        public string? Text { get; set; }             // TEXT string|expression
        public string? Wrap { get; set; }             // WRAP char
        public int? MaxLength { get; set; }           // MAXLENGTH

        public List<StyleObj> Styles { get; } = new(); // label styling via STYLE GEOMTRANSFORM
        public Dictionary<string,List<string[]>> Attributes { get; } = new(StringComparer.OrdinalIgnoreCase);

        public void Write(TextWriter w, int indent)
        {
            MapfileSerializer.WriteIndent(w, indent); w.WriteLine("LABEL");
            if (!string.IsNullOrWhiteSpace(Align)) MapfileSerializer.WriteKeyValues(w, indent+1, "ALIGN", Align!);
            if (!string.IsNullOrWhiteSpace(Angle)) MapfileSerializer.WriteKeyValues(w, indent+1, "ANGLE", Angle!);
            if (Buffer.HasValue) MapfileSerializer.WriteKeyValues(w, indent+1, "BUFFER", Buffer.Value.ToString(CultureInfo.InvariantCulture));

            if (Color != null && Color.Length==3) MapfileSerializer.WriteKeyValues(w, indent+1, "COLOR", Color.Select(i=>i.ToString(CultureInfo.InvariantCulture)).ToArray());
            else if (!string.IsNullOrWhiteSpace(ColorHex)) MapfileSerializer.WriteKeyValues(w, indent+1, "COLOR", MapfileSerializer.Quote(ColorHex!));
            else if (!string.IsNullOrWhiteSpace(ColorAttr)) MapfileSerializer.WriteKeyValues(w, indent+1, "COLOR", ColorAttr!);

            if (OutlineColor != null && OutlineColor.Length==3) MapfileSerializer.WriteKeyValues(w, indent+1, "OUTLINECOLOR", OutlineColor.Select(i=>i.ToString(CultureInfo.InvariantCulture)).ToArray());
            else if (!string.IsNullOrWhiteSpace(OutlineColorHex)) MapfileSerializer.WriteKeyValues(w, indent+1, "OUTLINECOLOR", MapfileSerializer.Quote(OutlineColorHex!));
            else if (!string.IsNullOrWhiteSpace(OutlineColorAttr)) MapfileSerializer.WriteKeyValues(w, indent+1, "OUTLINECOLOR", OutlineColorAttr!);
            if (OutlineWidth.HasValue) MapfileSerializer.WriteKeyValues(w, indent+1, "OUTLINEWIDTH", OutlineWidth.Value.ToString(CultureInfo.InvariantCulture));

            if (!string.IsNullOrWhiteSpace(Font)) MapfileSerializer.WriteKeyValues(w, indent+1, "FONT", MapfileSerializer.MaybeQuote(Font!));
            if (!string.IsNullOrWhiteSpace(Type)) MapfileSerializer.WriteKeyValues(w, indent+1, "TYPE", Type!);
            if (!string.IsNullOrWhiteSpace(Size)) MapfileSerializer.WriteKeyValues(w, indent+1, "SIZE", Size!);

            if (MaxOverlapAngle.HasValue) MapfileSerializer.WriteKeyValues(w, indent+1, "MAXOVERLAPANGLE", MaxOverlapAngle.Value.ToString(CultureInfo.InvariantCulture));
            if (MinScaleDenom.HasValue) MapfileSerializer.WriteKeyValues(w, indent+1, "MINSCALEDENOM", MinScaleDenom.Value.ToString(CultureInfo.InvariantCulture));
            if (MaxScaleDenom.HasValue) MapfileSerializer.WriteKeyValues(w, indent+1, "MAXSCALEDENOM", MaxScaleDenom.Value.ToString(CultureInfo.InvariantCulture));
            if (MinDistance.HasValue) MapfileSerializer.WriteKeyValues(w, indent+1, "MINDISTANCE", MinDistance.Value.ToString(CultureInfo.InvariantCulture));
            if (!string.IsNullOrWhiteSpace(MinFeatureSize)) MapfileSerializer.WriteKeyValues(w, indent+1, "MINFEATURESIZE", MinFeatureSize!);
            if (MinSize.HasValue) MapfileSerializer.WriteKeyValues(w, indent+1, "MINSIZE", MinSize.Value.ToString(CultureInfo.InvariantCulture));
            if (MaxSize.HasValue) MapfileSerializer.WriteKeyValues(w, indent+1, "MAXSIZE", MaxSize.Value.ToString(CultureInfo.InvariantCulture));

            if (!string.IsNullOrWhiteSpace(OffsetX) || !string.IsNullOrWhiteSpace(OffsetY)) MapfileSerializer.WriteKeyValues(w, indent+1, "OFFSET", (OffsetX??"0"), (OffsetY??"0"));
            if (!string.IsNullOrWhiteSpace(Position)) MapfileSerializer.WriteKeyValues(w, indent+1, "POSITION", Position!);
            if (!string.IsNullOrWhiteSpace(Priority)) MapfileSerializer.WriteKeyValues(w, indent+1, "PRIORITY", Priority!);
            if (!string.IsNullOrWhiteSpace(Partials)) MapfileSerializer.WriteKeyValues(w, indent+1, "PARTIALS", Partials!);
            if (!string.IsNullOrWhiteSpace(Force)) MapfileSerializer.WriteKeyValues(w, indent+1, "FORCE", Force!);

            if (RepeatDistance.HasValue) MapfileSerializer.WriteKeyValues(w, indent+1, "REPEATDISTANCE", RepeatDistance.Value.ToString(CultureInfo.InvariantCulture));
            if (ShadowColor != null && ShadowColor.Length==3) MapfileSerializer.WriteKeyValues(w, indent+1, "SHADOWCOLOR", ShadowColor.Select(i=>i.ToString(CultureInfo.InvariantCulture)).ToArray());
            else if (!string.IsNullOrWhiteSpace(ShadowColorHex)) MapfileSerializer.WriteKeyValues(w, indent+1, "SHADOWCOLOR", MapfileSerializer.Quote(ShadowColorHex!));
            if (!string.IsNullOrWhiteSpace(ShadowSizeX) || !string.IsNullOrWhiteSpace(ShadowSizeY)) MapfileSerializer.WriteKeyValues(w, indent+1, "SHADOWSIZE", (ShadowSizeX??"0"), (ShadowSizeY??"0"));

            if (!string.IsNullOrWhiteSpace(Text)) MapfileSerializer.WriteKeyValues(w, indent+1, "TEXT", MapfileSerializer.Quote(Text!));
            if (!string.IsNullOrWhiteSpace(Wrap)) MapfileSerializer.WriteKeyValues(w, indent+1, "WRAP", MapfileSerializer.Quote(Wrap!));
            if (MaxLength.HasValue) MapfileSerializer.WriteKeyValues(w, indent+1, "MAXLENGTH", MaxLength.Value.ToString(CultureInfo.InvariantCulture));

            foreach (var s in Styles) s.Write(w, indent+1);
            MapfileSerializer.WriteAttributes(w, indent+1, Attributes);
            MapfileSerializer.WriteIndent(w, indent); w.WriteLine("END");
        }
    }

    #endregion

    #region Parser

    internal static class MapfileParser
    {
        private enum CtxType { Map, Web, Metadata, Projection, OutputFormat, Layer, Class, Style, Label, Leader, Validation, Join, Identify }
        private sealed class Context{ public CtxType Type { get; } public object Node { get; } public Context(CtxType t, object n){ Type=t; Node=n; } }

        public static MapObj Parse(string text, string? baseDir)
        {
            // Expand INCLUDE directives if we know where to resolve them from
            text = ExpandIncludes(text, baseDir, new Stack<string>());

            var map = new MapObj();
            var stack = new Stack<Context>();
            stack.Push(new Context(CtxType.Map, map));

            using var sr = new StringReader(text);
            string? raw; int lineNo=0;
            while((raw=sr.ReadLine())!=null)
            {
                lineNo++;
                var line = StripComments(raw);
                if (string.IsNullOrWhiteSpace(line)) continue;
                var tokens = Tokenize(line);
                if (tokens.Count==0) continue;
                var head = tokens[0].ToUpperInvariant();

                // Special handling: FEATURE blocks inside LAYER
                if (head=="FEATURE")
                {
                    var ctxFeat = stack.Peek();
                    if (ctxFeat.Type!=CtxType.Layer)
                        throw new FormatException($"FEATURE outside LAYER at line {lineNo}");

                    var f = new FeatureObj();
                    // Read inner lines until matching END (POINTS/ITEMS sub-blocks may appear)
                    int depth = 0; // for POINTS/ITEMS sub-blocks
                    bool foundFeatureEnd = false;
                    while((raw=sr.ReadLine())!=null)
                    {
                        lineNo++;
                        var innerRaw = raw; // keep as-is
                        var innerStripped = StripComments(innerRaw);
                        var innerTokens = Tokenize(innerStripped);
                        if (innerTokens.Count>0)
                        {
                            var innerHead = innerTokens[0].ToUpperInvariant();
                            if (innerHead=="POINTS" || innerHead=="ITEMS")
                            {
                                depth++;
                                f.InnerLines.Add(innerRaw);
                                continue;
                            }
                            if (innerHead=="END")
                            {
                                if (depth==0)
                                {
                                    // This END closes the FEATURE block
                                    foundFeatureEnd = true;
                                    break; // stop reading feature
                                }
                                else
                                {
                                    depth--; f.InnerLines.Add(innerRaw); continue;
                                }
                            }
                        }
                        f.InnerLines.Add(innerRaw);
                    }

                    if (!foundFeatureEnd)
                        throw new FormatException($"FEATURE block started at line {lineNo - f.InnerLines.Count - 1} is missing closing END");
                    if (depth!=0)
                        throw new FormatException($"FEATURE block has unbalanced POINTS/ITEMS blocks (depth={depth}) at line {lineNo}");

                    ((LayerObj)ctxFeat.Node).Features.Add(f);
                    // FEATURE handled, continue with next outer line
                    continue;
                }

                if (head=="END") { if (stack.Count==0) throw new FormatException($"Unexpected END at line {lineNo}"); stack.Pop(); continue; }

                if (stack.Count==0) continue; // Allow final END to pop MAP context
                var ctx = stack.Peek();

                // Open blocks
                if (head=="MAP") { if (ctx.Type!=CtxType.Map || (ctx.Node as MapObj)==null) throw new FormatException($"Nested MAP not allowed at line {lineNo}"); continue; }
                if (head=="WEB") { var web=(ctx.Node as MapObj)!.Web; stack.Push(new Context(CtxType.Web, web)); continue; }
                if (head=="METADATA")
                {
                    switch(ctx.Type)
                    {
                        case CtxType.Web: stack.Push(new Context(CtxType.Metadata, ((WebObj)ctx.Node).Metadata)); continue;
                        case CtxType.Layer: stack.Push(new Context(CtxType.Metadata, ((LayerObj)ctx.Node).Metadata)); continue;
                        case CtxType.Class: stack.Push(new Context(CtxType.Metadata, ((ClassObj)ctx.Node).Metadata)); continue;
                        default: throw new FormatException($"METADATA not allowed here (line {lineNo})");
                    }
                }
                if (head=="VALIDATION")
                {
                    switch(ctx.Type)
                    {
                        case CtxType.Layer: stack.Push(new Context(CtxType.Validation, ((LayerObj)ctx.Node).Validation)); continue;
                        case CtxType.Class: stack.Push(new Context(CtxType.Validation, ((ClassObj)ctx.Node).Validation)); continue;
                        default: throw new FormatException($"VALIDATION not allowed here (line {lineNo})");
                    }
                }
                if (head=="PROJECTION")
                {
                    if (ctx.Type==CtxType.Map) { stack.Push(new Context(CtxType.Projection, ((MapObj)ctx.Node).Projection)); continue; }
                    if (ctx.Type==CtxType.Layer) { stack.Push(new Context(CtxType.Projection, ((LayerObj)ctx.Node).Projection)); continue; }
                    throw new FormatException($"PROJECTION not allowed here (line {lineNo})");
                }
                if (head=="OUTPUTFORMAT") { var of=new OutputFormatObj(); ((MapObj)ctx.Node).OutputFormats.Add(of); stack.Push(new Context(CtxType.OutputFormat, of)); continue; }
                if (head=="LAYER") { var layer=new LayerObj(); ((MapObj)ctx.Node).Layers.Add(layer); stack.Push(new Context(CtxType.Layer, layer)); continue; }
                if (head=="CLASS") { if (ctx.Type!=CtxType.Layer) throw new FormatException($"CLASS outside LAYER at line {lineNo}"); var c=new ClassObj(); ((LayerObj)ctx.Node).Classes.Add(c); stack.Push(new Context(CtxType.Class, c)); continue; }
                if (head=="STYLE")
                {
                    if (ctx.Type==CtxType.Class) { var s=new StyleObj(); ((ClassObj)ctx.Node).Styles.Add(s); stack.Push(new Context(CtxType.Style, s)); continue; }
                    if (ctx.Type==CtxType.Label) { var s=new StyleObj(); ((LabelObj)ctx.Node).Styles.Add(s); stack.Push(new Context(CtxType.Style, s)); continue; }
                    if (ctx.Type==CtxType.Leader){ var s=new StyleObj(); ((LeaderObj)ctx.Node).Styles.Add(s); stack.Push(new Context(CtxType.Style, s)); continue; }
                    throw new FormatException($"STYLE not allowed here (line {lineNo})");
                }
                if (head=="LABEL")
                {
                    if (ctx.Type!=CtxType.Class) throw new FormatException($"LABEL outside CLASS at line {lineNo}");
                    var l = new LabelObj(); ((ClassObj)ctx.Node).Labels.Add(l); stack.Push(new Context(CtxType.Label, l)); continue;
                }
                if (head=="LEADER")
                {
                    if (ctx.Type!=CtxType.Class) throw new FormatException($"LEADER outside CLASS at line {lineNo}");
                    var ld = new LeaderObj(); ((ClassObj)ctx.Node).Leader = ld; stack.Push(new Context(CtxType.Leader, ld)); continue;
                }
                if (head=="JOIN")
                {
                    if (ctx.Type!=CtxType.Layer) throw new FormatException($"JOIN outside LAYER at line {lineNo}");
                    var j=new JoinObj(); ((LayerObj)ctx.Node).Joins.Add(j); stack.Push(new Context(CtxType.Join, j)); continue;
                }
                if (head=="IDENTIFY")
                {
                    if (ctx.Type!=CtxType.Layer) throw new FormatException($"IDENTIFY outside LAYER at line {lineNo}");
                    stack.Push(new Context(CtxType.Identify, ((LayerObj)ctx.Node).Identify)); continue;
                }

                // Content by context
                switch(ctx.Type)
                {
                    case CtxType.Metadata: ParseMetadataLine((Dictionary<string,string>)ctx.Node, tokens, lineNo); break;
                    case CtxType.Validation: ParseValidationLine((Dictionary<string,string>)ctx.Node, tokens, lineNo); break;
                    case CtxType.Projection: ParseProjectionLine((List<string>)ctx.Node, tokens); break;
                    case CtxType.OutputFormat: ApplyKeyValuesToOutputFormat((OutputFormatObj)ctx.Node, tokens); break;
                    case CtxType.Web: AddToAttributes(((WebObj)ctx.Node).Attributes, tokens); break;
                    case CtxType.Layer: ApplyKeyValuesToLayer((LayerObj)ctx.Node, tokens); break;
                    case CtxType.Class: ApplyKeyValuesToClass((ClassObj)ctx.Node, tokens); break;
                    case CtxType.Style: ApplyKeyValuesToStyle((StyleObj)ctx.Node, tokens); break;
                    case CtxType.Label: ApplyKeyValuesToLabel((LabelObj)ctx.Node, tokens); break;
                    case CtxType.Leader: ApplyKeyValuesToLeader((LeaderObj)ctx.Node, tokens); break;
                    case CtxType.Map: ApplyKeyValuesToMap((MapObj)ctx.Node, tokens); break;
                    case CtxType.Join: ApplyKeyValuesToJoin((JoinObj)ctx.Node, tokens); break;
                    case CtxType.Identify: AddToAttributes((Dictionary<string,List<string[]>>)ctx.Node, tokens); break;
                    default: throw new NotSupportedException($"Unhandled context at line {lineNo}");
                }
            }

            if (stack.Count!=0) throw new FormatException("Unbalanced blocks: missing END(s)");
            return map;
        }

        private static void ParseMetadataLine(Dictionary<string,string> dict, List<string> tokens, int lineNo)
        {
            if (tokens.Count>=2)
            {
                var key = Unquote(tokens[0]);
                var val = string.Join(" ", tokens.Skip(1).Select(Unquote));
                dict[key]=val;
            }
            else throw new FormatException($"Invalid METADATA entry at line {lineNo}");
        }
        private static void ParseValidationLine(Dictionary<string,string> dict, List<string> tokens, int lineNo)
        {
            if (tokens.Count>=2)
            {
                var key = Unquote(tokens[0]);
                var val = string.Join(" ", tokens.Skip(1).Select(Unquote));
                dict[key]=val;
            }
            else throw new FormatException($"Invalid VALIDATION entry at line {lineNo}");
        }
        private static void ParseProjectionLine(List<string> list, List<string> tokens)
        { var text = string.Join(" ", tokens.Select(Unquote)).Trim(); if (!string.IsNullOrEmpty(text)) list.Add(text); }

        private static void ApplyKeyValuesToMap(MapObj map, List<string> tokens)
        {
            var key = tokens[0].ToUpperInvariant(); var vals = tokens.Skip(1).ToList(); var joined = string.Join(" ", vals);
            switch(key)
            {
                case "NAME": map.Name=Unquote(joined); break;
                case "STATUS": map.Status=joined.ToUpperInvariant(); break;
                case "EXTENT": map.Extent=ParseDoubles(vals,4); break;
                case "SIZE": map.Size=ParseInts(vals,2); break;
                case "UNITS": map.Units=joined; break;
                case "IMAGETYPE": map.ImageType=Unquote(joined); break;
                case "IMAGECOLOR": if (vals.Count>=1 && vals[0].StartsWith("#")) map.ImageColorHex=Unquote(vals[0]); else if (vals.Count>=3) map.ImageColor=ParseInts(vals,3); else AddToAttributes(map.Attributes, tokens); break;
                case "SHAPEPATH": map.ShapePath=Unquote(joined); break;
                case "SYMBOLSET": map.SymbolSet=Unquote(joined); break;
                case "FONTSET": map.FontSet=Unquote(joined); break;
                case "RESOLUTION": map.Resolution=(int)ParseDouble(vals); break;
                case "DEFRESOLUTION": map.DefResolution=(int)ParseDouble(vals); break;
                case "MAXSIZE": map.MaxSize=(int)ParseDouble(vals); break;
                case "ANGLE": map.Angle=ParseDouble(vals); break;
                case "TEMPLATEPATTERN": map.TemplatePattern=Unquote(joined); break;
                case "DATAPATTERN": map.DataPattern=Unquote(joined); break;
                case "DEBUG": map.Debug=joined; break;
                case "TRANSPARENT": map.Transparent=joined.ToUpperInvariant(); break;
                case "CONFIG": if (vals.Count>=2){ var ck=Unquote(vals[0]); var cv=Unquote(string.Join(" ", vals.Skip(1))); map.Config.Add(new KeyValuePair<string,string>(ck,cv)); } else AddToAttributes(map.Attributes, tokens); break;
                default: AddToAttributes(map.Attributes, tokens); break;
            }
        }

        private static void ApplyKeyValuesToLayer(LayerObj layer, List<string> tokens)
        {
            var key=tokens[0].ToUpperInvariant(); var vals=tokens.Skip(1).ToList(); var joined=string.Join(" ", vals);
            switch(key)
            {
                case "NAME": layer.Name=Unquote(joined); break;
                case "TYPE": layer.Type=joined.ToUpperInvariant(); break;
                case "STATUS": layer.Status=joined.ToUpperInvariant(); break;
                case "DATA": layer.Data=Unquote(joined); break;
                case "CONNECTIONTYPE": layer.ConnectionType=joined.ToUpperInvariant(); break;
                case "CONNECTION": layer.Connection=Unquote(joined); break;
                case "GROUP": layer.Group=Unquote(joined); break;
                case "CLASSGROUP": layer.ClassGroup=Unquote(joined); break;
                case "CLASSITEM": layer.ClassItem=Unquote(joined); break;
                case "LABELITEM": layer.LabelItem=Unquote(joined); break;
                case "MINSCALEDENOM": layer.MinScaleDenom=ParseDouble(vals); break;
                case "MAXSCALEDENOM": layer.MaxScaleDenom=ParseDouble(vals); break;
                case "MINSCALE": layer.MinScale=ParseDouble(vals); break;
                case "MAXSCALE": layer.MaxScale=ParseDouble(vals); break;
                case "MINGEOWIDTH": layer.MinGeoWidth=ParseDouble(vals); break;
                case "MAXGEOWIDTH": layer.MaxGeoWidth=ParseDouble(vals); break;
                case "SYMBOLSCALEDENOM": layer.SymbolScaleDenom=ParseDouble(vals); break;
                case "EXTENT": layer.Extent=ParseDoubles(vals,4); break;
                case "UNITS": layer.Units=joined; break;
                case "LABELMINSCALEDENOM": layer.LabelMinScaleDenom=ParseDouble(vals); break;
                case "LABELMAXSCALEDENOM": layer.LabelMaxScaleDenom=ParseDouble(vals); break;
                case "LABELREQUIRES": layer.LabelRequires=Unquote(joined); break;
                case "LABELCACHE": layer.LabelCache=joined.ToUpperInvariant(); break;
                case "DEBUG": layer.Debug=joined; break;
                case "ENCODING": layer.Encoding=Unquote(joined); break;
                case "FILTER": layer.Filter=Unquote(joined); break;
                case "FILTERITEM": layer.FilterItem=Unquote(joined); break;
                case "MINFEATURESIZE": layer.MinFeatureSize=(int)ParseDouble(vals); break;
                case "MAXFEATURES": layer.MaxFeatures=(int)ParseDouble(vals); break;
                case "MASK": layer.Mask=Unquote(joined); break;
                case "STYLEITEM": layer.StyleItem=joined; break;
                case "GEOMTRANSFORM": layer.GeomTransform=Unquote(joined); break;
                case "POSTLABELCACHE": layer.PostLabelCache=joined.ToUpperInvariant(); break;
                case "REQUIRES": layer.Requires=Unquote(joined); break;
                case "TRANSFORM": layer.Transform=joined.ToUpperInvariant(); break;
                case "TOLERANCE": layer.Tolerance=ParseDouble(vals); break;
                case "TOLERANCEUNITS": layer.ToleranceUnits=joined; break;
                case "HEADER": layer.Header=Unquote(joined); break;
                case "FOOTER": layer.Footer=Unquote(joined); break;
                case "TEMPLATE": layer.Template=Unquote(joined); break;
                case "OFFSITE": if (vals.Count>=1 && vals[0].StartsWith("#")) layer.OffsiteHex=Unquote(vals[0]); else if (vals.Count>=3) layer.OffsiteColor=ParseInts(vals,3); else AddToAttributes(layer.Attributes, tokens); break;
                case "OPACITY": if (vals.Count==1 && int.TryParse(vals[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var op)) layer.Opacity=op; else layer.OpacityKeyword=joined.ToUpperInvariant(); break;
                case "TILEINDEX": layer.TileIndex=Unquote(joined); break;
                case "TILEITEM": layer.TileItem=Unquote(joined); break;
                case "TILEFILTER": layer.TileFilter=Unquote(joined); break;
                case "TILEFILTERITEM": layer.TileFilterItem=Unquote(joined); break;
                case "PROCESSING": layer.Processing.Add(Unquote(joined)); break;
                case "CONNECTIONOPTIONS": if (vals.Count>=2){ var ck=Unquote(vals[0]); var cv=Unquote(string.Join(" ", vals.Skip(1))); layer.ConnectionOptions[ck]=cv; } else AddToAttributes(layer.Attributes, tokens); break;
                default: AddToAttributes(layer.Attributes, tokens); break;
            }
        }

        private static void ApplyKeyValuesToClass(ClassObj c, List<string> tokens)
        {
            var key=tokens[0].ToUpperInvariant(); var vals=tokens.Skip(1).ToList(); var joined=string.Join(" ", vals);
            switch(key)
            {
                case "NAME": c.Name=Unquote(joined); break;
                case "TITLE": c.Title=Unquote(joined); break;
                case "STATUS": c.Status=joined.ToUpperInvariant(); break;
                case "GROUP": c.Group=Unquote(joined); break;
                case "EXPRESSION": c.Expression=Unquote(joined); break;
                case "TEXT": c.Text=Unquote(joined); break;
                case "TEMPLATE": c.Template=Unquote(joined); break;
                case "KEYIMAGE": c.KeyImage=Unquote(joined); break;
                case "MINSCALEDENOM": c.MinScaleDenom=ParseDouble(vals); break;
                case "MAXSCALEDENOM": c.MaxScaleDenom=ParseDouble(vals); break;
                case "MINFEATURESIZE": c.MinFeatureSize=(int)ParseDouble(vals); break;
                case "DEBUG": c.Debug=joined; break;
                case "FALLBACK": c.Fallback = joined.Equals("TRUE", StringComparison.OrdinalIgnoreCase) || joined.Equals("ON", StringComparison.OrdinalIgnoreCase); break;
                default: AddToAttributes(c.Attributes, tokens); break;
            }
        }

        private static void ApplyKeyValuesToLeader(LeaderObj l, List<string> tokens)
        {
            var key=tokens[0].ToUpperInvariant(); var vals=tokens.Skip(1).ToList();
            switch(key)
            {
                case "GRIDSTEP": l.GridStep=(int)ParseDouble(vals); break;
                case "MAXDISTANCE": l.MaxDistance=(int)ParseDouble(vals); break;
                default: AddToAttributes(l.Attributes, tokens); break;
            }
        }

        private static void ApplyKeyValuesToStyle(StyleObj s, List<string> tokens)
        {
            var key=tokens[0].ToUpperInvariant(); var vals=tokens.Skip(1).ToList(); var joined=string.Join(" ", vals);
            switch(key)
            {
                case "COLOR": if (vals.Count>=1 && vals[0].StartsWith("#")) s.ColorHex=Unquote(vals[0]); else if (vals.Count>=1 && vals[0].StartsWith("[")) s.ColorAttr=joined; else if (vals.Count>=3) s.Color=ParseInts(vals,3); else AddToAttributes(s.Attributes, tokens); break;
                case "OUTLINECOLOR": if (vals.Count>=1 && vals[0].StartsWith("#")) s.OutlineColorHex=Unquote(vals[0]); else if (vals.Count>=1 && vals[0].StartsWith("[")) s.OutlineColorAttr=joined; else if (vals.Count>=3) s.OutlineColor=ParseInts(vals,3); else AddToAttributes(s.Attributes, tokens); break;
                case "OUTLINEWIDTH": s.OutlineWidth=ParseDouble(vals); break;
                case "SYMBOL": s.Symbol=Unquote(joined); break;
                case "ANGLE": s.Angle=joined; break;
                case "WIDTH": s.Width=ParseDouble(vals); break;
                case "SIZE": if (vals.Count==1 && double.TryParse(vals[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var sz)) s.Size=sz; else s.SizeExpr=joined; break;
                case "MINSCALEDENOM": s.MinScaleDenom=ParseDouble(vals); break;
                case "MAXSCALEDENOM": s.MaxScaleDenom=ParseDouble(vals); break;
                case "MINWIDTH": s.MinWidth=ParseDouble(vals); break;
                case "MAXWIDTH": s.MaxWidth=ParseDouble(vals); break;
                case "MINSIZE": s.MinSize=ParseDouble(vals); break;
                case "MAXSIZE": s.MaxSize=ParseDouble(vals); break;
                case "GAP": s.Gap=ParseDouble(vals); break;
                case "INITIALGAP": s.InitialGap=ParseDouble(vals); break;
                case "OFFSET": s.OffsetX=vals.ElementAtOrDefault(0); s.OffsetY=vals.ElementAtOrDefault(1); break;
                case "POLAROFFSET": s.PolarOffsetR=vals.ElementAtOrDefault(0); s.PolarOffsetA=vals.ElementAtOrDefault(1); break;
                case "LINECAP": s.LineCap=joined.ToUpperInvariant(); break;
                case "LINEJOIN": s.LineJoin=joined.ToUpperInvariant(); break;
                case "LINEJOINMAXSIZE": s.LineJoinMaxSize=(int)ParseDouble(vals); break;
                case "PATTERN": // Read numbers on the same line if present
                    foreach(var v in vals){ if(double.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out var d)) s.Pattern.Add(d); }
                    break;
                case "GEOMTRANSFORM": s.GeomTransform=Unquote(joined); break;
                case "OPACITY": s.Opacity=joined; break;
                case "RANGEITEM": s.RangeItem=Unquote(joined); break;
                case "COLORRANGE":
                    if (vals.Count==2 && vals[0].StartsWith("#") && vals[1].StartsWith("#")) { s.ColorRangeLow.hex=Unquote(vals[0]); s.ColorRangeHigh.hex=Unquote(vals[1]); }
                    else if (vals.Count==6) { s.ColorRangeLow.rgb=ParseInts(vals.Take(3).ToList(),3); s.ColorRangeHigh.rgb=ParseInts(vals.Skip(3).ToList(),3); }
                    else AddToAttributes(s.Attributes, tokens);
                    break;
                case "DATARANGE": if (vals.Count>=2) { s.DataRange=(ParseDouble(new List<string>{vals[0]}), ParseDouble(new List<string>{vals[1]})); } else AddToAttributes(s.Attributes, tokens); break;
                default: AddToAttributes(s.Attributes, tokens); break;
            }
        }

        private static void ApplyKeyValuesToLabel(LabelObj l, List<string> tokens)
        {
            var key=tokens[0].ToUpperInvariant(); var vals=tokens.Skip(1).ToList(); var joined=string.Join(" ", vals);
            switch(key)
            {
                case "ALIGN": l.Align=joined; break;
                case "ANGLE": l.Angle=joined; break;
                case "BUFFER": l.Buffer=(int)ParseDouble(vals); break;
                case "COLOR": if (vals.Count>=1 && vals[0].StartsWith("#")) l.ColorHex=Unquote(vals[0]); else if (vals[0].StartsWith("[")) l.ColorAttr=joined; else if (vals.Count>=3) l.Color=ParseInts(vals,3); else AddToAttributes(l.Attributes, tokens); break;
                case "OUTLINECOLOR": if (vals.Count>=1 && vals[0].StartsWith("#")) l.OutlineColorHex=Unquote(vals[0]); else if (vals[0].StartsWith("[")) l.OutlineColorAttr=joined; else if (vals.Count>=3) l.OutlineColor=ParseInts(vals,3); else AddToAttributes(l.Attributes, tokens); break;
                case "OUTLINEWIDTH": l.OutlineWidth=(int)ParseDouble(vals); break;
                case "FONT": l.Font=Unquote(joined); break;
                case "TYPE": l.Type=joined.ToUpperInvariant(); break;
                case "SIZE": l.Size=joined; break;
                case "MAXOVERLAPANGLE": l.MaxOverlapAngle=ParseDouble(vals); break;
                case "MINSCALEDENOM": l.MinScaleDenom=ParseDouble(vals); break;
                case "MAXSCALEDENOM": l.MaxScaleDenom=ParseDouble(vals); break;
                case "MINDISTANCE": l.MinDistance=(int)ParseDouble(vals); break;
                case "MINFEATURESIZE": l.MinFeatureSize=joined.ToUpperInvariant(); break;
                case "MINSIZE": l.MinSize=(int)ParseDouble(vals); break;
                case "MAXSIZE": l.MaxSize=(int)ParseDouble(vals); break;
                case "OFFSET": l.OffsetX=vals.ElementAtOrDefault(0); l.OffsetY=vals.ElementAtOrDefault(1); break;
                case "POSITION": l.Position=joined.ToLowerInvariant(); break;
                case "PRIORITY": l.Priority=joined; break;
                case "PARTIALS": l.Partials=joined.ToUpperInvariant(); break;
                case "FORCE": l.Force=joined.ToUpperInvariant(); break;
                case "REPEATDISTANCE": l.RepeatDistance=(int)ParseDouble(vals); break;
                case "SHADOWCOLOR": if (vals.Count>=1 && vals[0].StartsWith("#")) l.ShadowColorHex=Unquote(vals[0]); else if (vals.Count>=3) l.ShadowColor=ParseInts(vals,3); else AddToAttributes(l.Attributes, tokens); break;
                case "SHADOWSIZE": l.ShadowSizeX=vals.ElementAtOrDefault(0); l.ShadowSizeY=vals.ElementAtOrDefault(1); break;
                case "TEXT": l.Text=Unquote(joined); break;
                case "WRAP": l.Wrap=Unquote(joined); break;
                case "MAXLENGTH": l.MaxLength=(int)ParseDouble(vals); break;
                default: AddToAttributes(l.Attributes, tokens); break;
            }
        }

        private static void ApplyKeyValuesToOutputFormat(OutputFormatObj of, List<string> tokens)
        {
            var key=tokens[0].ToUpperInvariant(); var vals=tokens.Skip(1).ToList(); var joined=string.Join(" ", vals);
            switch(key)
            {
                case "NAME": of.Name=Unquote(joined); break;
                case "DRIVER": of.Driver=Unquote(joined); break;
                case "MIMETYPE": of.MimeType=Unquote(joined); break;
                case "EXTENSION": of.Extension=Unquote(joined); break;
                case "IMAGEMODE": of.ImageMode=joined.ToUpperInvariant(); break;
                case "TRANSPARENT": of.Transparent=joined.ToUpperInvariant(); break;
                default: AddToAttributes(of.Attributes, tokens); break;
            }
        }

        private static void ApplyKeyValuesToJoin(JoinObj j, List<string> tokens)
        {
            var key=tokens[0].ToUpperInvariant(); var joined=string.Join(" ", tokens.Skip(1));
            switch(key)
            {
                case "NAME": j.Name=Unquote(joined); break;
                case "TABLE": j.Table=Unquote(joined); break;
                case "FROM": j.From=Unquote(joined); break;
                case "TO": j.To=Unquote(joined); break;
                case "TYPE": j.Type=joined.ToUpperInvariant(); break;
                case "TEMPLATE": j.Template=Unquote(joined); break;
                default: if (!j.Attributes.TryGetValue(key, out var list)){ list=new List<string[]>(); j.Attributes[key]=list; } list.Add(tokens.Skip(1).Select(Unquote).ToArray()); break;
            }
        }

        // INCLUDE expansion (preprocessing)
        private static string ExpandIncludes(string text, string? baseDir, Stack<string> includeStack)
        {
            if (baseDir == null) return text; // cannot resolve; leave as-is

            var sb = new StringBuilder(text.Length);
            using var sr = new StringReader(text);
            string? raw;
            while((raw = sr.ReadLine()) != null)
            {
                var stripped = StripComments(raw);
                var tokens = Tokenize(stripped);
                if (tokens.Count > 0 && string.Equals(tokens[0], "INCLUDE", StringComparison.OrdinalIgnoreCase) && tokens.Count >= 2)
                {
                    var incPathPart = string.Join(" ", tokens.Skip(1));
                    var incPath = Unquote(incPathPart);
                    string candidate = Path.IsPathRooted(incPath) ? incPath : Path.Combine(baseDir, incPath);
                    string full = Path.GetFullPath(candidate);

                    if (includeStack.Contains(full))
                        throw new InvalidOperationException($"Cyclic INCLUDE detected: {full}");

                    if (File.Exists(full))
                    {
                        includeStack.Push(full);
                        var incText = File.ReadAllText(full);
                        var expanded = ExpandIncludes(incText, Path.GetDirectoryName(full), includeStack);
                        includeStack.Pop();
                        sb.AppendLine(expanded);
                    }
                    else
                    {
                        // Could not resolve: retain original line
                        sb.AppendLine(raw);
                    }
                }
                else
                {
                    sb.AppendLine(raw);
                }
            }
            return sb.ToString();
        }

        // Helpers
        private static string StripComments(string line)
        {
            var sb=new StringBuilder(line.Length); bool inQuotes=false; for(int i=0;i<line.Length;i++){ var c=line[i]; if(c=='"'){ inQuotes=!inQuotes; sb.Append(c);} else if(!inQuotes && c=='#'){ break; } else { sb.Append(c);} } return sb.ToString().Trim();
        }
        private static List<string> Tokenize(string line)
        {
            var tokens=new List<string>(); var sb=new StringBuilder(); bool inQuotes=false; void Flush(){ if(sb.Length>0){ tokens.Add(sb.ToString()); sb.Clear(); } } for(int i=0;i<line.Length;i++){ char c=line[i]; if(c=='"'){ sb.Append(c); inQuotes=!inQuotes; } else if(!inQuotes && char.IsWhiteSpace(c)){ Flush(); } else { sb.Append(c);} } Flush(); return tokens;
        }
        private static string Unquote(string s){ s=s.Trim(); if(s.Length>=2 && s[0]=='"' && s[^1]=='"'){ var inner=s.Substring(1, s.Length-2); return inner.Replace("\\\"","\""); } return s; }
        private static double[] ParseDoubles(List<string> vals, int expected){ if(vals.Count<expected) throw new FormatException($"Expected {expected} numbers, got {vals.Count}"); var arr=new double[expected]; for(int i=0;i<expected;i++) arr[i]=double.Parse(vals[i], CultureInfo.InvariantCulture); return arr; }
        private static int[] ParseInts(List<string> vals, int expected){ if(vals.Count<expected) throw new FormatException($"Expected {expected} integers, got {vals.Count}"); var arr=new int[expected]; for(int i=0;i<expected;i++) arr[i]=int.Parse(vals[i], CultureInfo.InvariantCulture); return arr; }
        private static double ParseDouble(List<string> vals){ if(vals.Count<1) throw new FormatException("Expected a numeric value"); return double.Parse(vals[0], CultureInfo.InvariantCulture); }
        private static void AddToAttributes(Dictionary<string,List<string[]>> bag, List<string> tokens){ var key=tokens[0]; var vals=tokens.Skip(1).Select(Unquote).ToArray(); if(!bag.TryGetValue(key, out var list)){ list=new List<string[]>(); bag[key]=list; } list.Add(vals); }
    }

    #endregion

    #region Serializer helpers

    internal static class MapfileSerializer
    {
        public static void WriteIndent(TextWriter w, int n){ for(int i=0;i<n;i++) w.Write("  "); }
        public static void WriteKeyValues(TextWriter w, int indent, string key, params string[] values)
        {
            WriteIndent(w, indent);
            if(values.Length==0){ w.WriteLine(key); return; }
            w.Write(key); w.Write(' ');
            for(int i=0;i<values.Length;i++){ if(i>0) w.Write(' '); w.Write(values[i]); }
            w.WriteLine();
        }
        public static void WriteAttributes(TextWriter w, int indent, Dictionary<string,List<string[]>> attrs)
        {
            foreach(var kv in attrs)
            {
                foreach(var values in kv.Value)
                {
                    var rendered = values.Select(MaybeQuote).ToArray();
                    WriteKeyValues(w, indent, kv.Key.ToUpperInvariant(), rendered);
                }
            }
        }
        public static string Quote(string s)=>"\""+s.Replace("\"","\\\"")+"\"";
        public static string MaybeQuote(string s){ if(NeedsQuoting(s)) return Quote(s); return s; }
        private static bool NeedsQuoting(string s){ if(string.IsNullOrEmpty(s)) return true; return s.Any(ch=>char.IsWhiteSpace(ch)||ch=='#'||ch=='"'); }
    }

    #endregion
}
