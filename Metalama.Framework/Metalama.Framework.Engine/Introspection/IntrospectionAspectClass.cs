// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.Utilities;
using Metalama.Framework.Introspection;
using System.Collections.Immutable;
using System.Linq;

namespace Metalama.Framework.Engine.Introspection;

internal sealed class IntrospectionAspectClass : BaseIntrospectionAspectClass
{
    private readonly IntrospectionFactory _factory;
    private readonly ImmutableArray<AspectInstanceResult> _aspectInstanceResults;

    public IntrospectionAspectClass(
        IAspectClass aspectClass,
        ImmutableArray<AspectInstanceResult> aspectInstanceResults,
        IntrospectionFactory factory )
        : base( aspectClass )
    {
        this._aspectInstanceResults = aspectInstanceResults;
        this._factory = factory;
    }

    [Memo]
    public override ImmutableArray<IIntrospectionAspectInstance> Instances
        => this._aspectInstanceResults.Select( x => this._factory.GetIntrospectionAspectInstance( x.AspectInstance ) )
            .ToImmutableArray<IIntrospectionAspectInstance>();
}