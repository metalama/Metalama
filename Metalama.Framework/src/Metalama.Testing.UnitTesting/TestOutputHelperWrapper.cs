// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Globalization;
using Xunit;

namespace Metalama.Testing.UnitTesting;

/// <summary>
/// An <see cref="ITestOutputHelper"/> that prefixes every line with the time at which it is written, and drops the
/// messages that arrive after the test has ended.
/// </summary>
/// <remarks>
/// xUnit throws an <see cref="InvalidOperationException"/> when a message is written after the test has ended, which
/// happens when an asynchronous operation of the code under test outlives the test, such as a file system watcher
/// handler. Without this guard, the exception escapes from a thread that has no handler and terminates the test host.
/// </remarks>
internal sealed class TestOutputHelperWrapper : ITestOutputHelper
{
    private readonly ITestOutputHelper _underlying;

    public TestOutputHelperWrapper( ITestOutputHelper underlying )
    {
        this._underlying = underlying;
    }

    public string Output => this._underlying.Output;

    public void Write( string message )
    {
        try
        {
            this._underlying.Write( message );
        }
        catch ( InvalidOperationException )
        {
            // The test has ended.
        }
    }

    public void Write( string format, params object[] args )
    {
        try
        {
            this._underlying.Write( format, args );
        }
        catch ( InvalidOperationException )
        {
            // The test has ended.
        }
    }

    public void WriteLine( string message )
    {
        try
        {
            this._underlying.WriteLine( DateTime.Now.ToString( "HH:mm:ss.fff", CultureInfo.InvariantCulture ) + " - " + message );
        }
        catch ( InvalidOperationException )
        {
            // The test has ended.
        }
    }

    public void WriteLine( string format, params object[] args )
    {
        try
        {
            this._underlying.WriteLine( DateTime.Now.ToString( "HH:mm:ss.fff", CultureInfo.InvariantCulture ) + " - " + format, args );
        }
        catch ( InvalidOperationException )
        {
            // The test has ended.
        }
    }
}
