// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

#if NETCOREAPP3_0_OR_GREATER
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Patterns.Caching.Tests;

public sealed class AsyncEnumerableTests : AsyncEnumTestsBase
{
    public AsyncEnumerableTests( ITestOutputHelper testOutputHelper ) : base( testOutputHelper ) { }

    [Fact]
    public void DoesNotBlockOnUnawaitedMethod()
    {
        _ = this.Instance.BlockedCachedEnumerable();

        // Success is indicated by this method completing.
    }

    [Fact]
    public void DoesNotBlockOnGetAsyncEnumerator()
    {
        // ReSharper disable once NotDisposedResource
        _ = this.Instance.BlockedCachedEnumerable().GetAsyncEnumerator();

        // Success is indicated by this method completing.
    }

    [Fact]
    public void DoesNotBlockOnUnawaitedFirstMoveNextAsync()
    {
        // ReSharper disable once NotDisposedResource
        _ = this.Instance.BlockedCachedEnumerable().GetAsyncEnumerator().MoveNextAsync();

        // Success is indicated by this method completing.
    }

    [Fact]
    public async Task IteratesCompletelyOnFirstAwaitedMoveNextAsync()
    {
        // ReSharper disable once NotDisposedResource
        _ = await this.Instance.CachedEnumerable().GetAsyncEnumerator().MoveNextAsync();

        Assert.Equal( "E1.E2.E3", this.StringBuilder.ToString() );
    }

    [Fact]
    public async Task DoesNotIterateOnSecondAwaitedMoveNextAsync()
    {
        // ReSharper disable once NotDisposedResource
        _ = await this.Instance.CachedEnumerable().GetAsyncEnumerator().MoveNextAsync();
        
        this.StringBuilder.Clear();
        
        // ReSharper disable once NotDisposedResource
        _ = await this.Instance.CachedEnumerable().GetAsyncEnumerator().MoveNextAsync();

        Assert.Equal( "", this.StringBuilder.ToString() );
    }

    [Fact]
    public async Task IteratesExpectedSequence1()
    {
        await this.Iterate( this.Instance.CachedEnumerable().GetAsyncEnumerator() );

        Assert.Equal( "E1.E2.E3.I1.I2[42].I2[99].I3", this.StringBuilder.ToString() );
    }

    [Fact]
    public async Task IteratesExpectedSequence2()
    {
        await this.Iterate( this.Instance.CachedEnumerable().GetAsyncEnumerator() );
        await this.Iterate( this.Instance.CachedEnumerable().GetAsyncEnumerator() );

        Assert.Equal( "E1.E2.E3.I1.I2[42].I2[99].I3.I1.I2[42].I2[99].I3", this.StringBuilder.ToString() );
    }

    [Fact]
    public async Task IteratesExpectedSequence3()
    {
        var seq = this.Instance.CachedEnumerable();

        // ReSharper disable once PossibleMultipleEnumeration
        await this.Iterate( seq.GetAsyncEnumerator() );

        // ReSharper disable once PossibleMultipleEnumeration
        await this.Iterate( seq.GetAsyncEnumerator() );

        Assert.Equal( "E1.E2.E3.I1.I2[42].I2[99].I3.I1.I2[42].I2[99].I3", this.StringBuilder.ToString() );
    }
}

#endif