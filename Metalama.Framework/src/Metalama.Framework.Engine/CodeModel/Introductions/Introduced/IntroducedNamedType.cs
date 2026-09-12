// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Collections;
using Metalama.Framework.Code.Comparers;
using Metalama.Framework.Code.Types;
using Metalama.Framework.Engine.CodeModel.Abstractions;
using Metalama.Framework.Engine.CodeModel.Collections;
using Metalama.Framework.Engine.CodeModel.Facets;
using Metalama.Framework.Engine.CodeModel.GenericContexts;
using Metalama.Framework.Engine.CodeModel.Introductions.BuilderData;
using Metalama.Framework.Engine.CodeModel.Introductions.ConstructedTypes;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.CodeModel.Visitors;
using Metalama.Framework.Engine.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using SpecialType = Metalama.Framework.Code.SpecialType;
using TypeKind = Metalama.Framework.Code.TypeKind;

namespace Metalama.Framework.Engine.CodeModel.Introductions.Introduced;

#pragma warning disable CS0659

internal sealed class IntroducedNamedType : IntroducedMemberOrNamedType, INamedTypeImpl
{
    private readonly NamedTypeBuilderData _namedTypeBuilderData;

    public IntroducedNamedType( NamedTypeBuilderData builderData, CompilationModel compilation, IGenericContext genericContext, bool? isNullable ) : base(
        compilation,
        genericContext )
    {
        this._namedTypeBuilderData = builderData;
        this.IsNullable = isNullable;
    }

    public override DeclarationBuilderData BuilderData => this._namedTypeBuilderData;

    /// <summary>
    /// Gets the immutable data of the builder that introduced this type, which the facet of its kind reads.
    /// </summary>
    internal NamedTypeBuilderData NamedTypeBuilderData => this._namedTypeBuilderData;

    protected override MemberOrNamedTypeBuilderData MemberOrNamedTypeBuilderData => this._namedTypeBuilderData;

    protected override NamedDeclarationBuilderData NamedDeclarationBuilderData => this._namedTypeBuilderData;

    public bool HasDefaultConstructor
        => this.TypeKind == TypeKind.Struct ||
           (this.TypeKind == TypeKind.Class && !this.IsAbstract && !this.IsStatic &&
            this.Constructors.Any( c => c.Parameters.Count == 0 ));

    [Memo]
    public INamedType? BaseType => this.MapDeclaration( this._namedTypeBuilderData.BaseType );

    public IImplementedInterfaceCollection AllImplementedInterfaces
        => new AllImplementedInterfacesCollection(
            this,
            this.Compilation.GetAllInterfaceImplementationCollection( this.Ref, false ) );

    public IImplementedInterfaceCollection ImplementedInterfaces
        => new ImplementedInterfacesCollection(
            this,
            this.Compilation.GetInterfaceImplementationCollection( this.Ref, false ) );

    INamespace INamedType.Namespace => this.ContainingNamespace;

    [Memo]
    public INamespace ContainingNamespace => this.GetContainingNamespace();

    private INamespace GetContainingNamespace()
    {
        var containingDeclaration = this.ContainingDeclaration;

        return containingDeclaration.DeclarationKind switch
        {
            DeclarationKind.Namespace when containingDeclaration is INamespace ns => ns,
            DeclarationKind.NamedType when containingDeclaration is INamedType type => type.ContainingNamespace,
            _ => throw new AssertionFailedException()
        };
    }

    [Memo]
    private IFullRef<INamedType> Ref => this.RefFactory.FromIntroducedDeclaration<INamedType>( this );

    public IRef<INamedType> ToRef() => this.Ref;

    IRef<IType> IType.ToRef() => this.Ref;

    IRef<INamespaceOrNamedType> INamespaceOrNamedType.ToRef() => this.Ref;

    private protected override IFullRef<IDeclaration> ToFullDeclarationRef() => this.Ref;

    INamedTypeCollection INamedType.NestedTypes => this.Types;

    [Memo]
    public string FullName => ((INamespaceOrNamedTypeImpl) this.ContainingDeclaration.AssertNotNull()).FullName + "." + this.Name;

    [Memo]
    public INamedTypeCollection Types
        => new NamedTypeCollection(
            this,
            this.Compilation.GetNamedTypeCollectionByParent( this.Ref ) );

    [Memo]
    public INamedTypeCollection AllTypes => new AllTypesCollection( this );

    [Memo]
    public IPropertyCollection Properties
        => new PropertyCollection(
            this,
            this.Compilation.GetPropertyCollection( this.Ref.DefinitionRef ) );

    [Memo]
    public IPropertyCollection AllProperties => new AllPropertiesCollection( this );

    [Memo]
    public IIndexerCollection Indexers
        => new IndexerCollection(
            this,
            this.Compilation.GetIndexerCollection( this.Ref.DefinitionRef ) );

    [Memo]
    public IIndexerCollection AllIndexers => new AllIndexersCollection( this );

