// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Diagnostics;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Extensibility;
using Metalama.Framework.Engine.Pipeline;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Metalama.Framework.Engine.Queries.Diagnostics;

internal sealed class DiagnosticQueryPipelineExtension : PipelineExtension
{
    public override bool Initialize( PipelineExtensionInitializationContext context )
    {
        context.ServiceBuilder.Add<IDiagnosticsQueryService>( _ => new DiagnosticsQueryService() );

        return true;
    }

    public override async Task<ExtensionPipelineContributorsResult> ExecutePipelineContributorsAsync(
        AspectPipelineConfiguration pipelineConfiguration,
        IEnumerable<IPipelineContributor> contributors,
        CompilationModel initialCompilation,
        CompilationModel finalCompilation,
        CancellationToken cancellationToken )
    {
        var diagnostics = new UserDiagnosticSink( pipelineConfiguration.ServiceProvider );

        await Task.WhenAll(
            contributors.OfKind( ContributorKind.DiagnosticSource )
                .Select( source => source.CollectDiagnosticsAsync( finalCompilation, diagnostics, cancellationToken ) ) );

        return new ExtensionPipelineContributorsResult( ImmutableArray<ITransitivePipelineContributor>.Empty, diagnostics.ToImmutable() );
    }

    /// <summary>
    /// Evaluates the diagnostic queries of <see cref="DesignTimeContributorsContext.NewContributors"/> on the final compilation of the stage.
    /// </summary>
    /// <remarks>
    /// The design-time pipeline accumulates the diagnostics of all the high-level stages. A contributor that is replayed in a later stage, for instance
    /// the contributor of a fabric when a low-level weaver splits the pipeline, is therefore evaluated only in the first stage, so that its
    /// diagnostics are reported once. The queries of the replayed contributors do not see the declarations that the later stages introduce.
    /// </remarks>
    public override Task<ExtensionPipelineContributorsResult> ExecuteDesignTimePipelineContributorsAsync(
        DesignTimeContributorsContext context,
        CancellationToken cancellationToken )
        => this.ExecutePipelineContributorsAsync(
            context.PipelineConfiguration,
            context.NewContributors,
            context.StageInitialCompilation,
            context.StageFinalCompilation,
            cancellationToken );
}