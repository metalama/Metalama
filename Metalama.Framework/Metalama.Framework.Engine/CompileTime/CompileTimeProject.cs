// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using K4os.Hash.xxHash;
using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Maintenance;
using Metalama.Backstage.Utilities;
using Metalama.Compiler;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code.Collections;
using Metalama.Framework.Engine.Collections;
using Metalama.Framework.Engine.CompileTime.Manifest;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Templating.Mapping;
using Metalama.Framework.Engine.Utilities;
using Metalama.Framework.Fabrics;
using Metalama.Framework.Options;
using Metalama.Framework.Services;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Metalama.Framework.Engine.CompileTime;

/// <summary>
/// Represents the compile-time project extracted from a run-time project, including its
/// <see cref="System.Reflection.Assembly"/> allowing for execution, and metadata.
/// </summary>
internal sealed class CompileTimeProject : IProjectService
{
    public static CompileTimeProject Empty { get; } = new();

    internal CompileTimeProjectManifest? Manifest { get; }

    internal string? CompiledAssemblyPath { get; }

    public AssemblyIdentity CompileTimeIdentity { get; }

    private readonly ITextMapFileProvider? _mapFileProvider;
    private readonly CacheableTemplateDiscoveryContextProvider? _cacheableTemplateDiscoveryContextProvider;

    private CompileTimeDomain Domain { get; }

    private DiagnosticManifest DiagnosticManifest { get; }

    private Assembly? _assembly;

    /// <summary>
    /// Gets the full path of the directory containing the transformed source code (typically under a temp directory). 
    /// This property is <c>null</c> is the current instance represents an empty project.
    /// </summary>
    public string? Directory { get; }

    /// <summary>
    /// Gets the identity of the run-time assembly for which this compile-time project was created.
    /// </summary>
    public AssemblyIdentity RunTimeIdentity { get; }

    public ITemplateReflectionContext? TemplateReflectionContext => this._cacheableTemplateDiscoveryContextProvider?.GetTemplateDiscoveryContext();

    /// <summary>
    /// Gets the list of aspect types (identified by their fully qualified reflection name) of the aspects
    /// declared in the current project.
    /// </summary>
    public IReadOnlyList<string> AspectTypes => this.Manifest?.AspectTypes ?? Array.Empty<string>();

    public IReadOnlyList<string> OtherTemplateTypes => this.Manifest?.OtherTemplateTypes ?? Array.Empty<string>();

    /// <summary>
    /// Gets the list of types that are exported using the <c>CompilerPlugin</c> attribute.
    /// </summary>
    public IReadOnlyList<string> PlugInTypes => this.Manifest?.PlugInTypes ?? Array.Empty<string>();

    /// <summary>
    /// Gets the list of types that implement the <see cref="Fabric"/> interface, but the <see cref="TransitiveProjectFabric"/>.
    /// </summary>
    public IReadOnlyList<string> FabricTypes => this.Manifest?.FabricTypes ?? Array.Empty<string>();

    /// <summary>
    /// Gets the list of types that implement the <see cref="TransitiveProjectFabric"/> interface.
    /// </summary>
    public IReadOnlyList<string> TransitiveFabricTypes => this.Manifest?.TransitiveFabricTypes ?? Array.Empty<string>();

    /// <summary>
    /// Gets the list of compile-time projects referenced by the current project.
    /// </summary>
    public IReadOnlyList<CompileTimeProject> References { get; }

    public IReadOnlyList<CompileTimeProject> ClosureProjects { get; }

    [Memo]
    private IReadOnlyDictionary<string, CompileTimeProject> ClosureProjectsByRunTimeAssemblyName => this.CreateClosureProjectsByRuntimeAssemblyName();

    [Memo]
    private IReadOnlyDictionary<string, CompileTimeProject> ClosureProjectsByCompileTimeAssemblyName
        => this.ClosureProjects.ToDictionary( p => p.CompileTimeIdentity.Name, p => p );

    private bool TryGetProjectByRunTimeAssemblyName( string runTimeName, [NotNullWhen( true )] out CompileTimeProject? project )
        => this.ClosureProjectsByRunTimeAssemblyName.TryGetValue( runTimeName, out project );

