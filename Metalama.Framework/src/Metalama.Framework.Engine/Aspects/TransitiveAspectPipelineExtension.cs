// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.Collections;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Extensibility;
using Metalama.Framework.Engine.Pipeline;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Metalama.Framework.Engine.Aspects;

internal partial class TransitiveAspectPipelineExtension : PipelineExtension
{
    public override IEnumerable<ITransitiveAspectsManifestExtension> GetTransitiveManifestExtensions( IEnumerable<ITransitivePipelineContributor> contributors )
        => contributors.OfKind( ContributorKind.TransitiveAspect ).Select( x => new SerializableTransitiveAspectInstance( x ) );

    public override IEnumerable<IPipelineContributor> GetPipelineContributorsFromTransitiveManifest(
        ImmutableArray<ITransitiveAspectsManifestExtension> extensions,
        IAspectClassResolver aspectClassResolver )
    {
        var transitiveAspectInstances = extensions.OfKind( ContributorKind.SerializableTransitiveAspect )
            .Select( i => i.ToAspectInstance( aspectClassResolver ) )
            .ToMultiValueDictionary( x => (IAspectClass) x.AspectClass, x => x );

        yield return new AspectSource( transitiveAspectInstances );
    }

    public override Task<ExtensionPipelineContributorsResult> ExecutePipelineContributorsAsync(
        AspectPipelineConfiguration pipelineConfiguration,
        IEnumerable<IPipelineContributor> contributors,
        CompilationModel initialCompilation,
        CompilationModel finalCompilation,
        CancellationToken cancellationToken )
        => Task.FromResult(
            new ExtensionPipelineContributorsResult( [..contributors.OfKind( ContributorKind.TransitiveAspect )], ImmutableUserDiagnosticList.Empty ) );
}