// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Metalama.Framework.Engine.AspectOrdering
{
    internal sealed class AspectOrderSpecification
    {
        public AspectOrderSpecification( IEnumerable<string> orderedLayers, bool applyToDerivedTypes )
        {
            this.ApplyToDerivedTypes = applyToDerivedTypes;
            this.OrderedLayers = orderedLayers.ToImmutableArray();
        }

        public AspectOrderSpecification( AspectOrderAttribute attribute, Location? location )
        {
            var attributeOrderedLayers = attribute.OrderedAspectLayers.ToMutableList();

            if ( attribute.Direction == AspectOrderDirection.RunTime )
            {
                // Set the order in compile-time order.
                attributeOrderedLayers.Reverse();
            }

            this.OrderedLayers = attributeOrderedLayers;
            this.DiagnosticLocation = location;
            this.ApplyToDerivedTypes = attribute.ApplyToDerivedTypes;
        }

        public bool ApplyToDerivedTypes { get; }

        public Location? DiagnosticLocation { get; }

        public IReadOnlyList<string> OrderedLayers { get; }
    }
}