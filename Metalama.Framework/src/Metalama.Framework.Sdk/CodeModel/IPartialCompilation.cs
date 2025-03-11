// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Compiler;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Metalama.Framework.Engine.CodeModel;

/// <summary>
/// Represents a subset of a Roslyn <see cref="Microsoft.CodeAnalysis.Compilation"/>. The subset is limited
/// to specific syntax trees.
/// </summary>
/// <seealso cref="PartialCompilationExtensions"/>
[PublicAPI]
public interface IPartialCompilation
{
    /// <summary>
    /// Gets the Roslyn <see cref="Microsoft.CodeAnalysis.Compilation"/>.
    /// </summary>
    Compilation Compilation { get; }

    /// <summary>
    /// Gets the list of syntax trees in the current subset.
    /// </summary>
    ImmutableDictionary<string, SyntaxTree> SyntaxTrees { get; }

    /// <summary>
    /// Gets a value indicating whether the current <see cref="IPartialCompilation"/> is actually partial, or represents a complete compilation.
    /// </summary>
    bool IsPartial { get; }

    /// <summary>
    /// Returns a copy of the current <see cref="IPartialCompilation"/> where the <see cref="SyntaxTrees"/> have been modified.
    /// </summary>
    /// <seealso cref="PartialCompilationExtensions.RewriteSyntaxTreesAsync(Metalama.Framework.Engine.CodeModel.IPartialCompilation,Microsoft.CodeAnalysis.CSharp.CSharpSyntaxRewriter,Metalama.Framework.Engine.Services.ProjectServiceProvider,System.Threading.CancellationToken)"/>
    IPartialCompilation WithSyntaxTreeTransformations( IReadOnlyList<SyntaxTreeTransformation>? transformations = null );

    /// <summary>
    /// Returns a copy of the current <see cref="IPartialCompilation"/> where the <see cref="Resources"/> have been modified.
    /// </summary>
    IPartialCompilation WithAdditionalResources( params ManagedResource[] resources );

    /// <summary>
    /// Gets the list of managed resources for the current compilation. This property is not defined at the design time, only at compile time.
    /// </summary>
    ImmutableArray<ManagedResource> Resources { get; }

    /// <summary>
    /// Gets the types declared in the current subset.
    /// </summary>
    ImmutableHashSet<INamedTypeSymbol> Types { get; }

    /// <summary>
    /// Gets the namespaces that contain types.
    /// </summary>
    ImmutableHashSet<INamespaceSymbol> Namespaces { get; }
}