// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Compiler;
using Metalama.Framework.Code;
using Metalama.Framework.Code.Collections;
using Metalama.Framework.Code.Comparers;
using Metalama.Framework.Engine.AdviceImpl.Attributes;
using Metalama.Framework.Engine.AdviceImpl.Introduction;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel.Abstractions;
using Metalama.Framework.Engine.CodeModel.Collections;
using Metalama.Framework.Engine.CodeModel.Factories;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.CodeModel.Introductions.BuilderData;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.CodeModel.Source;
using Metalama.Framework.Engine.CodeModel.UpdatableCollections;
using Metalama.Framework.Engine.Collections;
using Metalama.Framework.Engine.HierarchicalOptions;
using Metalama.Framework.Engine.Metrics;
using Metalama.Framework.Engine.SerializableIds;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.SyntaxGeneration;
using Metalama.Framework.Engine.Transformations;
using Metalama.Framework.Engine.Utilities;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Metalama.Framework.Options;
using Metalama.Framework.Project;
using Metalama.Framework.Services;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using SyntaxReference = Microsoft.CodeAnalysis.SyntaxReference;

namespace Metalama.Framework.Engine.CodeModel
{
    public sealed partial class CompilationModel : SymbolBasedDeclaration, ICompilationInternal, ISdkCompilation
    {
        private static int _nextId;
        private readonly int _id = Interlocked.Increment( ref _nextId );

        internal new RefFactory RefFactory { get; }

        static CompilationModel()
        {
            MetalamaEngineModuleInitializer.EnsureInitialized();
        }

        public static CompilationModel CreateInitialInstance(
            ProjectModel project,
            PartialCompilation compilation,
            AspectRepository? aspectRepository = null,
            HierarchicalOptionsManager? hierarchicalOptionsManager = null,
            ImmutableDictionaryOfArray<IRef<IDeclaration>, AnnotationInstance>? annotations = null,
            IExternalAnnotationProvider? externalAnnotationProvider = null,
            string? debugLabel = null )
            => new(
                project,
                compilation,
                aspectRepository,
                hierarchicalOptionsManager,
                annotations,
                externalAnnotationProvider,
                CompilationModelOptions.Default,
                debugLabel );

        public static CompilationModel CreateInitialInstance(
            ProjectModel project,
            Compilation compilation,
            ImmutableArray<ManagedResource> resources = default,
            AspectRepository? aspectRepository = null,
            HierarchicalOptionsManager? hierarchicalOptionsManager = null,
            ImmutableDictionaryOfArray<IRef<IDeclaration>, AnnotationInstance>? annotations = null,
            IExternalAnnotationProvider? externalAnnotationProvider = null,
            string? debugLabel = null )
            => new(
                project,
                PartialCompilation.CreateComplete( compilation, resources ),
                aspectRepository,
                hierarchicalOptionsManager,
                annotations,
                externalAnnotationProvider,
                CompilationModelOptions.Default,
                debugLabel );

        internal static CompilationModel CreateInitialInstance(
            ProjectModel project,
            Compilation compilation,
            CompilationModelOptions options,
            string? debugLabel )
            => new( project, PartialCompilation.CreateComplete( compilation ), null, null, null, null, options, debugLabel );

        // This collection index all attributes on types and members, but not attributes on the assembly and the module.
        private readonly ImmutableDictionaryOfArray<IRef<INamedType>, AttributeRef> _allMemberAttributesByType;

        public AspectRepository AspectRepository { get; }

        public HierarchicalOptionsManager? HierarchicalOptionsManager { get; }

        internal IExternalAnnotationProvider? ExternalAnnotationProvider { get; }

        public IEnumerable<T> GetAnnotations<T>( IDeclaration declaration )
            where T : class, IAnnotation
        {
            if ( declaration.BelongsToCurrentProject )
            {
                return this.Annotations[declaration.ToRef()].Select( i => i.Annotation as T ).WhereNotNull();
            }
            else if ( this.ExternalAnnotationProvider != null )
            {
                return this.ExternalAnnotationProvider.GetAnnotations( declaration ).OfType<T>();
            }
            else
            {
                return [];
            }
        }

