// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.Diagnostics;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Metalama.Framework.Engine.AspectOrdering
{
    internal sealed class AspectLayerOrderingSource : IAspectOrderingSource
    {
        private readonly ImmutableArray<AspectClass> _aspectTypes;

        public AspectLayerOrderingSource( ImmutableArray<AspectClass> aspectTypes )
        {
            this._aspectTypes = aspectTypes;
        }

        public IEnumerable<AspectOrderSpecification> GetAspectOrderSpecification( IDiagnosticAdder diagnosticAdder )
            => this._aspectTypes
                .Where( at => at.Layers.Length > 1 )
                .Select( at => new AspectOrderSpecification( at.Layers.Select( l => l.AspectLayerId.FullName ), false ) );
    }
}