// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Diagnostics;
using Metalama.Backstage.Infrastructure;
using Metalama.Backstage.Maintenance;
using Metalama.Backstage.Threading;
using Metalama.Compiler;
using Metalama.Framework.Aspects;
using Metalama.Framework.CompileTimeContracts;
using Metalama.Framework.Engine.AspectWeavers;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Options;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Metalama.Framework.RunTime;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Xml.Linq;
using IMethodSymbol = Microsoft.CodeAnalysis.IMethodSymbol;

namespace Metalama.Framework.Engine.CompileTime;

/// <summary>
/// Provides the location to the reference assemblies that are needed to create the compile-time projects.
/// This is achieved by creating an MSBuild project and restoring it.
/// </summary>
internal sealed class CompileTimeAssemblyLocator
{
    private const string _compileTimeFrameworkAssemblyName = "Metalama.Framework";
    private const string _compilerInterfaceAssemblyName = "Metalama.Compiler.Interface";
    private const string _defaultCompileTimeTargetFrameworks = "netstandard2.0;net8.0;net48";
    private static readonly ImmutableArray<string> _defaultNugetSources = GetDefaultNuGetSources().ToImmutableArray();

    /// <summary>
    /// Returns the path given to the metadata reference of an assembly that this class embeds as a manifest resource.
    /// </summary>
    /// <remarks>
    /// The path is a display string and not a file, because the assembly exists only inside the container. It is formed
    /// so that <see cref="IsEmbeddedAssemblyFilePath"/> recognizes it, which is how a caller can tell that the file
    /// system has nothing to say about the reference.
    /// </remarks>
    private static string GetEmbeddedAssemblyFilePath( string containerPath, string assemblyName ) => $"[{containerPath}]{assemblyName}.dll";

    /// <summary>
    /// Determines whether the given path of a metadata reference is one produced by
    /// <see cref="GetEmbeddedAssemblyFilePath"/>, that is, whether it names an assembly embedded as a manifest resource
    /// instead of a file.
    /// </summary>
    /// <remarks>
    /// Tested by shape rather than by asking the file system, because the path is one this class produced and because a
    /// path of that shape is not even syntactically valid, so an attempt to open it raises
    /// <see cref="System.IO.IOException"/> instead of reporting the file as absent.
    /// </remarks>
    public static bool IsEmbeddedAssemblyFilePath( string path ) => path.StartsWith( "[", StringComparison.Ordinal );

    private static IEnumerable<string> GetDefaultNuGetSources()
    {
        yield return "https://api.nuget.org/v3/index.json";

        if ( RuntimeInformation.IsOSPlatform( OSPlatform.Windows ) )
        {
            var programFilesX86 = string.Empty;

            try
            {
                programFilesX86 = Environment.GetFolderPath( Environment.SpecialFolder.ProgramFilesX86 );
            }
            catch ( PlatformNotSupportedException )
            {
                // Do nothing, the variable stays Empty.
            }

            if ( programFilesX86 != string.Empty )
            {
                yield return Path.Combine( programFilesX86, "Microsoft SDKs\\NuGetPackages" );
            }
        }
    }

    private readonly string _cacheDirectory = null!;
    private readonly string? _binaryLogDirectory;
    private readonly ILogger _logger;
    private readonly INamedLockService _lockService;
    private readonly DotNetTool _dotNetTool;
    private readonly int _restoreTimeout;
    private readonly ImmutableArray<string> _targetFrameworks;
    private readonly string? _sdkVersion;
    private readonly string? _msBuildBinPath;

    /// <summary>
    /// This compilation is used by the <see cref="SymbolClassifier"/> to determine if an API is available
    /// at compile time.
    /// </summary>
    private readonly Compilation _referenceCompilation = null!;

    private readonly CompilationContext _referenceCompilationContext = null!;
    private readonly IFileSystem _fileSystem;
    private readonly NuGetHelper _nuGetHelper;
    private readonly IReadOnlyList<string>? _nugetConfigFiles;

    /// <summary>
    /// The address of the package source that serves the Roslyn packages of the current build, or <c>null</c> when
    /// nuget.org serves them, which is the ordinary case. See issue #1885.
    /// </summary>
    private readonly string? _prereleasePackageSourceUrl;

    /// <summary>
    /// The configuration files that NuGet applies to the reference-assembly project without their being found by
    /// <see cref="NuGetHelper.GetConfigFiles"/>, that is, the user-level configuration file. It is resolved only when
    /// <see cref="_prereleasePackageSourceUrl"/> is not <c>null</c>, because it is read only to decide the mapping.
    /// </summary>
    private readonly ImmutableArray<string> _userNuGetConfigFiles = ImmutableArray<string>.Empty;

    /// <summary>
    /// The address of the package source that was declared without a package source mapping, which is what the failure
    /// classifier needs to explain a package resolution failure. See issue #1885.
    /// </summary>
    private string? _unmappedPrereleasePackageSourceUrl;

    /// <summary>
    /// Gets the name (without path and extension) of all compile-time assemblies, including Metalama, Roslyn and .NET standard.
    /// </summary>
    internal ImmutableHashSet<string> AssemblyNames { get; } = ImmutableHashSet<string>.Empty;

