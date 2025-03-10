// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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