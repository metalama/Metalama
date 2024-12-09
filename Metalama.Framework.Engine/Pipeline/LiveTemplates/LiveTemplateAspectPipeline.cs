// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.CompileTime;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Fabrics;
using Metalama.Framework.Engine.HierarchicalOptions;
using Metalama.Framework.Engine.Licensing;
using Metalama.Framework.Engine.Pipeline.CompileTime;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.SyntaxGeneration;
using Metalama.Framework.Engine.Utilities.Threading;
using Metalama.Framework.Engine.Validation;
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
        CompileTimeDomain domain,
        Func<AspectPipelineConfiguration, IAspectClass> aspectSelector,
        ISymbol targetSymbol ) : base( serviceProvider, ExecutionScenario.LiveTemplate, domain )
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

        return new PipelineContributorSources(
            ImmutableArray.Create<IAspectSource>( new AspectSource( this, aspectClass ) ),
            ImmutableArray<IValidatorSource>.Empty,
            ImmutableArray<IHierarchicalOptionsSource>.Empty );
    }

    public static async Task<FallibleResult<PartialCompilation>> ExecuteAsync(
        ServiceProvider<IProjectService> serviceProvider,
        CompileTimeDomain domain,
        AspectPipelineConfiguration? pipelineConfiguration,
        Func<AspectPipelineConfiguration, IAspectClass> aspectSelector,
        PartialCompilation inputCompilation,
        ISymbol targetSymbol,
        IDiagnosticAdder diagnosticAdder,
        bool isComputingPreview,
        TestableCancellationToken cancellationToken = default )
    {
        LiveTemplateAspectPipeline pipeline = new( serviceProvider, domain, aspectSelector, targetSymbol );

        var result = await pipeline.ExecuteAsync( inputCompilation, diagnosticAdder, pipelineConfiguration, cancellationToken );

        if ( result.IsSuccessful )
        {
            // Enforce licensing
            var aspectClass = aspectSelector( result.Value.Configuration );

            var licenseVerifier = result.Value.Configuration.ServiceProvider.GetService<LicenseVerifier>();

            if ( !isComputingPreview && licenseVerifier != null
                                     && !licenseVerifier.VerifyCanApplyLiveTemplate( serviceProvider, aspectClass, diagnosticAdder ) )
            {
                licenseVerifier.DetectToastNotifications();

                diagnosticAdder.Report(
                    LicensingDiagnosticDescriptors.CodeActionNotAvailable.CreateRoslynDiagnostic(
                        targetSymbol.GetDiagnosticLocation(),
                        ($"Apply [{aspectClass.DisplayName}] aspect", aspectClass.DisplayName) ) );

                return default;
            }
        }

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
        => new LinkerPipelineStage( compileTimeProject, configuration.AspectLayers );

    private sealed class AspectSource : IAspectSource
    {
        private readonly LiveTemplateAspectPipeline _parent;

        public AspectSource( LiveTemplateAspectPipeline parent, IAspectClass aspectClass )
        {
            this._parent = parent;

            this.AspectClasses = ImmutableArray.Create( aspectClass );
        }

        public ImmutableArray<IAspectClass> AspectClasses { get; }

        public Task CollectAspectInstancesAsync(
            IAspectClass aspectClass,
            OutboundActionCollectionContext context )
        {
            var targetDeclaration = context.Compilation.Factory.GetDeclaration( this._parent._targetSymbol );

            context.Collector.AddAspectInstance(
                ((AspectClass) aspectClass).CreateAspectInstance(
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