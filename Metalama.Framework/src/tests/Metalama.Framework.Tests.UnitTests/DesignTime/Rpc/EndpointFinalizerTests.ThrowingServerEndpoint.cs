// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.DesignTime.Rpc;
using System;
using System.Collections.Generic;

namespace Metalama.Framework.Tests.UnitTests.DesignTime.Rpc;

public sealed partial class EndpointFinalizerTests
{
    /// <summary>
    /// Server endpoint whose <c>Dispose( false )</c> throws, as it does when an assembly cannot be loaded on the finalizer thread.
    /// </summary>
    private sealed class ThrowingServerEndpoint : ServerEndpoint
    {
        public const string ExceptionMessage = "Test exception thrown by Dispose( false ).";

        public ThrowingServerEndpoint( IServiceProvider serviceProvider, string pipeName )
            : base( serviceProvider, pipeName ) { }

        public bool DisposeWasCalledFromFinalizer { get; private set; }

        protected override IEnumerable<RpcService> CreateServices() => [];

        protected override void Dispose( bool disposing )
        {
            if ( !disposing )
            {
                this.DisposeWasCalledFromFinalizer = true;

                throw new InvalidOperationException( ExceptionMessage );
            }

            base.Dispose( disposing );
        }
    }
}
