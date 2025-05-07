using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace BMapr.GDAL.WebApi.Models.MapFile
{
    public class MapFile
    {
        [JsonProperty("refcount")]
        [JsonPropertyName("refcount")]
        public int Refcount;

        [JsonProperty("numlayers")]
        [JsonPropertyName("numlayers")]
        public int Numlayers;

        [JsonProperty("maxlayers")]
        [JsonPropertyName("maxlayers")]
        public int Maxlayers;

        [JsonProperty("configoptions")]
        [JsonPropertyName("configoptions")]
        public Configoptions Configoptions;

        [JsonProperty("symbolset")]
        [JsonPropertyName("symbolset")]
        public Symbolset Symbolset;

        [JsonProperty("fontset")]
        [JsonPropertyName("fontset")]
        public Fontset Fontset;

        [JsonProperty("labelcache")]
        [JsonPropertyName("labelcache")]
        public Labelcache Labelcache;

        [JsonProperty("numoutputformats")]
        [JsonPropertyName("numoutputformats")]
        public int Numoutputformats;

        [JsonProperty("outputformat")]
        [JsonPropertyName("outputformat")]
        public Outputformat Outputformat;

        [JsonProperty("imagetype")]
        [JsonPropertyName("imagetype")]
        public string Imagetype;

        [JsonProperty("reference")]
        [JsonPropertyName("reference")]
        public Reference Reference;

        [JsonProperty("scalebar")]
        [JsonPropertyName("scalebar")]
        public Scalebar Scalebar;

        [JsonProperty("legend")]
        [JsonPropertyName("legend")]
        public Legend Legend;

        [JsonProperty("querymap")]
        [JsonPropertyName("querymap")]
        public Querymap Querymap;

        [JsonProperty("web")]
        [JsonPropertyName("web")]
        public Web Web;

        [JsonProperty("config")]
        [JsonPropertyName("config")]
        public object Config;

        [JsonProperty("datapattern")]
        [JsonPropertyName("datapattern")]
        public object Datapattern;

        [JsonProperty("templatepattern")]
        [JsonPropertyName("templatepattern")]
        public object Templatepattern;

        [JsonProperty("name")]
        [JsonPropertyName("name")]
        public string Name;

        [JsonProperty("status")]
        [JsonPropertyName("status")]
        public int Status;

        [JsonProperty("height")]
        [JsonPropertyName("height")]
        public int Height;

        [JsonProperty("width")]
        [JsonPropertyName("width")]
        public int Width;

        [JsonProperty("maxsize")]
        [JsonPropertyName("maxsize")]
        public int Maxsize;

        [JsonProperty("extent")]
        [JsonPropertyName("extent")]
        public Extent Extent;

        [JsonProperty("cellsize")]
        [JsonPropertyName("cellsize")]
        public double Cellsize;

        [JsonProperty("units")]
        [JsonPropertyName("units")]
        public string Units;

        [JsonProperty("scaledenom")]
        [JsonPropertyName("scaledenom")]
        public double Scaledenom;

        [JsonProperty("resolution")]
        [JsonPropertyName("resolution")]
        public double Resolution;

        [JsonProperty("defresolution")]
        [JsonPropertyName("defresolution")]
        public double Defresolution;

        [JsonProperty("shapepath")]
        [JsonPropertyName("shapepath")]
        public string Shapepath;

        [JsonProperty("mappath")]
        [JsonPropertyName("mappath")]
        public string Mappath;

        [JsonProperty("sldurl")]
        [JsonPropertyName("sldurl")]
        public object Sldurl;

        [JsonProperty("imagecolor")]
        [JsonPropertyName("imagecolor")]
        public Imagecolor Imagecolor;

        [JsonProperty("layerorder")]
        [JsonPropertyName("layerorder")]
        public Layerorder Layerorder;

        [JsonProperty("debug")]
        [JsonPropertyName("debug")]
        public int Debug;

        [JsonProperty("layers")]
        [JsonPropertyName("layers")]
        public List<Layer> Layers;

        [JsonProperty("projectGuid")]
        [JsonPropertyName("projectGuid")]
        public string ProjectGuid;
    }

    public class Layer
    {
        [JsonProperty("refcount")]
        [JsonPropertyName("refcount")]
        public int Refcount;

        [JsonProperty("numclasses")]
        [JsonPropertyName("numclasses")]
        public int Numclasses;

        [JsonProperty("maxclasses")]
        [JsonPropertyName("maxclasses")]
        public int Maxclasses;

        [JsonProperty("index")]
        [JsonPropertyName("index")]
        public int Index;

        [JsonProperty("numitems")]
        [JsonPropertyName("numitems")]
        public int Numitems;

        [JsonProperty("metadata")]
        [JsonPropertyName("metadata")]
        public Metadata Metadata;

        [JsonProperty("validation")]
        [JsonPropertyName("validation")]
        public Validation Validation;

        [JsonProperty("bindvals")]
        [JsonPropertyName("bindvals")]
        public Bindvals Bindvals;

        [JsonProperty("connectionoptions")]
        [JsonPropertyName("connectionoptions")]
        public Connectionoptions Connectionoptions;

        [JsonProperty("cluster")]
        [JsonPropertyName("cluster")]
        public Cluster Cluster;

        [JsonProperty("extent")]
        [JsonPropertyName("extent")]
        public Extent Extent;

        [JsonProperty("numprocessing")]
        [JsonPropertyName("numprocessing")]
        public int Numprocessing;

        [JsonProperty("numjoins")]
        [JsonPropertyName("numjoins")]
        public int Numjoins;

        [JsonProperty("utfdata")]
        [JsonPropertyName("utfdata")]
        public Utfdata Utfdata;

        [JsonProperty("compositer")]
        [JsonPropertyName("compositer")]
        public object Compositer;

        [JsonProperty("classitem")]
        [JsonPropertyName("classitem")]
        public object Classitem;

        [JsonProperty("header")]
        [JsonPropertyName("header")]
        public object Header;

        [JsonProperty("footer")]
        [JsonPropertyName("footer")]
        public object Footer;

        [JsonProperty("template")]
        [JsonPropertyName("template")]
        public object Template;

        [JsonProperty("name")]
        [JsonPropertyName("name")]
        public string Name;

        [JsonProperty("group")]
        [JsonPropertyName("group")]
        public object Group;

        [JsonProperty("status")]
        [JsonPropertyName("status")]
        public int Status;

        [JsonProperty("rendermode")]
        [JsonPropertyName("rendermode")]
        public string Rendermode;

        [JsonProperty("data")]
        [JsonPropertyName("data")]
        public string Data;

        [JsonProperty("type")]
        [JsonPropertyName("type")]
        public string Type;

        [JsonProperty("connectiontype")]
        [JsonPropertyName("connectiontype")]
        public string Connectiontype;

        [JsonProperty("tolerance")]
        [JsonPropertyName("tolerance")]
        public double Tolerance;

        [JsonProperty("toleranceunits")]
        [JsonPropertyName("toleranceunits")]
        public int Toleranceunits;

        [JsonProperty("symbolscaledenom")]
        [JsonPropertyName("symbolscaledenom")]
        public double Symbolscaledenom;

        [JsonProperty("minscaledenom")]
        [JsonPropertyName("minscaledenom")]
        public double Minscaledenom;

        [JsonProperty("maxscaledenom")]
        [JsonPropertyName("maxscaledenom")]
        public double Maxscaledenom;

        [JsonProperty("minfeaturesize")]
        [JsonPropertyName("minfeaturesize")]
        public int Minfeaturesize;

        [JsonProperty("labelminscaledenom")]
        [JsonPropertyName("labelminscaledenom")]
        public double Labelminscaledenom;

        [JsonProperty("labelmaxscaledenom")]
        [JsonPropertyName("labelmaxscaledenom")]
        public double Labelmaxscaledenom;

        [JsonProperty("mingeowidth")]
        [JsonPropertyName("mingeowidth")]
        public double Mingeowidth;

        [JsonProperty("maxgeowidth")]
        [JsonPropertyName("maxgeowidth")]
        public double Maxgeowidth;

        [JsonProperty("sizeunits")]
        [JsonPropertyName("sizeunits")]
        public int Sizeunits;

        [JsonProperty("maxfeatures")]
        [JsonPropertyName("maxfeatures")]
        public int Maxfeatures;

        [JsonProperty("startindex")]
        [JsonPropertyName("startindex")]
        public int Startindex;

        [JsonProperty("offsite")]
        [JsonPropertyName("offsite")]
        public Offsite Offsite;

        [JsonProperty("transform")]
        [JsonPropertyName("transform")]
        public int Transform;

        [JsonProperty("labelcache")]
        [JsonPropertyName("labelcache")]
        public int Labelcache;

        [JsonProperty("postlabelcache")]
        [JsonPropertyName("postlabelcache")]
        public int Postlabelcache;

        [JsonProperty("labelitem")]
        [JsonPropertyName("labelitem")]
        public object Labelitem;

        [JsonProperty("tileitem")]
        [JsonPropertyName("tileitem")]
        public string Tileitem;

        [JsonProperty("tileindex")]
        [JsonPropertyName("tileindex")]
        public object Tileindex;

        [JsonProperty("tilesrs")]
        [JsonPropertyName("tilesrs")]
        public object Tilesrs;

        [JsonProperty("units")]
        [JsonPropertyName("units")]
        public string Units;

        [JsonProperty("connection")]
        [JsonPropertyName("connection")]
        public object Connection;

        [JsonProperty("plugin_library")]
        [JsonPropertyName("plugin_library")]
        public object PluginLibrary;

        [JsonProperty("plugin_library_original")]
        [JsonPropertyName("plugin_library_original")]
        public object PluginLibraryOriginal;

        [JsonProperty("bandsitem")]
        [JsonPropertyName("bandsitem")]
        public object Bandsitem;

        [JsonProperty("filteritem")]
        [JsonPropertyName("filteritem")]
        public object Filteritem;

        [JsonProperty("styleitem")]
        [JsonPropertyName("styleitem")]
        public object Styleitem;

        [JsonProperty("requires")]
        [JsonPropertyName("requires")]
        public object Requires;

        [JsonProperty("labelrequires")]
        [JsonPropertyName("labelrequires")]
        public object Labelrequires;

        [JsonProperty("debug")]
        [JsonPropertyName("debug")]
        public int Debug;

        [JsonProperty("classgroup")]
        [JsonPropertyName("classgroup")]
        public object Classgroup;

        [JsonProperty("mask")]
        [JsonPropertyName("mask")]
        public object Mask;

        [JsonProperty("encoding")]
        [JsonPropertyName("encoding")]
        public object Encoding;

        [JsonProperty("utfitem")]
        [JsonPropertyName("utfitem")]
        public object Utfitem;

        [JsonProperty("utfitemindex")]
        [JsonPropertyName("utfitemindex")]
        public int Utfitemindex;

        [JsonProperty("classes")]
        [JsonPropertyName("classes")]
        public List<Class> Classes;
    }

    public class Backgroundcolor
    {
        [JsonProperty("red")]
        [JsonPropertyName("red")]
        public int Red;

        [JsonProperty("green")]
        [JsonPropertyName("green")]
        public int Green;

        [JsonProperty("blue")]
        [JsonPropertyName("blue")]
        public int Blue;

        [JsonProperty("alpha")]
        [JsonPropertyName("alpha")]
        public int Alpha;
    }

    public class Bindvals
    {
    }

    public class Class
    {
        [JsonProperty("metadata")]
        [JsonPropertyName("metadata")]
        public Metadata Metadata;

        [JsonProperty("validation")]
        [JsonPropertyName("validation")]
        public Validation Validation;

        [JsonProperty("numstyles")]
        [JsonPropertyName("numstyles")]
        public int Numstyles;

        [JsonProperty("numlabels")]
        [JsonPropertyName("numlabels")]
        public int Numlabels;

        [JsonProperty("refcount")]
        [JsonPropertyName("refcount")]
        public int Refcount;

        [JsonProperty("leader")]
        [JsonPropertyName("leader")]
        public object Leader;

        [JsonProperty("status")]
        [JsonPropertyName("status")]
        public int Status;

        [JsonProperty("isfallback")]
        [JsonPropertyName("isfallback")]
        public int Isfallback;

        [JsonProperty("name")]
        [JsonPropertyName("name")]
        public string Name;

        [JsonProperty("title")]
        [JsonPropertyName("title")]
        public object Title;

        [JsonProperty("minscaledenom")]
        [JsonPropertyName("minscaledenom")]
        public double Minscaledenom;

        [JsonProperty("maxscaledenom")]
        [JsonPropertyName("maxscaledenom")]
        public double Maxscaledenom;

        [JsonProperty("minfeaturesize")]
        [JsonPropertyName("minfeaturesize")]
        public double Minfeaturesize;

        [JsonProperty("debug")]
        [JsonPropertyName("debug")]
        public int Debug;

        [JsonProperty("keyimage")]
        [JsonPropertyName("keyimage")]
        public object Keyimage;

        [JsonProperty("group")]
        [JsonPropertyName("group")]
        public object Group;

        [JsonProperty("sizeunits")]
        [JsonPropertyName("sizeunits")]
        public int Sizeunits;

        [JsonProperty("template")]
        [JsonPropertyName("template")]
        public object Template;

        [JsonProperty("styles")]
        [JsonPropertyName("styles")]
        public List<Style> Styles;

        [JsonProperty("labels")]
        [JsonPropertyName("labels")]
        public List<object> Labels;
    }

    public class Cluster
    {
        [JsonProperty("maxdistance")]
        [JsonPropertyName("maxdistance")]
        public double Maxdistance;

        [JsonProperty("buffer")]
        [JsonPropertyName("buffer")]
        public double Buffer;

        [JsonProperty("region")]
        [JsonPropertyName("region")]
        public object Region;
    }

    public class Color
    {
        [JsonProperty("red")]
        [JsonPropertyName("red")]
        public int Red;

        [JsonProperty("green")]
        [JsonPropertyName("green")]
        public int Green;

        [JsonProperty("blue")]
        [JsonPropertyName("blue")]
        public int Blue;

        [JsonProperty("alpha")]
        [JsonPropertyName("alpha")]
        public int Alpha;
    }

    public class Configoptions
    {
        [JsonProperty("PROJ_LIB")]
        [JsonPropertyName("PROJ_LIB")]
        public string PROJLIB;
    }

    public class Connectionoptions
    {
    }

    public class Extent
    {
        [JsonProperty("minx")]
        [JsonPropertyName("minx")]
        public double Minx;

        [JsonProperty("miny")]
        [JsonPropertyName("miny")]
        public double Miny;

        [JsonProperty("maxx")]
        [JsonPropertyName("maxx")]
        public double Maxx;

        [JsonProperty("maxy")]
        [JsonPropertyName("maxy")]
        public double Maxy;
    }

    public class Fonts
    {
        [JsonProperty("numitems")]
        [JsonPropertyName("numitems")]
        public int Numitems;
    }

    public class Fontset
    {
        [JsonProperty("filename")]
        [JsonPropertyName("filename")]
        public string Filename;

        [JsonProperty("numfonts")]
        [JsonPropertyName("numfonts")]
        public int Numfonts;

        [JsonProperty("fonts")]
        [JsonPropertyName("fonts")]
        public Fonts Fonts;
    }

    public class Imagecolor
    {
        [JsonProperty("red")]
        [JsonPropertyName("red")]
        public int Red;

        [JsonProperty("green")]
        [JsonPropertyName("green")]
        public int Green;

        [JsonProperty("blue")]
        [JsonPropertyName("blue")]
        public int Blue;

        [JsonProperty("alpha")]
        [JsonPropertyName("alpha")]
        public int Alpha;
    }

    public class Label
    {
        [JsonProperty("refcount")]
        [JsonPropertyName("refcount")]
        public int Refcount;

        [JsonProperty("font")]
        [JsonPropertyName("font")]
        public object Font;

        [JsonProperty("color")]
        [JsonPropertyName("color")]
        public Color Color;

        [JsonProperty("outlinecolor")]
        [JsonPropertyName("outlinecolor")]
        public Outlinecolor Outlinecolor;

        [JsonProperty("outlinewidth")]
        [JsonPropertyName("outlinewidth")]
        public double Outlinewidth;

        [JsonProperty("shadowcolor")]
        [JsonPropertyName("shadowcolor")]
        public Shadowcolor Shadowcolor;

        [JsonProperty("shadowsizex")]
        [JsonPropertyName("shadowsizex")]
        public double Shadowsizex;

        [JsonProperty("shadowsizey")]
        [JsonPropertyName("shadowsizey")]
        public double Shadowsizey;

        [JsonProperty("size")]
        [JsonPropertyName("size")]
        public double Size;

        [JsonProperty("minsize")]
        [JsonPropertyName("minsize")]
        public double Minsize;

        [JsonProperty("maxsize")]
        [JsonPropertyName("maxsize")]
        public double Maxsize;

        [JsonProperty("position")]
        [JsonPropertyName("position")]
        public int Position;

        [JsonProperty("offsetx")]
        [JsonPropertyName("offsetx")]
        public double Offsetx;

        [JsonProperty("offsety")]
        [JsonPropertyName("offsety")]
        public double Offsety;

        [JsonProperty("angle")]
        [JsonPropertyName("angle")]
        public double Angle;

        [JsonProperty("anglemode")]
        [JsonPropertyName("anglemode")]
        public int Anglemode;

        [JsonProperty("buffer")]
        [JsonPropertyName("buffer")]
        public double Buffer;

        [JsonProperty("align")]
        [JsonPropertyName("align")]
        public int Align;

        [JsonProperty("wrap")]
        [JsonPropertyName("wrap")]
        public string Wrap;

        [JsonProperty("maxlength")]
        [JsonPropertyName("maxlength")]
        public double Maxlength;

        [JsonProperty("space_size_10")]
        [JsonPropertyName("space_size_10")]
        public double SpaceSize10;

        [JsonProperty("minfeaturesize")]
        [JsonPropertyName("minfeaturesize")]
        public double Minfeaturesize;

        [JsonProperty("autominfeaturesize")]
        [JsonPropertyName("autominfeaturesize")]
        public double Autominfeaturesize;

        [JsonProperty("minscaledenom")]
        [JsonPropertyName("minscaledenom")]
        public double Minscaledenom;

        [JsonProperty("maxscaledenom")]
        [JsonPropertyName("maxscaledenom")]
        public double Maxscaledenom;

        [JsonProperty("mindistance")]
        [JsonPropertyName("mindistance")]
        public double Mindistance;

        [JsonProperty("repeatdistance")]
        [JsonPropertyName("repeatdistance")]
        public double Repeatdistance;

        [JsonProperty("maxoverlapangle")]
        [JsonPropertyName("maxoverlapangle")]
        public double Maxoverlapangle;

        [JsonProperty("partials")]
        [JsonPropertyName("partials")]
        public int Partials;

        [JsonProperty("force")]
        [JsonPropertyName("force")]
        public int Force;

        [JsonProperty("encoding")]
        [JsonPropertyName("encoding")]
        public object Encoding;

        [JsonProperty("priority")]
        [JsonPropertyName("priority")]
        public int Priority;

        [JsonProperty("numstyles")]
        [JsonPropertyName("numstyles")]
        public int Numstyles;

        [JsonProperty("sizeunits")]
        [JsonPropertyName("sizeunits")]
        public int Sizeunits;
    }

    public class Labelcache
    {
        [JsonProperty("num_rendered_members")]
        [JsonPropertyName("num_rendered_members")]
        public int NumRenderedMembers;
    }

    public class Layerorder
    {
    }

    public class Legend
    {
        [JsonProperty("label")]
        [JsonPropertyName("label")]
        public Label Label;

        [JsonProperty("map")]
        [JsonPropertyName("map")]
        public object Map;

        [JsonProperty("transparent")]
        [JsonPropertyName("transparent")]
        public int Transparent;

        [JsonProperty("imagecolor")]
        [JsonPropertyName("imagecolor")]
        public Imagecolor Imagecolor;

        [JsonProperty("keysizex")]
        [JsonPropertyName("keysizex")]
        public double Keysizex;

        [JsonProperty("keysizey")]
        [JsonPropertyName("keysizey")]
        public double Keysizey;

        [JsonProperty("keyspacingx")]
        [JsonPropertyName("keyspacingx")]
        public double Keyspacingx;

        [JsonProperty("keyspacingy")]
        [JsonPropertyName("keyspacingy")]
        public double Keyspacingy;

        [JsonProperty("outlinecolor")]
        [JsonPropertyName("outlinecolor")]
        public Outlinecolor Outlinecolor;

        [JsonProperty("status")]
        [JsonPropertyName("status")]
        public int Status;

        [JsonProperty("height")]
        [JsonPropertyName("height")]
        public int Height;

        [JsonProperty("width")]
        [JsonPropertyName("width")]
        public int Width;

        [JsonProperty("position")]
        [JsonPropertyName("position")]
        public int Position;

        [JsonProperty("postlabelcache")]
        [JsonPropertyName("postlabelcache")]
        public int Postlabelcache;

        [JsonProperty("template")]
        [JsonPropertyName("template")]
        public object Template;
    }

    public class Maxcolor
    {
        [JsonProperty("red")]
        [JsonPropertyName("red")]
        public int Red;

        [JsonProperty("green")]
        [JsonPropertyName("green")]
        public int Green;

        [JsonProperty("blue")]
        [JsonPropertyName("blue")]
        public int Blue;

        [JsonProperty("alpha")]
        [JsonPropertyName("alpha")]
        public int Alpha;
    }

    public class Metadata
    {
        [JsonProperty("wms_srs")]
        [JsonPropertyName("wms_srs")]
        public string WmsSrs;

        [JsonProperty("wms_onlineresource")]
        [JsonPropertyName("wms_onlineresource")]
        public string WmsOnlineresource;

        [JsonProperty("wms_enable_request")]
        [JsonPropertyName("wms_enable_request")]
        public string WmsEnableRequest;

        [JsonProperty("msh_WFST110debug")]
        [JsonPropertyName("msh_WFST110debug")]
        public string MshWFST110debug;

        [JsonProperty("wfs_srs")]
        [JsonPropertyName("wfs_srs")]
        public string WfsSrs;

        [JsonProperty("msh_CacheWmsGetMap")]
        [JsonPropertyName("msh_CacheWmsGetMap")]
        public string MshCacheWmsGetMap;

        [JsonProperty("msh_WFST110enabled")]
        [JsonPropertyName("msh_WFST110enabled")]
        public string MshWFST110enabled;

        [JsonProperty("wfs_title")]
        [JsonPropertyName("wfs_title")]
        public string WfsTitle;

        [JsonProperty("wfs_onlineresource")]
        [JsonPropertyName("wfs_onlineresource")]
        public string WfsOnlineresource;

        [JsonProperty("wfs_enable_request")]
        [JsonPropertyName("wfs_enable_request")]
        public string WfsEnableRequest;

        [JsonProperty("wfs_abstract")]
        [JsonPropertyName("wfs_abstract")]
        public string WfsAbstract;

        [JsonProperty("msh_CacheWfsMetadata")]
        [JsonPropertyName("msh_CacheWfsMetadata")]
        public string MshCacheWfsMetadata;

        [JsonProperty("msh_CacheWfsGetFeature")]
        [JsonPropertyName("msh_CacheWfsGetFeature")]
        public string MshCacheWfsGetFeature;

        [JsonProperty("msh_CacheWmsGetCapabilities")]
        [JsonPropertyName("msh_CacheWmsGetCapabilities")]
        public string MshCacheWmsGetCapabilities;

        [JsonProperty("wms_title")]
        [JsonPropertyName("wms_title")]
        public string WmsTitle;

        [JsonProperty("msh_EPSG")]
        [JsonPropertyName("msh_EPSG")]
        public string MshEPSG;

        [JsonProperty("msh_Id")]
        [JsonPropertyName("msh_Id")]
        public string MshId;

        [JsonProperty("wfs_use_default_extent_for_getfeature")]
        [JsonPropertyName("wfs_use_default_extent_for_getfeature")]
        public string WfsUseDefaultExtentForGetfeature;

        [JsonProperty("msh_IdSequenceStartValue")]
        [JsonPropertyName("msh_IdSequenceStartValue")]
        public string MshIdSequenceStartValue;

        [JsonProperty("gml_featureid")]
        [JsonPropertyName("gml_featureid")]
        public string GmlFeatureid;

        [JsonProperty("msh_WFSTMsSqlServerGeography")]
        [JsonPropertyName("msh_WFSTMsSqlServerGeography")]
        public string MshWFSTMsSqlServerGeography;

        [JsonProperty("msh_Connection")]
        [JsonPropertyName("msh_Connection")]
        public string MshConnection;

        [JsonProperty("msh_WFSTuseMsSqlServerFeatureService")]
        [JsonPropertyName("msh_WFSTuseMsSqlServerFeatureService")]
        public string MshWFSTuseMsSqlServerFeatureService;

        [JsonProperty("msh_IdSquenceManual")]
        [JsonPropertyName("msh_IdSquenceManual")]
        public string MshIdSquenceManual;

        [JsonProperty("gml_include_items")]
        [JsonPropertyName("gml_include_items")]
        public string GmlIncludeItems;

        [JsonProperty("gml_types")]
        [JsonPropertyName("gml_types")]
        public string GmlTypes;

        [JsonProperty("msh_IdType")]
        [JsonPropertyName("msh_IdType")]
        public string MshIdType;

        [JsonProperty("oaf_description")]
        [JsonPropertyName("oaf_description")]
        public string OafDescription;

        [JsonProperty("oaf_title")]
        [JsonPropertyName("oaf_title")]
        public string OafTitle;
    }

    public class Mincolor
    {
        [JsonProperty("red")]
        [JsonPropertyName("red")]
        public int Red;

        [JsonProperty("green")]
        [JsonPropertyName("green")]
        public int Green;

        [JsonProperty("blue")]
        [JsonPropertyName("blue")]
        public int Blue;

        [JsonProperty("alpha")]
        [JsonPropertyName("alpha")]
        public int Alpha;
    }

    public class Offsite
    {
        [JsonProperty("red")]
        [JsonPropertyName("red")]
        public int Red;

        [JsonProperty("green")]
        [JsonPropertyName("green")]
        public int Green;

        [JsonProperty("blue")]
        [JsonPropertyName("blue")]
        public int Blue;

        [JsonProperty("alpha")]
        [JsonPropertyName("alpha")]
        public int Alpha;
    }

    public class Outlinecolor
    {
        [JsonProperty("red")]
        [JsonPropertyName("red")]
        public int Red;

        [JsonProperty("green")]
        [JsonPropertyName("green")]
        public int Green;

        [JsonProperty("blue")]
        [JsonPropertyName("blue")]
        public int Blue;

        [JsonProperty("alpha")]
        [JsonPropertyName("alpha")]
        public int Alpha;
    }

    public class Outputformat
    {
        [JsonProperty("numformatoptions")]
        [JsonPropertyName("numformatoptions")]
        public int Numformatoptions;

        [JsonProperty("name")]
        [JsonPropertyName("name")]
        public string Name;

        [JsonProperty("mimetype")]
        [JsonPropertyName("mimetype")]
        public string Mimetype;

        [JsonProperty("driver")]
        [JsonPropertyName("driver")]
        public string Driver;

        [JsonProperty("extension")]
        [JsonPropertyName("extension")]
        public string Extension;

        [JsonProperty("renderer")]
        [JsonPropertyName("renderer")]
        public int Renderer;

        [JsonProperty("imagemode")]
        [JsonPropertyName("imagemode")]
        public int Imagemode;

        [JsonProperty("transparent")]
        [JsonPropertyName("transparent")]
        public int Transparent;

        [JsonProperty("bands")]
        [JsonPropertyName("bands")]
        public int Bands;

        [JsonProperty("inmapfile")]
        [JsonPropertyName("inmapfile")]
        public int Inmapfile;
    }

    public class Querymap
    {
        [JsonProperty("map")]
        [JsonPropertyName("map")]
        public object Map;

        [JsonProperty("height")]
        [JsonPropertyName("height")]
        public int Height;

        [JsonProperty("width")]
        [JsonPropertyName("width")]
        public int Width;

        [JsonProperty("status")]
        [JsonPropertyName("status")]
        public int Status;

        [JsonProperty("style")]
        [JsonPropertyName("style")]
        public int Style;

        [JsonProperty("color")]
        [JsonPropertyName("color")]
        public Color Color;
    }

    public class Reference
    {
        [JsonProperty("map")]
        [JsonPropertyName("map")]
        public object Map;

        [JsonProperty("extent")]
        [JsonPropertyName("extent")]
        public Extent Extent;

        [JsonProperty("height")]
        [JsonPropertyName("height")]
        public int Height;

        [JsonProperty("width")]
        [JsonPropertyName("width")]
        public int Width;

        [JsonProperty("color")]
        [JsonPropertyName("color")]
        public Color Color;

        [JsonProperty("outlinecolor")]
        [JsonPropertyName("outlinecolor")]
        public Outlinecolor Outlinecolor;

        [JsonProperty("image")]
        [JsonPropertyName("image")]
        public object Image;

        [JsonProperty("status")]
        [JsonPropertyName("status")]
        public int Status;

        [JsonProperty("marker")]
        [JsonPropertyName("marker")]
        public int Marker;

        [JsonProperty("markername")]
        [JsonPropertyName("markername")]
        public object Markername;

        [JsonProperty("markersize")]
        [JsonPropertyName("markersize")]
        public double Markersize;

        [JsonProperty("minboxsize")]
        [JsonPropertyName("minboxsize")]
        public double Minboxsize;

        [JsonProperty("maxboxsize")]
        [JsonPropertyName("maxboxsize")]
        public double Maxboxsize;
    }

    public class Scalebar
    {
        [JsonProperty("transparent")]
        [JsonPropertyName("transparent")]
        public int Transparent;

        [JsonProperty("imagecolor")]
        [JsonPropertyName("imagecolor")]
        public Imagecolor Imagecolor;

        [JsonProperty("height")]
        [JsonPropertyName("height")]
        public int Height;

        [JsonProperty("width")]
        [JsonPropertyName("width")]
        public int Width;

        [JsonProperty("style")]
        [JsonPropertyName("style")]
        public int Style;

        [JsonProperty("intervals")]
        [JsonPropertyName("intervals")]
        public int Intervals;

        [JsonProperty("label")]
        [JsonPropertyName("label")]
        public Label Label;

        [JsonProperty("color")]
        [JsonPropertyName("color")]
        public Color Color;

        [JsonProperty("backgroundcolor")]
        [JsonPropertyName("backgroundcolor")]
        public Backgroundcolor Backgroundcolor;

        [JsonProperty("outlinecolor")]
        [JsonPropertyName("outlinecolor")]
        public Outlinecolor Outlinecolor;

        [JsonProperty("units")]
        [JsonPropertyName("units")]
        public int Units;

        [JsonProperty("status")]
        [JsonPropertyName("status")]
        public int Status;

        [JsonProperty("position")]
        [JsonPropertyName("position")]
        public int Position;

        [JsonProperty("postlabelcache")]
        [JsonPropertyName("postlabelcache")]
        public int Postlabelcache;

        [JsonProperty("align")]
        [JsonPropertyName("align")]
        public int Align;

        [JsonProperty("offsetx")]
        [JsonPropertyName("offsetx")]
        public double Offsetx;

        [JsonProperty("offsety")]
        [JsonPropertyName("offsety")]
        public double Offsety;
    }

    public class Shadowcolor
    {
        [JsonProperty("red")]
        [JsonPropertyName("red")]
        public int Red;

        [JsonProperty("green")]
        [JsonPropertyName("green")]
        public int Green;

        [JsonProperty("blue")]
        [JsonPropertyName("blue")]
        public int Blue;

        [JsonProperty("alpha")]
        [JsonPropertyName("alpha")]
        public int Alpha;
    }

    public class Style
    {
        [JsonProperty("refcount")]
        [JsonPropertyName("refcount")]
        public int Refcount;

        [JsonProperty("symbolname")]
        [JsonPropertyName("symbolname")]
        public object Symbolname;

        [JsonProperty("patternlength")]
        [JsonPropertyName("patternlength")]
        public double Patternlength;

        [JsonProperty("pattern")]
        [JsonPropertyName("pattern")]
        public List<object> Pattern;

        [JsonProperty("angle")]
        [JsonPropertyName("angle")]
        public double Angle;

        [JsonProperty("autoangle")]
        [JsonPropertyName("autoangle")]
        public int Autoangle;

        [JsonProperty("antialiased")]
        [JsonPropertyName("antialiased")]
        public int Antialiased;

        [JsonProperty("color")]
        [JsonPropertyName("color")]
        public Color Color;

        [JsonProperty("outlinecolor")]
        [JsonPropertyName("outlinecolor")]
        public Outlinecolor Outlinecolor;

        [JsonProperty("opacity")]
        [JsonPropertyName("opacity")]
        public double Opacity;

        [JsonProperty("mincolor")]
        [JsonPropertyName("mincolor")]
        public Mincolor Mincolor;

        [JsonProperty("maxcolor")]
        [JsonPropertyName("maxcolor")]
        public Maxcolor Maxcolor;

        [JsonProperty("minvalue")]
        [JsonPropertyName("minvalue")]
        public double Minvalue;

        [JsonProperty("maxvalue")]
        [JsonPropertyName("maxvalue")]
        public double Maxvalue;

        [JsonProperty("rangeitem")]
        [JsonPropertyName("rangeitem")]
        public object Rangeitem;

        [JsonProperty("rangeitemindex")]
        [JsonPropertyName("rangeitemindex")]
        public int Rangeitemindex;

        [JsonProperty("symbol")]
        [JsonPropertyName("symbol")]
        public int Symbol;

        [JsonProperty("size")]
        [JsonPropertyName("size")]
        public double Size;

        [JsonProperty("minsize")]
        [JsonPropertyName("minsize")]
        public double Minsize;

        [JsonProperty("maxsize")]
        [JsonPropertyName("maxsize")]
        public double Maxsize;

        [JsonProperty("gap")]
        [JsonPropertyName("gap")]
        public double Gap;

        [JsonProperty("initialgap")]
        [JsonPropertyName("initialgap")]
        public double Initialgap;

        [JsonProperty("linecap")]
        [JsonPropertyName("linecap")]
        public int Linecap;

        [JsonProperty("linejoin")]
        [JsonPropertyName("linejoin")]
        public int Linejoin;

        [JsonProperty("linejoinmaxsize")]
        [JsonPropertyName("linejoinmaxsize")]
        public double Linejoinmaxsize;

        [JsonProperty("width")]
        [JsonPropertyName("width")]
        public double Width;

        [JsonProperty("outlinewidth")]
        [JsonPropertyName("outlinewidth")]
        public double Outlinewidth;

        [JsonProperty("minwidth")]
        [JsonPropertyName("minwidth")]
        public double Minwidth;

        [JsonProperty("maxwidth")]
        [JsonPropertyName("maxwidth")]
        public double Maxwidth;

        [JsonProperty("offsetx")]
        [JsonPropertyName("offsetx")]
        public double Offsetx;

        [JsonProperty("offsety")]
        [JsonPropertyName("offsety")]
        public double Offsety;

        [JsonProperty("polaroffsetpixel")]
        [JsonPropertyName("polaroffsetpixel")]
        public double Polaroffsetpixel;

        [JsonProperty("polaroffsetangle")]
        [JsonPropertyName("polaroffsetangle")]
        public double Polaroffsetangle;

        [JsonProperty("minscaledenom")]
        [JsonPropertyName("minscaledenom")]
        public double Minscaledenom;

        [JsonProperty("maxscaledenom")]
        [JsonPropertyName("maxscaledenom")]
        public double Maxscaledenom;

        [JsonProperty("sizeunits")]
        [JsonPropertyName("sizeunits")]
        public int Sizeunits;
    }

    public class Symbolset
    {
        [JsonProperty("numsymbols")]
        [JsonPropertyName("numsymbols")]
        public int Numsymbols;

        [JsonProperty("maxsymbols")]
        [JsonPropertyName("maxsymbols")]
        public int Maxsymbols;

        [JsonProperty("filename")]
        [JsonPropertyName("filename")]
        public string Filename;

        [JsonProperty("imagecachesize")]
        [JsonPropertyName("imagecachesize")]
        public int Imagecachesize;
    }

    public class Utfdata
    {
    }

    public class Validation
    {
    }

    public class Web
    {
        [JsonProperty("metadata")]
        [JsonPropertyName("metadata")]
        public Metadata Metadata;

        [JsonProperty("validation")]
        [JsonPropertyName("validation")]
        public Validation Validation;

        [JsonProperty("imagepath")]
        [JsonPropertyName("imagepath")]
        public string Imagepath;

        [JsonProperty("imageurl")]
        [JsonPropertyName("imageurl")]
        public string Imageurl;

        [JsonProperty("temppath")]
        [JsonPropertyName("temppath")]
        public object Temppath;

        [JsonProperty("header")]
        [JsonPropertyName("header")]
        public object Header;

        [JsonProperty("footer")]
        [JsonPropertyName("footer")]
        public object Footer;

        [JsonProperty("empty")]
        [JsonPropertyName("empty")]
        public object Empty;

        [JsonProperty("error")]
        [JsonPropertyName("error")]
        public object Error;

        [JsonProperty("minscaledenom")]
        [JsonPropertyName("minscaledenom")]
        public double Minscaledenom;

        [JsonProperty("maxscaledenom")]
        [JsonPropertyName("maxscaledenom")]
        public double Maxscaledenom;

        [JsonProperty("mintemplate")]
        [JsonPropertyName("mintemplate")]
        public object Mintemplate;

        [JsonProperty("maxtemplate")]
        [JsonPropertyName("maxtemplate")]
        public object Maxtemplate;

        [JsonProperty("queryformat")]
        [JsonPropertyName("queryformat")]
        public string Queryformat;

        [JsonProperty("legendformat")]
        [JsonPropertyName("legendformat")]
        public string Legendformat;

        [JsonProperty("browseformat")]
        [JsonPropertyName("browseformat")]
        public string Browseformat;

        [JsonProperty("template")]
        [JsonPropertyName("template")]
        public object Template;
    }


}
