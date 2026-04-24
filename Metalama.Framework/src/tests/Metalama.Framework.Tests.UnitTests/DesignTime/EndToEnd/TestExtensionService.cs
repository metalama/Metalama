// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.DesignTime.Rpc;
using System.Threading.Tasks;

namespace Metalama.Framework.Tests.UnitTests.DesignTime.EndToEnd;

/// <summary>
/// Server-side RPC service implementation.
/// </summary>
public sealed class TestExtensionService : RpcService<ITestExtensionApi>
{
    public TestExtensionService( ServerEndpoint endpoint ) : base( endpoint ) { }

    protected override ITestExtensionApi CreateApi( IRpcEventSender eventSender ) => new Api();

    private sealed class Api : ITestExtensionApi
    {
        public Task PingAsync() => Task.CompletedTask;
    }
}