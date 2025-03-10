// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.DesignTime.Rpc;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

namespace Metalama.Framework.DesignTime.Pipeline;

/// <summary>
/// Represents a version (i.e. snapshot) of a project, i.e. essentially a <see cref="Compilation"/> with preprocessed info
/// about its <see cref="SyntaxTree"/> instances represented as a set of <see cref="SyntaxTreeVersion"/>.
/// </summary>
public interface IProjectVersion
{
    /// <summary>
    /// Gets the invariant project key.
    /// </summary>
    ProjectKey ProjectKey { get; }

    bool TryGetSyntaxTreeVersion( string path, out SyntaxTreeVersion syntaxTreeVersion );

    /// <summary>
    /// Gets the compilation of the current version.
    /// </summary>
    Compilation Compilation { get; }

    /// <summary>
    /// Gets the compilations directly referenced by the current <see cref="IProjectVersion"/>.
    /// </summary>
    ImmutableDictionary<ProjectKey, IProjectVersion> ReferencedProjectVersions { get; }
}