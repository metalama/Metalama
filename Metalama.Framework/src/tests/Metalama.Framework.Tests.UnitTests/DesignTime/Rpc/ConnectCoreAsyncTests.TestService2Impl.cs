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
    /// Second service implementation for testing dynamic client addition.
    /// </summary>
    private sealed class TestService2Impl : RpcService<ITestService2Api>
    {
        public TestService2Impl( ServerEndpoint serverEndpoint ) : base( serverEndpoint ) { }

        protected override ITestService2Api CreateApi( IRpcEventSender eventSender ) => new Api();

        private sealed class Api : ITestService2Api
        {
            public Task<string> GetValue2Async( CancellationToken cancellationToken ) => Task.FromResult( "TestValue2" );
        }
    }
}