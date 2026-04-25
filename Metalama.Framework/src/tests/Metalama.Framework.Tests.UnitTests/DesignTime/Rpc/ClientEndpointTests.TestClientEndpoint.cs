// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.DesignTime.Rpc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Metalama.Framework.Tests.UnitTests.DesignTime.Rpc;

public sealed partial class ClientEndpointTests
{
    /// <summary>
    /// Test client endpoint for ClientEndpoint tests.
    /// </summary>
    private sealed class TestClientEndpoint : ClientEndpoint
    {
        public TestClientEndpoint( IServiceProvider serviceProvider, string pipeName )
            : base( serviceProvider, pipeName ) { }

        protected override IEnumerable<RpcClient> CreateServiceClients() => [];

        public new TaskCompletionSource<bool> InitializedTask => base.InitializedTask;
    }
}