// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using K4os.Hash.xxHash;
using Metalama.Backstage.Diagnostics;
using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Maintenance;
using Metalama.Backstage.Utilities;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.CompileTime.Manifest;
using Metalama.Framework.Engine.CompileTime.Serialization;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Formatting;
using Metalama.Framework.Engine.Observers;
using Metalama.Framework.Engine.Options;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.SyntaxGeneration;
using Metalama.Framework.Engine.Templating;
using Metalama.Framework.Engine.Templating.Mapping;
using Metalama.Framework.Engine.Utilities;
using Metalama.Framework.Engine.Utilities.Diagnostics;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Metalama.Framework.Engine.Utilities.Threading;
using Metalama.Framework.Fabrics;
using Metalama.Framework.Options;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Threading;

namespace Metalama.Framework.Engine.CompileTime;

/// <summary>
/// This class is responsible for building a compile-time <see cref="Compilation"/> based on a run-time one.
/// </summary>
internal sealed partial class CompileTimeCompilationBuilder
{
    private const int _inconsistentFallbackLimit = 10;

    // The prefix need to be kept small so we don't consume too many characters in 
    // the TEMP directory, where our paths  must fit within a 256-character limit.
    public const string CompileTimeAssemblyPrefix = "ml!";

    private readonly ProjectServiceProvider _serviceProvider;
    private readonly CompileTimeDomain _domain;
    private readonly Dictionary<ulong, CompileTimeProject> _cache = new();
    private readonly IProjectOptions? _projectOptions;
    private readonly ICompileTimeCompilationBuilderObserver? _observer;
    private readonly ILogger _logger;
    private readonly OutputPathHelper _outputPathHelper;
    private readonly ITaskRunner _taskRunner;

    private static readonly Lazy<ImmutableDictionary<string, string>> _predefinedTypesSyntaxTree = new( GetPredefinedSyntaxTrees );

    private static ImmutableDictionary<string, string> GetPredefinedSyntaxTrees()
    {
        const string prefix = "_Resources_.";

        var assembly = typeof(CompileTimeCompilationBuilder).Assembly;

        // Weirdly enough the assembly prefix of the resource name is not constant; it may or may not include the Roslyn version
        // number.

        var files = assembly.GetManifestResourceNames()
            .Where( n => n.ContainsOrdinal( prefix ) )
            .ToImmutableDictionary(
                name => CompileTimeConstants.GetPrefixedSyntaxTreeName( name.Substring( name.IndexOf( prefix, StringComparison.Ordinal ) + prefix.Length ) )
                        + ".cs",
                name =>
                {
                    using var reader = new StreamReader( assembly.GetManifestResourceStream( name )! );

                    return reader.ReadToEnd();
                } );

        if ( files.IsEmpty )
        {
            throw new AssertionFailedException( "Could not find the predefined syntax trees." );
        }

        return files;
    }

    private static readonly Guid _buildId = AssemblyMetadataReader.GetInstance( typeof(CompileTimeCompilationBuilder).Assembly ).ModuleId;
    private readonly ClassifyingCompilationContextFactory _compilationContextFactory;
    private readonly ITempFileManager _tempFileManager;

    public CompileTimeCompilationBuilder(
        ProjectServiceProvider serviceProvider,
        CompileTimeDomain domain )
    {
        // Building the compile-time compilation is not considered to be performance-critical, so we can always normalize whitespace and preserve trivia.
        this._serviceProvider = serviceProvider.WithService( SyntaxGenerationOptions.Formatted, true );
        this._domain = domain;
        this._observer = serviceProvider.GetService<ICompileTimeCompilationBuilderObserver>();
        this._projectOptions = serviceProvider.GetService<IProjectOptions>();
        this._compilationContextFactory = serviceProvider.GetRequiredService<ClassifyingCompilationContextFactory>();
        this._logger = serviceProvider.GetLoggerFactory().CompileTime();
        this._tempFileManager = serviceProvider.Underlying.GetRequiredBackstageService<ITempFileManager>();
        this._outputPathHelper = new OutputPathHelper( this._tempFileManager );
        this._taskRunner = serviceProvider.Global.GetRequiredService<ITaskRunner>();
    }

    private ulong ComputeSourceHash( FrameworkName? targetFramework, IReadOnlyList<SyntaxTree> compileTimeTrees )
    {
        if ( compileTimeTrees.Count == 0 )
        {
            return 0;
        }

        XXH64 h = new();

        // Hash the target framework.
        if ( targetFramework != null )
        {
            this._logger.Trace?.Log( $"SourceHash: TargetFramework='{targetFramework}'" );
            h.Update( targetFramework.FullName );
        }

        // Hash compilation symbols.
        var preprocessorSymbols = compileTimeTrees.SelectAsReadOnlyList( x => x.Options )
            .SelectMany( x => x.PreprocessorSymbolNames )
            .Distinct()
            .OrderBy( x => x );

        foreach ( var symbol in preprocessorSymbols )
        {
            this._logger.Trace?.Log( $"SourceHash: Symbol='{symbol}'" );
            h.Update( symbol );
        }

        // Hash syntax trees.
        foreach ( var syntaxTree in compileTimeTrees.OrderBy( t => t.FilePath ) )
        {
            // SourceText.Checksum does not seem to return the same thing at compile time than at run time, so we take use our own algorithm.
            var text = syntaxTree.GetText().ToString();
            h.Update( text );

            this._logger.Trace?.Log( $"SourceHash: '{syntaxTree.FilePath}'={string.Join( "", HashUtilities.HashString( text ) )}" );
        }

        var digest = h.Digest();

        return digest;
    }