        internal ImmutableDictionaryOfArray<SerializableDeclarationId, IAnnotation> GetExportedAnnotations()
        {
            var builder = new ImmutableDictionaryOfArray<SerializableDeclarationId, IAnnotation>.Builder();

            foreach ( var (declarationId, annotation) in this.Annotations
                         .SelectMany(
                             group => group
                                 .Where( i => i.Export )
                                 .Select( i => (DeclarationId: group.Key.ToSerializableId(), i.Annotation) ) ) )
            {
                builder.Add( declarationId, annotation );
            }

            return builder.ToImmutable();
        }

        IHierarchicalOptionsManager ICompilationInternal.HierarchicalOptionsManager
            => this.HierarchicalOptionsManager ?? NullHierarchicalOptionsManager.Instance;

        private readonly Lazy<DerivedTypeIndex> _derivedTypes;

        private ImmutableDictionary<IRef, DeclarationBuilderData> _redirections =
            ImmutableDictionary.Create<IRef, DeclarationBuilderData>( RefEqualityComparer.Default );

        private ImmutableDictionary<IRef<IDeclaration>, int> _depthsCache =
            ImmutableDictionary.Create<IRef<IDeclaration>, int>( RefEqualityComparer<IDeclaration>.Default );

        SemanticModel ISdkCompilation.GetCachedSemanticModel( SyntaxTree syntaxTree ) => this.RoslynCompilation.GetCachedSemanticModel( syntaxTree );

        public DeclarationFactory Factory { get; }

        ISdkDeclarationFactory ISdkCompilation.Factory => this.Factory;

        IAspectRepository ICompilationInternal.AspectRepository => this.AspectRepository;

        internal ProjectModel Project { get; }

        IProject ICompilation.Project => this.Project;

        public CompilationContext CompilationContext { get; }

        public PartialCompilation PartialCompilation { get; }

        internal MetricManager MetricManager { get; }

        internal CompilationModelOptions Options { get; }

        internal SerializableTypeIdResolverForIType SerializableTypeIdResolver { get; }

        private readonly string? _debugLabel;

        private CompilationModel(
            ProjectModel project,
            PartialCompilation partialCompilation,
            AspectRepository? aspectRepository,
            HierarchicalOptionsManager? hierarchicalOptionsManager,
            ImmutableDictionaryOfArray<IRef<IDeclaration>, AnnotationInstance>? annotations,
            IExternalAnnotationProvider? externalAnnotationProvider,
            CompilationModelOptions? options,
            string? debugLabel ) : base( null )
        {
            this.PartialCompilation = partialCompilation;
            this.Project = project;
            this._debugLabel = debugLabel;
            this.ExternalAnnotationProvider = externalAnnotationProvider;
            this.CompilationContext = partialCompilation.Compilation.GetCompilationContext();
            this.RefFactory = new RefFactory( this );

            this._staticConstructors =
                ImmutableDictionary<IFullRef<INamedType>, ConstructorBuilderData>.Empty.WithComparers( RefEqualityComparer<INamedType>.Default );

            this._finalizers = ImmutableDictionary<IFullRef<INamedType>, MethodBuilderData>.Empty.WithComparers( RefEqualityComparer<INamedType>.Default );

            this.Annotations = annotations
                               ?? ImmutableDictionaryOfArray<IRef<IDeclaration>, AnnotationInstance>.Empty.WithKeyComparer(
                                   RefEqualityComparer<IDeclaration>.Default );

            this.AspectRepository = aspectRepository ?? new IncrementalAspectRepository( this );
            this.HierarchicalOptionsManager = hierarchicalOptionsManager;

            // If the MetricManager is not provided, we create an instance. This allows to test metrics independently from the pipeline.
            this.MetricManager = project.ServiceProvider.GetService<MetricManager>()
                                 ?? new MetricManager( (ServiceProvider<IProjectService>) project.ServiceProvider );

            this.Helpers = new CompilationHelpers( project.ServiceProvider, this.CompilationContext );
            this.Options = options ?? CompilationModelOptions.Default;

            // Initialize dictionaries of modified members.
            static void InitializeDictionary<T>( out ImmutableDictionary<IFullRef<INamedType>, T> dictionary )
            {
                dictionary = ImmutableDictionary.Create<IFullRef<INamedType>, T>( RefEqualityComparer<INamedType>.Default );
            }

            InitializeDictionary( out this._fields );
            InitializeDictionary( out this._methods );
            InitializeDictionary( out this._constructors );
            InitializeDictionary( out this._events );
            InitializeDictionary( out this._properties );
            InitializeDictionary( out this._indexers );
            InitializeDictionary( out this._allInterfaceImplementations );
            InitializeDictionary( out this._interfaceImplementations );

            this._namedTypesByParent =
                ImmutableDictionary.Create<IFullRef<INamespaceOrNamedType>, TypeUpdatableCollection>( RefEqualityComparer<INamespaceOrNamedType>.Default );

            this._namespaces = ImmutableDictionary.Create<IFullRef<INamespace>, NamespaceUpdatableCollection>( RefEqualityComparer<INamespace>.Default );

            this._namespaceBuilders = ImmutableDictionary<string, NamespaceBuilderData>.Empty.WithComparers( StringComparer.Ordinal );

            this._parameters =
                ImmutableDictionary.Create<IFullRef<IHasParameters>, ParameterUpdatableCollection>( RefEqualityComparer<IHasParameters>.Default );

            this._attributes =
                ImmutableDictionary.Create<IFullRef<IDeclaration>, AttributeUpdatableCollection>( RefEqualityComparer<IDeclaration>.Default );

            this.Factory = new DeclarationFactory( this );

            this.SerializableTypeIdResolver = new SerializableTypeIdResolverForIType( this );

            // Discover custom attributes.
            AttributeDiscoveryVisitor attributeDiscoveryVisitor = new( this );

            foreach ( var tree in partialCompilation.SyntaxTrees )
            {
                attributeDiscoveryVisitor.Visit( tree.Value );
            }

            this._allMemberAttributesByType = attributeDiscoveryVisitor.GetDiscoveredAttributes();

            this._derivedTypes = new Lazy<DerivedTypeIndex>(
                () => partialCompilation.LazyDerivedTypes.Value
                    .WithAdditionalAnalyzedTypes(
                        this._allMemberAttributesByType.Keys.Select( k => (INamedTypeSymbol?) k.GetSymbol( this.RoslynCompilation ) ).WhereNotNull() ) );
        }

