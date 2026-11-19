// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Collections.Generic;

namespace Metalama.Introspection.Navigation.Telemetry;

/// <summary>
/// The facts about a workspace load that a usage report contains.
/// </summary>
/// <param name="Host">The component that loaded the workspace.</param>
/// <param name="Kinds">The file extensions of the loaded projects or solutions, such as <c>.sln</c> or <c>.csproj</c>.</param>
/// <param name="ProjectCount">The number of distinct projects, regardless of their target frameworks.</param>
/// <param name="MetalamaProjectCount">The number of distinct projects that use Metalama.</param>
/// <param name="TargetFrameworks">The target frameworks of the projects.</param>
/// <param name="LoadDuration">The time the load took.</param>
/// <param name="ErrorCount">The number of errors reported while the workspace was loaded.</param>
/// <param name="WarningCount">The number of warnings reported while the workspace was loaded.</param>
/// <param name="ClientName">The name of the client: the MCP client, or LINQPad.</param>
/// <param name="ClientVersion">The version of the client.</param>
internal sealed record WorkspaceUsage(
    IntrospectionHost Host,
    IEnumerable<string> Kinds,
    int ProjectCount,
    int MetalamaProjectCount,
    IEnumerable<string> TargetFrameworks,
    TimeSpan LoadDuration,
    int ErrorCount,
    int WarningCount,
    string? ClientName,
    string? ClientVersion );