    private ulong ComputeProjectHash(
        AssemblyIdentity assemblyIdentity,
        IEnumerable<CompileTimeProject> referencedProjects,
        ulong sourceHash )
    {
        XXH64 h = new();

        // We include the MVID of the current module in the hash instead of for instance the version number.
        // The benefit is to avoid conflicts in our development environments where we rebuild without changing the version number.
        // The cost is that there will be redundant caches of compile-time projects in production because the exact same version has different
        // builds, one for each platform.
        h.Update( _buildId );
        this._logger.Trace?.Log( $"ProjectHash: BuildId='{_buildId}'" );

        h.Update( assemblyIdentity.Name );
        h.Update( assemblyIdentity.Version.ToString() );
        this._logger.Trace?.Log( $"ProjectHash: AssemblyIdentity='{assemblyIdentity.Name}, {assemblyIdentity.Version}'" );

        foreach ( var reference in referencedProjects.OrderBy( r => r.Hash ) )
        {
            h.Update( reference.Hash );
            this._logger.Trace?.Log( $"ProjectHash: '{reference.RunTimeIdentity.Name}'={reference.Hash}" );
        }

        h.Update( sourceHash );
        this._logger.Trace?.Log( $"ProjectHash: Source={sourceHash:x}" );

        if ( this._projectOptions != null )
        {
            h.Update( this._projectOptions.FormatCompileTimeCode );
            this._logger.Trace?.Log( $"ProjectHash: FormatCompileTimeCode={this._projectOptions.FormatCompileTimeCode}" );

            h.Update( this._projectOptions.AllowPreviewLanguageFeatures );
            this._logger.Trace?.Log( $"ProjectHash: AllowPreviewLanguageFeatures={this._projectOptions.AllowPreviewLanguageFeatures}" );

            h.Update( this._projectOptions.RequireOrderedAspects );
            this._logger.Trace?.Log( $"ProjectHash: RequireOrderedAspects={this._projectOptions.RequireOrderedAspects}" );

            h.Update( this._projectOptions.RoslynIsCompileTimeOnly );
            this._logger.Trace?.Log( $"ProjectHash: RoslynIsCompileTimeOnly={this._projectOptions.RoslynIsCompileTimeOnly}" );

            h.Update( this._projectOptions.CompileTimeTargetFrameworks );
            this._logger.Trace?.Log( $"ProjectHash: CompileTimeTargetFrameworks={this._projectOptions.CompileTimeTargetFrameworks}" );

            h.Update( this._projectOptions.TemplateLanguageVersion );
            this._logger.Trace?.Log( $"ProjectHash: TemplateLanguageVersion={this._projectOptions.TemplateLanguageVersion}" );
        }

        h.Update( RoslynApiVersion.Current );
        this._logger.Trace?.Log( $"ProjectHash: RoslynApiVersion={RoslynApiVersion.Current}" );

        var digest = h.Digest();

        return digest;
    }

    private bool TryCreateCompileTimeCompilation(
        ClassifyingCompilationContext compilationContext,
        IReadOnlyList<SyntaxTree> treesWithCompileTimeCode,
        IReadOnlyCollection<CompileTimeProject> referencedProjects,
        ImmutableArray<UsingDirectiveSyntax> globalUsings,
        OutputPaths outputPaths,
        IDiagnosticAdder diagnosticSink,
        CancellationToken cancellationToken,
        out Compilation? compileTimeCompilation,
        out ILocationAnnotationMap? locationAnnotationMap,
        out TemplateProjectManifest? compilationResultManifest,
        out bool referencesMetalamaSdk )
    {
        locationAnnotationMap = null;

        // If there is no compile-time tree, there is no need to do anything.
        if ( treesWithCompileTimeCode.Count == 0 )
        {
            compileTimeCompilation = null;
            compilationResultManifest = null;
            referencesMetalamaSdk = false;

            return true;
        }

        var runTimeCompilation = compilationContext.SourceCompilation;

        compileTimeCompilation = this.CreateEmptyCompileTimeCompilation( outputPaths.CompileTimeAssemblyName, referencedProjects );
        var serializableTypes = GetSerializableTypes( compilationContext, treesWithCompileTimeCode, cancellationToken );

        var compileTimeCompilationContext = compileTimeCompilation.GetCompilationContext();

        var templateSymbolManifestBuilder = new TemplateProjectManifestBuilder( compilationContext.SourceCompilation );
        var templateCompiler = new TemplateCompiler( this._serviceProvider, compilationContext, templateSymbolManifestBuilder );

        var produceCompileTimeCodeRewriter = new ProduceCompileTimeCodeRewriter(
            this._serviceProvider,
            this,
            compilationContext,
            compileTimeCompilationContext,
            serializableTypes,
            globalUsings,
            diagnosticSink,
            templateCompiler,
            referencedProjects,
            templateSymbolManifestBuilder,
            cancellationToken );

        var compileTimeToSourceMap = this._observer == null ? null : new Dictionary<string, string>();

        // Creates the new syntax trees. Store them in a dictionary mapping the transformed trees to the source trees.
        var transformedFileGenerator = new TransformedPathGenerator();

        var syntaxTrees = treesWithCompileTimeCode
            .SelectAsArray(
                t => (SyntaxTree: t, FileName: Path.GetFileNameWithoutExtension( t.FilePath ),
                      Hash: XXH64.DigestOf( Encoding.UTF8.GetBytes( t.GetText().ToString() ) )) )
            .OrderBy( t => t.FileName )
            .ThenBy( t => t.Hash )
            .Select(
                t =>
                {
                    var path = transformedFileGenerator.GetTransformedFilePath( t.FileName, t.Hash );

                    var compileTimeSyntaxRoot = produceCompileTimeCodeRewriter.Visit( t.SyntaxTree.GetRoot() )
                        .AssertNotNull();

                    compileTimeToSourceMap?.Add( path, t.SyntaxTree.FilePath );

                    // Remove all preprocessor trivias.
                    // PERF: only do this if the node actually contains preprocessor directives.
                    compileTimeSyntaxRoot = new RemovePreprocessorDirectivesRewriter().Visit( compileTimeSyntaxRoot ).AssertNotNull();

                    return CSharpSyntaxTree.Create(
                        (CSharpSyntaxNode) compileTimeSyntaxRoot,
                        SupportedCSharpVersions.DefaultParseOptions,
                        path,
                        Encoding.UTF8 );
                } )
            .ToReadOnlyList();

        locationAnnotationMap = templateCompiler.LocationAnnotationMap;
        compilationResultManifest = produceCompileTimeCodeRewriter.GetManifest();
        referencesMetalamaSdk = produceCompileTimeCodeRewriter.ReferencesMetalamaSdk;

        if ( !produceCompileTimeCodeRewriter.Success )
        {
            this._logger.Warning?.Log( $"TryCreateCompileTimeCompilation( '{runTimeCompilation.AssemblyName}' ): rewriting failed." );

            return false;
        }

        if ( !produceCompileTimeCodeRewriter.FoundCompileTimeCode )
        {
            // This happens if all compile-time code is illegitimate, i.e. was reported as an error and stripped.

            compileTimeCompilation = null;

            return true;
        }

        compileTimeCompilation = compileTimeCompilation.AddSyntaxTrees( syntaxTrees );
        compileTimeCompilation = new RemoveInvalidUsingRewriter( compileTimeCompilation ).VisitTrees( compileTimeCompilation );

        if ( this._projectOptions is { FormatCompileTimeCode: true } )
        {
            var codeFormatter = this._serviceProvider.GetRequiredService<CodeFormatter>();

            var compilation = compileTimeCompilation;
            var formattedCompilation = this._taskRunner.RunSynchronously( () => codeFormatter.FormatAllAsync( compilation, cancellationToken ) );

            if ( !(formattedCompilation.GetDiagnostics().Any( d => d.Severity == DiagnosticSeverity.Error ) &&
                   !compileTimeCompilation.GetDiagnostics().Any( d => d.Severity == DiagnosticSeverity.Error )) )
            {
                compileTimeCompilation = formattedCompilation;
            }
            else
            {
                this._logger.Warning?.Log(
                    $"The formatting of the compile-time project '{compileTimeCompilation.AssemblyName}' failed. Falling back to the unformatted code." );
            }
        }

        this._observer?.OnCompileTimeCompilation( compileTimeCompilation, compileTimeToSourceMap.AssertNotNull() );

        return true;
    }

