// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Introspection;

namespace Metalama.Framework.Engine.Introspection;

internal sealed class AggregatedIntrospectionAspectLayer : IIntrospectionAspectLayer
{
    private readonly IIntrospectionAspectLayer _anyLayer;

    public AggregatedIntrospectionAspectLayer( IIntrospectionAspectClass aspectClass, IIntrospectionAspectLayer anyLayer )
    {
        this._anyLayer = anyLayer;
        this.AspectClass = aspectClass;
    }

    public string Id => this._anyLayer.Id;

    public IIntrospectionAspectClass AspectClass { get; }

    public string? LayerName => this._anyLayer.LayerName;

    public int? Order => null;

    public int? ExplicitOrder => null;

    public bool IsDefaultLayer => this._anyLayer.IsDefaultLayer;
}