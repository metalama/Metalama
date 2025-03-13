// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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