    public static bool IsCompileTimeAssemblyName( string assemblyName )
        => assemblyName.StartsWith( CompileTimeAssemblyPrefix, StringComparison.OrdinalIgnoreCase );

    private CSharpCompilation CreateEmptyCompileTimeCompilation( string assemblyName, IEnumerable<CompileTimeProject> referencedProjects )
    {
        var assemblyLocator = this._serviceProvider.GetReferenceAssemblyLocator();
        var preprocessorServiceProvider = this._serviceProvider.GetService<ICompileTimePreprocessorSymbolProvider>();
        
        var preprocessorSymbols =
            preprocessorServiceProvider != null
                ? preprocessorServiceProvider.PreprocessorSymbols.Concat( "NETSTANDARD_2_0" )
                : ["NETSTANDARD_2_0"];

        var parseOptions = new CSharpParseOptions( preprocessorSymbols: preprocessorSymbols, languageVersion: SupportedCSharpVersions.Default );

        var references = assemblyLocator.MetadataReferences;

        var predefinedSyntaxTrees =
            _predefinedTypesSyntaxTree.Value.SelectAsArray( x => CSharpSyntaxTree.ParseText( x.Value, parseOptions, x.Key, Encoding.UTF8 ) );

        return CSharpCompilation.Create(
                assemblyName,
                predefinedSyntaxTrees,
                references,
                new CSharpCompilationOptions( OutputKind.DynamicallyLinkedLibrary, deterministic: true, optimizationLevel: OptimizationLevel.Debug ) )
            .AddReferences(
                referencedProjects
                    .Where( r => r is { IsEmpty: false, IsFramework: false } )
                    .Select( r => r.ToMetadataReference() ) );
    }

