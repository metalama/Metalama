// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using System;
using System.Collections.Generic;

namespace Metalama.Framework.Workspaces;

/// <summary>
/// An exception thrown when errors occur while loading a workspace.
/// </summary>
/// <seealso cref="Workspace"/>
/// <seealso cref="WorkspaceCollection"/>
[PublicAPI]
public sealed class WorkspaceLoadException : Exception
{
    /// <summary>
    /// Gets the list of error messages that occurred during workspace loading.
    /// </summary>
    public IReadOnlyList<string> Errors { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="WorkspaceLoadException"/> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="errors">The list of error messages.</param>
    public WorkspaceLoadException( string message, IReadOnlyList<string> errors ) : base( message )
    {
        this.Errors = errors;
    }
}