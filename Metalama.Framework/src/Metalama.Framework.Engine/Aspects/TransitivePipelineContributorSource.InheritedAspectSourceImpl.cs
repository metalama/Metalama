// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel.Abstractions;
using Metalama.Framework.Engine.Collections;
using Metalama.Framework.Engine.Extensibility;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities.Threading;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Metalama.Framework.Engine.Aspects;

internal sealed partial class TransitivePipelineContributorSource
{
    private sealed class InheritedAspectSourceImpl : IAspectSource
    {
        private readonly IConcurrentTaskRunner _concurrentTaskRunner;
        private readonly ImmutableDictionaryOfArray<IAspectClass, InheritableAspectInstance> _inheritedAspects;

        public InheritedAspectSourceImpl(
            ProjectServiceProvider serviceProvider,
            ImmutableDictionaryOfArray<IAspectClass, InheritableAspectInstance> inheritedAspects )
        {
            this._inheritedAspects = inheritedAspects;
            this._concurrentTaskRunner = serviceProvider.GetRequiredService<IConcurrentTaskRunner>();
        }

        public ContributorKind ContributorKind => ContributorKind.AspectSource;

        public IEnumerable<IAspectClass> AspectClasses => this._inheritedAspects.Keys;

        public bool ContainsAspectClass( IAspectClass aspectClass ) => this._inheritedAspects.ContainsKey( aspectClass );

        public Task CollectAspectInstancesAsync( AspectInstanceCollector collector )
        {
            var aspectClass = (AspectClass) collector.AspectClass;

            return this._concurrentTaskRunner.RunConcurrentlyAsync(
                this._inheritedAspects[aspectClass],
                ProcessAspectInstance,
                collector.CancellationToken );

            void ProcessAspectInstance( InheritableAspectInstance inheritedAspectInstance )
            {
                var baseDeclaration = inheritedAspectInstance.TargetDeclaration.GetTargetOrNull( collector.Compilation );

                if ( baseDeclaration == null )
                {
                    return;
                }

                // We need to provide instances on the first level of derivation only because the caller will add to the next levels.

                foreach ( var derived in ((IDeclarationImpl) baseDeclaration).GetDerivedDeclarations( DerivedTypesOptions.DirectOnly ) )
                {
                    collector.AddAspectInstance(
                        new AspectInstance(
                            inheritedAspectInstance.Aspect,
                            derived,
                            aspectClass,
                            new AspectPredecessor( AspectPredecessorKind.Inherited, inheritedAspectInstance ) ) );
                }
            }
        }
    }
}