// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Framework.Engine.AspectOrdering
{
    internal sealed class OrderedAspectLayer : AspectLayer
    {
        /// <summary>
        /// Gets the layer order including the alphabetical criteria.
        /// </summary>
        public int Order { get; }

        /// <summary>
        /// Gets the layer order without the alphabetical criteria. If aspects are incompletely ordered, several aspects
        /// can have the same value of this property.
        /// </summary>
        public int ExplicitOrder { get; }

        public OrderedAspectLayer( int order, int explicitOrder, AspectLayer aspectLayer ) : base( aspectLayer.AspectClass, aspectLayer.LayerName )
        {
            this.Order = order;
            this.ExplicitOrder = explicitOrder;
        }

        // For testing only.
        // Resharper disable once UnusedMember.Global
        internal OrderedAspectLayer( int order, string aspectName, string? layerName ) : base( aspectName, layerName )
        {
            this.Order = order;
            this.ExplicitOrder = order;
        }

        public override string ToString() => base.ToString() + " => " + this.ExplicitOrder;
    }
}