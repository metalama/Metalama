// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Backstage.Extensibility;

namespace Metalama.Backstage.Infrastructure;

[PublicAPI]
public interface IEnvironmentVariableProvider : IBackstageService
{
    string? GetEnvironmentVariable( string variable );

    /// <summary>
    /// Gets the current working directory of the process. Used, for example, to resolve the tooling telemetry policy:
    /// telemetry about the tooling itself is governed by the repository the process is running in, if any. See #1701.
    /// </summary>
    string CurrentDirectory { get; }
}