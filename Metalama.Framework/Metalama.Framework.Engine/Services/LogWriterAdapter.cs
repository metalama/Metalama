// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Compiler.Services;
using System;

namespace Metalama.Framework.Engine.Services;

internal sealed class LogWriterAdapter : ILogWriter
{
    private readonly Func<Backstage.Diagnostics.ILogWriter?> _getBackstageWriter;

    public LogWriterAdapter( Func<Backstage.Diagnostics.ILogWriter?> getBackstageWriter )
    {
        this._getBackstageWriter = getBackstageWriter;
    }

    public void Log( string message ) => this._getBackstageWriter()?.Log( message );
}