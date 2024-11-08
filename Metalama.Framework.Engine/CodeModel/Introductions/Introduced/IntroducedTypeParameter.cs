// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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

internal sealed class IntroducedTypeParameter(
    TypeParameterBuilderData builder,
    CompilationModel compilation,
    IGenericContext genericContext,
    bool? isNullableOverride )
    : IntroducedDeclaration( compilation, genericContext ), ITypeParameter
{
    public override DeclarationBuilderData BuilderData => builder;

    public TypeKind TypeKind => TypeKind.TypeParameter;

    public SpecialType SpecialType => SpecialType.None;

    public Type ToType() => throw new NotImplementedException();

    public bool? IsReferenceType => builder.IsReferenceType;

    public bool? IsNullable => isNullableOverride ?? builder.IsNullable;

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
            return this.Compilation.Factory.GetTypeParameter( builder, this.GenericContext, true );
        }
        else
        {
            return this.Compilation.Factory.CreateNullableValueType( this );
        }
    }

    public ITypeParameter ToNonNullable()
        => this.IsNullable == false ? this : this.Compilation.Factory.GetTypeParameter( builder, this.GenericContext );

    IType IType.ToNullable() => this.ToNullable();

    IType IType.ToNonNullable() => this.ToNonNullable();

    ICompilation ICompilationElement.Compilation => this.Compilation;

    public string Name => builder.Name;

    public int Index => builder.Index;

    [Memo]
    public IReadOnlyList<IType> TypeConstraints => this.MapDeclarationList( builder.TypeConstraints );

    public TypeKindConstraint TypeKindConstraint => builder.TypeKindConstraint;

    public bool AllowsRefStruct => builder.AllowsRefStruct;

    public VarianceKind Variance => builder.Variance;

    public bool? IsConstraintNullable => builder.IsConstraintNullable;

    public bool HasDefaultConstructorConstraint => builder.HasDefaultConstructorConstraint;

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

    public override int GetHashCode() => builder.GetHashCode();

    public override bool CanBeInherited => ((IDeclarationImpl) this.ContainingDeclaration.AssertNotNull()).CanBeInherited;

    public override IEnumerable<IDeclaration> GetDerivedDeclarations( DerivedTypesOptions options = default ) => throw new NotImplementedException();
}