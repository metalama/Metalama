// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.DesignTime.Rpc;
using System.Threading;
using System.Threading.Tasks;

namespace Metalama.Framework.Tests.UnitTests.DesignTime.Rpc;

public sealed partial class RpcServiceCancellationTests
{
    /// <summary>
    /// An RPC service that exposes the protected wait for initialization, which is otherwise reachable only through
    /// the service methods of the production services.
    /// </summary>
    /// <remarks>
    /// Every production service awaits this wait before doing anything, including
    /// <c>RpcService{TApi}.RaiseEventAsync</c>, which awaits it before it even checks whether there is a client to
    /// raise the event to. Exposing the wait directly is what makes the tests deterministic: they state a property of
    /// the wait itself rather than of whichever caller happens to reach it.
    /// </remarks>
    private sealed class TestService : RpcService<ITestApi>
    {
        public TestService( ServerEndpoint serverEndpoint ) : base( serverEndpoint ) { }

        protected override ITestApi CreateApi( IRpcEventSender eventSender ) => new Api();

        /// <summary>
        /// Awaits initialization, in the same way as every method of every production service does.
        /// </summary>
        public Task WaitAsync( CancellationToken cancellationToken ) => this.WaitUntilInitializedAsync( cancellationToken );

        private sealed class Api : ITestApi { }
    }
}
