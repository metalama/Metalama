// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Collections;
using Metalama.Framework.Code.Comparers;
using Metalama.Framework.Code.DeclarationBuilders;
using Metalama.Framework.Code.Types;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel.Abstractions;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.CodeModel.Introductions.BuilderData;
using Metalama.Framework.Engine.CodeModel.Introductions.Collections;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.CodeModel.Visitors;
using Metalama.Framework.Engine.Utilities;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Accessibility = Metalama.Framework.Code.Accessibility;
using SpecialType = Metalama.Framework.Code.SpecialType;
using TypeKind = Metalama.Framework.Code.TypeKind;

namespace Metalama.Framework.Engine.CodeModel.Introductions.Builders;

internal sealed class NamedTypeBuilder : MemberOrNamedTypeBuilder, INamedTypeBuilder, INamedTypeImpl, IMemberOrNamedTypeBuilderImpl
{
    private INamedType? _baseType;

    public TypeKind TypeKind { get; }

    public IntroducedRef<INamedType> Ref { get; }

    public TypeParameterBuilderList TypeParameters { get; } = [];

    public NamedTypeBuilder( AspectLayerInstance aspectLayerInstance, INamespaceOrNamedType declaringNamespaceOrType, string name, TypeKind typeKind ) : base(
        aspectLayerInstance,
        declaringNamespaceOrType as INamedType,
        name )
    {
        this.TypeKind = typeKind;

        this.ContainingNamespace = declaringNamespaceOrType switch
        {
            INamespace @namespace => @namespace,
            INamedType namedType => namedType.ContainingNamespace,
            _ => throw new AssertionFailedException( $"Unsupported: {declaringNamespaceOrType}" )
        };

        this.Ref = new IntroducedRef<INamedType>( this.Compilation.RefFactory );

        this.BaseType = ((CompilationModel) this.ContainingNamespace.Compilation).Factory.GetSpecialType( SpecialType.Object );

        if ( this.TypeKind == TypeKind.Interface )
        {
            // Interfaces are always abstract.
            this.IsAbstract = true;
        }
    }

    protected override void FreezeChildren()
    {
        base.FreezeChildren();

        foreach ( var typeParameter in this.TypeParameters )
        {
            typeParameter.Freeze();
        }
    }

    public ITypeParameterBuilder AddTypeParameter( string name )
    {
        this.CheckNotFrozen();

        var builder = new TypeParameterBuilder( this, this.TypeParameters.Count, name );
        this.TypeParameters.Add( builder );

        return builder;
    }

    public override IDeclaration ContainingDeclaration => (IDeclaration?) this.DeclaringType ?? this.ContainingNamespace;

    public bool HasDefaultConstructor => true;

    public INamedType? BaseType
    {
        get => this._baseType;
        set
        {
            this.CheckNotFrozen();

            if ( this.TypeKind == TypeKind.Interface && value?.SpecialType != SpecialType.Object )
            {
                throw new InvalidOperationException( "Interfaces cannot have a base type." );
            }

            this._baseType = value;
        }
    }

    [Memo]
    public IImplementedInterfaceCollection AllImplementedInterfaces => new EmptyImplementedInterfaceCollection();

    [Memo]
    public IImplementedInterfaceCollection ImplementedInterfaces => new EmptyImplementedInterfaceCollection();

    INamespace INamedType.Namespace => this.ContainingNamespace;

    public INamespace ContainingNamespace { get; }

    INamedTypeCollection INamedType.NestedTypes => this.Types;

    INamespace INamedType.ContainingNamespace => this.ContainingNamespace;

    public string FullName
        => this switch
        {
            { DeclaringType: not null } => $"{this.DeclaringType.FullName}.{this.Name}",
            { ContainingNamespace.IsGlobalNamespace: true } => this.Name,
            { ContainingNamespace.IsGlobalNamespace: false } => $"{this.ContainingNamespace.FullName}.{this.Name}",
            _ => throw new AssertionFailedException( $"Unsupported: {this}" )
        };

    [Memo]
    public INamedTypeCollection Types => new EmptyNamedTypeCollection();

    [Memo]
    public INamedTypeCollection AllTypes => new EmptyNamedTypeCollection();

    [Memo]
    public IPropertyCollection Properties => new EmptyPropertyCollection( this );

    [Memo]
    public IPropertyCollection AllProperties => new EmptyPropertyCollection( this );

    [Memo]
    public IIndexerCollection Indexers => new EmptyIndexerCollection( this );

    [Memo]
    public IIndexerCollection AllIndexers => new EmptyIndexerCollection( this );

    [Memo]
    public IFieldCollection Fields => new EmptyFieldCollection( this );

    [Memo]
    public IFieldCollection AllFields => new EmptyFieldCollection( this );

    [Memo]
    public IFieldOrPropertyCollection FieldsAndProperties => new EmptyFieldOrPropertyCollection( this );

    [Memo]
    public IFieldOrPropertyCollection AllFieldsAndProperties => new EmptyFieldOrPropertyCollection( this );

