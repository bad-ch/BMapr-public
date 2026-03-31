using System.Text.RegularExpressions;

namespace BMapr.GDAL.WebApi.Services;

public class FontService
{
    // First column is the name of the font, second column is the path to the font file (split are spaces or tabs)
    public static Dictionary<string, string> Load(string filePath)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var raw in File.ReadLines(filePath))
        {
            var line = raw.Trim();
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;

            // Split by 2+ spaces OR tabs
            var parts = Regex.Split(line, @"\s{2,}|\t+")
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToArray();

            if (parts.Length >= 2)
            {
                var alias = parts[0].Trim();
                var path = parts[1].Trim();
                if (File.Exists(path))
                {
                    map[alias] = path;
                }
                else
                {
                    Console.Error.WriteLine($"[Warn] File not found: '{path}' (alias: {alias})");
                }
            }
        }

        return map;
    }

    public static void GenerateFontMetadata(Dictionary<string, string> fontList, string outputPath)
    {
        if (!Path.Exists(outputPath))
        {
            throw new Exception($"Output path does not exist: {outputPath}");
        }

        fontList.ToList().ForEach(kvp =>
        {
            var name = kvp.Key;
            var pathFontFile = kvp.Value;
            var pathFont = Path.Combine(outputPath, name);

            if (Path.Exists(pathFont))
            {
                Directory.Delete(pathFont, true);
            }

            Directory.CreateDirectory(pathFont);

            var pngPath = Path.Combine(pathFont, "png");
            var svgPath = Path.Combine(pathFont, "svg");
            Directory.CreateDirectory(pngPath);
            Directory.CreateDirectory(svgPath);

            GlyphExporterService.ExportAll(pathFontFile, png: true, svg: false, outputPath: pathFont, fontSize: 256, canvasSize: 64);
        });
    }
}