        // The following dictionaries contain the members of types, if they have been modified. If they have not been modified,
        // the collection should be created from symbols.

        /// <summary>
        /// Initializes a new instance of the <see cref="CompilationModel"/> class that is based on a prototype instance but appends transformations.
        /// </summary>
        private CompilationModel(
            CompilationModel prototype,
            IReadOnlyCollection<ITransformation>? observableTransformations,
            IEnumerable<AspectInstance>? aspectInstances,
            string? debugLabel ) : this(
            prototype,
            true,
            debugLabel )
        {
            if ( observableTransformations != null )
            {
                foreach ( var transformation in observableTransformations )
                {
                    this.AddTransformation( transformation );
                }

                this.IsMutable = false;

                var allNewDeclarations =
                    observableTransformations
                        .OfType<IIntroduceDeclarationTransformation>()
                        .SelectMany( t => t.DeclarationBuilderData.GetOwnedDeclarations() );

                var allAttributes =
                    allNewDeclarations.SelectMany( c => c.Attributes )
                        .Concat( observableTransformations.OfType<IntroduceAttributeTransformation>().Select( x => x.BuilderData ) )
                        .Select( a => new IntroducedAttributeRef( a ) )
                        .ToReadOnlyList();

                // TODO: this cache may need to be smartly invalidated when we have interface introductions.
                this._allMemberAttributesByType = prototype._allMemberAttributesByType.AddRange( allAttributes, a => a.AttributeType );

                var attributeTypes = this._allMemberAttributesByType.Keys.Select( x => x.GetTarget( this ) );

                this._derivedTypes = new Lazy<DerivedTypeIndex>(
                    () => prototype._derivedTypes.Value
                        .WithIntroducedInterfaces( observableTransformations.OfType<IIntroduceInterfaceTransformation>() )
                        .WithIntroducedTypes( observableTransformations.OfType<IntroduceNamedTypeTransformation>() )
                        .WithAdditionalAnalyzedTypes( attributeTypes ) );
            }

            if ( aspectInstances != null )
            {
                this.AspectRepository = this.AspectRepository.WithAspectInstances( aspectInstances, this );
            }
        }

