// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.AdviceImpl.Attributes;
using Metalama.Framework.Engine.AdviceImpl.InterfaceImplementation;
using Metalama.Framework.Engine.AdviceImpl.Introduction;
using Metalama.Framework.Engine.CodeModel.Introductions.BuilderData;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.CodeModel.UpdatableCollections;
using Metalama.Framework.Engine.Collections;
using Metalama.Framework.Engine.Transformations;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Immutable;
using System.Linq;
using MethodKind = Metalama.Framework.Code.MethodKind;

namespace Metalama.Framework.Engine.CodeModel;

public sealed partial class CompilationModel
{
    private ImmutableDictionary<IFullRef<INamedType>, FieldUpdatableCollection> _fields;
    private ImmutableDictionary<IFullRef<INamedType>, MethodUpdatableCollection> _methods;
    private ImmutableDictionary<IFullRef<INamedType>, ConstructorUpdatableCollection> _constructors;
    private ImmutableDictionary<IFullRef<INamedType>, EventUpdatableCollection> _events;
    private ImmutableDictionary<IFullRef<INamedType>, PropertyUpdatableCollection> _properties;
    private ImmutableDictionary<IFullRef<INamedType>, IndexerUpdatableCollection> _indexers;
    private ImmutableDictionary<IFullRef<INamedType>, InterfaceUpdatableCollection> _interfaceImplementations;
    private ImmutableDictionary<IFullRef<INamedType>, AllInterfaceUpdatableCollection> _allInterfaceImplementations;
    private ImmutableDictionary<IFullRef<IHasParameters>, ParameterUpdatableCollection> _parameters;
    private ImmutableDictionary<IFullRef<IDeclaration>, AttributeUpdatableCollection> _attributes;
    private ImmutableDictionary<IFullRef<INamedType>, ConstructorBuilderData> _staticConstructors;
    private ImmutableDictionary<IFullRef<INamedType>, MethodBuilderData> _finalizers;
    private ImmutableDictionary<IFullRef<INamespaceOrNamedType>, TypeUpdatableCollection> _namedTypesByParent;
    private ImmutableDictionary<IFullRef<INamespace>, NamespaceUpdatableCollection> _namespaces;
    private TypeUpdatableCollection? _topLevelNamedTypes;
    private ImmutableDictionary<string, NamespaceBuilderData> _namespaceBuilders;
#if ROSLYN_5_0_0_OR_GREATER
    private ImmutableDictionary<IFullRef<INamedType>, ExtensionBlockUpdatableCollection> _extensionBlocks;
#endif

    internal ImmutableDictionaryOfArray<IRef<IDeclaration>, AnnotationInstance> Annotations { get; private set; }

    private bool IsMutable { get; }

    /// <summary>
    /// Gets the current revision number, which is incremented each time a transformation is added. Used to detect
    /// cache staleness in collections that cache computed results (e.g. <c>AllMethods</c>).
    /// </summary>
    internal int Revision { get; private set; }

    private TCollection GetMemberCollection<TOwner, TCollection>(
        ref ImmutableDictionary<IFullRef<TOwner>, TCollection> dictionary,
        bool requestMutableCollection,
        IFullRef<TOwner> declaration,
        Func<CompilationModel, IFullRef<TOwner>, TCollection> createCollection )
        where TOwner : class, IDeclaration
        where TCollection : IUpdatableCollection
    {
        Invariant.Assert( !(requestMutableCollection && !this.IsMutable) );

        // Normalize to definition ref. Callers like IntroducedNamedType may pass a constructed ref
        // (e.g. Test<int>) whose generic context is handled by the collection's facade, not by this dictionary.
        if ( !declaration.IsDefinition )
        {
            declaration = declaration.DefinitionRef;
        }

        // If the model is mutable, we need to return a mutable collection because it may be mutated after the
        // front-end collection is returned.
        var returnMutableCollection = requestMutableCollection || this.IsMutable;

        if ( dictionary.TryGetValue( declaration, out var collection ) )
        {
            if ( !ReferenceEquals( collection.Compilation, this ) && returnMutableCollection )
            {
                // The UpdateArray was created in another compilation snapshot, so it is not mutable in the current compilation.
                // We need to take a copy of it.
                collection = (TCollection) collection.Clone( this.Compilation );
                dictionary = dictionary.SetItem( declaration, collection );
            }
        }
        else
        {
            collection = createCollection( this.Compilation, declaration );
            dictionary = dictionary.SetItem( declaration, collection );
        }

        return collection;
    }

