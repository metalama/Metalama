// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using System;

namespace Metalama.Framework.Workspaces;

/// <summary>
/// An exception thrown then initializing MSBuild.
/// </summary>
[PublicAPI]
public sealed class MSBuildInitializationException : Exception
{
    public MSBuildInitializationException( string message ) : base( message ) { }

    /// <summary>
    /// Gets a value indicating whether a .NET SDK was found for a different architecture than the one of the current process.
    /// </summary>
    public bool HasArchitectureMismatch { get; init; }
}