    private bool TryEmit(
        OutputPaths outputPaths,
        Compilation compileTimeCompilation,
        IDiagnosticAdder diagnosticSink,
        TextMapDirectory? textMapDirectory,
        CancellationToken cancellationToken )
    {
        this._logger.Trace?.Log( $"TryEmit( '{compileTimeCompilation.AssemblyName}' )" );

        var outputDirectory = outputPaths.Directory.AssertNotNull();

        try
        {
            var emitOptions = new EmitOptions( pdbFilePath: outputPaths.Pdb, debugInformationFormat: DebugInformationFormat.PortablePdb );

            // Write the generated files to disk if we should.
            if ( !Directory.Exists( outputDirectory ) )
            {
                this._logger.Trace?.Log( $"Creating directory '{outputDirectory}'." );
                Directory.CreateDirectory( outputDirectory );
            }

            foreach ( var compileTimeSyntaxTree in compileTimeCompilation.SyntaxTrees )
            {
                var path = Path.Combine( outputDirectory, compileTimeSyntaxTree.FilePath );

                if ( path.Length > 254 )
                {
                    // We should generate, upstream, a path that is short enough. At this stage, it is too late to shorten it.
                    throw new AssertionFailedException( $"Path too long: '{path}'" );
                }

                var text = compileTimeSyntaxTree.GetText().ToString();

                this._logger.Trace?.Log( $"Writing code to '{path}'." );

                // Write the file in a retry loop to handle locks. It seems there are still file lock issues
                // despite the Mutex. 
                var bytes = Encoding.UTF8.GetBytes( text );

                RetryHelper.RetryWithLockDetection(
                    path,
                    p =>
                    {
                        File.WriteAllBytes( p, bytes );

                        // Set the file as read only to we have no temptation to edit it.
                        File.SetAttributes( p, FileAttributes.ReadOnly );
                    },
                    this._serviceProvider.Underlying,
                    logger: this._logger );

                // Reparse from the text. There is a little performance cost of doing that instead of keeping
                // the parsed syntax tree, however, it has the advantage of detecting syntax errors where we have a valid
                // object tree but an invalid syntax text. These errors are very difficult to diagnose in production situations.
                // We can't change anything in the SyntaxTree after we parse so we don't break the link between the PDB and the source file.

                var sourceText = SourceText.From( bytes, bytes.Length, Encoding.UTF8, SourceHashAlgorithm.Sha256 );

                var newTree = CSharpSyntaxTree.ParseText(
                    sourceText,
                    (CSharpParseOptions?) compileTimeSyntaxTree.Options,
                    path,
                    cancellationToken );

                compileTimeCompilation = compileTimeCompilation.ReplaceSyntaxTree( compileTimeSyntaxTree, newTree );
            }

            this._logger.Trace?.Log( $"Writing binary to '{outputPaths.Pe}'." );

            EmitResult? emitResult = null;

            RetryHelper.RetryWithLockDetection(
                outputPaths.Pe,
                _ =>
                {
                    // We don't write the PE stream directly to the final file because this operation is not atomic.
                    // Instead, we write to a temporary file, and then we move this file to the final destination, because
                    // moving a file is an atomic operation.
                    // Otherwise the cache directory would be treated as corrupted because of PE file is one of cache keys.

                    var tempPeFileName = Path.ChangeExtension( outputPaths.Pe, "tmp" );

                    using ( var peStream = File.Create( tempPeFileName ) )
                    using ( var pdbStream = File.Create( outputPaths.Pdb ) )
                    {
                        emitResult = compileTimeCompilation.Emit( peStream, pdbStream, options: emitOptions, cancellationToken: cancellationToken );
                    }

                    if ( emitResult.Success )
                    {
                        // Only move the file if emit was successful.
                        File.Move( tempPeFileName, outputPaths.Pe );
                    }
                },
                this._serviceProvider.Underlying,
                logger: this._logger );

            this._observer?.OnCompileTimeCompilationEmit( emitResult!.Diagnostics );

            // Reports a diagnostic in the original syntax tree.
            void ReportDiagnostics( IEnumerable<Diagnostic> diagnostics )
            {
                foreach ( var diagnostic in diagnostics )
                {
                    textMapDirectory ??= TextMapDirectory.Load( outputDirectory );

                    var transformedPath = diagnostic.Location.SourceTree?.FilePath;

                    if ( !string.IsNullOrEmpty( transformedPath ) && textMapDirectory.TryGetMapFile( transformedPath, out var mapFile ) )
                    {
                        var location = mapFile.GetSourceLocation( diagnostic.Location.SourceSpan );

                        var relocatedDiagnostic = Diagnostic.Create(
                            diagnostic.Id,
                            diagnostic.Descriptor.Category,
                            new NonLocalizedString( diagnostic.GetLocalizedMessage() ),
                            diagnostic.Severity,
                            diagnostic.DefaultSeverity,
                            true,
                            diagnostic.WarningLevel,
                            location: location );

                        diagnosticSink.Report( relocatedDiagnostic );
                    }
                    else
                    {
                        // Coverage: ignore
                        // (this should happen only in case of incorrect generation of compile-time, but in this case a graceful fallback
                        // is better than an exception).

                        diagnosticSink.Report( diagnostic );
                    }
                }
            }

            if ( !emitResult!.Success )
            {
                // When the compile-time assembly is invalid, to enable troubleshooting, we store the source files and the list of diagnostics
                // to a directory that will not be deleted after the build.
                var troubleshootingDirectory = Path.Combine(
                    this._tempFileManager.GetTempDirectory( "CompileTimeTroubleshooting", CleanUpStrategy.Always ),
                    Guid.NewGuid().ToString() );

                Directory.CreateDirectory( troubleshootingDirectory );

                foreach ( var syntaxTree in compileTimeCompilation.SyntaxTrees )
                {
                    var path = Path.Combine( troubleshootingDirectory, Path.GetFileName( syntaxTree.FilePath ) );

                    using ( var writer = File.CreateText( path ) )
                    {
                        syntaxTree.GetText().Write( writer, cancellationToken );
                    }
                }

                var diagnosticPath = Path.Combine( troubleshootingDirectory, "errors.txt" );

                using ( var errorFile = File.CreateText( diagnosticPath ) )
                {
                    errorFile.WriteLine( "Diagnostics:" );

                    foreach ( var diagnostic in emitResult.Diagnostics )
                    {
                        errorFile.WriteLine( "  " + diagnostic );
                    }

                    errorFile.WriteLine( "References:" );

                    foreach ( var reference in compileTimeCompilation.References )
                    {
                        errorFile.WriteLine( "  " + reference.Display );
                    }
                }

                this._logger.Trace?.Log(
                    $"TryEmit( '{compileTimeCompilation.AssemblyName}' ): failure: " +
                    string.Join( Environment.NewLine, emitResult.Diagnostics ) );

                diagnosticSink.Report(
                    TemplatingDiagnosticDescriptors.CannotEmitCompileTimeAssembly.CreateRoslynDiagnostic(
                        null,
                        troubleshootingDirectory ) );

                ReportDiagnostics( emitResult.Diagnostics.Where( d => d.Severity >= DiagnosticSeverity.Error ) );

                DeleteOutputFiles();

                return false;
            }
            else
            {
                this._logger.Trace?.Log( $"TryEmit( '{compileTimeCompilation.AssemblyName}' ): success." );

                return true;
            }
        }
        catch ( Exception e )
        {
            this._logger.Trace?.Log( e.ToString() );

            DeleteOutputFiles();

            throw;
        }

        void DeleteOutputFiles()
        {
            this._logger.Warning?.Log( $"Deleting directory '{outputDirectory}'." );

            try
            {
                // Try to delete with lock detection first to get a better error message.
                if ( Directory.Exists( outputDirectory ) )
                {
                    string? deletedDirectory = null;

                    using ( MutexHelper.WithGlobalLock( outputDirectory, this._logger ) )
                    {
                        var files = Directory.GetFiles( outputDirectory );

                        RetryHelper.RetryWithLockDetection(
                            files,
                            () =>
                            {
                                if ( Directory.Exists( outputDirectory ) )
                                {
                                    // Find a unique directory name transactional deletion.
                                    var deletedIndex = 0;

                                    do
                                    {
                                        deletedIndex++;

                                        deletedDirectory = Path.Combine(
                                            Path.GetDirectoryName( outputDirectory )!,
                                            Path.GetFileName( outputDirectory ) + $".del{deletedIndex}" );
                                    }
                                    while ( Directory.Exists( deletedDirectory ) );

                                    // To delete the directory atomically, rename it.
                                    Directory.Move( outputDirectory, deletedDirectory );
                                }
                            },
                            this._serviceProvider.Underlying );
                    }

                    // Delete the moved directory. There could also be a lock due to anti-virus programs.
                    RetryHelper.RetryWithLockDetection(
                        Directory.GetFiles( deletedDirectory! ),
                        () => Directory.Delete( deletedDirectory!, true ),
                        this._serviceProvider.Underlying );
                }
            }
            catch ( Exception e )
            {
                this._logger.Error?.Log( $"Cannot delete directory '{outputDirectory}': {e.Message}" );
            }
        }
    }

    private static List<UsingDirectiveSyntax> GetUsingsFromOptions( Compilation compilation )
    {
        return ((CSharpCompilation) compilation).Options.Usings.Select( x => SyntaxFactory.UsingDirective( ParseNamespace( x ) ).NormalizeWhitespace() )
            .ToMutableList();

        static NameSyntax ParseNamespace( string ns )
        {
            var parts = ns.Split( '.' );
            NameSyntax result = SyntaxFactory.IdentifierName( parts[0] );

            for ( var i = 1; i < parts.Length; i++ )
            {
                result = SyntaxFactory.QualifiedName( result, SyntaxFactory.IdentifierName( parts[i] ) );
            }

            return result;
        }
    }

