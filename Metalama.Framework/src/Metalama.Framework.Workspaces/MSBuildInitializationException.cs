// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using System;

namespace Metalama.Framework.Workspaces;

/// <summary>
/// An exception thrown when MSBuild initialization fails.
/// </summary>
/// <seealso cref="Workspace"/>
/// <seealso cref="WorkspaceCollection"/>
[PublicAPI]
public sealed class MSBuildInitializationException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MSBuildInitializationException"/> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    public MSBuildInitializationException( string message ) : base( message ) { }

    /// <summary>
    /// Gets a value indicating whether a .NET SDK was found for a different architecture than the one of the current process.
    /// </summary>
    public bool HasArchitectureMismatch { get; init; }
}