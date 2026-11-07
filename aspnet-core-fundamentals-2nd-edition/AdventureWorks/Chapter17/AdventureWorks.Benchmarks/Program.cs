using BenchmarkDotNet.Running;

// Runs the benchmarks that --filter selects.
BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly)
    .Run(args);
