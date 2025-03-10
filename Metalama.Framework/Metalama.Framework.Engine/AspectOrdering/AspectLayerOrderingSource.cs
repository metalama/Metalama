// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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