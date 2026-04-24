// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Patterns.Caching.Resilience;
using Xunit;

namespace Metalama.Patterns.Caching.Tests.Resilience;

public class RetryPolicyTests
{
    [Fact]
    public async Task TryAsync_FirstAttempt_ReturnsTrue()
    {
        var policy = new RetryPolicy();
        object? state = null;

        var result = await policy.TryAsync( OperationKind.GetItem, 0, null, ref state, CancellationToken.None );

        Assert.True( result );
    }

    [Fact]
    public async Task TryAsync_SecondAttempt_WithinMaxAttempts_ReturnsTrue()
    {
        var policy = new RetryPolicy { MaxAttempts = 5, BaseDelay = TimeSpan.Zero, NoDelayAttempts = 2 };

        object? state = null;

        var result = await policy.TryAsync( OperationKind.GetItem, 1, new InvalidOperationException(), ref state, CancellationToken.None );

        Assert.True( result );
    }

    [Fact]
    public async Task TryAsync_ExceedsMaxAttempts_ThrowsCachingException()
    {
        var policy = new RetryPolicy { MaxAttempts = 2 };
        object? state = null;

        await Assert.ThrowsAsync<CachingException>(
            async () => await policy.TryAsync( OperationKind.GetItem, 2, new InvalidOperationException(), ref state, CancellationToken.None ) );
    }

    [Fact]
    public async Task TryAsync_ExceptionTypeInList_Retries()
    {
        var policy = new RetryPolicy
        {
            MaxAttempts = 5, BaseDelay = TimeSpan.Zero, NoDelayAttempts = 2, RetryableExceptionTypes = [typeof(InvalidOperationException)]
        };

        object? state = null;

        var result = await policy.TryAsync( OperationKind.GetItem, 1, new InvalidOperationException(), ref state, CancellationToken.None );

        Assert.True( result );
    }

    [Fact]
    public async Task TryAsync_ExceptionTypeNotInList_ThrowsCachingException()
    {
        var policy = new RetryPolicy { MaxAttempts = 5, RetryableExceptionTypes = [typeof(InvalidOperationException)] };

        object? state = null;

        await Assert.ThrowsAsync<CachingException>(
            async () => await policy.TryAsync( OperationKind.GetItem, 1, new ArgumentException(), ref state, CancellationToken.None ) );
    }

    [Fact]
    public async Task TryAsync_DerivedExceptionType_MatchesBaseType()
    {
        var policy = new RetryPolicy { MaxAttempts = 5, BaseDelay = TimeSpan.Zero, NoDelayAttempts = 2, RetryableExceptionTypes = [typeof(Exception)] };

        object? state = null;

        var result = await policy.TryAsync( OperationKind.GetItem, 1, new InvalidOperationException(), ref state, CancellationToken.None );

        Assert.True( result );
    }

    [Fact]
    public async Task TryAsync_CancellationRequested_ThrowsTaskCanceledException()
    {
        var policy = new RetryPolicy { MaxAttempts = 5, BaseDelay = TimeSpan.FromSeconds( 10 ), NoDelayAttempts = 1 };

        object? state = null;

        using var cts = new CancellationTokenSource();
        // ReSharper disable once MethodHasAsyncOverload
        cts.Cancel();

        await Assert.ThrowsAsync<TaskCanceledException>(
            async () => await policy.TryAsync( OperationKind.GetItem, 1, new InvalidOperationException(), ref state, cts.Token ) );
    }

    [Theory]
    [InlineData( OperationKind.SetItem )]
    [InlineData( OperationKind.GetItem )]
    [InlineData( OperationKind.RemoveItem )]
    [InlineData( OperationKind.InvalidateDependency )]
    [InlineData( OperationKind.Background )]
    public async Task TryAsync_AllOperationKinds_Supported( OperationKind kind )
    {
        var policy = new RetryPolicy();
        object? state = null;

        var result = await policy.TryAsync( kind, 0, null, ref state, CancellationToken.None );

        Assert.True( result );
    }
}