    private static (IReadOnlyList<SyntaxTree> SyntaxTrees, ImmutableArray<UsingDirectiveSyntax> GlobalUsings) GetCompileTimeArtifacts(
        ClassifyingCompilationContext compilationContext,
        IReadOnlyList<SyntaxTree>? compileTimeTreesHint,
        CancellationToken cancellationToken )
    {
        var runTimeCompilation = compilationContext.SourceCompilation;

        List<SyntaxTree> compileTimeTrees = new();
        var globalUsings = GetUsingsFromOptions( runTimeCompilation );
        var classifier = compilationContext.SymbolClassifier;

        var trees = compileTimeTreesHint ?? runTimeCompilation.SyntaxTrees;

        var semanticModelProvider = runTimeCompilation.GetSemanticModelProvider();

        foreach ( var tree in trees )
        {
            FindCompileTimeCodeVisitor visitor = new( semanticModelProvider.GetSemanticModel( tree, true ), classifier, cancellationToken );
            visitor.Visit( tree.GetRoot() );

            if ( visitor.HasCompileTimeCode )
            {
                compileTimeTrees.Add( tree );
            }

            globalUsings.AddRange( visitor.GlobalUsings );
        }

        return (compileTimeTrees, globalUsings.ToImmutableArray());
    }

    private static IReadOnlyList<SerializableTypeInfo> GetSerializableTypes(
        ClassifyingCompilationContext runTimeCompilationContext,
        IEnumerable<SyntaxTree> compileTimeSyntaxTrees,
        CancellationToken cancellationToken )
    {
        var allSerializableTypes = new Dictionary<ISymbol, SerializableTypeInfo>( runTimeCompilationContext.CompilationContext.SymbolComparer );

        void OnSerializableTypeDiscovered( SerializableTypeInfo type )
        {
            if ( allSerializableTypes.TryGetValue( type.Type, out var existingType ) )
            {
                existingType.SerializedMembers.AddRange( type.SerializedMembers );
            }
            else
            {
                allSerializableTypes[type.Type] = type;
            }
        }

        var semanticModelProvider = runTimeCompilationContext.SemanticModelProvider;

        foreach ( var tree in compileTimeSyntaxTrees )
        {
            var visitor = new CollectSerializableTypesVisitor(
                runTimeCompilationContext,
                semanticModelProvider.GetSemanticModel( tree, true ),
                OnSerializableTypeDiscovered,
                cancellationToken );

            visitor.Visit( tree.GetRoot() );
        }

        return allSerializableTypes.Values.ToReadOnlyList();
    }

    internal bool TryGetCompileTimeProject(
        Compilation compilation,
        IReadOnlyList<SyntaxTree>? compileTimeTreesHint,
        IReadOnlyList<CompileTimeProject> referencedProjects,
        IDiagnosticAdder diagnosticSink,
        bool cacheOnly,
        out CompileTimeProject? project,
        CancellationToken cancellationToken )
        => this.TryGetCompileTimeProject(
            this._compilationContextFactory.GetInstance( compilation ),
            compileTimeTreesHint,
            referencedProjects,
            diagnosticSink,
            cacheOnly,
            out project,
            cancellationToken );

    /// <summary>
    /// Tries to create a compile-time <see cref="Compilation"/> given a run-time <see cref="Compilation"/>.
    /// </summary>
    internal bool TryGetCompileTimeProject(
        ClassifyingCompilationContext compilationContext,
        IReadOnlyList<SyntaxTree>? compileTimeTreesHint,
        IReadOnlyList<CompileTimeProject> referencedProjects,
        IDiagnosticAdder diagnosticSink,
        bool cacheOnly,
        out CompileTimeProject? project,
        CancellationToken cancellationToken )
    {
        var runTimeCompilation = compilationContext.SourceCompilation;

        // If the compilation does not reference Metalama.Framework, do not create a compile-time project.
        if ( !runTimeCompilation.References.OfType<PortableExecutableReference>()
                .Any(
                    p => p.FilePath != null && Path.GetFileNameWithoutExtension( p.FilePath )
                        .Equals( "Metalama.Framework", StringComparison.OrdinalIgnoreCase ) ) )
        {
            this._logger.Trace?.Log( $"TryGetCompileTimeProject( '{runTimeCompilation.AssemblyName}' ) : no reference to Metalama.Framework" );
            project = null;

            return true;
        }

        var compileTimeArtifacts = GetCompileTimeArtifacts( compilationContext, compileTimeTreesHint, cancellationToken );

        return this.TryGetCompileTimeProjectImpl(
            compilationContext,
            compileTimeArtifacts.SyntaxTrees,
            referencedProjects,
            compileTimeArtifacts.GlobalUsings,
            diagnosticSink,
            cacheOnly,
            out project,
            cancellationToken );
    }

    private bool TryGetCompileTimeProjectFromCache(
        Compilation runTimeCompilation,
        IReadOnlyList<CompileTimeProject> referencedProjects,
        OutputPaths outputPaths,
        ulong projectHash,
        [NotNullWhen( true )] out CompileTimeProject? project,
        out bool wasInconsistent,
        CacheableTemplateDiscoveryContextProvider? cacheableTemplateDiscoveryContextProvider )
    {
        this._logger.Trace?.Log( $"TryGetCompileTimeProjectFromCache( '{runTimeCompilation.AssemblyName}' )" );

        // Look in in-memory cache.
        if ( this._cache.TryGetValue( projectHash, out project ) )
        {
            this._logger.Trace?.Log( $"TryGetCompileTimeProjectFromCache( '{runTimeCompilation.AssemblyName}' ): found in memory cache." );
            wasInconsistent = false;

            return true;
        }

        if ( !this.CheckCompileTimeProjectDiskCache( runTimeCompilation.AssemblyName, outputPaths, out wasInconsistent ) )
        {
            return false;
        }

        this._logger.Trace?.Log( $"TryGetCompileTimeProjectFromCache( '{runTimeCompilation.AssemblyName}' ): deserializing." );

        // Deserialize the manifest.
        var manifest = CompileTimeProjectManifest.Deserialize( RetryHelper.Retry( () => File.OpenRead( outputPaths.Manifest ), logger: this._logger ) );

        if ( !this.CheckManifestDiskCache( outputPaths, manifest, out wasInconsistent ) )
        {
            return false;
        }

        project = CompileTimeProject.Create(
            this._serviceProvider,
            this._domain,
            runTimeCompilation.Assembly.Identity,
            new AssemblyIdentity( outputPaths.CompileTimeAssemblyName ),
            referencedProjects,
            manifest,
            outputPaths.Pe,
            outputPaths.Directory,
            FullPathTextMapFileProvider.Instance,
            cacheableTemplateDiscoveryContextProvider );

        this._cache.Add( projectHash, project );

        wasInconsistent = false;

        return true;
    }

