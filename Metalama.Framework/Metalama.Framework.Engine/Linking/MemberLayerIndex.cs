// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System;

namespace Metalama.Framework.Engine.Linking;

// ReSharper disable MemberCanBePrivate.Global
internal readonly struct MemberLayerIndex : IComparable<MemberLayerIndex>, IEquatable<MemberLayerIndex>
{
    /// <summary>
    /// Gets the index of the aspect layer. Zero is the state before any transformation.
    /// </summary>
    public int LayerIndex { get; }

    // ReSharper disable once MemberCanBePrivate.Local
    /// <summary>
    /// Gets the index of the aspect instance within the target type.
    /// </summary>
    public int InstanceIndex { get; }

    // ReSharper disable once MemberCanBePrivate.Local
    /// <summary>
    /// Gets the index of the transformation within the aspect instance.
    /// </summary>
    public int TransformationIndex { get; }

    public MemberLayerIndex( int layerIndex, int instanceIndex, int transformationIndex )
    {
        this.LayerIndex = layerIndex;
        this.InstanceIndex = instanceIndex;
        this.TransformationIndex = transformationIndex;
    }

    public int CompareTo( MemberLayerIndex other )
    {
        var layerDiff = this.LayerIndex - other.LayerIndex;

        if ( layerDiff == 0 )
        {
            var instanceDiff = this.InstanceIndex - other.InstanceIndex;

            if ( instanceDiff == 0 )
            {
                return this.TransformationIndex - other.TransformationIndex;
            }
            else
            {
                return instanceDiff;
            }
        }
        else
        {
            return layerDiff;
        }
    }

    public bool Equals( MemberLayerIndex other ) => this.CompareTo( other ) == 0;

    public override bool Equals( object? obj ) => obj is MemberLayerIndex mli && this.Equals( mli );

    public override int GetHashCode() => HashCode.Combine( this.LayerIndex, this.InstanceIndex, this.TransformationIndex );

    public override string ToString() => $"({this.LayerIndex}, {this.InstanceIndex}, {this.TransformationIndex})";

    internal MemberLayerIndex WithoutTransformationIndex()
    {
        return new MemberLayerIndex( this.LayerIndex, this.InstanceIndex, 0 );
    }

    public static bool operator ==( MemberLayerIndex a, MemberLayerIndex b ) => a.Equals( b );

    public static bool operator !=( MemberLayerIndex a, MemberLayerIndex b ) => !a.Equals( b );

    public static bool operator <( MemberLayerIndex a, MemberLayerIndex b ) => a.CompareTo( b ) < 0;

    public static bool operator <=( MemberLayerIndex a, MemberLayerIndex b ) => a.CompareTo( b ) <= 0;

    public static bool operator >( MemberLayerIndex a, MemberLayerIndex b ) => a.CompareTo( b ) > 0;

    public static bool operator >=( MemberLayerIndex a, MemberLayerIndex b ) => a.CompareTo( b ) >= 0;
}