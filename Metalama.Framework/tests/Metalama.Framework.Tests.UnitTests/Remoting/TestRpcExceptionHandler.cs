// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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