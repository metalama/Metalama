// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.DesignTime.Contracts.EntryPoint;
using Metalama.Framework.DesignTime.Contracts.Workspaces;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace Metalama.DesignTime.Hosting.Tests;

/// <summary>
/// Hosts Metalama in the in-process Visual Studio mode, with one <see cref="AdhocWorkspace"/> that contains one project, and
/// gets the Metalama services the way the Metalama extension of Visual Studio gets them.
/// </summary>
/// <remarks>
/// <para>
/// Metalama accepts one workspace per process, so the tests of this assembly share this fixture, and a test that changes the
/// workspace must not change what another test observes.
/// </para>
/// <para>
/// The sequence is the one that a test of the Visual Studio extension follows: enable the mode, create the workspace with
/// the analyzer references of Metalama, create the Metalama source generators, get the service provider from
/// <see cref="DesignTimeEntryPointManager.Instance"/>, give it the workspace through <see cref="IWorkspaceReceiver"/>, and
/// wait for the analysis.
/// </para>
/// <para>
/// The aspect and the code to which it applies are in two documents. A change to a document that contains compile-time
/// code pauses the design-time pipeline until the user resumes it, as in Visual Studio, so a test that changes the
/// workspace must change a document that contains run-time code only.
/// </para>
/// </remarks>
public sealed class VisualStudioInProcessFixture : IAsyncLifetime
{
    public const string ProjectName = "AspectProject";

    public const string AspectCode = """
                                     using System;
                                     using Metalama.Framework.Aspects;

                                     public class LogAttribute : OverrideMethodAspect
                                     {
                                         public override dynamic? OverrideMethod()
                                         {
                                             Console.WriteLine( "Hello" );

                                             return meta.Proceed();
                                         }
                                     }
                                     """;

    public const string Code = """
                               public class C
                               {
                                   [Log]
                                   public void M() { }
                               }
                               """;

    private ICompilerServiceProvider? _serviceProvider;

    public AdhocWorkspace Workspace { get; } = new();

    public ProjectId ProjectId { get; } = ProjectId.CreateNewId();

    public DocumentId DocumentId { get; private set; } = null!;

    public ICompilerServiceProvider ServiceProvider => this._serviceProvider ?? throw new InvalidOperationException();

    public Project Project => this.Workspace.CurrentSolution.GetProject( this.ProjectId )!;

    public async ValueTask InitializeAsync()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        // Metalama limits the length of the paths under METALAMA_TEMP, so the directory name is kept short.
        var rootDirectory = Path.Combine( Path.GetTempPath(), "mdth", Guid.NewGuid().ToString( "N" ).Substring( 0, 8 ) );

        // This must happen before any Metalama assembly is loaded.
        VisualStudioInProcessHost.Enable( Path.Combine( rootDirectory, "t" ) );

        this.AddProject( Path.Combine( rootDirectory, ProjectName ) );

        // Creating the source generator of Metalama.Framework.CompilerExtensions.dll registers the Visual Studio services.
        var generatorCount = await VisualStudioInProcessHost.CreateGeneratorsAsync( this.Project );
        Assert.Equal( 1, generatorCount );

        // This is how the extension gets the provider of the Metalama version that a project references.
        var version = typeof(DesignTimeEntryPointManager).Assembly.GetName().Version!;
        var consumer = DesignTimeEntryPointManager.Instance.GetConsumer( CurrentContractVersions.All );
        this._serviceProvider = await consumer.GetServiceProviderAsync( version, cancellationToken );
        Assert.NotNull( this._serviceProvider );

        // ReSharper disable once SuspiciousTypeConversion.Global
        var workspaceReceiver = (IWorkspaceReceiver?) this._serviceProvider.GetService( typeof(IWorkspaceReceiver) );
        Assert.NotNull( workspaceReceiver );
        workspaceReceiver.SetWorkspace( this.Workspace );