    public bool TryGetProjectByCompileTimeAssemblyName( string runTimeName, [NotNullWhen( true )] out CompileTimeProject? project )
        => this.ClosureProjectsByCompileTimeAssemblyName.TryGetValue( runTimeName, out project );

    /// <summary>
    /// Gets the list of transformed code files in the current project. 
    /// </summary>
    internal IReadOnlyList<CompileTimeFileManifest> CodeFiles => this.Manifest?.Files ?? Array.Empty<CompileTimeFileManifest>();

    [Memo]
    private ImmutableDictionaryOfArray<string, (CompileTimeFileManifest File, CompileTimeProject Project)> ClosureCodeFiles
        => this.ClosureProjects.SelectMany( p => p.CodeFiles.SelectAsReadOnlyList( f => (f, p) ) )
            .ToMultiValueDictionary( f => f.f.TransformedPath, f => f );

    [Memo]
    public IReadOnlyList<string> ClosureOptionTypes
        => this.ClosureProjects.SelectMany( p => p.Manifest?.OptionTypes ?? Enumerable.Empty<string>() )
            .ToReadOnlyList();
    
    /// <summary>
    /// Gets a <see cref="MetadataReference"/> corresponding to the current project.
    /// </summary>
    public MetadataReference ToMetadataReference() => MetadataReferenceCache.GetMetadataReference( this.AssertNotEmpty().CompiledAssemblyPath! );

    /// <summary>
    /// Gets the unique hash of the project, computed from the source code.
    /// </summary>
    public ulong Hash => this.Manifest?.SourceHash ?? 0;

    /// <summary>
    /// Gets a value indicating whether the current project is empty, i.e. does not contain any source code. Note that
    /// an empty project can STILL contain <see cref="References"/>.
    /// </summary>
    public bool IsEmpty => this.CompiledAssemblyPath == null && !this.IsFramework;

    public bool IsFramework => this.RunTimeIdentity.Name == "Metalama.Framework";

    /// <summary>
    /// Gets the CLR <see cref="System.Reflection.Assembly"/>, and loads it if necessary.
    /// </summary>
    private Assembly Assembly
    {
        get
        {
            this.LoadAssembly();

            return this._assembly!;
        }
    }

    private CompileTimeProject AssertNotEmpty()
    {
        if ( this.IsEmpty )
        {
            throw new InvalidOperationException();
        }

        return this;
    }

    private CompileTimeProject()
    {
        this.Manifest = new CompileTimeProjectManifest(
            "<empty>",
            "",
            [],
            [],
            [],
            [],
            [],
            [],
            [],
            null,
            0,
            [],
            [],
            false );

        this.Domain = null!;
        this.RunTimeIdentity = new AssemblyIdentity( "<empty>" );
        this.CompileTimeIdentity = new AssemblyIdentity( "<empty>" );
        this.References = [];
        this.ClosureProjects = [];
        this.DiagnosticManifest = DiagnosticManifest.Empty;
        this.ClosureDiagnosticManifest = DiagnosticManifest.Empty;
    }

    internal CompileTimeProject(
        ProjectServiceProvider serviceProvider,
        CompileTimeDomain domain,
        AssemblyIdentity runTimeIdentity,
        AssemblyIdentity compileTimeIdentity,
        IReadOnlyList<CompileTimeProject> references,
        CompileTimeProjectManifest? manifest,
        string? compiledAssemblyPath,
        ITextMapFileProvider? mapFileProvider,
        string? directory,
        CacheableTemplateDiscoveryContextProvider? cacheableTemplateDiscoveryContextProvider,
        Assembly? assembly = null,
        DiagnosticManifest? diagnosticManifest = null )
    {
        if ( compiledAssemblyPath != null && compileTimeIdentity.Name != Path.GetFileNameWithoutExtension( compiledAssemblyPath ) )
        {
            throw new AssertionFailedException(
                $"Assembly identify mismatch: the expected identity is '{compileTimeIdentity}', but the file path is '{compileTimeIdentity}'." );
        }

        this.Domain = domain;
        this.CompiledAssemblyPath = compiledAssemblyPath;
        this._mapFileProvider = mapFileProvider;
        this.Directory = directory;
        this._cacheableTemplateDiscoveryContextProvider = cacheableTemplateDiscoveryContextProvider;
        this.Manifest = manifest;
        this.RunTimeIdentity = runTimeIdentity;
        this.CompileTimeIdentity = compileTimeIdentity;
        this.References = references;

        this._assembly = assembly;
        this.ClosureProjects = this.SelectManyRecursiveDistinct( p => p.References ).ToImmutableArray();
        this.DiagnosticManifest = diagnosticManifest ?? this.GetDiagnosticManifest( serviceProvider );
        this.ClosureDiagnosticManifest = new DiagnosticManifest( this.ClosureProjects.SelectAsImmutableArray( p => p.DiagnosticManifest ) );

        // Check that the directory is valid.
        if ( manifest != null && directory != null )
        {
            var logger = serviceProvider.GetLoggerFactory().GetLogger( "CompileTimeProject" );

            foreach ( var file in manifest.Files )
            {
                var path = Path.Combine( directory, file.TransformedPath );

                if ( !File.Exists( path ) )
                {
                    logger.Error?.Log( $"The file '{path}' does not exist. This means that the directory is corrupt. Throwing an exception." );

                    throw new InvalidOperationException(
                        $"'The directory '{directory}' is in invalid state. Terminate all build processes, delete the directory and retry the build." );
                }
            }
        }
    }

