// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.Comparers;
using Metalama.Framework.Engine.AdditionalOutputs;
using Metalama.Framework.Engine.AspectOrdering;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.Collections;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Extensibility;
using Metalama.Framework.Engine.Transformations;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Metalama.Framework.Engine.Pipeline
{
    /// <summary>
    /// Contains the inputs and the outputs of a <see cref="PipelineStage"/>.
    /// </summary>
    [PublicAPI]
    public sealed class AspectPipelineResult
    {
        public ProjectModel Project { get; }

        /// <summary>
        /// Gets the resulting Roslyn compilation.
        /// </summary>
        public PartialCompilation LastCompilation { get; }

        /// <summary>
        /// Gets the set of diagnostics.
        /// </summary>
        public ImmutableUserDiagnosticList Diagnostics { get; }

        public PipelineContributorSources ContributorSources { get; }

        /// <summary>
        /// Gets the list of syntax trees to be added to the compilation (typically in a source generation scenario). The key is the "hint name" in the
        /// source generator API.
        /// </summary>
        public ImmutableArray<IntroducedSyntaxTree> AdditionalSyntaxTrees { get; }

        /// <summary>
        /// Gets the list of ordered aspect parts.
        /// </summary>
        internal ImmutableArray<OrderedAspectLayer> AspectLayers { get; }

        public ImmutableArray<IAspectInstance> ExternallyInheritableAspects { get; }

        public ImmutableDictionaryOfArray<IRef<IDeclaration>, AnnotationInstance> Annotations { get; }

        public ImmutableArray<ITransitivePipelineContributor> TransitiveContributors { get; }

        public CompilationModel? FirstCompilationModel { get; }

        public CompilationModel? LastCompilationModelOrNull { get; private set; }

        public CompilationModel LastCompilationModel
        {
            get
            {
                var firstCompilationModel = this.FirstCompilationModel.AssertNotNull();

                return this.LastCompilationModelOrNull ??=
                    CompilationModel.CreateInitialInstance(
                        firstCompilationModel.Project,
                        this.LastCompilation,
                        firstCompilationModel.AspectRepository,
                        firstCompilationModel.HierarchicalOptionsManager,
                        this.Annotations,
                        firstCompilationModel.ExternalAnnotationProvider );
            }
        }

        internal ImmutableArray<AspectInstanceResult> AspectInstanceResults { get; }

        public IReadOnlyList<IAspectInstance> AspectInstances => this.AspectInstanceResults.SelectAsImmutableArray( x => x.AspectInstance );

        public ImmutableArray<AdditionalCompilationOutputFile> AdditionalCompilationOutputFiles { get; }

        public ImmutableArray<ITransformationBase> Transformations { get; }

        public AspectPipelineConfiguration Configuration { get; }

        // Creates an empty instance, when there is no aspect in the project.
        internal AspectPipelineResult(
            PartialCompilation lastCompilation,
            ProjectModel project,
            AspectPipelineConfiguration configuration )
        {
            this.LastCompilation = lastCompilation;
            this.Diagnostics = ImmutableUserDiagnosticList.Empty;
            this.ContributorSources = PipelineContributorSources.Empty;
            this.AspectLayers = ImmutableArray<OrderedAspectLayer>.Empty;
            this.AspectInstanceResults = ImmutableArray<AspectInstanceResult>.Empty;
            this.ExternallyInheritableAspects = ImmutableArray<IAspectInstance>.Empty;
            this.TransitiveContributors = ImmutableArray<ITransitivePipelineContributor>.Empty;
            this.Project = project;
            this.Configuration = configuration;
            this.AdditionalSyntaxTrees = ImmutableArray<IntroducedSyntaxTree>.Empty;
            this.AdditionalCompilationOutputFiles = ImmutableArray<AdditionalCompilationOutputFile>.Empty;
            this.Transformations = ImmutableArray<ITransformationBase>.Empty;
            this.Annotations = ImmutableDictionaryOfArray<IRef<IDeclaration>, AnnotationInstance>.Empty;
        }

        internal AspectPipelineResult(
            PartialCompilation lastCompilation,
            ProjectModel project,
            ImmutableArray<OrderedAspectLayer> aspectLayers,
            CompilationModel firstCompilationModel,
            CompilationModel? lastCompilationModel, // Nullable because it can be created lazily.
            AspectPipelineConfiguration configuration,
            ImmutableUserDiagnosticList? diagnostics = null,
            PipelineContributorSources? sources = null,
            ImmutableArray<IAspectInstance> inheritableAspectInstances = default,
            ImmutableDictionaryOfArray<IRef<IDeclaration>, AnnotationInstance>? annotations = null,
            ImmutableArray<ITransitivePipelineContributor> extensions = default,
            ImmutableArray<IntroducedSyntaxTree> additionalSyntaxTrees = default,
            ImmutableArray<AspectInstanceResult> aspectInstanceResults = default,
            ImmutableArray<AdditionalCompilationOutputFile> additionalCompilationOutputFiles = default,
            ImmutableArray<ITransformationBase> transformations = default )
        {
            if ( lastCompilationModel != null && lastCompilation != lastCompilationModel.PartialCompilation )
            {
                throw new AssertionFailedException( "Compilation mismatch." );
            }

            this.LastCompilation = lastCompilation;
            this.Diagnostics = diagnostics ?? ImmutableUserDiagnosticList.Empty;
            this.ContributorSources = sources ?? PipelineContributorSources.Empty;
            this.AspectLayers = aspectLayers;
            this.FirstCompilationModel = firstCompilationModel.AssertNotNull();
            this.LastCompilationModelOrNull = lastCompilationModel;
            this.Configuration = configuration;
            this.AspectInstanceResults = aspectInstanceResults.IsDefault ? ImmutableArray<AspectInstanceResult>.Empty : aspectInstanceResults;
            this.ExternallyInheritableAspects = inheritableAspectInstances.IsDefault ? ImmutableArray<IAspectInstance>.Empty : inheritableAspectInstances;

            this.Annotations = annotations
                               ?? ImmutableDictionaryOfArray<IRef<IDeclaration>, AnnotationInstance>.Create( RefEqualityComparer<IDeclaration>.Default );

            this.TransitiveContributors =
                extensions.IsDefault ? ImmutableArray<ITransitivePipelineContributor>.Empty : extensions;

            this.Project = project;
            this.AdditionalSyntaxTrees = additionalSyntaxTrees.IsDefault ? ImmutableArray<IntroducedSyntaxTree>.Empty : additionalSyntaxTrees;

            this.AdditionalCompilationOutputFiles = additionalCompilationOutputFiles.IsDefault
                ? ImmutableArray<AdditionalCompilationOutputFile>.Empty
                : additionalCompilationOutputFiles;

            this.Transformations = transformations.IsDefault ? ImmutableArray<ITransformationBase>.Empty : transformations;

#if DEBUG
            if ( this.AdditionalSyntaxTrees.GroupBy( x => x.Name ).Any( g => g.Count() > 1 ) )
            {
                throw new AssertionFailedException( "Duplicate item in AdditionalSyntaxTrees." );
            }
#endif
        }

        public AspectPipelineResult WithAdditionalDiagnostics( ImmutableUserDiagnosticList diagnostics )
            => new(
                this.LastCompilation,
                this.Project,
                this.AspectLayers,
                this.FirstCompilationModel.AssertNotNull(),
                this.LastCompilationModel.AssertNotNull(),
                this.Configuration,
                this.Diagnostics.Concat( diagnostics ),
                this.ContributorSources,
                this.ExternallyInheritableAspects,
                this.Annotations,
                this.TransitiveContributors,
                this.AdditionalSyntaxTrees,
                this.AspectInstanceResults,
                this.AdditionalCompilationOutputFiles,
                this.Transformations );
    }
}