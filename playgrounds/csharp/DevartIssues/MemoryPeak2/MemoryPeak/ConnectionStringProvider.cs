namespace MemoryPeak
{
    using Devart.Data.Oracle;

    public static class ConnectionStringProvider
    {
        public static string GetConnectionString()
        {
            var connectionStringBuilder = new OracleConnectionStringBuilder
            {
                Server = "",
                UserId = "",
                Password = "",
#if NET
                    LicenseKey = "",
#endif
            };

            var connectionString = connectionStringBuilder.ToString();
            return connectionString;
        }
    }
}