    private bool CheckCompileTimeProjectDiskCache(
        string? assemblyName,
        OutputPaths outputPaths,
        out bool wasInconsistent )
    {
        var peExists = File.Exists( outputPaths.Pe );
        var manifestExists = File.Exists( outputPaths.Manifest );

        // Look on disk.
        if ( !peExists )
        {
            this._logger.Trace?.Log( $"CheckCompileTimeProjectDiskCache( '{assemblyName}' ): '{outputPaths.Pe}' not found." );
        }

        if ( !manifestExists )
        {
            this._logger.Trace?.Log( $"CheckCompileTimeProjectDiskCache( '{assemblyName}' ): '{outputPaths.Manifest}' not found." );
        }

        if ( peExists ^ manifestExists )
        {
            // Here we presume that other files (that are not checked and are cached) are never locked and never deleted without PE or manifest missing.
            this._logger.Trace?.Log(
                $"CheckCompileTimeProjectDiskCache( '{assemblyName}' ): '{outputPaths.Directory}' inconsistent, will attempt an alternate." );

            wasInconsistent = true;

            return false;
        }
        else if ( !peExists || !manifestExists )
        {
            this._logger.Trace?.Log( $"CheckCompileTimeProjectDiskCache( '{assemblyName}' ): Cache miss." );
            wasInconsistent = false;

            return false;
        }

        this._logger.Trace?.Log( $"CheckCompileTimeProjectDiskCache( '{assemblyName}' ): found on disk." );
        wasInconsistent = false;

        return true;
    }

    private bool CheckManifestDiskCache( OutputPaths outputPaths, CompileTimeProjectManifest manifest, out bool wasInconsistent )
    {
        var directory = outputPaths.Directory;

        foreach ( var file in manifest.Files )
        {
            var path = Path.Combine( directory, file.TransformedPath );

            if ( !File.Exists( path ) )
            {
                this._logger.Trace?.Log( $"The file '{path}' does not exist. This means that the directory is corrupt. Throwing an exception." );

                wasInconsistent = true;

                return false;
            }
        }

        wasInconsistent = false;

        return true;
    }

