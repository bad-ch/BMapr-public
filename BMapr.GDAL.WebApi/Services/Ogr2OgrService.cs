using OSGeo.GDAL;
using OSGeo.OGR;
using OSGeo.OSR;

namespace BMapr.GDAL.WebApi.Services
{
#pragma warning disable CS8600 // Rethrow to preserve stack details
#pragma warning disable CS8602
#pragma warning disable CS8603
#pragma warning disable CS8604
    public static class StringExtension
    {
        public static bool EqualsIgnoreCase(this string compareFrom, string compareWith)
        {
            return compareFrom.ToLower() == compareWith.ToLower();
        }
    }

    /// <summary>
    /// Well known ogr2ogr script, converted from Java, older version not everything supported
    /// </summary>
    public static class Ogr2OgrService
    {
        static bool bSkipFailures = false;
        static int nGroupTransactions = 200;
        static bool bPreserveFID = false;
        static int OGRNullFID = -1;
        static int nFIDToFetch = OGRNullFID;

        static int nCoordDim = -1;

        /*
        static class GeomOperation
        {
            private GeomOperation() { }
            public static GeomOperation NONE = new GeomOperation();
            public static GeomOperation SEGMENTIZE = new GeomOperation();
            public static GeomOperation SIMPLIFY_PRESERVE_TOPOLOGY = new GeomOperation();
        }
        */

        /************************************************************************/
        /*                                main()                                */
        /************************************************************************/

        public enum GeomOperation
        {
            NONE,
            SIMPLIFY_PRESERVE_TOPOLOGY,
            SEGMENTIZE
        }

