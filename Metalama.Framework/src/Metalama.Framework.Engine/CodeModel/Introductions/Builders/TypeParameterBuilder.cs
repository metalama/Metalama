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
using Metalama.Framework.Engine.CodeModel.Introductions.ConstructedTypes;
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
    private NullableTypeParameterBuilder? _nullable;

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
            this.ValidateChange( nameof(this.TypeKindConstraint), value != this._typeKindConstraint );
            this._typeKindConstraint = value;
        }
    }

    public override string Name
    {
        get => this._name;
        set
        {
            this.CheckNotFrozen();
            this.ValidateChange( nameof(this.Name), value != this._name );
            this._name = value;
        }
    }

    public VarianceKind Variance
    {
        get => this._variance;
        set
        {
            this.CheckNotFrozen();
            this.ValidateChange( nameof(this.Variance), value != this._variance );
            this._variance = value;
        }
    }

    public bool AllowsRefStruct
    {
        get => this._allowsRefStruct;
        set
        {
            this.CheckNotFrozen();
            this.ValidateChange( nameof(this.AllowsRefStruct), value != this._allowsRefStruct );

            this._allowsRefStruct = value;
        }
    }

    public bool? IsConstraintNullable
    {
        get => this._isConstraintNullable;
        set
        {
            this.CheckNotFrozen();
            this.ValidateChange( nameof(this.IsConstraintNullable), value != this._isConstraintNullable );
            this._isConstraintNullable = value;
        }
    }

    public bool HasDefaultConstructorConstraint
    {
        get => this._hasDefaultConstructorConstraint;
        set
        {
            this.CheckNotFrozen();
            this.ValidateChange( nameof(this.HasDefaultConstructorConstraint), value != this._hasDefaultConstructorConstraint );
            this._hasDefaultConstructorConstraint = value;
        }
    }

    public void AddTypeConstraint( IType type )
    {
        this.CheckNotFrozen();
        this.ValidateChange( nameof(this.TypeConstraints), true );
        this._typeConstraints.Add( this.Translate( type ) );
    }

    public void AddTypeConstraint( Type type )
    {
        this.CheckNotFrozen();
        this.ValidateChange( nameof(this.TypeConstraints), true );
        this._typeConstraints.Add( this.Compilation.Factory.GetTypeByReflectionType( type ) );
    }

    /// <summary>
    /// Replaces each type constraint by the result of a mapping, without validating the change with <see cref="Restrictions"/>. The declaring
    /// builder uses it to replace the prototypes of copied type parameters by their copies.
    /// </summary>
    internal void MapTypeConstraints( Func<IType, IType> map )
    {
        this.CheckNotFrozen();

        for ( var i = 0; i < this._typeConstraints.Count; i++ )
        {
            this._typeConstraints[i] = this.Translate( map( this._typeConstraints[i] ) );
        }
    }

    /// <summary>
    /// Gets the restrictions of the method builder that declares the type parameter, or <c>null</c>.
    /// </summary>
    internal override MethodBuilderRestrictions? Restrictions => (this.ContainingDeclaration as DeclarationBuilder)?.Restrictions;

    /// <summary>
    /// Validates a change of the type parameter with <see cref="Restrictions"/>, when the value changes.
    /// </summary>
    private void ValidateChange( string propertyName, bool isChanged )
    {
        if ( isChanged )
        {
            this.Restrictions?.ValidateTypeParameterChange( this, propertyName );
        }
    }

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

    /// <summary>
    /// Returns an array type of this type parameter.
    /// </summary>
    /// <remarks>
    /// The array type refers to the type parameter through its reference, which is resolved only when the element type is read. The element
    /// type can therefore be read only after the declaring builder is frozen, but the array type can be assigned to a builder before.
    /// </remarks>
    public IArrayType MakeArrayType( int rank = 1 ) => new ConstructedArrayType( this.Compilation, this._ref, rank );

    /// <summary>
    /// Returns a pointer type of this type parameter. The remarks of <see cref="MakeArrayType"/> apply.
    /// </summary>
    public IPointerType MakePointerType() => new ConstructedPointerType( this.Compilation, this._ref );

    /// <summary>
    /// Returns this type parameter, because a type parameter of a builder never carries a nullable annotation.
    /// </summary>
    public ITypeParameter ToNonNullable() => this;

    /// <summary>
    /// Returns this type parameter, because a type parameter of a builder never carries a nullable annotation.
    /// </summary>
    public ITypeParameter StripNullabilityAnnotation() => this;

    /// <summary>
    /// Returns the nullable form of this type parameter.
    /// </summary>
    /// <remarks>
    /// When the type parameter is constrained to value types, the nullable form is the generic instance <see cref="Nullable{T}"/>. Otherwise,
    /// it is the type parameter with a nullable annotation, represented by a <see cref="NullableTypeParameterBuilder"/>.
    /// </remarks>
    IType IType.ToNullable()
        => this.IsReferenceType == false
            ? this.Compilation.Factory.CreateNullableValueType( this )
            : this._nullable ??= new NullableTypeParameterBuilder( this, this._ref );

    IType IType.ToNonNullable() => this.ToNonNullable();

    IType IType.StripNullabilityAnnotation() => this.StripNullabilityAnnotation();

    IRef<ITypeParameter> ITypeParameter.ToRef() => this._ref;

    public IType ResolvedType => this;

    public TypeParameterKind TypeParameterKind
        => this.ContainingDeclaration.DeclarationKind switch
        {
            DeclarationKind.NamedType or DeclarationKind.ExtensionBlock => TypeParameterKind.Type,
            DeclarationKind.Method => TypeParameterKind.Method,
            _ => throw new AssertionFailedException()
        };

    protected override IFullRef<IDeclaration> ToFullDeclarationRef() => this._ref;

    IRef<IType> IType.ToRef() => this._ref;

    protected override void EnsureReferenceInitialized()
        => this._ref.BuilderData = new TypeParameterBuilderData( this, this.ContainingDeclaration.ToFullRef() );
}