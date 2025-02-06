// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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