using BMapr.GDAL.WebApi.Models.Spatial;
using System.Data;
using System.Data.SqlClient;
using BMapr.GDAL.WebApi.Models.Db;
using System.Text;

namespace BMapr.GDAL.WebApi.Services
{
    public class FeatureServiceMsSql
    {
        public SqlConnection Connection { get; set; }
        public string TableName { get; set; }
        public List<DbField> Fields { get; set; }
        public StringBuilder SqlLog { get; set; }
        public bool SqlLogEnabled { get; set; }
        public bool UseGeographyType { get; set; }
        public string IdType { get; set; }

        public FeatureServiceMsSql(string connection, string tableName, bool useGeographyType = false,bool sqlLog = false, string idType = "Incr")
        {
            Connection = new SqlConnection(connection);
            Connection.Open();
            TableName = tableName;
            Fields = GetFields();
            SqlLog = new StringBuilder();
            SqlLogEnabled = sqlLog;
            UseGeographyType = useGeographyType;
            IdType = idType;
        }

        public void Dispose()
        {
            Connection.Close();
            Connection.Dispose();
        }

        private string GetDataTypeFromIdType(string idType)
        {
            switch (idType)
            {
                case "AutoGuid":
                    return "uniqueidentifier";
                case "AutoInt":
                    return "int";
                case "AutoBigInt":
                    return "bigint";
            }

            throw new Exception($"Id type <{idType}> not supported");
        }

        public void Insert(FeatureList featureList)
        {
            LogSql(SqlLogEnabled, "");
            LogSql(SqlLogEnabled, "-- INSERT");
            LogSql(SqlLogEnabled, "");

            var transaction = Connection.BeginTransaction("BeginTransaction");

            foreach (var featureBody in featureList.Bodies)
            {
                var values = GetValues(featureBody);

                if (!values.Any())
                {
                    //todo log
                    continue;
                }

                var colums = string.Join(',', values.Where(x => x.IsAutoIncrement == null || !((bool)x.IsAutoIncrement)).Select(x => x.Name));
                var data = string.Join(',', values.Where(x=> x.IsAutoIncrement == null || !((bool)x.IsAutoIncrement)).Select(x => x.Value));
                var sql = $"INSERT INTO {TableName} ({colums}) VALUES({data});{(IdType == "Incr" ? "SELECT @NEWID = SCOPE_IDENTITY();" : "")}";

                if (IdType== "AutoGuid" || IdType == "AutoInt" || IdType == "AutoBigInt")
                {
                    colums = string.Join(',', values.Where(x => x.Name != featureList.IdFieldName).Select(x => x.Name));
                    data = string.Join(',', values.Where(x => x.Name != featureList.IdFieldName).Select(x => x.Value));

                    sql = $"DECLARE @MyTableVar table([{featureList.IdFieldName}] [{GetDataTypeFromIdType(IdType)}]);INSERT INTO {TableName} ({colums}) OUTPUT INSERTED.[{featureList.IdFieldName}] INTO @MyTableVar VALUES({data});SELECT [{featureList.IdFieldName}] FROM @MyTableVar;";
                }

                LogSql(SqlLogEnabled, sql);
                //File.WriteAllText(Path.Combine(Path.GetTempPath(), $"SQL INSERT {Guid.NewGuid()}"), sql);

                if (IdType == "Incr")
                {
                    using (var cmd = new SqlCommand(sql, Connection, transaction))
                    {
                        cmd.Parameters.Add("@NEWID", SqlDbType.Int).Direction = ParameterDirection.Output;
                        cmd.ExecuteScalar();

                        var newId = cmd.Parameters["@NEWID"];
                        featureBody.Id = Convert.ToInt64(newId.Value);
                    }
                }
                else if (IdType == "AutoGuid" || IdType == "AutoInt" || IdType == "AutoBigInt")
                {
                    using (var cmd = new SqlCommand(sql, Connection, transaction))
                    {
                        var result = cmd.ExecuteScalar();
                        featureBody.Id = result?.ToString() ?? string.Empty;
                    }
                }
                else
                {
                    using (var cmd = new SqlCommand(sql, Connection, transaction))
                    {
                        cmd.ExecuteScalar();
                    }
                }
            }

            transaction.Commit();
        }

