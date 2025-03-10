// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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