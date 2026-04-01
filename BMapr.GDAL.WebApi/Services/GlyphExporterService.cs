using SkiaSharp;
using System.Collections.Generic;
using System.Text;
using BMapr.GDAL.WebApi.Models.Map;
using Newtonsoft.Json;

namespace BMapr.GDAL.WebApi.Services;

public class GlyphExporterService
{

    private readonly SKTypeface _typeface;
    private readonly SKFont _font;
    private readonly float _fontSize;
    private readonly int _canvasSize;

    public GlyphExporterService(string fontPath, float fontSize = 2048f, int canvasSize = 256)
    {
        if (!File.Exists(fontPath))
            throw new FileNotFoundException("Font not found.", fontPath);

        _typeface = SKTypeface.FromFile(fontPath);
        _font = new SKFont(_typeface, fontSize);
        _fontSize = fontSize;
        _canvasSize = canvasSize;
    }

    public bool SupportsChar(string text)
    {
        ushort glyph = _font.GetGlyphs(text)[0];
        return glyph != 0;
    }

    // ✅ PNG Export
    public void ExportPng(string text, string outputPath)
    {
        if (!SupportsChar(text)) return;

        ushort glyphId = _font.GetGlyphs(text)[0];
        SKPath path = _font.GetGlyphPath(glyphId);

        if (path == null || path.Bounds.Width <= 0) return;

        SKRect glyphBounds = path.Bounds;

        float scale = Math.Min(_canvasSize / glyphBounds.Width, _canvasSize / glyphBounds.Height) * 0.9f;

        using var surface = SKSurface.Create(new SKImageInfo(_canvasSize, _canvasSize));
        var canvas = surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        using var paint = new SKPaint
        {
            Typeface = _typeface,
            TextSize = _fontSize,
            Color = SKColors.Black,
            IsAntialias = true
        };

        float scaledWidth = glyphBounds.Width * scale;
        float scaledHeight = glyphBounds.Height * scale;
        float x = (_canvasSize - scaledWidth) / 2 - glyphBounds.Left * scale;
        float y = (_canvasSize - scaledHeight) / 2 - glyphBounds.Top * scale;

        canvas.Save();
        canvas.Translate(x, y);
        canvas.Scale(scale);
        canvas.DrawText(text, 0, 0, paint);
        canvas.Restore();
        canvas.Flush();

        using var img = surface.Snapshot();
        using var data = img.Encode(SKEncodedImageFormat.Png, 100);

        File.WriteAllBytes(outputPath, data.ToArray());
    }


    // ✅ Convert SKPath → SVG
    private string PathToSvg(SKPath path)
    {
        var sb = new StringBuilder();

        var iter = path.CreateRawIterator();
        SKPoint[] pts = new SKPoint[4];

        SKPathVerb verb;
        while ((verb = iter.Next(pts)) != SKPathVerb.Done)
        {
            switch (verb)
            {
                case SKPathVerb.Move:
                    sb.AppendFormat("M {0} {1} ", pts[0].X, pts[0].Y);
                    break;

                case SKPathVerb.Line:
                    sb.AppendFormat("L {0} {1} ", pts[1].X, pts[1].Y);
                    break;

                case SKPathVerb.Quad:
                    sb.AppendFormat("Q {0} {1} {2} {3} ",
                        pts[1].X, pts[1].Y,
                        pts[2].X, pts[2].Y);
                    break;

                case SKPathVerb.Cubic:
                    sb.AppendFormat("C {0} {1} {2} {3} {4} {5} ",
                        pts[1].X, pts[1].Y,
                        pts[2].X, pts[2].Y,
                        pts[3].X, pts[3].Y);
                    break;

                case SKPathVerb.Close:
                    sb.Append("Z ");
                    break;
            }
        }

        return sb.ToString().Trim();
    }


    // ✅ SVG Export
    public void ExportSvg(string text, string outputPath)
    {
        if (!SupportsChar(text)) return;

        ushort glyphId = _font.GetGlyphs(text)[0];
        SKPath path = _font.GetGlyphPath(glyphId);

        if (path == null || path.Bounds.Width <= 0) return;

        string d = PathToSvg(path);
        SKRect b = path.Bounds;

        string svg =
$@"<svg xmlns='http://www.w3.org/2000/svg'
      width='{b.Width}'
      height='{b.Height}'
      viewBox='{b.Left} {b.Top} {b.Width} {b.Height}'>
    <path d='{d}' fill='black'/>
</svg>";

        File.WriteAllText(outputPath, svg, Encoding.UTF8);
    }

    // ✅ PNG Export as Base64
    public string ExportPngAsBase64(string pngPath)
    {
        if (!File.Exists(pngPath))
            throw new FileNotFoundException("PNG file not found.", pngPath);

        byte[] imageBytes = File.ReadAllBytes(pngPath);
        string base64String = Convert.ToBase64String(imageBytes);

        return $"data:image/png;base64,{base64String}";
    }

    public static void ExportAll(string fontFilePath, bool png, bool svg, string outputPath, float fontSize = 2048f, int canvasSize = 256)
    {

        var exporter = new GlyphExporterService(fontFilePath, fontSize, canvasSize);
        var exportedChars = new List<FontCharacterItem>();

        for (int cp = 0; cp <= 0x10FFFF; cp++)
        {
            if (cp is >= 0xD800 and <= 0xDFFF) continue;

            string ch;
            try { ch = char.ConvertFromUtf32(cp); } catch { continue; }

            if (!exporter.SupportsChar(ch)) continue;

            string hex = cp.ToString("X");

            var pngContent = string.Empty;

            if (png)
            {
                var pngPath = Path.Combine(outputPath, $"png/{hex}.png");
                exporter.ExportPng(ch, pngPath);

                if (!File.Exists(pngPath))
                {
                   continue;
                }

                pngContent = exporter.ExportPngAsBase64(pngPath);
            }

            if (svg)
            {
                exporter.ExportSvg(ch, Path.Combine(outputPath, $"svg/{hex}.svg"));
            }

            exportedChars.Add(new FontCharacterItem(){Hex = hex, Code = Convert.ToInt32(hex, 16), ImageContent = pngContent});
        }

        var content = JsonConvert.SerializeObject(exportedChars, Formatting.Indented);

        File.WriteAllText(Path.Combine(outputPath, "font-list.json"), content, Encoding.UTF8);
    }
}