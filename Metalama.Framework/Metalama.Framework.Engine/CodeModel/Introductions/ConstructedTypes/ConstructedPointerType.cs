// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Comparers;
using Metalama.Framework.Code.Types;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.CodeModel.Visitors;
using System;

namespace Metalama.Framework.Engine.CodeModel.Introductions.ConstructedTypes;

internal sealed class ConstructedPointerType : ConstructedType, IPointerType
{
    private readonly IFullRef<IType> _pointedAtType;

    public ConstructedPointerType( CompilationModel compilation, IFullRef<IType> pointedAtType ) : base( compilation )
    {
        this._pointedAtType = pointedAtType;
    }

    public override ICompilationElement Translate( CompilationModel newCompilation, IGenericContext? genericContext = null, Type? interfaceType = null )
    {
        if ( ReferenceEquals( newCompilation, this.Compilation ) )
        {
            return this;
        }
        else
        {
            return new ConstructedPointerType( newCompilation, this._pointedAtType );
        }
    }

    public override IType Accept( TypeRewriter visitor ) => visitor.Visit( this );

    protected override IFullRef<IType> ToTypeFullRef() => this.Compilation.RefFactory.FromConstructedType<IArrayType>( this );

    public override TypeKind TypeKind => TypeKind.Pointer;

    public override bool? IsReferenceType => false;

    public override bool? IsNullable => false;

    public IType PointedAtType => this._pointedAtType.GetTarget( this.Compilation );

    public override bool Equals( IType? otherType, TypeComparison typeComparison )
    {
        if ( otherType is not ConstructedPointerType otherConstructedPointerType )
        {
            // We assume that the type cannot be represented as a symbol-backed type, i.e. the decision
            // to create a ConstructedType vs a SymbolConstructedType must be properly taken upstream.
            return false;
        }

        if ( !this._pointedAtType.Equals( otherConstructedPointerType._pointedAtType, typeComparison.ToRefComparison() ) )
        {
            return false;
        }

        return true;
    }

    public override int GetHashCode( TypeComparison typeComparison )
        => HashCode.Combine( this._pointedAtType.GetHashCode( typeComparison.ToRefComparison() ), 541 );

    protected override IType ToNullableCore() => throw new NotSupportedException();

    protected override IType ToNonNullableCore() => throw new NotSupportedException();
}