    /// <summary>
    /// Creates a <see cref="CompileTimeProject"/> that includes source code.
    /// </summary>
    internal static CompileTimeProject Create(
        ProjectServiceProvider serviceProvider,
        CompileTimeDomain domain,
        AssemblyIdentity runTimeIdentity,
        AssemblyIdentity compileTimeIdentity,
        IReadOnlyList<CompileTimeProject> references,
        CompileTimeProjectManifest manifest,
        string? compiledAssemblyPath,
        string? sourceDirectory,
        ITextMapFileProvider? mapFileProvider,
        CacheableTemplateDiscoveryContextProvider? templateDiscoveryContextProvider )
        => new(
            serviceProvider,
            domain,
            runTimeIdentity,
            compileTimeIdentity,
            references,
            manifest,
            compiledAssemblyPath,
            mapFileProvider,
            sourceDirectory,
            templateDiscoveryContextProvider );

    /// <summary>
    /// Creates a <see cref="CompileTimeProject"/> that does not include any source code.
    /// </summary>
    public static CompileTimeProject CreateEmpty(
        ProjectServiceProvider serviceProvider,
        CompileTimeDomain domain,
        AssemblyIdentity runTimeIdentity,
        AssemblyIdentity compileTimeIdentity,
        IReadOnlyList<CompileTimeProject>? references = null )
        => new(
            serviceProvider,
            domain,
            runTimeIdentity,
            compileTimeIdentity,
            references ?? Array.Empty<CompileTimeProject>(),
            null,
            null,
            null,
            null,
            null );