    /// <summary>
    /// Gets the full path of executable system assemblies for the current platform.
    /// </summary>
    internal ImmutableArray<string> AdditionalCompileTimeAssemblyPaths { get; }

    internal ImmutableDictionary<string, AssemblyIdentity> AssemblyIdentities { get; } = ImmutableDictionary<string, AssemblyIdentity>.Empty;

    internal bool IsStandardAssemblyName( string assemblyName )
        => string.Equals( assemblyName, "System.Private.CoreLib", StringComparison.OrdinalIgnoreCase )
           || this.AssemblyNames.Contains( assemblyName );

    /// <summary>
    /// Gets the full path of all standard assemblies, including Metalama, Roslyn and .NET standard.
    /// </summary>
    internal ImmutableArray<MetadataReference> MetadataReferences { get; }

    /// <summary>
    /// Creates a <see cref="CompileTimeAssemblyLocator"/>, or reports to <paramref name="diagnostics"/> and returns
    /// <c>false</c> when the compile-time reference assemblies cannot be resolved.
    /// </summary>
    /// <remarks>
    /// Resolving the reference assemblies runs a nested build, which fails for reasons that belong to the environment
    /// rather than to Metalama: a NuGet feed that requires credentials, a <c>global.json</c> that pins an absent .NET
    /// SDK, and so on. Such a failure is reported as a diagnostic and not thrown, so that the caller can abort the
    /// pipeline the way it aborts for any other diagnostic. See issue #1744.
    /// </remarks>
    internal static bool TryCreate(
        in ProjectServiceProvider serviceProvider,
        string additionalReferences,
        ITempFileManager tempFileManager,
        IDiagnosticAdder diagnostics,
        [NotNullWhen( true )] out CompileTimeAssemblyLocator? locator )
    {
        var candidate = new CompileTimeAssemblyLocator( serviceProvider, additionalReferences, tempFileManager, diagnostics, out var success );
        locator = success ? candidate : null;

        return success;
    }

