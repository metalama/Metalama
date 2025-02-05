// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Microsoft.CodeAnalysis;
using System.Globalization;

namespace Metalama.Framework.DesignTime.Pipeline.Dependencies;

/// <summary>
/// A weak equality key to identify types, where some collisions are acceptable.
/// </summary>
public readonly struct TypeDependencyKey : IEquatable<TypeDependencyKey>
{
    private readonly int _hashCode;
    private readonly string? _text;

    internal TypeDependencyKey( ITypeSymbol type, bool storeTypeName )
    {
        var hashCode = default(HashCode);

        for ( var d = (ISymbol) type;
              d is { } and not IAssemblySymbol and not IModuleSymbol and not INamespaceSymbol { IsGlobalNamespace: true };
              d = d.ContainingSymbol )
        {
            hashCode.Add( d.Name );
        }

        this._hashCode = hashCode.ToHashCode();
        this._text = storeTypeName ? type.ToString() : null;
    }

    // For test only.
    internal TypeDependencyKey( string name )
    {
        // We should generate the same hashcode than in the production constructor so that we can match a hand-generated TypeDependencyKey
        // with a symbol-generated TypeDependencyKey.
        var hashCode = default(HashCode);
        var nameParts = name.Split( '.' );

        for ( var i = nameParts.Length - 1; i >= 0; i-- )
        {
            hashCode.Add( nameParts[i] );
        }

        this._hashCode = hashCode.ToHashCode();
        this._text = name;
    }

    public bool Equals( TypeDependencyKey other ) => this._hashCode == other._hashCode;

    public override bool Equals( object? obj ) => obj is TypeDependencyKey other && this.Equals( other );

    public override int GetHashCode() => this._hashCode;

    public static bool operator ==( TypeDependencyKey left, TypeDependencyKey right ) => left.Equals( right );

    public static bool operator !=( TypeDependencyKey left, TypeDependencyKey right ) => !left.Equals( right );

    public override string ToString() => this._text ?? this._hashCode.ToString( CultureInfo.InvariantCulture );
}