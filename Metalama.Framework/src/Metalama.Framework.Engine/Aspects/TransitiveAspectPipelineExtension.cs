// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Diagnostics;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code.Collections;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.Collections;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Extensibility;
using Metalama.Framework.Engine.Pipeline;
using Metalama.Framework.Engine.Services;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Metalama.Framework.Engine.Aspects;

internal partial class TransitiveAspectPipelineExtension : PipelineExtension
{
    private ILogger? _logger;

    public override bool Initialize( PipelineExtensionInitializationContext context )
    {
        this._logger = context.ServiceProvider.GetLoggerFactory().GetLogger( nameof(TransitiveAspectPipelineExtension) );

        return true;
    }

    public override IEnumerable<ITransitiveAspectsManifestExtension> GetTransitiveManifestExtensions( IEnumerable<ITransitivePipelineContributor> contributors )
        => contributors.OfKind( ContributorKind.TransitiveAspectInstance ).Select( x => new SerializableTransitiveAspectInstance( x ) );

    public override IEnumerable<IPipelineContributor> GetPipelineContributorsFromTransitiveManifest(
        ImmutableArray<ITransitiveAspectsManifestExtension> extensions,
        IAspectClassResolver aspectClassResolver )
    {
        var serializableInstances = extensions.OfKind( ContributorKind.SerializableTransitiveAspectInstance );

        var transitiveAspectInstancesBuilder = ImmutableDictionaryOfArray<IAspectClass, AspectInstance>.CreateBuilder();

        foreach ( var instance in serializableInstances )
        {
            var aspectInstance = instance.ToAspectInstance( aspectClassResolver );

            if ( aspectInstance != null )
            {
                transitiveAspectInstancesBuilder.Add( aspectInstance.AspectClass, aspectInstance );
            }
            else
            {
                this._logger?.Warning?.Log(
                    $"Cannot create a transitive aspect instance for aspect class '{instance.AspectClassName}'. " +
                    $"The aspect class was not found. This can happen when the referenced assembly was compiled with a different version of Metalama." );
            }
        }

        yield return new AspectSource( transitiveAspectInstancesBuilder.ToImmutable() );
    }

    public override Task<ExtensionPipelineContributorsResult> ExecutePipelineContributorsAsync(
        AspectPipelineConfiguration pipelineConfiguration,
        IEnumerable<IPipelineContributor> contributors,
        CompilationModel initialCompilation,
        CompilationModel finalCompilation,
        CancellationToken cancellationToken )
        => Task.FromResult(
            new ExtensionPipelineContributorsResult(
                contributors.OfKind( ContributorKind.TransitiveAspectInstance ).ToImmutableArray<ITransitivePipelineContributor>(),
                ImmutableUserDiagnosticList.Empty ) );

    public override Task<ExtensionPipelineContributorsResult> ExecuteDesignTimePipelineContributorsAsync(
        AspectPipelineConfiguration pipelineConfiguration,
        IEnumerable<IPipelineContributor> contributors,
        CompilationModel initialCompilation,
        CompilationModel finalCompilation,
        CancellationToken cancellationToken )
        => Task.FromResult(
            new ExtensionPipelineContributorsResult(
                contributors.OfKind( ContributorKind.TransitiveAspectInstance ).ToImmutableArray<ITransitivePipelineContributor>(),
                ImmutableUserDiagnosticList.Empty ) );
}