    private bool TryGetCompileTimeProjectImpl(
        ClassifyingCompilationContext compilationContext,
        IReadOnlyList<SyntaxTree> sourceTreesWithCompileTimeCode,
        IReadOnlyList<CompileTimeProject> referencedProjects,
        ImmutableArray<UsingDirectiveSyntax> globalUsings,
        IDiagnosticAdder diagnosticSink,
        bool cacheOnly,
        out CompileTimeProject? project,
        CancellationToken cancellationToken )
    {
        var runTimeCompilation = compilationContext.SourceCompilation;

        // Check the in-process cache.
        var (sourceHash, projectHash, outputPaths) =
            this.GetPreCacheProjectInfo( runTimeCompilation, sourceTreesWithCompileTimeCode, referencedProjects );

        void ReportCachedDiagnostics( CompileTimeProject project )
        {
            var diagnostics = project.Manifest.AssertNotNull().Diagnostics;

            if ( diagnostics?.Any() == true )
            {
                var sourceTreesForDiagnostics = sourceTreesWithCompileTimeCode.OrderBy( t => t.FilePath ).ToArray();

                diagnosticSink.Report( diagnostics.SelectAsReadOnlyList( d => d.ToDiagnostic( sourceTreesForDiagnostics ) ) );
            }
        }

        if ( this.TryGetCompileTimeProjectFromCache(
                runTimeCompilation,
                referencedProjects,
                outputPaths,
                projectHash,
                out project,
                out var wasInconsistent,
                null ) )
        {
            ReportCachedDiagnostics( project );

            return true;
        }

        if ( cacheOnly )
        {
            // We were asked to get cache projects only. Don't create it.

            var alternateDirectoryOrdinal = 0;

            if ( !wasInconsistent )
            {
                // The primary directory was not present, no need to enter lock.
                project = null;

                return false;
            }

            // The main directory was inconsistent, we will enter a lock to synchronize with other processes that may be creating a fallback.
            using ( this.WithLock( outputPaths.CompileTimeAssemblyName ) )
            {
                while ( alternateDirectoryOrdinal < _inconsistentFallbackLimit )
                {
                    if ( this.TryGetCompileTimeProjectFromCache(
                            runTimeCompilation,
                            referencedProjects,
                            outputPaths,
                            projectHash,
                            out project,
                            out wasInconsistent,
                            null ) )
                    {
                        ReportCachedDiagnostics( project );

                        return true;
                    }

                    if ( wasInconsistent )
                    {
                        // If the cache directory is not consistent, attempt the next alternate fallback.
                        alternateDirectoryOrdinal++;
                        outputPaths = outputPaths.WithAlternateOrdinal( alternateDirectoryOrdinal );
                    }
                    else
                    {
                        // The cache directory was not present, we can return.
                        project = null;

                        return false;
                    }
                }
            }

            throw CreateTooManyInconsistentCacheDirectoriesException( runTimeCompilation.AssemblyName, outputPaths );
        }
        else
        {
            var alternateDirectoryOrdinal = 0;

            // Do not try to try more than 10 alternates, probability of that happening is low and we may get into an infinite cycle.
            using ( this.WithLock( outputPaths.CompileTimeAssemblyName ) )
            {
                while ( alternateDirectoryOrdinal < _inconsistentFallbackLimit )
                {
                    // Do a second cache lookup within the lock.
                    if ( this.TryGetCompileTimeProjectFromCache(
                            runTimeCompilation,
                            referencedProjects,
                            outputPaths,
                            projectHash,
                            out project,
                            out wasInconsistent,
                            null ) )
                    {
                        ReportCachedDiagnostics( project );

                        // Coverage: ignore (this depends on a multi-threaded condition)
                        return true;
                    }

                    if ( wasInconsistent )
                    {
                        // The cache directory was not consistent, i.e. files are missing and most likely the file that exists is locked.                    
                        // We will defer to a suffixed directory (or a higher suffix).
                        alternateDirectoryOrdinal++;
                        outputPaths = outputPaths.WithAlternateOrdinal( alternateDirectoryOrdinal );

                        continue;
                    }

                    var diagnostics = new List<Diagnostic>();

                    // Without this local function, the closure for this method causes a memory leak.
                    static DiagnosticAdderAdapter CreateDiagnosticAdder( IDiagnosticAdder diagnosticSink, List<Diagnostic> diagnostics )
                    {
                        return new DiagnosticAdderAdapter(
                            diagnostic =>
                            {
                                // Report diagnostics to the current sink and also store them for the cache.
                                diagnosticSink.Report( diagnostic );
                                diagnostics.Add( diagnostic );
                            } );
                    }

                    var diagnosticAdder = CreateDiagnosticAdder( diagnosticSink, diagnostics );

                    // Generate the C# compilation.
                    if ( !this.TryCreateCompileTimeCompilation(
                            compilationContext,
                            sourceTreesWithCompileTimeCode,
                            referencedProjects,
                            globalUsings,
                            outputPaths,
                            diagnosticAdder,
                            cancellationToken,
                            out var compileTimeCompilation,
                            out var locationAnnotationMap,
                            out var compilationResultManifest,
                            out var referencesMetalamaSdk ) )
                    {
                        project = null;

                        this._logger.Trace?.Log( $"TryCreateCompileTimeCompilation( '{runTimeCompilation.AssemblyName}' ): TryCreateCompileTimeCompilation." );

                        return false;
                    }

                    if ( compileTimeCompilation == null )
                    {
                        // The run-time compilation does not contain compile-time classes, but it can have compile-time references.

                        if ( referencedProjects.Count == 0 )
                        {
                            project = null;
                        }
                        else
                        {
                            project = CompileTimeProject.CreateEmpty(
                                this._serviceProvider,
                                this._domain,
                                runTimeCompilation.Assembly.Identity,
                                new AssemblyIdentity( outputPaths.CompileTimeAssemblyName ),
                                referencedProjects );
                        }

                        return true;
                    }
                    else
                    {
                        var textMapDirectory = TextMapDirectory.Create( compileTimeCompilation, locationAnnotationMap! );

                        if ( !this.TryEmit( outputPaths, compileTimeCompilation, diagnosticSink, textMapDirectory, cancellationToken ) )
                        {
                            project = null;

                            this._logger.Trace?.Log( $"TryGetCompileTimeProjectImpl( '{runTimeCompilation.AssemblyName}' ): emit failed." );

                            return false;
                        }

                        textMapDirectory.Write( outputPaths.Directory );

                        // Create the manifest.
                        var compilationForManifest = compileTimeCompilation;
                        var aspectType = compilationForManifest.GetTypeByMetadataName( typeof(IAspect).FullName.AssertNotNull() );
                        var fabricType = compilationForManifest.GetTypeByMetadataName( typeof(Fabric).FullName.AssertNotNull() );
                        var transitiveFabricType = compilationForManifest.GetTypeByMetadataName( typeof(TransitiveProjectFabric).FullName.AssertNotNull() );
                        var templateProviderType = compilationForManifest.GetTypeByMetadataName( typeof(ITemplateProvider).FullName.AssertNotNull() );
                        var optionType = compilationForManifest.GetTypeByMetadataName( typeof(IHierarchicalOptions).FullName.AssertNotNull() );

                        bool IsAspect( INamedTypeSymbol t )
                        {
                            return compilationForManifest.HasImplicitConversion( t, aspectType );
                        }

                        bool IsFabric( INamedTypeSymbol t )
                        {
                            return compilationForManifest.HasImplicitConversion( t, fabricType );
                        }

                        var allTypes = compilationForManifest.Assembly.GetAllTypes().ToReadOnlyList();

                        var aspectTypeNames = allTypes
                            .Where( IsAspect )
                            .Select( t => t.GetReflectionFullName().AssertNotNull() )
                            .ToReadOnlyList();

                        var fabricTypes = allTypes
                            .Where(
                                t => IsFabric( t ) &&
                                     !compilationForManifest.HasImplicitConversion( t, transitiveFabricType ) )
                            .ToReadOnlyList();

                        var fabricTypeNames = fabricTypes
                            .SelectAsArray( t => t.GetReflectionFullName().AssertNotNull() );

                        var transitiveFabricTypeNames = allTypes
                            .Where( t => compilationForManifest.HasImplicitConversion( t, transitiveFabricType ) )
                            .Concat( fabricTypes.Where( t => t.GetAttributes().Any( a => a.AttributeClass?.Name == nameof(InheritableAttribute) ) ) )
                            .Select( t => t.GetReflectionFullName().AssertNotNull() )
                            .ToReadOnlyList();

                        var compilerPlugInTypeNames = allTypes
                            .Where( t => t.GetAttributes().Any( a => a.AttributeClass?.Name == nameof(MetalamaPlugInAttribute) ) )
                            .Select( t => t.GetReflectionFullName().AssertNotNull() )
                            .ToReadOnlyList();

                        var otherTemplateTypeNames = allTypes
                            .Where( t => compilationForManifest.HasImplicitConversion( t, templateProviderType ) && !IsAspect( t ) && !IsFabric( t ) )
                            .Select( t => t.GetReflectionFullName().AssertNotNull() )
                            .ToReadOnlyList();

                        var optionTypeNames = allTypes.Where( t => compilationForManifest.HasImplicitConversion( t, optionType ) )
                            .Select( t => t.GetReflectionFullName().AssertNotNull() )
                            .ToReadOnlyList();

                        Dictionary<string, int>? sourceFilePathIndexes = null;

                        if ( diagnostics.Any() )
                        {
                            sourceFilePathIndexes = sourceTreesWithCompileTimeCode
                                .OrderBy( x => x.FilePath )
                                .Select( ( tree, index ) => (tree.FilePath, index) )
                                .ToDictionary( x => x.FilePath, x => x.index );
                        }

                        var manifest = new CompileTimeProjectManifest(
                            runTimeCompilation.Assembly.Identity.ToString(),
                            runTimeCompilation.GetTargetFramework()?.ToString() ?? "",
                            aspectTypeNames,
                            compilerPlugInTypeNames,
                            fabricTypeNames,
                            transitiveFabricTypeNames,
                            otherTemplateTypeNames,
                            optionTypeNames,
                            referencedProjects.SelectAsImmutableArray( r => r.RunTimeIdentity.GetDisplayName() ),
                            compilationResultManifest,
                            sourceHash,
                            textMapDirectory.FilesByTargetPath.Values.Select( f => new CompileTimeFileManifest( f ) ).ToArray(),
                            diagnostics.SelectAsArray( d => new CompileTimeDiagnosticManifest( d, sourceFilePathIndexes! ) ),
                            referencesMetalamaSdk );

                        project = CompileTimeProject.Create(
                            this._serviceProvider,
                            this._domain,
                            runTimeCompilation.Assembly.Identity,
                            compileTimeCompilation.Assembly.Identity,
                            referencedProjects,
                            manifest,
                            outputPaths.Pe,
                            outputPaths.Directory,
                            textMapDirectory,
                            null );

                        this._logger.Trace?.Log( $"Writing manifest to '{outputPaths.Manifest}'." );

                        using ( var manifestStream = File.Create( outputPaths.Manifest ) )
                        {
                            manifest.Serialize( manifestStream );
                        }
                    }

                    this._cache.Add( projectHash, project );

                    return true;
                }
            }

            throw CreateTooManyInconsistentCacheDirectoriesException( runTimeCompilation.AssemblyName, outputPaths );
        }
    }

