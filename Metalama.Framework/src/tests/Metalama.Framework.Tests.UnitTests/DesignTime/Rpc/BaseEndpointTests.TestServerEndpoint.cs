// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.DesignTime.Rpc;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Metalama.Framework.Tests.UnitTests.DesignTime.Rpc;

public sealed partial class BaseEndpointTests
{
    /// <summary>
    /// Test endpoint that exposes the protected ExecuteBackgroundTask method for testing.
    /// </summary>
    private sealed class TestServerEndpoint : ServerEndpoint
    {
        public TestServerEndpoint( IServiceProvider serviceProvider, string pipeName )
            : base( serviceProvider, pipeName ) { }

        protected override IEnumerable<RpcService> CreateServices() => [];

        public void ExecuteBackgroundTaskForTest( Func<CancellationToken, Task> action, string description, bool registerTask = true )
            => this.ExecuteBackgroundTask( action, description, registerTask );
    }
}