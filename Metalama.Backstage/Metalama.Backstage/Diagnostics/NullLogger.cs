// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Utilities;
using System;

namespace Metalama.Backstage.Diagnostics;

public class NullLogger : ILogger, ILoggerFactory
{
    private NullLogger() { }

    public static NullLogger Instance { get; } = new();

    ILogWriter? ILogger.Trace => null;

    ILogWriter? ILogger.Info => null;

    ILogWriter? ILogger.Warning => null;

    ILogWriter? ILogger.Error => null;

    public ILogger WithPrefix( string prefix ) => this;

    ILogger ILoggerFactory.GetLogger( string category ) => this;

    public void Flush() { }

    public IDisposable EnterScope( string scope ) => default(DisposableAction);
}