        await VisualStudioInProcessHost.WaitForAnalysisAsync( cancellationToken );
    }

    private void AddProject( string projectDirectory )
    {
        var projectPath = Path.Combine( projectDirectory, ProjectName + ".csproj" );

        var loader = HostAnalyzerAssemblyLoader.Create();
        var analyzerPaths = new[] { GetMetadata( "CompilerInterfacePath" ), GetMetadata( "CompilerExtensionsPath" ) };

        foreach ( var path in analyzerPaths )
        {
            loader.AddDependencyLocation( path );
        }

        var analyzerReferences = analyzerPaths.Select( p => (AnalyzerReference) new AnalyzerFileReference( p, loader ) ).ToArray();

        // The properties that Metalama.Framework.targets makes visible to the compiler. NETCoreSdkVersion is required: the
        // compile-time language version is derived from it.
        var globalConfig = $"""
                            is_global = true
                            build_property.MetalamaEnabled = true
                            build_property.AssemblyName = {ProjectName}
                            build_property.TargetFramework = netstandard2.0
                            build_property.MSBuildProjectFullPath = {projectPath}
                            build_property.NETCoreSdkVersion = {GetMetadata( "NETCoreSdkVersion" )}
                            """;

        this.DocumentId = DocumentId.CreateNewId( this.ProjectId );

        var projectInfo = ProjectInfo.Create(
                this.ProjectId,
                VersionStamp.Create(),
                ProjectName,
                ProjectName,
                LanguageNames.CSharp,
                filePath: projectPath,
                compilationOptions: new CSharpCompilationOptions( OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable ),
                parseOptions: new CSharpParseOptions( LanguageVersion.Latest, preprocessorSymbols: ["METALAMA"] ),
                metadataReferences: GetMetadataReferences(),
                analyzerReferences: analyzerReferences )
            .WithDocuments(
            [
                CreateDocument( DocumentId.CreateNewId( this.ProjectId ), "Aspect.cs", AspectCode, projectDirectory ),
                CreateDocument( this.DocumentId, "Code.cs", Code, projectDirectory )
            ] )
            .WithAnalyzerConfigDocuments( [CreateDocument( DocumentId.CreateNewId( this.ProjectId ), ".globalconfig", globalConfig, projectDirectory )] );

        this.Workspace.AddProject( projectInfo );
    }

    private static DocumentInfo CreateDocument( DocumentId id, string name, string text, string directory )
        => DocumentInfo.Create(
            id,
            name,
            loader: TextLoader.From( TextAndVersion.Create( SourceText.From( text ), VersionStamp.Create() ) ),
            filePath: Path.Combine( directory, name ) );

    /// <summary>
    /// Returns the references of the hosted project: the assemblies of the current runtime, and <c>Metalama.Framework.dll</c>.
    /// </summary>
    private static IEnumerable<MetadataReference> GetMetadataReferences()
    {
#if NET
        // The assemblies of the shared framework, which include the ones that the compiler requires for dynamic code.
        var runtimeDirectory = Path.GetDirectoryName( typeof(object).Assembly.Location )!;

        var runtimeAssemblies = ((string) AppContext.GetData( "TRUSTED_PLATFORM_ASSEMBLIES" )!)
            .Split( Path.PathSeparator )
            .Where( p => string.Equals( Path.GetDirectoryName( p ), runtimeDirectory, StringComparison.OrdinalIgnoreCase ) );
#else
        var runtimeDirectory = Path.GetDirectoryName( typeof(object).Assembly.Location )!;

        var runtimeAssemblies = new[] { "mscorlib.dll", "System.dll", "System.Core.dll", "netstandard.dll", "System.Runtime.dll" }
            .Select( f => Path.Combine( runtimeDirectory, f ) );
#endif

        return runtimeAssemblies.Append( GetMetadata( "MetalamaFrameworkPath" ) ).Select( p => MetadataReference.CreateFromFile( p ) );
    }

    private static string GetMetadata( string key )
        => typeof(VisualStudioInProcessFixture).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>().Single( a => a.Key == key ).Value!;

    public ValueTask DisposeAsync()
    {
        this.Workspace.Dispose();

        return default;
    }
}
