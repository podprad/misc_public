namespace MemoryPeak
{
    using System;
    using System.Diagnostics;
    using Devart.Data.Oracle;

    public static class Program
    {
        public static void Main()
        {
            try
            {
                var connectionStringBuilder = new OracleConnectionStringBuilder
                {
                    Server = "",
                    UserId = "",
                    Password = "",
#if NET
                    LicenseKey = "",
#endif

                    // DefaultFetchSize = 100,
                };

                var connectionString = connectionStringBuilder.ToString();

                int fetchSize = 0;
                int fetchSizeMax = 4000;
                int fetchSizeIncrement = 50;

                while (fetchSize <= fetchSizeMax)
                {
                    using (var connection = new OracleConnection(connectionString))
                    {
                        connection.Open();

                        var stopwatch = new Stopwatch();
                        stopwatch.Start();

                        var process = Process.GetCurrentProcess();
                        var memoryBefore = process.PrivateMemorySize64;

                        using (var cmd = connection.CreateCommand())
                        {
                            cmd.FetchSize = fetchSize;
                            cmd.CommandText = "SELECT :p0 as P1, :p0 as P2, :p0 as P3, :p0 as P4, :p0 as P5, :p0 as P6, :p0 as P7, LEVEL FROM DUAL CONNECT BY LEVEL <= 2000";
                            cmd.Parameters.AddWithValue("p0", "any string");

                            var reader = cmd.ExecuteReader();
                            while (reader.Read())
                            {
                            }
                        }

                        stopwatch.Stop();

                        process.Refresh();
                        var memoryAfter = process.PrivateMemorySize64;
                        var memoryDiffInMegabytes = Math.Max(0, memoryAfter - memoryBefore) / 1024 / 1024;
                        Console.WriteLine($"Fetch size={fetchSize}, elapsed={(long)stopwatch.Elapsed.TotalMilliseconds}ms, memory peak={memoryDiffInMegabytes}");

                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                    }

                    fetchSize += fetchSizeIncrement;
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }

            Console.WriteLine("Done");
            Console.ReadKey();
        }
    }
}