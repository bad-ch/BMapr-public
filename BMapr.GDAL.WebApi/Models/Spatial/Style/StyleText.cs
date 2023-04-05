namespace BMapr.GDAL.WebApi.Models.Spatial.Style
{
    public class StyleText
    {
        public string TextAlign { get; set; }
        public string TextBaseline { get; set; }
        public string Font  { get; set; }
        public string Text { get; set; }
        public StyleFill Fill { get; set; }
        public StyleStroke Stroke { get; set; }
        public double OffsetX { get; set; }
        public double OffsetY { get; set; }
        public double Rotation { get; set; }
    }
}
