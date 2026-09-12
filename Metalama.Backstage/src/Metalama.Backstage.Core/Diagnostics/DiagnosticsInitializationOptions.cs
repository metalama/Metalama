// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using System;

namespace Metalama.Backstage.Diagnostics;

[PublicAPI]
public record DiagnosticsInitializationOptions
{
    /// <summary>
    /// Gets an optional action that registers the <see cref="ILoggerFactory"/>.
    /// </summary>
    public Func<IServiceProvider, ILoggerFactory>? CreateLoggingFactory { get; init; }

    /// <summary>
    /// Gets a delegate called for any trace message. When this property is set to a non-null value, detailed tracing is enabled
    /// for all categories.
    /// </summary>
    public Action<string>? TraceAction { get; init; }
}