    private (ulong SourceHash, ulong ProjectHash, OutputPaths OutputPaths) GetPreCacheProjectInfo(
        Compilation runTimeCompilation,
        IReadOnlyList<SyntaxTree> sourceTreesWithCompileTimeCode,
        IEnumerable<CompileTimeProject> referencedProjects )
    {
        var targetFramework = runTimeCompilation.GetTargetFramework();
        var assemblyIdentity = runTimeCompilation.Assembly.Identity;

        var sourceHash = this.ComputeSourceHash( targetFramework, sourceTreesWithCompileTimeCode );
        var projectHash = this.ComputeProjectHash( assemblyIdentity, referencedProjects, sourceHash );

        var outputPaths = this._outputPathHelper.GetOutputPaths( runTimeCompilation.AssemblyName!, targetFramework, projectHash );

        return (sourceHash, projectHash, outputPaths);
    }

    /// <summary>
    /// Tries to compile (to a binary image) a project given its manifest and syntax trees. 
    /// </summary>
    public bool TryCompileDeserializedProject(
        AssemblyIdentity runTimeAssemblyIdentity,
        CompileTimeProjectManifest manifest,
        IReadOnlyList<SyntaxTree> syntaxTrees,
        IReadOnlyList<CompileTimeProject> referencedProjects,
        IDiagnosticAdder diagnosticAdder,
        CancellationToken cancellationToken,
        out string? compileTimeAssemblyName,
        out string assemblyPath,
        out string? sourceDirectory )
    {
        var runTimeAssemblyName = runTimeAssemblyIdentity.Name;
        var targetFramework = string.IsNullOrEmpty( manifest.TargetFramework ) ? null : new FrameworkName( manifest.TargetFramework );

        this._logger.Trace?.Log( $"TryCompileDeserializedProject( '{runTimeAssemblyName}' )" );
        var projectHash = this.ComputeProjectHash( runTimeAssemblyIdentity, referencedProjects, manifest.SourceHash );

        var outputPaths = this._outputPathHelper.GetOutputPaths( runTimeAssemblyName, targetFramework, projectHash );

        var compilation = this.CreateEmptyCompileTimeCompilation( outputPaths.CompileTimeAssemblyName, referencedProjects )
            .AddSyntaxTrees( syntaxTrees );

        var alternateOrdinal = 0;

        using ( this.WithLock( outputPaths.CompileTimeAssemblyName ) )
        {
            while ( alternateOrdinal < _inconsistentFallbackLimit )
            {
                if ( this.CheckCompileTimeProjectDiskCache( runTimeAssemblyName, outputPaths, out var wasInconsistent ) )
                {
                    // If the file already exists, given that it has a strong hash, it means that the assembly has already been 
                    // emitted and it does not need to be done a second time.

                    assemblyPath = outputPaths.Pe;
                    sourceDirectory = outputPaths.Directory;
                    compileTimeAssemblyName = outputPaths.CompileTimeAssemblyName;

                    this._logger.Trace?.Log( $"TryCompileDeserializedProject( '{runTimeAssemblyName}' ): compile-time project already exists." );

                    if ( this.CheckManifestDiskCache( outputPaths, manifest, out wasInconsistent ) )
                    {
                        return true;
                    }
                }
                else
                {
                    if ( !wasInconsistent )
                    {
                        assemblyPath = outputPaths.Pe;
                        sourceDirectory = outputPaths.Directory;
                        compileTimeAssemblyName = outputPaths.CompileTimeAssemblyName;

                        var emitResult = this.TryEmit( outputPaths, compilation, diagnosticAdder, null, cancellationToken );

                        if ( emitResult )
                        {
                            // If successful, we will add the manifest which is not written by TryEmit.
                            using ( var stream = File.OpenWrite( outputPaths.Manifest ) )
                            {
                                manifest.Serialize( stream );
                            }
                        }

                        return emitResult;
                    }
                }

                Invariant.Assert( wasInconsistent );

                // The cache was inconsistent, we will defer to a suffixed directory.
                alternateOrdinal++;
                outputPaths = outputPaths.WithAlternateOrdinal( alternateOrdinal );
            }
        }

        throw CreateTooManyInconsistentCacheDirectoriesException( runTimeAssemblyName, outputPaths );
    }

    private static Exception CreateTooManyInconsistentCacheDirectoriesException( string? runTimeAssemblyName, OutputPaths outputPaths )
        => new InvalidOperationException(
            $"TryGetCompileTimeProjectImpl( '{runTimeAssemblyName}' ): too many inconsistent cache directories for the compile-time assembly. " +
            $"Please delete \"{Path.GetDirectoryName( outputPaths.Directory )}\" directory before retrying the build. " +
            $"If this occurs on a build server, please verify that the cache is correctly cleaned up between builds." );

    private IDisposable WithLock( string compileTimeAssemblyName ) => MutexHelper.WithGlobalLock( compileTimeAssemblyName, this._logger );
}