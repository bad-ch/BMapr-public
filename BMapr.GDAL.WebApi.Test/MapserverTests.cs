using System.Reflection;
using System.Xml;
using BMapr.GDAL.WebApi.Models;
using BMapr.GDAL.WebApi.Models.Spatial;
using BMapr.GDAL.WebApi.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OSGeo.MapServer;

namespace BMapr.GDAL.WebApi.Test
{
    [TestClass]
    public class MapserverTests
    {
        [TestMethod]
        public void Reflection()
        {
            var filePath = Path.GetFullPath("../../../TestFiles/bb.map");
            var mapserverService = new MapserverService(new FileInfo(filePath));

            var result = mapserverService.GetMetadata(mapserverService.Map);

            var content = JsonConvert.SerializeObject(result, Newtonsoft.Json.Formatting.Indented,
                new JsonSerializerSettings()
                {
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                });
        }

        [TestMethod]
        public void CreateLegendFromSLD()
        {
            var mapserver = new Mapserver();

            mapserver.AddLayer("test", Mapserver.LayerConnectionType.Inline, Mapserver.LayerGeometryType.Polygon);

            mapserver.AddGeometryToLayer("test", "POLYGON((685953.338 192591.152, 690000 195000, 685953.338 198000,685953.338 192591.152))", "Test2\npost2");
            mapserver.AddGeometryToLayer("test", "POLYGON((685953.338 192591.152, 695000 196000, 685953.338 198000,685953.338 192591.152))", "test\npost");

            mapserver.MapserverSetExtent(685953.338, 192591.152, 695160.857, 200250.855, 2400, 1200);

            string sldDescription = @"<StyledLayerDescriptor version=""1.0.0"" >
                <NamedLayer>
                    <Name>test</Name>
                    <UserStyle>
                        <FeatureTypeStyle>
                            <Rule>
                                <Name>Default</Name>
                                <PolygonSymbolizer>
                                    <Fill>
                                        <CssParameter name=""fill"">#ccccff</CssParameter>
                                        <CssParameter name=""fill-opacity"">0.5</CssParameter>
                                    </Fill>
                                    <Stroke>
                                        <CssParameter name=""stroke"">#ff0000</CssParameter>
                                        <CssParameter name=""stroke-width"">8.00</CssParameter>
                                        <CssParameter name=""stroke-dasharray"">15 8</CssParameter>
                                    </Stroke>
                                </PolygonSymbolizer>
		                          <TextSymbolizer>
                                    <Label>
                                       <ogc:PropertyName>0</ogc:PropertyName>
                                    </Label>
                                    <Font>
                                      <CssParameter name=""font-family"">Arial</CssParameter>
                                      <CssParameter name=""font-size"">24</CssParameter>
                                      <CssParameter name=""font-style"">italic</CssParameter>
                                      <CssParameter name=""font-weight"">bold</CssParameter>
                                    </Font>
                                    <LabelPlacement>
                                      <PointPlacement>
                                        <AnchorPoint>
                                          <AnchorPointX>0.0</AnchorPointX>
                                          <AnchorPointY>0.0</AnchorPointY>
                                        </AnchorPoint>
                                        <Displacement>
                                          <DisplacementX>0</DisplacementX>
                                          <DisplacementY>0</DisplacementY>
                                        </Displacement>
				                        <Rotation>45</Rotation>
                                      </PointPlacement>
                                    </LabelPlacement>
			                        <Halo>
				                        <Radius>3</Radius>
				                        <Fill>
					                        <CssParameter name=""fill"">#FFFFFF</CssParameter>
				                        </Fill>
			                        </Halo>
                                    <Fill>
				                        <CssParameter name=""fill"">#0000FF</CssParameter>
                                    </Fill>
                                  </TextSymbolizer>                            
                            </Rule>
                        </FeatureTypeStyle>
                    </UserStyle>
                </NamedLayer>
            </StyledLayerDescriptor>";

            mapserver.ApplySldDefinition("test", sldDescription);

            var imageBlueFeature = mapserver.MapserverDrawImage("image/png", 2400, 1200); //take style from layer modified with sld

            var path = Path.Combine(Path.GetTempPath(), String.Format("GAMapserverAddGeometry2_Blue_{0}.png", Guid.NewGuid()));
            File.WriteAllBytes(path, imageBlueFeature);

            //AssertImage.AreSimilar(new Bitmap("..\\testimages\\GAMapserverAddGeometry2_Blue_reference.png", true), imageBlueFeature as Bitmap, 0, 0);
        }
    }
}