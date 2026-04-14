// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using System.Collections.Generic;

namespace Metalama.Framework.Engine.CodeModel.Comparers;

/// <summary>
/// Deterministic ordering comparer for <see cref="IHasParameters"/>. Compares by parameter count, then by each
/// parameter's <see cref="RefKind"/> and type (via <see cref="TypeOrderingComparer"/>), then — for methods —
/// by type-parameter count and name. Stateless singleton.
/// </summary>
internal sealed class SignatureOrderingComparer : IComparer<IHasParameters>
{
    public static SignatureOrderingComparer Instance { get; } = new();

    private SignatureOrderingComparer() { }

    public int Compare( IHasParameters? x, IHasParameters? y )
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

        var count = x.Parameters.Count - y.Parameters.Count;

        if ( count != 0 )
        {
            return count;
        }

        for ( var i = 0; i < x.Parameters.Count; i++ )
        {
            var refKind = (int) x.Parameters[i].RefKind - (int) y.Parameters[i].RefKind;

            if ( refKind != 0 )
            {
                return refKind;
            }

            var type = TypeOrderingComparer.Instance.Compare( x.Parameters[i].Type, y.Parameters[i].Type );

            if ( type != 0 )
            {
                return type;
            }
        }

        if ( x.DeclarationKind == DeclarationKind.Method && x is IMethod mx && y.DeclarationKind == DeclarationKind.Method && y is IMethod my )
        {
            var tp = mx.TypeParameters.Count - my.TypeParameters.Count;

            if ( tp != 0 )
            {
                return tp;
            }

            var name = string.CompareOrdinal( mx.Name, my.Name );

            if ( name != 0 )
            {
                return name;
            }
        }

        return 0;
    }
}