    /// <remarks>
    /// The constructor interleaves the steps that can fail with the assignment of the fields that depend on them, so it
    /// reports through <paramref name="diagnostics"/> and returns early instead of throwing, setting
    /// <paramref name="success"/> to <c>false</c>. A partially initialized instance never escapes
    /// <see cref="TryCreate"/>, which is the only caller.
    /// </remarks>
    private CompileTimeAssemblyLocator(
        in ProjectServiceProvider serviceProvider,
        string additionalReferences,
        ITempFileManager tempFileManager,
        IDiagnosticAdder diagnostics,
        out bool success )
    {
        success = false;

        this._logger = serviceProvider.GetLoggerFactory().GetLogger( nameof(CompileTimeAssemblyLocator) );
        this._lockService = serviceProvider.Global.GetRequiredBackstageService<INamedLockService>();
        this._fileSystem = serviceProvider.Global.GetRequiredBackstageService<IFileSystem>();
        this._nuGetHelper = new NuGetHelper( serviceProvider.Global );
        this._sdkVersion = serviceProvider.GetRequiredService<IProjectOptions>().SdkVersion;
        this._msBuildBinPath = serviceProvider.GetRequiredService<IProjectOptions>().MSBuildBinPath;

        this._dotNetTool = new DotNetTool( serviceProvider.Global );

        var projectOptions = serviceProvider.GetRequiredService<IProjectOptions>();

        this._restoreTimeout = projectOptions.ReferenceAssemblyRestoreTimeout ?? 120_000;

        this._logger.Trace?.Log(
            "Assembly versions: " + string.Join(
                ", ",
                new[] { this.GetType(), typeof(IAspect), typeof(IAspectWeaver), typeof(ITemplateSyntaxFactory), typeof(FieldOrPropertyInfo) }
                    .SelectAsReadOnlyList( x => x.Assembly.Location ) ) );

        this._targetFrameworks = ParseTargetFrameworks(
            string.IsNullOrEmpty( projectOptions.CompileTimeTargetFrameworks )
                ? _defaultCompileTimeTargetFrameworks
                : projectOptions.CompileTimeTargetFrameworks! );

        // The parsed value is rejoined with the separator that MSBuild expects, because it is written into the
        // TargetFrameworks property of the temporary project, and it is what the cache key is computed from, so that
        // two spellings of one set of target frameworks share a cache directory.
        var targetFrameworksString = string.Join( ";", this._targetFrameworks );

        if ( !this._targetFrameworks.Contains( "netstandard2.0" ) )
        {
            ReportInvalidTargetFrameworks( diagnostics, targetFrameworksString, "it must include 'netstandard2.0'" );

            return;
        }

        // Load nuget.config.
        if ( projectOptions.ProjectPath != null )
        {
            this._nugetConfigFiles = this._nuGetHelper.GetConfigFiles( projectOptions.ProjectPath );
        }

        // On a version branch that compiles against a prerelease Roslyn, the requested Roslyn packages are served by a
        // package source that the user has no reason to declare, so the generated nuget.config declares it. See #1885.
        this._prereleasePackageSourceUrl = RoslynApiVersion.Current.ToPrereleasePackageSourceUrl();

        if ( this._prereleasePackageSourceUrl != null )
        {
            var userConfigFile = this._nuGetHelper.GetUserConfigFile();

            if ( userConfigFile != null )
            {
                this._userNuGetConfigFiles = ImmutableArray.Create( userConfigFile );
            }
        }

        // Get additional NuGet source through the legacy RestoreSources project option.
        string? additionalNugetSources = null;

        if ( projectOptions.RestoreSources != null )
        {
            var sources = projectOptions.RestoreSources
                .Split( ';' )
                .Except( _defaultNugetSources )
                .ToArray();

            if ( sources.Any() )
            {
                additionalNugetSources = string.Join( ";", sources );
            }
        }

        // Compute a unique hash for the combination of factors.
        using var hashHandle = HashUtilities.AllocateHasher();
        var hashBuilder = hashHandle.Value;
        hashBuilder.Append( additionalReferences );
        hashBuilder.Append( targetFrameworksString );
        hashBuilder.Append( additionalNugetSources );
        hashBuilder.Append( RoslynApiVersion.Current );

        foreach ( var nugetConfigFile in this._nugetConfigFiles ?? [] )
        {
            var nugetConfigContent = this._fileSystem.ReadAllText( nugetConfigFile );
            hashBuilder.Append( nugetConfigContent );
        }

        // The prerelease package source, and the user-level configuration file that decides how it is mapped, both
        // change the generated nuget.config, so a directory built without them must not be reused. Nothing is appended
        // when there is no such source, so that a branch that compiles against a released Roslyn keeps the hash it has
        // today and its cache directories stay valid. See #1885.
        if ( this._prereleasePackageSourceUrl != null )
        {
            hashBuilder.Append( this._prereleasePackageSourceUrl );

            foreach ( var userConfigFile in this._userNuGetConfigFiles )
            {
                hashBuilder.Append( this._fileSystem.ReadAllText( userConfigFile ) );
            }
        }

        // Include optional salt for cache invalidation (useful for testing).
        if ( !string.IsNullOrEmpty( projectOptions.AssemblyLocatorSalt ) )
        {
            hashBuilder.Append( projectOptions.AssemblyLocatorSalt );
        }

        var projectHash = hashBuilder.GetCurrentHashAsUInt64().ToString( "x", CultureInfo.InvariantCulture );

        this._cacheDirectory = tempFileManager.GetTempDirectory( TempDirectories.AssemblyLocator, CleanUpStrategy.WhenUnused, projectHash );
        this._binaryLogDirectory = projectOptions.AssemblyLocatorBinaryLogDirectory;

        // Get Metalama implementation contract assemblies (but not the public API, for which we need a special compile-time build).
        var metalamaImplementationAssemblies =
            new[] { typeof(IAspectWeaver), typeof(ITemplateSyntaxFactory) }.ToDictionary(
                x => x.Assembly.GetName().Name.AssertNotNull(),
                x => x.Assembly.Location );

        // Force Metalama.Compiler.Interface to be loaded in the AppDomain.
        MetalamaCompilerInfo.EnsureInitialized();

        var metalamaImplementationAssemblyNames = metalamaImplementationAssemblies.Keys;
        var metalamaImplementationPaths = metalamaImplementationAssemblies.Values;

        // Get system assemblies.
        if ( !this.TryGetReferenceAssembliesManifest(
                targetFrameworksString,
                additionalReferences,
                additionalNugetSources,
                projectOptions.AssemblyLocatorHooksDirectory,
                diagnostics,
                out var referencePaths ) )
        {
            return;
        }

        // Sets the collection of all standard assemblies, i.e. system assemblies and ours.
        this.AssemblyNames = metalamaImplementationAssemblyNames
            .Concat( [_compileTimeFrameworkAssemblyName, _compilerInterfaceAssemblyName] )
            .Concat( referencePaths.SelectAsReadOnlyList( x => Path.GetFileNameWithoutExtension( x ).AssertNotNull() ) )
            .ToImmutableHashSet( StringComparer.OrdinalIgnoreCase );

        // Also provide our embedded assemblies.

        var embeddedAssemblies =
            new[] { _compileTimeFrameworkAssemblyName, _compilerInterfaceAssemblyName }.SelectAsImmutableArray(
                name
                    => (MetadataReference)
                    MetadataReference.CreateFromStream(
                        this.GetType().Assembly.GetManifestResourceStream( name + ".dll" )
                        ?? throw new InvalidOperationException( $"{name}.dll not found in assembly manifest resources." ),
                        filePath: GetEmbeddedAssemblyFilePath( this.GetType().Assembly.Location, name ) ) );

        this._logger.Trace?.Log( "System assemblies: " + string.Join( ", ", referencePaths ) );
        this._logger.Trace?.Log( "Metalama assemblies: " + string.Join( ", ", metalamaImplementationPaths ) );

        this.MetadataReferences =
            referencePaths
                .Concat( metalamaImplementationPaths )
                .SelectAsReadOnlyCollection( MetadataReferenceCache.GetMetadataReference )
                .Concat( embeddedAssemblies )
                .ToImmutableArray();

        var compilation = CSharpCompilation.Create( "ReferenceAssemblies", references: this.MetadataReferences );

        this.AssemblyIdentities = compilation.SourceModule.ReferencedAssemblySymbols
            .GroupBy( s => s.Identity.Name )
            .ToImmutableDictionary( s => s.Key, s => s.OrderByDescending( x => x.Identity.Version ).First().Identity );

        if ( !this.TryGetAdditionalCompileTimeAssembliesDirectory( diagnostics, out var additionalCompileTimeAssembliesDirectory ) )
        {
            return;
        }

        var additionalCompileTimeAssemblies = Directory.GetFiles( additionalCompileTimeAssembliesDirectory, "*.dll" );

        this.AdditionalCompileTimeAssemblyPaths =
            additionalCompileTimeAssemblies.Where( p => !p.EndsWith( "TempProject.dll", StringComparison.OrdinalIgnoreCase ) ).ToImmutableArray();

        this._referenceCompilation =
            CSharpCompilation.Create(
                nameof(CompileTimeAssemblyLocator),
                [],
                this.MetadataReferences,
                new CSharpCompilationOptions( OutputKind.DynamicallyLinkedLibrary, deterministic: true, optimizationLevel: OptimizationLevel.Debug ) );

        this._referenceCompilationContext = this._referenceCompilation.GetCompilationContext();

        success = true;
    }

