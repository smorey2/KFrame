using Microsoft.Extensions.Configuration;
using System;
using System.Data;
using System.Data.SqlClient;

namespace Contoso.Data.Services
{
    public interface IDbService
    {
        IDbConnection GetConnection(string id = null);
    }

    public class DbService : IDbService
    {
        public static int CommandTimeout => 60;
        public static int LongCommandTimeout => 360;
        public static int VeryLongCommandTimeout => 3600;
        public static int VeryVeryLongCommandTimeout => 36000;
        public IDbConnection GetConnection(string name = null)
        {
            if (Configuration == null)
                throw new InvalidOperationException("DbService.Configuration must be set before using GetConnection()");
            return new SqlConnection(Configuration.GetConnectionString(name ?? "Main"));
        }
        public static IConfiguration Configuration { get; set; }
    }
}
