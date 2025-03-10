// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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