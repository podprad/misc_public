namespace PdfButcher.Benchmarks
{
    using System;
    using System.Linq;
    using System.Reflection;
    using BenchmarkDotNet.Configs;
    using BenchmarkDotNet.Diagnostics.Windows;
    using BenchmarkDotNet.Exporters;
    using BenchmarkDotNet.Jobs;
    using BenchmarkDotNet.Running;

    public static class Program
    {
        public static void Main()
        {
            var config = new CustomBenchmarkConfig
            {
                // IterationCount = 2,
                UseInliningDiagnoser = true,

#if DEBUG
                DebugRun = true,
#endif
            };

            // config.FilterByType = typeof(StreamReading.ByteReadingBenchmark);
            // config.FilterByType = typeof(MiscParsing.NumberParsingBenchmark);
            config.FilterByType = typeof(IO.PdfLexerBenchmark);
            // config.FilterByMethod = nameof(StreamReading.LoohAheadReadingBenchmark.BinaryLookAheadReader);

            // config.FilterByMethod = nameof(StreamReading.ByteReadingBenchmark.FrameVsSmallReads);

            // config.FilterByType = typeof(SearchAlgorithms.BoyerAndMooreExperimentalBenchmark);
            // config.FilterByMethod = nameof(SearchAlgorithms.BoyerAndMooreExperimentalBenchmark.BoyerAndMoore_performance);

            RunBenchmarkCore(config);
        }

        private static void RunBenchmarkCore(CustomBenchmarkConfig customBenchmarkConfig)
        {
            var config = ManualConfig.CreateMinimumViable()
                .AddExporter(new HtmlExporter());

            if (customBenchmarkConfig.UseInliningDiagnoser)
            {
                config = config.AddDiagnoser(new InliningDiagnoser(false, new[] { "PdfButcher", "PdfButcher.Benchmarks" }));
            }

            var job = Job.Default;

            if (customBenchmarkConfig.DebugRun)
            {
                job = Job.InProcess;
            }

            if (customBenchmarkConfig.IterationCount != null)
            {
                job = job.WithIterationCount(customBenchmarkConfig.IterationCount.Value);
            }

            config = config.AddJob(job);

            if (customBenchmarkConfig.DebugRun)
            {
                config = config.WithOption(ConfigOptions.DisableOptimizationsValidator, true);
            }

            if (customBenchmarkConfig.FilterByType != null && !string.IsNullOrEmpty(customBenchmarkConfig.FilterByMethod))
            {
                var methods = customBenchmarkConfig.FilterByType.GetMethods(BindingFlags.Instance | BindingFlags.Public)
                    .Where(m => m.Name == customBenchmarkConfig.FilterByMethod)
                    .ToArray();

                BenchmarkRunner.Run(
                    customBenchmarkConfig.FilterByType,
                    methods,
                    config);
            }
            else if (customBenchmarkConfig.FilterByType != null)
            {
                BenchmarkRunner.Run(customBenchmarkConfig.FilterByType, config);
            }
            else
            {
                BenchmarkRunner.Run(typeof(Program).Assembly, config);
            }
        }

        private class CustomBenchmarkConfig
        {
            public Type FilterByType { get; set; }

            public string FilterByMethod { get; set; }

            public bool UseInliningDiagnoser { get; set; }

            public int? IterationCount { get; set; }

            public bool DebugRun { get; set; }
        }
    }
}