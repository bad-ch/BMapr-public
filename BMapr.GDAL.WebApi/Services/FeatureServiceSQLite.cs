using BMapr.GDAL.WebApi.Models.Db;
using BMapr.GDAL.WebApi.Models.Spatial;
using OSGeo.OGR;
using System.Data;
using System.Data.SQLite;
using System.Text;

namespace BMapr.GDAL.WebApi.Services
{
    /// <summary>
    /// Feature service for SQLite databases using the GeoPackage format.
    /// Geometry columns are stored as GeoPackage geometry blobs (WKB with GPKG header).
    /// </summary>
    public class FeatureServiceSQLite : IDisposable
    {
        private readonly ILogger<FeatureServiceSQLite>? _logger;

        // GeoPackage geometry column provider type sentinel (used analogously to MsSql ProviderType 29)
        private const int GpkgGeometryProviderType = 29;

        public SQLiteConnection Connection { get; set; }
        public string TableName { get; set; }
        public List<DbField> Fields { get; set; }
        public StringBuilder SqlLog { get; set; }
        public bool SqlLogEnabled { get; set; }
        public string IdType { get; set; }

        public FeatureServiceSQLite(string connectionString, string tableName, bool sqlLog = false, string idType = "Incr", ILogger<FeatureServiceSQLite>? logger = null)
        {
            _logger = logger;

            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentException("Connection string must not be empty.", nameof(connectionString));
            if (string.IsNullOrWhiteSpace(tableName))
                throw new ArgumentException("Table name must not be empty.", nameof(tableName));

            Connection = new SQLiteConnection(connectionString);

            try
            {
                Connection.Open();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to open SQLite connection.");
                throw;
            }

            SqlLog = new StringBuilder();
            SqlLogEnabled = sqlLog;
            TableName = tableName;
            IdType = idType;

            try
            {
                Fields = GetFields();
                _logger?.LogDebug("Loaded {FieldCount} fields for table {Table}.", Fields.Count, TableName);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to read schema for table {Table}.", TableName);
                throw;
            }
        }

        public void Dispose()
        {
            try
            {
                if (Connection.State != ConnectionState.Closed)
                    Connection.Close();
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Error while closing connection.");
            }
            finally
            {
                Connection.Dispose();
            }
        }

        public void Insert(FeatureList featureList)
        {
            LogSql(SqlLogEnabled, "-- INSERT");

            using var transaction = Connection.BeginTransaction();

            try
            {
                foreach (var featureBody in featureList.Bodies)
                {
                    var values = GetValues(featureBody);

                    if (!values.Any())
                    {
                        _logger?.LogWarning("Insert, get no values");
                        continue;
                    }

                    var nonAutoIncrFields = values.Where(x => x.IsAutoIncrement == null || !((bool)x.IsAutoIncrement)).ToList();

                    string columns, paramNames, sql;

                    if (IdType == "AutoGuid" || IdType == "AutoInt" || IdType == "AutoBigInt")
                    {
                        var filtered = nonAutoIncrFields.Where(x => x.Name != featureList.IdFieldName).ToList();
                        columns = string.Join(',', filtered.Select(x => x.Name));
                        paramNames = string.Join(',', filtered.Select(x => $"@{x.Name}"));
                        sql = $"INSERT INTO {TableName} ({columns}) VALUES({paramNames});";
                    }
                    else
                    {
                        columns = string.Join(',', nonAutoIncrFields.Select(x => x.Name));
                        paramNames = string.Join(',', nonAutoIncrFields.Select(x => $"@{x.Name}"));
                        sql = $"INSERT INTO {TableName} ({columns}) VALUES({paramNames});";
                    }

                    LogSql(SqlLogEnabled, sql);

                    using var cmd = new SQLiteCommand(sql, Connection, transaction);
                    AddParameters(cmd, values, featureList.IdFieldName, IdType);
                    cmd.ExecuteNonQuery();

                    if (IdType == "Incr" || IdType == "AutoInt" || IdType == "AutoBigInt")
                    {
                        featureBody.Id = Connection.LastInsertRowId;
                    }
                    else if (IdType == "AutoGuid")
                    {
                        // GUID was generated client-side and stored; retrieve it from the last inserted row via rowid
                        var guidField = featureList.IdFieldName;
                        using var selectCmd = new SQLiteCommand($"SELECT {guidField} FROM {TableName} WHERE rowid = last_insert_rowid();", Connection, transaction);
                        var result = selectCmd.ExecuteScalar();
                        featureBody.Id = result?.ToString() ?? string.Empty;
                    }
                }

                transaction.Commit();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Insert failed.");
                transaction.Rollback();
            }
        }

