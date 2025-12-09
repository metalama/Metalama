// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.DesignTime.Rpc;
using System;
using System.Collections.Generic;

namespace Metalama.Framework.Tests.UnitTests.DesignTime.Rpc;

public sealed partial class RpcServiceRaiseEventTests
{
    /// <summary>
    /// Test client endpoint that uses EventTestClient.
    /// </summary>
    private sealed class EventTestClientEndpoint : ClientEndpoint
    {
        public EventTestClientEndpoint( IServiceProvider serviceProvider, string pipeName )
            : base( serviceProvider, pipeName ) { }

        protected override IEnumerable<RpcClient> CreateServiceClients() => [new EventTestClient( this )];
    }
}
