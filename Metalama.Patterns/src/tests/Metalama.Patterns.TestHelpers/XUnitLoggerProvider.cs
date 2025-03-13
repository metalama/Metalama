// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using Xunit.Abstractions;

namespace Metalama.Patterns.TestHelpers;

internal sealed class XUnitLoggerProvider : ILoggerProvider
{
    private readonly ConcurrentDictionary<string, XUnitLogger> _loggers = new();
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly LogObserver _observer;

    internal XUnitLoggerProvider( ITestOutputHelper testOutputHelper, LogObserver observer )
    {
        this._testOutputHelper = testOutputHelper;
        this._observer = observer;
    }

    void IDisposable.Dispose() { }

    public ILogger CreateLogger( string categoryName ) => this._loggers.GetOrAdd( categoryName, n => new XUnitLogger( this, n ) );

    internal void WriteLine( string s )
    {
        this._testOutputHelper.WriteLine( s );
        this._observer.WriteLine( s );
    }
}