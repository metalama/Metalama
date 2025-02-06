// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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