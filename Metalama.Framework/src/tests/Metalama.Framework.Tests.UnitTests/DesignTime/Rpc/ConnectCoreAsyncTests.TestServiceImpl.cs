// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.DesignTime.Rpc;
using System.Threading;
using System.Threading.Tasks;

namespace Metalama.Framework.Tests.UnitTests.DesignTime.Rpc;

public sealed partial class ConnectCoreAsyncTests
{
    /// <summary>
    /// Simple service implementation for testing.
    /// </summary>
    private sealed class TestServiceImpl : RpcService<ITestServiceApi>
    {
        public TestServiceImpl( ServerEndpoint serverEndpoint ) : base( serverEndpoint ) { }

        protected override ITestServiceApi CreateApi( IRpcEventSender eventSender ) => new Api();

        private sealed class Api : ITestServiceApi
        {
            public Task<string> GetValueAsync( CancellationToken cancellationToken ) => Task.FromResult( "TestValue" );
        }
    }
}