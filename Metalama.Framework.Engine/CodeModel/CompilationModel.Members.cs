// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.AdviceImpl.Attributes;
using Metalama.Framework.Engine.AdviceImpl.InterfaceImplementation;
using Metalama.Framework.Engine.AdviceImpl.Introduction;
using Metalama.Framework.Engine.CodeModel.Introductions.BuilderData;
using Metalama.Framework.Engine.CodeModel.Introductions.Builders;
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

    internal ImmutableDictionaryOfArray<IRef<IDeclaration>, AnnotationInstance> Annotations { get; private set; }

    private bool IsMutable { get; }

    internal bool Contains( FieldBuilderData fieldBuilder )
        => (this._fields.TryGetValue( fieldBuilder.DeclaringType, out var fields )
            && fields.Contains( fieldBuilder.ToRef() ))
           || this.IsRedirected( fieldBuilder.ToRef() );

    internal bool Contains( MethodBuilderData methodBuilder )
        => methodBuilder switch
        {
            { MethodKind: MethodKind.Finalizer } =>
                this._finalizers.TryGetValue( methodBuilder.DeclaringType, out var finalizer )
                && finalizer == methodBuilder,
            _ =>
                this._methods.TryGetValue( methodBuilder.DeclaringType, out var methods )
                && methods.Contains( methodBuilder.ToRef() )
        };

    internal bool Contains( ConstructorBuilderData constructorBuilder )
        => (this._constructors.TryGetValue( constructorBuilder.DeclaringType, out var constructors )
            && constructors.Contains( constructorBuilder.ToRef() ))
           || (this._staticConstructors.TryGetValue( constructorBuilder.DeclaringType, out var staticConstructors )
               && staticConstructors == constructorBuilder);

    internal bool Contains( EventBuilderData eventBuilder )
        => this._events.TryGetValue( eventBuilder.DeclaringType, out var events )
           && events.Contains( eventBuilder.ToRef() );

    internal bool Contains( PropertyBuilderData propertyBuilder )
        => this._properties.TryGetValue( propertyBuilder.DeclaringType, out var properties )
           && properties.Contains( propertyBuilder.ToRef() );

    internal bool Contains( IndexerBuilderData indexerBuilder )
        => this._indexers.TryGetValue( indexerBuilder.DeclaringType, out var indexers )
           && indexers.Contains( indexerBuilder.ToRef() );

    internal bool Contains( ParameterBuilderData parameterBuilder )
        => parameterBuilder.ContainingDeclaration switch
        {
            null => false,
            _ => this._parameters.TryGetValue( parameterBuilder.ContainingDeclaration.As<IHasParameters>(), out var parameters )
                 && parameters.Contains( parameterBuilder.ToRef() )
        };

    internal bool Contains( NamedTypeBuilder namedTypeBuilder )
    {
        var namespaceOrNamedType = (INamespaceOrNamedType?) namedTypeBuilder.DeclaringType
                                   ?? namedTypeBuilder.ContainingNamespace ?? throw new AssertionFailedException();

        return this._namedTypesByParent.TryGetValue(
                   namespaceOrNamedType.ToFullRef(),
                   out var namedTypes )
               && namedTypes.Contains( namedTypeBuilder.ToRef() );
    }

    internal bool Contains( NamespaceBuilder namespaceBuilder )
    {
        // Anomaly with namespaces: many instances of the NamespaceBuilder class can represent the same entity, so we rely on the full name.
        return this._namespaceBuilders.ContainsKey( namespaceBuilder.FullName );
    }

    private TCollection GetMemberCollection<TOwner, TCollection>(
        ref ImmutableDictionary<IFullRef<TOwner>, TCollection> dictionary,
        bool requestMutableCollection,
        IFullRef<TOwner> declaration,
        Func<CompilationModel, IFullRef<TOwner>, TCollection> createCollection )
        where TOwner : class, IDeclaration
        where TCollection : IUpdatableCollection
    {
        Invariant.Assert( !(requestMutableCollection && !this.IsMutable) );
        Invariant.Assert( declaration.IsDefinition );

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

    internal TypeUpdatableCollection GetTopLevelNamedTypeCollection( bool mutable = false )
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

        // Replaced declaration should be always removed before adding the replacement.
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

        if ( transformation is IntroduceParameterTransformation appendParameterTransformation )
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

    private void AddDeclaration( DeclarationBuilderData declaration )
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
                var types = this.GetNamedTypeCollectionByParent(
                    namedType.ContainingDeclaration.AssertNotNull().As<INamespaceOrNamedType>(),
                    true );

                types.Add( namedType.ToRef() );

                if ( namedType.DeclaringType == null )
                {
                    var topLevelTypes = this.GetTopLevelNamedTypeCollection( true );
                    topLevelTypes.Add( namedType.ToRef() );
                }

                break;

            case NamespaceBuilderData ns:
                // Anomaly with namespaces:
                // Aspects on different types of the same depth can independently introduce identical namespaces.
                // This must be resolved here.
                // It means we will have several instances of NamespaceBuilder pointing to the same entity.

                if ( !this._namespaceBuilders.ContainsKey( ns.FullName ) )
                {
                    var namespaces = this.GetNamespaceCollection(
                        ns.ContainingDeclaration.AssertNotNull().As<INamespace>(),
                        true );

                    namespaces.Add( ns.ToRef() );
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