    internal FieldUpdatableCollection GetFieldCollection( IFullRef<INamedType> declaringType, bool mutable = false )
        => this.GetMemberCollection<INamedType, FieldUpdatableCollection>(
            ref this._fields,
            mutable,
            declaringType,
            static ( c, t ) => new FieldUpdatableCollection( c, t ) );

    internal MethodUpdatableCollection GetMethodCollection( IFullRef<INamedType> declaringType, bool mutable = false )
        => this.GetMemberCollection<INamedType, MethodUpdatableCollection>(
            ref this._methods,
            mutable,
            declaringType,
            static ( c, t ) => new MethodUpdatableCollection( c, t ) );

    internal ConstructorUpdatableCollection GetConstructorCollection( IFullRef<INamedType> declaringType, bool mutable = false )
        => this.GetMemberCollection<INamedType, ConstructorUpdatableCollection>(
            ref this._constructors,
            mutable,
            declaringType,
            static ( c, t ) => new ConstructorUpdatableCollection( c, t ) );

    internal PropertyUpdatableCollection GetPropertyCollection( IFullRef<INamedType> declaringType, bool mutable = false )
        => this.GetMemberCollection<INamedType, PropertyUpdatableCollection>(
            ref this._properties,
            mutable,
            declaringType,
            static ( c, t ) => new PropertyUpdatableCollection( c, t ) );

    internal IndexerUpdatableCollection GetIndexerCollection( IFullRef<INamedType> declaringType, bool mutable = false )
        => this.GetMemberCollection<INamedType, IndexerUpdatableCollection>(
            ref this._indexers,
            mutable,
            declaringType,
            static ( c, t ) => new IndexerUpdatableCollection( c, t ) );

    internal EventUpdatableCollection GetEventCollection( IFullRef<INamedType> declaringType, bool mutable = false )
        => this.GetMemberCollection<INamedType, EventUpdatableCollection>(
            ref this._events,
            mutable,
            declaringType,
            static ( c, t ) => new EventUpdatableCollection( c, t ) );

    internal InterfaceUpdatableCollection GetInterfaceImplementationCollection( IFullRef<INamedType> declaringType, bool mutable )
        => this.GetMemberCollection<INamedType, InterfaceUpdatableCollection>(
            ref this._interfaceImplementations,
            mutable,
            declaringType,
            ( c, t ) => new InterfaceUpdatableCollection( c, t ) );

    internal AllInterfaceUpdatableCollection GetAllInterfaceImplementationCollection( IFullRef<INamedType> declaringType, bool mutable )
        => this.GetMemberCollection<INamedType, AllInterfaceUpdatableCollection>(
            ref this._allInterfaceImplementations,
            mutable,
            declaringType,
            static ( c, t ) => new AllInterfaceUpdatableCollection( c, t ) );

    internal ParameterUpdatableCollection GetParameterCollection( IFullRef<IHasParameters> parent, bool mutable = false )
        => this.GetMemberCollection<IHasParameters, ParameterUpdatableCollection>(
            ref this._parameters,
            mutable,
            parent,
            static ( c, t ) => new ParameterUpdatableCollection( c, t ) );

    internal TypeUpdatableCollection GetNamedTypeCollectionByParent( IFullRef<INamespaceOrNamedType> parent, bool mutable = false )
        => this.GetMemberCollection<INamespaceOrNamedType, TypeUpdatableCollection>(
            ref this._namedTypesByParent,
            mutable,
            parent,
            static ( c, t ) => new TypeUpdatableCollection( c, t ) );

    private TypeUpdatableCollection GetTopLevelNamedTypeCollection( bool mutable = false )
    {
        if ( this._topLevelNamedTypes != null )
        {
            if ( !ReferenceEquals( this._topLevelNamedTypes.Compilation, this ) && mutable )
            {
                // The UpdateArray was created in another compilation snapshot, so it is not mutable in the current compilation.
                // We need to take a copy of it.
                this._topLevelNamedTypes = (TypeUpdatableCollection) this._topLevelNamedTypes.Clone( this.Compilation );
            }
        }
        else
        {
            this._topLevelNamedTypes = new TypeUpdatableCollection( this.Compilation );
        }

        return this._topLevelNamedTypes;
    }

    internal NamespaceUpdatableCollection GetNamespaceCollection( IFullRef<INamespace> declaringNamespace, bool mutable = false )
        => this.GetMemberCollection<INamespace, NamespaceUpdatableCollection>(
            ref this._namespaces,
            mutable,
            declaringNamespace,
            static ( c, t ) => new NamespaceUpdatableCollection( c, t ) );

#if ROSLYN_5_0_0_OR_GREATER
    internal ExtensionBlockUpdatableCollection GetExtensionBlockCollection( IFullRef<INamedType> declaringType, bool mutable = false )
        => this.GetMemberCollection<INamedType, ExtensionBlockUpdatableCollection>(
            ref this._extensionBlocks,
            mutable,
            declaringType,
            static ( c, t ) => new ExtensionBlockUpdatableCollection( c, t ) );
#endif

