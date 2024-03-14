namespace MemoryPeak
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime;
    using System.Text;
    using Devart.Data.Oracle;

    public static class Program
    {
        private static readonly Dictionary<int, byte[]> FragmentedHeap = new Dictionary<int, byte[]>();

        public static void Main()
        {
#if NET
            GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
#endif

            var process = Process.GetCurrentProcess();

            try
            {
                Console.WriteLine("Fragmenting heap");
                FragmentHeap();

                process.Refresh();
                PrintCurrentMemory(process);

                GC.Collect();
                GC.WaitForPendingFinalizers();

                process.Refresh();
                PrintCurrentMemory(process);

                var connectionString = ConnectionStringProvider.GetConnectionString();

                int fetchSizeStart = 250;
                int iterationsCount = 100;
                int fetchSizeIncrement = 50;
                int fetchSizeMax = 500;

                int fetchSize = fetchSizeStart;
                for(int i = 0; i < iterationsCount; i++)
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();

                    using (var connection = new OracleConnection(connectionString))
                    {
                        connection.Open();

                        var stopwatch = new Stopwatch();
                        stopwatch.Start();

                        process.Refresh();
                        var memoryBefore = process.PrivateMemorySize64;

                        int usedFetchSize;

                        using (var cmd = connection.CreateCommand())
                        {
                            if (fetchSize > 0)
                            {
                                cmd.FetchSize = fetchSize;
                            }

                            // var repeatedParameters = Enumerable.Range(1, 100).Select(g => $":p0 as P{g}").ToArray();
                            // var repeatedParametersJoined = string.Join(", ", repeatedParameters);
                            // cmd.CommandText = $"SELECT {repeatedParametersJoined}, LEVEL FROM DUAL CONNECT BY LEVEL <= 10000";
                            cmd.CommandText = "SELECT :p0 as P1, LEVEL FROM DUAL CONNECT BY LEVEL <= 2000";
                            cmd.Parameters.AddWithValue("p0", "any string");

                            var reader = cmd.ExecuteReader();
                            while (reader.Read())
                            {
                            }

                            usedFetchSize = cmd.FetchSize;
                        }

                        stopwatch.Stop();

                        process.Refresh();
                        var memoryAfter = process.PrivateMemorySize64;
                        var memoryDiffInMegabytes = Math.Max(0, memoryAfter - memoryBefore) / 1024 / 1024;
                        Console.WriteLine($"Fetch size={usedFetchSize}, elapsed={(long)stopwatch.Elapsed.TotalMilliseconds}ms, memory peak={memoryDiffInMegabytes}");
                    }

                    fetchSize += fetchSizeIncrement;

                    fetchSize = Math.Min(fetchSizeMax, fetchSize);
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }

            Console.WriteLine("Done");
        }

        private static void PrintCurrentMemory(Process process)
        {
            Console.WriteLine($"Current memory {process.PrivateMemorySize64 / 1024 / 1024}MB");
        }

        private static void FragmentHeap()
        {
            int fragmentsCount = 10;
            var fragmentSizeInMegabytes = 128;

            for (int i = 0; i < fragmentsCount; i++)
            {
                var bytes = new byte[1 * 1024 * 1024 * fragmentSizeInMegabytes];
                FragmentedHeap[i] = bytes;
            }

            for (int i = 0; i < fragmentsCount; i++)
            {
                if (i % 2 == 0)
                {
                    FragmentedHeap[i] = null;
                }
            }

            var builder = new StringBuilder();

            for (int i = 0; i < 10; i++)
            {
                var value = FragmentedHeap[i];

                var mark = value != null ? "#" : "_";

                builder.Append(mark);
            }

            Console.WriteLine(builder.ToString());
        }
    }
}