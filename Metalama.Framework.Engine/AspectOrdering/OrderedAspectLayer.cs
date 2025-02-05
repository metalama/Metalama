// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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