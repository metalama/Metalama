// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.Aspects;
using System;

namespace Metalama.Framework.Engine.AspectOrdering
{
    internal class AspectLayer : IEquatable<AspectLayerId>
    {
        private readonly IBoundAspectClass? _aspectClass;

        public AspectLayer( IBoundAspectClass aspectClass, string? layerName )
        {
            this._aspectClass = aspectClass;
            this.AspectLayerId = new AspectLayerId( aspectClass, layerName );
        }

        // Constructor for testing only.
        public AspectLayer( string aspectTypeName, string? layerName )
        {
            this.AspectLayerId = new AspectLayerId( aspectTypeName, layerName );
        }

        public IBoundAspectClass AspectClass => this._aspectClass.AssertNotNull();

        public AspectLayerId AspectLayerId { get; }

        public bool IsDefault => this.AspectLayerId.IsDefault;

        public string AspectName => this.AspectLayerId.AspectName;

        public string? LayerName => this.AspectLayerId.LayerName;

        public bool Equals( AspectLayerId other ) => this.AspectLayerId == other;

        public override int GetHashCode() => this.AspectLayerId.GetHashCode();

        public override string ToString() => this.AspectLayerId.ToString();
    }
}