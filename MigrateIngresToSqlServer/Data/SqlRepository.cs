using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Text;
using System.Threading.Tasks;

namespace MigrateIngresToSqlServer.Data
{
    // https://stackoverflow.com/questions/1104082/programmatically-create-a-sql-server-ce-table-from-datatable
    internal class SqlRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;
        private static Dictionary<Type, SqlDbType> _typeMap;

        public SqlRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task SaveAsync(DataTable dataTable)
        {
            SetupTypeMap();

            CreateSqlDbTable(dataTable);

            await SaveToSqlAsync(dataTable);
        }

        private async Task SaveToSqlAsync(DataTable dataTable)
        {
            using var bulkCopy = new SqlBulkCopy(_connectionFactory.GetOpenConnection())
            {
                DestinationTableName = dataTable.TableName,
                BulkCopyTimeout = 0,
                BatchSize = 10000
            };

            foreach (var column in dataTable.Columns)
            {
                bulkCopy.ColumnMappings.Add(column.ToString(), column.ToString());
            }

            try
            {
                await bulkCopy.WriteToServerAsync(dataTable);
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
                throw;
            }
        }

        private void CreateSqlDbTable(DataTable dataTable)
        {
            var createTableStatement = GetCreateTableStatement(dataTable);

            using var cmd = new SqlCommand(createTableStatement, _connectionFactory.GetOpenConnection());
            var result = cmd.ExecuteNonQuery();
        }

        private static void SetupTypeMap()
        {
            _typeMap = new Dictionary<Type, SqlDbType>
            {
                [typeof(short)] = SqlDbType.Int,
                [typeof(int)] = SqlDbType.Int,
                [typeof(long)] = SqlDbType.BigInt,
                [typeof(double)] = SqlDbType.Decimal,
                [typeof(decimal)] = SqlDbType.Decimal,
                [typeof(bool)] = SqlDbType.Bit,
                [typeof(string)] = SqlDbType.VarChar,
                [typeof(char)] = SqlDbType.VarChar,
                [typeof(Guid)] = SqlDbType.UniqueIdentifier,
                [typeof(DateTime)] = SqlDbType.DateTime,
                [typeof(DateTimeOffset)] = SqlDbType.DateTimeOffset,
                [typeof(byte[])] = SqlDbType.Binary,
                [typeof(byte?)] = SqlDbType.Bit,
                [typeof(int?)] = SqlDbType.Int,
                [typeof(double?)] = SqlDbType.Decimal,
                [typeof(decimal?)] = SqlDbType.Decimal,
                [typeof(bool?)] = SqlDbType.Bit,
                [typeof(char?)] = SqlDbType.VarChar,
                [typeof(Guid?)] = SqlDbType.UniqueIdentifier,
                [typeof(DateTime?)] = SqlDbType.DateTime,
                [typeof(DateTimeOffset?)] = SqlDbType.DateTimeOffset,
            };
        }

        private static string GetSqlServerTypeName(SqlDbType dbType, int size)
        {
            // Conversions according to: http://msdn.microsoft.com/en-us/library/ms173018.aspx
            if (size > 0)
            {
                return Enum.GetName(typeof(SqlDbType), dbType) + $"({size})";
            }

            if (dbType == SqlDbType.Decimal)
            {
                return Enum.GetName(typeof(SqlDbType), dbType) + "(35, 18)";
            }

            return Enum.GetName(typeof(SqlDbType), dbType);
        }

        private static string GetCreateTableStatement(DataTable table)
        {
            var builder = new StringBuilder();
            builder.Append($"CREATE TABLE [{table.TableName}] (");

            foreach (DataColumn column in table.Columns)
            {
                var dbType = _typeMap[column.DataType];
                builder.Append("[");
                builder.Append(column.ColumnName);
                builder.Append("]");
                builder.Append(" ");
                var typeName = GetSqlServerTypeName(dbType, column.MaxLength);
                builder.Append(typeName);
                builder.Append(", ");
            }

            if (table.Columns.Count > 0)
                builder.Length -= 2;

            builder.Append(")");

            return builder.ToString();
        }


        public async Task<bool> TableExistsAsync(string tableName)
        {
            var query = $"SELECT COUNT(1) FROM dbo.sysobjects WHERE [name] = '{tableName}' AND xtype = 'U'";

            using var command = new SqlCommand(query, _connectionFactory.GetOpenConnection());

            var count = (int)await command.ExecuteScalarAsync();

            return count > 0;
        }

        public async Task<int> GetTableRowCountAsync(string tableName)
        {
            var query = $"SELECT COUNT(1) FROM dbo.{tableName}";

            using var command = new SqlCommand(query, _connectionFactory.GetOpenConnection());

            return (int)await command.ExecuteScalarAsync();
        }
    }
}