    internal AttributeUpdatableCollection GetAttributeCollection( IFullRef<IDeclaration> parent, bool mutable = false )
        => this.GetMemberCollection<IDeclaration, AttributeUpdatableCollection>(
            ref this._attributes,
            mutable,
            parent,
            static ( c, t ) => new AttributeUpdatableCollection( c, t ) );

    internal ConstructorBuilderData? GetStaticConstructor( INamedTypeSymbol declaringType )
    {
        this._staticConstructors.TryGetValue( declaringType.ToRef( this.RefFactory ), out var value );

        return value;
    }

    internal MethodBuilderData? GetFinalizer( INamedTypeSymbol declaringType )
    {
        this._finalizers.TryGetValue( declaringType.ToRef( this.RefFactory ), out var value );

        return value;
    }

    internal void AddTransformation( ITransformation transformation )
    {
        if ( !this.IsMutable )
        {
            throw new InvalidOperationException( "Cannot add transformation to an immutable compilation." );
        }

        if ( transformation.Observability == TransformationObservability.None )
        {
            return;
        }

        this.Revision++;

        // Replaced declaration should be always removed before adding the replacement.
        // ReSharper disable once ConvertIfStatementToSwitchStatement
        if ( transformation is IReplaceMemberTransformation replaceMember )
        {
            this.AddReplaceMemberTransformation( replaceMember );
        }

        if ( transformation is RemoveAttributesTransformation removeAttributes )
        {
            this.RemoveAttributes( removeAttributes );
        }

        // IMPORTANT: Keep the builder interface in this condition for linker tests, which use fake builders.
        if ( transformation is IIntroduceDeclarationTransformation introduceDeclarationTransformation )
        {
            var builder = introduceDeclarationTransformation.DeclarationBuilderData;

            this.AddDeclaration( builder );
        }

        if ( transformation is ReplaceParameterTransformation replaceParameterTransformation )
        {
            var parameterCollection = this.GetParameterCollection(
                replaceParameterTransformation.Parameter.ContainingDeclaration.As<IHasParameters>(),
                true );

            parameterCollection.Replace( replaceParameterTransformation.ReplacedParameterIndex, replaceParameterTransformation.Parameter );
        }
        else if ( transformation is IntroduceParameterTransformation appendParameterTransformation )
        {
            this.AddDeclaration( appendParameterTransformation.Parameter );
        }

        if ( transformation is IIntroduceInterfaceTransformation introduceInterface )
        {
            this.AddIntroduceInterfaceTransformation( introduceInterface );
        }

        if ( transformation is AddAnnotationTransformation addAnnotationTransformation )
        {
            this.AddAnnotation( addAnnotationTransformation );
        }

        if ( transformation is SetHasImplementationTransformation setHasImplementation )
        {
            this._membersWithSetImplementation = this._membersWithSetImplementation.Add( setHasImplementation.TargetMember );
        }
    }

    private void AddAnnotation( AddAnnotationTransformation addAnnotationTransformation )
        => this.Annotations =
            this.Annotations.Add(
                addAnnotationTransformation.TargetDeclaration,
                addAnnotationTransformation.AnnotationInstance );

    private void RemoveAttributes( RemoveAttributesTransformation removeAttributes )
    {
        var attributes = this.GetAttributeCollection( removeAttributes.ContainingDeclaration, true );
        attributes.Remove( removeAttributes.AttributeType );
    }

