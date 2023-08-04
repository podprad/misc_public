namespace MemoryPeak
{
    using System;
    using System.Data.Entity;
    using System.Diagnostics;
    using System.Linq;
    using Devart.Data.Oracle;
    using Devart.Data.Oracle.Entity.Configuration;

    public static class Program
    {
        public static void Main()
        {
            DbConfiguration.SetConfiguration(new MyOracleConfiguration());

            var connectionStringBuilder = new OracleConnectionStringBuilder
            {
                Server = "OSCYPEK3",
                UserId = "",
                Password = "",
                LicenseKey = "",
            };

            var connectionString = connectionStringBuilder.ToString();

            while (true)
            {
                using (var connection = new OracleConnection(connectionString))
                {
                    connection.Open();

                    // In our application we use real database-first context, however I managed
                    // to reproduce it by base DbContext and simple SELECT from DUAL.
                    using (var context = new DbContext(connection, false))
                    {
                        var elements = context.Database.SqlQuery<MyDummyEntity>("SELECT :p0 as P1, LEVEL FROM DUAL CONNECT BY LEVEL <= 2000", "any string");

                        var process = Process.GetCurrentProcess();
                        Console.WriteLine($"Private bytes before: {process.PrivateMemorySize64}");

                        var result = elements.ToList();
                        Console.WriteLine($"Fetched {result.Count} objects");

                        // After calling ToList() we get ~500MB peak.
                        // In our 32bit applications it sometimes leads to OutOfMemoryException.
                        process.Refresh();
                        Console.WriteLine($"Private bytes after: {process.PrivateMemorySize64}");

                        Thread.Sleep(2000);
                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                    }
                }
            }
        }

        public class MyDummyEntity
        {
            public string P1 { get; set; }

            public long LEVEL { get; set; }
        }

        private class MyOracleConfiguration : DbConfiguration
        {
            public MyOracleConfiguration()
            {
                var config = OracleEntityProviderConfig.Instance;

                config.CodeFirstOptions.ColumnTypeCasingConventionCompatibility = false;
                config.DmlOptions.ReuseParameters = true;
                config.SqlFormatting.Disable();
                config.Workarounds.ProviderManifestToken = "Oracle, 19.0.0.0";
                config.DmlOptions.BatchUpdates.Enabled = true;

                object services = new Devart.Data.Oracle.Entity.OracleEntityProviderServices();
                var castedServices = (System.Data.Entity.Core.Common.DbProviderServices)services;

                const string ProviderName = "Devart.Data.Oracle";
                this.SetProviderServices(ProviderName, castedServices);
                this.SetProviderFactory(ProviderName, new OracleProviderFactory());
            }
        }
    }
}