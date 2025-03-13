// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Comparers;
using Metalama.Framework.Code.Types;
using Metalama.Framework.Engine.CodeModel.Abstractions;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.CodeModel.Visitors;
using System;
using SpecialType = Metalama.Framework.Code.SpecialType;
using TypeKind = Metalama.Framework.Code.TypeKind;

namespace Metalama.Framework.Engine.CodeModel.Introductions.ConstructedTypes;

internal abstract class ConstructedType : ITypeImpl
{
    protected ConstructedType( CompilationModel compilation )
    {
        this.Compilation = compilation;
    }

    ICompilation ICompilationElement.Compilation => this.Compilation;

    public abstract ICompilationElement Translate( CompilationModel newCompilation, IGenericContext? genericContext = null, Type? interfaceType = null );

    public abstract IType Accept( TypeRewriter visitor );

    public CompilationModel Compilation { get; }

    DeclarationKind ICompilationElement.DeclarationKind => DeclarationKind.Type;

    public string ToDisplayString( CodeDisplayFormat? format = null, CodeDisplayContext? context = null )
        => DisplayStringFormatter.Format( this, format, context );

    public bool Equals( IType? other ) => this.Equals( other, TypeComparison.Default );

    public IRef<IType> ToRef() => this.ToTypeFullRef();

    protected abstract IFullRef<IType> ToTypeFullRef();

    public abstract TypeKind TypeKind { get; }

    SpecialType IType.SpecialType => SpecialType.None;

    public Type ToType() => throw new NotImplementedException();

    public abstract bool? IsReferenceType { get; }

    public abstract bool? IsNullable { get; }

    public bool Equals( SpecialType specialType ) => false;

    public abstract bool Equals( IType? otherType, TypeComparison typeComparison );

    public bool Equals( Type? otherType, TypeComparison typeComparison = TypeComparison.Default )
        => otherType != null && this.Equals( this.Compilation.Factory.GetTypeByReflectionType( otherType ), typeComparison );

    public override bool Equals( object? obj )
        => obj switch
        {
            IType otherType => this.Equals( otherType ),
            Type otherType => this.Equals( otherType ),
            _ => false
        };

    public IArrayType MakeArrayType( int rank = 1 ) => new ConstructedArrayType( this.Compilation, this.ToTypeFullRef(), rank, isNullable: false );

    public IPointerType MakePointerType() => new ConstructedPointerType( this.Compilation, this.ToTypeFullRef() );

    public IType ToNullable() => this.ToNullableCore();

    public IType ToNonNullable() => this.ToNonNullableCore();

    protected abstract IType ToNullableCore();

    protected abstract IType ToNonNullableCore();

    public abstract int GetHashCode( TypeComparison refComparison );

    public override int GetHashCode() => this.GetHashCode( TypeComparison.Default );
}