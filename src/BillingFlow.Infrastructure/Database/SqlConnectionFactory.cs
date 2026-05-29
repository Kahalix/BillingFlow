// File: src/BillingFlow.Infrastructure/Database/SqlConnectionFactory.cs
using System.Data;

using BillingFlow.Application.Interfaces;

using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace BillingFlow.Infrastructure.Database;

public class SqlConnectionFactory(IConfiguration configuration) : IDbConnectionFactory
{
    public IDbConnection CreateConnection()
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        return new SqlConnection(connectionString);
    }
}
