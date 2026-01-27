// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if ROSLYN_5_0_0_OR_GREATER
using Metalama.Framework.Code;
using Metalama.Framework.Code.Collections;
using Metalama.Framework.Code.Comparers;
using Metalama.Framework.Code.Types;
using Metalama.Framework.Engine.CodeModel.Abstractions;
using Metalama.Framework.Engine.CodeModel.Collections;
using Metalama.Framework.Engine.CodeModel.GenericContexts;
using Metalama.Framework.Engine.CodeModel.Introductions.BuilderData;
using Metalama.Framework.Engine.CodeModel.Introductions.Collections;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.CodeModel.Visitors;
using Metalama.Framework.Engine.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using SpecialType = Metalama.Framework.Code.SpecialType;
using TypeKind = Metalama.Framework.Code.TypeKind;

namespace Metalama.Framework.Engine.CodeModel.Introductions.Introduced;

#pragma warning disable CS0659

/// <summary>
/// Represents an introduced extension block in the code model.
/// </summary>
internal sealed class IntroducedExtensionBlock : IntroducedMemberOrNamedType, IExtensionBlock, INamedTypeImpl
{
    private readonly ExtensionBlockBuilderData _builderData;

    public IntroducedExtensionBlock(
        ExtensionBlockBuilderData builderData,
        CompilationModel compilation,
        IGenericContext genericContext )
        : base( compilation, genericContext )
    {
        this._builderData = builderData;
    }

    public override DeclarationBuilderData BuilderData => this._builderData;

    protected override MemberOrNamedTypeBuilderData MemberOrNamedTypeBuilderData => this._builderData;

    protected override NamedDeclarationBuilderData NamedDeclarationBuilderData => this._builderData;

    public override DeclarationKind DeclarationKind => DeclarationKind.ExtensionBlock;

    #region IExtensionBlock Implementation

    [Memo]
    public IType ReceiverType => this.MapType( this._builderData.ReceiverParameter.Type );

    [Memo]
    public IParameter ReceiverParameter
        => new IntroducedParameter( this._builderData.ReceiverParameter, this.Compilation, this.GenericContext, this );

    public new INamedType DeclaringType => base.DeclaringType.AssertNotNull();

    public IRef<IExtensionBlock> ToRef() => this.Ref;

    #endregion

    #region INamedType Implementation

    public bool HasDefaultConstructor => false;

    public INamedType? BaseType => null;

    public IImplementedInterfaceCollection AllImplementedInterfaces => new EmptyImplementedInterfaceCollection();

    public IImplementedInterfaceCollection ImplementedInterfaces => new EmptyImplementedInterfaceCollection();

    INamespace INamedType.Namespace => this.ContainingNamespace;

    [Memo]
    public INamespace ContainingNamespace => this.GetContainingNamespace();

    private INamespace GetContainingNamespace()
        => this.ContainingDeclaration switch
        {
            INamespace ns => ns,
            INamedType type => type.ContainingNamespace,
            _ => throw new AssertionFailedException()
        };

    [Memo]
    private IFullRef<IExtensionBlock> Ref => this.RefFactory.FromIntroducedDeclaration<IExtensionBlock>( this );

    IRef<INamedType> INamedType.ToRef() => this.Ref.As<INamedType>();

    IRef<IType> IType.ToRef() => this.Ref.As<IType>();

    IRef<INamespaceOrNamedType> INamespaceOrNamedType.ToRef() => this.Ref.As<INamespaceOrNamedType>();

    private protected override IFullRef<IDeclaration> ToFullDeclarationRef() => this.Ref;

    INamedTypeCollection INamedType.NestedTypes => this.Types;

    [Memo]
    public string FullName => ((INamespaceOrNamedTypeImpl) this.ContainingDeclaration.AssertNotNull()).FullName + ".<extension>";

    [Memo]
    public INamedTypeCollection Types => new EmptyNamedTypeCollection();

    [Memo]
    public INamedTypeCollection AllTypes => new EmptyNamedTypeCollection();

    [Memo]
    public IPropertyCollection Properties
        => new PropertyCollection(
            this,
            this.Compilation.GetPropertyCollection( this.Ref.DefinitionRef.As<INamedType>() ) );

    [Memo]
    public IPropertyCollection AllProperties => new AllPropertiesCollection( this );

    [Memo]
    public IIndexerCollection Indexers
        => new IndexerCollection(
            this,
            this.Compilation.GetIndexerCollection( this.Ref.DefinitionRef.As<INamedType>() ) );

    [Memo]
    public IIndexerCollection AllIndexers => new AllIndexersCollection( this );

