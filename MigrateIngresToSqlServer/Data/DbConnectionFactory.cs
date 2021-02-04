using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace MigrateIngresToSqlServer.Data
{
    internal interface IDbConnectionFactory
    {
        SqlConnection GetOpenConnection();
    }

    internal class DbConnectionFactory : IDbConnectionFactory, IDisposable
    {
        private SqlConnection _connection;

        private readonly string _connectionString = ConfigurationManager.ConnectionStrings["DestinationDb"].ConnectionString;

        public SqlConnection GetOpenConnection()
        {
            if (_connection != null && _connection.State == ConnectionState.Open)
                return _connection;

            _connection = new SqlConnection(_connectionString);
            _connection.Open();

            return _connection;
        }

        public void Dispose()
        {
            if (_connection != null && _connection.State == ConnectionState.Open)
            {
                _connection.Dispose();
            }
        }
    }
}
