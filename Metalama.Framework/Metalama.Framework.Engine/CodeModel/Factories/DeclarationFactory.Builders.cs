// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Types;
using Metalama.Framework.Engine.CodeModel.Abstractions;
using Metalama.Framework.Engine.CodeModel.GenericContexts;
using Metalama.Framework.Engine.CodeModel.Introductions.BuilderData;
using Metalama.Framework.Engine.CodeModel.Introductions.ConstructedTypes;
using Metalama.Framework.Engine.CodeModel.Introductions.Introduced;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.Utilities;
using Microsoft.CodeAnalysis;
using System;
using System.Linq;
using SpecialType = Microsoft.CodeAnalysis.SpecialType;

namespace Metalama.Framework.Engine.CodeModel.Factories;

public partial class DeclarationFactory
{
    private readonly Cache<DeclarationBuilderData, IDeclaration> _builderCache;

    private readonly record struct CreateFromBuilderArgs<TBuilderData>(
        TBuilderData Builder,
        GenericContext GenericContext,
        DeclarationFactory Factory,
        bool IsNullable )
    {
        public CompilationModel Compilation => this.Factory._compilationModel;
    }

    private delegate TDeclaration CreateFromBuilderDelegate<out TDeclaration, TBuilder>( in CreateFromBuilderArgs<TBuilder> args );

    private TDeclaration GetDeclarationFromBuilder<TDeclaration, TBuilderData>(
        TBuilderData builder,
        IGenericContext? genericContext,
        CreateFromBuilderDelegate<TDeclaration, TBuilderData> createBuiltDeclaration,
        bool isNullable = false,
        bool supportsRedirection = false )
        where TDeclaration : class, IDeclaration
        where TBuilderData : DeclarationBuilderData
    {
        using ( StackOverflowHelper.Detect() )
        {
            return (TDeclaration) this._builderCache.GetOrAdd(
                builder,
                genericContext.AsGenericContext(),
                typeof(TDeclaration),
                static ( _, gc, x ) =>
                {
                    if ( x.supportsRedirection && x.me._compilationModel.TryGetRedirectedDeclaration(
                            x.builder.ToFullRef(),
                            out var redirected ) )
                    {
                        // It's normal that redirections redirect to builders that have the same reference!
                        // Beware not to enable redirections for this call, as this would cause an infinite recursion.
                        return x.me.GetDeclarationFromBuilder( (TBuilderData) redirected, gc, x.createBuiltDeclaration );
                    }

                    return x.createBuiltDeclaration( new CreateFromBuilderArgs<TBuilderData>( x.builder, gc ?? GenericContext.Empty, x.me, x.isNullable ) );
                },
                (me: this, builder, createBuiltDeclaration, supportsRedirection, isNullable) );
        }
    }

    internal IAttribute GetAttribute( AttributeBuilderData attributeBuilder, IGenericContext? genericContext = null )
        => this.GetDeclarationFromBuilder<IAttribute, AttributeBuilderData>(
            attributeBuilder,
            genericContext,
            static ( in CreateFromBuilderArgs<AttributeBuilderData> args ) => new IntroducedAttribute( args.Builder, args.Compilation, args.GenericContext ) );

    private IParameter GetParameter(
        ParameterBuilderData parameterBuilder,
        IGenericContext? genericContext = null )
        => this.GetDeclarationFromBuilder<IParameter, ParameterBuilderData>(
            parameterBuilder,
            genericContext,
            static ( in CreateFromBuilderArgs<ParameterBuilderData> args ) => new IntroducedParameter(
                args.Builder,
                args.Compilation,
                args.GenericContext,
                (IHasParameters) args.Builder.ContainingDeclaration.GetTarget( args.Compilation ) ) );

    internal ITypeParameter GetTypeParameter(
        TypeParameterBuilderData typeParameterBuilder,
        IGenericContext? genericContext = null,
        bool isNullable = false )
        => this.GetDeclarationFromBuilder<ITypeParameter, TypeParameterBuilderData>(
            typeParameterBuilder,
            genericContext,
            static ( in CreateFromBuilderArgs<TypeParameterBuilderData> args )
                => new IntroducedTypeParameter( args.Builder, args.Compilation, args.GenericContext, args.IsNullable ),
            isNullable: isNullable );

