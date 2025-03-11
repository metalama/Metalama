// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Patterns.TestHelpers;

public sealed class LogObserver
{
    private readonly List<string> _log = new();

    // ReSharper disable once InconsistentlySynchronizedField
    public IReadOnlyList<string> Lines => this._log;

    internal void WriteLine( string s )
    {
        lock ( this._log )
        {
            this._log.Add( s );
        }
    }
}