// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Infrastructure;
using Metalama.Backstage.Testing;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Backstage.Tests.Infrastructure;

public sealed class BackstageBackgroundTasksServiceTests
{
    private readonly ITestOutputHelper _testOutput;

    public BackstageBackgroundTasksServiceTests( ITestOutputHelper testOutput )
    {
        this._testOutput = testOutput;
    }

    [Fact]
    public async Task LoadTest()
    {
        var service = new BackstageBackgroundTasksService();
        const int n = 1000;
        var completedTasks = 0;

        for ( var i = 0; i < n; i++ )
        {
            _ = service.Enqueue(
                async () =>
                {
                    await Task.Yield();
                    Interlocked.Increment( ref completedTasks );
                } );
        }

        await service.WhenNoPendingTaskAsync();

        Assert.Equal( n, completedTasks );
    }

    [Fact]
    public async Task LoadTestWithPauses()
    {
        var service = new BackstageBackgroundTasksService();
        const int n = 100, m = 100;
        var completedTasks = 0;

        for ( var i = 0; i < n; i++ )
        {
            for ( var j = 0; j < m; j++ )
            {
                _ = service.Enqueue(
                    async () =>
                    {
                        await Task.Yield();
                        Interlocked.Increment( ref completedTasks );
                    } );
            }

            await Task.Delay( 10 );
        }

        await service.WhenNoPendingTaskAsync();

        Assert.Equal( n * m, completedTasks );
    }

    [Fact]
    public async Task NestedEnqueueDuringCompletionIsNotLost()
    {
        // #1751: a background task that enqueues another one is exactly what the telemetry upload does. The services
        // initializer enqueues StartUpload, and StartUpload enqueues the start of the upload process. If completion
        // refuses that nested enqueue, the upload process is never started and the queue is never uploaded.
        var service = new BackstageBackgroundTasksService();

        var outerStarted = new TaskCompletionSource<bool>();
        var releaseOuter = new TaskCompletionSource<bool>();
        var nestedRan = 0;
        Exception? nestedEnqueueException = null;

        _ = service.Enqueue(
            async () =>
            {
                outerStarted.SetResult( true );
                await releaseOuter.Task;

                try
                {
                    await service.Enqueue( () => Interlocked.Increment( ref nestedRan ) );
                }
                catch ( Exception e )
                {
                    nestedEnqueueException = e;
                }
            } );

        // Start completing while the outer task is still running, exactly as the shutdown handler does.
        await outerStarted.Task;
        var completion = service.CompleteAsync();
        releaseOuter.SetResult( true );

        await completion;

        Assert.Null( nestedEnqueueException );
        Assert.Equal( 1, nestedRan );
    }

    [Fact]
    public async Task FaultOfABackgroundTaskIsReported()
    {
        // #1765: nothing awaits an enqueued task, so a task that throws used to fail in complete silence. That is how
        // the telemetry upload managed to be dead for a year without a single log line.
        var loggerFactory = new TestLoggerFactory( this._testOutput );
        var service = new BackstageBackgroundTasksService();
        service.SetLogger( loggerFactory.GetLogger( "BackgroundTasks" ) );

        await service.Enqueue( () => throw new InvalidOperationException( "Simulated background task failure." ) );
        await service.WhenNoPendingTaskAsync();

        Assert.Contains( loggerFactory.Entries, e => e.Message.ContainsOrdinal( "Simulated background task failure." ) );
    }

    [Fact]
    public async Task FaultOfABackgroundTaskDoesNotBreakCompletion()
    {
        // A failing task must still count as completed, otherwise a process would wait for it for ever on shutdown.
        var service = new BackstageBackgroundTasksService();

        await service.Enqueue( () => throw new InvalidOperationException( "Simulated background task failure." ) );

        await service.CompleteAsync();
    }
}