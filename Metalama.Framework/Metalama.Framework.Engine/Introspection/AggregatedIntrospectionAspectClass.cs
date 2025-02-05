// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Utilities;
using Metalama.Framework.Introspection;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Metalama.Framework.Engine.Introspection;

internal sealed class AggregatedIntrospectionAspectClass : BaseIntrospectionAspectClass
{
    private readonly IEnumerable<IIntrospectionAspectInstance> _instances;

    public AggregatedIntrospectionAspectClass( IAspectClass aspectClass, IEnumerable<IIntrospectionAspectInstance> instances ) : base( aspectClass )
    {
        this._instances = instances;
    }

    [Memo]
    public override ImmutableArray<IIntrospectionAspectInstance> Instances => this._instances.ToImmutableArray();
}