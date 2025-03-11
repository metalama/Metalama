// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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