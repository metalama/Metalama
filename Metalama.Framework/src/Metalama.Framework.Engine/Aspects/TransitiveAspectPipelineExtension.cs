// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Extensibility;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Metalama.Framework.Engine.Aspects;

internal class TransitiveAspectPipelineExtension : PipelineExtension
{
    public override IEnumerable<ITransitiveAspectsManifestExtension> GetTransitiveManifestExtensions( IEnumerable<ITransitivePipelineContributor> contributors )
        => contributors.OfKind( ContributorKind.TransitiveAspect );

    public override IEnumerable<IExtensionPipelineContributor> GetPipelineContributorsFromTransitiveManifest(
        ImmutableArray<ITransitiveAspectsManifestExtension> extensions )
        => extensions.OfKind( ContributorKind.TransitiveAspect );
}