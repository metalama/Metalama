// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Infrastructure;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Metalama.Backstage.Tests.Infrastructure;

public sealed class BackstageBackgroundTasksServiceTests
{
    [Fact]
    public async Task LoadTest()
    {
        var service = new BackstageBackgroundTasksService();
        const int n = 1000;
        var completedTasks = 0;

        for ( var i = 0; i < n; i++ )
        {
            service.Enqueue(
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
                service.Enqueue(
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
}