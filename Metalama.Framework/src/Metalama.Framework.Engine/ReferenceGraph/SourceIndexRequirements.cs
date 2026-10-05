// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Code;
using Metalama.Framework.Engine.Extensibility;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Metalama.Framework.Engine.ReferenceGraph;

/// <summary>
/// The inputs of <see cref="PipelineExtension.GetSourceIndexRequirements"/>.
/// </summary>
/// <param name="Contributors">All extension contributors of the stage.</param>
/// <param name="HighLevelStageIndex">The zero-based index of the stage among the high-level stages of the pipeline execution.</param>
[PublicAPI]
public sealed record SourceIndexRequirementsContext( IReadOnlyCollection<IPipelineContributor> Contributors, int HighLevelStageIndex );

/// <summary>
/// The requirements of one extension for the index of the references of the source compilation of one stage.
/// </summary>
/// <param name="Requirements">The requirements, which are merged with those of the other extensions.</param>
[PublicAPI]
public sealed record SourceIndexRequirements( ImmutableArray<ReferenceIndexerRequirements> Requirements )
{
    /// <summary>
    /// Gets an instance that requires nothing.
    /// </summary>
    public static SourceIndexRequirements None { get; } = new( ImmutableArray<ReferenceIndexerRequirements>.Empty );

    /// <summary>
    /// Gets the syntax nodes of the declarations that contain every reference that the extension needs, or a default array when the extension
    /// needs the references of every syntax tree.
    /// </summary>
    /// <remarks>
    /// A root is the syntax node of a member, a type, a namespace, a compilation unit, a variable declarator of a field or an event field, or
    /// the expression body of a property or an indexer. The index covers only the union of the roots when every extension that returned
    /// requirements also returned roots. An empty array means that the extension needs the references of no declaration.
    /// </remarks>
    public ImmutableArray<SyntaxNode> DeclarationRoots { get; init; }

    /// <summary>
    /// Gets a value indicating whether the instance requires nothing: it has no requirement, or every requirement has
    /// <see cref="ReferenceKinds.None"/>.
    /// </summary>
    /// <remarks>
    /// <see cref="ReferenceIndexerRequirements.Create"/> returns <see cref="ReferenceKinds.None"/> for a request that the index cannot serve, for
    /// instance the references of a finalizer. <see cref="ReferenceIndexerOptions"/> ignores such a requirement, so an index that receives only
    /// such requirements does not walk the source compilation.
    /// </remarks>
    public bool IsEmpty => this.Requirements.IsDefaultOrEmpty || this.Requirements.All( r => r.ReferenceKinds == ReferenceKinds.None );
}
