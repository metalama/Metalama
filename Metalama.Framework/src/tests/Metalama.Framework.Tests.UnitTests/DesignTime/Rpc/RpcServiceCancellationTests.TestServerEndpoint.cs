// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.DesignTime.Rpc;
using Metalama.Framework.Engine;
using System;
using System.Collections.Generic;

namespace Metalama.Framework.Tests.UnitTests.DesignTime.Rpc;

public sealed partial class RpcServiceCancellationTests
{
    /// <summary>
    /// A server endpoint hosting a single <see cref="TestService"/>, which the tests never connect a client to.
    /// </summary>
    /// <remarks>
    /// A service is initialized when a client attaches, so an endpoint no client attaches to is exactly the state the
    /// tests need. It is also a state that occurs in production: the analysis process always creates its endpoints,
    /// and no client attaches to them when the Visual Studio extension is not installed.
    /// </remarks>
    private sealed class TestServerEndpoint : ServerEndpoint
    {
        public TestServerEndpoint( IServiceProvider serviceProvider, string pipeName )
            : base( serviceProvider, pipeName ) { }

        private TestService? _service;

        protected override IEnumerable<RpcService> CreateServices() => [this._service = new TestService( this )];

        /// <summary>
        /// Gets the service this endpoint hosts. Available once <see cref="ServerEndpoint.Start"/> has been called,
        /// which is when the endpoint creates its services.
        /// </summary>
        public TestService Service => this._service.AssertNotNull();
    }
}
