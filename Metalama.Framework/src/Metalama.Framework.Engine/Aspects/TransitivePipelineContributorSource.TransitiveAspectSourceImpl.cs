// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Engine.Collections;
using Metalama.Framework.Engine.Extensibility;
using System.Collections.Immutable;
using System.Threading.Tasks;

namespace Metalama.Framework.Engine.Aspects;

internal sealed partial class TransitivePipelineContributorSource
{
    private sealed class TransitiveAspectSourceImpl : IAspectSource
    {
        private readonly ImmutableDictionaryOfArray<IAspectClass, TransitiveAspectInstance> _transitiveAspects;

        public TransitiveAspectSourceImpl( ImmutableDictionaryOfArray<IAspectClass, TransitiveAspectInstance> transitiveAspects )
        {
            this._transitiveAspects = transitiveAspects;
        }

        public ImmutableArray<IAspectClass> AspectClasses => this._transitiveAspects.Keys.ToImmutableArray();

        public Task CollectAspectInstancesAsync( AspectInstanceCollector collector )
        {
            var aspectClass = (AspectClass) collector.AspectClass;

            foreach ( var aspectInstance in this._transitiveAspects[aspectClass] )
            {
                collector.AddAspectInstance(
                    new AspectInstance(
                        aspectInstance.Aspect,
                        aspectInstance.TargetDeclaration.GetTarget( collector.Compilation ),
                        aspectClass,
                        ImmutableArray<AspectPredecessor>.Empty ) );
            }

            return Task.CompletedTask;
        }

        public ContributorKind ContributorKind => ContributorKind.AspectSource;
    }
}