    /// <summary>
    /// Splits the value of the <see cref="MSBuildPropertyNames.MetalamaCompileTimeTargetFrameworks"/> property into
    /// target framework monikers.
    /// </summary>
    /// <remarks>
    /// Both the comma and the semicolon are accepted. The property reaches the compiler through the generated analyzer
    /// configuration file, in which a semicolon starts a comment, so the build normalizes the semicolon that a user
    /// naturally writes in MSBuild into a comma. A value set directly through <see cref="IProjectOptions"/>, as in
    /// tests, does not go through that file and keeps whichever separator it was given. See issue #1789.
    /// </remarks>
    internal static ImmutableArray<string> ParseTargetFrameworks( string value )
        => value
            .Split( [',', ';'], StringSplitOptions.RemoveEmptyEntries )
            .SelectAsArray( f => f.Trim() )
            .Where( f => f.Length > 0 )
            .ToImmutableArray();

    /// <summary>
    /// Creates the exception reported when the <see cref="MSBuildPropertyNames.MetalamaCompileTimeTargetFrameworks"/>
    /// property does not describe a usable set of target frameworks.
    /// </summary>
    /// <remarks>
    /// A <see cref="DiagnosticException"/> and not an <see cref="InvalidOperationException"/>, because the value comes
    /// from the project and the user is the one who can correct it. Reported as <c>LAMA0001</c>, it invited a crash
    /// report for what is a configuration mistake. See issues #1744 and #1789.
    /// </remarks>
    private static void ReportInvalidTargetFrameworks( IDiagnosticAdder diagnostics, string value, string requirement )
        => diagnostics.Report(
            GeneralDiagnosticDescriptors.InvalidCompileTimeTargetFrameworks.CreateRoslynDiagnostic(
                null,
                (MSBuildPropertyNames.MetalamaCompileTimeTargetFrameworks, value, requirement) ) );

    private bool TryGetAdditionalCompileTimeAssembliesDirectory( IDiagnosticAdder diagnostics, [NotNullWhen( true )] out string? directory )
    {
        var targetFrameworks = string.Join( ";", this._targetFrameworks );

        var platform = Environment.Version.Major < 6
            ? this._targetFrameworks.FirstOrDefault( f => f.StartsWith( "net4", StringComparison.Ordinal ) )
            : this._targetFrameworks.FirstOrDefault( f => f is ['n', 'e', 't', '1' or (>= '6' and <= '9'), ..] );

        if ( platform == null )
        {
            var requirement = Environment.Version.Major < 6
                ? "it must include a .NET Framework 4.x target framework"
                : "it must include a .NET 6.0 or later target framework";

            ReportInvalidTargetFrameworks( diagnostics, targetFrameworks, requirement );
            directory = null;

            return false;
        }

        directory = Path.Combine( this._cacheDirectory, "bin", "Debug", platform );

        return true;
    }

    public static string GetAdditionalReferences( IProjectOptions options )
        => GetAdditionalAssemblyReferences( options )
           + GetAdditionalPackageReferences( options );

    private static string GetAdditionalAssemblyReferences( IProjectOptions options )
    {
        // We currently ignore the TargetFramework property of compile-time assemblies, which means that only .NET Standard 2.0
        // assemblies are supported here.

        return
            string.Join(
                Environment.NewLine,
                options.CompileTimeAssemblies.Select( a => $"\t\t<Reference Include=\"{Path.GetFullPath( a.Path ?? a.Name )}\"/>" ) );
    }

