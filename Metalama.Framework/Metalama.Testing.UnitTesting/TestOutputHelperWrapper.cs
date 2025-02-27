// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System;
using System.Globalization;
using Xunit.Abstractions;

namespace Metalama.Testing.UnitTesting;

internal sealed class TestOutputHelperWrapper : ITestOutputHelper
{
    private readonly ITestOutputHelper _underlying;

    public TestOutputHelperWrapper( ITestOutputHelper underlying )
    {
        this._underlying = underlying;
    }

    public void WriteLine( string message )
        => this._underlying.WriteLine( DateTime.Now.ToString( "HH:mm:ss.fff", CultureInfo.InvariantCulture ) + " - " + message );

    public void WriteLine( string format, params object[] args )
        => this._underlying.WriteLine( DateTime.Now.ToString( "HH:mm:ss.fff", CultureInfo.InvariantCulture ) + " - " + format, args );
}