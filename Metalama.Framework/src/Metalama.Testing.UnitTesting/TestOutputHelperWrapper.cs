// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Globalization;
using Xunit;

namespace Metalama.Testing.UnitTesting;

/// <summary>
/// An <see cref="ITestOutputHelper"/> that prefixes every line with the time at which it is written.
/// </summary>
internal sealed class TestOutputHelperWrapper : ITestOutputHelper
{
    private readonly ITestOutputHelper _underlying;

    public TestOutputHelperWrapper( ITestOutputHelper underlying )
    {
        this._underlying = underlying;
    }

    public string Output => this._underlying.Output;

    public void Write( string message ) => this._underlying.Write( message );

    public void Write( string format, params object[] args ) => this._underlying.Write( format, args );

    public void WriteLine( string message )
        => this._underlying.WriteLine( DateTime.Now.ToString( "HH:mm:ss.fff", CultureInfo.InvariantCulture ) + " - " + message );

    public void WriteLine( string format, params object[] args )
        => this._underlying.WriteLine( DateTime.Now.ToString( "HH:mm:ss.fff", CultureInfo.InvariantCulture ) + " - " + format, args );
}
