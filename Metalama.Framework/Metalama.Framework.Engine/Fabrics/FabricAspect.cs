// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Eligibility;
using Metalama.Framework.Engine.Aspects;
using System;
using System.Collections.Immutable;
using System.Linq;

namespace Metalama.Framework.Engine.Fabrics
{
    /// <summary>
    /// An implementation of <see cref="IAspect"/> that invokes all fabrics on the declaration.
    /// </summary>
    internal sealed class FabricAspect<T> : IAspect<T>
        where T : class, IDeclaration
    {
        private readonly ImmutableArray<FabricTemplateClass> _templateClasses;

        public FabricAspect( ImmutableArray<FabricTemplateClass> templateClasses )
        {
            if ( templateClasses.Any( c => c.Driver.Kind != FabricKind.Type ) )
            {
                throw new ArgumentOutOfRangeException( nameof(templateClasses), "Only type fabrics are supported." );
            }

            this._templateClasses = templateClasses;
        }

        public void BuildAspect( IAspectBuilder<T> builder )
        {
            var internalBuilder = (IAspectBuilderInternal) builder;

            foreach ( var templateClass in this._templateClasses )
            {
                var fabricInstance = new FabricInstance( templateClass.Driver, builder.Target );

                using ( internalBuilder.WithPredecessor( new AspectPredecessor( AspectPredecessorKind.Fabric, fabricInstance ) ) )
                {
                    _ = ((TypeFabricDriver) templateClass.Driver).TryExecute( internalBuilder, templateClass, fabricInstance );
                }
            }
        }

        void IEligible<T>.BuildEligibility( IEligibilityBuilder<T> builder ) { }
    }
}