    private static string GetAdditionalPackageReferences( IProjectOptions options )
    {
        if ( options.CompileTimePackages.IsDefaultOrEmpty )
        {
            return "";
        }

        if ( string.IsNullOrEmpty( options.ProjectAssetsFile ) )
        {
            throw new InvalidOperationException( "The CompileTimePackages property is defined, but ProjectAssetsFile is not." );
        }

        if ( string.IsNullOrEmpty( options.TargetFrameworkMoniker ) && string.IsNullOrWhiteSpace( options.TargetFramework ) )
        {
            throw new InvalidOperationException(
                "The CompileTimePackages property is defined, but both TargetFramework and TargetFrameworkMoniker are undefined." );
        }

        var resolvedPackages = new Dictionary<string, string>();

        var assetsJson = JObject.Parse( File.ReadAllText( options.ProjectAssetsFile.AssertNotNull() ) );
        JToken? packages = null;

        if ( !string.IsNullOrEmpty( options.TargetFrameworkMoniker ) )
        {
            packages = assetsJson["targets"]?[options.TargetFrameworkMoniker];
        }

        if ( packages == null && !string.IsNullOrEmpty( options.TargetFramework ) )
        {
            packages = assetsJson["targets"]?[options.TargetFramework];
        }

        if ( packages == null )
        {
            throw new InvalidOperationException(
                $"'{options.ProjectAssetsFile}' does not contain targets for '{options.TargetFrameworkMoniker}' or '{options.TargetFramework}'." );
        }

        foreach ( var package in packages )
        {
            var nameVersion = ((JProperty) package).Name;
            var parts = nameVersion.Split( '/' );

            var packageName = parts[0];
            var packageVersion = parts[1];

            if ( options.CompileTimePackages.Contains( packageName ) )
            {
                resolvedPackages.Add( packageName, $"\t\t<PackageReference Include=\"{packageName}\" Version=\"{packageVersion}\"/>" );
            }
        }

        var missingPackages = options.CompileTimePackages.Where( x => !resolvedPackages.ContainsKey( x ) ).ToReadOnlyList();

        if ( missingPackages.Count > 0 )
        {
            throw new InvalidOperationException(
                $"No package was found for the following {MSBuildItemNames.MetalamaCompileTimePackage}: {string.Join( ", ", missingPackages )}" );
        }

        return string.Join( Environment.NewLine, resolvedPackages.OrderBy( x => x.Key ).Select( x => x.Value ) );
    }

    /// <summary>
    /// Determines if a symbol (typically one from the run-time compilation) exists in compile-time references.
    /// </summary>
    internal bool? IsSymbolAvailable( ISymbol symbol, CompilationContext compilation ) => this.TryGetAvailableSymbol( symbol, compilation, out _ );

    private bool? TryGetAvailableSymbol( ISymbol symbol, CompilationContext compilation, out ISymbol? availableSymbol )
    {
        symbol = symbol.OriginalDefinition;

        switch ( symbol.Kind )
        {
            case SymbolKind.Method when symbol is IMethodSymbol { ReducedFrom: { } reducedFrom }:
                return this.TryGetAvailableSymbol( reducedFrom, compilation, out availableSymbol );

            case SymbolKind.Method when symbol is IMethodSymbol { MethodKind: MethodKind.BuiltinOperator }:
                // For some reason, DocumentationId mapping does not work for operators.
                availableSymbol = null;

                return null;

            default:
                {
                    // DocumentationId seems to work.
                    var symbolId = DocumentationCommentId.CreateDeclarationId( symbol );

                    if ( symbolId == null )
                    {
                        availableSymbol = null;

                        return false;
                    }

                    var compileTimeSymbol = DocumentationCommentId.GetFirstSymbolForDeclarationId( symbolId, this._referenceCompilation );

                    // Filter out symbols that are not externally visible (e.g. internal types in reference assemblies).
                    // DocumentationCommentId matching can find internal types like System.SR that exist in BCL assemblies
                    // (System.Buffers, System.Collections.Immutable, etc.) but are not accessible to user code.
                    // Treating these as compile-time available causes false positives when user code defines a type with the same name.
                    if ( compileTimeSymbol != null && !IsExternallyAccessible( compileTimeSymbol ) )
                    {
                        compileTimeSymbol = null;
                    }

                    if ( compileTimeSymbol == null )
                    {
                        // We didn't find the exact symbol, but there could still be a more general overload.
                        // So do overload resolution based on the parameter types of the run-time overload.

                        if ( symbol.Kind is SymbolKind.Method or SymbolKind.Property
                             && symbol is (IMethodSymbol or IPropertySymbol { IsIndexer: true }) and { ContainingType: { } containingType } )
                        {
                            if ( this.TryGetAvailableSymbol( containingType, compilation, out var compileTimeContainingType ) != true )
                            {
                                availableSymbol = null;

                                return false;
                            }

                            var compileTimeMembers = ((INamedTypeSymbol) compileTimeContainingType!).GetMembersOfCompatibleSignature(
                                this._referenceCompilationContext,
                                symbol,
                                compilation );

                            compileTimeSymbol = compileTimeMembers.FirstOrDefault();
                        }
                    }

                    availableSymbol = compileTimeSymbol;

                    return availableSymbol != null;
                }
        }
    }

    /// <summary>
    /// Determines whether a symbol from a reference assembly is externally accessible (i.e., visible to code outside its assembly).
    /// This filters out internal types like System.SR that exist in BCL assemblies but are implementation details.
    /// </summary>
    private static bool IsExternallyAccessible( ISymbol symbol )
    {
        var current = symbol;

        while ( current != null )
        {
            switch ( current.DeclaredAccessibility )
            {
                case Accessibility.Public:
                case Accessibility.Protected:
                case Accessibility.ProtectedOrInternal:
                    // These are visible externally.
                    break;

                case Accessibility.Internal:
                case Accessibility.Private:
                case Accessibility.ProtectedAndInternal:
                case Accessibility.NotApplicable:
                    return false;

                default:
                    return false;
            }

            current = current.ContainingType;
        }

        return true;
    }

