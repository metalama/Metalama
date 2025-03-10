// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Aspects;
using Microsoft.CodeAnalysis;

namespace Metalama.Framework.Engine.Linking;

internal static class IntermediateSymbolSemanticExtensions
{
    public static IntermediateSymbolSemantic<T> ToSemantic<T>( this T symbol, IntermediateSymbolSemanticKind kind )
        where T : ISymbol
    {
        return ((ISymbol) symbol).ToSemantic( kind ).ToTyped<T>();
    }

    public static IntermediateSymbolSemantic ToSemantic( this ISymbol symbol, IntermediateSymbolSemanticKind kind )
    {
        return new IntermediateSymbolSemantic( symbol, kind );
    }

    public static AspectReferenceTarget ToAspectReferenceTarget(
        this IntermediateSymbolSemantic target,
        AspectReferenceTargetKind kind = AspectReferenceTargetKind.Self )
    {
        return new AspectReferenceTarget( target.Symbol, target.Kind, kind );
    }

    public static AspectReferenceTarget ToAspectReferenceTarget( this IntermediateSymbolSemantic<IMethodSymbol> target )
    {
        return new AspectReferenceTarget( target.Symbol, target.Kind, AspectReferenceTargetKind.Self );
    }

    public static AspectReferenceTarget ToAspectReferenceTarget(
        this IntermediateSymbolSemantic<IPropertySymbol> property,
        AspectReferenceTargetKind targetKind )
    {
        Invariant.Assert( targetKind is AspectReferenceTargetKind.PropertyGetAccessor or AspectReferenceTargetKind.PropertySetAccessor );

        return new AspectReferenceTarget( property.Symbol, property.Kind, targetKind );
    }

    public static AspectReferenceTarget ToAspectReferenceTarget(
        this IntermediateSymbolSemantic<IEventSymbol> @event,
        AspectReferenceTargetKind targetKind )
    {
        Invariant.Assert( targetKind is AspectReferenceTargetKind.EventAddAccessor or AspectReferenceTargetKind.EventRemoveAccessor );

        return new AspectReferenceTarget( @event.Symbol, @event.Kind, targetKind );
    }
}