        private CompilationModel( CompilationModel prototype, bool mutable, string? debugLabel, CompilationModelOptions? options = null ) : base( null )
        {
            this.IsMutable = mutable;
            this.RefFactory = prototype.RefFactory; // Intentionally sharing the RefFactory.
            this.Project = prototype.Project;
            this.Helpers = prototype.Helpers;
            this.Options = options ?? prototype.Options;
            this._debugLabel = debugLabel;
            this.ExternalAnnotationProvider = prototype.ExternalAnnotationProvider;

            this._derivedTypes = prototype._derivedTypes;
            this.PartialCompilation = prototype.PartialCompilation;
            this.CompilationContext = prototype.CompilationContext;
            this._methods = prototype._methods;
            this._constructors = prototype._constructors;
            this._fields = prototype._fields;
            this._properties = prototype._properties;
            this._indexers = prototype._indexers;
            this._events = prototype._events;
            this._interfaceImplementations = prototype._interfaceImplementations;
            this._allInterfaceImplementations = prototype._allInterfaceImplementations;
            this._staticConstructors = prototype._staticConstructors;
            this._finalizers = prototype._finalizers;
            this.Annotations = prototype.Annotations;
            this._parameters = prototype._parameters;
            this._attributes = prototype._attributes;
            this._namedTypesByParent = prototype._namedTypesByParent;
            this._namespaces = prototype._namespaces;
            this._namespaceBuilders = prototype._namespaceBuilders;

            this.Factory = new DeclarationFactory( this );
            this.SerializableTypeIdResolver = new SerializableTypeIdResolverForIType( this );
            this._depthsCache = prototype._depthsCache;
            this._redirections = prototype._redirections;
            this._allMemberAttributesByType = prototype._allMemberAttributesByType;
            this.AspectRepository = prototype.AspectRepository;
            this.HierarchicalOptionsManager = prototype.HierarchicalOptionsManager;
            this.MetricManager = prototype.MetricManager;
            this.SerializableTypeIdResolver = prototype.SerializableTypeIdResolver;
        }

        private CompilationModel( CompilationModel prototype, AspectRepository aspectRepository, string? debugLabel ) : this( prototype, false, debugLabel )
        {
            this.AspectRepository = aspectRepository;
        }

        public SyntaxGenerationContext GetSyntaxGenerationContext( SyntaxGenerationOptions options, SyntaxNode node )
            => this.CompilationContext.GetSyntaxGenerationContext( options, node );

        internal CompilationModel WithTransformationsAndAspectInstances(
            IReadOnlyCollection<ITransformation>? transformations,
            IEnumerable<AspectInstance>? aspectInstances,
            string? debugLabel = null )
        {
            if ( transformations?.Count == 0 && aspectInstances == null )
            {
                return this;
            }

            return new CompilationModel( this, transformations, aspectInstances, debugLabel );
        }

        internal CompilationModel WithTransformations( IReadOnlyCollection<ITransformation>? transformations, string? debugLabel = null )
        {
            if ( transformations?.Count == 0 )
            {
                return this;
            }

            return new CompilationModel( this, transformations, null, debugLabel );
        }

        public CompilationModel WithAspectRepository( AspectRepository aspectRepository, string? debugLabel )
            => this.AspectRepository == aspectRepository ? this : new CompilationModel( this, aspectRepository, debugLabel );

        [Memo]
        public INamedTypeCollection Types
            => new NamedTypeCollection(
                this,
                this.GetTopLevelNamedTypeCollection() );

        [Memo]
        public INamedTypeCollection AllTypes
            => new NamedTypeCollection(
                this,
                this.GetTopLevelNamedTypeCollection(),
                true );

        [Memo]
        public override IAttributeCollection Attributes
            => new AttributeCollection(
                this,
                this.GetAttributeCollection( this.RefFactory.ForCompilation() ) );

        public override DeclarationKind DeclarationKind => DeclarationKind.Compilation;

        public override string ToDisplayString( CodeDisplayFormat? format = null, CodeDisplayContext? context = null )
            => this.RoslynCompilation.AssemblyName ?? "<Anonymous>";

#pragma warning disable CS0809
        [Obsolete( "This method call is redundant." )]
        public override CompilationModel Compilation => this;
#pragma warning restore CS0809

