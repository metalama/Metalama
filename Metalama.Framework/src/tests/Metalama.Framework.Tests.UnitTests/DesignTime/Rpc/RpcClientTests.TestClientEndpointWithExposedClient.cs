// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.DesignTime.Rpc;
using System;
using System.Collections.Generic;

namespace Metalama.Framework.Tests.UnitTests.DesignTime.Rpc;

public sealed partial class RpcClientTests
{
    /// <summary>
    /// Test client endpoint that exposes the RpcClient before connection.
    /// This allows testing races between GetApiDangerous and initialization.
    /// </summary>
    private sealed class TestClientEndpointWithExposedClient : ClientEndpoint
    {
        public TestClientEndpointWithExposedClient( IServiceProvider serviceProvider, string pipeName )
            : base( serviceProvider, pipeName )
        {
            // Create the client immediately so it can be accessed before connecting.
            this.ExposedClient = new TestClient( this );
        }

        /// <summary>
        /// Gets the client that was created in the constructor.
        /// This client may not yet be initialized (connected to the server).
        /// </summary>
        public TestClient ExposedClient { get; }

        protected override IEnumerable<RpcClient> CreateServiceClients() => [this.ExposedClient];
    }
}
