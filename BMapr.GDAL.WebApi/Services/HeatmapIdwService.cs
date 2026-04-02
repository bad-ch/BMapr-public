using OSGeo.GDAL;
using OSGeo.OGR;
using OSGeo.OSR;

namespace BMapr.GDAL.WebApi.Test;

public static class HeatmapIdwService
{
    private sealed record Pt(double X, double Y, double Z);

    public enum OutputType
    {
        Data,   // Single-band float32 data
        RGBA    // 4-band RGBA with color ramp
    }

    /// <summary>
    /// Color value as RGBA
    /// </summary>
    public readonly struct Color
    {
        public byte R { get; init; }
        public byte G { get; init; }
        public byte B { get; init; }
        public byte A { get; init; }

        public Color(byte r, byte g, byte b, byte a = 255)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }
    }

    /// <summary>
    /// Default heatmap colormap (blue -> cyan -> green -> yellow -> red)
    /// </summary>
    private static Color[] GetDefaultColormap()
    {
        return new[]
        {
            new Color(0, 0, 139),       // Dark blue (0%)
            new Color(0, 0, 255),       // Blue (12.5%)
            new Color(0, 255, 255),     // Cyan (25%)
            new Color(0, 255, 0),       // Green (37.5%)
            new Color(255, 255, 0),     // Yellow (50%)
            new Color(255, 165, 0),     // Orange (62.5%)
            new Color(255, 0, 0),       // Red (75%)
            new Color(139, 0, 0)        // Dark red (100%)
        };
    }

    /// <summary>
    /// Interpolate color from colormap based on normalized value [0-1]
    /// </summary>
    private static Color InterpolateColor(double normalizedValue, Color[] colormap)
    {
        if (normalizedValue <= 0) return colormap[0];
        if (normalizedValue >= 1) return colormap[colormap.Length - 1];

        double scaledIndex = normalizedValue * (colormap.Length - 1);
        int index0 = (int)Math.Floor(scaledIndex);
        int index1 = Math.Min(index0 + 1, colormap.Length - 1);
        double blend = scaledIndex - index0;

        var c0 = colormap[index0];
        var c1 = colormap[index1];

        return new Color(
            (byte)Math.Round(c0.R * (1 - blend) + c1.R * blend),
            (byte)Math.Round(c0.G * (1 - blend) + c1.G * blend),
            (byte)Math.Round(c0.B * (1 - blend) + c1.B * blend),
            (byte)Math.Round(c0.A * (1 - blend) + c1.A * blend)
        );
    }

    public static void CreateIdwGeoTiff(
        string geojsonPath,
        string zFieldName,
        string outTiffPath,
        double pixelSize,
        double padding = 0,
        double power = 2.0,
        double smoothing = 0.0,
        double searchRadius = 0.0,
        int maxPoints = 0,
        int minPoints = 1,
        double nodata = -9999.0,
        string? assumeInputSrs = null,
        int maxDegreeOfParallelism = -1,
        OutputType outputType = OutputType.Data,
        Color[]? customColormap = null
    )
    {
        Gdal.AllRegister();
        Ogr.RegisterAll();

        using var ds = Ogr.Open(geojsonPath, 0);
        if (ds == null) throw new Exception("Cannot open GeoJSON.");

        using var layer = ds.GetLayerByIndex(0);
        if (layer == null) throw new Exception("Cannot open layer 0.");

        var defn = layer.GetLayerDefn();
        int zIdx = defn.GetFieldIndex(zFieldName);
        if (zIdx < 0) throw new Exception($"Field '{zFieldName}' not found.");

        SpatialReference? srs = layer.GetSpatialRef();
        if (srs == null && !string.IsNullOrWhiteSpace(assumeInputSrs))
        {
            srs = new SpatialReference(null);
            srs.SetFromUserInput(assumeInputSrs);
        }

        var pts = new List<Pt>(capacity: (int)Math.Max(1024, layer.GetFeatureCount(1)));
        layer.ResetReading();

        Feature feat;
        while ((feat = layer.GetNextFeature()) != null)
        {
            using (feat)
            {
                var geom = feat.GetGeometryRef();
                if (geom == null) continue;
                if (geom.GetGeometryType() != wkbGeometryType.wkbPoint &&
                    geom.GetGeometryType() != wkbGeometryType.wkbPoint25D)
                    continue;

                double x = geom.GetX(0);
                double y = geom.GetY(0);

                if (!feat.IsFieldSet(zIdx)) continue;
                double z = feat.GetFieldAsDouble(zIdx);

                pts.Add(new Pt(x, y, z));
            }
        }

        if (pts.Count == 0)
            throw new Exception("No valid point features with Z values found.");

        var env = new Envelope();
        layer.GetExtent(env, 1);

        double xmin = env.MinX - padding;
        double xmax = env.MaxX + padding;
        double ymin = env.MinY - padding;
        double ymax = env.MaxY + padding;

        int cols = (int)Math.Ceiling((xmax - xmin) / pixelSize);
        int rows = (int)Math.Ceiling((ymax - ymin) / pixelSize);

        double[] gt = { xmin, pixelSize, 0, ymax, 0, -pixelSize };

        float[] raster = new float[cols * rows];
        double s2 = smoothing * smoothing;
        double sr2 = searchRadius > 0 ? searchRadius * searchRadius : 0;

        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = maxDegreeOfParallelism
        };

        Parallel.For(0, rows, parallelOptions, row =>
        {
            double y = ymax - (row + 0.5) * pixelSize;
            int rowOffset = row * cols;

            Span<double> bestD2 = maxPoints > 0 ? new double[maxPoints] : default;
            Span<double> bestZ = maxPoints > 0 ? new double[maxPoints] : default;

            for (int col = 0; col < cols; col++)
            {
                double x = xmin + (col + 0.5) * pixelSize;
                double wSum = 0.0;
                double zwSum = 0.0;
                int used = 0;
                int bestCount = 0;

                for (int i = 0; i < pts.Count; i++)
                {
                    var p = pts[i];
                    double dx = x - p.X;
                    double dy = y - p.Y;
                    double d2xy = dx * dx + dy * dy;

                    if (searchRadius > 0 && d2xy > sr2)
                        continue;

                    if (d2xy == 0)
                    {
                        raster[rowOffset + col] = (float)p.Z;
                        goto NextCell;
                    }

                    if (maxPoints > 0)
                    {
                        if (bestCount < maxPoints)
                        {
                            bestD2[bestCount] = d2xy;
                            bestZ[bestCount] = p.Z;
                            bestCount++;
                        }
                        else
                        {
                            int worst = 0;
                            double worstD = bestD2[0];
                            for (int k = 1; k < maxPoints; k++)
                            {
                                if (bestD2[k] > worstD)
                                {
                                    worstD = bestD2[k];
                                    worst = k;
                                }
                            }
                            if (d2xy < worstD)
                            {
                                bestD2[worst] = d2xy;
                                bestZ[worst] = p.Z;
                            }
                        }
                    }
                    else
                    {
                        double d = Math.Sqrt(d2xy + s2);
                        double w = 1.0 / Math.Pow(d, power);
                        wSum += w;
                        zwSum += w * p.Z;
                        used++;
                    }
                }

                if (maxPoints > 0)
                {
                    for (int k = 0; k < bestCount; k++)
                    {
                        double d = Math.Sqrt(bestD2[k] + s2);
                        double w = 1.0 / Math.Pow(d, power);
                        wSum += w;
                        zwSum += w * bestZ[k];
                    }
                    used = bestCount;
                }

                raster[rowOffset + col] = (used >= minPoints && wSum > 0)
                    ? (float)(zwSum / wSum)
                    : (float)nodata;

            NextCell:
                ;
            }
        });

        // Write output
        var drv = Gdal.GetDriverByName("GTiff");
        if (drv == null) throw new Exception("GTiff driver not available.");

        string[] options =
        {
            "TILED=YES",
            "COMPRESS=LZW",
            "BIGTIFF=IF_SAFER"
        };

        if (outputType == OutputType.Data)
        {
            // Write single-band float32 data
            using var outDs = drv.Create(outTiffPath, cols, rows, 1, DataType.GDT_Float32, options);
            outDs.SetGeoTransform(gt);

            if (srs != null)
            {
                srs.ExportToWkt(out string wkt, null);
                outDs.SetProjection(wkt);
            }

            var band = outDs.GetRasterBand(1);
            band.SetNoDataValue(nodata);
            band.WriteRaster(0, 0, cols, rows, raster, cols, rows, 0, 0);
            band.FlushCache();
            outDs.FlushCache();
        }
        else
        {
            // Write RGBA with color mapping
            var colormap = customColormap ?? GetDefaultColormap();

            // Calculate min/max for normalization
            double minVal = double.MaxValue;
            double maxVal = double.MinValue;

            for (int i = 0; i < raster.Length; i++)
            {
                float val = raster[i];
                if (val != (float)nodata)
                {
                    minVal = Math.Min(minVal, val);
                    maxVal = Math.Max(maxVal, val);
                }
            }

            if (minVal == double.MaxValue) minVal = 0;
            if (maxVal == double.MinValue) maxVal = 1;

            double range = maxVal - minVal;
            if (Math.Abs(range) < 1e-10) range = 1;

            // Create RGBA buffers
            byte[] rBand = new byte[cols * rows];
            byte[] gBand = new byte[cols * rows];
            byte[] bBand = new byte[cols * rows];
            byte[] aBand = new byte[cols * rows];

            for (int i = 0; i < raster.Length; i++)
            {
                float val = raster[i];

                if (val == (float)nodata)
                {
                    // Transparent for nodata
                    rBand[i] = 0;
                    gBand[i] = 0;
                    bBand[i] = 0;
                    aBand[i] = 0;
                }
                else
                {
                    // Normalize to [0-1] and interpolate color
                    double normalized = (val - minVal) / range;
                    var color = InterpolateColor(normalized, colormap);

                    rBand[i] = color.R;
                    gBand[i] = color.G;
                    bBand[i] = color.B;
                    aBand[i] = color.A;
                }
            }

            using var outDs = drv.Create(outTiffPath, cols, rows, 4, DataType.GDT_Byte, options);
            outDs.SetGeoTransform(gt);

            if (srs != null)
            {
                srs.ExportToWkt(out string wkt, null);
                outDs.SetProjection(wkt);
            }

            var rasterBand = outDs.GetRasterBand(1);
            rasterBand.WriteRaster(0, 0, cols, rows, rBand, cols, rows, 0, 0);
            rasterBand.FlushCache();

            var gRasterBand = outDs.GetRasterBand(2);
            gRasterBand.WriteRaster(0, 0, cols, rows, gBand, cols, rows, 0, 0);
            gRasterBand.FlushCache();

            var bRasterBand = outDs.GetRasterBand(3);
            bRasterBand.WriteRaster(0, 0, cols, rows, bBand, cols, rows, 0, 0);
            bRasterBand.FlushCache();

            var aRasterBand = outDs.GetRasterBand(4);
            aRasterBand.WriteRaster(0, 0, cols, rows, aBand, cols, rows, 0, 0);
            aRasterBand.FlushCache();

            outDs.FlushCache();
        }
    }
}
