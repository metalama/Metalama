// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.AspectOrdering;
using Metalama.Framework.Engine.Utilities;
using System;
using System.Linq;

namespace Metalama.Framework.Engine.Aspects
{
    /// <summary>
    /// Represents the identity of an aspect layer.
    /// </summary>
    internal readonly struct AspectLayerId : IEquatable<AspectLayerId>, IEquatable<AspectLayer>
    {
        private static readonly char[] _separators = [':'];

        public static AspectLayerId Null => default;

        public static bool operator ==( AspectLayerId left, AspectLayerId right ) => left.Equals( right );

        public static bool operator !=( AspectLayerId left, AspectLayerId right ) => !left.Equals( right );

        public AspectLayerId( IAspectClass aspectClass, string? layerName = null ) : this( aspectClass.FullName, layerName ) { }

        public AspectLayerId( string aspectName, string? layerName = null )
        {
            this.AspectName = aspectName;
            this.LayerName = layerName;
            this.AspectShortName = aspectName.Split( '.' ).Last().TrimSuffix( "Attribute" );
        }

        public static AspectLayerId FromString( string s )
        {
            var parts = s.Split( _separators );

            if ( parts.Length == 1 )
            {
                return new AspectLayerId( parts[0] );
            }

            // Coverage: ignore (TODO 28625)
            return new AspectLayerId( parts[0], parts.Length == 2 ? parts[1] : null );
        }

        public bool IsDefault => this.LayerName == null;

        public string AspectName { get; }

        public string AspectShortName { get; }

        public string? LayerName { get; }

        public string FullName => this.AspectName == null! ? "(null)" : this.LayerName == null ? this.AspectName : this.AspectName + ":" + this.LayerName;

        public override string ToString() => this.FullName;

        public bool Equals( AspectLayerId other )
            => StringComparer.Ordinal.Equals( this.AspectName, other.AspectName ) && StringComparer.Ordinal.Equals( this.LayerName, other.LayerName );

        public override int GetHashCode()
            => (this.AspectName == null! ? 0 : StringComparer.Ordinal.GetHashCode( this.AspectName ))
               ^ (this.LayerName == null ? 0 : StringComparer.Ordinal.GetHashCode( this.LayerName ));

        public bool Equals( AspectLayer? other ) => other != null && this.Equals( other.AspectLayerId );

        public override bool Equals( object? obj )
            => obj switch
            {
                null => false,
                AspectLayerId id => this.Equals( id ),
                AspectLayer layer => this.Equals( layer ),
                _ => false
            };
    }
}