    [Memo]
    public IFieldCollection Fields
        => new FieldCollection(
            this,
            this.Compilation.GetFieldCollection( this.Ref.DefinitionRef ) );

    [Memo]
    public IFieldCollection AllFields => new AllFieldsCollection( this );

    [Memo]
    public IFieldOrPropertyCollection FieldsAndProperties => new FieldAndPropertiesCollection( this.Fields, this.Properties );

    [Memo]
    public IFieldOrPropertyCollection AllFieldsAndProperties => new AllFieldsAndPropertiesCollection( this );

    [Memo]
    public IEventCollection Events
        => new EventCollection(
            this,
            this.Compilation.GetEventCollection( this.Ref.DefinitionRef ) );

    [Memo]
    public IEventCollection AllEvents => new AllEventsCollection( this );

    [Memo]
    public IMethodCollection Methods
        => new MethodCollection(
            this,
            this.Compilation.GetMethodCollection( this.Ref.DefinitionRef ) );

    [Memo]
    public IMethodCollection AllMethods => new AllMethodsCollection( this );

    IConstructor? INamedType.PrimaryConstructor
        => this._namedTypeBuilderData is RecordBuilderData { PrimaryConstructor: { } primaryConstructor }
            ? this.MapDeclaration( primaryConstructor )
            : null;

    [Memo]
    public IConstructorCollection Constructors
        => new ConstructorCollection(
            this,
            this.Compilation.GetConstructorCollection( this.Ref.DefinitionRef ) );

    IConstructor? INamedType.StaticConstructor => null;

    IMethod? INamedType.Finalizer => null;

    [Memo]
    public IExtensionBlockCollection ExtensionBlocks => this.GetExtensionBlocksCore();

    private IExtensionBlockCollection GetExtensionBlocksCore()
    {
        var collection = this.Compilation.GetExtensionBlockCollection( this.Ref );

        return new ExtensionBlockCollection( this, collection.ToImmutableArray() );
    }

    /// <summary>
    /// Gets the facet of the type, which is the structure that its kind gives it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// An introduced type reports the facet of its kind, which is section 5.2 of
    /// <c>Metalama.Framework/docs/introducing-types.md</c>. This is the same call that <c>SourceNamedTypeImpl</c>
    /// makes, and it serves every kind at once, because the collection dispatches on the four flags above and
    /// constructs nothing for a type that has none.
    /// </para>
    /// </remarks>
    [Memo]
    public ITypeFacetCollection Facets => TypeFacetCollection.Create( this );

    public INamedType TypeDefinition => this.Definition;

    [Memo]
    public INamedType Definition => this.Compilation.Factory.GetNamedType( this._namedTypeBuilderData ).AssertNotNull();

    protected override IMemberOrNamedType GetDefinition() => this.Definition;

    /// <summary>
    /// Gets the underlying type, which for an enum is the underlying integral type that the builder data carries and
    /// for every other kind is the type itself.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="Metalama.Framework.Code.Types.IEnumFacet.UnderlyingType"/> reads this property, so an enum that
    /// answered itself here would report itself as its own underlying type.
    /// </para>
    /// </remarks>
    [Memo]
    public INamedType UnderlyingType
        => this._namedTypeBuilderData is EnumBuilderData { UnderlyingType: { } underlyingType }
            ? this.MapDeclaration( underlyingType ).AssertNotNull()
            : this.Definition;

    public TypeKind TypeKind => this._namedTypeBuilderData.TypeKind;

    public SpecialType SpecialType => SpecialType.None;

    public Type ToType() => throw new NotImplementedException();

    /// <summary>
    /// Gets a value indicating whether the type is a reference type.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The test names the two value kinds rather than the reference ones, which is the rule Roslyn applies to a
    /// symbol, so a kind that is added later is reported as a reference type by default. A class, an interface and a
    /// delegate are all reference types, and a struct and an enum are not. A union is reported as a struct, so it is
    /// a value type as well.
    /// </para>
    /// <para>
    /// The value decides more than what this property returns, because <c>ToNullable</c> below branches on it: a
    /// delegate reported as a value type would produce <c>Nullable&lt;TDelegate&gt;</c>, which the language does not
    /// accept. Issue #1840 records the same class of defect.
    /// </para>
    /// </remarks>
    public bool? IsReferenceType => this._namedTypeBuilderData.TypeKind is not (TypeKind.Struct or TypeKind.Enum);

    public bool IsReadOnly => this._namedTypeBuilderData.IsReadOnly;

    public bool IsRef => this._namedTypeBuilderData.IsRef;

    public bool IsDelegate => this._namedTypeBuilderData.TypeKind == TypeKind.Delegate;

    public bool IsEnum => this._namedTypeBuilderData.TypeKind == TypeKind.Enum;


    public bool IsRecord => this._namedTypeBuilderData.IsRecord;

    public bool IsClosed => this._namedTypeBuilderData.IsClosed;

    public bool IsUnion => this._namedTypeBuilderData.IsUnion;

    public bool? IsNullable { get; }

