using System.Xml;
using BMapr.GDAL.WebApi.Models;
using BMapr.GDAL.WebApi.Models.Spatial;
using BMapr.GDAL.WebApi.Services;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;

namespace BMapr.GDAL.WebApi.Test
{
    [TestClass]
    public class FeatureServiceMsSqlTests
    {
        private const string ConnectionString = "Data Source=BAD-ZBOOK;Initial Catalog=GisData;Persist Security Info=True;User ID=sa;Password=sa;Pooling=False;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=True;Command Timeout=0";

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

    }
}
