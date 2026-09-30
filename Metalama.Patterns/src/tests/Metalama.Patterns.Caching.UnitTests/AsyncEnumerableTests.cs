// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if NETCOREAPP3_0_OR_GREATER
using Xunit;

namespace Metalama.Patterns.Caching.Tests;

public sealed class AsyncEnumerableTests : AsyncEnumTestsBase
{
    public AsyncEnumerableTests( ITestOutputHelper testOutputHelper ) : base( testOutputHelper ) { }

    [Fact]
    public void DoesNotBlockOnUnawaitedMethod()
    {
        _ = this.BlockedCachedEnumerable();

        // Success is indicated by this method completing.
    }

    [Fact]
    public void DoesNotBlockOnGetAsyncEnumerator()
    {
        // ReSharper disable once NotDisposedResource
        _ = this.BlockedCachedEnumerable().GetAsyncEnumerator( TestContext.Current.CancellationToken );

        // Success is indicated by this method completing.
    }

    [Fact]
    public void DoesNotBlockOnUnawaitedFirstMoveNextAsync()
    {
        // ReSharper disable once NotDisposedResource
        _ = this.BlockedCachedEnumerable().GetAsyncEnumerator( TestContext.Current.CancellationToken ).MoveNextAsync();

        // Success is indicated by this method completing.
    }

    [Fact]
    public async Task IteratesCompletelyOnFirstAwaitedMoveNextAsync()
    {
        // ReSharper disable once NotDisposedResource
        _ = await this.Instance.CachedEnumerable().GetAsyncEnumerator( TestContext.Current.CancellationToken ).MoveNextAsync();

        Assert.Equal( "E1.E2.E3", this.GetLog() );
    }

    [Fact]
    public async Task DoesNotIterateOnSecondAwaitedMoveNextAsync()
    {
        // ReSharper disable once NotDisposedResource
        _ = await this.Instance.CachedEnumerable().GetAsyncEnumerator( TestContext.Current.CancellationToken ).MoveNextAsync();

        this.ClearLog();

        // ReSharper disable once NotDisposedResource
        _ = await this.Instance.CachedEnumerable().GetAsyncEnumerator( TestContext.Current.CancellationToken ).MoveNextAsync();

        Assert.Equal( "", this.GetLog() );
    }

    [Fact]
    public async Task IteratesExpectedSequence1()
    {
        await this.Iterate( this.Instance.CachedEnumerable().GetAsyncEnumerator( TestContext.Current.CancellationToken ) );

        Assert.Equal( "E1.E2.E3.I1.I2[42].I2[99].I3", this.GetLog() );
    }

    [Fact]
    public async Task IteratesExpectedSequence2()
    {
        await this.Iterate( this.Instance.CachedEnumerable().GetAsyncEnumerator( TestContext.Current.CancellationToken ) );
        await this.Iterate( this.Instance.CachedEnumerable().GetAsyncEnumerator( TestContext.Current.CancellationToken ) );

        Assert.Equal( "E1.E2.E3.I1.I2[42].I2[99].I3.I1.I2[42].I2[99].I3", this.GetLog() );
    }

    [Fact]
    public async Task IteratesExpectedSequence3()
    {
        var seq = this.Instance.CachedEnumerable();

        // ReSharper disable once PossibleMultipleEnumeration
        await this.Iterate( seq.GetAsyncEnumerator( TestContext.Current.CancellationToken ) );

        // ReSharper disable once PossibleMultipleEnumeration
        await this.Iterate( seq.GetAsyncEnumerator( TestContext.Current.CancellationToken ) );

        Assert.Equal( "E1.E2.E3.I1.I2[42].I2[99].I3.I1.I2[42].I2[99].I3", this.GetLog() );
    }
}

#endif