// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

#if NETCOREAPP3_0_OR_GREATER
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Patterns.Caching.Tests;

public sealed class AsyncEnumeratorTests : AsyncEnumTestsBase
{
    public AsyncEnumeratorTests( ITestOutputHelper testOutputHelper ) : base( testOutputHelper ) { }

    [Fact]
    public void DoesNotBlockOnUnawaitedMethod()
    {
        _ = this.Instance.BlockedCachedEnumerator();

        // Success is indicated by this method completing.
    }

    [Fact]
    public void DoesNotBlockOnUnawaitedFirstMoveNextAsync()
    {
        _ = this.Instance.BlockedCachedEnumerator().MoveNextAsync();

        // Success is indicated by this method completing.
    }

    [Fact]
    public async Task IteratesCompletelyOnFirstAwaitedMoveNextAsync()
    {
        _ = await this.Instance.CachedEnumerator().MoveNextAsync();

        Assert.Equal( "E1.E2.E3", this.StringBuilder.ToString() );
    }

    [Fact]
    public async Task DoesNotIterateOnSecondAwaitedMoveNextAsync()
    {
        _ = await this.Instance.CachedEnumerator().MoveNextAsync();
        this.StringBuilder.Clear();
        _ = await this.Instance.CachedEnumerator().MoveNextAsync();

        Assert.Equal( "", this.StringBuilder.ToString() );
    }

    [Fact]
    public async Task IteratesExpectedSequence1()
    {
        await this.Iterate( this.Instance.CachedEnumerator() );

        Assert.Equal( "E1.E2.E3.I1.I2[42].I2[99].I3", this.StringBuilder.ToString() );
    }

    [Fact]
    public async Task IteratesExpectedSequence2()
    {
        await this.Iterate( this.Instance.CachedEnumerator() );
        await this.Iterate( this.Instance.CachedEnumerator() );

        Assert.Equal( "E1.E2.E3.I1.I2[42].I2[99].I3.I1.I2[42].I2[99].I3", this.StringBuilder.ToString() );
    }
}

#endif