// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.DesignTime.Rpc;

namespace Metalama.Framework.Tests.UnitTests.DesignTime.Rpc;

public sealed partial class ServerEndpointTests
{
    /// <summary>
    /// Minimal RPC service for testing.
    /// </summary>
    private sealed class TestService : RpcService<ITestApi>
    {
        public TestService( ServerEndpoint serverEndpoint ) : base( serverEndpoint ) { }

        protected override ITestApi CreateApi( IRpcEventSender eventSender ) => new Api();

        private sealed class Api : ITestApi { }
    }
}