    internal IMethod GetMethod(
        MethodBuilderData methodBuilder,
        IGenericContext? genericContext = null )
        => this.GetDeclarationFromBuilder<IMethod, MethodBuilderData>(
            methodBuilder,
            genericContext,
            static ( in CreateFromBuilderArgs<MethodBuilderData> args ) => new IntroducedMethod( args.Builder, args.Compilation, args.GenericContext ) );

    internal IMethod GetAccessor( MethodBuilderData methodBuilder, IGenericContext? genericContext = null )
        => this.GetDeclarationFromBuilder(
            methodBuilder,
            genericContext,
            static ( in CreateFromBuilderArgs<MethodBuilderData> args ) =>
                ((IHasAccessors) args.Builder.ContainingDeclaration.GetTarget( args.Compilation, args.GenericContext ))
                .GetAccessor( args.Builder.MethodKind )
                .AssertNotNull() );

    internal IConstructor GetConstructor(
        ConstructorBuilderData constructorBuilder,
        IGenericContext? genericContext = null )
        => this.GetDeclarationFromBuilder<IConstructor, ConstructorBuilderData>(
            constructorBuilder,
            genericContext,
            static ( in CreateFromBuilderArgs<ConstructorBuilderData> args )
                => new IntroducedConstructor( args.Builder, args.Compilation, args.GenericContext ),
            true );

    // Fields support redirections, but fields redirect to properties, so it is not handled at this level.
    internal IField GetField(
        FieldBuilderData fieldBuilder,
        IGenericContext? genericContext = null )
        => this.GetDeclarationFromBuilder<IField, FieldBuilderData>(
            fieldBuilder,
            genericContext,
            static ( in CreateFromBuilderArgs<FieldBuilderData> args ) => new IntroducedField( args.Builder, args.Compilation, args.GenericContext ) );

    internal IProperty GetProperty(
        PropertyBuilderData propertyBuilder,
        IGenericContext? genericContext = null )
        => this.GetDeclarationFromBuilder<IProperty, PropertyBuilderData>(
            propertyBuilder,
            genericContext,
            static ( in CreateFromBuilderArgs<PropertyBuilderData> args ) => new IntroducedProperty( args.Builder, args.Compilation, args.GenericContext ) );

    internal IIndexer GetIndexer(
        IndexerBuilderData indexerBuilder,
        IGenericContext? genericContext = null )
        => this.GetDeclarationFromBuilder<IIndexer, IndexerBuilderData>(
            indexerBuilder,
            genericContext,
            static ( in CreateFromBuilderArgs<IndexerBuilderData> args ) => new IntroducedIndexer( args.Builder, args.Compilation, args.GenericContext ) );

    internal IEvent GetEvent(
        EventBuilderData eventBuilder,
        IGenericContext? genericContext = null )
        => this.GetDeclarationFromBuilder<IEvent, EventBuilderData>(
            eventBuilder,
            genericContext,
            static ( in CreateFromBuilderArgs<EventBuilderData> args ) => new IntroducedEvent( args.Builder, args.Compilation, args.GenericContext ) );

    internal INamedType GetNamedType(
        NamedTypeBuilderData namedTypeBuilder,
        IGenericContext? genericContext = null,
        bool isNullable = false )
        => this.GetDeclarationFromBuilder<INamedType, NamedTypeBuilderData>(
            namedTypeBuilder,
            genericContext,
            static ( in CreateFromBuilderArgs<NamedTypeBuilderData> args ) => new IntroducedNamedType(
                args.Builder,
                args.Compilation,
                args.GenericContext,
                args.IsNullable ),
            isNullable: isNullable );

    internal INamespace GetNamespace(
        NamespaceBuilderData namespaceBuilder,
        IGenericContext? genericContext = null )
        => this.GetDeclarationFromBuilder<INamespace, NamespaceBuilderData>(
            namespaceBuilder,
            genericContext,
            static ( in CreateFromBuilderArgs<NamespaceBuilderData> args ) => new IntroducedNamespace( args.Builder, args.Compilation ) );

