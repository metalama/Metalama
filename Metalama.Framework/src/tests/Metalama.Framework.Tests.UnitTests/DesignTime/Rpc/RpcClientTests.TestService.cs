// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.DesignTime.Rpc;
using System.Threading;
using System.Threading.Tasks;

namespace Metalama.Framework.Tests.UnitTests.DesignTime.Rpc;

public sealed partial class RpcClientTests
{
    /// <summary>
    /// Test RPC service that supports event broadcasting.
    /// </summary>
    internal sealed class TestService : RpcService<ITestApi>
    {
        public TestService( ServerEndpoint serverEndpoint ) : base( serverEndpoint ) { }

        protected override ITestApi CreateApi( IRpcEventSender eventSender ) => new Api();

        /// <summary>
        /// Raises a test event to all connected clients.
        /// </summary>
        public Task RaiseTestEventAsync( TestEventData data, CancellationToken cancellationToken )
            => this.RaiseEventAsync( data, cancellationToken );

        private sealed class Api : ITestApi { }
    }
}
