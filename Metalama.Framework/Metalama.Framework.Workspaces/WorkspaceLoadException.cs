// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using System;
using System.Collections.Generic;

namespace Metalama.Framework.Workspaces;

[PublicAPI]
public sealed class WorkspaceLoadException : Exception
{
    public IReadOnlyList<string> Errors { get; }

    public WorkspaceLoadException( string message, IReadOnlyList<string> errors ) : base( message )
    {
        this.Errors = errors;
    }
}