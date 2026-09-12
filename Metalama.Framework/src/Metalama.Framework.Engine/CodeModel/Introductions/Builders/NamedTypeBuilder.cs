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
    private bool _isReadOnly;
    private bool _isRef;

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
#if !ROSLYN_5_11_0_OR_GREATER

                // ModifierHelper.GetTypeSyntaxModifierList emits SyntaxKind.ClosedKeyword under the same condition,
                // because that member exists in the latest Roslyn variant only. Which variant runs is decided by the
                // host, which loads a variant only when its own Roslyn is at least the version that the variant binds
                // against, as Directory.Packages.md describes. The variant that serves a host whose Roslyn predates
                // C# 15 refuses the value instead of generating an ordinary abstract class, so that an aspect never
                // silently produces a hierarchy that is not closed.
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

    /// <summary>
    /// Gets a value indicating whether the introduced type is a union written with the <c>union</c> keyword, which
    /// <c>UnionBuilder</c> is and no other builder is.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The language reports a union declaration as a struct, so the type kind alone does not tell a union apart.
    /// Only the form written with the <c>union</c> keyword is introduced, which section 6.3 of
    /// <c>Metalama.Framework/docs/introducing-unions.md</c> decides.
    /// </para>
    /// </remarks>
    public virtual bool IsUnion => false;

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
        Invariant.Assert(
            typeKind is TypeKind.Class or TypeKind.Struct or TypeKind.Interface or TypeKind.Extension or TypeKind.Enum or TypeKind.Delegate );

        // A record is a class or a struct that carries the record modifier, which is how the code model represents it:
        // TypeKind.RecordClass and TypeKind.RecordStruct are obsolete. See Metalama.Framework/docs/introducing-records.md.
        Invariant.Assert( !isRecord || typeKind is TypeKind.Class or TypeKind.Struct );

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

    /// <summary>
    /// Sets the base type that the language gives to a type of this kind. The value is the semantic base, which is
    /// what the code model reports, and not the base list that is emitted: a struct, an enum and a delegate emit no
    /// base list, and an enum emits its underlying type in that position instead.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A kind that has a builder class of its own overrides this method rather than adding an arm here, so that
    /// the class of the kind carries what the language gives that kind.
    /// </para>
    /// </remarks>
    protected virtual void InitializeBaseType()
    {
        // This class represents a class, an interface, a struct and an extension block, so it decides between the
        // two bases those four kinds have. An enum and a delegate have a class of their own and override this.
        this.SetBaseTypeCore(
            this.TypeKind == TypeKind.Struct
                ? this.Compilation.Factory.GetSpecialType( SpecialType.ValueType )
                : this.Compilation.Factory.GetSpecialType( SpecialType.Object ) );
    }

    /// <summary>
    /// Assigns the base type without passing through the setter of <see cref="BaseType"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A kind whose language form has no base list refuses every assignment through the setter, and the base type of
    /// such a kind is still what the code model reports, so the initialization assigns the field instead of the
    /// property.
    /// </para>
    /// </remarks>
    protected void SetBaseTypeCore( INamedType? baseType ) => this._baseType = baseType;

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

    /// <summary>
    /// Gets the facet of the type, which a builder does not have.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A builder describes a type that is being constructed, whose members are not resolvable, so it has no
    /// structure to report, and reporting an empty structure would be a false answer rather than an incomplete one.
    /// An aspect that reads the facet of a builder has made a mistake, and the exception says so at the place the
    /// mistake was made.
    /// </para>
    /// <para>
    /// See section 5.1 of <c>Metalama.Framework/docs/introducing-types.md</c>, which supersedes implementation
    /// guideline 5 of <c>type-facets.md</c>. The flags below do not throw, which is what keeps a caller that asks
    /// what kind a type is working: only a caller that asks for the structure meets the exception.
    /// </para>
    /// </remarks>
    public ITypeFacetCollection Facets
        => throw new NotSupportedException(
            $"The type '{this.Name}' is still being constructed, so it has no facet. Read the facet of the introduced type, which the advice returns." );

    /// <summary>
    /// Gets or sets a value indicating whether the type is declared with the <c>readonly</c> modifier, which the
    /// language allows on a struct only.
    /// </summary>
    public virtual bool IsReadOnly
    {
        get => this._isReadOnly;
        set
        {
            this.CheckNotFrozen();

            if ( value && this.TypeKind != TypeKind.Struct )
            {
                throw new InvalidOperationException(
                    $"The type '{this.Name}' cannot be readonly because the language allows the readonly modifier on a struct only." );
            }

            this._isReadOnly = value;
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether the type is declared with the <c>ref</c> modifier, which the language
    /// allows on a struct that is not a record.
    /// </summary>
    public virtual bool IsRef
    {
        get => this._isRef;
        set
        {
            this.CheckNotFrozen();

            if ( value && this.TypeKind != TypeKind.Struct )
            {
                throw new InvalidOperationException(
                    $"The type '{this.Name}' cannot be a ref struct because the language allows the ref modifier on a struct only." );
            }

            this._isRef = value;
        }
    }

    public bool IsDelegate => this.TypeKind == TypeKind.Delegate;

    public bool IsEnum => this.TypeKind == TypeKind.Enum;

    public INamedType TypeDefinition => this;

    public INamedType Definition => this;

    /// <summary>
    /// Gets the underlying type, which an enum overrides with its underlying integral type. Every other kind is its
    /// own underlying type.
    /// </summary>
    public virtual INamedType UnderlyingType => this;

    public SpecialType SpecialType => SpecialType.None;

    /// <summary>
    /// Gets a value indicating whether the type is a reference type.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The test names the two value kinds rather than the reference ones, which is the rule Roslyn applies to a
    /// symbol, so a kind that is added later is reported as a reference type by default. A class, an interface, a
    /// delegate and an extension block are all reference types, and a struct and an enum are not. A union is
    /// reported as a struct, so it is a value type as well.
    /// </para>
    /// <para>
    /// The value decides more than what this property returns, because <c>ToNullable</c> branches on it: a delegate
    /// reported as a value type would produce <c>Nullable&lt;TDelegate&gt;</c>, which the language does not accept.
    /// Issue #1840 records the same class of defect.
    /// </para>
    /// </remarks>
    public bool? IsReferenceType => this.TypeKind is not (TypeKind.Struct or TypeKind.Enum);

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
        this.Ref.BuilderData = this.CreateBuilderData( this.ContainingDeclaration.ToFullRef() );
    }

    /// <summary>
    /// Creates the immutable data of this builder. A kind that carries more than <see cref="NamedTypeBuilderData"/>
    /// overrides this method and returns the derived class of that kind.
    /// </summary>
    protected virtual NamedTypeBuilderData CreateBuilderData( IFullRef<IDeclaration> containingDeclaration )
        => new( this, containingDeclaration );

    public NamedTypeBuilderData BuilderData => (NamedTypeBuilderData) this.Ref.BuilderData;
}