        public static void Execute(string[] args, int forceDim = -1)
        {
            //GdalConfiguration.ConfigureGdal();
            //GdalConfiguration.ConfigureOgr();

            nCoordDim = forceDim;

            string pszFormat = "ESRI Shapefile";
            string pszDataSource = null;
            string pszDestDataSource = null;
            var papszLayers = new List<string>();
            var papszDSCO = new List<string>();
            var papszLCO = new List<string>();
            bool bTransform = false;
            bool bAppend = false, bUpdate = false, bOverwrite = false;
            string pszOutputSRSDef = null;
            string pszSourceSRSDef = null;
            SpatialReference poOutputSRS = null;
            SpatialReference poSourceSRS = null;
            string pszNewLayerName = null;
            string pszWHERE = null;
            Geometry poSpatialFilter = null;
            string pszSelect;
            List<string> papszSelFields = null;
            string pszSQLStatement = null;
            int eGType = -2;
            GeomOperation eGeomOp = GeomOperation.NONE;
            double dfGeomOpParam = 0;
            var papszFieldTypesToString = new List<string>();
            bool bDisplayProgress = false;
            //ProgressCallback pfnProgress = null;
            bool bClipSrc = false;
            Geometry poClipSrc = null;
            string pszClipSrcDS = null;
            string pszClipSrcSQL = null;
            string pszClipSrcLayer = null;
            string pszClipSrcWhere = null;
            Geometry poClipDst = null;
            string pszClipDstDS = null;
            string pszClipDstSQL = null;
            string pszClipDstLayer = null;
            string pszClipDstWhere = null;
            string pszSrcEncoding = null;
            string pszDstEncoding = null;
            bool bExplodeCollections = false;
             string pszZField = null;

            Ogr.DontUseExceptions();

            /* -------------------------------------------------------------------- */
            /*      Register format(s).                                             */
            /* -------------------------------------------------------------------- */
            if (Ogr.GetDriverCount() == 0)
                Ogr.RegisterAll();

            /* -------------------------------------------------------------------- */
            /*      Processing command line arguments.                              */
            /* -------------------------------------------------------------------- */

            args = Ogr.GeneralCmdLineProcessor(args, 0); //todo difference to java

            if (args.Length < 2)
            {
                Usage();
                return;
            }

            for (int iArg = 0; iArg < args.Length; iArg++)
            {
                if (args[iArg].EqualsIgnoreCase("-f") && iArg < args.Length - 1)
                {
                    pszFormat = args[++iArg];
                }
                else if (args[iArg].EqualsIgnoreCase("-dsco") && iArg < args.Length - 1)
                {
                    papszDSCO.Add(args[++iArg]);
                }
                else if (args[iArg].EqualsIgnoreCase("-lco") && iArg < args.Length - 1)
                {
                    papszLCO.Add(args[++iArg]);
                }
                else if (args[iArg].EqualsIgnoreCase("-preserve_fid"))
                {
                    bPreserveFID = true;
                }
                else if (args[iArg].Length >= 5 && args[iArg].Substring(0, 5).EqualsIgnoreCase("-skip")) //java is start- and endIndex 0,5, c# is startIndex, length
                {
                    bSkipFailures = true;
                    nGroupTransactions = 1; /* #2409 */
                }
                else if (args[iArg].EqualsIgnoreCase("-append"))
                {
                    bAppend = true;
                    bUpdate = true;
                }
                else if (args[iArg].EqualsIgnoreCase("-overwrite"))
                {
                    bOverwrite = true;
                    bUpdate = true;
                }
                else if (args[iArg].EqualsIgnoreCase("-update"))
                {
                    bUpdate = true;
                }
                else if (args[iArg].EqualsIgnoreCase("-fid") && args[iArg + 1] != null)
                {
                    nFIDToFetch = System.Convert.ToInt32(args[++iArg]);
                }
                else if (args[iArg].EqualsIgnoreCase("-sql") && args[iArg + 1] != null)
                {
                    pszSQLStatement = args[++iArg];
                }
                else if (args[iArg].EqualsIgnoreCase("-nln") && iArg < args.Length - 1)
                {
                    pszNewLayerName = args[++iArg];
                }
                else if (args[iArg].EqualsIgnoreCase("-nlt") && iArg < args.Length - 1)
                {
                    if (args[iArg + 1].EqualsIgnoreCase("NONE"))
                        eGType = (int)OSGeo.OGR.wkbGeometryType.wkbNone;
                    else if (args[iArg + 1].EqualsIgnoreCase("GEOMETRY"))
                        eGType = (int)OSGeo.OGR.wkbGeometryType.wkbUnknown;
                    else if (args[iArg + 1].EqualsIgnoreCase("POINT"))
                        eGType = (int)OSGeo.OGR.wkbGeometryType.wkbPoint;
                    else if (args[iArg + 1].EqualsIgnoreCase("LINESTRING"))
                        eGType = (int)OSGeo.OGR.wkbGeometryType.wkbLineString;
                    else if (args[iArg + 1].EqualsIgnoreCase("POLYGON"))
                        eGType = (int)OSGeo.OGR.wkbGeometryType.wkbPolygon;
                    else if (args[iArg + 1].EqualsIgnoreCase("GEOMETRYCOLLECTION"))
                        eGType = (int)OSGeo.OGR.wkbGeometryType.wkbGeometryCollection;
                    else if (args[iArg + 1].EqualsIgnoreCase("MULTIPOINT"))
                        eGType = (int)OSGeo.OGR.wkbGeometryType.wkbMultiPoint;
                    else if (args[iArg + 1].EqualsIgnoreCase("MULTILINESTRING"))
                        eGType = (int)OSGeo.OGR.wkbGeometryType.wkbMultiLineString;
                    else if (args[iArg + 1].EqualsIgnoreCase("MULTIPOLYGON"))
                        eGType = (int)OSGeo.OGR.wkbGeometryType.wkbMultiPolygon;
                    else if (args[iArg + 1].EqualsIgnoreCase("GEOMETRY25D"))
                        eGType = (int)OSGeo.OGR.wkbGeometryType.wkbUnknown | Ogr.wkb25DBit;
                    else if (args[iArg + 1].EqualsIgnoreCase("POINT25D"))
                        eGType = (int)OSGeo.OGR.wkbGeometryType.wkbPoint25D;
                    else if (args[iArg + 1].EqualsIgnoreCase("LINESTRING25D"))
                        eGType = (int)OSGeo.OGR.wkbGeometryType.wkbLineString25D;
                    else if (args[iArg + 1].EqualsIgnoreCase("POLYGON25D"))
                        eGType = (int)OSGeo.OGR.wkbGeometryType.wkbPolygon25D;
                    else if (args[iArg + 1].EqualsIgnoreCase("GEOMETRYCOLLECTION25D"))
                        eGType = (int)OSGeo.OGR.wkbGeometryType.wkbGeometryCollection25D;
                    else if (args[iArg + 1].EqualsIgnoreCase("MULTIPOINT25D"))
                        eGType = (int)OSGeo.OGR.wkbGeometryType.wkbMultiPoint25D;
                    else if (args[iArg + 1].EqualsIgnoreCase("MULTILINESTRING25D"))
                        eGType = (int)OSGeo.OGR.wkbGeometryType.wkbMultiLineString25D;
                    else if (args[iArg + 1].EqualsIgnoreCase("MULTIPOLYGON25D"))
                        eGType = (int)OSGeo.OGR.wkbGeometryType.wkbMultiPolygon25D;
                    else
                    {
                        Console.Write("-nlt " + args[iArg + 1] + ": type not recognised.");
                        break;
                    }
                    iArg++;
                }
                else if ((args[iArg].EqualsIgnoreCase("-tg") ||
                          args[iArg].EqualsIgnoreCase("-gt")) && iArg < args.Length - 1)
                {
                    nGroupTransactions = System.Convert.ToInt32(args[++iArg]);
                }
                else if (args[iArg].EqualsIgnoreCase("-s_srs") && iArg < args.Length - 1)
                {
                    pszSourceSRSDef = args[++iArg];
                }
                else if (args[iArg].EqualsIgnoreCase("-a_srs") && iArg < args.Length - 1)
                {
                    pszOutputSRSDef = args[++iArg];
                }
                else if (args[iArg].EqualsIgnoreCase("-t_srs") && iArg < args.Length - 1)
                {
                    pszOutputSRSDef = args[++iArg];
                    bTransform = true;
                }
                else if (args[iArg].EqualsIgnoreCase("-spat") && iArg + 4 < args.Length)
                {
                    Geometry oRing = new Geometry(OSGeo.OGR.wkbGeometryType.wkbLinearRing);
                    double xmin = System.Convert.ToDouble(args[++iArg]);
                    double ymin = System.Convert.ToDouble(args[++iArg]);
                    double xmax = System.Convert.ToDouble(args[++iArg]);
                    double ymax = System.Convert.ToDouble(args[++iArg]);
                    oRing.AddPoint(xmin, ymin, 0); //todo java has no z value
                    oRing.AddPoint(xmin, ymax, 0); //todo java has no z value
                    oRing.AddPoint(xmax, ymax, 0); //todo java has no z value
                    oRing.AddPoint(xmax, ymin, 0); //todo java has no z value
                    oRing.AddPoint(xmin, ymin, 0); //todo java has no z value

                    poSpatialFilter = new Geometry(OSGeo.OGR.wkbGeometryType.wkbPolygon);
                    poSpatialFilter.AddGeometry(oRing);
                }
                else if (args[iArg].EqualsIgnoreCase("-where") && args[iArg + 1] != null)
                {
                    pszWHERE = args[++iArg];
                }
                else if (args[iArg].EqualsIgnoreCase("-select") && args[iArg + 1] != null)
                {
                    pszSelect = args[++iArg];
                    papszSelFields = pszSelect.Split(new string[] { " ," }, StringSplitOptions.None).ToList();
                }
                else if (args[iArg].EqualsIgnoreCase("-simplify") && iArg < args.Length - 1)
                {
                    eGeomOp = GeomOperation.SIMPLIFY_PRESERVE_TOPOLOGY;
                    dfGeomOpParam = System.Convert.ToDouble(args[++iArg]);
                }
                else if (args[iArg].EqualsIgnoreCase("-segmentize") && iArg < args.Length - 1)
                {
                    eGeomOp = GeomOperation.SEGMENTIZE;
                    dfGeomOpParam = System.Convert.ToDouble(args[++iArg]);
                }
                else if (args[iArg].EqualsIgnoreCase("-fieldTypeToString") && iArg < args.Length - 1)
                {
                    var tokenizer = args[++iArg].Split(new string[] { " ," }, StringSplitOptions.None).ToList();

                    foreach (var token in tokenizer)
                    {
                        if (token.EqualsIgnoreCase("Integer") ||
                            token.EqualsIgnoreCase("Real") ||
                            token.EqualsIgnoreCase("String") ||
                            token.EqualsIgnoreCase("Date") ||
                            token.EqualsIgnoreCase("Time") ||
                            token.EqualsIgnoreCase("DateTime") ||
                            token.EqualsIgnoreCase("Binary") ||
                            token.EqualsIgnoreCase("IntegerList") ||
                            token.EqualsIgnoreCase("RealList") ||
                            token.EqualsIgnoreCase("StringList"))
                        {
                            papszFieldTypesToString.Add(token);
                        }
                        else if (token.EqualsIgnoreCase("All"))
                        {
                            papszFieldTypesToString = new List<string>();
                            papszFieldTypesToString.Add("All");
                            break;
                        }
                        else
                        {
                            Console.WriteLine("Unhandled type for fieldtypeasstring option : " + token);
                            Usage();
                        }
                    }
                }
                else if (args[iArg].EqualsIgnoreCase("-progress"))
                {
                    bDisplayProgress = true;
                }
                /*else if( args[iArg].EqualsIgnoreCase("-wrapdateline") )
                {
                    bWrapDateline = true;
                }
                */
                else if (args[iArg].EqualsIgnoreCase("-clipsrc") && iArg < args.Length - 1)
                {
                    bClipSrc = true;
                    if (IsNumber(args[iArg + 1]) && iArg < args.Length - 4)
                    {
                        Geometry oRing = new Geometry(OSGeo.OGR.wkbGeometryType.wkbLinearRing);
                        double xmin = System.Convert.ToDouble(args[++iArg]);
                        double ymin = System.Convert.ToDouble(args[++iArg]);
                        double xmax = System.Convert.ToDouble(args[++iArg]);
                        double ymax = System.Convert.ToDouble(args[++iArg]);
                        oRing.AddPoint(xmin, ymin, 0); //todo java has no z value
                        oRing.AddPoint(xmin, ymax, 0); //todo java has no z value
                        oRing.AddPoint(xmax, ymax, 0); //todo java has no z value
                        oRing.AddPoint(xmax, ymin, 0); //todo java has no z value
                        oRing.AddPoint(xmin, ymin, 0); //todo java has no z value

                        poClipSrc = new Geometry(OSGeo.OGR.wkbGeometryType.wkbPolygon);
                        poClipSrc.AddGeometry(oRing);
                    }
                    else if ((args[iArg + 1].Length >= 7 && args[iArg + 1].Substring(0, 7).EqualsIgnoreCase("POLYGON")) ||
                             (args[iArg + 1].Length >= 12 && args[iArg + 1].Substring(0, 12).EqualsIgnoreCase("MULTIPOLYGON")))
                    {
                        poClipSrc = Geometry.CreateFromWkt(args[iArg + 1]);
                        if (poClipSrc == null)
                        {
                            Console.WriteLine("FAILURE: Invalid geometry. Must be a valid POLYGON or MULTIPOLYGON WKT\n\n");
                            Usage();
                        }
                        iArg++;
                    }
                    else if (args[iArg + 1].EqualsIgnoreCase("spat_extent"))
                    {
                        iArg++;
                    }
                    else
                    {
                        pszClipSrcDS = args[iArg + 1];
                        iArg++;
                    }
                }
                else if (args[iArg].EqualsIgnoreCase("-clipsrcsql") && iArg < args.Length - 1)
                {
                    pszClipSrcSQL = args[iArg + 1];
                    iArg++;
                }
                else if (args[iArg].EqualsIgnoreCase("-clipsrclayer") && iArg < args.Length - 1)
                {
                    pszClipSrcLayer = args[iArg + 1];
                    iArg++;
                }
                else if (args[iArg].EqualsIgnoreCase("-clipsrcwhere") && iArg < args.Length - 1)
                {
                    pszClipSrcWhere = args[iArg + 1];
                    iArg++;
                }
                else if (args[iArg].EqualsIgnoreCase("-clipdst") && iArg < args.Length - 1)
                {
                    if (IsNumber(args[iArg + 1]) && iArg < args.Length - 4)
                    {
                        Geometry oRing = new Geometry(OSGeo.OGR.wkbGeometryType.wkbLinearRing);
                        double xmin = System.Convert.ToDouble(args[++iArg]);
                        double ymin = System.Convert.ToDouble(args[++iArg]);
                        double xmax = System.Convert.ToDouble(args[++iArg]);
                        double ymax = System.Convert.ToDouble(args[++iArg]);
                        oRing.AddPoint(xmin, ymin, 0); //todo java has no z value
                        oRing.AddPoint(xmin, ymax, 0); //todo java has no z value
                        oRing.AddPoint(xmax, ymax, 0); //todo java has no z value
                        oRing.AddPoint(xmax, ymin, 0); //todo java has no z value
                        oRing.AddPoint(xmin, ymin, 0); //todo java has no z value

                        poClipDst = new Geometry(OSGeo.OGR.wkbGeometryType.wkbPolygon);
                        poClipDst.AddGeometry(oRing);
                    }
                    else if ((args[iArg + 1].Length >= 7 && args[iArg + 1].Substring(0, 7).EqualsIgnoreCase("POLYGON")) ||
                             (args[iArg + 1].Length >= 12 && args[iArg + 1].Substring(0, 12).EqualsIgnoreCase("MULTIPOLYGON")))
                    {
                        poClipDst = Geometry.CreateFromWkt(args[iArg + 1]);
                        if (poClipDst == null)
                        {
                            Console.WriteLine("FAILURE: Invalid geometry. Must be a valid POLYGON or MULTIPOLYGON WKT\n\n");
                            Usage();
                        }
                        iArg++;
                    }
                    else if (args[iArg + 1].EqualsIgnoreCase("spat_extent"))
                    {
                        iArg++;
                    }
                    else
                    {
                        pszClipDstDS = args[iArg + 1];
                        iArg++;
                    }
                }
                else if (args[iArg].EqualsIgnoreCase("-clipdstsql") && iArg < args.Length - 1)
                {
                    pszClipDstSQL = args[iArg + 1];
                    iArg++;
                }
                else if (args[iArg].EqualsIgnoreCase("-clipdstlayer") && iArg < args.Length - 1)
                {
                    pszClipDstLayer = args[iArg + 1];
                    iArg++;
                }
                else if (args[iArg].EqualsIgnoreCase("-clipdstwhere") && iArg < args.Length - 1)
                {
                    pszClipDstWhere = args[iArg + 1];
                    iArg++;
                }
                else if (args[iArg].EqualsIgnoreCase("-explodecollections"))
                {
                    bExplodeCollections = true;
                }
                else if (args[iArg].EqualsIgnoreCase("-zfield") && iArg < args.Length - 1)
                {
                    pszZField = args[iArg + 1];
                    iArg++;
                }
                else if (args[iArg][0] == '-') //Java .charAt(0)
                {
                    Usage();
                }
                else if (pszDestDataSource == null)
                    pszDestDataSource = args[iArg];
                else if (pszDataSource == null)
                    pszDataSource = args[iArg];
                else
                    papszLayers.Add(args[iArg]);
            }

            if (pszDataSource == null)
                Usage();

            if (bPreserveFID && bExplodeCollections)
            {
                Console.WriteLine("FAILURE: cannot use -preserve_fid and -explodecollections at the same time\n\n");
                Usage();
            }

            if (bClipSrc && pszClipSrcDS != null)
            {
                poClipSrc = LoadGeometry(pszClipSrcDS, pszClipSrcSQL, pszClipSrcLayer, pszClipSrcWhere);
                if (poClipSrc == null)
                {
                    Console.WriteLine("FAILURE: cannot load source clip geometry\n\n");
                    Usage();
                }
            }
            else if (bClipSrc && poClipSrc == null)
            {
                if (poSpatialFilter != null)
                    poClipSrc = poSpatialFilter.Clone();
                if (poClipSrc == null)
                {
                    Console.WriteLine("FAILURE: -clipsrc must be used with -spat option or a\n" +
                                     "bounding box, WKT string or datasource must be specified\n\n");
                    Usage();
                }
            }

            if (pszClipDstDS != null)
            {
                poClipDst = LoadGeometry(pszClipDstDS, pszClipDstSQL, pszClipDstLayer, pszClipDstWhere);
                if (poClipDst == null)
                {
                    Console.WriteLine("FAILURE: cannot load dest clip geometry\n\n");
                    Usage();
                }
            }
            /* -------------------------------------------------------------------- */
            /*      Open data source.                                               */
            /* -------------------------------------------------------------------- */
            DataSource poDS;

            poDS = Ogr.Open(pszDataSource, 0); //java false

            /* -------------------------------------------------------------------- */
            /*      Report failure                                                  */
            /* -------------------------------------------------------------------- */
            if (poDS == null)
            {
                Console.WriteLine("FAILURE:\n" +
                                   "Unable to open datasource ` " + pszDataSource + "' with the following drivers.");

                for (int iDriver = 0; iDriver < Ogr.GetDriverCount(); iDriver++)
                {
                    Console.WriteLine("  . " + Ogr.GetDriver(iDriver).GetName());
                }

                return;
            }

            /* -------------------------------------------------------------------- */
            /*      Try opening the output datasource as an existing, writable      */
            /* -------------------------------------------------------------------- */
            DataSource poODS = null;
            OSGeo.OGR.Driver poDriver = null;

            if (bUpdate)
            {
                poODS = Ogr.Open(pszDestDataSource, 1); //java true
                if (poODS == null)
                {
                    if (bOverwrite || bAppend)
                    {
                        poODS = Ogr.Open(pszDestDataSource, 0);
                        if (poODS == null)
                        {
                            /* ok the datasource doesn't exist at all */
                            bUpdate = false;
                        }
                        else
                        {
                            poODS.Dispose();
                            poODS = null;
                        }
                    }

                    if (bUpdate)
                    {
                        Console.WriteLine("FAILURE:\n" +
                                           "Unable to open existing output datasource `" + pszDestDataSource + "'.");
                        return;
                    }
                }

                else if (papszDSCO.Count > 0) // java .size()
                {
                    Console.WriteLine("WARNING: Datasource creation options ignored since an existing datasource\n" +
                                       "         being updated.");
                }

                if (poODS != null)
                    poDriver = poODS.GetDriver();
            }

            /* -------------------------------------------------------------------- */
            /*      Find the output driver.                                         */
            /* -------------------------------------------------------------------- */
            if (!bUpdate)
            {
                int iDriver;

                poDriver = Ogr.GetDriverByName(pszFormat);
                if (poDriver == null)
                {
                    Console.WriteLine("Unable to find driver `" + pszFormat + "'.");
                    Console.WriteLine("The following drivers are available:");

                    for (iDriver = 0; iDriver < Ogr.GetDriverCount(); iDriver++)
                    {
                        Console.WriteLine("  . " + Ogr.GetDriver(iDriver).GetName());
                    }
                    return;
                }

                if (poDriver.TestCapability(Ogr.ODrCCreateDataSource) == false)
                {
                    Console.WriteLine(pszFormat + " driver does not support data source creation.");
                    return;
                }

                /* -------------------------------------------------------------------- */
                /*      Special case to improve user experience when translating        */
                /*      a datasource with multiple layers into a shapefile. If the      */
                /*      user gives a target datasource with .shp and it does not exist, */
                /*      the shapefile driver will try to create a file, but this is not */
                /*      appropriate because here we have several layers, so create      */
                /*      a directory instead.                                            */
                /* -------------------------------------------------------------------- */
                if (poDriver.GetName().EqualsIgnoreCase("ESRI Shapefile") &&
                    pszSQLStatement == null &&
                    (papszLayers.Count > 1 ||
                     (papszLayers.Count == 0 && poDS.GetLayerCount() > 1)) &&
                    pszNewLayerName == null &&
                    (pszDestDataSource.EndsWith(".shp") || pszDestDataSource.EndsWith(".SHP")))
                {
                    var directoryInfo = new DirectoryInfo(pszDestDataSource);
                    if (!directoryInfo.Exists)
                    {
                        Console.WriteLine(
                            "Failed to create directory " + pszDestDataSource + "\n" +
                            "for shapefile datastore.");
                        return;
                    }
                }

                /* -------------------------------------------------------------------- */
                /*      Create the output data source.                                  */
                /* -------------------------------------------------------------------- */
                poODS = poDriver.CreateDataSource(pszDestDataSource, papszDSCO.ToArray());
                if (poODS == null)
                {
                    Console.WriteLine(pszFormat + " driver failed to create " + pszDestDataSource);
                    return;
                }
            }

            /* -------------------------------------------------------------------- */
            /*      Parse the output SRS definition if possible.                    */
            /* -------------------------------------------------------------------- */
            if (pszOutputSRSDef != null)
            {
                poOutputSRS = new SpatialReference("");
                if (poOutputSRS.SetFromUserInput(pszOutputSRSDef) != 0)
                {
                    Console.WriteLine("Failed to process SRS definition: " + pszOutputSRSDef);
                    return;
                }
                //GDAL 3.0 poOutputSRS.SetAxisMappingStrategy(AxisMappingStrategy.OAMS_TRADITIONAL_GIS_ORDER); //important change !!!
            }

            /* -------------------------------------------------------------------- */
            /*      Parse the source SRS definition if possible.                    */
            /* -------------------------------------------------------------------- */
            if (pszSourceSRSDef != null)
            {
                poSourceSRS = new SpatialReference("");
                if (poSourceSRS.SetFromUserInput(pszSourceSRSDef) != 0)
                {
                    Console.WriteLine("Failed to process SRS definition: " + pszSourceSRSDef);
                    return;
                }
                //GDAL 3.0 poSourceSRS.SetAxisMappingStrategy(AxisMappingStrategy.OAMS_TRADITIONAL_GIS_ORDER); //important change !!!
            }

            /* -------------------------------------------------------------------- */
            /*      Special case for -sql clause.  No source layers required.       */
            /* -------------------------------------------------------------------- */
            if (pszSQLStatement != null)
            {
                OSGeo.OGR.Layer poResultSet;

                if (pszWHERE != null)
                    Console.WriteLine("-where clause ignored in combination with -sql.");
                if (papszLayers.Count > 0)
                    Console.WriteLine("layer names ignored in combination with -sql.");

                poResultSet = poDS.ExecuteSQL(pszSQLStatement, poSpatialFilter,
                    null);

                if (poResultSet != null)
                {
                    long nCountLayerFeatures = 0;
                    if (bDisplayProgress)
                    {
                        if (!poResultSet.TestCapability(Ogr.OLCFastFeatureCount))
                        {
                            Console.WriteLine("Progress turned off as fast feature count is not available.");
                            bDisplayProgress = false;
                        }
                        else
                        {
                            nCountLayerFeatures = poResultSet.GetFeatureCount(1); //c# has force
                            //TODO pfnProgress = new TermProgressCallback();
                        }
                    }

                    /* -------------------------------------------------------------------- */
                    /*      Special case to improve user experience when translating into   */
                    /*      single file shapefile and source has only one layer, and that   */
                    /*      the layer name isn't specified                                  */
                    /* -------------------------------------------------------------------- */
                    if (poDriver.GetName().EqualsIgnoreCase("ESRI Shapefile") &&
                        pszNewLayerName == null)
                    {
                        var f = new DirectoryInfo(pszDestDataSource);
                        if (f.Exists && f.GetFiles().Count() == 0) //todo correct
                        {
                            pszNewLayerName = f.Name;
                            int posPoint = pszNewLayerName.LastIndexOf('.');
                            if (posPoint != -1)
                                pszNewLayerName = pszNewLayerName.Substring(0, posPoint);
                        }
                    }

                    if (!TranslateLayer(poDS, poResultSet, poODS, papszLCO,
                        pszNewLayerName, bTransform, poOutputSRS,
                        poSourceSRS, papszSelFields, bAppend, eGType,
                        bOverwrite, eGeomOp, dfGeomOpParam, papszFieldTypesToString,
                        nCountLayerFeatures, poClipSrc, poClipDst, bExplodeCollections,
                        pszZField, pszWHERE))//, pfnProgress))
                    {
                        Console.WriteLine(
                            "Terminating translation prematurely after failed\n" +
                            "translation from sql statement.");

                        return;
                    }
                    poDS.ReleaseResultSet(poResultSet);
                }
            }
            else
            {
                int nLayerCount = 0;
                OSGeo.OGR.Layer[] papoLayers = null;

                /* -------------------------------------------------------------------- */
                /*      Process each data source layer.                                 */
                /* -------------------------------------------------------------------- */
                if (papszLayers.Count == 0)
                {
                    nLayerCount = poDS.GetLayerCount();
                    papoLayers = new OSGeo.OGR.Layer[nLayerCount];

                    for (int iLayer = 0;
                        iLayer < nLayerCount;
                        iLayer++)
                    {
                        OSGeo.OGR.Layer poLayer = poDS.GetLayerByIndex(iLayer);

                        if (poLayer == null)
                        {
                            Console.WriteLine("FAILURE: Couldn't fetch advertised layer " + iLayer + "!");
                            return;
                        }

                        papoLayers[iLayer] = poLayer;
                    }
                }
                /* -------------------------------------------------------------------- */
                /*      Process specified data source layers.                           */
                /* -------------------------------------------------------------------- */
                else
                {
                    nLayerCount = papszLayers.Count;
                    papoLayers = new OSGeo.OGR.Layer[nLayerCount];

                    for (int iLayer = 0;
                        iLayer < papszLayers.Count;
                        iLayer++)
                    {
                        OSGeo.OGR.Layer poLayer = poDS.GetLayerByName((String)papszLayers[iLayer]);

                        if (poLayer == null)
                        {
                            Console.WriteLine("FAILURE: Couldn't fetch advertised layer " + (String)papszLayers[iLayer] + "!");
                            return;
                        }

                        papoLayers[iLayer] = poLayer;
                    }
                }

                /* -------------------------------------------------------------------- */
                /*      Special case to improve user experience when translating into   */
                /*      single file shapefile and source has only one layer, and that   */
                /*      the layer name isn't specified                                  */
                /* -------------------------------------------------------------------- */
                if (poDriver.GetName().EqualsIgnoreCase("ESRI Shapefile") &&
                    nLayerCount == 1 && pszNewLayerName == null)
                {
                    var f = new DirectoryInfo(pszDestDataSource);
                    if (f.Exists && f.GetFiles().Length == 0)
                    {
                        pszNewLayerName = f.Name;
                        int posPoint = pszNewLayerName.LastIndexOf('.');
                        if (posPoint != -1)
                            pszNewLayerName = pszNewLayerName.Substring(0, posPoint);
                    }
                }

                long[] panLayerCountFeatures = new long[nLayerCount];
                long nCountLayersFeatures = 0;
                long nAccCountFeatures = 0;

                /* First pass to apply filters and count all features if necessary */
                for (int iLayer = 0;
                    iLayer < nLayerCount;
                    iLayer++)
                {
                    OSGeo.OGR.Layer poLayer = papoLayers[iLayer];

                    if (pszWHERE != null)
                    {
                        if (poLayer.SetAttributeFilter(pszWHERE) != Ogr.OGRERR_NONE)
                        {
                            Console.WriteLine("FAILURE: SetAttributeFilter(" + pszWHERE + ") failed.");
                            if (!bSkipFailures)
                                return;
                        }
                    }

                    if (poSpatialFilter != null)
                        poLayer.SetSpatialFilter(poSpatialFilter);

                    if (bDisplayProgress)
                    {
                        if (!poLayer.TestCapability(Ogr.OLCFastFeatureCount))
                        {
                            Console.WriteLine("Progress turned off as fast feature count is not available.");
                            bDisplayProgress = false;
                        }
                        else
                        {
                            panLayerCountFeatures[iLayer] = poLayer.GetFeatureCount(1); //force
                            nCountLayersFeatures += panLayerCountFeatures[iLayer];
                        }
                    }
                }

                /* Second pass to do the real job */
                for (int iLayer = 0;
                    iLayer < nLayerCount;
                    iLayer++)
                {
                    OSGeo.OGR.Layer poLayer = papoLayers[iLayer];

                    if (bDisplayProgress)
                    {
                        /* pfnProgress = new GDALScaledProgress(
                             nAccCountFeatures * 1.0 / nCountLayersFeatures,
                             (nAccCountFeatures + panLayerCountFeatures[iLayer]) * 1.0 / nCountLayersFeatures,
                             new TermProgressCallback());
                        */
                    }

                    nAccCountFeatures += panLayerCountFeatures[iLayer];

                    if (!TranslateLayer(poDS, poLayer, poODS, papszLCO,
                            pszNewLayerName, bTransform, poOutputSRS,
                            poSourceSRS, papszSelFields, bAppend, eGType,
                            bOverwrite, eGeomOp, dfGeomOpParam, papszFieldTypesToString,
                            panLayerCountFeatures[iLayer], poClipSrc, poClipDst, bExplodeCollections,
                            pszZField, pszWHERE) //, pfnProgress)
                        && !bSkipFailures)
                    {
                        Console.WriteLine(
                            "Terminating translation prematurely after failed\n" +
                            "translation of layer " + poLayer.GetLayerDefn().GetName() + " (use -skipfailures to skip errors)");

                        return;
                    }
                }
            }

            /* -------------------------------------------------------------------- */
            /*      Close down.                                                     */
            /* -------------------------------------------------------------------- */
            /* We must explicitly destroy the output dataset in order the file */
            /* to be properly closed ! */
            poODS.Dispose();
            poDS.Dispose();
        }

