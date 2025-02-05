// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Infrastructure;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Metalama.Backstage.Tests.Infrastructure;

public class BackstageBackgroundTasksServiceTests
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