        public void Update(FeatureList featureList)
        {
            LogSql(SqlLogEnabled, "-- UPDATE");

            using var transaction = Connection.BeginTransaction();

            try
            {
                foreach (var featureBody in featureList.Bodies)
                {
                    var values = GetValues(featureBody);

                    if (!values.Any())
                    {
                        _logger?.LogWarning("Update, get no values");
                        continue;
                    }

                    var valuesToSet = values.Where(x => x.IsAutoIncrement == null || !((bool)x.IsAutoIncrement)).ToList();
                    var setClause = string.Join(',', valuesToSet.Select(x => $"{x.Name}=@{x.Name}"));

                    string sql;

                    if (IdType == "Number" || IdType == "Incr")
                    {
                        sql = $"UPDATE {TableName} SET {setClause} WHERE {featureList.IdFieldName}=@__id;";
                    }
                    else // String or Guid
                    {
                        sql = $"UPDATE {TableName} SET {setClause} WHERE {featureList.IdFieldName}=@__id;";
                    }

                    LogSql(SqlLogEnabled, sql);

                    using var cmd = new SQLiteCommand(sql, Connection, transaction);
                    AddParameters(cmd, valuesToSet, featureList.IdFieldName, IdType);
                    cmd.Parameters.AddWithValue("@__id", featureBody.Id?.ToString());
                    cmd.ExecuteNonQuery();
                }

                transaction.Commit();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Update failed.");
                transaction.Rollback();
            }
        }

        public void Delete(FeatureList featureList)
        {
            LogSql(SqlLogEnabled, "-- DELETE");

            using var transaction = Connection.BeginTransaction();

            try
            {
                string idsToDelete;

                if (IdType == "Number" || IdType == "Incr")
                {
                    idsToDelete = string.Join(',', featureList.Bodies.Select(x => x.Id));
                }
                else // String or Guid
                {
                    idsToDelete = string.Join(',', featureList.Bodies.Select(x => $"'{x.Id}'"));
                }

                var sql = $"DELETE FROM {TableName} WHERE {featureList.IdFieldName} IN ({idsToDelete});";

                LogSql(SqlLogEnabled, sql);

                using var cmd = new SQLiteCommand(sql, Connection, transaction);
                cmd.ExecuteNonQuery();

                transaction.Commit();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Delete failed.");
                transaction.Rollback();
            }
        }

        private List<DbField> GetFields()
        {
            var fields = new List<DbField>();

            using var command = new SQLiteCommand($"SELECT * FROM {TableName} LIMIT 0", Connection);
            using var reader = command.ExecuteReader(CommandBehavior.SchemaOnly);

            var schemaTable = reader.GetSchemaTable();

            if (schemaTable == null)
                return fields;

            // Detect geometry columns from gpkg_geometry_columns if the table exists (GeoPackage)
            var gpkgGeometryColumns = GetGpkgGeometryColumnNames();

            int index = 0;
            foreach (DataRow colRow in schemaTable.Rows)
            {
                var columnName = colRow.Field<string>("ColumnName") ?? string.Empty;
                var dataType = colRow.Field<Type>("DataType");

                var isGeometry = gpkgGeometryColumns.Contains(columnName, StringComparer.OrdinalIgnoreCase);

                var field = new DbField()
                {
                    IsAutoIncrement = colRow.Field<bool?>("IsAutoIncrement") ?? false,
                    Name = columnName,
                    Type = dataType,
                    TypeName = dataType?.ToString(),
                    ProviderType = isGeometry ? GpkgGeometryProviderType : (colRow.IsNull("ProviderType") ? 0 : Convert.ToInt32(colRow["ProviderType"])),
                    MaxLength = colRow.IsNull("ColumnSize") ? null : (int?)Convert.ToInt32(colRow["ColumnSize"]),
                    IsNullable = colRow.Field<bool?>("AllowDBNull") ?? true,
                    Index = index++,
                };

                fields.Add(field);
            }

            return fields;
        }

        private HashSet<string> GetGpkgGeometryColumnNames()
        {
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            try
            {
                using var checkCmd = new SQLiteCommand(
                    "SELECT name FROM sqlite_master WHERE type='table' AND name='gpkg_geometry_columns';",
                    Connection);
                var exists = checkCmd.ExecuteScalar();

                if (exists == null || exists == DBNull.Value)
                    return names;

                using var cmd = new SQLiteCommand(
                    $"SELECT column_name FROM gpkg_geometry_columns WHERE table_name=@tbl;",
                    Connection);
                cmd.Parameters.AddWithValue("@tbl", TableName);

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    names.Add(reader.GetString(0));
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Could not read gpkg_geometry_columns; geometry detection may be incomplete.");
            }

            return names;
        }

        private DbFieldValue GetFieldValue(DbField dbField, string? value)
        {
            return new DbFieldValue()
            {
                IsAutoIncrement = dbField.IsAutoIncrement,
                Name = dbField.Name,
                Type = dbField.Type,
                ProviderType = dbField.ProviderType,
                TypeName = dbField.TypeName,
                MaxLength = dbField.MaxLength,
                IsNullable = dbField.IsNullable,
                Value = value
            };
        }

