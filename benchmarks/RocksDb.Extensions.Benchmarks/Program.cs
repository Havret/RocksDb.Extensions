// See https://aka.ms/new-console-template for more information

using BenchmarkDotNet.Running;
using RocksDb.Extensions.Benchmarks;

BenchmarkRunner.Run(typeof(RocksDbBenchmark));
