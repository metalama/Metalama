// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities.Threading;
using Metalama.Testing.UnitTesting;
using System;
using System.Diagnostics;
using System.Threading;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.TestFramework;

/// <summary>
/// Tests the <see cref="MetalamaTestContext.CancellationToken"/>, which is signalled by the timeout of the context or by
/// the token passed to the constructor.
/// </summary>
public sealed class MetalamaTestContextCancellationTests : UnitTestClass
{
    /// <summary>
    /// The upper bound of the wait for the timeout of the context. The timeout itself is one millisecond, so this bound is
    /// reached only when the timeout does not work.
    /// </summary>
    private static readonly TimeSpan _maxWait = TimeSpan.FromMinutes( 1 );

    private CancellationToken? _tokenPassedToCreateTestContextCore;

    [Fact]
    public void ExternalCancellation_SignalsToken_WithoutExiting()
    {
        Assert.SkipWhen( Debugger.IsAttached, "The context has no timeout and uses the external token directly when a debugger is attached." );

        using var externalSource = new CancellationTokenSource();
        using var testContext = new MetalamaTestContext( new MetalamaTestContextOptions(), externalSource.Token );
        var exitManager = testContext.ServiceProvider.Global.GetRequiredService<ApplicationExitManager>();

        Assert.False( testContext.CancellationToken.IsCancellationRequested );

        externalSource.Cancel();

        Assert.True( testContext.CancellationToken.IsCancellationRequested );
        Assert.False( exitManager.Token.IsCancellationRequested );
    }

    [Fact]
    public void Timeout_SignalsToken_AndExits()
    {
        Assert.SkipWhen( Debugger.IsAttached, "The context has no timeout when a debugger is attached." );

        using var testContext = new MetalamaTestContext( new MetalamaTestContextOptions { Timeout = TimeSpan.FromMilliseconds( 1 ) } );
        var exitManager = testContext.ServiceProvider.Global.GetRequiredService<ApplicationExitManager>();

        // The timeout signals the token of the context before it signals that the application is exiting.
        Assert.True( exitManager.Token.WaitHandle.WaitOne( _maxWait ) );
        Assert.True( testContext.CancellationToken.IsCancellationRequested );
    }

    [Fact]
    public void CreateTestContext_PassesTokenOfXunit()
    {
        using var testContext = this.CreateTestContext();

        Assert.Equal( TestContext.Current.CancellationToken, this._tokenPassedToCreateTestContextCore );
    }

    protected override MetalamaTestContext CreateTestContextCore(
        MetalamaTestContextOptions contextOptions,
        IAdditionalServiceCollection services,
        CancellationToken cancellationToken )
    {
        this._tokenPassedToCreateTestContextCore = cancellationToken;

        return base.CreateTestContextCore( contextOptions, services, cancellationToken );
    }
}
