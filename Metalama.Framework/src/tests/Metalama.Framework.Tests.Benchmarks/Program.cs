// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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