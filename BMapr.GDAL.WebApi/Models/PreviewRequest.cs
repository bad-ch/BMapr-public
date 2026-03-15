namespace BMapr.GDAL.WebApi.Models;

public class PreviewRequest
{
    public string Style { get; set; } = string.Empty;
    
    public string? Symbol { get; set; }

    /// <summary>
    /// How the preview should be rendered:
    /// "point", "line", or "polygon".
    /// Default: "point".
    /// </summary>
    public string? Geometry { get; set; }

    public int Width { get; set; }

    public int Height { get; set; }

    /// <summary>
    /// Background color for the preview.
    /// "transparent" (default) or a hex color like "#ffffff".
    /// </summary>
    public string? Background { get; set; }
}