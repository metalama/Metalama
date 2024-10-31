// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace Metalama.Framework.Engine.CompileTime;

/// <summary>
/// Provides the location to the reference assemblies that are needed to create the compile-time projects.
/// This is achieved by creating an MSBuild project and restoring it.
/// </summary>
internal sealed class ReferenceAssemblyLocator
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
    private readonly ReferenceAssembliesManifest _referenceAssembliesManifest;
    private readonly IPlatformInfo _platformInfo;
    private readonly DotNetTool _dotNetTool;
    private readonly int _restoreTimeout;
    private readonly ImmutableArray<string> _targetFrameworks;

    /// <summary>
    /// Gets the name (without path and extension) of all standard assemblies, including Metalama, Roslyn and .NET standard.
    /// </summary>
    internal ImmutableHashSet<string> StandardAssemblyNames { get; }

    /// <summary>
    /// Gets the full path of reference system assemblies (.NET Standard and Roslyn). 
    /// </summary>
    private ImmutableArray<string> SystemReferenceAssemblyPaths { get; }

    /// <summary>
    /// Gets the full path of executable system assemblies for the current platform.
    /// </summary>
    internal ImmutableArray<string> AdditionalCompileTimeAssemblyPaths { get; }

    internal ImmutableDictionary<string, AssemblyIdentity> StandardAssemblyIdentities { get; }

    internal bool IsStandardAssemblyName( string assemblyName )
        => string.Equals( assemblyName, "System.Private.CoreLib", StringComparison.OrdinalIgnoreCase )
           || this.StandardAssemblyNames.Contains( assemblyName );

    /// <summary>
    /// Gets the full path of all standard assemblies, including Metalama, Roslyn and .NET standard.
    /// </summary>
    internal ImmutableArray<MetadataReference> StandardCompileTimeMetadataReferences { get; }

    internal ReferenceAssemblyLocator( in ProjectServiceProvider serviceProvider, string additionalPackageReferences, ITempFileManager tempFileManager )
    {
        this._logger = serviceProvider.GetLoggerFactory().GetLogger( nameof(ReferenceAssemblyLocator) );

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
            additionalPackageReferences is "" && targetFrameworksString is _defaultCompileTimeTargetFrameworks && additionalNugetSources is null
            && RoslynApiVersion.Current == RoslynApiVersion.Highest
                ? "default"
                : HashUtilities.HashString( $"{additionalPackageReferences}\n{targetFrameworksString}\n{additionalNugetSources}\n{RoslynApiVersion.Current}" );

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
        this._referenceAssembliesManifest = this.GetReferenceAssembliesManifest(
            targetFrameworksString,
            additionalPackageReferences,
            additionalNugetSources,
            projectOptions.AssemblyLocatorHooksDirectory );

        this.SystemReferenceAssemblyPaths = this._referenceAssembliesManifest.ReferenceAssemblies;

        // Sets the collection of all standard assemblies, i.e. system assemblies and ours.
        this.StandardAssemblyNames = metalamaImplementationAssemblyNames
            .Concat( [_compileTimeFrameworkAssemblyName, _compilerInterfaceAssemblyName] )
            .Concat( this.SystemReferenceAssemblyPaths.Select( x => Path.GetFileNameWithoutExtension( x ).AssertNotNull() ) )
            .ToImmutableHashSet( StringComparer.OrdinalIgnoreCase );

        // Also provide our embedded assemblies.

        var embeddedAssemblies =
            new[] { _compileTimeFrameworkAssemblyName, _compilerInterfaceAssemblyName, "Metalama.SystemTypes" }.SelectAsImmutableArray(
                name => (MetadataReference)
                    MetadataReference.CreateFromStream(
                        this.GetType().Assembly.GetManifestResourceStream( name + ".dll" )
                        ?? throw new InvalidOperationException( $"{name}.dll not found in assembly manifest resources." ),
                        filePath: $"[{this.GetType().Assembly.Location}]{name}.dll" ) );

        this._logger.Trace?.Log( "System assemblies: " + string.Join( ", ", this.SystemReferenceAssemblyPaths ) );
        this._logger.Trace?.Log( "Metalama assemblies: " + string.Join( ", ", metalamaImplementationPaths ) );

        this.StandardCompileTimeMetadataReferences =
            this.SystemReferenceAssemblyPaths
                .Concat( metalamaImplementationPaths )
                .Select( MetadataReferenceCache.GetMetadataReference )
                .Concat( embeddedAssemblies )
                .ToImmutableArray();

        var compilation = CSharpCompilation.Create( "ReferenceAssemblies", references: this.StandardCompileTimeMetadataReferences );

        this.StandardAssemblyIdentities = compilation.SourceModule.ReferencedAssemblySymbols
            .GroupBy( s => s.Identity.Name )
            .ToImmutableDictionary( s => s.Key, s => s.OrderByDescending( x => x.Identity.Version ).First().Identity );

        var additionalCompileTimeAssemblies = Directory.GetFiles( this.GetAdditionalCompileTimeAssembliesDirectory(), "*.dll" );

        this.AdditionalCompileTimeAssemblyPaths =
            additionalCompileTimeAssemblies.Where( p => !p.EndsWith( "TempProject.dll", StringComparison.OrdinalIgnoreCase ) ).ToImmutableArray();
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

    internal static string GetAdditionalPackageReferences( IProjectOptions options )
    {
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

    internal bool IsSystemType( INamedTypeSymbol namedType )
    {
        var ns = namedType.ContainingNamespace.IsGlobalNamespace ? "" : namedType.ContainingNamespace.GetFullName().AssertNotNull();

        return this._referenceAssembliesManifest.Types.TryGetValue( ns, out var types ) && types.Contains( namedType.MetadataName );
    }

    private ReferenceAssembliesManifest GetReferenceAssembliesManifest(
        string targetFrameworks,
        string additionalPackageReferences,
        string? additionalNugetSources,
        string? hooksDirectory )
    {
        using ( MutexHelper.WithGlobalLock( this._cacheDirectory, this._logger ) )
        {
            var referencesJsonPath = Path.Combine( this._cacheDirectory, "references.json" );
            var assembliesListPath = Path.Combine( this._cacheDirectory, "assemblies-netstandard2.0.txt" );

            // See if the file is present in cache.
            if ( File.Exists( referencesJsonPath ) )
            {
                this._logger.Trace?.Log( $"Reading '{referencesJsonPath}'." );

                var referencesJson = File.ReadAllText( referencesJsonPath );

                var manifest = JsonConvert.DeserializeObject<ReferenceAssembliesManifest>( referencesJson ).AssertNotNull();

                var missingFiles = manifest.ReferenceAssemblies.Where( f => !File.Exists( f ) ).ToReadOnlyList();

                if ( missingFiles.Count == 0 )
                {
                    var additionalCompileTimeAssembliesDirectory = this.GetAdditionalCompileTimeAssembliesDirectory();

                    if ( Directory.Exists( additionalCompileTimeAssembliesDirectory ) )
                    {
                        return manifest;
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
                   </PropertyGroup>
                   <ItemGroup>
                     <PackageReference Include="Microsoft.CodeAnalysis.CSharp" Version="{RoslynApiVersion.Current.ToNuGetVersionString()}" />
                     <PackageReference Include="Metalama.Framework.RunTime" Version="{AssemblyMetadataReader.GetInstance( typeof(ReferenceAssemblyLocator).Assembly ).GetPackageVersion( "Metalama.Framework.RunTime" )}" />
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

            // Build the list of exported files.
            List<MetadataInfo> assemblyMetadatas = new();

            foreach ( var assemblyPath in assemblies )
            {
                if ( !MetadataReader.TryGetMetadata( assemblyPath, out var metadataInfo ) )
                {
                    throw new InvalidOperationException( $"Cannot read '{assemblyPath}'." );
                }

                assemblyMetadatas.Add( metadataInfo );
            }

            var exportedTypes = assemblyMetadatas
                .SelectMany( m => m.ExportedTypes )
                .GroupBy( ns => ns.Key )
                .ToImmutableDictionary( ns => ns.Key, ns => ns.SelectMany( n => n.Value ).Distinct( StringComparer.Ordinal ).ToImmutableHashSet() );

            // Done.
            var result = new ReferenceAssembliesManifest( assemblies.ToImmutableArray(), exportedTypes );

            this._logger.Trace?.Log( $"Writing '{referencesJsonPath}'." );

            File.WriteAllText( referencesJsonPath, JsonConvert.SerializeObject( result, Newtonsoft.Json.Formatting.Indented ) );

            return result;
        }
    }
}