    private void AddReplaceMemberTransformation( IReplaceMemberTransformation transformation )
    {
        if ( transformation.ReplacedMember == null )
        {
            return;
        }

        var replaced = transformation.ReplacedMember;
        this.Factory.Invalidate( replaced );

        switch ( replaced )
        {
            case IFullRef<IConstructor> replacedConstructor:
                if ( !replacedConstructor.Definition.IsStatic )
                {
                    var constructors = this.GetConstructorCollection( replacedConstructor.ContainingDeclaration.AssertNotNull().As<INamedType>(), true );
                    constructors.Remove( replacedConstructor );
                }
                else
                {
                    // Nothing to do, static constructor is replaced in the collection earlier.
                }

                break;

            case IFullRef<IField> replacedField:
                var fields = this.GetFieldCollection( replacedField.ContainingDeclaration.AssertNotNull().As<INamedType>(), true );
                fields.Remove( replacedField );

                break;

            default:
                throw new AssertionFailedException( $"Unexpected declaration: '{replaced}'." );
        }

        // Update the redirection cache.
        if ( transformation is { ReplacedMember: { } replacedMember } )
        {
            if ( transformation is IIntroduceDeclarationTransformation introduceDeclarationTransformation )
            {
                var newBuilder = introduceDeclarationTransformation.DeclarationBuilderData;

                Invariant.Assert( !(replacedMember is IIntroducedRef replacedBuilderRef && newBuilder.Equals( replacedBuilderRef.BuilderData )) );

                this._redirections = this._redirections.Add( replacedMember, newBuilder );
            }
            else
            {
                throw new AssertionFailedException( $"Unexpected transformation type: {transformation.GetType()}." );
            }
        }
    }

    /// <summary>
    /// Gets a reference to the declaration of <paramref name="ns"/> in the merged namespace tree, or <c>null</c> if
    /// <paramref name="ns"/> and the merged namespace tree share a single declaration of that namespace.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A compilation exposes two namespace trees. The tree of <see cref="IAssembly.GlobalNamespace"/> is backed by the
    /// namespaces of the source module and contains the declarations of this compilation only. It is the tree passed
    /// to an aspect, therefore the tree into which an aspect introduces a namespace or a type. The other tree is
    /// merged over this compilation and its references. The resolution of a <see cref="SerializableTypeId"/> or of a
    /// <see cref="SerializableDeclarationId"/> starts there, because it is the only tree that contains the types of
    /// the referenced assemblies.
    /// </para>
    /// <para>
    /// A namespace declared both by this compilation and by a referenced assembly has one declaration in each tree,
    /// and each of them has its own collections of child namespaces and types. A declaration added to one of these
    /// collections is not returned by the other, and an identifier naming it therefore fails to resolve.
    /// <see cref="AddDeclaration"/> adds it to both. See issue #1825.
    /// </para>
    /// <para>
    /// This method returns <c>null</c> in the two cases where a single declaration exists and one insertion is
    /// therefore sufficient. The first is a namespace introduced by an aspect: it has no symbol, and
    /// <see cref="AddDeclaration"/> has already added it to both trees, so both return that instance. The second is a
    /// namespace declared by this compilation only: Roslyn returns the single constituent instead of creating a merged
    /// namespace, so both trees again return one instance. The global namespace is declared by every assembly and has
    /// two declarations in every compilation.
    /// </para>
    /// </remarks>
    private IFullRef<INamespace>? GetMergedNamespaceRef( IFullRef<INamespace> ns )
    {
        if ( ns is not ISymbolRef { Symbol: INamespaceSymbol moduleNamespace } )
        {
            // The namespace was introduced by an aspect and has no symbol.
            return null;
        }

        var mergedNamespace = this.RoslynCompilation.GetCompilationNamespace( moduleNamespace );

        if ( mergedNamespace == null || SymbolEqualityComparer.Default.Equals( mergedNamespace, moduleNamespace ) )
        {
            // Roslyn returned the single constituent instead of a merged namespace.
            return null;
        }

        return mergedNamespace.ToRef( this.RefFactory ).As<INamespace>();
    }