    /// <summary>
    /// Creates a <see cref="CompileTimeProject"/> for an assembly that contains Metalama compile-time code but has not been transformed. This is the case
    /// normally for public APIs of SDK-based extensions. Returns <c>false</c> if the assembly is not loaded in the current AppDomain because it means
    /// it has not been loaded as an analyzer.
    /// </summary>
    public static bool TryCreateUntransformed(
        ProjectServiceProvider serviceProvider,
        CompileTimeDomain domain,
        AssemblyIdentity assemblyIdentity,
        string assemblyPath,
        CacheableTemplateDiscoveryContextProvider? templateDiscoveryContextProvider,
        [NotNullWhen( true )] out CompileTimeProject? compileTimeProject )
    {
        // Compute a unique hash based on the binary. 
        XXH64 hash = new();
        var buffer = new byte[1024];

        using ( var file = File.OpenRead( assemblyPath ) )
        {
            int read;

            while ( (read = file.Read( buffer, 0, 1024 )) > 0 )
            {
                hash.Update( buffer, 0, read );
            }
        }

        var projectHash = hash.Digest();

        var assemblyName = new AssemblyName( assemblyIdentity.ToString() );

        // Ignore collectible assemblies now, as they are not considered later when resolving.
        var assembly = AppDomainUtility
            .GetLoadedAssemblies( a => !AssemblyLoader.IsCollectible( a ) && AssemblyName.ReferenceMatchesDefinition( assemblyName, a.GetName() ) )
            .FirstOrDefault();

        if ( assembly == null )
        {
            var tempFileManager = serviceProvider.Underlying.GetRequiredBackstageService<ITempFileManager>();
            var outputPathHelper = new OutputPathHelper( tempFileManager );
            var outputDirectory = outputPathHelper.GetOutputPaths( assemblyIdentity.Name, null, projectHash ).Directory;

            var outputPath = Path.Combine( outputDirectory, Path.GetFileName( assemblyPath ) );

            RetryHelper.Retry(
                () =>
                {
                    if ( !File.Exists( outputPath ) )
                    {
                        using ( MutexHelper.WithGlobalLock( outputPath ) )
                        {
                            File.Copy( assemblyPath, outputPath );
                        }
                    }
                } );

            assembly = domain.LoadAssembly( outputPath );
        }

        // Find interesting types.
        var types = assembly.GetTypes();
        var aspectTypes = types.Where( t => typeof(IAspect).IsAssignableFrom( t ) ).Select( t => t.FullName ).ToImmutableArray();

        var fabricTypes = types
            .Where( t => typeof(ProjectFabric).IsAssignableFrom( t ) && !typeof(TransitiveProjectFabric).IsAssignableFrom( t ) )
            .Select( t => t.FullName.AssertNotNull() )
            .ToReadOnlyList();

        var transitiveFabricTypes = types
            .Where( t => typeof(TransitiveProjectFabric).IsAssignableFrom( t ) )
            .Select( t => t.FullName.AssertNotNull() )
            .ToReadOnlyList();

        var templateProviders = types
            .Where( t => typeof(ITemplateProvider).IsAssignableFrom( t ) )
            .Select( t => t.FullName.AssertNotNull() )
            .ToReadOnlyList();

        var optionTypes = types
            .Where( t => typeof(IHierarchicalOptions).IsAssignableFrom( t ) )
            .Select( t => t.FullName.AssertNotNull() )
            .ToReadOnlyList();

        // Since this method is used for SDK assemblies, we assume that Metalama SDK is used.
        const bool referencesMetalamaSdk = true;

        // Create a manifest.
        var manifest = new CompileTimeProjectManifest(
            assemblyIdentity.ToString(),
            "",
            aspectTypes,
            Array.Empty<string>(),
            fabricTypes,
            transitiveFabricTypes,
            templateProviders,
            optionTypes,
            null,
            TemplateProjectManifest.Empty,
            projectHash,
            Array.Empty<CompileTimeFileManifest>(),
            Array.Empty<CompileTimeDiagnosticManifest>(),
            referencesMetalamaSdk );

        compileTimeProject = new CompileTimeProject(
            serviceProvider,
            domain,
            assemblyIdentity,
            assemblyIdentity,
            Array.Empty<CompileTimeProject>(),
            manifest,
            assemblyPath,
            null,
            null,
            templateDiscoveryContextProvider,
            assembly );

        return true;
    }

    /// <summary>
    /// Serializes the current project (its manifest and source code) into a stream that can be embedded as a managed resource.
    /// </summary>
    private void Serialize( Stream stream )
    {
        this.AssertNotEmpty();

        using ( var archive = new ZipArchive( stream, ZipArchiveMode.Create, true, Encoding.UTF8 ) )
        {
            // Write syntax trees.

            foreach ( var sourceFile in this.CodeFiles )
            {
                var sourceText = File.ReadAllText( Path.Combine( this.Directory!, sourceFile.TransformedPath ) );

                var entry = archive.CreateEntry( sourceFile.TransformedPath, CompressionLevel.Optimal );
                using var entryWriter = new StreamWriter( entry.Open() );
                entryWriter.Write( sourceText );
            }

            // Write manifest.
            var manifestEntry = archive.CreateEntry( "manifest.json", CompressionLevel.Optimal );
            var manifestStream = manifestEntry.Open();
            this.Manifest!.Serialize( manifestStream );
        }
    }

    /// <summary>
    /// Returns a managed resource that contains the serialized project.
    /// </summary>
    /// <returns></returns>
    public ManagedResource ToResource()
    {
        this.AssertNotEmpty();

        var stream = new MemoryStream();
        this.Serialize( stream );
        var bytes = stream.ToArray();

        return new ManagedResource(
            CompileTimeConstants.CompileTimeProjectResourceName,
            bytes,
            true );
    }

