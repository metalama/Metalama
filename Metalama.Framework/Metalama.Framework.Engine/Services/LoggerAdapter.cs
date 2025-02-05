// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Compiler.Services;
using System;

namespace Metalama.Framework.Engine.Services;

internal sealed class LoggerAdapter : ILogger
{
    public LoggerAdapter( Backstage.Diagnostics.ILogger backstageLogger )
    {
        this.Trace = CreateWriter( () => backstageLogger.Trace );
        this.Info = CreateWriter( () => backstageLogger.Info );
        this.Warning = CreateWriter( () => backstageLogger.Warning );
        this.Error = CreateWriter( () => backstageLogger.Error );

        static ILogWriter CreateWriter( Func<Backstage.Diagnostics.ILogWriter?> getBackstageWriter ) => new LogWriterAdapter( getBackstageWriter );
    }

    public ILogWriter Trace { get; }

    public ILogWriter Info { get; }

    public ILogWriter Warning { get; }

    public ILogWriter Error { get; }
}