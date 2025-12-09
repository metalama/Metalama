// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Diagnostics;
using Metalama.Framework.DesignTime.Rpc;
using System;

namespace Metalama.Framework.Tests.UnitTests.DesignTime.Rpc;

public sealed partial class BaseEndpointTests
{
    /// <summary>
    /// Test exception handler that tracks whether an exception was handled.
    /// </summary>
    private sealed class TestExceptionHandler : IRpcExceptionHandler
    {
        public bool ExceptionWasHandled { get; private set; }

        public void OnException( Exception exception, ILogger logger, bool isCancellation )
        {
            this.ExceptionWasHandled = true;
        }
    }
}
