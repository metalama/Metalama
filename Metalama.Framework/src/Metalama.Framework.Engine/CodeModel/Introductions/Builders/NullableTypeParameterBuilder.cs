// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Collections;
using Metalama.Framework.Code.Comparers;
using Metalama.Framework.Code.Types;
using Metalama.Framework.Engine.CodeModel.Abstractions;
using Metalama.Framework.Engine.CodeModel.Introductions.ConstructedTypes;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.CodeModel.Visitors;
using System;
using System.Collections.Generic;

namespace Metalama.Framework.Engine.CodeModel.Introductions.Builders;

/// <summary>
/// Represents the nullable annotation of a <see cref="TypeParameterBuilder"/> that can be a reference type, for instance <c>T?</c> for an
/// unconstrained <c>T</c>, before the builder is frozen.
/// </summary>
/// <remarks>
/// <para>
/// A type parameter of a builder has no declaration of its own for the annotated form until it is frozen. This class reads the properties of
/// the builder, reports <see cref="IsNullable"/> as <c>true</c>, and returns a reference to the builder that carries the annotation, so the
/// declarations that use it resolve the annotated type parameter once the builder is frozen.
/// </para>
/// </remarks>
internal sealed class NullableTypeParameterBuilder : ITypeParameter, ITypeImpl
{
    private readonly TypeParameterBuilder _builder;
    private readonly IntroducedRef<ITypeParameter> _ref;

    public NullableTypeParameterBuilder( TypeParameterBuilder builder, IntroducedRef<ITypeParameter> builderRef )
    {
        this._builder = builder;
        this._ref = builderRef.WithNullability( true );
    }

    /// <summary>
    /// Gets the builder of the type parameter.
    /// </summary>
    public TypeParameterBuilder Builder => this._builder;

    public string Name => this._builder.Name;

    public int Index => this._builder.Index;

    public IReadOnlyList<IType> TypeConstraints => this._builder.TypeConstraints;

    public TypeKindConstraint TypeKindConstraint => this._builder.TypeKindConstraint;

    public bool AllowsRefStruct => this._builder.AllowsRefStruct;

    public VarianceKind Variance => this._builder.Variance;

    public bool? IsConstraintNullable => this._builder.IsConstraintNullable;

    public bool HasDefaultConstructorConstraint => this._builder.HasDefaultConstructorConstraint;

    public TypeParameterKind TypeParameterKind => this._builder.TypeParameterKind;

    public IType ResolvedType => this;

    public TypeKind TypeKind => TypeKind.TypeParameter;

    public SpecialType SpecialType => SpecialType.None;

    public bool? IsReferenceType => this._builder.IsReferenceType;

    public bool? IsNullable => true;

    public CompilationModel Compilation => this._builder.Compilation;

    ICompilation ICompilationElement.Compilation => this._builder.Compilation;

    public DeclarationKind DeclarationKind => DeclarationKind.TypeParameter;

    public IDeclarationOrigin Origin => this._builder.Origin;

    public IDeclaration? ContainingDeclaration => this._builder.ContainingDeclaration;

    public IAttributeCollection Attributes => ((IDeclaration) this._builder).Attributes;

    public bool IsImplicitlyDeclared => this._builder.IsImplicitlyDeclared;

    public int Depth => ((IDeclaration) this._builder).Depth;

    public bool BelongsToCurrentProject => ((IDeclaration) this._builder).BelongsToCurrentProject;

    public System.Collections.Immutable.ImmutableArray<SourceReference> Sources => ((IDeclaration) this._builder).Sources;

    public IGenericContext GenericContext => ((IDeclaration) this._builder).GenericContext;

    public Type ToType() => throw new NotImplementedException();

    public bool Equals( SpecialType specialType ) => false;

    public bool Equals( IType? other ) => this.Equals( other, TypeComparison.Default );

    public bool Equals( IType? otherType, TypeComparison typeComparison )
        => otherType switch
        {
            NullableTypeParameterBuilder other => ReferenceEquals( other._builder, this._builder ),
            TypeParameterBuilder other => typeComparison != TypeComparison.IncludeNullability && ReferenceEquals( other, this._builder ),
            _ => false
        };

    public bool Equals( Type? otherType, TypeComparison typeComparison = TypeComparison.Default ) => false;

    public bool Equals( IDeclaration? other ) => other is IType otherType && this.Equals( otherType );

    public override bool Equals( object? obj ) => obj is IType otherType && this.Equals( otherType );

    public override int GetHashCode() => this._builder.GetHashCode();

    public IArrayType MakeArrayType( int rank = 1 ) => new ConstructedArrayType( this._builder.Compilation, this._ref, rank );

    public IPointerType MakePointerType() => throw new NotSupportedException( "A pointer type cannot have a nullable element type." );

    IType IType.ToNullable() => this;

    IType IType.ToNonNullable() => this._builder;

    IType IType.StripNullabilityAnnotation() => this._builder;

    public ITypeParameter ToNonNullable() => this._builder;

    public ITypeParameter StripNullabilityAnnotation() => this._builder;

    IRef<ITypeParameter> ITypeParameter.ToRef() => this._ref;

    IRef<IType> IType.ToRef() => this._ref;

    IRef<IDeclaration> IDeclaration.ToRef() => this._ref;

    public SerializableDeclarationId ToSerializableId() => throw new NotSupportedException();

    public IAssembly DeclaringAssembly => this._builder.DeclaringAssembly;

    public string ToDisplayString( CodeDisplayFormat? format = null, CodeDisplayContext? context = null )
        => this._builder.ToDisplayString( format, context ) + "?";

    public override string ToString() => this.ToDisplayString();

    public IType Accept( TypeRewriter visitor ) => visitor.Visit( this );

    public ICompilationElement Translate( CompilationModel newCompilation, IGenericContext? genericContext = null, Type? interfaceType = null )
        => ReferenceEquals( newCompilation, this._builder.Compilation )
            ? this
            : this._ref.GetTarget( newCompilation, genericContext, interfaceType );
}