    /// <summary>
    /// Gets the command line of the nested reference-assembly build when it is run through <c>dotnet build</c>.
    /// </summary>
    /// <remarks>
    /// See <see cref="GetMSBuildToolArguments"/> for the reason why node reuse and multi-node builds are switched off.
    /// </remarks>
    internal static string GetDotNetToolArguments( string binaryLogFileName ) => $"build -nodeReuse:false -m:1 -bl:{binaryLogFileName}";

    /// <summary>
    /// Gets the command line of the nested reference-assembly build when it is run through <c>MSBuild.exe</c>, which is
    /// the case for old-style .NET Framework projects, for which the .NET SDK version is unknown.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This build is started from inside the compiler, which itself runs inside a task of the outer build. With its
    /// default settings, MSBuild would build with as many worker nodes as there are processors and leave those nodes
    /// alive for about fifteen minutes so that a later build can reuse them. Requesting worker nodes from within a
    /// build whose own nodes are all occupied, waiting for the compiler that started this build, is a re-entrancy
    /// hazard: whether a node can be acquired depends on how many of them happen to be free at that instant, which is
    /// what made the failure reported in issue #1740 intermittent and unrelated to any axis of the build matrix.
    /// </para>
    /// <para>
    /// A single-project build gains nothing from either parallelism or node reuse, so both are switched off. The
    /// nested build then runs entirely in the process that Metalama starts, which has the further benefit that
    /// terminating that process when it exceeds its time budget genuinely terminates the build, instead of leaving
    /// worker processes behind to finish it.
    /// </para>
    /// </remarks>
    internal static string GetMSBuildToolArguments( string projectFilePath, string binaryLogFileName )
        => $"\"{projectFilePath}\" /t:Restore;Build /nodeReuse:false /m:1 /bl:{binaryLogFileName}";

