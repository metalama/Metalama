// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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