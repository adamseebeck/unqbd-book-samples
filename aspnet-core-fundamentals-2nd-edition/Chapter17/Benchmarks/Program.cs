using BenchmarkDotNet.Running;

BenchmarkSwitcher.FromAssembly(typeof(CsvParsingBenchmarks).Assembly).Run(args);
