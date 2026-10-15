using BMapr.GDAL.WebApi.Models;
using BMapr.GDAL.WebApi.Models.Spatial;
using BMapr.GDAL.WebApi.Services;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Xml;

namespace BMapr.GDAL.WebApi.Test
{
    [TestClass]
    public class FeatureServiceMsSqlTests
    {
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

        // Set to true to commit transactions, false to rollback
        private static bool CommitTransactions = true;

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
       
        private const string ConnectionString = "Data Source=BAD-ZBOOK;Initial Catalog=GisData;Persist Security Info=True;User ID=sa;Password=sa;Pooling=False;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=True;Command Timeout=0";


        [TestMethod]
        public void GetFields()
        {
            if (!IsConnectionAvailable())
            {
                Assert.Inconclusive("SQL Server instance is not available. Test skipped.");
            }

            var featureService = new FeatureServiceMsSql(ConnectionString, "dbo.SpatialFeature");
            Assert.IsTrue(featureService.Fields.Count == 17);
            featureService.Dispose();
        }

        [TestMethod]
        public void CreateFeatureSqlServer()
        {
            if (!IsConnectionAvailable())
            {
                Assert.Inconclusive("SQL Server instance is not available. Test skipped.");
            }

            using (var connection = new SqlConnection(ConnectionString))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        var testGuid1 = Guid.NewGuid();
                        var testGuid2 = Guid.NewGuid();

                        var featureList = new FeatureList()
                        {
                            IdFieldName = "Id",
                            Bodies = new List<FeatureBody>()
                            {
                                new FeatureBody()
                                {
                                    Properties = new Dictionary<string, object>(){{ "VarcharMax", $"New** {testGuid1}" } },
                                    Geometry = "POLYGON((2600520 1150191, 2715764 1186190,2511651 1138667,2600520 1150191))",
                                    Epsg = 0
                                },
                                new FeatureBody()
                                {
                                    Properties = new Dictionary<string, object>(){{ "VarcharMax", $"New**** {testGuid2}" } },
                                    Geometry = "POLYGON((2511651 1138667,2600520 1150191, 2626895 1174666,2511651 1138667))",
                                    Epsg = 0
                                }
                            }
                        };

                        // Create a feature service with the existing connection
                        var featureService = new FeatureServiceMsSql(ConnectionString, "dbo.SpatialFeature");

                        // Insert the features
                        int rowsBeforeInsert = GetRowCount(connection, transaction, "dbo.SpatialFeature");
                        featureService.Insert(featureList);
                        int rowsAfterInsert = GetRowCount(connection, transaction, "dbo.SpatialFeature");

                        // Validate that data was inserted
                        Assert.IsTrue(rowsAfterInsert > rowsBeforeInsert, "Data should be inserted into the table.");
                        Assert.AreEqual(2, rowsAfterInsert - rowsBeforeInsert, "Exactly 2 rows should be inserted.");

                        // Validate the inserted values
                        ValidateInsertedData(connection, transaction, testGuid1, testGuid2);

                        featureService.Dispose();
                    }
                    finally
                    {
                        // Rollback the transaction to undo all changes
                        transaction.Rollback();
                    }
                }
            }
        }

        [TestMethod]
        public void InsertWithAllDataTypes_StringVariations()
        {
            if (!IsConnectionAvailable())
            {
                Assert.Inconclusive("SQL Server instance is not available. Test skipped.");
            }

            try
            {
                var testLogger = new TestLogger();

                var featureList = new FeatureList()
                {
                    IdFieldName = "Id",
                    Bodies = new List<FeatureBody>()
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
                                { "Description", "Special chars test: @#$%^&*()[]{}|;:,.<>?" },
                                { "Varchar10", "ABC" },
                                { "Varchar50", "Short" },
                                { "VarcharMax", "Extra long string with many words repeated multiple times to test the varchar max column storage and retrieval capabilities." },
                                { "Category", "Special" }
                            },
                            Geometry = "POLYGON((2511651 1138667, 2600520 1150191, 2626895 1174666, 2511651 1138667))",
                            Epsg = 0
                        },
                        new FeatureBody()
                        {
                            Properties = new Dictionary<string, object>()
                            {
                                { "Name", "StringTest3" },
                                { "Description", "Unicode test: éàü ñ ß" },
                                { "Varchar10", "Test" },
                                { "Varchar50", "Unicode characters test" },
                                { "VarcharMax", "Mixed content with numbers 123456 and symbols !@#$%" },
                                { "Category", "Unicode" }
                            },
                            Geometry = "POLYGON((2626895 1174666, 2715764 1186190, 2600520 1150191, 2626895 1174666))",
                            Epsg = 0
                        }
                    }
                };

                var featureService = new FeatureServiceMsSql(ConnectionString, "dbo.SpatialFeature", logger: testLogger);
                featureService.Insert(featureList);
                featureService.Dispose();

                if (testLogger.ErrorMessages.Any())
                {
                    Assert.Fail($"Insert failed with error: {string.Join("; ", testLogger.ErrorMessages)}");
                }

                // Verify record was inserted
                Assert.IsTrue(VerifyRecordExists(ConnectionString, "StringTest"), "String test record should exist in database.");
            }
            catch (Exception ex)
            {
                Assert.Fail($"Test failed with exception: {ex.Message}");
            }
        }

        [TestMethod]
        public void InsertWithAllDataTypes_IntegerVariations()
        {
            if (!IsConnectionAvailable())
            {
                Assert.Inconclusive("SQL Server instance is not available. Test skipped.");
            }

            try
            {
                var testLogger = new TestLogger();

                var featureList = new FeatureList()
                {
                    IdFieldName = "Id",
                    Bodies = new List<FeatureBody>()
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
                    }
                };

                var featureService = new FeatureServiceMsSql(ConnectionString, "dbo.SpatialFeature", logger: testLogger);
                featureService.Insert(featureList);
                featureService.Dispose();

                if (testLogger.ErrorMessages.Any())
                {
                    Assert.Fail($"Insert failed with error: {string.Join("; ", testLogger.ErrorMessages)}");
                }

                Assert.IsTrue(VerifyRecordExists(ConnectionString, "Parcel 1"), "Integer test records should exist.");
            }
            catch (Exception ex)
            {
                Assert.Fail($"Test failed with exception: {ex.Message}");
            }
        }

        [TestMethod]
        public void InsertWithAllDataTypes_DecimalAndFloat()
        {
            if (!IsConnectionAvailable())
            {
                Assert.Inconclusive("SQL Server instance is not available. Test skipped.");
            }

            try
            {
                var testLogger = new TestLogger();

                var featureList = new FeatureList()
                {
                    IdFieldName = "Id",
                    Bodies = new List<FeatureBody>()
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
                    }
                };

                var featureService = new FeatureServiceMsSql(ConnectionString, "dbo.SpatialFeature", logger: testLogger);
                featureService.Insert(featureList);
                featureService.Dispose();

                if (testLogger.ErrorMessages.Any())
                {
                    Assert.Fail($"Insert failed with error: {string.Join("; ", testLogger.ErrorMessages)}");
                }

                Assert.IsTrue(VerifyRecordExists(ConnectionString, "DecimalPositiveTest"), "Decimal test records should exist.");
            }
            catch (Exception ex)
            {
                Assert.Fail($"Test failed with exception: {ex.Message}");
            }
        }

        [TestMethod]
        public void InsertWithAllDataTypes_BooleanValues()
        {
            if (!IsConnectionAvailable())
            {
                Assert.Inconclusive("SQL Server instance is not available. Test skipped.");
            }

            try
            {
                var testLogger = new TestLogger();

                var featureList = new FeatureList()
                {
                    IdFieldName = "Id",
                    Bodies = new List<FeatureBody>()
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
                    }
                };

                var featureService = new FeatureServiceMsSql(ConnectionString, "dbo.SpatialFeature", logger: testLogger);
                featureService.Insert(featureList);
                featureService.Dispose();

                if (testLogger.ErrorMessages.Any())
                {
                    Assert.Fail($"Insert failed with error: {string.Join("; ", testLogger.ErrorMessages)}");
                }

                Assert.IsTrue(VerifyRecordExists(ConnectionString, "BooleanTrueTest"), "Boolean test records should exist.");
            }
            catch (Exception ex)
            {
                Assert.Fail($"Test failed with exception: {ex.Message}");
            }
        }

        [TestMethod]
        public void InsertWithAllDataTypes_DateTimeVariations()
        {
            if (!IsConnectionAvailable())
            {
                Assert.Inconclusive("SQL Server instance is not available. Test skipped.");
            }

            try
            {
                var testLogger = new TestLogger();
                // Use date format that SQL Server will understand
                var dateStr1 = "2024-03-15";
                var dateStr2 = "2000-01-01";
                var dateStr3 = "2099-12-31";
                var dateTimeStr1 = "2024-03-15 14:30:45";
                var dateTimeStr2 = "2000-01-01 00:00:00";
                var dateTimeStr3 = "2099-12-31 23:59:59";

                var featureList = new FeatureList()
                {
                    IdFieldName = "Id",
                    Bodies = new List<FeatureBody>()
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
                    }
                };

                var featureService = new FeatureServiceMsSql(ConnectionString, "dbo.SpatialFeature", logger: testLogger);
                featureService.Insert(featureList);
                featureService.Dispose();

                if (testLogger.ErrorMessages.Any())
                {
                    Assert.Fail($"Insert failed with error: {string.Join("; ", testLogger.ErrorMessages)}");
                }

                Assert.IsTrue(VerifyRecordExists(ConnectionString, "Parcel 2"), "DateTime test records should exist.");
            }
            catch (Exception ex)
            {
                Assert.Fail($"Test failed with exception: {ex.Message}");
            }
        }

        [TestMethod]
        public void InsertWithAllDataTypes_GuidAndBinary()
        {
            if (!IsConnectionAvailable())
            {
                Assert.Inconclusive("SQL Server instance is not available. Test skipped.");
            }

            try
            {
                var testLogger = new TestLogger();
                var guid1 = Guid.NewGuid();
                var guid2 = Guid.NewGuid();
                var guid3 = Guid.Empty;

                var binaryData1 = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 };
                var binaryData2 = new byte[] { 0xFF, 0xFE, 0xFD, 0xFC };
                var binaryData3 = Enumerable.Range(0, 100).Select(i => (byte)(i % 256)).ToArray();

                var featureList = new FeatureList()
                {
                    IdFieldName = "Id",
                    Bodies = new List<FeatureBody>()
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
                    }
                };

                var featureService = new FeatureServiceMsSql(ConnectionString, "dbo.SpatialFeature", logger: testLogger);
                featureService.Insert(featureList);
                featureService.Dispose();

                if (testLogger.ErrorMessages.Any())
                {
                    Assert.Fail($"Insert failed with error: {string.Join("; ", testLogger.ErrorMessages)}");
                }

                Assert.IsTrue(VerifyRecordExists(ConnectionString, "GuidBinaryTest1"), "Guid/Binary test records should exist.");
            }
            catch (Exception ex)
            {
                Assert.Fail($"Test failed with exception: {ex.Message}");
            }
        }

        [TestMethod]
        public void InsertWithAllDataTypes_NullValues()
        {
            if (!IsConnectionAvailable())
            {
                Assert.Inconclusive("SQL Server instance is not available. Test skipped.");
            }

            try
            {
                var testLogger = new TestLogger();

                var featureList = new FeatureList()
                {
                    IdFieldName = "Id",
                    Bodies = new List<FeatureBody>()
                    {
                        new FeatureBody()
                        {
                            Properties = new Dictionary<string, object>()
                            {
                                { "Name", "NullValuesTest" }
                            },
                            Geometry = "POLYGON((2600520 1150191, 2715764 1186190, 2511651 1138667, 2600520 1150191))",
                            Epsg = 0
                        },
                        new FeatureBody()
                        {
                            Properties = new Dictionary<string, object>()
                            {
                                { "Name", null },
                                { "Description", null },
                                { "IntValue", null },
                                { "BigIntValue", null },
                                { "DecimalValue", null },
                                { "FloatValue", null },
                                { "BooleanValue", null },
                                { "DateValue", null },
                                { "DateTimeValue", null },
                                { "UniqueValue", null },
                                { "BinaryValue", null }
                            },
                            Geometry = "POLYGON((2511651 1138667, 2600520 1150191, 2626895 1174666, 2511651 1138667))",
                            Epsg = 0
                        }
                    }
                };

                var featureService = new FeatureServiceMsSql(ConnectionString, "dbo.SpatialFeature", logger: testLogger);
                featureService.Insert(featureList);
                featureService.Dispose();

                if (testLogger.ErrorMessages.Any())
                {
                    Assert.Fail($"Insert failed with error: {string.Join("; ", testLogger.ErrorMessages)}");
                }

                Assert.IsTrue(VerifyRecordExists(ConnectionString, "NullValuesTest"), "Null test records should exist.");
            }
            catch (Exception ex)
            {
                Assert.Fail($"Test failed with exception: {ex.Message}");
            }
        }

        [TestMethod]
        public void InsertWithAllDataTypes_ComprehensiveMixed()
        {
            if (!IsConnectionAvailable())
            {
                Assert.Inconclusive("SQL Server instance is not available. Test skipped.");
            }

            try
            {
                var testLogger = new TestLogger();
                var testGuid = Guid.NewGuid();
                var dateStr = "2024-06-15";
                var dateTimeStr = "2024-06-15 10:30:45";
                var binaryData = new byte[] { 0x48, 0x65, 0x6C, 0x6C, 0x6F };

                var featureList = new FeatureList()
                {
                    IdFieldName = "Id",
                    Bodies = new List<FeatureBody>()
                    {
                        new FeatureBody()
                        {
                            Properties = new Dictionary<string, object>()
                            {
                                { "Name", "Parcel 3" },
                                { "Description", "Comprehensive test with all data types" },
                                { "Varchar10", "TestABC" },
                                { "Varchar50", "Medium length test string" },
                                { "VarcharMax", "This is a very long string value that exceeds typical varchar limits. " },
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
                    }
                };

                var featureService = new FeatureServiceMsSql(ConnectionString, "dbo.SpatialFeature", logger: testLogger);
                featureService.Insert(featureList);
                featureService.Dispose();

                if (testLogger.ErrorMessages.Any())
                {
                    Assert.Fail($"Insert failed with error: {string.Join("; ", testLogger.ErrorMessages)}");
                }

                Assert.IsTrue(VerifyRecordExists(ConnectionString, "Parcel 3"), "Comprehensive test record should exist.");
            }
            catch (Exception ex)
            {
                Assert.Fail($"Test failed with exception: {ex.Message}");
            }
        }

        #region Helper Methods

        private static bool VerifyRecordExists(string connectionString, string namePattern)
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    using (var command = new SqlCommand(
                        "SELECT COUNT(*) FROM dbo.SpatialFeature WHERE Name LIKE @Pattern",
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

        private static int GetRowCount(SqlConnection connection, SqlTransaction transaction, string tableName)
        {
            using (var command = new SqlCommand($"SELECT COUNT(*) FROM {tableName}", connection, transaction))
            {
                var result = command.ExecuteScalar();
                return result != null ? Convert.ToInt32(result) : 0;
            }
        }

        private static void ValidateInsertedData(SqlConnection connection, SqlTransaction transaction, Guid testGuid1, Guid testGuid2)
        {
            // First, get the actual column names from the table
            List<string> columns = GetTableColumns(connection, transaction, "dbo.SpatialFeature");

            // Build query with available columns
            string selectColumns = string.Join(", ", columns.Where(c => 
                c.Equals("VarcharMax", StringComparison.OrdinalIgnoreCase) || 
                c.Equals("Geometry", StringComparison.OrdinalIgnoreCase) ||
                c.Equals("Shape", StringComparison.OrdinalIgnoreCase) ||
                c.Equals("Epsg", StringComparison.OrdinalIgnoreCase) ||
                c.Equals("SRID", StringComparison.OrdinalIgnoreCase) ||
                c.Equals("Id", StringComparison.OrdinalIgnoreCase)));

            using (var command = new SqlCommand($@"
                SELECT TOP 2 {selectColumns}
                FROM dbo.SpatialFeature 
                WHERE VarcharMax LIKE '%' + @Guid1 + '%' OR VarcharMax LIKE '%' + @Guid2 + '%'
                ORDER BY Id DESC", connection, transaction))
            {
                command.Parameters.AddWithValue("@Guid1", testGuid1.ToString());
                command.Parameters.AddWithValue("@Guid2", testGuid2.ToString());

                using (var reader = command.ExecuteReader())
                {
                    // Read the second inserted row (most recent)
                    Assert.IsTrue(reader.Read(), "First inserted row should exist.");
                    var varcharMax2 = reader["VarcharMax"].ToString();

                    Assert.IsTrue(varcharMax2?.Contains(testGuid2.ToString()), "Second row should contain the second GUID.");

                    // Read the first inserted row
                    Assert.IsTrue(reader.Read(), "Second inserted row should exist.");
                    var varcharMax1 = reader["VarcharMax"].ToString();

                    Assert.IsTrue(varcharMax1?.Contains(testGuid1.ToString()), "First row should contain the first GUID.");
                }
            }
        }

        private static List<string> GetTableColumns(SqlConnection connection, SqlTransaction transaction, string tableName)
        {
            var columns = new List<string>();
            using (var command = new SqlCommand($@"
                SELECT COLUMN_NAME 
                FROM INFORMATION_SCHEMA.COLUMNS 
                WHERE TABLE_NAME = @TableName 
                ORDER BY ORDINAL_POSITION", connection, transaction))
            {
                command.Parameters.AddWithValue("@TableName", tableName.Split('.').Last());
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        columns.Add(reader["COLUMN_NAME"].ToString() ?? "");
                    }
                }
            }
            return columns;
        }

        #endregion
    }
}
