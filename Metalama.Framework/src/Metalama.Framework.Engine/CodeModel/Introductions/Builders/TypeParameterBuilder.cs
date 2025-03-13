// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Comparers;
using Metalama.Framework.Code.DeclarationBuilders;
using Metalama.Framework.Code.Types;
using Metalama.Framework.Engine.CodeModel.Abstractions;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.CodeModel.Introductions.BuilderData;
using Metalama.Framework.Engine.CodeModel.References;
using System;
using System.Collections.Generic;
using SpecialType = Metalama.Framework.Code.SpecialType;
using TypeKind = Metalama.Framework.Code.TypeKind;
using TypeParameterKind = Metalama.Framework.Code.TypeParameterKind;
using VarianceKind = Metalama.Framework.Code.VarianceKind;

namespace Metalama.Framework.Engine.CodeModel.Introductions.Builders;

#pragma warning disable CS0659

internal sealed class TypeParameterBuilder : NamedDeclarationBuilder, ITypeParameterBuilder
{
    private readonly IntroducedRef<ITypeParameter> _ref;

    private readonly List<IType> _typeConstraints = [];
    private bool _allowsRefStruct;
    private TypeKindConstraint _typeKindConstraint;
    private VarianceKind _variance;
    private bool? _isConstraintNullable;
    private bool _hasDefaultConstructorConstraint;
    private string _name;

    public TypeParameterBuilder( MethodBuilder containingMethod, int index, string name ) : base( containingMethod.AspectLayerInstance )
    {
        this.ContainingDeclaration = containingMethod;
        this.Index = index;
        this._ref = new IntroducedRef<ITypeParameter>( this.Compilation.RefFactory );
        this._name = name;
    }

    public TypeParameterBuilder( NamedTypeBuilder containingType, int index, string name ) : base( containingType.AspectLayerInstance )
    {
        this.ContainingDeclaration = containingType;
        this.Index = index;
        this._ref = new IntroducedRef<ITypeParameter>( this.Compilation.RefFactory );
        this._name = name;
    }

    public int Index { get; }

    public IReadOnlyList<IType> TypeConstraints => this._typeConstraints;

    public TypeKindConstraint TypeKindConstraint
    {
        get => this._typeKindConstraint;
        set
        {
            this.CheckNotFrozen();
            this._typeKindConstraint = value;
        }
    }

    public override string Name
    {
        get => this._name;
        set
        {
            this.CheckNotFrozen();
            this._name = value;
        }
    }

    public VarianceKind Variance
    {
        get => this._variance;
        set
        {
            this.CheckNotFrozen();
            this._variance = value;
        }
    }

    public bool AllowsRefStruct
    {
        get => this._allowsRefStruct;
        set
        {
            this.CheckNotFrozen();

            this._allowsRefStruct = value;
        }
    }

    public bool? IsConstraintNullable
    {
        get => this._isConstraintNullable;
        set
        {
            this.CheckNotFrozen();
            this._isConstraintNullable = value;
        }
    }

    public bool HasDefaultConstructorConstraint
    {
        get => this._hasDefaultConstructorConstraint;
        set
        {
            this.CheckNotFrozen();
            this._hasDefaultConstructorConstraint = value;
        }
    }

    public void AddTypeConstraint( IType type ) => this._typeConstraints.Add( this.Translate( type ) );

    public void AddTypeConstraint( Type type ) => this._typeConstraints.Add( this.Compilation.Factory.GetTypeByReflectionType( type ) );

    TypeKind IType.TypeKind => TypeKind.TypeParameter;

    public SpecialType SpecialType => SpecialType.None;

    public Type ToType() => throw new NotImplementedException();

    public bool? IsReferenceType => this.IsReferenceTypeImpl();

    public bool? IsNullable => this.IsNullableImpl();

    ICompilation ICompilationElement.Compilation => this.Compilation;

    public override bool IsDesignTimeObservable => true;

    public override IDeclaration ContainingDeclaration { get; }

    public override DeclarationKind DeclarationKind => DeclarationKind.TypeParameter;

    public override bool CanBeInherited => ((IDeclarationImpl) this.ContainingDeclaration).CanBeInherited;

    bool IType.Equals( SpecialType specialType ) => false;

    bool IEquatable<IType>.Equals( IType? other ) => this.Equals( other, TypeComparison.Default );

    public bool Equals( IType? otherType, TypeComparison typeComparison )
        => this.Compilation.Comparers.GetTypeComparer( typeComparison ).Equals( this, otherType );

    public bool Equals( Type? otherType, TypeComparison typeComparison = TypeComparison.Default )
        => otherType != null && this.Equals( this.Compilation.Factory.GetTypeByReflectionType( otherType ), typeComparison );

    public override bool Equals( object? obj )
        => obj switch
        {
            IType otherType => this.Equals( otherType ),
            Type otherType => this.Equals( otherType ),
            _ => false
        };

    // TODO: Type constructions can't be supported with the current model because the NamedTypeBuilder would need to be frozen,
    // but when these methods would be used (in the build action), it is not frozen yet.

    public IArrayType MakeArrayType( int rank = 1 ) => throw new NotImplementedException();

    public IPointerType MakePointerType() => throw new NotImplementedException();

    public ITypeParameter ToNonNullable() => throw new NotImplementedException();

    IType IType.ToNullable() => throw new NotImplementedException();

    IType IType.ToNonNullable() => this.ToNonNullable();

    IRef<ITypeParameter> ITypeParameter.ToRef() => this._ref;

    public IType ResolvedType => this;

    public TypeParameterKind TypeParameterKind
        => this.ContainingDeclaration.DeclarationKind switch
        {
            DeclarationKind.NamedType => TypeParameterKind.Type,
            DeclarationKind.Method => TypeParameterKind.Method,
            _ => throw new AssertionFailedException()
        };

    protected override IFullRef<IDeclaration> ToFullDeclarationRef() => this._ref;

    IRef<IType> IType.ToRef() => this._ref;

    protected override void EnsureReferenceInitialized() 
        => this._ref.BuilderData = new TypeParameterBuilderData( this, this.ContainingDeclaration.ToFullRef() );
}