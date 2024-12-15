using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using BMapr.GDAL.WebApi.Services;
using Newtonsoft.Json;

namespace BMapr.GDAL.WebApi.Models.Spatial.Style
{
    public class Style
    {
        public StyleFill Fill { get; set; }
        public StyleStroke Stroke { get; set; }
        public StyleImage? Image { get; set; }
        public StyleText? Text { get; set; }

        public string? Hash { get; set; }

        //public string StyleHash => GetStyleHash();

        //public string GetStyleHash()
        //{
        //    return GetMD5Hash(JsonConvert.SerializeObject(this));
        //}

        //private string GetMD5Hash(string value)
        //{
        //    if ((value == null) || (value.Length == 0))
        //    {
        //        return string.Empty;
        //    }

        //    MD5 md5 = new MD5CryptoServiceProvider();
        //    byte[] textToHash = Encoding.Default.GetBytes(value);
        //    byte[] result = md5.ComputeHash(textToHash);

        //    return System.BitConverter.ToString(result);
        //}

        public string GetSldFromStyle(Style style, Mapserver.LayerGeometryType geometryType, int scale)
        {
            var pointStyle = "";
            var lineStyle = "";
            var areaStyle = "";
            var fill = "";
            var stroke = "";
            var text = "";

            double factor = 3; //scale < 5000 ? 2 : 1;

            try
            {
                if (style.Fill != null)
                {
                    fill = $@"
                        <Fill>
                            <CssParameter name=""fill"">{GetHex(style.Fill.Color)}</CssParameter>
                            <CssParameter name=""fill-opacity"">{(style.Fill.Color.Length == 4 ? style.Fill.Color[3] : 1)}</CssParameter>
                        </Fill>
                    ";
                }

                if (style.Stroke != null)
                {
                    stroke = $@"
                        <Stroke>
                            <CssParameter name=""stroke"">{GetHex(style.Stroke.Color)}</CssParameter>
                            <CssParameter name=""stroke-width"">{style.Stroke.Width * factor}</CssParameter>
                            <!--<CssParameter name=""stroke-dasharray"">15 0</CssParameter>-->
                        </Stroke>
                    ";
                }

                if (geometryType == Mapserver.LayerGeometryType.Polygon)
                {
                    areaStyle = $@"<PolygonSymbolizer>
                                    {fill}
                                    {stroke}
                            </PolygonSymbolizer>";
                }
                else if (geometryType == Mapserver.LayerGeometryType.Polyline)
                {
                    lineStyle = $@"<LineSymbolizer>
	                                {stroke}
                                </LineSymbolizer>";
                }
                else if (geometryType == Mapserver.LayerGeometryType.Point)
                {
                    if (style.Image != null && style.Image.Circle != null)
                    {
                        var circleFill = style.Image.Circle.Fill != null ? $@"

                         <Fill>
                           <CssParameter name=""fill"">{GetHex(style.Image.Circle.Fill.Color)}</CssParameter>
                         </Fill>

                    " : "";

                        var circleStroke = style.Image.Circle.Stroke != null ? $@"

                        <Stroke>
                            <CssParameter name=""stroke"">{GetHex(style.Image.Circle.Stroke.Color)}</CssParameter>
                            <CssParameter name=""stroke-width"">{style.Image.Circle.Stroke.Width * factor * 0.5}</CssParameter>
                        </Stroke>

                    " : "";

                        pointStyle = $@"
                        <PointSymbolizer>
                         <Graphic>
                           <Mark>
                             <WellKnownName>circle</WellKnownName>
                                {circleFill}
                                {circleStroke}
                           </Mark>
                           <Size>{style.Image.Circle.Radius * factor}</Size>
                         </Graphic>
                        </PointSymbolizer>";
                    }
                    else
                    {
                        var url = style.Image.Icon.Src;
                        var size = style.Image.Icon.ImageSize * style.Image.Icon.Scale;

                        pointStyle = $@"<PointSymbolizer>
                                           <Graphic>
                                             <ExternalGraphic>
                                               <OnlineResource
                                                 xlink:type=""simple""
                                                 xlink:href=""{url}"" />
                                               <Format>image/png</Format>
                                             </ExternalGraphic>
                                             <Size>{size * factor}</Size>
                                           </Graphic>
                                    </PointSymbolizer>";
                    }
                }
                else
                {
                    return string.Empty;
                }

                //todo remove, for debugging reasons only

                if (style.Text != null)
                {

                    var font = style.Text.Font.Split(' ');
                    var fontSize = System.Convert.ToInt32(font[1].Replace("px", "")) * factor * 0.5;

                    //center per default
                    var anchorX = 0.5;
                    var anchorY = 0.5;

                    if (style.Text.TextAlign == "right")
                    {
                        anchorX = 0;
                        anchorY = 0;
                    }
                    else if (style.Text.TextAlign == "left")
                    {
                        anchorX = 1;
                        anchorY = 1;
                    }

                    text = $@"

                    <TextSymbolizer>
                        <Label>
                           <ogc:PropertyName>0</ogc:PropertyName>
                        </Label>
                        <Font>
                          <CssParameter name=""font-family"">{font[2]}</CssParameter>
                          <CssParameter name=""font-size"">{fontSize}</CssParameter>
                          <!--<CssParameter name=""font-style"">italic</CssParameter>-->
                          <!--<CssParameter name=""font-weight"">bold</CssParameter>-->
                        </Font>
                        <LabelPlacement>
                          <PointPlacement>
                            <AnchorPoint>
                              <AnchorPointX>{anchorX}</AnchorPointX>
                              <AnchorPointY>{anchorY}</AnchorPointY>
                            </AnchorPoint>
                            <Displacement>
                              <DisplacementX>0</DisplacementX>
                              <DisplacementY>0</DisplacementY>
                            </Displacement>
				            <Rotation>0</Rotation>
                          </PointPlacement>
                        </LabelPlacement>
			            <Halo>
				            <Radius>{style.Text.Stroke.Width * factor}</Radius>
				            <Fill>
					            <CssParameter name=""fill"">{GetHex(style.Text.Stroke.Color)}</CssParameter>
				            </Fill>
			            </Halo>
                        <Fill>
				            <CssParameter name=""fill"">{GetHex(style.Text.Fill.Color)}</CssParameter>
                        </Fill>
                      </TextSymbolizer>   
                ";
                }
            }
            catch (Exception e)
            {
                //todo default style ??
            }

            var sldDescription = $@"<StyledLayerDescriptor version=""1.0.0"" >
                <NamedLayer>
                    <Name>test</Name>
                    <UserStyle>
                        <FeatureTypeStyle>
                            <Rule>
                                <Name>Default</Name>
                                {pointStyle}
                                {lineStyle}
                                {areaStyle}
		                        {text}                           
                            </Rule>
                        </FeatureTypeStyle>
                    </UserStyle>
                </NamedLayer>
            </StyledLayerDescriptor>";

            return sldDescription;
        }

        private static string GetHex(double[] rgba)
        {
            if (rgba.Length < 3)
            {
                return string.Empty;
            }

            Color myColor = Color.FromArgb((int)rgba[0], (int)rgba[1], (int)rgba[2]);

            return "#" + myColor.R.ToString("X2") + myColor.G.ToString("X2") + myColor.B.ToString("X2");
        }
    }
}
