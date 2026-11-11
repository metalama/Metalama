// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using SharpCrafters.Backstage.Diagnostics;
using Metalama.Framework.DesignTime.Utilities;
using Metalama.Framework.Engine.Utilities.Threading;
using Metalama.Testing.UnitTesting;
using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.DesignTime;

#pragma warning disable VSTHRD200

public sealed class QuietPeriodTimerTests : UnitTestClass
{
    /// <summary>
    /// An exception thrown by a <see cref="QuietPeriodTimer.Tick"/> handler on the timer thread must be passed to the
    /// handler given to the constructor instead of terminating the process, and the timer must remain usable.
    /// See https://github.com/metalama/Metalama/issues/2317.
    /// </summary>
    [Fact]
    public async Task ExceptionInTickHandlerIsReportedAndTimerRemainsUsable()
    {
        using var testContext = this.CreateTestContext();

        var reported = new TaskCompletionSource<Exception>( TaskCreationOptions.RunContinuationsAsynchronously );
        var secondTick = new TaskCompletionSource<bool>( TaskCreationOptions.RunContinuationsAsynchronously );
        var tickCount = 0;

        using var timer = new QuietPeriodTimer( TimeSpan.FromMilliseconds( 1 ), NullLogger.Instance, e => reported.TrySetResult( e ) );

        timer.Tick += ( _, _ ) =>
        {
            if ( ++tickCount == 1 )
            {
                throw new UnauthorizedAccessException( "Access to the path is denied." );
            }

            secondTick.TrySetResult( true );
        };

        timer.Restart();

        var exception = await reported.Task.WithCancellation( testContext.CancellationToken );
        Assert.IsType<UnauthorizedAccessException>( exception );

        timer.Restart();

        Assert.True( await secondTick.Task.WithCancellation( testContext.CancellationToken ) );
    }

    [Fact]
    public async Task ExceptionInReportingHandlerIsSwallowed()
    {
        using var testContext = this.CreateTestContext();

        var reportingCalled = new TaskCompletionSource<bool>( TaskCreationOptions.RunContinuationsAsynchronously );

        using var timer = new QuietPeriodTimer(
            TimeSpan.FromMilliseconds( 1 ),
            NullLogger.Instance,
            _ =>
            {
                reportingCalled.TrySetResult( true );

                throw new IOException( "The reporter failed too." );
            } );

        timer.Tick += ( _, _ ) => throw new InvalidOperationException();

        timer.Restart();

        // Reaching this point without the test host crashing is the assertion.
        Assert.True( await reportingCalled.Task.WithCancellation( testContext.CancellationToken ) );
    }
}
