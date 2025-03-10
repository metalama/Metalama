// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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