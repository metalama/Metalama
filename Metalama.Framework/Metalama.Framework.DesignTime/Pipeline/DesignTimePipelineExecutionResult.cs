// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.Collections;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Extensibility;
using Metalama.Framework.Engine.HierarchicalOptions;
using Metalama.Framework.Engine.Pipeline;
using Metalama.Framework.Engine.Transformations;
using Metalama.Framework.Options;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

namespace Metalama.Framework.DesignTime.Pipeline
{
    /// <summary>
    /// Results produced by <see cref="DesignTimeAspectPipeline"/>.
    /// </summary>
    /// <param name="InputSyntaxTrees">The syntax trees for which the pipeline was executed.</param>
    /// <param name="IntroducedSyntaxTrees">The syntax trees introduced by the pipeline (for source generators).</param>
    /// <param name="Diagnostics">The list of diagnostics and suppressions.</param>
    internal sealed record DesignTimePipelineExecutionResult(
        ImmutableDictionary<string, SyntaxTree> InputSyntaxTrees,
        IReadOnlyList<IntroducedSyntaxTree> IntroducedSyntaxTrees,
        ImmutableUserDiagnosticList Diagnostics,
        IReadOnlyList<InheritableAspectInstance> InheritableAspects,
        IReadOnlyList<KeyValuePair<HierarchicalOptionsKey, IHierarchicalOptions>> InheritableOptions,
        ImmutableArray<ITransitivePipelineContributor> Extensions,
        ImmutableArray<IAspectInstance> AspectInstances,
        IReadOnlyCollection<ITransformationBase> Transformations,
        ImmutableDictionaryOfArray<IRef<IDeclaration>, AnnotationInstance> Annotations );
}