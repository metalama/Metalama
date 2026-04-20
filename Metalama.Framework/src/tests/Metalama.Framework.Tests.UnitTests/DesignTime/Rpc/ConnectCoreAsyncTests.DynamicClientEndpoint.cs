// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.DesignTime.Rpc;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace Metalama.Framework.Tests.UnitTests.DesignTime.Rpc;

public sealed partial class ConnectCoreAsyncTests
{
    /// <summary>
    /// Client endpoint that exposes AddServiceClientsAsync for testing.
    /// </summary>
    private sealed class DynamicClientEndpoint : ClientEndpoint
    {
        public DynamicClientEndpoint( IServiceProvider serviceProvider, string pipeName )
            : base( serviceProvider, pipeName ) { }

        protected override IEnumerable<RpcClient> CreateServiceClients() => [new TestServiceClient( this )];

        /// <summary>
        /// Exposes <see cref="ClientEndpoint.AddServiceClientsAsync"/> for testing.
        /// </summary>
        public Task AddServiceClientsForTestAsync(
            string pipeName,
            ImmutableArray<RpcClient> clients,
            CancellationToken cancellationToken )
            => this.AddServiceClientsAsync( pipeName, clients, cancellationToken );
    }
}