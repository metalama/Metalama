// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.AspectWeavers;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Utilities.Threading;
using Metalama.Framework.Engine.Utilities.UserCode;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace Metalama.Framework.Engine.Pipeline;

/// <summary>
/// A <see cref="PipelineStage"/> that has a single aspect backed by a low-level <see cref="IAspectWeaver"/>.
/// </summary>
internal sealed class LowLevelPipelineStage : PipelineStage
{
    private readonly IAspectWeaver _aspectWeaver;
    private readonly AspectClass _aspectClass;

    public LowLevelPipelineStage( IAspectWeaver aspectWeaver, IBoundAspectClass aspectClass )
    {
        this._aspectWeaver = aspectWeaver;
        this._aspectClass = (AspectClass) aspectClass;
    }

    /// <inheritdoc/>
    public override async Task<FallibleResult<AspectPipelineResult>> ExecuteAsync(
        AspectPipelineConfiguration pipelineConfiguration,
        AspectPipelineResult input,
        IDiagnosticAdder diagnostics,
        TestableCancellationToken cancellationToken )
    {
        var compilationModel = input.LastCompilationModel;

        var collector = new AspectInstanceCollector( this._aspectClass, compilationModel, diagnostics, cancellationToken );

        await Task.WhenAll(
            input.ContributorSources.Contributors.OfType<IAspectSource>()
                .Select( s => s.CollectAspectInstancesAsync( collector ) ) );

        var aspectInstances = collector.AspectInstances
            .GroupBy(
                i => i.TargetDeclaration.GetSymbol( compilationModel.RoslynCompilation )
                    .AssertNotNull( "The Roslyn compilation should include all introduced declarations." ) )
            .ToImmutableDictionary( g => g.Key, g => (IAspectInstance) AggregateAspectInstance.GetInstance( g ) );

        if ( !aspectInstances.Any() )
        {
            return input;
        }

        var projectServiceProvider = pipelineConfiguration.ServiceProvider;

        var context = new AspectWeaverContext(
            this._aspectClass,
            aspectInstances,
            input.LastCompilation,
            diagnostics.Report,
            pipelineConfiguration.ServiceProvider.Underlying,
            input.Project,
            this._aspectClass.GeneratedCodeAnnotation,
            compilationModel.CompilationContext,
            compilationModel.Factory,
            compilationModel.HierarchicalOptionsManager.AssertNotNull(),
            cancellationToken );

        var executionContext = UserCodeExecutionContext.CreateInstance(
            projectServiceProvider,
            UserCodeDescription.Create( "calling the TransformAsync method for the weaver {0}", this._aspectWeaver.GetType() ),
            compilationModel,
            diagnostics: diagnostics );

        var userCodeInvoker = projectServiceProvider.GetRequiredService<UserCodeInvoker>();
        var success = await userCodeInvoker.TryInvokeAsync( () => this._aspectWeaver.TransformAsync( context ), executionContext );

        if ( !success )
        {
            return default;
        }

        var newCompilation = (PartialCompilation) context.Compilation;

        // TODO: update AspectCompilation.Aspects and CompilationModels
        // (the problem here is that we don't necessarily need CompilationModels after a low-level pipeline, because
        // they are supposed to be "unmanaged" at the end of the pipeline. Currently this condition is not properly enforced,
        // and we don't test what happens when a low-level stage is before a high-level stage).
        var newCompilationModel = newCompilation == compilationModel.PartialCompilation ? compilationModel : null;

        return new AspectPipelineResult(
            newCompilation,
            input.Project,
            input.AspectLayers,
            input.FirstCompilationModel.AssertNotNull(),
            newCompilationModel,
            input.Configuration,
            input.Diagnostics,
            input.ContributorSources,
            aspectInstanceResults: aspectInstances.SelectAsImmutableArray(
                x => new AspectInstanceResult(
                    x.Value,
                    AdviceOutcome.Default,
                    default,
                    default,
                    default ) ) );
    }
}