        /************************************************************************/
        /*                               Usage()                                */
        /************************************************************************/

        static void Usage()

        {
            Console.WriteLine("Usage: ogr2ogr [--help-general] [-skipfailures] [-append] [-update] [-gt n]\n" +
                             "               [-select field_list] [-where restricted_where] \n" +
                             "               [-progress] [-sql <sql statement>] \n" +
                             "               [-spat xmin ymin xmax ymax] [-preserve_fid] [-fid FID]\n" +
                             "               [-a_srs srs_def] [-t_srs srs_def] [-s_srs srs_def]\n" +
                             "               [-f format_name] [-overwrite] [[-dsco NAME=VALUE] ...]\n" +
                             "               [-simplify tolerance]\n" +
                             // "               [-segmentize max_dist] [-fieldTypeToString All|(type1[,type2]*)]\n" +
                             "               [-fieldTypeToString All|(type1[,type2]*)] [-explodecollections]\n" +
                             "               dst_datasource_name src_datasource_name\n" +
                             "               [-lco NAME=VALUE] [-nln name] [-nlt type] [layer [layer ...]]\n" +
                             "\n" +
                             " -f format_name: output file format name, possible values are:\n");

            for (int iDriver = 0; iDriver < Ogr.GetDriverCount(); iDriver++)
            {
                var poDriver = Ogr.GetDriver(iDriver);

                if (poDriver.TestCapability(Ogr.ODrCCreateDataSource))
                    Console.WriteLine("     -f \"" + poDriver.GetName() + "\"\n");
            }

            Console.WriteLine(" -append: Append to existing layer instead of creating new if it exists\n" +
                             " -overwrite: delete the output layer and recreate it empty\n" +
                             " -update: Open existing output datasource in update mode\n" +
                             " -progress: Display progress on terminal. Only works if input layers have the \"fast feature count\" capability\n" +
                             " -select field_list: Comma-delimited list of fields from input layer to\n" +
                             "                     copy to the new layer (defaults to all)\n" +
                             " -where restricted_where: Attribute query (like SQL WHERE)\n" +
                             " -sql statement: Execute given SQL statement and save result.\n" +
                             " -skipfailures: skip features or layers that fail to convert\n" +
                             " -gt n: group n features per transaction (default 200)\n" +
                             " -spat xmin ymin xmax ymax: spatial query extents\n" +
                             " -simplify tolerance: distance tolerance for simplification.\n" +
                             //" -segmentize max_dist: maximum distance between 2 nodes.\n" +
                             //"                       Used to create intermediate points\n" +
                             " -dsco NAME=VALUE: Dataset creation option (format specific)\n" +
                             " -lco  NAME=VALUE: Layer creation option (format specific)\n" +
                             " -nln name: Assign an alternate name to the new layer\n" +
                             " -nlt type: Force a geometry type for new layer.  One of NONE, GEOMETRY,\n" +
                             "      POINT, LINESTRING, POLYGON, GEOMETRYCOLLECTION, MULTIPOINT,\n" +
                             "      MULTIPOLYGON, or MULTILINESTRING.  Add \"25D\" for 3D layers.\n" +
                             "      Default is type of source layer.\n" +
                             " -fieldTypeToString type1,...: Converts fields of specified types to\n" +
                             "      fields of type string in the new layer. Valid types are : \n" +
                             "      Integer, Real, String, Date, Time, DateTime, Binary, IntegerList, RealList,\n" +
                             "      StringList. Special value All can be used to convert all fields to strings.\n");

            Console.WriteLine(" -a_srs srs_def: Assign an output SRS\n" +
                             " -t_srs srs_def: Reproject/transform to this SRS on output\n" +
                             " -s_srs srs_def: Override source SRS\n" +
                             "\n" +
                             " Srs_def can be a full WKT definition (hard to escape properly),\n" +
                             " or a well known definition (i.e. EPSG:4326) or a file with a WKT\n" +
                             " definition.\n");

            return;
        }