        public void Update(FeatureList featureList)
        {
            LogSql(SqlLogEnabled, "");
            LogSql(SqlLogEnabled, "-- UPDATE");
            LogSql(SqlLogEnabled, "");

            var transaction = Connection.BeginTransaction("BeginTransaction");

            foreach (var featureBody in featureList.Bodies)
            {
                var values = GetValues(featureBody);

                if (!values.Any())
                {
                    //todo log
                    continue;
                }

                var valuesToSet = values.Where(x => x.IsAutoIncrement == null || !((bool) x.IsAutoIncrement));
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
                //File.WriteAllText(Path.Combine(Path.GetTempPath(),$"SQL UPDATE {Guid.NewGuid()}"), sql);

                using (var cmd = new SqlCommand(sql, Connection, transaction))
                {
                    cmd.ExecuteNonQuery();
                }
            }

            transaction.Commit();
        }

        public void Delete(FeatureList featureList)
        {
            LogSql(SqlLogEnabled, "");
            LogSql(SqlLogEnabled, "-- DELETE");
            LogSql(SqlLogEnabled, "");

            var transaction = Connection.BeginTransaction("BeginTransaction");
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
            //File.WriteAllText(Path.Combine(Path.GetTempPath(), $"SQL DELETE {Guid.NewGuid()}"), sql);

            using (var cmd = new SqlCommand(sql, Connection, transaction))
            {
                cmd.ExecuteNonQuery();
            }

            transaction.Commit();
        }

        private List<DbField> GetFields()
        {
            var fields = new List<DbField>();

            using (SqlCommand command = new SqlCommand(@"SELECT * FROM " + TableName, Connection))
            {
                using (var reader = command.ExecuteReader(CommandBehavior.SchemaOnly))
                {
                    DataTable schemaTable = reader.GetSchemaTable();

                    foreach (DataRow colRow in schemaTable.Rows)
                    {
                        var field = new DbField()
                        {
                            IsAutoIncrement = colRow.Field<bool>("IsAutoIncrement"),
                            Name = colRow.Field<String>("ColumnName") ?? "",
                            Type = colRow.Field<System.Type>("DataType"),
                            TypeName = (colRow.Field<System.Type>("DataType"))?.ToString(),
                            ProviderType = colRow.Field<Int32>("ProviderType"),
                            MaxLength = colRow.Field<Int32>("ColumnSize"),
                            IsNullable = colRow.Field<bool>("AllowDBNull"),
                        };

                        fields.Add(field);
                    }
                }
            }

            return fields;
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
                Value =  value
            };
        }
        private List<DbFieldValue> GetValues(FeatureBody featureBody)
        {
            var properties = new List<DbFieldValue>();

            foreach (var column in featureBody.Properties)
            {
                foreach (var field in Fields)
                {
                    if (column.Key != field.Name)
                    {
                        continue;
                    }

                    var fieldType = field.Type.ToString();

                    switch (fieldType)
                    {
                        case "System.String":
                        case "System.Char":
                        case "System.Guid":
                            properties.Add(GetFieldValue(field, $"'{column.Value}'"));
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
                            properties.Add(GetFieldValue(field, $"{column.Value}"));
                            break;
                        case "System.DateTime":
                            properties.Add(GetFieldValue(field, $"'{column.Value}'")); // todo check
                            break;
                        case "System.DateTimeOffset":
                            properties.Add(GetFieldValue(field, $"CAST('{column.Value}' AS DATETIMEOFFSET)"));
                            break;
                        default:
                            throw new Exception($"Type not found {field.Name}");
                    }
                }
            }

            if (!string.IsNullOrEmpty(featureBody.Geometry) && Fields.Any(x => x.ProviderType == 29) && !UseGeographyType)
            {
                var field = Fields.First(x => x.ProviderType == 29); //what is if we have more than one geometry field
                properties.Add(GetFieldValue(field, $"geometry::STGeomFromText('{featureBody.Geometry}', {featureBody.Epsg})"));
            }

            // for geography the provider type is 29 as well, that's quite ugly
            if (!string.IsNullOrEmpty(featureBody.Geometry) && Fields.Any(x => x.ProviderType == 29) && UseGeographyType)
            {
                var invertedGeometry = GeometryService.InvertWktGeometry(featureBody.Geometry);

                var field = Fields.First(x => x.ProviderType == 29); //what is if we have more than one geometry field
                properties.Add(GetFieldValue(field, $"geography::STGeomFromText('{invertedGeometry.Value}', {featureBody.Epsg})"));
            }

            return properties;
        }

        private void LogSql(bool logEnabled, string content)
        {
            if (!logEnabled)
            {
                return;
            }

            SqlLog.AppendLine(content);
        }
    }
}
