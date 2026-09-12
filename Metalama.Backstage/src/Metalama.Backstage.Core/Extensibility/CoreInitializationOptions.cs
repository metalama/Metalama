// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Backstage.Application;
using Metalama.Backstage.Diagnostics;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization.Metadata;

namespace Metalama.Backstage.Extensibility;

/// <summary>
/// The options of <see cref="RegisterCoreServices.AddCoreServices"/>.
/// </summary>
/// <param name="ProductProfile">The profile of the product family that hosts the services.</param>
/// <param name="ApplicationInfo">The description of the host process.</param>
[PublicAPI]
public sealed record CoreInitializationOptions( ProductProfile ProductProfile, IApplicationInfo ApplicationInfo )
{
    /// <summary>
    /// Gets a value indicating whether the logging, profiling and debugging services should be registered.
    /// </summary>
    public bool AddDiagnostics { get; init; }

    /// <summary>
    /// Gets a value indicating whether the crash dump service should be registered. It is registered anyway when
    /// <see cref="AddDiagnostics"/> is <c>true</c>.
    /// </summary>
    public bool AddDumper { get; init; }

    /// <summary>
    /// Gets a value indicating whether the services that locate and start the tools (the worker and the desktop
    /// notifier) should be registered.
    /// </summary>
    public bool AddTools { get; init; }

    /// <summary>
    /// Gets a value indicating whether the current program executes from a development environment, in which case
    /// the tools are located under the bin directory of their respective projects.
    /// </summary>
    public bool IsDevelopmentEnvironment { get; init; }

    /// <summary>
    /// Gets a delegate that registers the service that extracts the tools, when <see cref="AddTools"/> is <c>true</c>.
    /// </summary>
    public Action<ServiceProviderBuilder>? AddToolsExtractor { get; init; }

    /// <summary>
    /// Gets the diagnostic (tracing) options, considered when <see cref="AddDiagnostics"/> is <c>true</c>.
    /// </summary>
    public DiagnosticsInitializationOptions DiagnosticsOptions { get; init; } = new();

    /// <summary>
    /// Gets the resolvers of the serializable configuration types, in the order in which they are consulted. The
    /// host gives the source-generated contexts of the packages it uses, then its own.
    /// </summary>
    public IReadOnlyList<IJsonTypeInfoResolver> JsonTypeInfoResolvers { get; init; } = [];
}
