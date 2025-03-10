// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Comparers;
using Metalama.Framework.Code.Types;
using Metalama.Framework.Engine.CodeModel.Abstractions;
using Metalama.Framework.Engine.CodeModel.Introductions.BuilderData;
using Metalama.Framework.Engine.CodeModel.Introductions.ConstructedTypes;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.Utilities;
using System;
using System.Collections.Generic;

namespace Metalama.Framework.Engine.CodeModel.Introductions.Introduced;

internal sealed class IntroducedTypeParameter : IntroducedDeclaration, ITypeParameter
{
    private readonly TypeParameterBuilderData _typeParameterBuilderData;
    private readonly bool? _isNullableOverride;

    public IntroducedTypeParameter(
        TypeParameterBuilderData builder,
        CompilationModel compilation,
        IGenericContext genericContext,
        bool? isNullableOverride ) : base(
        compilation,
        genericContext )
    {
        this._typeParameterBuilderData = builder;
        this._isNullableOverride = isNullableOverride;
    }

    public override DeclarationBuilderData BuilderData => this._typeParameterBuilderData;

    public TypeKind TypeKind => TypeKind.TypeParameter;

    public SpecialType SpecialType => SpecialType.None;

    public Type ToType() => throw new NotImplementedException();

    public bool? IsReferenceType => this._typeParameterBuilderData.IsReferenceType;

    public bool? IsNullable => this._isNullableOverride ?? this._typeParameterBuilderData.IsNullable;

    bool IType.Equals( SpecialType specialType ) => false;

    public bool Equals( IType? otherType, TypeComparison typeComparison )
        => otherType is IntroducedTypeParameter otherBuildTypeParameter && otherBuildTypeParameter.BuilderData.Equals( this.BuilderData );

    public IArrayType MakeArrayType( int rank = 1 ) => new ConstructedArrayType( this.Compilation, this.Ref, rank );

    public IPointerType MakePointerType() => new ConstructedPointerType( this.Compilation, this.Ref );

    private IType ToNullable()
    {
        if ( this.IsNullable == true )
        {
            return this;
        }
        else if ( this.IsReferenceType ?? true )
        {
            return this.Compilation.Factory.GetTypeParameter( this._typeParameterBuilderData, this.GenericContext, true );
        }
        else
        {
            return this.Compilation.Factory.CreateNullableValueType( this );
        }
    }

    public ITypeParameter ToNonNullable()
        => this.IsNullable == false ? this : this.Compilation.Factory.GetTypeParameter( this._typeParameterBuilderData, this.GenericContext );

    IType IType.ToNullable() => this.ToNullable();

    IType IType.ToNonNullable() => this.ToNonNullable();

    ICompilation ICompilationElement.Compilation => this.Compilation;

    public string Name => this._typeParameterBuilderData.Name;

    public int Index => this._typeParameterBuilderData.Index;

    [Memo]
    public IReadOnlyList<IType> TypeConstraints => this.MapDeclarationList( this._typeParameterBuilderData.TypeConstraints );

    public TypeKindConstraint TypeKindConstraint => this._typeParameterBuilderData.TypeKindConstraint;

    public bool AllowsRefStruct => this._typeParameterBuilderData.AllowsRefStruct;

    public VarianceKind Variance => this._typeParameterBuilderData.Variance;

    public bool? IsConstraintNullable => this._typeParameterBuilderData.IsConstraintNullable;

    public bool HasDefaultConstructorConstraint => this._typeParameterBuilderData.HasDefaultConstructorConstraint;

    private IFullRef<ITypeParameter> Ref => this.RefFactory.FromIntroducedDeclaration<ITypeParameter>( this );

    IRef<ITypeParameter> ITypeParameter.ToRef() => this.Ref;

    [Memo]
    public IType ResolvedType => this.MapType( this.Ref );

    public TypeParameterKind TypeParameterKind
        => this.ContainingDeclaration.DeclarationKind switch
        {
            DeclarationKind.NamedType => TypeParameterKind.Type,
            DeclarationKind.Method => TypeParameterKind.Method,
            _ => throw new AssertionFailedException()
        };

    IRef<IType> IType.ToRef() => this.Ref;

    private protected override IFullRef<IDeclaration> ToFullDeclarationRef() => this.Ref;

    public bool Equals( IType? other ) => this.Equals( other, TypeComparison.Default );

    public bool Equals( Type? otherType, TypeComparison typeComparison = TypeComparison.Default )
        => otherType != null && this.Equals( this.Compilation.Factory.GetTypeByReflectionType( otherType ), typeComparison );

    public override int GetHashCode() => this._typeParameterBuilderData.GetHashCode();

    public override bool CanBeInherited => ((IDeclarationImpl) this.ContainingDeclaration.AssertNotNull()).CanBeInherited;

    public override IEnumerable<IDeclaration> GetDerivedDeclarations( DerivedTypesOptions options = default ) => throw new NotImplementedException();
}