    private bool TryGetReferenceAssembliesManifest(
        string targetFrameworks,
        string additionalPackageReferences,
        string? additionalNugetSources,
        string? hooksDirectory,
        IDiagnosticAdder diagnostics,
        [NotNullWhen( true )] out IReadOnlyList<string>? referencePaths )
    {
        using ( this._lockService.WithGlobalLock( this._cacheDirectory ) )
        {
            var assembliesListPath = Path.Combine( this._cacheDirectory, "assemblies-netstandard2.0.txt" );

            // See if the file is present in cache.
            if ( File.Exists( assembliesListPath ) )
            {
                this._logger.Trace?.Log( $"Reading '{assembliesListPath}'." );

                var assembliesFromFile = File.ReadAllLines( assembliesListPath );

                var missingFiles = assembliesFromFile.Where( f => !File.Exists( f ) ).ToReadOnlyList();

                if ( missingFiles.Count == 0 )
                {
                    if ( !this.TryGetAdditionalCompileTimeAssembliesDirectory( diagnostics, out var additionalCompileTimeAssembliesDirectory ) )
                    {
                        referencePaths = null;

                        return false;
                    }

                    if ( Directory.Exists( additionalCompileTimeAssembliesDirectory ) )
                    {
                        referencePaths = assembliesFromFile;

                        return true;
                    }
                    else
                    {
                        this._logger.Warning?.Log(
                            $"The following directory did no longer exist so the reference project has to be rebuilt: {additionalCompileTimeAssembliesDirectory}." );
                    }
                }
                else
                {
                    this._logger.Warning?.Log(
                        $"The following file(s) did no longer exist so the reference project has to be rebuilt: {string.Join( ",", missingFiles )}." );
                }
            }

            Directory.CreateDirectory( this._cacheDirectory );

            GlobalJsonHelper.WriteCurrentVersion( this._cacheDirectory, this._sdkVersion );

            var initialTargets = "";
            var hooksPropsImport = "";
            var hooksTargetsImport = "";
            var hooksImportWarnings = "";

            if ( hooksDirectory != null )
            {
                hooksDirectory = hooksDirectory.Replace( '\\', '/' ).Trim().TrimEnd( '/' );

                if ( !Path.IsPathRooted( hooksDirectory ) )
                {
                    hooksDirectory = $"$(MSBuildThisFileDirectory){hooksDirectory}";
                }

                initialTargets = " InitialTargets=\"_WarnOfImports\"";

                hooksPropsImport = $@"
  <Import Project=""{hooksDirectory}/Metalama.AssemblyLocator.Build.props"" Condition=""Exists('{hooksDirectory}/Metalama.AssemblyLocator.Build.props')"" />";

                hooksTargetsImport = $@"
  <Import Project=""{hooksDirectory}/Metalama.AssemblyLocator.Build.targets"" Condition=""Exists('{hooksDirectory}/Metalama.AssemblyLocator.Build.targets')"" />";

                hooksImportWarnings = $@"
  <Target Name=""_WarnOfImports"">
    <Warning Text=""'{hooksDirectory}/Metalama.AssemblyLocator.Build.props' imported."" Condition=""Exists('{hooksDirectory}/Metalama.AssemblyLocator.Build.props')"" />
    <Warning Text=""'{hooksDirectory}/Metalama.AssemblyLocator.Build.targets' imported."" Condition=""Exists('{hooksDirectory}/Metalama.AssemblyLocator.Build.targets')"" />
  </Target>";
            }

            // We don't add a reference to Microsoft.CSharp because this package is used to support dynamic code, and we don't want
            // dynamic code at compile time. We prefer compilation errors.

            var projectText =
                $"""
                 <Project{initialTargets}>
                   <PropertyGroup>
                     <ImportDirectoryPackagesProps>false</ImportDirectoryPackagesProps>
                     <ImportDirectoryBuildProps>false</ImportDirectoryBuildProps>
                     <ImportDirectoryBuildTargets>false</ImportDirectoryBuildTargets>
                   </PropertyGroup>{hooksPropsImport}
                   <Import Project="Sdk.props" Sdk="Microsoft.NET.Sdk" />
                   <PropertyGroup>
                     <TargetFrameworks>{targetFrameworks}</TargetFrameworks>
                     <OutputType>Exe</OutputType>
                     <LangVersion>latest</LangVersion>
                     <RestoreAdditionalProjectSources>{additionalNugetSources}</RestoreAdditionalProjectSources>
                     <RestoreIgnoreFailedSources>true</RestoreIgnoreFailedSources>
                   </PropertyGroup>
                   <ItemGroup>
                     <PackageReference Include="Microsoft.CodeAnalysis.CSharp" Version="{RoslynApiVersion.Current.ToNuGetVersionString()}" />
                     {additionalPackageReferences}
                   </ItemGroup>
                   <Target Name="WriteAssembliesList" AfterTargets="Build" Condition="'$(TargetFramework)'!=''">
                     <WriteLinesToFile File="assemblies-$(TargetFramework).txt" Overwrite="true" Lines="@(ReferencePathWithRefAssemblies)" />
                   </Target>{hooksImportWarnings}
                   <Import Project="Sdk.targets" Sdk="Microsoft.NET.Sdk" />{hooksTargetsImport}
                 </Project>
                 """;

            var projectFilePath = Path.Combine( this._cacheDirectory, "TempProject.csproj" );
            this._logger.Trace?.Log( $"Writing '{projectFilePath}':" + Environment.NewLine + projectText );

            File.WriteAllText( projectFilePath, projectText );

            // Writing a dummy program.
            var programFilePath = Path.Combine( this._cacheDirectory, "Program.cs" );
            this._logger.Trace?.Log( $"Writing '{programFilePath}'." );

            File.WriteAllText( programFilePath, "System.Console.WriteLine(\"Hello, world.\");" );

            // Writing nuget.config. A file is written even when none was discovered, when there is a prerelease package
            // source to declare, because that source is then the whole content of the file. See #1885.
            if ( this._nugetConfigFiles is { Count: > 0 } || this._prereleasePackageSourceUrl != null )
            {
                var discoveredConfigFiles = this._nugetConfigFiles ?? Array.Empty<string>();

                var nugetConfigDocument = this._nuGetHelper.MergeConfigFiles( discoveredConfigFiles )
                                          ?? new XDocument( new XElement( "configuration" ) );

                var log = new List<string>( discoveredConfigFiles );

                if ( this._prereleasePackageSourceUrl != null )
                {
                    log.AddRange( this._userNuGetConfigFiles );

                    var addPackageSourceResult = this._nuGetHelper.AddPackageSource(
                        nugetConfigDocument,
                        SupportedCSharpVersions.RoslynPrereleaseSourceKey,
                        this._prereleasePackageSourceUrl,
                        SupportedCSharpVersions.RoslynPackagePattern,
                        this._userNuGetConfigFiles );

                    log.Add(
                        $"Added the package source '{SupportedCSharpVersions.RoslynPrereleaseSourceKey}' at '{this._prereleasePackageSourceUrl}'." );

                    if ( addPackageSourceResult.IsMappingWritten )
                    {
                        log.Add( $"Mapped the pattern '{SupportedCSharpVersions.RoslynPackagePattern}' to that source." );
                    }
                    else if ( addPackageSourceResult.ConflictingPattern != null )
                    {
                        this._unmappedPrereleasePackageSourceUrl = this._prereleasePackageSourceUrl;

                        log.Add(
                            $"Did not map the pattern '{SupportedCSharpVersions.RoslynPackagePattern}' to that source, because the package source "
                            + $"'{addPackageSourceResult.ConflictingSourceKey}' already declares the pattern '{addPackageSourceResult.ConflictingPattern}'." );
                    }
                    else
                    {
                        log.Add(
                            $"Did not map the pattern '{SupportedCSharpVersions.RoslynPackagePattern}' to that source, because the configuration "
                            + "declares no packageSourceMapping section." );
                    }
                }

                var nuGetConfigPath = Path.Combine( this._cacheDirectory, "nuget.config" );
                this._logger.Trace?.Log( $"Writing '{nuGetConfigPath}'." );
                this._fileSystem.WriteAllText( nuGetConfigPath, nugetConfigDocument.ToString() );
                this._fileSystem.WriteAllText( nuGetConfigPath + ".log", string.Join( Environment.NewLine, log ) );
            }

            this._logger.Trace?.Log( $"Building with restore timeout {this._restoreTimeout}." );

            // The binary log is written in the working directory, i.e. in the cache directory. Its name is computed here
            // and not inline in the arguments so that the diagnostic reported on a failure can name its full path, which
            // is the only artifact from which such a failure can be diagnosed after the fact. See #1740 and #1746.
            var binaryLogFileName = $"msbuild_{Guid.NewGuid():N}.binlog";
            var binaryLogPath = Path.Combine( this._cacheDirectory, binaryLogFileName );

            try
            {
                // When NETCoreSdkVersion is not available (e.g., old-style .NET Framework projects built with msbuild.exe),
                // use msbuild.exe directly instead of dotnet.exe.
                if ( string.IsNullOrEmpty( this._sdkVersion ) && !string.IsNullOrEmpty( this._msBuildBinPath ) )
                {
                    var msBuildTool = new MSBuildTool( this._msBuildBinPath );

                    msBuildTool.Execute( GetMSBuildToolArguments( projectFilePath, binaryLogFileName ), this._cacheDirectory, this._restoreTimeout );
                }
                else
                {
                    // Remove configuration environment variable to avoid having different output directory than Debug.
                    // Build scripts may rely on env var to set the configuration in MSBuild.
                    // Case insensitive comparison needed because MSBuild is case insensitive.
                    this._dotNetTool.Execute(
                        GetDotNetToolArguments( binaryLogFileName ),
                        this._cacheDirectory,
                        this._restoreTimeout,
                        envVar => !StringComparer.OrdinalIgnoreCase.Equals( envVar.Key, "configuration" ) );
                }
            }
            catch ( ProcessFailedException exception )
            {
                this.CopyBinaryLog( binaryLogPath );
                this.ReportReferenceAssemblyBuildFailure( exception, projectFilePath, binaryLogPath, diagnostics );
                referencePaths = null;

                return false;
            }

            var assemblies = File.ReadAllLines( assembliesListPath );

            if ( assemblies.Length == 0 )
            {
                throw new AssertionFailedException( $"The file '{assembliesListPath}' is empty." );
            }

            referencePaths = assemblies;

            return true;
        }
    }

