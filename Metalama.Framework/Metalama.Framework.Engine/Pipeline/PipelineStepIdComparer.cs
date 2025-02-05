// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.AspectOrdering;
using Metalama.Framework.Engine.Aspects;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Metalama.Framework.Engine.Pipeline
{
    /// <summary>
    /// A comparer for <see cref="PipelineStepId"/>. It must be initialized with a collection of <see cref="OrderedAspectLayer"/> because
    /// the <see cref="PipelineStepId"/> type itself does not contain enough information to compare itself.
    /// </summary>
    internal sealed class PipelineStepIdComparer : Comparer<PipelineStepId>
    {
        private readonly ImmutableDictionary<AspectLayerId, OrderedAspectLayer> _orderedAspectLayers;

        public PipelineStepIdComparer( IEnumerable<OrderedAspectLayer> orderedAspectLayers )
        {
            this._orderedAspectLayers = orderedAspectLayers.ToImmutableDictionary( p => p.AspectLayerId, p => p );
        }

        public OrderedAspectLayer GetOrderedAspectLayer( AspectLayerId id ) => this._orderedAspectLayers[id];

        public bool Contains( AspectLayerId id ) => this._orderedAspectLayers.ContainsKey( id );

        public override int Compare( PipelineStepId x, PipelineStepId y )
        {
            if ( x.AspectLayerId != y.AspectLayerId )
            {
                var xOrderedPart = this._orderedAspectLayers[x.AspectLayerId];
                var yOrderedPart = this._orderedAspectLayers[y.AspectLayerId];

                // First order by topological distance.
                var compareAspectLayerOrder = xOrderedPart.Order.CompareTo( yOrderedPart.Order );

                if ( compareAspectLayerOrder != 0 )
                {
                    return compareAspectLayerOrder;
                }

                // If topological distance is identical, order by name.
                var nameOrder = string.Compare( xOrderedPart.AspectName, yOrderedPart.AspectName, StringComparison.Ordinal );

                if ( nameOrder != 0 )
                {
                    return nameOrder;
                }
            }

            /*
             * Within the same aspect layer and the same depth of the declaring type, aspects must be executed in the following order:
             * 1. Discovering advices on the type itself.
             * 2. Executing advices discovered at type level, by depth.
             * 3. Discovering advices on the members (can add advices to the declaring type).
             * 4. Executing advices discovered at member level, by depth.
             * 5. Discovering advices on the parameters (can add advices to the declaring member but not to the declaring type).
             * 6. Executing advices discovered at parameter level, by depth.
             *
             *  which means that the order criteria are the following:
             *
             *  1. Aspect layer
             *  2. Depth of the aspect target down to the type level, i.e. type, namespace or compilation (but not member or parameter).
             *  3. Depth of the aspect target with respect to the declaring type, i.e. 0 (type), 1 (member), 2 (parameter) -- which is
             *     equivalent to the depth of the aspect target itself.
             *  4. Phase: discover or transform
             *  5. Depth of the advice target
             */

            var compareAspectTypeDepth = x.AspectTargetTypeDepth.CompareTo( y.AspectTargetTypeDepth );

            if ( compareAspectTypeDepth != 0 )
            {
                return compareAspectTypeDepth;
            }

            var compareAspectDepth = x.AspectTargetDepth.CompareTo( y.AspectTargetDepth );

            if ( compareAspectDepth != 0 )
            {
                return compareAspectDepth;
            }

            var compareAdviceTargetDepth = x.AdviceTargetDepth.CompareTo( y.AdviceTargetDepth );

            if ( compareAdviceTargetDepth != 0 )
            {
                return compareAdviceTargetDepth;
            }

            if ( !x.Equals( y ) )
            {
                // The steps must be different here, otherwise there would be a duplicate key in the skip list of PipelineStepsState.
                throw new AssertionFailedException( $"'{x}' and '{y}' are not strongly ordered." );
            }

            return 0;
        }
    }
}