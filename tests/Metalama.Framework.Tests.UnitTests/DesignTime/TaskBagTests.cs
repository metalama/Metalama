// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Diagnostics;
using Metalama.Framework.DesignTime.Utilities;
using Metalama.Testing.UnitTesting;
using System.Threading.Tasks;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.DesignTime;

#pragma warning disable VSTHRD200

public sealed class TaskBagTests : UnitTestClass
{
    [Fact]
    public async Task NonYielding()
    {
        using var testContext = this.CreateTestContext();
        var bag = new TaskBag( NullLogger.Instance, testContext.ServiceProvider.Global );

        for ( var i = 0; i < 1000; i++ )
        {
            bag.Run( () => Task.CompletedTask );
        }

        await bag.WaitAllAsync( testContext.CancellationToken );

        // This is to test that there is no memory leak.
        Assert.True( bag.IsEmpty );
    }

    [Fact]
    public async Task Yielding()
    {
        using var testContext = this.CreateTestContext();
        var bag = new TaskBag( NullLogger.Instance, testContext.ServiceProvider.Global );

        for ( var i = 0; i < 1000; i++ )
        {
            bag.Run( async () => await Task.Yield() );
        }

        await bag.WaitAllAsync( testContext.CancellationToken );

        // This is to test that there is no memory leak.
        Assert.True( bag.IsEmpty );
    }
}