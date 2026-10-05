// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.Extensibility;
using Metalama.Framework.Engine.Services;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Metalama.Framework.Engine.ReferenceGraph;

/// <summary>
/// Builds the index of the references of the source compilation once per pipeline execution, in the source stage, from the merged requirements
/// of all extensions, so that the extensions share the walk of the syntax and the binding of member bodies.
/// </summary>
/// <remarks>
/// <para>
/// The class holds no state that belongs to a pipeline execution, because several pipelines can use one configuration at the same time. The
/// index of a pipeline execution is a <see cref="SourceReferenceIndex"/>, which the pipeline passes to the extensions through the context
/// of their transforming hook.
/// </para>
/// <para>
/// The methods receive the service provider of the pipeline execution. A project service that stored it would be cached in the project service
/// provider, which outlives a compilation, and would retain the compilation-bound services of that execution.
/// </para>
/// </remarks>
[PublicAPI]
public static class SourceReferenceIndexService
{
    /// <summary>
    /// The design-time indexes, keyed by semantic model. An entry is collected with its semantic model.
    /// </summary>
    private static readonly ConditionalWeakTable<SemanticModel, InboundReferenceIndex> _designTimeIndexes = new();

    /// <summary>
    /// Starts the index of the source stage, which is the first high-level stage of the pipeline execution. The index is not built until an
    /// extension reads it.
    /// </summary>
    /// <param name="serviceProvider">The service provider of the pipeline execution.</param>
    /// <param name="sourceCompilation">The source compilation of the pipeline.</param>
    /// <param name="requirements">The requirements that each extension returned.</param>
    /// <exception cref="ArgumentException">A declaration root does not belong to a syntax tree of <paramref name="sourceCompilation"/>.</exception>
    /// <remarks>
    /// The index covers <paramref name="sourceCompilation"/>, which is the source compilation of the pipeline. The declaration roots must
    /// therefore be nodes of the source compilation. The stages that follow a low-level weaver create no index.
    /// </remarks>
    internal static SourceReferenceIndex Create(
        in ProjectServiceProvider serviceProvider,
        CompilationModel sourceCompilation,
        IEnumerable<SourceIndexRequirements> requirements )
    {
        var nonEmptyRequirements = requirements.Where( r => !r.IsEmpty ).ToList();

        var options = new ReferenceIndexerOptions( nonEmptyRequirements.SelectMany( r => r.Requirements ) );

        // The walk is restricted to the declaration roots only when every extension that needs references gave roots.
        var rootsByTree = nonEmptyRequirements.Count > 0 && nonEmptyRequirements.All( r => r.DeclarationRoots != null )
            ? SourceReferenceIndex.MergeRoots( nonEmptyRequirements.SelectMany( r => r.DeclarationRoots!.Value ) )
            : null;

        // A root of another syntax tree would make a task of the build fail when it gets the semantic model of the tree.
        if ( rootsByTree != null )
        {
            foreach ( var syntaxTree in rootsByTree.Keys )
            {
                if ( !sourceCompilation.RoslynCompilation.ContainsSyntaxTree( syntaxTree ) )
                {
                    throw new ArgumentException(
                        $"A declaration root belongs to the syntax tree '{syntaxTree.FilePath}', which is not a syntax tree of the source compilation.",
                        nameof(requirements) );
                }
            }
        }

        return new SourceReferenceIndex( serviceProvider, sourceCompilation, options, nonEmptyRequirements.Count > 0, rootsByTree );
    }

    /// <summary>
    /// Returns the index of the references of one semantic model at design time.
    /// </summary>
    /// <remarks>
    /// The index is built once per <see cref="SemanticModel"/> object with <see cref="DesignTimeAspectPipelineResultExtensionCollection.IndexOptions"/>,
    /// and cached in a table whose entries are collected with the semantic model, so every extension that analyzes the same semantic model reads
    /// the same index.
    /// The cache is keyed by the semantic model only, although the index depends on the options. A semantic model belongs to one compilation of
    /// one project, and the design-time pipeline gives the same extension collection to every analysis of that compilation, so a second call
    /// with other options does not happen in practice. A caller that passes other options for the same semantic model receives the index of
    /// the first call.
    /// </remarks>
    public static InboundReferenceIndex GetDesignTimeIndex(
        in ProjectServiceProvider serviceProvider,
        SemanticModel semanticModel,
        DesignTimeAspectPipelineResultExtensionCollection extensions,
        CancellationToken cancellationToken )
    {
        if ( _designTimeIndexes.TryGetValue( semanticModel, out var index ) )
        {
            return index;
        }

        var builder = new InboundReferenceIndexBuilder( serviceProvider, extensions.IndexOptions, SymbolEqualityComparer.Default );
        builder.IndexSemanticModel( semanticModel, cancellationToken );
        index = builder.ToReadOnly();

        // Another thread may have added an index for the same semantic model. Both are equivalent, and the one in the table is returned.
        return _designTimeIndexes.GetValue( semanticModel, _ => index );
    }
}
