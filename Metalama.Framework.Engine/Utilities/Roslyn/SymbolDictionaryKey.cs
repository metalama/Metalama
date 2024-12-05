// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.SerializableIds;
using Metalama.Framework.Engine.Utilities.Comparers;
using Microsoft.CodeAnalysis;
using System;

namespace Metalama.Framework.Engine.Utilities.Roslyn;

/// <summary>
/// Represents a dictionary key for an <see cref="ISymbol"/>. When created using the <see cref="CreatePersistentKey"/> method, the key
/// does not hold a reference to the <see cref="ISymbol"/> itself, but only its <see cref="SymbolId"/>. The instance created
/// by this method is meant to be stored in the dictionary as the key, and to have a longer lifetime than the compilation.
/// When created using the <see cref="CreateLookupKey"/> method, the key holds a reference to the <see cref="ISymbol"/>. The comparison
/// with the <see cref="SymbolId"/> is done lazily, only in case where the hash codes match. This instance is meant to be
/// used for a dictionary lookup.
/// </summary>
public readonly struct SymbolDictionaryKey : IEquatable<SymbolDictionaryKey>
{
    private readonly int _hashCode;
    private readonly object _identity; // Can be a string (SymbolId) or an ISymbol.

    private SymbolDictionaryKey( int hashCode, object identity )
    {
        this._hashCode = hashCode;
        this._identity = identity;
    }

    public bool Equals( SymbolDictionaryKey other )
    {
        if ( this._hashCode != other._hashCode )
        {
            return false;
        }

        return this.GetSymbolId().Equals( other.GetSymbolId() );
    }

    public static SymbolDictionaryKey CreatePersistentKey( ISymbol symbol )
        => new( StructuralSymbolComparer.Default.GetHashCode( symbol ), SymbolId.Create( symbol ).ToString() );

    public static SymbolDictionaryKey CreateLookupKey( ISymbol symbol ) => new( StructuralSymbolComparer.Default.GetHashCode( symbol ), symbol );

    internal SymbolId GetSymbolId()
        => this._identity switch
        {
            string s => new SymbolId( s ),
            ISymbol s => SymbolId.Create( s ),
            _ => throw new AssertionFailedException( $"Unexpected key type: {this._identity.GetType()}" )
        };

    public IRef<IDeclaration> ToRef()
    {
        var symbolId = this._identity as string ?? throw new InvalidOperationException();

        return DurableRefFactory.FromSymbolId<IDeclaration>( new SymbolId( symbolId ) );
    }

    public override bool Equals( object? obj ) => obj is SymbolDictionaryKey other && this.Equals( other );

    public override int GetHashCode() => this._hashCode;

    public static bool operator ==( SymbolDictionaryKey left, SymbolDictionaryKey right ) => left.Equals( right );

    public static bool operator !=( SymbolDictionaryKey left, SymbolDictionaryKey right ) => !left.Equals( right );

    public override string ToString() => this._identity?.ToString() ?? "null";
}