    /// <summary>
    /// Gets a compile-time reflection <see cref="Type"/> defined in the current project.
    /// </summary>
    /// <param name="reflectionName"></param>
    /// <returns></returns>
    public Type? GetTypeOrNull( string reflectionName )
    {
        if ( this.IsEmpty )
        {
            return null;
        }
        else
        {
            var type = this.Assembly.GetType( reflectionName, false );

            if ( type == null )
            {
                return null;
            }

            // Check that the type is linked properly. An error here may be caused by a bug in 
            // a handler of the AppDomain.AssemblyResolve event.
            var iAspectInterface = type.GetInterfaces().FirstOrDefault( i => i.FullName == typeof(IAspect).FullName );

            if ( iAspectInterface != null && !typeof(IAspect).IsAssignableFrom( iAspectInterface ) )
            {
                // There must have been some assembly version mismatch.
                throw new AssertionFailedException( "Assembly version mismatch." );
            }

            return type;
        }
    }

    public bool TryGetType( string reflectionName, string runTimeAssemblyName, out Type? type )
    {
        if ( !this.TryGetProjectByRunTimeAssemblyName( runTimeAssemblyName, out var project ) )
        {
            type = null;

            return false;
        }

        type = project.GetTypeOrNull( reflectionName );

        return type != null;
    }

    private IReadOnlyDictionary<string, CompileTimeProject> CreateClosureProjectsByRuntimeAssemblyName()
    {
        var result = new Dictionary<string, CompileTimeProject>();

        foreach ( var project in this.ClosureProjects )
        {
            // When two versions of the same assembly are referenced, we get two references with the same assembly name.
            // This selects the higher version, which would be the one that should be used in this scenario.
            if ( !result.TryGetValue( project.RunTimeIdentity.Name, out var existing )
                 || existing.RunTimeIdentity.Version < project.RunTimeIdentity.Version )
            {
                result[project.RunTimeIdentity.Name] = project;
            }
        }

        return result;
    }

    public Type GetType( string reflectionName )
        => this.GetTypeOrNull( reflectionName ) ?? throw new ArgumentOutOfRangeException(
            nameof(reflectionName),
            $"Cannot find a type named '{reflectionName}' in the compile-time project '{this.CompileTimeIdentity}'." );

    internal (CompileTimeFileManifest? File, CompileTimeProject? Project) FindCodeFileFromTransformedPath( string transformedCodePath )
        => this.ClosureCodeFiles[Path.GetFileName( transformedCodePath )]
            .OrderByDescending( t => t.File.TransformedPath.Length )
            .FirstOrDefault();

    private void LoadAssembly()
    {
        this.AssertNotEmpty();

        if ( this._assembly == null )
        {
            // We need to recursively load all dependent assemblies to prevent FileNotFoundException.

            foreach ( var reference in this.References )
            {
                if ( !reference.IsEmpty )
                {
                    reference.LoadAssembly();
                }
            }

            this._assembly = this.Domain.GetOrLoadAssembly( this.CompileTimeIdentity, this.CompiledAssemblyPath! );
        }
    }

    public override string ToString() => this.RunTimeIdentity.ToString();

    /// <summary>
    /// Gets a <see cref="TextMapFile"/> given a the path of the transformed code file.
    /// </summary>
    internal TextMapFile? GetTextMap( string csFilePath )
    {
        if ( this._mapFileProvider != null )
        {
            this._mapFileProvider.TryGetMapFile( csFilePath, out var file );

            return file;
        }
        else
        {
            return null;
        }
    }

    internal DiagnosticManifest ClosureDiagnosticManifest { get; }

    private DiagnosticManifest GetDiagnosticManifest( in ProjectServiceProvider serviceProvider )
    {
        if ( this.IsEmpty )
        {
            return DiagnosticManifest.Empty;
        }

        var declaringTypes = this.Assembly.GetTypes();

        var service = new DiagnosticDefinitionDiscoveryService( serviceProvider );
        var diagnostics = service.GetDiagnosticDefinitions( declaringTypes ).ToImmutableArray();
        var suppressions = service.GetSuppressionDefinitions( declaringTypes ).ToImmutableArray();

        return new DiagnosticManifest( diagnostics, suppressions );
    }
}