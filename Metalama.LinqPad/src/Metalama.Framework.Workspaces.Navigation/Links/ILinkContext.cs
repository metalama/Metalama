// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using System.Diagnostics.CodeAnalysis;

namespace Metalama.Framework.Workspaces.Navigation.Links;

/// <summary>
/// Connects links to the loaded workspaces: it maps a compilation to the scope of a link, and a scope back to a compilation.
/// </summary>
internal interface ILinkContext
{
    /// <summary>
    /// Gets the workspace, scope and code version of a compilation of the code model.
    /// </summary>
    bool TryGetScope( ICompilation compilation, out string? workspace, [NotNullWhen( true )] out string? scope, out bool isTransformed );

    /// <summary>
    /// Gets the link of an object that is neither a declaration nor a type but has a stable anchor of its own,
    /// such as a project or a workspace.
    /// </summary>
    Link? GetObjectAnchor( object value );

    /// <summary>
    /// Gets the current fingerprint of a workspace.
    /// </summary>
    string? GetFingerprint( string? workspace );

    bool TryGetCompilation( string? workspace, string scope, bool isTransformed, [NotNullWhen( true )] out ICompilation? compilation, [NotNullWhen( false )] out string? error );

    /// <summary>
    /// Resolves an anchor that is a collection name, such as <c>projects</c> or <c>diagnostics</c>.
    /// </summary>
    bool TryGetCollectionAnchor( string? workspace, string scope, string name, out object? value, [NotNullWhen( false )] out string? error );
}
