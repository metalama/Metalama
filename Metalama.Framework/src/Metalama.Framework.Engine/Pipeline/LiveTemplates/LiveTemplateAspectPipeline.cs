// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.CompileTime;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Extensibility;
using Metalama.Framework.Engine.Pipeline.CompileTime;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.SyntaxGeneration;
using Metalama.Framework.Engine.Utilities.Threading;
using Metalama.Framework.Services;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace Metalama.Framework.Engine.Pipeline.LiveTemplates;

/// <summary>
/// An implementation of the <see cref="AspectPipeline"/> that applies an aspect to source code in the interactive process.
/// </summary>
public sealed class LiveTemplateAspectPipeline : AspectPipeline
{
    private readonly Func<AspectPipelineConfiguration, IAspectClass> _aspectSelector;
    private readonly ISymbol _targetSymbol;

    private LiveTemplateAspectPipeline(
        ServiceProvider<IProjectService> serviceProvider,
        Func<AspectPipelineConfiguration, IAspectClass> aspectSelector,
        ISymbol targetSymbol ) : base( serviceProvider, ExecutionScenario.LiveTemplate )
    {
        this._aspectSelector = aspectSelector;
        this._targetSymbol = targetSymbol;
    }

    protected override SyntaxGenerationOptions GetSyntaxGenerationOptions() => SyntaxGenerationOptions.Formatted;

    private protected override PipelineContributorSources CreatePipelineContributorSources(
        AspectPipelineConfiguration configuration,
        CompilationContext compilationContext,
        CancellationToken cancellationToken )
    {
        var aspectClass = this._aspectSelector( configuration );

        return new PipelineContributorSources( ImmutableArray.Create<IPipelineContributor>( new AspectSource( this, aspectClass ) ) );
    }

    public static async Task<FallibleResult<PartialCompilation>> ExecuteAsync(
        ServiceProvider<IProjectService> serviceProvider,
        AspectPipelineConfiguration? pipelineConfiguration,
        Func<AspectPipelineConfiguration, IAspectClass> aspectSelector,
        PartialCompilation inputCompilation,
        ISymbol targetSymbol,
        IDiagnosticAdder diagnosticAdder,
        TestableCancellationToken cancellationToken = default )
    {
        LiveTemplateAspectPipeline pipeline = new( serviceProvider, aspectSelector, targetSymbol );

        var result = await pipeline.ExecuteAsync( inputCompilation, diagnosticAdder, pipelineConfiguration, cancellationToken );

        if ( !result.IsSuccessful )
        {
            return default;
        }
        else
        {
            return result.Value.LastCompilation;
        }
    }

    private protected override HighLevelPipelineStage CreateHighLevelStage(
        PipelineStageConfiguration configuration,
        CompileTimeProject compileTimeProject )
        => new LinkerPipelineStage( configuration.AspectLayers );

    private sealed class AspectSource : IAspectSource
    {
        private readonly LiveTemplateAspectPipeline _parent;

        public AspectSource( LiveTemplateAspectPipeline parent, IAspectClass aspectClass )
        {
            this._parent = parent;

            this.AspectClasses = ImmutableArray.Create( aspectClass );
        }

        public ImmutableArray<IAspectClass> AspectClasses { get; }

        public Task CollectAspectInstancesAsync( AspectInstanceCollector collector )
        {
            var targetDeclaration = collector.Compilation.Factory.GetDeclaration( this._parent._targetSymbol );

            var aspectClass = (AspectClass) collector.AspectClass;

            collector.AddAspectInstance(
                aspectClass.CreateAspectInstance(
                    targetDeclaration,
                    (IAspect) Activator.CreateInstance( this.AspectClasses[0].Type ).AssertNotNull(),
                    new AspectPredecessor(
                        AspectPredecessorKind.Interactive,
                        new LiveTemplatePredecessor( targetDeclaration.ToRef() ) ) ) );

            return Task.CompletedTask;
        }
    }

    private sealed class LiveTemplatePredecessor : IAspectPredecessor
    {
        public LiveTemplatePredecessor( IRef<IDeclaration> targetDeclaration )
        {
            this.TargetDeclaration = targetDeclaration;
        }

        public int PredecessorDegree => 0;

        public IRef<IDeclaration> TargetDeclaration { get; }

        public ImmutableArray<AspectPredecessor> Predecessors => ImmutableArray<AspectPredecessor>.Empty;
    }
}