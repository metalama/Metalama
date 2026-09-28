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

    /// <summary>
    /// Verifies that <see cref="MetalamaTestContext.Dispose()"/> waits for a timeout callback that is running, so that the
    /// callback does not use the cancellation token source or the application exit manager after they are disposed.
    /// </summary>
    /// <remarks>
    /// The callback is blocked in its first statement, which writes to the test output. When the callback is released, it
    /// signals the token of the context. Only the callback signals this token: disposing the context does not. The test
    /// records whether the token had been signalled when <see cref="MetalamaTestContext.Dispose()"/> returned, which is
    /// true only if the method waited for the callback.
    /// </remarks>
    [Fact]
    public void Dispose_WhileTimeoutCallbackRuns_WaitsForTheCallback()
    {
        Assert.SkipWhen( Debugger.IsAttached, "The context has no timeout when a debugger is attached." );

        // The events are not disposed, because a regression would leave the callback waiting on one of them.
        var callbackEntered = new ManualResetEventSlim();
        var releaseCallback = new ManualResetEventSlim();

        var testContext = new MetalamaTestContext( new MetalamaTestContextOptions { Timeout = Timeout.InfiniteTimeSpan } );
        var tokenSignalled = false;
        using var registration = testContext.CancellationToken.Register( () => Volatile.Write( ref tokenSignalled, true ) );

        testContext.TestOutputWriter = new BlockingTestOutputHelper( callbackEntered, releaseCallback );
        testContext.ExpireTimeout();

        Assert.True( callbackEntered.Wait( _maxWait ) );

        var tokenSignalledWhenDisposeReturned = false;

        var disposingThread = new Thread(
            () =>
            {
                testContext.Dispose();
                tokenSignalledWhenDisposeReturned = Volatile.Read( ref tokenSignalled );
            } );

        disposingThread.Start();

        // The callback is released once Dispose blocks, which it does only when it waits for the callback, or once it has
        // returned without waiting.
        Assert.True( SpinWait.SpinUntil( () => (disposingThread.ThreadState & (System.Threading.ThreadState.WaitSleepJoin | System.Threading.ThreadState.Stopped)) != 0, _maxWait ) );

        releaseCallback.Set();

        Assert.True( disposingThread.Join( _maxWait ) );
        Assert.True( tokenSignalledWhenDisposeReturned );
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

    /// <summary>
    /// A test output helper that blocks the first call to <see cref="WriteLine(string)"/> until the test releases it.
    /// </summary>
    private sealed class BlockingTestOutputHelper : ITestOutputHelper
    {
        private readonly ManualResetEventSlim _entered;
        private readonly ManualResetEventSlim _release;

        public BlockingTestOutputHelper( ManualResetEventSlim entered, ManualResetEventSlim release )
        {
            this._entered = entered;
            this._release = release;
        }

        /// <inheritdoc />
        public string Output => "";

        /// <inheritdoc />
        public void Write( string message ) { }

        /// <inheritdoc />
        public void Write( string format, params object[] args ) { }

        /// <inheritdoc />
        public void WriteLine( string message )
        {
            this._entered.Set();
            this._release.Wait();
        }

        /// <inheritdoc />
        public void WriteLine( string format, params object[] args ) => this.WriteLine( format );
    }
}