        static int CSLFindString(List<string> v,  string str)
        {
            int i = 0;
            //Enumeration e = v.elements();
            foreach (var item in v)
            {
                // string strIter = (String)e.nextElement();
                if (item.EqualsIgnoreCase(str))
                    return i;
                i++;
            }
            return -1;
        }

        static bool IsNumber( string pszStr)
        {
            try
            {
                System.Convert.ToDouble(pszStr);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        static Geometry LoadGeometry( string pszDS,
             string pszSQL,
             string pszLyr,
             string pszWhere)
        {
            DataSource poDS;
            OSGeo.OGR.Layer poLyr;
            Feature poFeat;
            Geometry poGeom = null;

            poDS = Ogr.Open(pszDS, 0);
            if (poDS == null)
                return null;

            if (pszSQL != null)
                poLyr = poDS.ExecuteSQL(pszSQL, null, null);
            else if (pszLyr != null)
                poLyr = poDS.GetLayerByName(pszLyr);
            else
                poLyr = poDS.GetLayerByIndex(0);

            if (poLyr == null)
            {
                Console.WriteLine("Failed to identify source layer from datasource.\n");
                poDS.Dispose();
                return null;
            }

            if (pszWhere != null)
                poLyr.SetAttributeFilter(pszWhere);

            while ((poFeat = poLyr.GetNextFeature()) != null)
            {
                Geometry poSrcGeom = poFeat.GetGeometryRef();
                if (poSrcGeom != null)
                {
                    int eType = wkbFlatten((int)poSrcGeom.GetGeometryType()); //todo correct

                    if (poGeom == null)
                        poGeom = new Geometry(OSGeo.OGR.wkbGeometryType.wkbMultiPolygon);

                    if (eType == (int)OSGeo.OGR.wkbGeometryType.wkbPolygon)
                        poGeom.AddGeometry(poSrcGeom);
                    else if (eType == (int)OSGeo.OGR.wkbGeometryType.wkbMultiPolygon)
                    {
                        int iGeom;
                        int nGeomCount = poSrcGeom.GetGeometryCount();

                        for (iGeom = 0; iGeom < nGeomCount; iGeom++)
                        {
                            poGeom.AddGeometry(poSrcGeom.GetGeometryRef(iGeom));
                        }
                    }
                    else
                    {
                        Console.WriteLine("ERROR: Geometry not of polygon type.\n");
                        if (pszSQL != null)
                            poDS.ReleaseResultSet(poLyr);
                        poDS.Dispose();
                        return null;
                    }
                }
            }

            if (pszSQL != null)
                poDS.ReleaseResultSet(poLyr);
            poDS.Dispose();

            return poGeom;
        }


        static int wkbFlatten(int eType)
        {
            return eType; //todo what does this & (~OSGeo.OGR.wkbGeometryType.wkb25DBit);
        }

        /************************************************************************/
        /*                               SetZ()                                 */
        /************************************************************************/
        static void SetZ(Geometry poGeom, double dfZ)
        {
            if (poGeom == null)
                return;
            switch (wkbFlatten((int)poGeom.GetGeometryType()))
            {
                case (int)OSGeo.OGR.wkbGeometryType.wkbPoint:
                    poGeom.SetPoint(0, poGeom.GetX(0), poGeom.GetY(0), dfZ); //correct ?
                    break;

                case (int)OSGeo.OGR.wkbGeometryType.wkbLineString:
                case (int)OSGeo.OGR.wkbGeometryType.wkbLinearRing:
                    {
                        int i;
                        for (i = 0; i < poGeom.GetPointCount(); i++)
                            poGeom.SetPoint(i, poGeom.GetX(i), poGeom.GetY(i), dfZ);
                        break;
                    }

                case (int)OSGeo.OGR.wkbGeometryType.wkbPolygon:
                case (int)OSGeo.OGR.wkbGeometryType.wkbMultiPoint:
                case (int)OSGeo.OGR.wkbGeometryType.wkbMultiLineString:
                case (int)OSGeo.OGR.wkbGeometryType.wkbMultiPolygon:
                case (int)OSGeo.OGR.wkbGeometryType.wkbGeometryCollection:
                    {
                        int i;
                        for (i = 0; i < poGeom.GetGeometryCount(); i++)
                            SetZ(poGeom.GetGeometryRef(i), dfZ);
                        break;
                    }

                default:
                    break;
            }
        }


        /************************************************************************/
        /*                           TranslateLayer()                           */
        /************************************************************************/

        static bool TranslateLayer(DataSource poSrcDS,
                OSGeo.OGR.Layer poSrcLayer,
                DataSource poDstDS,
                List<string> papszLCO,
                string pszNewLayerName,
                bool bTransform,
                SpatialReference poOutputSRS,
                SpatialReference poSourceSRS,
                List<string> papszSelFields,
                bool bAppend, int eGType, bool bOverwrite,
                GeomOperation eGeomOp,
                double dfGeomOpParam,
                List<string> papszFieldTypesToString,
                long nCountLayerFeatures,
                Geometry poClipSrc,
                Geometry poClipDst,
                bool bExplodeCollections,
                string pszZField,
                string pszWHERE) //,
                                 //ProgressCallback pfnProgress)

        {
            OSGeo.OGR.Layer poDstLayer;
            FeatureDefn poSrcFDefn;
            int eErr;
            bool bForceToPolygon = false;
            bool bForceToMultiPolygon = false;
            bool bForceToMultiLineString = false;

            try
            {

                if (pszNewLayerName == null)
                    pszNewLayerName = poSrcLayer.GetLayerDefn().GetName();

                if (wkbFlatten(eGType) == (int)OSGeo.OGR.wkbGeometryType.wkbPolygon)
                    bForceToPolygon = true;
                else if (wkbFlatten(eGType) == (int)OSGeo.OGR.wkbGeometryType.wkbMultiPolygon)
                    bForceToMultiPolygon = true;
                else if (wkbFlatten(eGType) == (int)OSGeo.OGR.wkbGeometryType.wkbMultiLineString)
                    bForceToMultiLineString = true;

                /* -------------------------------------------------------------------- */
                /*      Setup coordinate transformation if we need it.                  */
                /* -------------------------------------------------------------------- */
                CoordinateTransformation poCT = null;

                if (bTransform)
                {
                    if (poSourceSRS == null)
                        poSourceSRS = poSrcLayer.GetSpatialRef();

                    if (poSourceSRS == null)
                    {
                        Console.WriteLine("Can't transform coordinates, source layer has no\n" +
                                          "coordinate system.  Use -s_srs to set one.");
                        return false; //java translation correct System.exit(1);
                    }

                    /*CPLAssert( null != poSourceSRS );
                    CPLAssert( null != poOutputSRS );*/

                    /* New in GDAL 1.10. Before was "new CoordinateTransformation(srs,dst)". */
                    poCT = new OSGeo.OSR.CoordinateTransformation(poSourceSRS, poOutputSRS);

                    //if (poCT == null)
                    //{
                    //     string pszWKT = null;

                    //    Console.WriteLine("Failed to create coordinate transformation between the\n" +
                    //                       "following coordinate systems.  This may be because they\n" +
                    //                       "are not transformable, or because projection services\n" +
                    //                       "(PROJ.4 DLL/.so) could not be loaded.");

                    //    pszWKT = poSourceSRS.ExportToPrettyWkt(0);
                    //    Console.WriteLine("Source:\n" + pszWKT);

                    //    pszWKT = poOutputSRS.ExportToPrettyWkt(0);
                    //    Console.WriteLine("Target:\n" + pszWKT);
                    //    System.exit(1);
                    //}
                }

                /* -------------------------------------------------------------------- */
                /*      Get other info.                                                 */
                /* -------------------------------------------------------------------- */
                poSrcFDefn = poSrcLayer.GetLayerDefn();

                if (poOutputSRS == null)
                    poOutputSRS = poSrcLayer.GetSpatialRef();

                /* -------------------------------------------------------------------- */
                /*      Find the layer.                                                 */
                /* -------------------------------------------------------------------- */

                /* GetLayerByName() can instantiate layers that would have been */
                /* 'hidden' otherwise, for example, non-spatial tables in a */
                /* PostGIS-enabled database, so this apparently useless command is */
                /* not useless. (#4012) */
                Gdal.PushErrorHandler("CPLQuietErrorHandler");
                poDstLayer = poDstDS.GetLayerByName(pszNewLayerName);
                Gdal.PopErrorHandler();
                Gdal.ErrorReset();

                int iLayer = -1;
                if (poDstLayer != null)
                {
                    int nLayerCount = poDstDS.GetLayerCount();
                    for (iLayer = 0; iLayer < nLayerCount; iLayer++)
                    {
                        OSGeo.OGR.Layer poLayer = poDstDS.GetLayerByIndex(iLayer);

                        if (poLayer != null
                            && poLayer.GetName() == (poDstLayer.GetName())) //todo java translation .equals
                        {
                            break;
                        }
                    }

                    if (iLayer == nLayerCount)
                        /* shouldn't happen with an ideal driver */
                        poDstLayer = null;
                }

                /* -------------------------------------------------------------------- */
                /*      If the user requested overwrite, and we have the layer in       */
                /*      question we need to delete it now so it will get recreated      */
                /*      (overwritten).                                                  */
                /* -------------------------------------------------------------------- */
                if (poDstLayer != null && bOverwrite)
                {
                    if (poDstDS.DeleteLayer(iLayer) != 0)
                    {
                        Console.WriteLine(
                            "DeleteLayer() failed when overwrite requested.");
                        return false;
                    }

                    poDstLayer = null;
                }

                /* -------------------------------------------------------------------- */
                /*      If the layer does not exist, then create it.                    */
                /* -------------------------------------------------------------------- */
                if (poDstLayer == null)
                {
                    if (eGType == -2)
                    {
                        eGType = (int)poSrcFDefn.GetGeomType();

                        if (bExplodeCollections)
                        {
                            int n25DBit = eGType & (int)Ogr.wkb25DBit;
                            if (wkbFlatten(eGType) == (int)OSGeo.OGR.wkbGeometryType.wkbMultiPoint)
                            {
                                eGType = (int)OSGeo.OGR.wkbGeometryType.wkbPoint | n25DBit;
                            }
                            else if (wkbFlatten(eGType) == (int)OSGeo.OGR.wkbGeometryType.wkbMultiLineString)
                            {
                                eGType = (int)OSGeo.OGR.wkbGeometryType.wkbLineString | n25DBit;
                            }
                            else if (wkbFlatten(eGType) == (int)OSGeo.OGR.wkbGeometryType.wkbMultiPolygon)
                            {
                                eGType = (int)OSGeo.OGR.wkbGeometryType.wkbPolygon | n25DBit;
                            }
                            else if (wkbFlatten(eGType) == (int)OSGeo.OGR.wkbGeometryType.wkbGeometryCollection)
                            {
                                eGType = (int)OSGeo.OGR.wkbGeometryType.wkbUnknown | n25DBit;
                            }
                        }

                        if (pszZField != null)
                            eGType |= Ogr.wkb25DBit;
                    }

                    if (nCoordDim == 2)
                    {
                        eGType = eGType & ~Ogr.wkb25DBit;
                    }
                    else if (nCoordDim == 3)
                    {
                        eGType = eGType | Ogr.wkb25DBit;
                    }

                    if (poDstDS.TestCapability(Ogr.ODsCCreateLayer) == false)
                    {
                        Console.WriteLine(
                            "Layer " + pszNewLayerName + "not found, and CreateLayer not supported by driver.");
                        return false;
                    }

                    Gdal.ErrorReset();

                    poDstLayer = poDstDS.CreateLayer(pszNewLayerName, poOutputSRS, (OSGeo.OGR.wkbGeometryType)eGType,
                        papszLCO.ToArray());

                    if (poDstLayer == null)
                        return false;

                    bAppend = false;
                }

                /* -------------------------------------------------------------------- */
                /*      Otherwise we will append to it, if append was requested.        */
                /* -------------------------------------------------------------------- */
                else if (!bAppend)
                {
                    Console.WriteLine("FAILED: Layer " + pszNewLayerName +
                                      "already exists, and -append not specified.\n" +
                                      "        Consider using -append, or -overwrite.");
                    return false;
                }
                else
                {
                    if (papszLCO.Count > 0)
                    {
                        Console.WriteLine("WARNING: Layer creation options ignored since an existing layer is\n" +
                                          "         being appended to.");
                    }
                }

                /* -------------------------------------------------------------------- */
                /*      Add fields.  Default to copy all field.                         */
                /*      If only a subset of all fields requested, then output only      */
                /*      the selected fields, and in the order that they were            */
                /*      selected.                                                       */
                /* -------------------------------------------------------------------- */
                int iField;

                /* Initialize the index-to-index map to -1's */
                int nSrcFieldCount = poSrcFDefn.GetFieldCount();
                int[] panMap = new int[nSrcFieldCount];
                for (iField = 0; iField < nSrcFieldCount; iField++)
                    panMap[iField] = -1;

                FeatureDefn poDstFDefn = poDstLayer.GetLayerDefn();

                if (papszSelFields != null && !bAppend)
                {
                    int nDstFieldCount = 0;
                    if (poDstFDefn != null)
                        nDstFieldCount = poDstFDefn.GetFieldCount();

                    for (iField = 0; iField < papszSelFields.Count; iField++)
                    {
                        int iSrcField = poSrcFDefn.GetFieldIndex((String)papszSelFields[iField]);
                        if (iSrcField >= 0)
                        {
                            FieldDefn poSrcFieldDefn = poSrcFDefn.GetFieldDefn(iSrcField);
                            FieldDefn oFieldDefn = new FieldDefn(poSrcFieldDefn.GetNameRef(),
                                poSrcFieldDefn.GetFieldType());
                            oFieldDefn.SetWidth(poSrcFieldDefn.GetWidth());
                            oFieldDefn.SetPrecision(poSrcFieldDefn.GetPrecision());

                            if (papszFieldTypesToString != null &&
                                (CSLFindString(papszFieldTypesToString, "All") != -1 ||
                                 CSLFindString(papszFieldTypesToString,
                                     Ogr.GetFieldTypeName(poSrcFDefn.GetFieldDefn(iSrcField).GetFieldType())) != -1))
                                oFieldDefn.SetType(OSGeo.OGR.FieldType.OFTString);

                            /* The field may have been already created at layer creation */
                            int iDstField = -1;
                            if (poDstFDefn != null)
                                iDstField = poDstFDefn.GetFieldIndex(oFieldDefn.GetNameRef());
                            if (iDstField >= 0)
                            {
                                panMap[iSrcField] = iDstField;
                            }
                            else if (poDstLayer.CreateField(oFieldDefn, 1) == 0) //one more parameter approx_ok
                            {
                                /* now that we've created a field, GetLayerDefn() won't return NULL */
                                if (poDstFDefn == null)
                                    poDstFDefn = poDstLayer.GetLayerDefn();

                                /* Sanity check : if it fails, the driver is buggy */
                                if (poDstFDefn != null &&
                                    poDstFDefn.GetFieldCount() != nDstFieldCount + 1)
                                {
                                    Console.WriteLine(
                                        "The output driver has claimed to have added the " + oFieldDefn.GetNameRef() +
                                        " field, but it did not!");
                                }
                                else
                                {
                                    panMap[iSrcField] = nDstFieldCount;
                                    nDstFieldCount++;
                                }
                            }

                        }
                        else
                        {
                            Console.WriteLine("Field '" + (String)papszSelFields[iField] +
                                              "' not found in source layer.");
                            if (!bSkipFailures)
                                return false;
                        }
                    }

                    /* -------------------------------------------------------------------- */
                    /* Use SetIgnoredFields() on source layer if available                  */
                    /* -------------------------------------------------------------------- */

                    /* Here we differ from the ogr2ogr.cpp implementation since the OGRFeatureQuery */
                    /* isn't mapped to swig. So in that case just don't use SetIgnoredFields() */
                    /* to avoid issue raised in #4015 */
                    if (pszWHERE == null
                    ) //todo does not find poSrcLayer.TestCapability(Ogr.OLCIgnoreFields)  in the bindings
                    {
                        int iSrcField;
                        var papszIgnoredFields = new List<string>();
                        for (iSrcField = 0; iSrcField < nSrcFieldCount; iSrcField++)
                        {
                             string pszFieldName =
                                poSrcFDefn.GetFieldDefn(iSrcField).GetNameRef();
                            bool bFieldRequested = false;
                            for (iField = 0; iField < papszSelFields.Count; iField++)
                            {
                                if (pszFieldName.EqualsIgnoreCase((String)papszSelFields[iField]))
                                {
                                    bFieldRequested = true;
                                    break;
                                }
                            }

                            if (pszZField != null && pszFieldName.EqualsIgnoreCase(pszZField))
                                bFieldRequested = true;

                            /* If source field not requested, add it to ignored files list */
                            if (!bFieldRequested)
                                papszIgnoredFields.Add(pszFieldName);
                        }

                        poSrcLayer.SetIgnoredFields(papszIgnoredFields.ToArray());
                    }
                }
                else if (!bAppend)
                {
                    int nDstFieldCount = 0;
                    if (poDstFDefn != null)
                        nDstFieldCount = poDstFDefn.GetFieldCount();
                    for (iField = 0; iField < nSrcFieldCount; iField++)
                    {
                        FieldDefn poSrcFieldDefn = poSrcFDefn.GetFieldDefn(iField);
                        FieldDefn oFieldDefn = new FieldDefn(poSrcFieldDefn.GetNameRef(),
                            poSrcFieldDefn.GetFieldType());
                        oFieldDefn.SetWidth(poSrcFieldDefn.GetWidth());
                        oFieldDefn.SetPrecision(poSrcFieldDefn.GetPrecision());

                        if (papszFieldTypesToString != null &&
                            (CSLFindString(papszFieldTypesToString, "All") != -1 ||
                             CSLFindString(papszFieldTypesToString,
                                 Ogr.GetFieldTypeName(poSrcFDefn.GetFieldDefn(iField).GetFieldType())) != -1))
                            oFieldDefn.SetType(OSGeo.OGR.FieldType.OFTString);

                        /* The field may have been already created at layer creation */
                        int iDstField = -1;
                        if (poDstFDefn != null)
                            iDstField = poDstFDefn.GetFieldIndex(oFieldDefn.GetNameRef());
                        if (iDstField >= 0)
                        {
                            panMap[iField] = iDstField;
                        }
                        else if (poDstLayer.CreateField(oFieldDefn, 1) == 0)
                        {
                            /* now that we've created a field, GetLayerDefn() won't return NULL */
                            if (poDstFDefn == null)
                                poDstFDefn = poDstLayer.GetLayerDefn();

                            /* Sanity check : if it fails, the driver is buggy */
                            if (poDstFDefn != null &&
                                poDstFDefn.GetFieldCount() != nDstFieldCount + 1)
                            {
                                Console.WriteLine(
                                    "The output driver has claimed to have added the " + oFieldDefn.GetNameRef() +
                                    " field, but it did not!");
                            }
                            else
                            {
                                panMap[iField] = nDstFieldCount;
                                nDstFieldCount++;
                            }
                        }
                    }
                }
                else
                {
                    /* For an existing layer, build the map by fetching the index in the destination */
                    /* layer for each source field */
                    if (poDstFDefn == null)
                    {
                        Console.WriteLine("poDstFDefn == NULL.\n");
                        return false;
                    }

                    for (iField = 0; iField < nSrcFieldCount; iField++)
                    {
                        FieldDefn poSrcFieldDefn = poSrcFDefn.GetFieldDefn(iField);
                        int iDstField = poDstFDefn.GetFieldIndex(poSrcFieldDefn.GetNameRef());
                        if (iDstField >= 0)
                            panMap[iField] = iDstField;
                    }
                }

                /* -------------------------------------------------------------------- */
                /*      Transfer features.                                              */
                /* -------------------------------------------------------------------- */
                Feature poFeature;
                int nFeaturesInTransaction = 0;
                long nCount = 0;

                int iSrcZField = -1;
                if (pszZField != null)
                {
                    iSrcZField = poSrcFDefn.GetFieldIndex(pszZField);
                }

                poSrcLayer.ResetReading();

                if (nGroupTransactions > 0)
                    poDstLayer.StartTransaction();

                while (true)
                {
                    Feature poDstFeature = null;

                    if (nFIDToFetch != OGRNullFID)
                    {
                        // Only fetch feature on first pass.
                        if (nFeaturesInTransaction == 0)
                            poFeature = poSrcLayer.GetFeature(nFIDToFetch);
                        else
                            poFeature = null;
                    }
                    else
                        poFeature = poSrcLayer.GetNextFeature();

                    if (poFeature == null)
                        break;

                    int nParts = 0;
                    int nIters = 1;
                    if (bExplodeCollections)
                    {
                        Geometry poSrcGeometry = poFeature.GetGeometryRef();
                        if (poSrcGeometry != null)
                        {
                            switch (wkbFlatten((int)poSrcGeometry.GetGeometryType()))
                            {
                                case (int)OSGeo.OGR.wkbGeometryType.wkbMultiPoint:
                                case (int)OSGeo.OGR.wkbGeometryType.wkbMultiLineString:
                                case (int)OSGeo.OGR.wkbGeometryType.wkbMultiPolygon:
                                case (int)OSGeo.OGR.wkbGeometryType.wkbGeometryCollection:
                                    nParts = poSrcGeometry.GetGeometryCount();
                                    nIters = nParts;
                                    if (nIters == 0)
                                        nIters = 1;
                                    break;
                                default:
                                    break;
                            }
                        }
                    }

                    for (int iPart = 0; iPart < nIters; iPart++)
                    {

                        if (++nFeaturesInTransaction == nGroupTransactions)
                        {
                            poDstLayer.CommitTransaction();
                            poDstLayer.StartTransaction();
                            nFeaturesInTransaction = 0;
                        }

                        Gdal.ErrorReset();
                        poDstFeature = new Feature(poDstLayer.GetLayerDefn());

                        if (poDstFeature.SetFromWithMap(poFeature, 1, panMap.Length, panMap) != 0) //TODO what's that ?
                        {
                            if (nGroupTransactions > 0)
                                poDstLayer.CommitTransaction();

                            Console.WriteLine(
                                "Unable to translate feature " + poFeature.GetFID() + " from layer " +
                                poSrcFDefn.GetName());

                            poFeature.Dispose();
                            poFeature = null;
                            poDstFeature.Dispose();
                            poDstFeature = null;
                            return false;
                        }

                        if (bPreserveFID)
                            poDstFeature.SetFID(poFeature.GetFID());

                        Geometry poDstGeometry = poDstFeature.GetGeometryRef();
                        if (poDstGeometry != null)
                        {
                            if (nParts > 0)
                            {
                                /* For -explodecollections, extract the iPart(th) of the geometry */
                                Geometry poPart = poDstGeometry.GetGeometryRef(iPart).Clone();
                                poDstFeature.SetGeometryDirectly(poPart);
                                poDstGeometry = poPart;
                            }

                            if (iSrcZField != -1)
                            {
                                SetZ(poDstGeometry, poFeature.GetFieldAsDouble(iSrcZField));
                                /* This will correct the coordinate dimension to 3 */
                                Geometry poDupGeometry = poDstGeometry.Clone();
                                poDstFeature.SetGeometryDirectly(poDupGeometry);
                                poDstGeometry = poDupGeometry;
                            }

                            if (nCoordDim == 2 || nCoordDim == 3)
                            {
                                poDstGeometry.SetCoordinateDimension(nCoordDim);
                            }

                            if (eGeomOp == GeomOperation.SEGMENTIZE)
                            {
                                /*if (poDstFeature.GetGeometryRef() != null && dfGeomOpParam > 0)
                                    poDstFeature.GetGeometryRef().segmentize(dfGeomOpParam);*/
                            }
                            else if (eGeomOp == GeomOperation.SIMPLIFY_PRESERVE_TOPOLOGY && dfGeomOpParam > 0)
                            {
                                Geometry poNewGeom = poDstGeometry.SimplifyPreserveTopology(dfGeomOpParam);
                                if (poNewGeom != null)
                                {
                                    poDstFeature.SetGeometryDirectly(poNewGeom);
                                    poDstGeometry = poNewGeom;
                                }
                            }

                            if (poClipSrc != null)
                            {
                                Geometry poClipped = poDstGeometry.Intersection(poClipSrc);
                                if (poClipped == null || poClipped.IsEmpty())
                                {
                                    /* Report progress */
                                    nCount++;
                                    //if (pfnProgress != null)
                                    //    pfnProgress.run(nCount * 1.0 / nCountLayerFeatures, "");
                                    poDstFeature.Dispose();
                                    continue;
                                }

                                poDstFeature.SetGeometryDirectly(poClipped);
                                poDstGeometry = poClipped;
                            }

                            if (poCT != null)
                            {
                                eErr = poDstGeometry.Transform(poCT);
                                if (eErr != 0)
                                {
                                    if (nGroupTransactions > 0)
                                        poDstLayer.CommitTransaction();

                                    Console.WriteLine("Failed to reproject feature" + poFeature.GetFID() +
                                                      " (geometry probably out of source or destination SRS).");
                                    if (!bSkipFailures)
                                    {
                                        poFeature.Dispose();
                                        poFeature = null;
                                        poDstFeature.Dispose();
                                        poDstFeature = null;
                                        return false;
                                    }
                                }
                            }
                            else if (poOutputSRS != null)
                            {
                                poDstGeometry.AssignSpatialReference(poOutputSRS);
                            }

                            if (poClipDst != null)
                            {
                                Geometry poClipped = poDstGeometry.Intersection(poClipDst);
                                if (poClipped == null || poClipped.IsEmpty())
                                {
                                    /* Report progress */
                                    nCount++;
                                    //if (pfnProgress != null)
                                    //    pfnProgress.run(nCount * 1.0 / nCountLayerFeatures, "");
                                    poDstFeature.Dispose();
                                    continue;
                                }

                                poDstFeature.SetGeometryDirectly(poClipped);
                                poDstGeometry = poClipped;
                            }

                            if (bForceToPolygon)
                            {
                                poDstFeature.SetGeometryDirectly(Ogr.ForceToPolygon(poDstGeometry));
                            }

                            else if (bForceToMultiPolygon)
                            {
                                poDstFeature.SetGeometryDirectly(Ogr.ForceToMultiPolygon(poDstGeometry));
                            }

                            else if (bForceToMultiLineString)
                            {
                                poDstFeature.SetGeometryDirectly(Ogr.ForceToMultiLineString(poDstGeometry));
                            }
                        }

                        Gdal.ErrorReset();
                        if (poDstLayer.CreateFeature(poDstFeature) != 0
                            && !bSkipFailures)
                        {
                            if (nGroupTransactions > 0)
                                poDstLayer.RollbackTransaction();

                            poDstFeature.Dispose();
                            poDstFeature = null;
                            return false;
                        }

                        poDstFeature.Dispose();
                        poDstFeature = null;
                    }

                    poFeature.Dispose();
                    poFeature = null;

                    /* Report progress */
                    nCount++;
                    //if (pfnProgress != null)
                    //    pfnProgress.run(nCount * 1.0 / nCountLayerFeatures, "");

                }

                if (nGroupTransactions > 0)
                    poDstLayer.CommitTransaction();

                return true;

            }
            catch (Exception ex)
            {
                //Log.Error($"Ogr2Ogr, TranslateLayer: {ex.Message}");
                return false;
            }
        }
    }
}
