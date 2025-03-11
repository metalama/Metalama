// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Diagnostics;
using Metalama.Framework.DesignTime.Rpc;
using System;
using Xunit.Abstractions;

namespace Metalama.Framework.Tests.UnitTests.Remoting;

internal sealed class TestRpcExceptionHandler : IRpcExceptionHandler
{
    private readonly ITestOutputHelper _testOutputHelper;

    public TestRpcExceptionHandler( ITestOutputHelper testOutputHelper )
    {
        this._testOutputHelper = testOutputHelper;
    }

    public void OnException( Exception e, ILogger logger, bool isDisposing )
    {
        try
        {
            this._testOutputHelper.WriteLine( e.ToString() );
        }
        catch ( InvalidOperationException ) { }
    }
}