// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Collections.Immutable;

namespace Metalama.Framework.DesignTime.Rpc.Notifications;

/// <summary>
/// Event data raised when a compilation result changes in a project.
/// </summary>
[RpcContract]
public sealed class CompilationResultChangedEventData : RpcEventData
{
    /// <summary>
    /// Gets the key of the project whose compilation result changed.
    /// </summary>
    public ProjectKey ProjectKey { get; }

    /// <summary>
    /// Gets a value indicating whether this is a partial compilation affecting only some syntax trees.
    /// </summary>
    public bool IsPartialCompilation { get; }

    /// <summary>
    /// Gets the paths of the syntax trees that were affected by the compilation change.
    /// </summary>
    public ImmutableArray<string> SyntaxTreePaths { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CompilationResultChangedEventData"/> class.
    /// </summary>
    /// <param name="projectKey">The key of the affected project.</param>
    /// <param name="isPartialCompilation">Whether this is a partial compilation.</param>
    /// <param name="syntaxTreePaths">The paths of affected syntax trees.</param>
    public CompilationResultChangedEventData( ProjectKey projectKey, bool isPartialCompilation, ImmutableArray<string> syntaxTreePaths )
    {
        this.ProjectKey = projectKey;
        this.IsPartialCompilation = isPartialCompilation;
        this.SyntaxTreePaths = syntaxTreePaths.IsDefault ? ImmutableArray<string>.Empty : syntaxTreePaths;
    }

    /// <inheritdoc />
    public override string Category => "";
}