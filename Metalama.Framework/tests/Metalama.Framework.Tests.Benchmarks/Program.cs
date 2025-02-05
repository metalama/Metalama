// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

#pragma warning disable CA1822, CA1050

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

BenchmarkRunner.Run<Benchmarks>();

public sealed class Benchmarks
{
    [Benchmark]
    public void NewObject()
    {
        var semaphore = new SemaphoreSlim( 1 );
        semaphore.Wait();
        semaphore.Release();
        semaphore.Dispose();
    }

    [Benchmark]
    public void Pooled()
    {
        // using var handle = Pools.SemaphoreSlim.Allocate();
        // var semaphore = handle.Value;
        // semaphore.Wait();
        // semaphore.Release();
    }
}