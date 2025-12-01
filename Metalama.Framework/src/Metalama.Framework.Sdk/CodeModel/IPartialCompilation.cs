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
/// Represents a subset of a Roslyn <see cref="Microsoft.CodeAnalysis.Compilation"/>, limited to specific syntax trees.
/// This interface provides an immutable view of the compilation that can be transformed by aspect weavers.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="IPartialCompilation"/> is the primary interface for manipulating compilations in <see cref="Metalama.Framework.Engine.AspectWeavers.IAspectWeaver"/> implementations.
/// It wraps a Roslyn <see cref="Microsoft.CodeAnalysis.Compilation"/> and provides methods to transform syntax trees.
/// </para>
/// <para>
/// <b>Compile-time vs design-time behavior:</b> At compile time, this always represents the complete compilation.
/// At design time (IDE), this represents only the inheritance closure of files that have been modified and need to be recompiled,
/// which is why the <see cref="IsPartial"/> property exists.
/// </para>
/// <para>
/// Key operations:
/// </para>
/// <list type="bullet">
/// <item><description><see cref="WithSyntaxTreeTransformations"/>: Apply syntax tree modifications (add, remove, replace).</description></item>
/// <item><description><see cref="WithAdditionalResources"/>: Add managed resources to the compilation.</description></item>
/// </list>
/// <para>
/// This interface is immutable. All modification methods return a new <see cref="IPartialCompilation"/> instance.
/// </para>
/// </remarks>
/// <seealso cref="PartialCompilationExtensions"/>
/// <seealso cref="Metalama.Framework.Engine.AspectWeavers.AspectWeaverContext"/>
/// <seealso href="@aspect-weavers"/>
[PublicAPI]
public interface IPartialCompilation
{
    /// <summary>
    /// Gets the underlying Roslyn <see cref="Microsoft.CodeAnalysis.Compilation"/>.
    /// </summary>
    Compilation Compilation { get; }

    /// <summary>
    /// Gets the syntax trees in the current subset, keyed by file path.
    /// </summary>
    ImmutableDictionary<string, SyntaxTree> SyntaxTrees { get; }

    /// <summary>
    /// Gets a value indicating whether this <see cref="IPartialCompilation"/> represents a subset of the full compilation,
    /// or the complete compilation.
    /// </summary>
    /// <value><c>true</c> if this represents a partial view; <c>false</c> if it represents the complete compilation.</value>
    bool IsPartial { get; }

    /// <summary>
    /// Returns a new <see cref="IPartialCompilation"/> with the specified syntax tree transformations applied.
    /// </summary>
    /// <param name="transformations">The list of transformations to apply. Each <see cref="SyntaxTreeTransformation"/>
    /// can add, remove, or replace a syntax tree. Pass <c>null</c> or empty list to return the same instance.</param>
    /// <returns>A new <see cref="IPartialCompilation"/> with the transformations applied.</returns>
    /// <seealso cref="PartialCompilationExtensions.RewriteSyntaxTreesAsync(Metalama.Framework.Engine.CodeModel.IPartialCompilation,Microsoft.CodeAnalysis.CSharp.CSharpSyntaxRewriter,Metalama.Framework.Engine.Services.ProjectServiceProvider,System.Threading.CancellationToken)"/>
    IPartialCompilation WithSyntaxTreeTransformations( IReadOnlyList<SyntaxTreeTransformation>? transformations = null );

    /// <summary>
    /// Returns a new <see cref="IPartialCompilation"/> with additional managed resources.
    /// </summary>
    /// <param name="resources">The resources to add to the compilation output.</param>
    /// <returns>A new <see cref="IPartialCompilation"/> with the additional resources.</returns>
    IPartialCompilation WithAdditionalResources( params ManagedResource[] resources );

    /// <summary>
    /// Gets the list of managed resources for the current compilation.
    /// </summary>
    /// <remarks>
    /// This property is only available at compile time, not at design time.
    /// </remarks>
    ImmutableArray<ManagedResource> Resources { get; }

    /// <summary>
    /// Gets the named types declared in the syntax trees of this subset.
    /// </summary>
    ImmutableHashSet<INamedTypeSymbol> Types { get; }

    /// <summary>
    /// Gets the namespaces that contain types in this subset.
    /// </summary>
    ImmutableHashSet<INamespaceSymbol> Namespaces { get; }
}