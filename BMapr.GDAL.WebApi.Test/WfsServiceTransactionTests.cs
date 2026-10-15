using System.Xml;
using BMapr.GDAL.WebApi.Models;
using BMapr.GDAL.WebApi.Models.Spatial;
using BMapr.GDAL.WebApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace BMapr.GDAL.WebApi.Test
{
    [TestClass]
    public class WfsServiceTransactionTests
    {
        private const string ConnectionString = "Data Source=BAD-ZBOOK;Initial Catalog=GisData;Persist Security Info=True;User ID=sa;Password=sa;Pooling=False;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=True;Command Timeout=0";

        private class TestLogger : ILogger<FeatureServiceMsSql>
        {
            public List<string> ErrorMessages { get; } = new List<string>();
            public List<string> WarningMessages { get; } = new List<string>();

            public IDisposable? BeginScope<TState>(TState state) => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            {
                var message = formatter(state, exception);
                if (logLevel == LogLevel.Error)
                    ErrorMessages.Add(message);
                else if (logLevel == LogLevel.Warning)
                    WarningMessages.Add(message);
            }
        }

        private static bool IsConnectionAvailable()
        {
            try
            {
                using (var connection = new SqlConnection(ConnectionString))
                {
                    connection.Open();
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        #region Helper Methods

        /// <summary>
        /// Converts a FeatureBody with properties to GML format for WFS Transaction
        /// Each feature is a complete XML element that can be loaded as its own document
        /// Geometry is converted from WKT coordinates to GML format
        /// </summary>
        private string ConvertFeatureToGml(FeatureBody feature, string featureName = "SpatialFeature")
        {
            var gml = new System.Text.StringBuilder();
            gml.Append($"<{featureName}>");

            // Add properties
            foreach (var property in feature.Properties)
            {
                var value = property.Value;
                if (value == null)
                {
                    gml.Append($"<{property.Key}/>");
                }
                else if (value is byte[] binaryValue)
                {
                    var hexString = Convert.ToHexString(binaryValue);
                    gml.Append($"<{property.Key}>{hexString}</{property.Key}>");
                }
                else if (value is bool boolValue)
                {
                    // Store booleans as lowercase strings "true" or "false" for proper XSD boolean parsing
                    gml.Append($"<{property.Key}>{(boolValue ? "true" : "false")}</{property.Key}>");
                }
                else
                {
                    var stringValue = value.ToString();
                    gml.Append($"<{property.Key}>{System.Net.WebUtility.HtmlEncode(stringValue)}</{property.Key}>");
                }
            }

            // Add geometry as GML if present
            // Convert WKT format like "POLYGON((x1 y1, x2 y2, ...))" to GML format
            if (!string.IsNullOrEmpty(feature.Geometry))
            {
                var gmlGeometry = ConvertWktToGml(feature.Geometry);
                gml.Append($"<Geometry>{gmlGeometry}</Geometry>");
            }

            gml.Append($"</{featureName}>");
            return gml.ToString();
        }

        /// <summary>
        /// Converts WKT geometry format to GML format
        /// Example: POLYGON((x1 y1, x2 y2, x3 y3, x1 y1)) -> <gml:Polygon><gml:exterior><gml:LinearRing><gml:posList>x1 y1 x2 y2 x3 y3 x1 y1</gml:posList></gml:LinearRing></gml:exterior></gml:Polygon>
        /// </summary>
        private string ConvertWktToGml(string wkt)
        {
            wkt = wkt.Trim();

            if (wkt.StartsWith("POLYGON", StringComparison.OrdinalIgnoreCase))
            {
                // Extract coordinates from POLYGON((x1 y1, x2 y2, ...))
                var match = System.Text.RegularExpressions.Regex.Match(wkt, @"POLYGON\(\((.*?)\)\)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    var coords = match.Groups[1].Value;
                    // Convert "x1 y1, x2 y2, ..." to "x1 y1 x2 y2 ..."
                    var posList = coords.Replace(",", " ").Replace("  ", " ").Trim();
                    return $"<gml:Polygon><gml:exterior><gml:LinearRing><gml:posList>{posList}</gml:posList></gml:LinearRing></gml:exterior></gml:Polygon>";
                }
            }
            else if (wkt.StartsWith("LINESTRING", StringComparison.OrdinalIgnoreCase))
            {
                var match = System.Text.RegularExpressions.Regex.Match(wkt, @"LINESTRING\((.*?)\)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    var coords = match.Groups[1].Value;
                    var posList = coords.Replace(",", " ").Replace("  ", " ").Trim();
                    return $"<gml:LineString><gml:posList>{posList}</gml:posList></gml:LineString>";
                }
            }
            else if (wkt.StartsWith("POINT", StringComparison.OrdinalIgnoreCase))
            {
                var match = System.Text.RegularExpressions.Regex.Match(wkt, @"POINT\((.*?)\)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    var coords = match.Groups[1].Value.Trim();
                    return $"<gml:Point><gml:pos>{coords}</gml:pos></gml:Point>";
                }
            }

            // If format not recognized, return empty GML
            return $"<gml:Point><gml:pos>0 0</gml:pos></gml:Point>";
        }

        /// <summary>
        /// Creates a WFS 1.1.0 Transaction XML request with separate Insert elements for each GML feature
        /// This format properly separates features so each one becomes a single string in the Insert[] array
        /// </summary>
        private string CreateTransactionRequest(List<FeatureBody> features, string featureName = "SpatialFeature")
        {
            var xml = new System.Text.StringBuilder();
            xml.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            xml.AppendLine("<Transaction service=\"WFS\" version=\"1.1.0\"");
            xml.AppendLine("  xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\"");
            xml.AppendLine("  xmlns=\"http://www.opengis.net/wfs\"");
            xml.AppendLine("  xmlns:gml=\"http://www.opengis.net/gml\"");
            xml.AppendLine("  xsi:schemaLocation=\"http://www.opengis.net/wfs http://schemas.opengis.net/wfs/1.1.0/wfs.xsd\">");

            // Each feature gets its own Insert element
            // SetCDATAForInserts will wrap each Insert's content in CDATA
            // Then XmlService.DeserializeString will create an array where each Insert becomes one string item
            foreach (var feature in features)
            {
                xml.AppendLine("  <Insert>");
                var gmlFeature = ConvertFeatureToGml(feature, featureName);
                xml.Append(gmlFeature);
                xml.AppendLine("  </Insert>");
            }

            xml.AppendLine("</Transaction>");
            return xml.ToString();
        }

        /// <summary>
        /// Verifies if a record with a specific name pattern exists in the database
        /// </summary>
        /// <summary>
        /// Queries the database to get feature ID by Name
        /// Returns the ID of the first matching record
        /// </summary>
        private static long QueryFeatureIdByName(string nameValue)
        {
            try
            {
                using (var connection = new SqlConnection(ConnectionString))
                {
                    connection.Open();
                    using (var command = new SqlCommand(
                        "SELECT TOP 1 [Id] FROM dbo.SpatialFeature WHERE [Name] = @Name ORDER BY [Id] DESC",
                        connection))
                    {
                        command.Parameters.AddWithValue("@Name", nameValue);
                        var result = command.ExecuteScalar();
                        if (result != null)
                        {
                            return Convert.ToInt64(result);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error querying feature ID: {ex.Message}");
            }
            return -1;
        }

        /// <summary>
        /// Creates a WFS 1.1.0 Update transaction XML request with correct schema structure
        /// Filter contains a single FeatureId element with fid attribute
        /// </summary>
        private string CreateUpdateTransactionRequest(long featureId, Dictionary<string, object> propertyUpdates, string layerName = "SpatialFeature")
        {
            var xml = new System.Text.StringBuilder();
            xml.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            xml.AppendLine("<Transaction service=\"WFS\" version=\"1.1.0\"");
            xml.AppendLine("  xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\"");
            xml.AppendLine("  xmlns=\"http://www.opengis.net/wfs\"");
            xml.AppendLine("  xmlns:ogc=\"http://www.opengis.net/ogc\"");
            xml.AppendLine("  xmlns:gml=\"http://www.opengis.net/gml\"");
            xml.AppendLine("  xsi:schemaLocation=\"http://www.opengis.net/wfs http://schemas.opengis.net/wfs/1.1.0/wfs.xsd\">");

            xml.AppendLine($"  <Update typeName=\"{layerName}\">");

            // Add Property elements
            foreach (var propertyUpdate in propertyUpdates)
            {
                xml.AppendLine("    <Property>");
                xml.AppendLine($"      <Name>{propertyUpdate.Key}</Name>");

                // Value handling
                if (propertyUpdate.Value == null)
                {
                    xml.AppendLine("      <Value></Value>");
                }
                else if (propertyUpdate.Value is bool boolValue)
                {
                    xml.AppendLine($"      <Value>{(boolValue ? "true" : "false")}</Value>");
                }
                else if (propertyUpdate.Value is byte[] binaryValue)
                {
                    var hexString = Convert.ToHexString(binaryValue);
                    xml.AppendLine($"      <Value>{hexString}</Value>");
                }
                else
                {
                    var stringValue = propertyUpdate.Value.ToString();
                    xml.AppendLine($"      <Value>{System.Net.WebUtility.HtmlEncode(stringValue)}</Value>");
                }

                xml.AppendLine("    </Property>");
            }

            // Add Filter with FeatureId - single Filter element containing FeatureId
            xml.AppendLine("    <Filter>");
            xml.AppendLine($"      <FeatureId fid=\"{layerName}.{featureId}\"/>");
            xml.AppendLine("    </Filter>");

            xml.AppendLine("  </Update>");
            xml.AppendLine("</Transaction>");
            return xml.ToString();
        }

        /// <summary>
        /// Creates a WFS 1.1.0 Delete transaction XML request with correct schema structure
        /// Filter array contains FeatureId elements with fid attributes
        /// </summary>
        private string CreateDeleteTransactionRequest(long featureId, string layerName = "SpatialFeature")
        {
            var xml = new System.Text.StringBuilder();
            xml.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            xml.AppendLine("<Transaction service=\"WFS\" version=\"1.1.0\"");
            xml.AppendLine("  xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\"");
            xml.AppendLine("  xmlns=\"http://www.opengis.net/wfs\"");
            xml.AppendLine("  xmlns:ogc=\"http://www.opengis.net/ogc\"");
            xml.AppendLine("  xmlns:gml=\"http://www.opengis.net/gml\"");
            xml.AppendLine("  xsi:schemaLocation=\"http://www.opengis.net/wfs http://schemas.opengis.net/wfs/1.1.0/wfs.xsd\">");

            xml.AppendLine($"  <Delete typeName=\"{layerName}\">");
            xml.AppendLine("    <Filter>");
            xml.AppendLine($"      <FeatureId fid=\"{layerName}.{featureId}\"/>");
            xml.AppendLine("    </Filter>");
            xml.AppendLine("  </Delete>");
            xml.AppendLine("</Transaction>");
            return xml.ToString();
        }

        private static bool VerifyRecordExists(string namePattern)
        {
            try
            {
                using (var connection = new SqlConnection(ConnectionString))
                {
                    connection.Open();
                    using (var command = new SqlCommand(
                        "SELECT COUNT(*) FROM dbo.SpatialFeature WHERE [Name] LIKE @Pattern",
                        connection))
                    {
                        command.Parameters.AddWithValue("@Pattern", $"%{namePattern}%");
                        var result = command.ExecuteScalar();
                        return result != null && Convert.ToInt32(result) > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error verifying record: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Creates a test MapConfig with layer configuration for dbo.SpatialFeature
        /// </summary>
        private static MapConfig CreateMapConfigForTesting()
        {
            var mapConfig = new MapConfig();

            // Mock layer configuration for SpatialFeature table
            var layerConfig = new MapLayerConfig
            {
                LayerName = "SpatialFeature",
                TableName = "dbo.SpatialFeature",
                Connection = ConnectionString,
                Id = "Id",
                IdType = "AutoInt",
                GeometryFieldName = "Geometry",
                EPSG = 2056,
                WFSTuseMsSqlServerFeatureService = true,
                WFSTMsSqlServerGeography = false,
                IdSquenceManual = false,
                IdSequenceStartValue = 1
            };

            // Add layer to MapConfig
            mapConfig.MapLayers.Add("SpatialFeature", layerConfig);
            mapConfig.WFST110enabled = true;
            return mapConfig;
        }

        /// <summary>
        /// Creates a test Config object for testing
        /// </summary>
        private static Config CreateTestConfig()
        {
            var config = new Config();
            // Mock minimal config needed for debug logging paths
            return config;
        }

        #endregion

        #region Test Methods

        [TestMethod]
        public void TransactionInsertWithAllDataTypes_StringVariations()
        {
            if (!IsConnectionAvailable())
            {
                Assert.Inconclusive("SQL Server instance is not available. Test skipped.");
            }

            try
            {
                // Create test features with string data
                var features = new List<FeatureBody>()
                {
                    new FeatureBody()
                    {
                        Properties = new Dictionary<string, object>()
                        {
                            { "Name", "StringTest1" },
                            { "Description", "This is a long description with special characters: !@#$%^&*()" },
                            { "Varchar10", "Test1234" },
                            { "Varchar50", "This is a medium length string test" },
                            { "VarcharMax", "This is a very long string content that can exceed normal varchar limits. Lorem ipsum dolor sit amet, consectetur adipiscing elit." },
                            { "Category", "TestCategory" }
                        },
                        Geometry = "POLYGON((2600520 1150191, 2715764 1186190, 2511651 1138667, 2600520 1150191))",
                        Epsg = 0
                    },
                    new FeatureBody()
                    {
                        Properties = new Dictionary<string, object>()
                        {
                            { "Name", "StringTest2" },
                            { "Description", "Second test with Unicode: Ñoño, Zürich, 日本" },
                            { "Varchar10", "Short" },
                            { "Varchar50", "Another medium test string" },
                            { "VarcharMax", "Extended content with more data..." },
                            { "Category", "TestCategory2" }
                        },
                        Geometry = "POLYGON((2511651 1138667, 2600520 1150191, 2626895 1174666, 2511651 1138667))",
                        Epsg = 0
                    }
                };

                // Create WFS Transaction XML with GML
                var transactionXml = CreateTransactionRequest(features, "SpatialFeature");
                Assert.IsNotNull(transactionXml, "Transaction XML should be created from features");
                Assert.IsTrue(transactionXml.Contains("<Name>StringTest1"), "Transaction XML should contain feature data");

                // Call WfsService.TransactionCore with the GML transaction
                var mapConfig = CreateMapConfigForTesting();
                var config = CreateTestConfig();
                var result = WfsService.TransactionCore(transactionXml, mapConfig, config, "TestProject", null);

                // Verify result is successful
                Assert.IsNotNull(result, "Transaction result should not be null");
                Assert.IsInstanceOfType(result, typeof(ContentResult), "Result should be ContentResult");

                var contentResult = result as ContentResult;
                Assert.AreEqual(200, contentResult.StatusCode, "Result should have 200 status code");
                Assert.IsTrue(contentResult.Content.Contains("TransactionResponse"), "Result should contain TransactionResponse");
                Assert.IsTrue(contentResult.Content.Contains("totalInserted"), "Result should indicate inserted features");

                // Verify records were actually inserted to database
                Assert.IsTrue(VerifyRecordExists("StringTest"), "String test records should exist in database after transaction.");

                // UPDATE: Modify the first inserted record
                var featureId = QueryFeatureIdByName("StringTest1");
                Assert.IsTrue(featureId > 0, "Should successfully query inserted feature ID");

                var updateProperties = new Dictionary<string, object>()
                {
                    { "Description", "UPDATED: This is an updated description" },
                    { "Category", "UpdatedCategory" }
                };
                var updateXml = CreateUpdateTransactionRequest(featureId, updateProperties, "SpatialFeature");
                Assert.IsNotNull(updateXml, "Update transaction XML should be created");

                var updateResult = WfsService.TransactionCore(updateXml, mapConfig, config, "TestProject", null);
                Assert.IsNotNull(updateResult, "Update result should not be null");
                Assert.IsInstanceOfType(updateResult, typeof(ContentResult), "Update result should be ContentResult");

                var updateContentResult = updateResult as ContentResult;
                Assert.AreEqual(200, updateContentResult.StatusCode, "Update should have 200 status code");
                Assert.IsTrue(updateContentResult.Content.Contains("TransactionResponse"), "Update result should contain TransactionResponse");
                Assert.IsTrue(updateContentResult.Content.Contains("totalUpdated"), "Update result should indicate updated features");

                // DELETE: Remove the updated record
                var deleteXml = CreateDeleteTransactionRequest(featureId, "SpatialFeature");
                Assert.IsNotNull(deleteXml, "Delete transaction XML should be created");

                var deleteResult = WfsService.TransactionCore(deleteXml, mapConfig, config, "TestProject", null);
                Assert.IsNotNull(deleteResult, "Delete result should not be null");
                Assert.IsInstanceOfType(deleteResult, typeof(ContentResult), "Delete result should be ContentResult");

                var deleteContentResult = deleteResult as ContentResult;
                Assert.AreEqual(200, deleteContentResult.StatusCode, "Delete should have 200 status code");
                Assert.IsTrue(deleteContentResult.Content.Contains("TransactionResponse"), "Delete result should contain TransactionResponse");
                Assert.IsTrue(deleteContentResult.Content.Contains("totalDeleted"), "Delete result should indicate deleted features");
            }
            catch (Exception ex)
            {
                Assert.Fail($"Test failed with exception: {ex.Message}");
            }
        }

        [TestMethod]
        public void TransactionInsertWithAllDataTypes_IntegerVariations()
        {
            if (!IsConnectionAvailable())
            {
                Assert.Inconclusive("SQL Server instance is not available. Test skipped.");
            }

            try
            {
                var features = new List<FeatureBody>()
                {
                    new FeatureBody()
                    {
                        Properties = new Dictionary<string, object>()
                        {
                            { "Name", "Parcel 1" },
                            { "IntValue", 2147483647 },
                            { "BigIntValue", 9223372036854775807L }
                        },
                        Geometry = "POLYGON((2600520 1150191, 2715764 1186190, 2511651 1138667, 2600520 1150191))",
                        Epsg = 0
                    },
                    new FeatureBody()
                    {
                        Properties = new Dictionary<string, object>()
                        {
                            { "Name", "NegativeIntegerTest" },
                            { "IntValue", -2147483648 },
                            { "BigIntValue", -9223372036854775808L }
                        },
                        Geometry = "POLYGON((2511651 1138667, 2600520 1150191, 2626895 1174666, 2511651 1138667))",
                        Epsg = 0
                    },
                    new FeatureBody()
                    {
                        Properties = new Dictionary<string, object>()
                        {
                            { "Name", "ZeroIntegerTest" },
                            { "IntValue", 0 },
                            { "BigIntValue", 0L }
                        },
                        Geometry = "POLYGON((2626895 1174666, 2715764 1186190, 2600520 1150191, 2626895 1174666))",
                        Epsg = 0
                    }
                };

                var transactionXml = CreateTransactionRequest(features, "SpatialFeature");
                Assert.IsNotNull(transactionXml, "Transaction XML should be created from features");
                Assert.IsTrue(transactionXml.Contains("<Name>Parcel 1"), "Transaction XML should contain feature data");

                var mapConfig = CreateMapConfigForTesting();
                var config = CreateTestConfig();
                var result = WfsService.TransactionCore(transactionXml, mapConfig, config, "TestProject", null);

                Assert.IsNotNull(result, "Transaction result should not be null");
                Assert.IsInstanceOfType(result, typeof(ContentResult), "Result should be ContentResult");

                var contentResult = result as ContentResult;
                Assert.AreEqual(200, contentResult.StatusCode, "Result should have 200 status code");
                Assert.IsTrue(contentResult.Content.Contains("TransactionResponse"), "Result should contain TransactionResponse");
                Assert.IsTrue(VerifyRecordExists("Parcel 1"), "Integer test records should exist.");

                // UPDATE: Modify the first inserted record
                var featureId = QueryFeatureIdByName("Parcel 1");
                Assert.IsTrue(featureId > 0, "Should successfully query inserted feature ID");

                var updateProperties = new Dictionary<string, object>()
                {
                    { "IntValue", 999999 },
                    { "BigIntValue", 9999999999L }
                };
                var updateXml = CreateUpdateTransactionRequest(featureId, updateProperties, "SpatialFeature");
                Assert.IsNotNull(updateXml, "Update transaction XML should be created");

                var updateResult = WfsService.TransactionCore(updateXml, mapConfig, config, "TestProject", null);
                Assert.IsNotNull(updateResult, "Update result should not be null");
                Assert.IsInstanceOfType(updateResult, typeof(ContentResult), "Update result should be ContentResult");

                var updateContentResult = updateResult as ContentResult;
                Assert.AreEqual(200, updateContentResult.StatusCode, "Update should have 200 status code");
                Assert.IsTrue(updateContentResult.Content.Contains("totalUpdated"), "Update result should indicate updated features");

                // DELETE: Remove the updated record
                var deleteXml = CreateDeleteTransactionRequest(featureId, "SpatialFeature");
                Assert.IsNotNull(deleteXml, "Delete transaction XML should be created");

                var deleteResult = WfsService.TransactionCore(deleteXml, mapConfig, config, "TestProject", null);
                Assert.IsNotNull(deleteResult, "Delete result should not be null");
                Assert.IsInstanceOfType(deleteResult, typeof(ContentResult), "Delete result should be ContentResult");

                var deleteContentResult = deleteResult as ContentResult;
                Assert.AreEqual(200, deleteContentResult.StatusCode, "Delete should have 200 status code");
                Assert.IsTrue(deleteContentResult.Content.Contains("totalDeleted"), "Delete result should indicate deleted features");
            }
            catch (Exception ex)
            {
                Assert.Fail($"Test failed with exception: {ex.Message}");
            }
        }

        [TestMethod]
        public void TransactionInsertWithAllDataTypes_DecimalAndFloat()
        {
            if (!IsConnectionAvailable())
            {
                Assert.Inconclusive("SQL Server instance is not available. Test skipped.");
            }

            try
            {
                var features = new List<FeatureBody>()
                {
                    new FeatureBody()
                    {
                        Properties = new Dictionary<string, object>()
                        {
                            { "Name", "DecimalPositiveTest" },
                            { "DecimalValue", 12345.6789m },
                            { "FloatValue", 1234.56789 }
                        },
                        Geometry = "POLYGON((2600520 1150191, 2715764 1186190, 2511651 1138667, 2600520 1150191))",
                        Epsg = 0
                    },
                    new FeatureBody()
                    {
                        Properties = new Dictionary<string, object>()
                        {
                            { "Name", "DecimalNegativeTest" },
                            { "DecimalValue", -98765.4321m },
                            { "FloatValue", -9876.54321 }
                        },
                        Geometry = "POLYGON((2511651 1138667, 2600520 1150191, 2626895 1174666, 2511651 1138667))",
                        Epsg = 0
                    },
                    new FeatureBody()
                    {
                        Properties = new Dictionary<string, object>()
                        {
                            { "Name", "DecimalZeroTest" },
                            { "DecimalValue", 0.0000m },
                            { "FloatValue", 0.0 }
                        },
                        Geometry = "POLYGON((2626895 1174666, 2715764 1186190, 2600520 1150191, 2626895 1174666))",
                        Epsg = 0
                    },
                    new FeatureBody()
                    {
                        Properties = new Dictionary<string, object>()
                        {
                            { "Name", "DecimalSmallValueTest" },
                            { "DecimalValue", 0.0001m },
                            { "FloatValue", 0.00001 }
                        },
                        Geometry = "POLYGON((2715764 1186190, 2626895 1174666, 2511651 1138667, 2715764 1186190))",
                        Epsg = 0
                    }
                };

                var transactionXml = CreateTransactionRequest(features, "SpatialFeature");
                Assert.IsNotNull(transactionXml, "Transaction XML should be created from features");
                Assert.IsTrue(transactionXml.Contains("<Name>DecimalPositiveTest"), "Transaction XML should contain feature data");

                var mapConfig = CreateMapConfigForTesting();
                var config = CreateTestConfig();
                var result = WfsService.TransactionCore(transactionXml, mapConfig, config, "TestProject", null);

                Assert.IsNotNull(result, "Transaction result should not be null");
                Assert.IsInstanceOfType(result, typeof(ContentResult), "Result should be ContentResult");

                var contentResult = result as ContentResult;
                Assert.AreEqual(200, contentResult.StatusCode, "Result should have 200 status code");
                Assert.IsTrue(contentResult.Content.Contains("TransactionResponse"), "Result should contain TransactionResponse");
                Assert.IsTrue(VerifyRecordExists("DecimalPositiveTest"), "Decimal test records should exist.");

                // UPDATE: Modify the first inserted record
                var featureId = QueryFeatureIdByName("DecimalPositiveTest");
                Assert.IsTrue(featureId > 0, "Should successfully query inserted feature ID");

                var updateProperties = new Dictionary<string, object>()
                {
                    { "DecimalValue", 99999.9999m },
                    { "FloatValue", 9999.99999 }
                };
                var updateXml = CreateUpdateTransactionRequest(featureId, updateProperties, "SpatialFeature");
                Assert.IsNotNull(updateXml, "Update transaction XML should be created");

                var updateResult = WfsService.TransactionCore(updateXml, mapConfig, config, "TestProject", null);
                Assert.IsNotNull(updateResult, "Update result should not be null");
                Assert.IsInstanceOfType(updateResult, typeof(ContentResult), "Update result should be ContentResult");

                var updateContentResult = updateResult as ContentResult;
                Assert.AreEqual(200, updateContentResult.StatusCode, "Update should have 200 status code");
                Assert.IsTrue(updateContentResult.Content.Contains("totalUpdated"), "Update result should indicate updated features");

                // DELETE: Remove the updated record
                var deleteXml = CreateDeleteTransactionRequest(featureId, "SpatialFeature");
                Assert.IsNotNull(deleteXml, "Delete transaction XML should be created");

                var deleteResult = WfsService.TransactionCore(deleteXml, mapConfig, config, "TestProject", null);
                Assert.IsNotNull(deleteResult, "Delete result should not be null");
                Assert.IsInstanceOfType(deleteResult, typeof(ContentResult), "Delete result should be ContentResult");

                var deleteContentResult = deleteResult as ContentResult;
                Assert.AreEqual(200, deleteContentResult.StatusCode, "Delete should have 200 status code");
                Assert.IsTrue(deleteContentResult.Content.Contains("totalDeleted"), "Delete result should indicate deleted features");
            }
            catch (Exception ex)
            {
                Assert.Fail($"Test failed with exception: {ex.Message}");
            }
        }

        [TestMethod]
        public void TransactionInsertWithAllDataTypes_BooleanValues()
        {
            if (!IsConnectionAvailable())
            {
                Assert.Inconclusive("SQL Server instance is not available. Test skipped.");
            }

            try
            {
                var features = new List<FeatureBody>()
                {
                    new FeatureBody()
                    {
                        Properties = new Dictionary<string, object>()
                        {
                            { "Name", "BooleanTrueTest" },
                            { "BooleanValue", true }
                        },
                        Geometry = "POLYGON((2600520 1150191, 2715764 1186190, 2511651 1138667, 2600520 1150191))",
                        Epsg = 0
                    },
                    new FeatureBody()
                    {
                        Properties = new Dictionary<string, object>()
                        {
                            { "Name", "BooleanFalseTest" },
                            { "BooleanValue", false }
                        },
                        Geometry = "POLYGON((2511651 1138667, 2600520 1150191, 2626895 1174666, 2511651 1138667))",
                        Epsg = 0
                    },
                    new FeatureBody()
                    {
                        Properties = new Dictionary<string, object>()
                        {
                            { "Name", "BooleanNullTest" }
                        },
                        Geometry = "POLYGON((2626895 1174666, 2715764 1186190, 2600520 1150191, 2626895 1174666))",
                        Epsg = 0
                    }
                };

                var transactionXml = CreateTransactionRequest(features, "SpatialFeature");
                Assert.IsNotNull(transactionXml, "Transaction XML should be created from features");
                Assert.IsTrue(transactionXml.Contains("<Name>BooleanTrueTest"), "Transaction XML should contain feature data");

                var mapConfig = CreateMapConfigForTesting();
                var config = CreateTestConfig();
                var result = WfsService.TransactionCore(transactionXml, mapConfig, config, "TestProject", null);

                Assert.IsNotNull(result, "Transaction result should not be null");
                Assert.IsInstanceOfType(result, typeof(ContentResult), "Result should be ContentResult");

                var contentResult = result as ContentResult;
                Assert.AreEqual(200, contentResult.StatusCode, "Result should have 200 status code");
                Assert.IsTrue(contentResult.Content.Contains("TransactionResponse"), "Result should contain TransactionResponse");
                Assert.IsTrue(VerifyRecordExists("BooleanTrueTest"), "Boolean test records should exist.");

                // UPDATE: Modify the first inserted record
                var featureId = QueryFeatureIdByName("BooleanTrueTest");
                Assert.IsTrue(featureId > 0, "Should successfully query inserted feature ID");

                var updateProperties = new Dictionary<string, object>()
                {
                    { "BooleanValue", false }
                };
                var updateXml = CreateUpdateTransactionRequest(featureId, updateProperties, "SpatialFeature");
                Assert.IsNotNull(updateXml, "Update transaction XML should be created");

                var updateResult = WfsService.TransactionCore(updateXml, mapConfig, config, "TestProject", null);
                Assert.IsNotNull(updateResult, "Update result should not be null");
                Assert.IsInstanceOfType(updateResult, typeof(ContentResult), "Update result should be ContentResult");

                var updateContentResult = updateResult as ContentResult;
                Assert.AreEqual(200, updateContentResult.StatusCode, "Update should have 200 status code");
                Assert.IsTrue(updateContentResult.Content.Contains("totalUpdated"), "Update result should indicate updated features");

                // DELETE: Remove the updated record
                var deleteXml = CreateDeleteTransactionRequest(featureId, "SpatialFeature");
                Assert.IsNotNull(deleteXml, "Delete transaction XML should be created");

                var deleteResult = WfsService.TransactionCore(deleteXml, mapConfig, config, "TestProject", null);
                Assert.IsNotNull(deleteResult, "Delete result should not be null");
                Assert.IsInstanceOfType(deleteResult, typeof(ContentResult), "Delete result should be ContentResult");

                var deleteContentResult = deleteResult as ContentResult;
                Assert.AreEqual(200, deleteContentResult.StatusCode, "Delete should have 200 status code");
                Assert.IsTrue(deleteContentResult.Content.Contains("totalDeleted"), "Delete result should indicate deleted features");
            }
            catch (Exception ex)
            {
                Assert.Fail($"Test failed with exception: {ex.Message}");
            }
        }

        [TestMethod]
        public void TransactionInsertWithAllDataTypes_DateTimeVariations()
        {
            if (!IsConnectionAvailable())
            {
                Assert.Inconclusive("SQL Server instance is not available. Test skipped.");
            }

            try
            {
                var dateStr1 = "2024-03-15";
                var dateStr2 = "2000-01-01";
                var dateStr3 = "2099-12-31";
                var dateTimeStr1 = "2024-03-15 14:30:45";
                var dateTimeStr2 = "2000-01-01 00:00:00";
                var dateTimeStr3 = "2099-12-31 23:59:59";

                var features = new List<FeatureBody>()
                {
                    new FeatureBody()
                    {
                        Properties = new Dictionary<string, object>()
                        {
                            { "Name", "Parcel 2" },
                            { "DateValue", dateStr1 },
                            { "DateTimeValue", dateTimeStr1 }
                        },
                        Geometry = "POLYGON((2600520 1150191, 2715764 1186190, 2511651 1138667, 2600520 1150191))",
                        Epsg = 0
                    },
                    new FeatureBody()
                    {
                        Properties = new Dictionary<string, object>()
                        {
                            { "Name", "DateTimeYearMinTest" },
                            { "DateValue", dateStr2 },
                            { "DateTimeValue", dateTimeStr2 }
                        },
                        Geometry = "POLYGON((2511651 1138667, 2600520 1150191, 2626895 1174666, 2511651 1138667))",
                        Epsg = 0
                    },
                    new FeatureBody()
                    {
                        Properties = new Dictionary<string, object>()
                        {
                            { "Name", "DateTimeYearMaxTest" },
                            { "DateValue", dateStr3 },
                            { "DateTimeValue", dateTimeStr3 }
                        },
                        Geometry = "POLYGON((2626895 1174666, 2715764 1186190, 2600520 1150191, 2626895 1174666))",
                        Epsg = 0
                    }
                };

                var transactionXml = CreateTransactionRequest(features, "SpatialFeature");
                Assert.IsNotNull(transactionXml, "Transaction XML should be created from features");
                Assert.IsTrue(transactionXml.Contains("<Name>Parcel 2"), "Transaction XML should contain feature data");

                var mapConfig = CreateMapConfigForTesting();
                var config = CreateTestConfig();
                var result = WfsService.TransactionCore(transactionXml, mapConfig, config, "TestProject", null);

                Assert.IsNotNull(result, "Transaction result should not be null");
                Assert.IsInstanceOfType(result, typeof(ContentResult), "Result should be ContentResult");

                var contentResult = result as ContentResult;
                Assert.AreEqual(200, contentResult.StatusCode, "Result should have 200 status code");
                Assert.IsTrue(contentResult.Content.Contains("TransactionResponse"), "Result should contain TransactionResponse");
                Assert.IsTrue(VerifyRecordExists("Parcel 2"), "DateTime test records should exist after transaction.");

                // UPDATE: Modify the first inserted record
                var featureId = QueryFeatureIdByName("Parcel 2");
                Assert.IsTrue(featureId > 0, "Should successfully query inserted feature ID");

                var updateProperties = new Dictionary<string, object>()
                {
                    { "DateValue", "2025-06-20" },
                    { "DateTimeValue", "2025-06-20 12:00:00" }
                };
                var updateXml = CreateUpdateTransactionRequest(featureId, updateProperties, "SpatialFeature");
                Assert.IsNotNull(updateXml, "Update transaction XML should be created");

                var updateResult = WfsService.TransactionCore(updateXml, mapConfig, config, "TestProject", null);
                Assert.IsNotNull(updateResult, "Update result should not be null");
                Assert.IsInstanceOfType(updateResult, typeof(ContentResult), "Update result should be ContentResult");

                var updateContentResult = updateResult as ContentResult;
                Assert.AreEqual(200, updateContentResult.StatusCode, "Update should have 200 status code");
                Assert.IsTrue(updateContentResult.Content.Contains("totalUpdated"), "Update result should indicate updated features");

                // DELETE: Remove the updated record
                var deleteXml = CreateDeleteTransactionRequest(featureId, "SpatialFeature");
                Assert.IsNotNull(deleteXml, "Delete transaction XML should be created");

                var deleteResult = WfsService.TransactionCore(deleteXml, mapConfig, config, "TestProject", null);
                Assert.IsNotNull(deleteResult, "Delete result should not be null");
                Assert.IsInstanceOfType(deleteResult, typeof(ContentResult), "Delete result should be ContentResult");

                var deleteContentResult = deleteResult as ContentResult;
                Assert.AreEqual(200, deleteContentResult.StatusCode, "Delete should have 200 status code");
                Assert.IsTrue(deleteContentResult.Content.Contains("totalDeleted"), "Delete result should indicate deleted features");
            }
            catch (Exception ex)
            {
                Assert.Fail($"Test failed with exception: {ex.Message}");
            }
        }

        [TestMethod]
        public void TransactionInsertWithAllDataTypes_GuidAndBinary()
        {
            if (!IsConnectionAvailable())
            {
                Assert.Inconclusive("SQL Server instance is not available. Test skipped.");
            }

            try
            {
                var guid1 = Guid.NewGuid();
                var guid2 = Guid.NewGuid();
                var guid3 = Guid.NewGuid();
                var binaryData1 = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 };
                var binaryData2 = new byte[] { 0xFF, 0xFE, 0xFD, 0xFC };
                var binaryData3 = new byte[] { 0xAA, 0xBB, 0xCC, 0xDD, 0xEE };

                var features = new List<FeatureBody>()
                {
                    new FeatureBody()
                    {
                        Properties = new Dictionary<string, object>()
                        {
                            { "Name", "GuidBinaryTest1" },
                            { "UniqueValue", guid1 },
                            { "BinaryValue", binaryData1 }
                        },
                        Geometry = "POLYGON((2600520 1150191, 2715764 1186190, 2511651 1138667, 2600520 1150191))",
                        Epsg = 0
                    },
                    new FeatureBody()
                    {
                        Properties = new Dictionary<string, object>()
                        {
                            { "Name", "GuidBinaryTest2" },
                            { "UniqueValue", guid2 },
                            { "BinaryValue", binaryData2 }
                        },
                        Geometry = "POLYGON((2511651 1138667, 2600520 1150191, 2626895 1174666, 2511651 1138667))",
                        Epsg = 0
                    },
                    new FeatureBody()
                    {
                        Properties = new Dictionary<string, object>()
                        {
                            { "Name", "GuidBinaryTest3" },
                            { "UniqueValue", guid3 },
                            { "BinaryValue", binaryData3 }
                        },
                        Geometry = "POLYGON((2626895 1174666, 2715764 1186190, 2600520 1150191, 2626895 1174666))",
                        Epsg = 0
                    }
                };

                var transactionXml = CreateTransactionRequest(features, "SpatialFeature");
                Assert.IsNotNull(transactionXml, "Transaction XML should be created from features");
                Assert.IsTrue(transactionXml.Contains("<Name>GuidBinaryTest1"), "Transaction XML should contain feature data");

                var mapConfig = CreateMapConfigForTesting();
                var config = CreateTestConfig();
                var result = WfsService.TransactionCore(transactionXml, mapConfig, config, "TestProject", null);

                Assert.IsNotNull(result, "Transaction result should not be null");
                Assert.IsInstanceOfType(result, typeof(ContentResult), "Result should be ContentResult");

                var contentResult = result as ContentResult;
                Assert.AreEqual(200, contentResult.StatusCode, "Result should have 200 status code");
                Assert.IsTrue(contentResult.Content.Contains("TransactionResponse"), "Result should contain TransactionResponse");
                Assert.IsTrue(VerifyRecordExists("GuidBinaryTest1"), "Guid/Binary test records should exist.");

                // UPDATE: Modify the first inserted record
                var featureId = QueryFeatureIdByName("GuidBinaryTest1");
                Assert.IsTrue(featureId > 0, "Should successfully query inserted feature ID");

                var newGuid = Guid.NewGuid();
                var newBinaryData = new byte[] { 0x11, 0x22, 0x33, 0x44, 0x55 };
                var updateProperties = new Dictionary<string, object>()
                {
                    { "UniqueValue", newGuid },
                    { "BinaryValue", newBinaryData }
                };
                var updateXml = CreateUpdateTransactionRequest(featureId, updateProperties, "SpatialFeature");
                Assert.IsNotNull(updateXml, "Update transaction XML should be created");

                var updateResult = WfsService.TransactionCore(updateXml, mapConfig, config, "TestProject", null);
                Assert.IsNotNull(updateResult, "Update result should not be null");
                Assert.IsInstanceOfType(updateResult, typeof(ContentResult), "Update result should be ContentResult");

                var updateContentResult = updateResult as ContentResult;
                Assert.AreEqual(200, updateContentResult.StatusCode, "Update should have 200 status code");
                Assert.IsTrue(updateContentResult.Content.Contains("totalUpdated"), "Update result should indicate updated features");

                // DELETE: Remove the updated record
                var deleteXml = CreateDeleteTransactionRequest(featureId, "SpatialFeature");
                Assert.IsNotNull(deleteXml, "Delete transaction XML should be created");

                var deleteResult = WfsService.TransactionCore(deleteXml, mapConfig, config, "TestProject", null);
                Assert.IsNotNull(deleteResult, "Delete result should not be null");
                Assert.IsInstanceOfType(deleteResult, typeof(ContentResult), "Delete result should be ContentResult");

                var deleteContentResult = deleteResult as ContentResult;
                Assert.AreEqual(200, deleteContentResult.StatusCode, "Delete should have 200 status code");
                Assert.IsTrue(deleteContentResult.Content.Contains("totalDeleted"), "Delete result should indicate deleted features");
            }
            catch (Exception ex)
            {
                Assert.Fail($"Test failed with exception: {ex.Message}");
            }
        }

        [TestMethod]
        public void TransactionInsertWithAllDataTypes_SimpleRecords()
        {
            if (!IsConnectionAvailable())
            {
                Assert.Inconclusive("SQL Server instance is not available. Test skipped.");
            }

            try
            {
                // Test simple records with minimal properties
                var features = new List<FeatureBody>()
                {
                    new FeatureBody()
                    {
                        Properties = new Dictionary<string, object>()
                        {
                            { "Name", "SimpleRecord1" },
                            { "Description", "First simple record" }
                        },
                        Geometry = "POLYGON((2600520 1150191, 2715764 1186190, 2511651 1138667, 2600520 1150191))",
                        Epsg = 0
                    },
                    new FeatureBody()
                    {
                        Properties = new Dictionary<string, object>()
                        {
                            { "Name", "SimpleRecord2" },
                            { "Description", "Second simple record" }
                        },
                        Geometry = "POLYGON((2511651 1138667, 2600520 1150191, 2626895 1174666, 2511651 1138667))",
                        Epsg = 0
                    }
                };

                var transactionXml = CreateTransactionRequest(features, "SpatialFeature");
                Assert.IsNotNull(transactionXml, "Transaction XML should be created from features");
                Assert.IsTrue(transactionXml.Contains("<Name>SimpleRecord1"), "Transaction XML should contain feature data");

                var mapConfig = CreateMapConfigForTesting();
                var config = CreateTestConfig();
                var result = WfsService.TransactionCore(transactionXml, mapConfig, config, "TestProject", null);

                Assert.IsNotNull(result, "Transaction result should not be null");
                Assert.IsInstanceOfType(result, typeof(ContentResult), "Result should be ContentResult");

                var contentResult = result as ContentResult;
                Assert.AreEqual(200, contentResult.StatusCode, "Result should have 200 status code");
                Assert.IsTrue(contentResult.Content.Contains("TransactionResponse"), "Result should contain TransactionResponse");
                Assert.IsTrue(VerifyRecordExists("SimpleRecord1"), "Simple test records should be inserted successfully.");

                // UPDATE: Modify the first inserted record
                var featureId = QueryFeatureIdByName("SimpleRecord1");
                Assert.IsTrue(featureId > 0, "Should successfully query inserted feature ID");

                var updateProperties = new Dictionary<string, object>()
                {
                    { "Description", "UPDATED: First simple record" }
                };
                var updateXml = CreateUpdateTransactionRequest(featureId, updateProperties, "SpatialFeature");
                Assert.IsNotNull(updateXml, "Update transaction XML should be created");

                var updateResult = WfsService.TransactionCore(updateXml, mapConfig, config, "TestProject", null);
                Assert.IsNotNull(updateResult, "Update result should not be null");
                Assert.IsInstanceOfType(updateResult, typeof(ContentResult), "Update result should be ContentResult");

                var updateContentResult = updateResult as ContentResult;
                Assert.AreEqual(200, updateContentResult.StatusCode, "Update should have 200 status code");
                Assert.IsTrue(updateContentResult.Content.Contains("totalUpdated"), "Update result should indicate updated features");

                // DELETE: Remove the updated record
                var deleteXml = CreateDeleteTransactionRequest(featureId, "SpatialFeature");
                Assert.IsNotNull(deleteXml, "Delete transaction XML should be created");

                var deleteResult = WfsService.TransactionCore(deleteXml, mapConfig, config, "TestProject", null);
                Assert.IsNotNull(deleteResult, "Delete result should not be null");
                Assert.IsInstanceOfType(deleteResult, typeof(ContentResult), "Delete result should be ContentResult");

                var deleteContentResult = deleteResult as ContentResult;
                Assert.AreEqual(200, deleteContentResult.StatusCode, "Delete should have 200 status code");
                Assert.IsTrue(deleteContentResult.Content.Contains("totalDeleted"), "Delete result should indicate deleted features");
            }
            catch (Exception ex)
            {
                Assert.Fail($"Test failed with exception: {ex.Message}");
            }
        }

        [TestMethod]
        public void TransactionInsertWithAllDataTypes_ComprehensiveMixed()
        {
            if (!IsConnectionAvailable())
            {
                Assert.Inconclusive("SQL Server instance is not available. Test skipped.");
            }

            try
            {
                var dateStr = "2024-03-15";
                var dateTimeStr = "2024-03-15 14:30:45";
                var testGuid = Guid.NewGuid();
                var binaryData = new byte[] { 0x48, 0x65, 0x6C, 0x6C, 0x6F }; // "Hello" in hex

                var features = new List<FeatureBody>()
                {
                    new FeatureBody()
                    {
                        Properties = new Dictionary<string, object>()
                        {
                            { "Name", "Parcel 3" },
                            { "Description", "Comprehensive test with all data types" },
                            { "Varchar10", "Compre" },
                            { "Varchar50", "Comprehensive test record with mixed data" },
                            { "VarcharMax", "This record contains all possible data types in a single feature for comprehensive testing" },
                            { "Category", "Mixed" },
                            { "IntValue", 42 },
                            { "BigIntValue", 9876543210L },
                            { "DecimalValue", 1234.5678m },
                            { "FloatValue", 3.14159 },
                            { "BooleanValue", true },
                            { "DateValue", dateStr },
                            { "DateTimeValue", dateTimeStr },
                            { "UniqueValue", testGuid },
                            { "BinaryValue", binaryData }
                        },
                        Geometry = "POLYGON((2600520 1150191, 2715764 1186190, 2511651 1138667, 2600520 1150191))",
                        Epsg = 0
                    }
                };

                var transactionXml = CreateTransactionRequest(features, "SpatialFeature");
                Assert.IsNotNull(transactionXml, "Transaction XML should be created from features");
                Assert.IsTrue(transactionXml.Contains("<Name>Parcel 3"), "Transaction XML should contain feature data");

                var mapConfig = CreateMapConfigForTesting();
                var config = CreateTestConfig();
                var result = WfsService.TransactionCore(transactionXml, mapConfig, config, "TestProject", null);

                Assert.IsNotNull(result, "Transaction result should not be null");
                Assert.IsInstanceOfType(result, typeof(ContentResult), "Result should be ContentResult");

                var contentResult = result as ContentResult;
                Assert.AreEqual(200, contentResult.StatusCode, "Result should have 200 status code");
                Assert.IsTrue(contentResult.Content.Contains("TransactionResponse"), "Result should contain TransactionResponse");
                Assert.IsTrue(VerifyRecordExists("Parcel 3"), "Comprehensive mixed test record should exist.");

                // UPDATE: Modify the inserted record
                var featureId = QueryFeatureIdByName("Parcel 3");
                Assert.IsTrue(featureId > 0, "Should successfully query inserted feature ID");

                var newDateStr = "2025-12-25";
                var newDateTimeStr = "2025-12-25 18:00:00";
                var updateProperties = new Dictionary<string, object>()
                {
                    { "Description", "UPDATED: Comprehensive test with all data types" },
                    { "Category", "UpdatedMixed" },
                    { "IntValue", 999 },
                    { "BooleanValue", false },
                    { "DateValue", newDateStr },
                    { "DateTimeValue", newDateTimeStr }
                };
                var updateXml = CreateUpdateTransactionRequest(featureId, updateProperties, "SpatialFeature");
                Assert.IsNotNull(updateXml, "Update transaction XML should be created");

                var updateResult = WfsService.TransactionCore(updateXml, mapConfig, config, "TestProject", null);
                Assert.IsNotNull(updateResult, "Update result should not be null");
                Assert.IsInstanceOfType(updateResult, typeof(ContentResult), "Update result should be ContentResult");

                var updateContentResult = updateResult as ContentResult;
                Assert.AreEqual(200, updateContentResult.StatusCode, "Update should have 200 status code");
                Assert.IsTrue(updateContentResult.Content.Contains("totalUpdated"), "Update result should indicate updated features");

                // DELETE: Remove the updated record
                var deleteXml = CreateDeleteTransactionRequest(featureId, "SpatialFeature");
                Assert.IsNotNull(deleteXml, "Delete transaction XML should be created");

                var deleteResult = WfsService.TransactionCore(deleteXml, mapConfig, config, "TestProject", null);
                Assert.IsNotNull(deleteResult, "Delete result should not be null");
                Assert.IsInstanceOfType(deleteResult, typeof(ContentResult), "Delete result should be ContentResult");

                var deleteContentResult = deleteResult as ContentResult;
                Assert.AreEqual(200, deleteContentResult.StatusCode, "Delete should have 200 status code");
                Assert.IsTrue(deleteContentResult.Content.Contains("totalDeleted"), "Delete result should indicate deleted features");
            }
            catch (Exception ex)
            {
                Assert.Fail($"Test failed with exception: {ex.Message}");
            }
        }

        #endregion
    }
}
