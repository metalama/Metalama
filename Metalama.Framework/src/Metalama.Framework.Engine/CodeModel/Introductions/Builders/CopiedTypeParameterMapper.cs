// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Comparers;
using Metalama.Framework.Code.Types;
using Metalama.Framework.Engine.CodeModel.Visitors;

namespace Metalama.Framework.Engine.CodeModel.Introductions.Builders;

/// <summary>
/// Replaces, in a type, the type parameters that were copied with <c>AddTypeParameter(ITypeParameter, bool)</c> by their copies.
/// </summary>
/// <remarks>
/// <para>
/// The copies are looked up in the builder given to the constructor and in the builders that contain it, so a parameter of a method copied
/// into a type whose type parameters were also copied refers to the copies of both. A copy is a type parameter of a builder, which has no
/// symbol, so the arrays, pointers and generic instances that contain it are rebuilt as constructed types of the code model.
/// </para>
/// </remarks>
internal sealed class CopiedTypeParameterMapper : TypeRewriter
{
    private readonly DeclarationBuilder _builder;

    private CopiedTypeParameterMapper( DeclarationBuilder builder )
    {
        this._builder = builder;
    }

    /// <summary>
    /// Returns a type in which the copied type parameters of a builder and of the builders that contain it are replaced by their copies.
    /// </summary>
    public static IType Map( DeclarationBuilder builder, IType type )
        => HasCopies( builder ) ? new CopiedTypeParameterMapper( builder ).Visit( type ) : type;

    private static bool HasCopies( DeclarationBuilder builder )
    {
        for ( var current = builder; current != null; current = current.ContainingDeclaration as DeclarationBuilder )
        {
            if ( current.CopiedTypeParameters is { Count: > 0 } )
            {
                return true;
            }
        }

        return false;
    }

    internal override IType Visit( ITypeParameter typeParameter )
    {
        for ( var current = this._builder; current != null; current = current.ContainingDeclaration as DeclarationBuilder )
        {
            if ( current.CopiedTypeParameters == null )
            {
                continue;
            }

            foreach ( var (prototype, copy) in current.CopiedTypeParameters )
            {
                if ( prototype.Equals( typeParameter, TypeComparison.Default ) )
                {
                    return typeParameter.IsNullable == true ? ((IType) copy).ToNullable() : copy;
                }
            }
        }

        return typeParameter;
    }

    internal override IType Visit( IArrayType arrayType )
    {
        var elementType = this.Visit( arrayType.ElementType );

        if ( ReferenceEquals( elementType, arrayType.ElementType ) )
        {
            return arrayType;
        }

        var result = elementType.MakeArrayType( arrayType.Rank );

        return arrayType.IsNullable == true ? result.ToNullable() : result;
    }

    internal override IType Visit( IPointerType pointerType )
    {
        var pointedAtType = this.Visit( pointerType.PointedAtType );

        return ReferenceEquals( pointedAtType, pointerType.PointedAtType ) ? pointerType : pointedAtType.MakePointerType();
    }

    internal override IType Visit( INamedType namedType )
    {
        var result = base.Visit( namedType );

        return !ReferenceEquals( result, namedType ) && namedType.IsNullable == true && result.IsNullable != true ? result.ToNullable() : result;
    }
}
