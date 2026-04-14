// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Types;
using System.Collections.Generic;

namespace Metalama.Framework.Engine.CodeModel.Comparers;

/// <summary>
/// Deterministic ordering comparer for <see cref="IType"/>. Stateless singleton, thread-safe, allocation-free.
/// Dispatches on <see cref="IType.TypeKind"/> to pairwise helpers — the unary <see cref="Visitors.TypeVisitor{T}"/>
/// is not a good fit because it would require per-call allocation or <c>[ThreadStatic]</c> for two-operand comparison.
/// </summary>
internal sealed class TypeOrderingComparer : IComparer<IType>
{
    public static TypeOrderingComparer Instance { get; } = new();

    private TypeOrderingComparer() { }

    public int Compare( IType? x, IType? y )
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

        var kindDiff = (int) x.TypeKind - (int) y.TypeKind;

        if ( kindDiff != 0 )
        {
            return kindDiff;
        }

        return x.TypeKind switch
        {
            TypeKind.Array => this.CompareArrays( (IArrayType) x, (IArrayType) y ),
            TypeKind.Pointer => this.Compare( ((IPointerType) x).PointedAtType, ((IPointerType) y).PointedAtType ),
            TypeKind.FunctionPointer => 0, // IFunctionPointerType has no observable signature in the public API; treat as equal.
            TypeKind.TypeParameter => CompareTypeParameters( (ITypeParameter) x, (ITypeParameter) y ),
            TypeKind.Dynamic => 0,

            // Class/Struct/Interface/Enum/Delegate/Tuple/Extension all implement INamedType — matches TypeVisitor<T>'s routing.
            _ => this.CompareNamedTypes( (INamedType) x, (INamedType) y )
        };
    }

    private int CompareNamedTypes( INamedType x, INamedType y )
    {
        var ns = CompareNamespaces( x.ContainingNamespace, y.ContainingNamespace );

        if ( ns != 0 )
        {
            return ns;
        }

        var decl = (x.DeclaringType, y.DeclaringType) switch
        {
            (null, null) => 0,
            (null, _) => -1,
            (_, null) => 1,
            var (a, b) => this.Compare( a, b )
        };

        if ( decl != 0 )
        {
            return decl;
        }

        var name = string.CompareOrdinal( x.Name, y.Name );

        if ( name != 0 )
        {
            return name;
        }

        var arity = x.TypeArguments.Count - y.TypeArguments.Count;

        if ( arity != 0 )
        {
            return arity;
        }

        for ( var i = 0; i < x.TypeArguments.Count; i++ )
        {
            var a = this.Compare( x.TypeArguments[i], y.TypeArguments[i] );

            if ( a != 0 )
            {
                return a;
            }
        }

        return 0;
    }

    private int CompareArrays( IArrayType x, IArrayType y )
    {
        var rank = x.Rank - y.Rank;

        if ( rank != 0 )
        {
            return rank;
        }

        return this.Compare( x.ElementType, y.ElementType );
    }

    private static int CompareTypeParameters( ITypeParameter x, ITypeParameter y )
    {
        var kind = (int) x.TypeParameterKind - (int) y.TypeParameterKind;

        if ( kind != 0 )
        {
            return kind;
        }

        return x.Index - y.Index;
    }

    private static int CompareNamespaces( INamespace? x, INamespace? y )
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

        var outer = CompareNamespaces( x.ContainingNamespace, y.ContainingNamespace );

        if ( outer != 0 )
        {
            return outer;
        }

        return string.CompareOrdinal( x.Name, y.Name );
    }
}