    [Memo]
    public IEventCollection Events => new EmptyEventCollection( this );

    [Memo]
    public IEventCollection AllEvents => new EmptyEventCollection( this );

    [Memo]
    public IMethodCollection Methods => new EmptyMethodCollection( this );

    [Memo]
    public IMethodCollection AllMethods => new EmptyMethodCollection( this );

    public IConstructor? PrimaryConstructor => null;

    [Memo]
    public IConstructorCollection Constructors => new EmptyConstructorCollection( this );

    public IConstructor? StaticConstructor => null;

    public IMethod? Finalizer => null;

    public bool IsReadOnly => false;

    public bool IsRef => false;

    public INamedType TypeDefinition => this;

    public INamedType Definition => this;

    public INamedType UnderlyingType => this;

    public SpecialType SpecialType => SpecialType.None;

    public bool? IsReferenceType => true;

    public bool? IsNullable => false;

    ITypeParameterList IGeneric.TypeParameters => this.TypeParameters;

    [Memo]
    public IReadOnlyList<IType> TypeArguments => Array.Empty<IType>();

    public bool IsGeneric => this.TypeParameters.Count > 0;

    public bool IsCanonicalGenericInstance => true;

    Accessibility IMemberOrNamedType.Accessibility => this.Accessibility;

    public override bool IsDesignTimeObservable => true;

    public int Depth => this.ContainingNamespace.Depth + 1;

    public bool BelongsToCurrentProject => true;

    public ImmutableArray<SourceReference> Sources => ImmutableArray<SourceReference>.Empty;

    IMemberOrNamedType IMemberOrNamedType.Definition => this;

    public override DeclarationKind DeclarationKind => DeclarationKind.NamedType;

    public override bool CanBeInherited => false;

    protected override IFullRef<IDeclaration> ToFullDeclarationRef() => this.Ref;

    public bool Equals( SpecialType specialType ) => false;

    public bool Equals( IType? otherType, TypeComparison typeComparison )
        => this.Compilation.Comparers.GetTypeComparer( typeComparison ).Equals( this, otherType );

    public bool Equals( Type? otherType, TypeComparison typeComparison = TypeComparison.Default )
        => otherType != null && this.Equals( this.Compilation.Factory.GetTypeByReflectionType( otherType ), typeComparison );

    // TODO: Type constructions can't be supported with the current model because the NamedTypeBuilder would need to be frozen,
    // but when these methods would be used (in the build action), it is not frozen yet.

    public IArrayType MakeArrayType( int rank = 1 ) => throw new NotImplementedException();

    public IPointerType MakePointerType() => throw new NotImplementedException();

    public INamedType ToNullable() => throw new NotImplementedException();

    public IType ToNonNullable() => throw new NotImplementedException();

    public INamedType MakeGenericInstance( IReadOnlyList<IType> typeArguments ) => throw new NotImplementedException();

    IType IType.ToNullable() => this.ToNullable();

    public bool Equals( IType? other ) => this.Compilation.Comparers.Default.Equals( this, other );

    public bool Equals( INamedType? other ) => this.Compilation.Comparers.Default.Equals( this, other );

    public bool IsSubclassOf( INamedType type ) => false;

    public Type ToType() => throw new NotImplementedException();

    public bool TryFindImplementationForInterfaceMember( IMember interfaceMember, [NotNullWhen( true )] out IMember? implementationMember )
        => throw new NotSupportedException( "This method is not supported on the builder." );

    IReadOnlyList<IMember> INamedTypeImpl.GetOverridingMembers( IMember member )
        => throw new NotSupportedException( "This method is not supported on the builder." );

    bool INamedTypeImpl.IsImplementationOfInterfaceMember( IMember typeMember, IMember interfaceMember )
        => throw new NotSupportedException( "This method is not supported on the builder." );

    IType ITypeImpl.Accept( TypeRewriter visitor ) => visitor.Visit( this );

    [Memo]
    public override SyntaxTree PrimarySyntaxTree
        => this.ContainingDeclaration switch
        {
            INamespace => this.Compilation.RoslynCompilation.CreateEmptySyntaxTree(
                this.TypeParameters.Count > 0 ? $"{this.FullName}`{this.TypeParameters.Count}.cs" : $"{this.FullName}.cs" ),
            INamedType namedType => namedType.GetPrimarySyntaxTree().AssertNotNull(),
            _ => throw new AssertionFailedException( $"Unsupported: {this.ContainingDeclaration}" )
        };

    IRef<INamedType> INamedType.ToRef() => this.Ref;

    IRef<INamespaceOrNamedType> INamespaceOrNamedType.ToRef() => this.Ref;

    IRef<IType> IType.ToRef() => this.Ref;

    protected override void EnsureReferenceInitialized()
    {
        this.Ref.BuilderData = new NamedTypeBuilderData( this, this.ContainingDeclaration.ToFullRef() );
    }

    public NamedTypeBuilderData BuilderData => (NamedTypeBuilderData) this.Ref.BuilderData;
}