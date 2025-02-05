// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.Utilities.Comparers;
using Microsoft.CodeAnalysis;
using System;

namespace Metalama.Framework.Engine.Linking;

internal readonly struct AspectReferenceTarget : IEquatable<AspectReferenceTarget>
{
    /// <summary>
    /// Gets the target symbol. For accessor reference this is always the target property, indexer or event.
    /// </summary>
    public ISymbol Symbol { get; }

    /// <summary>
    /// Gets the semantic of the symbol that is referenced.
    /// </summary>
    public IntermediateSymbolSemanticKind SemanticKind { get; }

    /// <summary>
    /// Gets the kind of target. For properties/events/indexers this specifies which accessor is referenced.
    /// </summary>
    public AspectReferenceTargetKind TargetKind { get; }

    public AspectReferenceTarget( ISymbol symbol, IntermediateSymbolSemanticKind semantic, AspectReferenceTargetKind targetKind )
    {
        // Normalize the target.
        (this.Symbol, this.TargetKind) = (symbol, targetKind) switch
        {
            (IMethodSymbol { MethodKind: MethodKind.PropertyGet, AssociatedSymbol: IPropertySymbol property }, AspectReferenceTargetKind.Self)
                => ((ISymbol) property, AspectReferenceTargetKind.PropertyGetAccessor),
            (IMethodSymbol { MethodKind: MethodKind.PropertySet, AssociatedSymbol: IPropertySymbol property }, AspectReferenceTargetKind.Self)
                => (property, AspectReferenceTargetKind.PropertySetAccessor),
            (IMethodSymbol { MethodKind: MethodKind.EventAdd, AssociatedSymbol: IEventSymbol @event }, AspectReferenceTargetKind.Self)
                => (@event, AspectReferenceTargetKind.EventAddAccessor),
            (IMethodSymbol { MethodKind: MethodKind.EventRemove, AssociatedSymbol: IEventSymbol @event }, AspectReferenceTargetKind.Self)
                => (@event, AspectReferenceTargetKind.EventRemoveAccessor),
            (IMethodSymbol method, AspectReferenceTargetKind.Self) => (method, AspectReferenceTargetKind.Self),
            (IPropertySymbol property, AspectReferenceTargetKind.PropertyGetAccessor or AspectReferenceTargetKind.PropertySetAccessor)
                => (property, targetKind),
            (IEventSymbol @event, AspectReferenceTargetKind.EventAddAccessor or AspectReferenceTargetKind.EventRemoveAccessor)
                => (@event, targetKind),
            (IPropertySymbol property, AspectReferenceTargetKind.Self) => (property, AspectReferenceTargetKind.Self),
            (IEventSymbol @event, AspectReferenceTargetKind.Self) => (@event, AspectReferenceTargetKind.Self),
            (IFieldSymbol field, AspectReferenceTargetKind.PropertyGetAccessor or AspectReferenceTargetKind.PropertySetAccessor) => (field, targetKind),
            _ => throw new AssertionFailedException( $"Unexpected combination: ('{symbol}', {targetKind})." )
        };

        this.SemanticKind = semantic;
    }

    public bool Equals( AspectReferenceTarget other )
        => StructuralSymbolComparer.Default.Equals( this.Symbol, other.Symbol )
           && other.SemanticKind == this.SemanticKind
           && other.TargetKind == this.TargetKind;

    public override int GetHashCode()
        =>

            // PERF: Cast enum to byte otherwise it will be boxed on .NET Framework.
            HashCode.Combine(
                StructuralSymbolComparer.Default.GetHashCode( this.Symbol ),
                (byte) this.SemanticKind,
                (byte) this.TargetKind );

    public override string ToString() => $"({this.SemanticKind}, {this.Symbol}, {this.TargetKind})";
}