        public Compilation RoslynCompilation => this.PartialCompilation.Compilation;

        IDeclarationFactory ICompilation.Factory => this.Factory;

        public IReadOnlyList<IManagedResource> ManagedResources => throw new NotImplementedException();

        public ICompilationComparers Comparers => this.CompilationContext.Comparers;

        public INamespace GlobalNamespace => this.Factory.GetNamespace( this.RoslynCompilation.SourceModule.GlobalNamespace );

        public IEnumerable<INamedType> GetDerivedTypes( INamedType baseType, DerivedTypesOptions options = default )
        {
            OnUnsupportedDependency( $"{nameof(ICompilation)}.{nameof(this.GetDerivedTypes)}" );

            return this._derivedTypes.Value.GetDerivedTypesInCurrentCompilation( baseType, options );
        }

        internal IEnumerable<IFullRef<INamedType>> GetDerivedTypes( IFullRef<INamedType> baseType, DerivedTypesOptions options = default )
        {
            OnUnsupportedDependency( $"{nameof(ICompilation)}.{nameof(this.GetDerivedTypes)}" );

            return this._derivedTypes.Value.GetDerivedTypesInCurrentCompilation( baseType, options );
        }

        public IEnumerable<INamedType> GetDerivedTypes( Type baseType, DerivedTypesOptions options = default )
            => this.GetDerivedTypes( (INamedType) this.Factory.GetTypeByReflectionType( baseType ), options );

        // TODO: throw an exception when the caller tries to get aspects that have not been initialized yet.

        public override IDeclarationOrigin Origin => DeclarationOrigin.Source;

        public override IDeclaration? ContainingDeclaration => null;

        DeclarationKind ICompilationElement.DeclarationKind => DeclarationKind.Compilation;

        public override bool Equals( IDeclaration? other ) => ReferenceEquals( this, other );

        ICompilation ICompilationElement.Compilation => this;

        public IEnumerable<IAttribute> GetAllAttributesOfType( Type type, bool includeDerivedTypes = false )
            => this.GetAllAttributesOfType( (INamedType) this.Factory.GetTypeByReflectionType( type ), includeDerivedTypes );

        public IEnumerable<IAttribute> GetAllAttributesOfType( INamedType type, bool includeDerivedTypes = false )
        {
            if ( includeDerivedTypes )
            {
                var attributeTypes = this._derivedTypes.Value.GetDerivedTypes( type ).Append( type );

                return attributeTypes.SelectMany( GetAllAttributesOfExactType );
            }
            else
            {
                return GetAllAttributesOfExactType( type );
            }

            IEnumerable<IAttribute> GetAllAttributesOfExactType( INamedType t )
            {
                return this._allMemberAttributesByType[t.ToRef()]
                    .Select(
                        a =>
                        {
                            if ( !a.TryGetTarget( this, out var target ) )
                            {
                                // Skipped by WhereNotNull.
                                return null;
                            }

                            return target;
                        } )
                    .WhereNotNull();
            }
        }

        internal int GetDepth( IDeclaration declaration )
        {
            var reference = declaration.ToRef();

            if ( this._depthsCache.TryGetValue( reference, out var value ) )
            {
                return value;
            }

            switch ( declaration )
            {
                case INamedType namedType:
                    return this.GetDepth( namedType );

                case ICompilation:
                    return 0;

                case IAssembly:
                    // Order with Compilation matters. We want the root compilation to be ordered first.
                    return 1;

                case INamespace { DeclaringAssembly.IsExternal: true } ns:
                    throw new InvalidOperationException( $"Cannot compute the depth of '{ns.FullName}' because it is an external namespace." );

                case INamespace { IsGlobalNamespace: true }:
                    // We want the global namespace to be processed after all assembly references.
                    return 2;

                case INamespace ns:
                    {
                        // Then, we just count the number of components in the namespace, and we add the depth of the global namespace.
                        var depth = 3;

                        foreach ( var c in ns.FullName )
                        {
                            if ( c == '.' )
                            {
                                depth++;
                            }
                        }

                        return depth;
                    }

                default:
                    {
                        var depth = this.GetDepth( declaration.ContainingDeclaration! ) + 1;
                        this._depthsCache = this._depthsCache.SetItem( reference, depth );

                        return depth;
                    }
            }
        }

