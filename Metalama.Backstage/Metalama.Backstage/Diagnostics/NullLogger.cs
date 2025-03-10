// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Utilities;
using System;

namespace Metalama.Backstage.Diagnostics;

public sealed class NullLogger : ILogger, ILoggerFactory
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