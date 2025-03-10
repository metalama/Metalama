// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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