        internal int GetDepth( INamedType namedType )
        {
            if ( namedType.DeclaringAssembly.IsExternal )
            {
                return 0;
            }

            var reference = namedType.ToRef();

            if ( this._depthsCache.TryGetValue( reference, out var depth ) )
            {
                return depth;
            }

            depth = this.GetDepth( namedType.ContainingNamespace );

            if ( namedType.BaseType is { DeclaringAssembly.IsExternal: false } baseType )
            {
                depth = Math.Max( depth, this.GetDepth( baseType ) );
            }

            foreach ( var interfaceImplementation in namedType.ImplementedInterfaces )
            {
                if ( !interfaceImplementation.DeclaringAssembly.IsExternal )
                {
                    depth = Math.Max( depth, this.GetDepth( interfaceImplementation ) );
                }
            }

            depth++;

            this._depthsCache = this._depthsCache.SetItem( reference, depth );

            return depth;
        }

        internal bool IsRedirected( IRef reference )
        {
            return reference is IRef<IDeclaration> declarationRef && this._redirections.ContainsKey( declarationRef );
        }

        internal bool TryGetRedirectedDeclaration( IRef reference, [NotNullWhen( true )] out DeclarationBuilderData? redirected )
        {
            return this._redirections.TryGetValue( reference, out redirected );
        }

        [Memo]
        private IFullRef<ICompilation> Ref => this.RefFactory.ForCompilation();

        public IRef<ICompilation> ToRef() => this.Ref;

        IRef<IDeclaration> IDeclaration.ToRef() => this.Ref;

        IRef<IAssembly> IAssembly.ToRef() => this.Ref;

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences => ImmutableArray<SyntaxReference>.Empty;

        public override IEnumerable<IDeclaration> GetDerivedDeclarations( DerivedTypesOptions options = default ) => [];

        string IDisplayable.ToDisplayString( CodeDisplayFormat? format, CodeDisplayContext? context ) => this.RoslynCompilation.AssemblyName ?? "";

        [Memo]
        public override IAssembly DeclaringAssembly => this.Factory.GetAssembly( this.RoslynCompilation.Assembly );

        public override ISymbol Symbol => this.RoslynCompilation.Assembly;

        internal string? Name => this.RoslynCompilation.AssemblyName;

        public override string ToString()
        {
            if ( this._debugLabel == null )
            {
                return this.RoslynCompilation.AssemblyName ?? $"<anonymous> #{this._id}";
            }
            else
            {
                return $"{this.RoslynCompilation.AssemblyName} ({this._debugLabel}) #{this._id}";
            }
        }

        private CompilationHelpers Helpers { get; }

        ICompilationHelpers ICompilationInternal.Helpers => this.Helpers;

        bool IAssembly.IsExternal => false;

        [Memo]
        public IAssemblyIdentity Identity => new AssemblyIdentityModel( this.RoslynCompilation.Assembly.Identity );

        public override Location? DiagnosticLocation => null;

        public override bool CanBeInherited => false;

        public override SyntaxTree? PrimarySyntaxTree => null;

        public bool IsPartial => this.PartialCompilation.IsPartial;

        [Memo]
        internal DeclarationCache Cache => new( this );

        IDeclarationCache ICompilation.Cache => this.Cache;

        internal CompilationModel CreateMutableClone( string? debugLabel = null ) => new( this, true, debugLabel, this.Options );

        internal CompilationModel CreateImmutableClone( string? debugLabel = null ) => new( this, false, debugLabel, this.Options );

        public bool AreInternalsVisibleFrom( IAssembly assembly ) => this.RoslynCompilation.Assembly.AreInternalsVisibleToImpl( assembly.GetSymbol() );

        [Memo]
        public IAssemblyCollection ReferencedAssemblies => new ReferencedAssemblyCollection( this, this.RoslynCompilation.SourceModule );

        public override bool BelongsToCurrentProject => true;

        private protected override IFullRef<IDeclaration> ToFullDeclarationRef() => this.Ref;
    }
}