        private DbFieldValue GetGeometryFieldValue(DbField dbField, byte[] wkb)
        {
            return new DbFieldValue()
            {
                IsAutoIncrement = dbField.IsAutoIncrement,
                Name = dbField.Name,
                Type = dbField.Type,
                ProviderType = dbField.ProviderType,
                TypeName = dbField.TypeName,
                MaxLength = dbField.MaxLength,
                IsNullable = dbField.IsNullable,
                Value = null,
                RawValue = wkb
            };
        }

        private List<DbFieldValue> GetValues(FeatureBody featureBody)
        {
            var properties = new List<DbFieldValue>();

            foreach (var column in featureBody.Properties)
            {
                foreach (var field in Fields)
                {
                    if (!string.Equals(column.Key, field.Name, StringComparison.OrdinalIgnoreCase))
                        continue;

                    var fieldType = field.Type?.ToString() ?? string.Empty;

                    switch (fieldType)
                    {
                        case "System.String":
                        case "System.Char":
                        case "System.Guid":
                            properties.Add(GetFieldValue(field, column.Value?.ToString()));
                            break;
                        case "System.Boolean":
                        case "System.Byte":
                        case "System.SByte":
                        case "System.Decimal":
                        case "System.Double":
                        case "System.Single":
                        case "System.Int32":
                        case "System.UInt32":
                        case "System.IntPtr":
                        case "System.UIntPtr":
                        case "System.Int64":
                        case "System.UInt64":
                        case "System.Int16":
                        case "System.UInt16":
                            properties.Add(GetFieldValue(field, column.Value?.ToString()));
                            break;
                        case "System.DateTime":
                        case "System.DateTimeOffset":
                            properties.Add(GetFieldValue(field, column.Value?.ToString()));
                            break;
                        default:
                            throw new Exception($"Type not found {field.Name}");
                    }
                }
            }

            // Handle geometry column (GeoPackage: store as GPKG geometry blob)
            if (!string.IsNullOrEmpty(featureBody.Geometry) && Fields.Any(x => x.ProviderType == GpkgGeometryProviderType))
            {
                var field = Fields.First(x => x.ProviderType == GpkgGeometryProviderType);
                var wkbBytes = WktToGpkgBlob(featureBody.Geometry, featureBody.Epsg);
                properties.Add(GetGeometryFieldValue(field, wkbBytes));
            }

            return properties;
        }

        /// <summary>
        /// Converts a WKT geometry string into a GeoPackage geometry blob.
        /// The GPKG blob format is: 'GP' magic + version + flags + SRS ID + optional envelope + WKB.
        /// </summary>
        private static byte[] WktToGpkgBlob(string wkt, int epsg)
        {
            GdalConfiguration.ConfigureOgr();

            using var geometry = Ogr.CreateGeometryFromWkt(ref wkt, new OSGeo.OSR.SpatialReference(""));

            byte[] wkb = new byte[geometry.WkbSize()];
            geometry.ExportToWkb(wkb, wkbByteOrder.wkbNDR);

            // Build minimal GPKG geometry blob header (no envelope, little-endian, non-empty)
            // Magic: 0x47 0x50 ('GP')
            // Version: 0x00
            // Flags: 0x01 (little-endian WKB, no envelope, non-empty)
            // SRS ID: 4 bytes little-endian int32
            var header = new byte[8];
            header[0] = 0x47; // 'G'
            header[1] = 0x50; // 'P'
            header[2] = 0x00; // version
            header[3] = 0x01; // flags: wkbByteOrder=little-endian, envelope=none, empty=false
            var srsBytes = BitConverter.GetBytes(epsg);
            if (!BitConverter.IsLittleEndian) Array.Reverse(srsBytes);
            Buffer.BlockCopy(srsBytes, 0, header, 4, 4);

            var blob = new byte[header.Length + wkb.Length];
            Buffer.BlockCopy(header, 0, blob, 0, header.Length);
            Buffer.BlockCopy(wkb, 0, blob, header.Length, wkb.Length);

            return blob;
        }

        private static void AddParameters(SQLiteCommand cmd, List<DbFieldValue> values, string idFieldName, string idType)
        {
            var skipId = idType == "AutoGuid" || idType == "AutoInt" || idType == "AutoBigInt";

            foreach (var v in values)
            {
                if (skipId && string.Equals(v.Name, idFieldName, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (v.IsAutoIncrement == true)
                    continue;

                var paramName = $"@{v.Name}";

                if (v.RawValue is byte[] rawBytes)
                {
                    var param = cmd.Parameters.Add(paramName, DbType.Binary);
                    param.Value = rawBytes;
                }
                else
                {
                    cmd.Parameters.AddWithValue(paramName, (object?)v.Value ?? DBNull.Value);
                }
            }
        }

        private void LogSql(bool logEnabled, string content)
        {
            if (!logEnabled)
                return;

            SqlLog.AppendLine(content);
        }
    }
}
