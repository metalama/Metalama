// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Microsoft.Extensions.Logging;

namespace Metalama.Patterns.TestHelpers;

internal sealed class XUnitLogger : ILogger
{
    private readonly XUnitLoggerProvider _provider;
    private readonly string _name;

    public XUnitLogger( XUnitLoggerProvider provider, string name )
    {
        this._provider = provider;
        this._name = name;
    }

    public void Log<TState>( LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter )
    {
        this._provider.WriteLine( $"{logLevel} {this._name}: {formatter( state, exception )}" );
    }

    public bool IsEnabled( LogLevel logLevel ) => true;

    public IDisposable BeginScope<TState>( TState state )
        where TState : notnull
        => throw new NotImplementedException();
}