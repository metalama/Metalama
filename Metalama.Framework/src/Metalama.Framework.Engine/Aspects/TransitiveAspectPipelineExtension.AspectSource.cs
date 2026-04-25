// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Collections;
using Metalama.Framework.Engine.Extensibility;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Metalama.Framework.Engine.Aspects;

internal sealed partial class TransitiveAspectPipelineExtension
{
    private sealed class AspectSource : IAspectSource
    {
        private readonly ImmutableDictionaryOfArray<IAspectClass, AspectInstance> _transitiveAspects;

        public AspectSource( ImmutableDictionaryOfArray<IAspectClass, AspectInstance> transitiveAspects )
        {
            this._transitiveAspects = transitiveAspects;
        }

        public IEnumerable<IAspectClass> AspectClasses => this._transitiveAspects.Keys;

        public bool ContainsAspectClass( IAspectClass aspectClass ) => this._transitiveAspects.ContainsKey( aspectClass );

        public Task CollectAspectInstancesAsync( AspectInstanceCollector collector )
        {
            var aspectClass = collector.AspectClass;

            foreach ( var aspectInstance in this._transitiveAspects[aspectClass] )
            {
                collector.AddAspectInstance( aspectInstance );
            }

            return Task.CompletedTask;
        }

        public ContributorKind ContributorKind => ContributorKind.AspectSource;
    }
}