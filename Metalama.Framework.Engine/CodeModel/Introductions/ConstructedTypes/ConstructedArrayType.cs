// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Comparers;
using Metalama.Framework.Code.Types;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.CodeModel.Visitors;
using System;
using TypeKind = Metalama.Framework.Code.TypeKind;

namespace Metalama.Framework.Engine.CodeModel.Introductions.ConstructedTypes;

internal sealed class ConstructedArrayType : ConstructedType, IArrayType
{
    private readonly IFullRef<IType> _elementType;

    public ConstructedArrayType( CompilationModel compilation, IFullRef<IType> elementType, int rank, bool? isNullable = false ) : base( compilation )
    {
        this._elementType = elementType;
        this.Rank = rank;
        this.IsNullable = isNullable;
    }

    public override ICompilationElement Translate( CompilationModel newCompilation, IGenericContext? genericContext = null, Type? interfaceType = null )
    {
        if ( ReferenceEquals( newCompilation, this.Compilation ) )
        {
            return this;
        }
        else
        {
            return new ConstructedArrayType( newCompilation, this._elementType, this.Rank, this.IsNullable );
        }
    }

    public override IType Accept( TypeRewriter visitor ) => visitor.Visit( this );

    protected override IFullRef<IType> ToTypeFullRef() => this.Compilation.RefFactory.FromConstructedType<IArrayType>( this );

    public override TypeKind TypeKind => TypeKind.Array;

    public override bool? IsReferenceType => true;

    public override bool? IsNullable { get; }

    public IType ElementType => this._elementType.GetTarget( this.Compilation );

    public int Rank { get; }

    public override bool Equals( IType? otherType, TypeComparison typeComparison )
    {
        if ( otherType is not ConstructedArrayType otherConstructedArrayType )
        {
            // We assume that the type cannot be represented as a symbol-backed type, i.e. the decision
            // to create a ConstructedType vs a SymbolConstructedType must be properly taken upstream.
            return false;
        }

        if ( !this._elementType.Equals( otherConstructedArrayType._elementType, typeComparison.ToRefComparison() ) )
        {
            return false;
        }

        if ( this.Rank != otherConstructedArrayType.Rank )
        {
            return false;
        }

        if ( typeComparison == TypeComparison.IncludeNullability && this.IsNullable != otherConstructedArrayType.IsNullable )
        {
            return false;
        }

        return true;
    }

    public override int GetHashCode( TypeComparison typeComparison )
    {
        var hashCode = HashCode.Combine( this._elementType.GetHashCode( typeComparison.ToRefComparison() ), this.Rank );

        if ( typeComparison == TypeComparison.IncludeNullability )
        {
            hashCode = HashCode.Combine( hashCode, this.IsNullable );
        }

        return hashCode;
    }

    public new IArrayType ToNullable() => this.IsNullable == true ? this : new ConstructedArrayType( this.Compilation, this._elementType, this.Rank, true );

    public new IArrayType ToNonNullable() => this.IsNullable == false ? this : new ConstructedArrayType( this.Compilation, this._elementType, this.Rank );

    protected override IType ToNullableCore() => this.ToNullable();

    protected override IType ToNonNullableCore() => this.ToNonNullable();
}