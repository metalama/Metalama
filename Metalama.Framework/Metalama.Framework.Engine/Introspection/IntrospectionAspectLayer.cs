// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.AspectOrdering;
using Metalama.Framework.Engine.Utilities;
using Metalama.Framework.Introspection;

namespace Metalama.Framework.Engine.Introspection;

internal sealed class IntrospectionAspectLayer : IIntrospectionAspectLayer
{
    private readonly OrderedAspectLayer _aspectLayer;
    private readonly IntrospectionFactory _factory;

    public IntrospectionAspectLayer( OrderedAspectLayer aspectLayer, IntrospectionFactory factory )
    {
        this._aspectLayer = aspectLayer;
        this._factory = factory;
    }

    [Memo]
    public string Id => this._aspectLayer.AspectLayerId.ToString();

    public IIntrospectionAspectClass AspectClass => this._factory.GetIntrospectionAspectClass( this._aspectLayer.AspectClass );

    public string? LayerName => this._aspectLayer.LayerName;

    public int? Order => this._aspectLayer.Order;

    public int? ExplicitOrder => this._aspectLayer.ExplicitOrder;

    public bool IsDefaultLayer => this._aspectLayer.IsDefault;
}