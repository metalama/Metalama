// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Diagnostics;
using Metalama.Backstage.Infrastructure;
using Metalama.Backstage.Maintenance;
using Metalama.Backstage.Utilities;
using Metalama.Compiler;
using Metalama.Framework.Aspects;
using Metalama.Framework.CompileTimeContracts;
using Metalama.Framework.Engine.AspectWeavers;
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
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
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
    private const string _defaultCompileTimeTargetFrameworks = "netstandard2.0;net6.0;net48";
    private static readonly ImmutableArray<string> _defaultNugetSources = GetDefaultNuGetSources().ToImmutableArray();

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

    private readonly string _cacheDirectory;
    private readonly ILogger _logger;
    private readonly IPlatformInfo _platformInfo;
    private readonly DotNetTool _dotNetTool;
    private readonly int _restoreTimeout;
    private readonly ImmutableArray<string> _targetFrameworks;

    /// <summary>
    /// This compilation is used by the <see cref="SymbolClassifier"/> to determine if an API is available
    /// at compile time.
    /// </summary>
    private readonly Compilation _referenceCompilation;

    private readonly CompilationContext _referenceCompilationContext;

    /// <summary>
    /// Gets the name (without path and extension) of all compile-time assemblies, including Metalama, Roslyn and .NET standard.
    /// </summary>
    internal ImmutableHashSet<string> AssemblyNames { get; }

    /// <summary>
    /// Gets the full path of executable system assemblies for the current platform.
    /// </summary>
    internal ImmutableArray<string> AdditionalCompileTimeAssemblyPaths { get; }

    internal ImmutableDictionary<string, AssemblyIdentity> AssemblyIdentities { get; }

    internal bool IsStandardAssemblyName( string assemblyName )
        => string.Equals( assemblyName, "System.Private.CoreLib", StringComparison.OrdinalIgnoreCase )
           || this.AssemblyNames.Contains( assemblyName );

    /// <summary>
    /// Gets the full path of all standard assemblies, including Metalama, Roslyn and .NET standard.
    /// </summary>
    internal ImmutableArray<MetadataReference> MetadataReferences { get; }

    internal CompileTimeAssemblyLocator( in ProjectServiceProvider serviceProvider, string additionalReferences, ITempFileManager tempFileManager )
    {
        this._logger = serviceProvider.GetLoggerFactory().GetLogger( nameof(CompileTimeAssemblyLocator) );

        this._platformInfo = serviceProvider.Global.GetRequiredBackstageService<IPlatformInfo>();
        this._dotNetTool = new DotNetTool( serviceProvider.Global );

        var projectOptions = serviceProvider.GetRequiredService<IProjectOptions>();

        this._restoreTimeout = projectOptions.ReferenceAssemblyRestoreTimeout ?? 120_000;

        this._logger.Trace?.Log(
            "Assembly versions: " + string.Join(
                ", ",
                new[] { this.GetType(), typeof(IAspect), typeof(IAspectWeaver), typeof(ITemplateSyntaxFactory), typeof(FieldOrPropertyInfo) }
                    .SelectAsReadOnlyList( x => x.Assembly.Location ) ) );

        var targetFrameworksString = string.IsNullOrEmpty( projectOptions.CompileTimeTargetFrameworks )
            ? _defaultCompileTimeTargetFrameworks
            : projectOptions.CompileTimeTargetFrameworks;

        this._targetFrameworks = targetFrameworksString.Split( ';' ).ToImmutableArray();

        if ( !this._targetFrameworks.Contains( "netstandard2.0" ) )
        {
            throw new InvalidOperationException(
                $"Custom MetalamaCompileTimeTargetFrameworks has to include 'netstandard2.0', but it was {this._targetFrameworks}" );
        }

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

        // ReSharper disable once RedundantLogicalConditionalExpressionOperand
        var projectHash =
            HashUtilities.HashString( $"{additionalReferences}\n{targetFrameworksString}\n{additionalNugetSources}\n{RoslynApiVersion.Current}" );

        this._cacheDirectory = tempFileManager.GetTempDirectory( TempDirectories.AssemblyLocator, CleanUpStrategy.WhenUnused, projectHash );

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
        var referencePaths = this.GetReferenceAssembliesManifest(
            targetFrameworksString,
            additionalReferences,
            additionalNugetSources,
            projectOptions.AssemblyLocatorHooksDirectory );

        // Sets the collection of all standard assemblies, i.e. system assemblies and ours.
        this.AssemblyNames = metalamaImplementationAssemblyNames
            .Concat( [_compileTimeFrameworkAssemblyName, _compilerInterfaceAssemblyName] )
            .Concat( referencePaths.SelectAsReadOnlyList( x => Path.GetFileNameWithoutExtension( x ).AssertNotNull() ) )
            .ToImmutableHashSet( StringComparer.OrdinalIgnoreCase );

        // Also provide our embedded assemblies.

        var embeddedAssemblies =
            new[] { _compileTimeFrameworkAssemblyName, _compilerInterfaceAssemblyName, "Metalama.SystemTypes" }.SelectAsImmutableArray(
                name => (MetadataReference)
                    MetadataReference.CreateFromStream(
                        this.GetType().Assembly.GetManifestResourceStream( name + ".dll" )
                        ?? throw new InvalidOperationException( $"{name}.dll not found in assembly manifest resources." ),
                        filePath: $"[{this.GetType().Assembly.Location}]{name}.dll" ) );

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

        var additionalCompileTimeAssemblies = Directory.GetFiles( this.GetAdditionalCompileTimeAssembliesDirectory(), "*.dll" );

        this.AdditionalCompileTimeAssemblyPaths =
            additionalCompileTimeAssemblies.Where( p => !p.EndsWith( "TempProject.dll", StringComparison.OrdinalIgnoreCase ) ).ToImmutableArray();

        this._referenceCompilation =
            CSharpCompilation.Create(
                nameof(CompileTimeAssemblyLocator),
                [],
                this.MetadataReferences,
                new CSharpCompilationOptions( OutputKind.DynamicallyLinkedLibrary, deterministic: true, optimizationLevel: OptimizationLevel.Debug ) );

        this._referenceCompilationContext = CompilationContextFactory.GetCompilationContext( this._referenceCompilation );
    }

    private string GetAdditionalCompileTimeAssembliesDirectory()
    {
        string platform;

        if ( Environment.Version.Major < 6 )
        {
            platform = this._targetFrameworks.FirstOrDefault( f => f.StartsWith( "net4", StringComparison.Ordinal ) )
                       ?? throw new InvalidOperationException( "Custom MetalamaCompileTimeTargetFrameworks did not include .NET Framework 4.x." );
        }
        else
        {
            platform = this._targetFrameworks.FirstOrDefault( f => f is ['n', 'e', 't', >= '6' and <= '9', ..] )
                       ?? throw new InvalidOperationException( "Custom MetalamaCompileTimeTargetFrameworks did not include .NET 6+." );
        }

        return Path.Combine( this._cacheDirectory, "bin", "Debug", platform );
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
                options.CompileTimeAssemblies.Select( a => $"\t\t<Reference Include=\"{Path.GetFullPath( a.Path )}\"/>" ) );
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

        switch ( symbol )
        {
            case IMethodSymbol { ReducedFrom: { } reducedFrom }:
                return this.TryGetAvailableSymbol( reducedFrom, compilation, out availableSymbol );

            case IMethodSymbol { MethodKind: MethodKind.BuiltinOperator }:
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

                    if ( compileTimeSymbol == null )
                    {
                        // We didn't find the exact symbol, but there could still be a more general overload.
                        // So do overload resolution based on the parameter types of the run-time overload.

                        if ( symbol is (IMethodSymbol or IPropertySymbol { IsIndexer: true }) and { ContainingType: { } containingType } )
                        {
                            if ( this.TryGetAvailableSymbol( containingType, compilation, out var compileTimeContainingType ) != true )
                            {
                                availableSymbol = null;
                                return false;
                            }

                            var compileTimeMembers = SymbolSignatureMatcher.GetMembersOfCompatibleSignature( (INamedTypeSymbol) compileTimeContainingType!, this._referenceCompilationContext, symbol, compilation );

                            compileTimeSymbol = compileTimeMembers.FirstOrDefault();
                        }
                    }

                    availableSymbol = compileTimeSymbol;
                    return availableSymbol != null;
                }
        }
    }

    private IReadOnlyList<string> GetReferenceAssembliesManifest(
        string targetFrameworks,
        string additionalPackageReferences,
        string? additionalNugetSources,
        string? hooksDirectory )
    {
        using ( MutexHelper.WithGlobalLock( this._cacheDirectory, this._logger ) )
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
                    var additionalCompileTimeAssembliesDirectory = this.GetAdditionalCompileTimeAssembliesDirectory();

                    if ( Directory.Exists( additionalCompileTimeAssembliesDirectory ) )
                    {
                        return assembliesFromFile;
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

            GlobalJsonHelper.WriteCurrentVersion( this._cacheDirectory, this._platformInfo );

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
  <Import Project=""{{hooksDirectory}}/Metalama.AssemblyLocator.Build.targets"" Condition=""Exists('{{hooksDirectory}}/Metalama.AssemblyLocator.Build.targets')"" />";

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

            var programFilePath = Path.Combine( this._cacheDirectory, "Program.cs" );
            this._logger.Trace?.Log( $"Writing '{programFilePath}'." );

            File.WriteAllText( programFilePath, "System.Console.WriteLine(\"Hello, world.\");" );

            // We may consider executing msbuild.exe instead of dotnet.exe when the build itself runs using msbuild.exe.
            // This way we wouldn't need to require a .NET SDK to be installed. Also, it seems that Rider requires the full path.
            var arguments = $"build -bl:msbuild_{Guid.NewGuid():N}.binlog";

            this._logger.Trace?.Log( $"Building with restore timeout {this._restoreTimeout}." );

            // Remove configuration environment variable to avoid having different output directory than Debug.
            // Build scripts may rely on env var to set the configuration in MSBuild.
            // Case insensitive comparison needed because MSBuild is case insensitive.
            this._dotNetTool.Execute(
                arguments,
                this._cacheDirectory,
                this._restoreTimeout,
                envVar => !StringComparer.OrdinalIgnoreCase.Equals( envVar.Key, "configuration" ) );

            var assemblies = File.ReadAllLines( assembliesListPath );

            if ( assemblies.Length == 0 )
            {
                throw new AssertionFailedException( $"The file '{assembliesListPath}' is empty." );
            }

            return assemblies;
        }
    }
}