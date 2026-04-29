using BMapr.GDAL.WebApi.Models.Db;
using BMapr.GDAL.WebApi.Models.Spatial;
using Npgsql;
using System.Data;
using System.Text;

namespace BMapr.GDAL.WebApi.Services
{
    /// <summary>
    /// Feature service for PostgreSQL databases with PostGIS extension.
    /// Geometry columns are stored using PostGIS ST_GeomFromText / ST_SetSRID.
    /// </summary>
    public class FeatureServicePg : IDisposable
    {
        private readonly ILogger<FeatureServicePg>? _logger;

        // PostGIS geometry provider type sentinel (NpgsqlDbType maps geometry to oid 17; we use a custom sentinel for detection)
        private const int PostGisGeometryProviderType = 29;

        public NpgsqlConnection Connection { get; set; }
        public string TableName { get; set; }
        public List<DbField> Fields { get; set; }
        public StringBuilder SqlLog { get; set; }
        public bool SqlLogEnabled { get; set; }
        public string IdType { get; set; }

        public FeatureServicePg(string connectionString, string tableName, bool sqlLog = false, string idType = "Incr", ILogger<FeatureServicePg>? logger = null)
        {
            _logger = logger;

            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentException("Connection string must not be empty.", nameof(connectionString));
            if (string.IsNullOrWhiteSpace(tableName))
                throw new ArgumentException("Table name must not be empty.", nameof(tableName));

            Connection = new NpgsqlConnection(connectionString);

            try
            {
                Connection.Open();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to open PostgreSQL connection.");
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

                    var nonAutoIncr = values.Where(x => x.IsAutoIncrement == null || !((bool)x.IsAutoIncrement)).ToList();

                    string columns, data, sql;

                    if (IdType == "AutoGuid" || IdType == "AutoInt" || IdType == "AutoBigInt")
                    {
                        var filtered = nonAutoIncr.Where(x => x.Name != featureList.IdFieldName).ToList();
                        columns = string.Join(',', filtered.Select(x => x.Name));
                        data = string.Join(',', filtered.Select(x => x.Value));
                        sql = $"INSERT INTO {TableName} ({columns}) VALUES({data}) RETURNING {featureList.IdFieldName};";
                    }
                    else if (IdType == "Incr")
                    {
                        columns = string.Join(',', nonAutoIncr.Select(x => x.Name));
                        data = string.Join(',', nonAutoIncr.Select(x => x.Value));
                        sql = $"INSERT INTO {TableName} ({columns}) VALUES({data}) RETURNING {featureList.IdFieldName};";
                    }
                    else
                    {
                        columns = string.Join(',', nonAutoIncr.Select(x => x.Name));
                        data = string.Join(',', nonAutoIncr.Select(x => x.Value));
                        sql = $"INSERT INTO {TableName} ({columns}) VALUES({data});";
                    }

                    LogSql(SqlLogEnabled, sql);

                    using var cmd = new NpgsqlCommand(sql, Connection, transaction);

                    if (IdType == "Incr" || IdType == "AutoGuid" || IdType == "AutoInt" || IdType == "AutoBigInt")
                    {
                        var result = cmd.ExecuteScalar();
                        featureBody.Id = result?.ToString() ?? string.Empty;
                    }
                    else
                    {
                        cmd.ExecuteNonQuery();
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

                    var valuesToSet = values.Where(x => x.IsAutoIncrement == null || !((bool)x.IsAutoIncrement));
                    var valuesSet = string.Join(',', valuesToSet.Select(x => $"{x.Name}={x.Value}"));
                    string sql;

                    if (IdType == "Number" || IdType == "Incr")
                    {
                        sql = $"UPDATE {TableName} SET {valuesSet} WHERE {featureList.IdFieldName}={featureBody.Id} ;";
                    }
                    else // String or Guid
                    {
                        sql = $"UPDATE {TableName} SET {valuesSet} WHERE {featureList.IdFieldName}='{featureBody.Id}' ;";
                    }

                    LogSql(SqlLogEnabled, sql);

                    using var cmd = new NpgsqlCommand(sql, Connection, transaction);
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

                var sql = $"DELETE FROM {TableName} WHERE {featureList.IdFieldName} IN ({idsToDelete}) ;";

                LogSql(SqlLogEnabled, sql);

                using var cmd = new NpgsqlCommand(sql, Connection, transaction);
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

            // Detect PostGIS geometry columns from information_schema
            var geometryColumns = GetPostGisGeometryColumnNames();

            using var command = new NpgsqlCommand($"SELECT * FROM {TableName} LIMIT 0", Connection);
            using var reader = command.ExecuteReader(CommandBehavior.SchemaOnly);

            var schemaTable = reader.GetSchemaTable();

            if (schemaTable == null)
                return fields;

            foreach (DataRow colRow in schemaTable.Rows)
            {
                var columnName = colRow.Field<string>("ColumnName") ?? string.Empty;
                var dataType = colRow.Field<Type>("DataType");

                var isGeometry = geometryColumns.Contains(columnName, StringComparer.OrdinalIgnoreCase);

                var field = new DbField()
                {
                    IsAutoIncrement = colRow.Field<bool?>("IsAutoIncrement") ?? false,
                    Name = columnName,
                    Type = dataType,
                    TypeName = dataType?.ToString(),
                    ProviderType = isGeometry ? PostGisGeometryProviderType : (colRow.IsNull("ProviderType") ? 0 : Convert.ToInt32(colRow["ProviderType"])),
                    MaxLength = colRow.IsNull("ColumnSize") ? null : (int?)Convert.ToInt32(colRow["ColumnSize"]),
                    IsNullable = colRow.Field<bool?>("AllowDBNull") ?? true,
                };

                fields.Add(field);
            }

            return fields;
        }

        private HashSet<string> GetPostGisGeometryColumnNames()
        {
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            try
            {
                // geometry_columns is a view provided by PostGIS
                var sql = @"
                    SELECT f_geometry_column
                    FROM geometry_columns
                    WHERE f_table_name = @tbl";

                using var cmd = new NpgsqlCommand(sql, Connection);
                cmd.Parameters.AddWithValue("@tbl", TableName.Trim('"').ToLowerInvariant());

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    names.Add(reader.GetString(0));
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Could not read geometry_columns; geometry detection may be incomplete.");
            }

            return names;
        }

        private DbFieldValue GetFieldValue(DbField dbField, string value)
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
                            properties.Add(GetFieldValue(field, column.Value == null ? "NULL" : $"'{column.Value}'"));
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
                            properties.Add(GetFieldValue(field, column.Value == null ? "NULL" : $"{column.Value}"));
                            break;
                        case "System.DateTime":
                            properties.Add(GetFieldValue(field, column.Value == null ? "NULL" : $"'{column.Value}'")); // todo check
                            break;
                        case "System.DateTimeOffset":
                            properties.Add(GetFieldValue(field, column.Value == null ? "NULL" : $"'{column.Value}'")); // todo check
                            break;
                        default:
                            throw new Exception($"Type not found {field.Name}");
                    }
                }
            }

            // PostGIS geometry: use ST_GeomFromText with SRID
            if (!string.IsNullOrEmpty(featureBody.Geometry) && Fields.Any(x => x.ProviderType == PostGisGeometryProviderType))
            {
                var field = Fields.First(x => x.ProviderType == PostGisGeometryProviderType);
                properties.Add(GetFieldValue(field, $"ST_GeomFromText('{featureBody.Geometry}', {featureBody.Epsg})"));
            }

            return properties;
        }

        private void LogSql(bool logEnabled, string content)
        {
            if (!logEnabled)
                return;

            SqlLog.AppendLine(content);
        }
    }
}
