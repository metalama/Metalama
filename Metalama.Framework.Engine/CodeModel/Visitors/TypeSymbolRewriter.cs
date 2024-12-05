// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using System;
using System.Linq;

namespace Metalama.Framework.Engine.CodeModel.Visitors;

internal abstract class TypeSymbolRewriter
{
    [PublicAPI]
    protected static TypeSymbolRewriter Null { get; } = new NullRewriter();

    private readonly Compilation _compilation;

    protected TypeSymbolRewriter( Compilation compilation )
    {
        this._compilation = compilation;
    }

    internal virtual ITypeSymbol Visit( IArrayTypeSymbol arrayTypeSymbol )
    {
        var elementTypeSymbol = this.Visit( arrayTypeSymbol.ElementType );

        if ( elementTypeSymbol == arrayTypeSymbol.ElementType )
        {
            return arrayTypeSymbol;
        }
        else
        {
            return this._compilation.CreateArrayTypeSymbol( elementTypeSymbol, arrayTypeSymbol.Rank, elementTypeSymbol.NullableAnnotation );
        }
    }

    public virtual ITypeSymbol Visit( ITypeSymbol typeSymbol )
        => typeSymbol.TypeKind switch
        {
            TypeKind.Unknown => typeSymbol,
            TypeKind.Array => this.Visit( (IArrayTypeSymbol) typeSymbol ),
            TypeKind.Class or TypeKind.Delegate or TypeKind.Enum or TypeKind.Interface or TypeKind.Struct => this.Visit( (INamedTypeSymbol) typeSymbol ),
            TypeKind.Dynamic => this.Visit( (IDynamicTypeSymbol) typeSymbol ),
            TypeKind.Error => typeSymbol,
            TypeKind.Module => typeSymbol,
            TypeKind.Pointer => this.Visit( (IPointerTypeSymbol) typeSymbol ),
            TypeKind.TypeParameter => this.Visit( (ITypeParameterSymbol) typeSymbol ),
            TypeKind.Submission => typeSymbol,
            TypeKind.FunctionPointer => typeSymbol,
            _ => throw new ArgumentOutOfRangeException()
        };

    internal virtual ITypeSymbol Visit( IDynamicTypeSymbol dynamicTypeSymbol ) => dynamicTypeSymbol;

    internal virtual ITypeSymbol Visit( IPointerTypeSymbol pointerTypeSymbol )
    {
        var pointedAtTypeSymbol = this.Visit( pointerTypeSymbol.PointedAtType );

        if ( pointedAtTypeSymbol == pointerTypeSymbol.PointedAtType )
        {
            return pointerTypeSymbol;
        }
        else
        {
            return this._compilation.CreatePointerTypeSymbol( pointedAtTypeSymbol );
        }
    }

    internal virtual ITypeSymbol Visit( IFunctionPointerTypeSymbol functionPointerTypeSymbol )
    {
        return functionPointerTypeSymbol;
    }

    internal virtual ITypeSymbol Visit( INamedTypeSymbol namedTypeSymbol )
    {
        INamedTypeSymbol typeDefinition;

        if ( !namedTypeSymbol.IsGenericType )
        {
            // Fast track.
            return namedTypeSymbol;
        }

        if ( namedTypeSymbol.ContainingType != null )
        {
            var mappedDeclaringType = this.Visit( namedTypeSymbol.ContainingType );
            typeDefinition = mappedDeclaringType.GetTypeMembers( namedTypeSymbol.Name, namedTypeSymbol.Arity ).Single();

            if ( namedTypeSymbol.TypeParameters.Length == 0 )
            {
                // The nested type itself is not generic.
                return typeDefinition;
            }
        }
        else
        {
            typeDefinition = namedTypeSymbol;
        }

        var typeArguments = new ITypeSymbol[namedTypeSymbol.TypeArguments.Length];

        for ( var index = 0; index < namedTypeSymbol.TypeArguments.Length; index++ )
        {
            var t = namedTypeSymbol.TypeArguments[index];
            var argumentTypeSymbol = this.Visit( t );
            typeArguments[index] = argumentTypeSymbol;
        }

        return typeDefinition.ConstructedFrom.Construct( typeArguments );
    }

    internal virtual ITypeSymbol Visit( ITypeParameterSymbol typeSymbolParameter ) => typeSymbolParameter;

    private sealed class NullRewriter : TypeSymbolRewriter
    {
        public NullRewriter() : base( null! ) { }

        public override ITypeSymbol Visit( ITypeSymbol elementTypeSymbol ) => elementTypeSymbol;

        internal override ITypeSymbol Visit( IArrayTypeSymbol arrayTypeSymbol ) => arrayTypeSymbol;

        internal override ITypeSymbol Visit( IDynamicTypeSymbol dynamicTypeSymbol ) => dynamicTypeSymbol;

        internal override ITypeSymbol Visit( IPointerTypeSymbol pointerTypeSymbol ) => pointerTypeSymbol;

        internal override ITypeSymbol Visit( INamedTypeSymbol namedTypeSymbol ) => namedTypeSymbol;

        internal override ITypeSymbol Visit( ITypeParameterSymbol typeSymbolParameter ) => typeSymbolParameter;

        internal override ITypeSymbol Visit( IFunctionPointerTypeSymbol functionPointerTypeSymbol ) => functionPointerTypeSymbol;
    }
}