    internal void AddDeclaration( DeclarationBuilderData declaration )
    {
        // TODO Perf: switch on DeclarationKind,

        switch ( declaration )
        {
            case MethodBuilderData { MethodKind: MethodKind.Finalizer } finalizer:
                var finalizerDeclaringType = finalizer.DeclaringType;

                if ( this._finalizers.ContainsKey( finalizerDeclaringType ) )
                {
                    // Duplicate.
                    throw new AssertionFailedException( $"The type '{finalizer.DeclaringType}' already contains a finalizer." );
                }

                this._finalizers = this._finalizers.SetItem( finalizerDeclaringType, finalizer );

                break;

            case MethodBuilderData method:
                var methods = this.GetMethodCollection( method.DeclaringType, true ).AssertCast<MethodUpdatableCollection>();
                methods.Add( method.ToRef() );

                break;

            case ConstructorBuilderData { IsStatic: false } constructor:
                var constructors = this.GetConstructorCollection( constructor.DeclaringType, true );
                constructors.Add( constructor.ToRef() );

                break;

            case ConstructorBuilderData { IsStatic: true } staticConstructorBuilder:
                var staticCtorDeclaringType = staticConstructorBuilder.DeclaringType;

                if ( this._staticConstructors.ContainsKey( staticCtorDeclaringType ) )
                {
                    // Duplicate.
                    throw new AssertionFailedException( $"The type '{staticConstructorBuilder.DeclaringType}' already contains a static constructor." );
                }

                this._staticConstructors = this._staticConstructors.SetItem( staticCtorDeclaringType, staticConstructorBuilder );

                break;

            case FieldBuilderData field:
                var fields = this.GetFieldCollection( field.DeclaringType, true );
                fields.Add( field.ToRef() );

                break;

            case PropertyBuilderData property:
                var properties = this.GetPropertyCollection( property.DeclaringType, true );
                properties.Add( property.ToRef() );

                break;

            case IndexerBuilderData indexer:
                var indexers = this.GetIndexerCollection( indexer.DeclaringType, true );
                indexers.Add( indexer.ToRef() );

                break;

            case EventBuilderData @event:
                var events = this.GetEventCollection( @event.DeclaringType, true );
                events.Add( @event.ToRef() );

                break;

            case ParameterBuilderData parameter:
                var parameters = this.GetParameterCollection( parameter.ContainingDeclaration.As<IHasParameters>(), true );
                parameters.Add( parameter );

                break;

            case AttributeBuilderData attribute:
                var attributes = this.GetAttributeCollection( attribute.ContainingDeclaration, true );
                attributes.Add( attribute );

                break;

            case NamedTypeBuilderData namedType:
                var declaringNamespaceOrType = namedType.ContainingDeclaration.AssertNotNull().As<INamespaceOrNamedType>();

                this.GetNamedTypeCollectionByParent( declaringNamespaceOrType, true ).Add( namedType.ToRef() );

                if ( namedType.DeclaringType == null )
                {
                    // The containing declaration is a namespace, which can have a second declaration in the merged tree.
                    if ( this.GetMergedNamespaceRef( declaringNamespaceOrType.As<INamespace>() ) is { } mergedTypeNamespace )
                    {
                        this.GetNamedTypeCollectionByParent( mergedTypeNamespace.As<INamespaceOrNamedType>(), true ).Add( namedType.ToRef() );
                    }

                    var topLevelTypes = this.GetTopLevelNamedTypeCollection( true );
                    topLevelTypes.Add( namedType.ToRef() );
                }

                break;

#if ROSLYN_5_0_0_OR_GREATER
            case ExtensionBlockBuilderData extensionBlock:
                var extensionBlocks = this.GetExtensionBlockCollection( extensionBlock.DeclaringType.AssertNotNull(), true );
                extensionBlocks.Add( extensionBlock.ToRef() );

                break;
#endif

            case NamespaceBuilderData ns:
                // Anomaly with namespaces:
                // Aspects on different types of the same depth can independently introduce identical namespaces.
                // This must be resolved here.
                // It means we will have several instances of NamespaceBuilder pointing to the same entity.

                if ( !this._namespaceBuilders.ContainsKey( ns.FullName ) )
                {
                    var declaringNamespace = ns.ContainingDeclaration.AssertNotNull().As<INamespace>();

                    this.GetNamespaceCollection( declaringNamespace, true ).Add( ns.ToRef() );

                    if ( this.GetMergedNamespaceRef( declaringNamespace ) is { } mergedParentNamespace )
                    {
                        var mergedNamespaces = this.GetNamespaceCollection( mergedParentNamespace, true );

                        // The merged declaration can already have a child namespace of that name, declared by a
                        // referenced assembly and not by this compilation. Adding the introduced namespace would
                        // replace it in the collection, so the introduced namespace is added only when the name is
                        // free, and it remains reachable from the tree of IAssembly.GlobalNamespace only.
                        if ( mergedNamespaces.OfName( ns.Name ).IsDefaultOrEmpty )
                        {
                            mergedNamespaces.Add( ns.ToRef() );
                        }
                    }

                    this._namespaceBuilders = this._namespaceBuilders.Add( ns.FullName, ns );
                }

                break;

            default:
                throw new AssertionFailedException( $"Unexpected declaration type: {declaration.GetType()}." );
        }
    }

    private void AddIntroduceInterfaceTransformation( IIntroduceInterfaceTransformation transformation )
    {
        var introduceInterface = (IntroduceInterfaceTransformation) transformation;

        var targetType = introduceInterface.TargetType;

        var interfaces = this.GetInterfaceImplementationCollection( targetType, true );

        interfaces.Add( introduceInterface );

        foreach ( var type in this.GetDerivedTypes( targetType ).Concat( targetType ) )
        {
            var allInterfaces = this.GetAllInterfaceImplementationCollection( type, true );

            allInterfaces.Add( introduceInterface );
        }
    }
}