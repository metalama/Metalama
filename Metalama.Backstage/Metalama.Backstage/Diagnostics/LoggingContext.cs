// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Utilities;
using System;
using System.Threading;

namespace Metalama.Backstage.Diagnostics;

internal sealed class LoggingContext
{
    private static readonly AsyncLocal<LoggingContext?> _current = new();

    public LoggingContext( string scope )
    {
        this.Scope = scope;
    }

    public string Scope { get; }

    public static LoggingContext? Current => _current.Value;

    public static DisposableAction EnterScope( string scope, Action<string>? closeScope = null )
    {
        var previousScope = Current;
        _current.Value = new LoggingContext( scope );

        return new DisposableAction(
            () =>
            {
                closeScope?.Invoke( scope );
                _current.Value = previousScope;
            } );
    }
}