    internal IDeclaration GetDeclaration(
        DeclarationBuilderData builder,
        IGenericContext? genericContext = null,
        Type? interfaceType = null )
    {
        // Note that interfaceType may be a non-final interface, e.g. IFieldOrProperty.
        Invariant.Assert( interfaceType == null || builder.DeclarationKind.GetPossibleDeclarationInterfaceTypes().Any( interfaceType.IsAssignableFrom ) );

        return builder switch
        {
            // TODO PERF: switch based on DeclarationKind or use an array of delegates.

            MethodBuilderData { ContainingDeclaration.DeclarationKind: DeclarationKind.NamedType } methodBuilder => this.GetMethod(
                methodBuilder,
                genericContext ),
            MethodBuilderData
            {
                ContainingDeclaration.DeclarationKind: DeclarationKind.Property or DeclarationKind.Event or DeclarationKind.Field or DeclarationKind.Indexer
            } accessorBuilder => this.GetAccessor( accessorBuilder, genericContext ),
            FieldBuilderData fieldBuilder when interfaceType == null || interfaceType != typeof(IProperty) => this.GetField( fieldBuilder, genericContext ),
            FieldBuilderData fieldBuilder when interfaceType == typeof(IProperty) => fieldBuilder.OverridingProperty.AssertNotNull()
                .GetTarget( this._compilationModel, genericContext ),
            PropertyBuilderData propertyBuilder when interfaceType == null || interfaceType != typeof(IField) =>
                this.GetProperty( propertyBuilder, genericContext ),
            PropertyBuilderData { OriginalField: { } originalField } when interfaceType == typeof(IField) => (IDeclaration) originalField.GetTarget(
                this._compilationModel,
                genericContext,
                interfaceType ),
            IndexerBuilderData indexerBuilder => this.GetIndexer( indexerBuilder, genericContext ),
            EventBuilderData eventBuilder => this.GetEvent( eventBuilder, genericContext ),
            ParameterBuilderData parameterBuilder => this.GetParameter( parameterBuilder, genericContext ),
            AttributeBuilderData attributeBuilder => this.GetAttribute( attributeBuilder, genericContext ),
            TypeParameterBuilderData genericParameterBuilder => this.GetTypeParameter( genericParameterBuilder, genericContext ),
            ConstructorBuilderData constructorBuilder => this.GetConstructor( constructorBuilder, genericContext ),
            NamedTypeBuilderData namedTypeBuilder => this.GetNamedType( namedTypeBuilder, genericContext ),
            NamespaceBuilderData namespaceBuilder => this.GetNamespace( namespaceBuilder, genericContext ),

            // This is for linker tests (fake builders), which resolve to themselves.
            // ReSharper disable once SuspiciousTypeConversion.Global
            ISdkRef reference => (IDeclaration) reference.GetTarget( this._compilationModel ).AssertNotNull(),
            _ => throw new AssertionFailedException( $"Cannot get a declaration for a {builder.GetType()}" )
        };
    }

    internal IArrayType MakeArrayType( IType elementType, int rank )
        => elementType switch
        {
            ISymbolBasedCompilationElement symbolBased => this.MakeArrayType( (ITypeSymbol) symbolBased.Symbol, rank ),
            _ => new ConstructedArrayType( this._compilationModel, elementType.ToFullRef(), rank )
        };

    internal IPointerType MakePointerType( IType pointedType )
        => pointedType switch
        {
            ISymbolBasedCompilationElement symbolBased => this.MakePointerType( (ITypeSymbol) symbolBased.Symbol ),
            _ => new ConstructedPointerType( this._compilationModel, pointedType.ToFullRef() )
        };

    internal INamedType CreateNullableValueType( IType type )
    {
        var nullableType = this.GetNamedType( this._compilationModel.RoslynCompilation.GetSpecialType( SpecialType.System_Nullable_T ) );

        return nullableType.MakeGenericInstance( type );
    }
}