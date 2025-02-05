// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.DesignTime.Rpc;
using Metalama.Framework.Engine;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.DesignTime;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Pipeline.DesignTime;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities.Threading;

namespace Metalama.Framework.DesignTime.Preview;

public sealed class TransformationPreviewServiceImpl : PreviewPipelineBasedService, ITransformationPreviewServiceImpl
{
    public TransformationPreviewServiceImpl( GlobalServiceProvider serviceProvider ) : base( serviceProvider ) { }

    internal async Task<SerializablePreviewTransformationResult> PreviewTransformationAsync(
        ProjectKey projectKey,
        string syntaxTreeName,
        TestableCancellationToken cancellationToken = default )
    {
        var preparation = await this.PrepareExecutionAsync( projectKey, syntaxTreeName, cancellationToken );

        if ( !preparation.Success )
        {
            return SerializablePreviewTransformationResult.Failure( preparation.ErrorMessages ?? Array.Empty<string>() );
        }

        // Execute the compile-time pipeline with the design-time project configuration.
        var previewPipeline = new PreviewAspectPipeline(
            preparation.ServiceProvider.AssertNotNull(),
            ExecutionScenario.Preview );

        DiagnosticBag diagnostics = new();

        var pipelineResult = await previewPipeline.ExecutePreviewAsync(
            diagnostics,
            preparation.PartialCompilation!,
            preparation.Configuration!,
            cancellationToken );

        var errorMessages = FormatErrors( diagnostics );

        if ( !pipelineResult.IsSuccessful || errorMessages.Length > 0 )
        {
            return SerializablePreviewTransformationResult.Failure( errorMessages );
        }

        var transformedSyntaxTree = pipelineResult.Value.SyntaxTrees[syntaxTreeName];

        return SerializablePreviewTransformationResult.Success( JsonSerializationHelper.CreateSerializableSyntaxTree( transformedSyntaxTree ), null );
    }

    public Task<SerializablePreviewTransformationResult> PreviewTransformationAsync(
        ProjectKey projectKey,
        string syntaxTreeName,
        CancellationToken cancellationToken )
        => this.PreviewTransformationAsync( projectKey, syntaxTreeName, cancellationToken.ToTestable() );
}