    /// <summary>
    /// Copies the binary log of the nested reference-assembly build into the directory named by
    /// <see cref="IProjectOptions.AssemblyLocatorBinaryLogDirectory"/>, when that option is set.
    /// </summary>
    /// <remarks>
    /// The nested build writes its binary log into the cache directory, which is under the temporary directory of the
    /// machine. A continuous integration agent that runs the build in a container discards that directory together with
    /// the container, so the log of a failure cannot be collected afterwards. A repository that needs to publish the log
    /// sets the option to a directory that outlives the build, such as its artifacts directory.
    /// </remarks>
    private void CopyBinaryLog( string binaryLogPath )
    {
        if ( string.IsNullOrEmpty( this._binaryLogDirectory ) )
        {
            return;
        }

        try
        {
            if ( !File.Exists( binaryLogPath ) )
            {
                this._logger.Warning?.Log( $"The binary log '{binaryLogPath}' does not exist, so it was not copied." );

                return;
            }

            Directory.CreateDirectory( this._binaryLogDirectory! );

            var destination = Path.Combine( this._binaryLogDirectory!, Path.GetFileName( binaryLogPath ) );
            File.Copy( binaryLogPath, destination, true );

            this._logger.Trace?.Log( $"The binary log was copied to '{destination}'." );
        }
        catch ( Exception e )
        {
            // Copying the log assists a diagnosis and is not required for one. A failure to copy it must not replace
            // the diagnostic that describes the failure of the nested build itself.
            this._logger.Warning?.Log( $"Cannot copy the binary log to '{this._binaryLogDirectory}': {e.Message}" );
        }
    }

    /// <summary>
    /// Converts the failure of the nested reference-assembly build into a <see cref="DiagnosticException"/>, so that it
    /// is reported to the user as an actionable diagnostic instead of an unexpected exception and a crash report.
    /// </summary>
    /// <remarks>
    /// The complete output of the child process is written to the Metalama log, because the diagnostic can quote only a
    /// few of its lines: a Roslyn diagnostic cannot contain line breaks, and a console transcript embedded in a build
    /// error is unreadable. See issue #1744.
    /// </remarks>
    private void ReportReferenceAssemblyBuildFailure(
        ProcessFailedException exception,
        string projectFilePath,
        string binaryLogPath,
        IDiagnosticAdder diagnostics )
    {
        this._logger.Error?.Log( exception.Message );

        var diagnostic =
            exception.HasTimedOut
                ? GeneralDiagnosticDescriptors.ReferenceAssemblyBuildTimedOut.CreateRoslynDiagnostic(
                    null,
                    (projectFilePath, exception.Timeout / 1000f, MSBuildPropertyNames.MetalamaReferenceAssemblyRestoreTimeout, binaryLogPath) )
                : GeneralDiagnosticDescriptors.ReferenceAssemblyBuildFailed.CreateRoslynDiagnostic(
                    null,
                    (projectFilePath, exception.ExitCode!.Value, ReferenceAssemblyBuildFailureClassifier.GetReportedErrors( exception.Output ),
                     ReferenceAssemblyBuildFailureClassifier.GetProbableCause(
                         exception.Output,
                         this._cacheDirectory,
                         this._sdkVersion,
                         this._unmappedPrereleasePackageSourceUrl), binaryLogPath) );

        diagnostics.Report( diagnostic );
    }
}