    [Memo]
    public IFieldCollection Fields => new EmptyFieldCollection( this ); // Extension blocks cannot have fields.

    [Memo]
    public IFieldCollection AllFields => new EmptyFieldCollection( this );

    [Memo]
    public IFieldOrPropertyCollection FieldsAndProperties => new FieldAndPropertiesCollection( this.Fields, this.Properties );

    [Memo]
    public IFieldOrPropertyCollection AllFieldsAndProperties => new AllFieldsAndPropertiesCollection( this );

    [Memo]
    public IEventCollection Events => new EmptyEventCollection( this ); // Extension blocks cannot have events.

    [Memo]
    public IEventCollection AllEvents => new EmptyEventCollection( this );

    [Memo]
    public IMethodCollection Methods
        => new MethodCollection(
            this,
            this.Compilation.GetMethodCollection( this.Ref.DefinitionRef.As<INamedType>() ) );

    [Memo]
    public IMethodCollection AllMethods => new AllMethodsCollection( this );

    IConstructor? INamedType.PrimaryConstructor => null;

    [Memo]
    public IConstructorCollection Constructors => new EmptyConstructorCollection( this ); // Extension blocks cannot have constructors

    IConstructor? INamedType.StaticConstructor => null;

    IMethod? INamedType.Finalizer => null;

    [Memo]
    public IExtensionBlockCollection ExtensionBlocks => new ExtensionBlockCollection( this, [] );

    public INamedType TypeDefinition => this.Definition;

    [Memo]
    public INamedType Definition => this.Compilation.Factory.GetExtensionBlock( this._builderData ).AssertNotNull();

    protected override IMemberOrNamedType GetDefinition() => this.Definition;

    public INamedType UnderlyingType => this.Definition;

    public TypeKind TypeKind => TypeKind.Extension;

    public SpecialType SpecialType => SpecialType.None;

    public Type ToType() => throw new NotSupportedException( "Extension blocks cannot be converted to System.Type." );

    public bool? IsReferenceType => true;

    public bool IsReadOnly => false;

    public bool IsRef => false;

    public bool IsRecord => false;

    public bool? IsNullable => false; // Extension blocks don't support nullability annotations.

    [Memo]
    public ITypeParameterList TypeParameters => new TypeParameterList( this, this._builderData.TypeParameters.Select( t => t.ToRef() ).ToReadOnlyList() );

    [Memo]
    public IReadOnlyList<IType> TypeArguments => this._builderData.TypeParameters.SelectAsImmutableArray( t => this.MapType( t.ToRef() ) );

    public bool IsGeneric => this._builderData.TypeParameters.Length > 0;

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

    public IArrayType MakeArrayType( int rank = 1 ) => throw new NotSupportedException( "Extension blocks cannot be used as array element types." );

    public IPointerType MakePointerType() => throw new NotSupportedException( "Extension blocks cannot be used as pointer types." );

    // Extension blocks don't support nullability annotations, so these methods return this.
    IType IType.ToNullable() => this;

    public IType ToNonNullable() => this;

    public INamedType StripNullabilityAnnotation() => this;

    IType IType.StripNullabilityAnnotation() => this;

    public INamedType ToNullable() => this;

    public INamedType MakeGenericInstance( IReadOnlyList<IType> typeArguments )
    {
        var genericContext = new IntroducedGenericContext(
            typeArguments.SelectAsImmutableArray( t => t.ToFullRef() ),
            this.ToFullDeclarationRef(),
            this.GenericContext as IntroducedGenericContext );

        return this.Compilation.Factory.GetExtensionBlock( this._builderData, genericContext );
    }

    public bool Equals( IType? other ) => this.Compilation.Comparers.Default.Equals( this, other );

    public bool Equals( INamedType? other ) => this.Compilation.Comparers.Default.Equals( this, other );

    public override bool CanBeInherited => false;

    public override IEnumerable<IDeclaration> GetDerivedDeclarations( DerivedTypesOptions options = DerivedTypesOptions.Default )
        => []; // Extension blocks cannot be inherited.

    public bool IsSubclassOf( INamedType type ) => false;

    public bool TryFindImplementationForInterfaceMember( IMember interfaceMember, [NotNullWhen( true )] out IMember? implementationMember )
    {
        implementationMember = null;

        return false; // Extension blocks don't implement interfaces.
    }

    IReadOnlyList<IMember> INamedTypeImpl.GetOverridingMembers( IMember member ) => [];

    bool INamedTypeImpl.IsImplementationOfInterfaceMember( IMember typeMember, IMember interfaceMember ) => false;

    IType ITypeImpl.Accept( TypeRewriter visitor ) => visitor.Visit( this );

    #endregion
}
#endif