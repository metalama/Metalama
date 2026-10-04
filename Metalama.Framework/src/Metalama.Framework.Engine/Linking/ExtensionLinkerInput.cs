// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Extensibility.CallSites;
using Microsoft.CodeAnalysis;
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
    public ExtensionLinkerInput( IReadOnlyDictionary<SyntaxTree, IReadOnlyDictionary<SyntaxNode, CallSiteRedirection>> callSiteRedirections )
    {
        this.CallSiteRedirections = callSiteRedirections;
    }

    /// <summary>
    /// Gets the requested call-site redirections, keyed by the syntax tree of the source compilation and then by the source node. The node keys are
    /// compared by reference.
    /// </summary>
    public IReadOnlyDictionary<SyntaxTree, IReadOnlyDictionary<SyntaxNode, CallSiteRedirection>> CallSiteRedirections { get; }

    /// <summary>
    /// Gets a value indicating whether the input contains no redirection.
    /// </summary>
    public bool IsEmpty => this.CallSiteRedirections.Count == 0;
}
