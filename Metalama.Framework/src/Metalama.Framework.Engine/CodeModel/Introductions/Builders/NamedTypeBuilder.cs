// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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

internal class NamedTypeBuilder : MemberOrNamedTypeBuilder, INamedTypeBuilder, INamedTypeImpl, IMemberOrNamedTypeBuilderImpl
{
    private INamedType? _baseType;
    private bool _isClosed;

    public TypeKind TypeKind { get; }

    public bool IsRecord { get; }

    /// <summary>
    /// Gets or sets a value indicating whether the introduced type is declared with the <c>closed</c> modifier of
    /// C# 15.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The setter enforces the three restrictions that the language states: the type must be a class, and it must be
    /// neither sealed nor static. Roslyn reports the last two as <c>ERR_ClosedSealedStatic</c>.
    /// </para>
    /// <para>
    /// The setter also refuses the value <c>true</c> when the host that runs Metalama uses a version of Roslyn that
    /// does not offer C# 15. That host is the compiler during a build and the integrated development environment at
    /// design time. The host decides which variant of the engine is loaded, and the variant that serves such a host
    /// cannot emit the modifier.
    /// </para>
    /// </remarks>
    public virtual bool IsClosed
    {
        get => this._isClosed;
        set
        {
            this.CheckNotFrozen();

            if ( value )
            {
#if !(ROSLYN_5_10_0_OR_GREATER && ALLOW_PREVIEW_LANG_VERSION)

                // ModifierHelper.GetTypeSyntaxModifierList emits SyntaxKind.ClosedKeyword under the same condition,
                // because that member exists in the latest Roslyn variant only. Which variant runs is decided by the
                // host, which loads a variant only when its own Roslyn is at least the version that the variant binds
                // against, as Directory.Packages.md describes. The variant that serves a host whose Roslyn predates
                // C# 15 refuses the value instead of generating an ordinary abstract class, so that an aspect never
                // silently produces a hierarchy that is not closed. Remove ALLOW_PREVIEW_LANG_VERSION from this
                // condition, and from the condition of ModifierHelper, when issue #1936 brings a Roslyn that
                // publishes the member without the RSEXPERIMENTAL006 marker.
                throw new InvalidOperationException(
                    $"The type '{this.Name}' cannot be closed because the host that runs Metalama uses a version of Roslyn that does not support the closed modifier of C# 15. At design time, that host is the integrated development environment." );
#else
                if ( this.TypeKind != TypeKind.Class )
                {
                    throw new InvalidOperationException(
                        $"The type '{this.Name}' cannot be closed because the language allows the closed modifier on a class only." );
                }

                if ( this.IsSealed )
                {
                    throw new InvalidOperationException(
                        $"The type '{this.Name}' cannot be closed because it is sealed, and the language forbids the closed modifier on a sealed class." );
                }

                if ( this.IsStatic )
                {
                    throw new InvalidOperationException(
                        $"The type '{this.Name}' cannot be closed because it is static, and the language forbids the closed modifier on a static class." );
                }
#endif
            }

            this._isClosed = value;
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether the introduced type is abstract. The getter returns <c>true</c> when
    /// <see cref="IsClosed"/> is <c>true</c>, because a closed class is implicitly abstract, which is what Roslyn
    /// reports for a closed class declared in source. The setter refuses the value <c>false</c> for a closed type,
    /// for the same reason.
    /// </summary>
    public override bool IsAbstract
    {
        get => base.IsAbstract || this._isClosed;

        set
        {
            this.CheckNotFrozen();

            if ( !value && this._isClosed )
            {
                throw new InvalidOperationException(
                    $"The type '{this.Name}' must be abstract because it is closed, and the language makes a closed class implicitly abstract." );
            }

            base.IsAbstract = value;
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether the introduced type is sealed. The setter refuses a closed type,
    /// because the language forbids the two modifiers together.
    /// </summary>
    public override bool IsSealed
    {
        get => base.IsSealed;
        set
        {
            this.CheckNotFrozen();

            if ( value && this._isClosed )
            {
                throw new InvalidOperationException(
                    $"The type '{this.Name}' cannot be sealed because it is closed, and the language forbids the closed modifier on a sealed class." );
            }

            base.IsSealed = value;
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether the introduced type is static. The setter refuses a closed type,
    /// because the language forbids the two modifiers together.
    /// </summary>
    public override bool IsStatic
    {
        get => base.IsStatic;
        set
        {
            this.CheckNotFrozen();

            if ( value && this._isClosed )
            {
                throw new InvalidOperationException(
                    $"The type '{this.Name}' cannot be static because it is closed, and the language forbids the closed modifier on a static class." );
            }

            base.IsStatic = value;
        }
    }

    public IntroducedRef<INamedType> Ref { get; }

    public TypeParameterBuilderList TypeParameters { get; } = [];

    public NamedTypeBuilder(
        AspectLayerInstance aspectLayerInstance,
        INamespaceOrNamedType declaringNamespaceOrType,
        string name,
        TypeKind typeKind,
        bool isRecord = false ) : base(
        aspectLayerInstance,
        declaringNamespaceOrType as INamedType,
        name )
    {
        Invariant.Assert( typeKind is TypeKind.Class or TypeKind.Struct or TypeKind.Interface or TypeKind.Extension );
        Invariant.Assert( !isRecord ); // Introducing records is not yet supported.

        this.TypeKind = typeKind;
        this.IsRecord = isRecord;

        this.ContainingNamespace = declaringNamespaceOrType.DeclarationKind switch
        {
            DeclarationKind.Namespace when declaringNamespaceOrType is INamespace @namespace => @namespace,
            DeclarationKind.NamedType when declaringNamespaceOrType is INamedType namedType => namedType.ContainingNamespace,
            _ => throw new AssertionFailedException( $"Unsupported: {declaringNamespaceOrType}" )
        };

        this.Ref = new IntroducedRef<INamedType>( this.Compilation.RefFactory );

        // ReSharper disable once VirtualMemberCallInConstructor
        this.InitializeBaseType();

        if ( this.TypeKind == TypeKind.Interface )
        {
            // Interfaces are always abstract.
            // ReSharper disable once VirtualMemberCallInConstructor
            this.IsAbstract = true;
        }
    }

    protected virtual void InitializeBaseType()
    {
        this.BaseType = ((CompilationModel) this.ContainingNamespace.Compilation).Factory.GetSpecialType( SpecialType.Object );
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

    public virtual INamedType? BaseType
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

    public IExtensionBlockCollection ExtensionBlocks => throw new NotImplementedException();

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

    public INamedType StripNullabilityAnnotation() => throw new NotImplementedException();

    IType IType.StripNullabilityAnnotation() => this.StripNullabilityAnnotation();

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
        => this.ContainingDeclaration.DeclarationKind switch
        {
            DeclarationKind.Namespace => this.Compilation.RoslynCompilation.CreateEmptySyntaxTree(
                this.TypeParameters.Count > 0 ? $"{this.FullName}`{this.TypeParameters.Count}.cs" : $"{this.FullName}.cs" ),
            DeclarationKind.NamedType when this.ContainingDeclaration is INamedType namedType => namedType.GetPrimarySyntaxTree().AssertNotNull(),
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