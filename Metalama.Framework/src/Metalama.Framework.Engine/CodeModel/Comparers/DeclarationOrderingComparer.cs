// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using System.Collections.Generic;

namespace Metalama.Framework.Engine.CodeModel.Comparers;

/// <summary>
/// Deterministic ordering comparer for <see cref="IDeclaration"/>. Stateless singleton.
/// Sort key: <see cref="IDeclaration.Depth"/>, then <see cref="IDeclaration.ContainingDeclaration"/> (recursive),
/// then name (if the declaration is an <see cref="INamedDeclaration"/>), then signature (if the declaration is
/// an <see cref="IHasParameters"/>), then type structure (if the declaration is an <see cref="IType"/>).
/// </summary>
internal sealed class DeclarationOrderingComparer : IComparer<IDeclaration>
{
    public static DeclarationOrderingComparer Instance { get; } = new();

    private DeclarationOrderingComparer() { }

    public int Compare( IDeclaration? x, IDeclaration? y )
    {
        if ( ReferenceEquals( x, y ) )
        {
            return 0;
        }

        if ( x == null )
        {
            return -1;
        }

        if ( y == null )
        {
            return 1;
        }

        // 1. Depth.
        var depth = x.Depth - y.Depth;

        if ( depth != 0 )
        {
            return depth;
        }

        // 2. Containing declaration (recursive).
        var parent = (x.ContainingDeclaration, y.ContainingDeclaration) switch
        {
            (null, null) => 0,
            (null, _) => -1,
            (_, null) => 1,
            var (a, b) => this.Compare( a, b )
        };

        if ( parent != 0 )
        {
            return parent;
        }

        // 3. Name (when available).
        var name = string.CompareOrdinal( GetName( x ), GetName( y ) );

        if ( name != 0 )
        {
            return name;
        }

        // 4. Signature (for overloadable declarations).
#pragma warning disable LAMA0860
        if ( x is IHasParameters hx && y is IHasParameters hy )
#pragma warning restore LAMA0860
        {
            var sig = SignatureOrderingComparer.Instance.Compare( hx, hy );

            if ( sig != 0 )
            {
                return sig;
            }
        }

        // 5. Full type structure tiebreak (type args, arrays, pointers, ...) for types.
        if ( x is IType tx && y is IType ty )
        {
            var t = TypeOrderingComparer.Instance.Compare( tx, ty );

            if ( t != 0 )
            {
                return t;
            }
        }

        return 0;
    }

    private static string GetName( IDeclaration declaration )
#pragma warning disable LAMA0860
        => declaration is INamedDeclaration named ? named.Name : string.Empty;
#pragma warning restore LAMA0860
}