    [Memo]
    public ITypeParameterList TypeParameters
        => new TypeParameterList( this, this._namedTypeBuilderData.TypeParameters.Select( t => t.ToRef() ).ToReadOnlyList() );

    [Memo]
    public IReadOnlyList<IType> TypeArguments
        => this._namedTypeBuilderData.TypeParameters.SelectAsImmutableArray(
            t =>
            {
                // First resolve the type parameter without context to get the ITypeParameter declaration.
                var typeParam = (IType) t.ToRef().GetTarget( this.Compilation );

                // Then map it through the generic context, which substitutes e.g. T -> int.
                return this.GenericContext.Map( typeParam );
            } );

    public bool IsGeneric => this._namedTypeBuilderData.TypeParameters.Length > 0;

    public bool IsCanonicalGenericInstance => this.GenericContext.IsEmptyOrIdentity;

    public ExecutionScope ExecutionScope => ExecutionScope.RunTime;

    public bool Equals( SpecialType specialType ) => false;

    public bool Equals( Type? otherType, TypeComparison typeComparison = TypeComparison.Default )
        => otherType != null && this.Equals( this.Compilation.Factory.GetTypeByReflectionType( otherType ), typeComparison );

    public bool Equals( IType? otherType, TypeComparison typeComparison )
        => this.Compilation.Comparers.GetTypeComparer( typeComparison ).Equals( this, otherType );

    public override bool Equals( object? obj )
        => obj switch
        {
            IType otherType => this.Equals( otherType ),
            Type otherType => this.Equals( otherType ),
            _ => false
        };

    public IArrayType MakeArrayType( int rank = 1 ) => new ConstructedArrayType( this.Compilation, this.Ref, rank );

    public IPointerType MakePointerType() => new ConstructedPointerType( this.Compilation, this.Ref );

    IType IType.ToNullable() => this.ToNullable();

    public IType ToNonNullable()
    {
        if ( this.IsNullable == false )
        {
            return this;
        }
        else if ( this.IsReferenceType ?? true )
        {
            return this.Compilation.Factory.GetNamedType( this._namedTypeBuilderData, this.GenericContext );
        }
        else
        {
            // A value type that is not a Nullable<T> carries no nullability annotation, so there is nothing to remove.
            // The nullable form of an introduced value type is a Nullable<T> constructed over it, which is a type read
            // from source and does not reach this implementation. See issue #1840.
            return this;
        }
    }

    public INamedType StripNullabilityAnnotation()
    {
        if ( this.IsNullable == null )
        {
            return this;
        }
        else
        {
            return this.Compilation.Factory.GetNamedType( this._namedTypeBuilderData, this.GenericContext, null );
        }
    }

    IType IType.StripNullabilityAnnotation() => this.StripNullabilityAnnotation();

    public INamedType ToNullable()
    {
        if ( this.IsNullable == true )
        {
            return this;
        }
        else if ( this.IsReferenceType ?? true )
        {
            return this.Compilation.Factory.GetNamedType( this._namedTypeBuilderData, this.GenericContext, true );
        }
        else
        {
            // The nullable form of a value type is Nullable<T> constructed over it, which is how the implementation
            // for a type read from source answers as well, so that an aspect cannot tell the two apart. See #1840.
            return this.Compilation.Factory.CreateNullableValueType( this );
        }
    }

    public INamedType MakeGenericInstance( IReadOnlyList<IType> typeArguments )
    {
        var genericContext = new IntroducedGenericContext(
            typeArguments.SelectAsImmutableArray( t => t.ToFullRef() ),
            this.ToFullDeclarationRef(),
            this.GenericContext as IntroducedGenericContext );

        return this.Compilation.Factory.GetNamedType( this._namedTypeBuilderData, genericContext );
    }

    public bool Equals( IType? other ) => this.Compilation.Comparers.Default.Equals( this, other );

    public bool Equals( INamedType? other ) => this.Compilation.Comparers.Default.Equals( this, other );

    public override bool CanBeInherited => !this.IsSealed;

    public override IEnumerable<IDeclaration> GetDerivedDeclarations( DerivedTypesOptions options = DerivedTypesOptions.Default )
        => Array.Empty<IDeclaration>(); // TODO

    public bool IsSubclassOf( INamedType type ) => type.SpecialType == SpecialType.Object;

    public bool TryFindImplementationForInterfaceMember( IMember interfaceMember, [NotNullWhen( true )] out IMember? implementationMember )
        => throw new NotImplementedException( "TryFindImplementationForInterfaceMember on introduced types is not yet implemented." );

    IReadOnlyList<IMember> INamedTypeImpl.GetOverridingMembers( IMember member )
        => throw new NotImplementedException( "GetOverridingMembers on introduced types is not yet implemented." );

    bool INamedTypeImpl.IsImplementationOfInterfaceMember( IMember typeMember, IMember interfaceMember )
        => throw new NotSupportedException( "IsImplementationOfInterfaceMember on introduced types is not yet implemented." );

    IType ITypeImpl.Accept( TypeRewriter visitor ) => visitor.Visit( this );
}