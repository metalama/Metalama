// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Extensibility.CallSites;
using Metalama.Framework.Engine.Transformations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Metalama.Framework.Engine.Linking;

/// <summary>
/// Holds the input that extensions give to the linker through <see cref="ExtensionTransformationFactory"/>.
/// </summary>
internal sealed class ExtensionLinkerInput
{
    /// <summary>
    /// Gets an input that contains no redirection.
    /// </summary>
    public static ExtensionLinkerInput Empty { get; } = new( ImmutableDictionary<SyntaxTree, IReadOnlyDictionary<SyntaxNode, CallSiteRedirection>>.Empty );

    /// <summary>
    /// Initializes a new instance of the <see cref="ExtensionLinkerInput"/> class.
    /// </summary>
    public ExtensionLinkerInput(
        IReadOnlyDictionary<SyntaxTree, IReadOnlyDictionary<SyntaxNode, CallSiteRedirection>> callSiteRedirections,
        CompilationUnitSyntax? callSiteForwarders = null,
        ImmutableArray<ITransformation> transformations = default )
    {
        this.CallSiteRedirections = callSiteRedirections;
        this.CallSiteForwarders = callSiteForwarders;
        this.Transformations = transformations.IsDefault ? ImmutableArray<ITransformation>.Empty : transformations;
    }

    /// <summary>
    /// Gets the transformations that introduce the types and methods that extensions declared, and that generate the bodies of these methods.
    /// The pipeline adds them to the final compilation before the linker runs, and the linker applies them like the transformations of aspects.
    /// </summary>
    public ImmutableArray<ITransformation> Transformations { get; }

    /// <summary>
    /// Gets the requested call-site redirections, keyed by the syntax tree of the source compilation and then by the source node. The node keys are
    /// compared by reference.
    /// </summary>
    public IReadOnlyDictionary<SyntaxTree, IReadOnlyDictionary<SyntaxNode, CallSiteRedirection>> CallSiteRedirections { get; }

    /// <summary>
    /// Gets the compilation unit that declares the forwarders called by the redirected call sites in conditional accesses, or <c>null</c> when no
    /// forwarder is needed. The linker adds it to the compilation as a new syntax tree.
    /// </summary>
    public CompilationUnitSyntax? CallSiteForwarders { get; }

    /// <summary>
    /// Gets a value indicating whether the input contains no redirection and no transformation.
    /// </summary>
    public bool IsEmpty => this.CallSiteRedirections.Count == 0 && this.Transformations.IsEmpty;
}
