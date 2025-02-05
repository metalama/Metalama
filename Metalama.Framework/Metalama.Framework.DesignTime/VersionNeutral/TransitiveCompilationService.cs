// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.DesignTime.Contracts.Pipeline;
using Metalama.Framework.DesignTime.Pipeline;
using Metalama.Framework.Engine.Pipeline.DesignTime;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities.Threading;
using Microsoft.CodeAnalysis;

namespace Metalama.Framework.DesignTime.VersionNeutral;

internal sealed class TransitiveCompilationService : ITransitiveCompilationService
{
    private readonly DesignTimeAspectPipelineFactory _pipelineFactory;

    public TransitiveCompilationService( GlobalServiceProvider serviceProvider )
    {
        this._pipelineFactory = serviceProvider.GetRequiredService<DesignTimeAspectPipelineFactory>();
    }

    public async ValueTask GetTransitiveAspectManifestAsync(
        Compilation compilation,
        ITransitiveCompilationResult?[] result,
        CancellationToken cancellationToken )
    {
        // Get the pipeline.
        var pipeline = await this._pipelineFactory.GetPipelineAndWaitAsync( compilation, cancellationToken );

        if ( pipeline == null )
        {
            result[0] = TransitiveCompilationResult.Failed( Array.Empty<Diagnostic>() );

            return;
        }

        // Execute the pipeline.
        var pipelineResult = await pipeline.ExecuteAsync( compilation, AsyncExecutionContext.Get(), cancellationToken.ToTestable() );

        if ( !pipelineResult.IsSuccessful )
        {
            result[0] = TransitiveCompilationResult.Failed( pipelineResult.Diagnostics.ToArray() );
        }
        else
        {
            var pipelineConfiguration = pipelineResult.Value.Configuration;
            var projectServiceProvider = pipelineConfiguration.ServiceProvider;

            result[0] = TransitiveCompilationResult.Success(
                pipelineResult.Value.Status == DesignTimeAspectPipelineStatus.Paused,
                pipelineResult.Value.Result.GetSerializedTransitiveAspectManifest( projectServiceProvider, compilation.GetCompilationContext() ) );
        }
    }
}