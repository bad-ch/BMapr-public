namespace BMapr.GDAL.WebApi.Models.Map;

public class FontItem
{
    public string Name { get; set; }

    public List<FontCharacterItem> Characters { get; set; } = [];
}