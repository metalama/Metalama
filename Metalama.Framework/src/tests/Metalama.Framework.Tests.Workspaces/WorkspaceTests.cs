// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Introspection;
using Metalama.Framework.Workspaces;
using Metalama.Testing.UnitTesting;
using Microsoft.CodeAnalysis;
using SharpCrafters.Backstage.Telemetry;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Workspace = Metalama.Framework.Workspaces.Workspace;

#pragma warning disable VSTHRD200

namespace Metalama.Framework.Tests.Workspaces
{
    public sealed class WorkspaceTests : UnitTestClass
    {
        /// <summary>
        /// The MSBuild properties that every test of this class passes when it loads a workspace.
        /// </summary>
        /// <remarks>
        /// The first test that loads a project builds the project that resolves the compile-time reference assemblies.
        /// This build is cached per Metalama version, so one test pays the full cost of the restore. On a loaded build agent,
        /// this cost can exceed the default timeout of 120 seconds, and the load then fails with <c>LAMA0083</c>.
        /// The timeout is therefore raised to 10 minutes.
        /// </remarks>
        internal static readonly ImmutableDictionary<string, string> DefaultProperties = ImmutableDictionary<string, string>.Empty
            .Add( "MetalamaReferenceAssemblyRestoreTimeout", "600000" );

        /// <summary>
        /// The properties of <see cref="DefaultProperties"/>, and empty values for the properties that MSBuild would otherwise read from the environment variables of the test process.
        /// </summary>
        private static readonly ImmutableDictionary<string, string> _buildProperties = DefaultProperties
            .Add( "DOTNET_ROOT_X64", "" )
            .Add( "MSBUILD_EXE_PATH", "" )
            .Add( "MSBuildSDKsPath", "" );

        [Fact]
        public async Task LoadProjectSingleTarget()
        {
            using var testContext = this.CreateTestContext();

            var projectPath = Path.Combine( testContext.BaseDirectory, "Project.csproj" );
            var codePath = Path.Combine( testContext.BaseDirectory, "Code.cs" );

            await File.WriteAllTextAsync(
                projectPath,
                @"
<Project Sdk=""Microsoft.NET.Sdk"">
    <PropertyGroup>
        <TargetFramework>net10.0</TargetFramework>
    </PropertyGroup>
</Project>
",
                testContext.CancellationToken );

            await File.WriteAllTextAsync( codePath, "class MyClass {}", testContext.CancellationToken );

            var workspaceCollection = new WorkspaceCollection( testContext.ServiceProvider );

            using var workspace = await workspaceCollection.LoadAsync( [projectPath], _buildProperties, cancellationToken: testContext.CancellationToken );

            CheckWorkspace( workspace );

            Assert.Single( workspace.Projects );
            Assert.Single( workspace.Projects[0].Types );

            Assert.False( workspace.Projects[0].IsMetalamaEnabled );
            Assert.Same( workspace.Projects[0].SourceCode.Compilations[0], workspace.Projects[0].TransformedCode.Compilations[0] );
        }

        [Fact]
        public async Task LoadProjectMultiTarget()
        {
            using var testContext = this.CreateTestContext();

            var projectPath = Path.Combine( testContext.BaseDirectory, "Project.csproj" );
            var codePath = Path.Combine( testContext.BaseDirectory, "Code.cs" );

            await File.WriteAllTextAsync(
                projectPath,
                @"
<Project Sdk=""Microsoft.NET.Sdk"">
    <PropertyGroup>
        <TargetFrameworks>netstandard2.0;net10.0</TargetFrameworks>
    </PropertyGroup>
</Project>
",
                testContext.CancellationToken );

            await File.WriteAllTextAsync( codePath, "class MyClass {}", testContext.CancellationToken );

            var workspaceCollection = new WorkspaceCollection( testContext.ServiceProvider );

            using var workspace = await workspaceCollection.LoadAsync( [projectPath], DefaultProperties, cancellationToken: testContext.CancellationToken );

            CheckWorkspace( workspace );

            Assert.Equal( 2, workspace.Projects.Length );
            Assert.Equal( 2, workspace.SourceCode.Types.Length );
        }

        [Fact]
        public async Task IgnoreErrors()
        {
            using var testContext = this.CreateTestContext();

            var projectPath = await CreateMetalamaEnabledProjectAsync(
                testContext,
                "using Metalama.Framework.Aspects;  [CompileTime] class MyClass /* Intentional syntax error in compile-time code .*/ " );

            var workspaceCollection = new WorkspaceCollection( testContext.ServiceProvider );

            using var workspace = await workspaceCollection.LoadAsync( [projectPath], _buildProperties, cancellationToken: testContext.CancellationToken );

            Assert.Throws<CompilationFailedException>( () => workspace.AspectInstances );
            Assert.Throws<CompilationFailedException>( () => workspace.AspectClasses );
            Assert.Throws<CompilationFailedException>( () => workspace.TransformedCode );
            Assert.Throws<CompilationFailedException>( () => workspace.Transformations );
            Assert.Throws<CompilationFailedException>( () => workspace.Advice );

            // Changing the option in the workspace should have effect deep inside the code model.
            workspace.WithIgnoreErrors();

            Assert.Empty( workspace.AspectInstances );
            Assert.Empty( workspace.AspectClasses );
            Assert.Empty( workspace.Transformations );
            Assert.Empty( workspace.Advice );
        }

        internal static async Task<string> CreateMetalamaEnabledProjectAsync(
            MetalamaTestContext testContext,
            string code,
            string? projectName = null,
            string[]? dependentProjectPaths = null )
        {
            dependentProjectPaths ??= [];
            projectName ??= "Foo";

            var compilationForReferences = testContext.CreateCSharpCompilation( "" );

            var libraryReferences = compilationForReferences.ExternalReferences.OfType<PortableExecutableReference>()
                .Where( r => Path.GetFileName( r.FilePath )?.StartsWith( "Metalama", StringComparison.Ordinal ) == true )
                .Select( r => $"<Reference Include=\"{r.FilePath}\" />" );

            var projectReferences = dependentProjectPaths.Select( r => $"<ProjectReference Include=\"{r}\" />" );

            var projectDirectory = Path.Combine( testContext.BaseDirectory, projectName );
            Directory.CreateDirectory( projectDirectory );
            var projectPath = Path.Combine( projectDirectory, $"{projectName}.csproj" );
            var codePath = Path.Combine( projectDirectory, $"Code.cs" );

            await File.WriteAllTextAsync(
                projectPath,
                $@"
<Project Sdk=""Microsoft.NET.Sdk"">
    <PropertyGroup>
        <TargetFramework>net10.0</TargetFramework>
        <DefineConstants>METALAMA</DefineConstants>
        <Nullable>enable</Nullable>
    </PropertyGroup>
    <ItemGroup>
        {string.Join( Environment.NewLine, libraryReferences )}
        {string.Join( Environment.NewLine, projectReferences )}
    </ItemGroup>
</Project>
" );

            await File.WriteAllTextAsync( codePath, code );

            return projectPath;
        }

        [Fact]
        public async Task MetalamaEnabled()
        {
            using var testContext = this.CreateTestContext();

            var projectPath = await CreateMetalamaEnabledProjectAsync(
                testContext,
                @"
using Metalama.Framework.Advising; 
using Metalama.Framework.Aspects; 

class MyAspect : TypeAspect
{
   [Introduce] void IntroducedMethod(){}
}
[MyAspect]
class MyClass {}" );

            var workspaceCollection = new WorkspaceCollection( testContext.ServiceProvider );

            using var workspace = await workspaceCollection.LoadAsync( [projectPath], DefaultProperties, cancellationToken: testContext.CancellationToken );

            CheckWorkspace( workspace );

            Assert.True( workspace.IsMetalamaEnabled );
            Assert.Equal( 4, workspace.AspectClasses.Length );
            Assert.Single( workspace.AspectInstances );
            var targetFramework = Assert.Single( workspace.SourceCode.TargetFrameworks );
            Assert.Equal( "net10.0", targetFramework );
        }

        /// <summary>
        /// Verifies that a workspace opens one telemetry context when it is loaded, for the directory of the loaded project,
        /// and that an exception contained by the reference indexer is reported to it through the service provider of the
        /// project. See issue #2419.
        /// </summary>
        [Fact]
        public async Task TelemetryContext_OpenedOnLoadAndUsedForContainedExceptions()
        {
            using var testContext = this.CreateTestContext();
            testContext.ExpectsReportedExceptions = true;

            var projectPath = await CreateMetalamaEnabledProjectAsync( testContext, "class A { static void F() {} void M() { F(); } }" );

            var workspaceCollection = new WorkspaceCollection( testContext.ServiceProvider );

            using var workspace = await workspaceCollection.LoadAsync( [projectPath], DefaultProperties, cancellationToken: testContext.CancellationToken );

            CheckWorkspace( workspace );

            var telemetryContext = workspace.TelemetryContext;
            Assert.NotNull( telemetryContext );

            // The walker fails on the invocation statement, and the reference index contains the failure. The name is the one that
            // FaultInjectionPoints.GetReferenceIndexWalkerVisit returns, which this project cannot access.
            testContext.FaultInjector.ArmFault( "ReferenceIndexWalker.Visit.ExpressionStatement" );

            var methodF = workspace.Projects.Single().Types.Single().Methods.OfName( "F" ).Single();
            Assert.Empty( methodF.GetInboundReferences( cancellationToken: testContext.CancellationToken ) );

            var reported = Assert.Single( testContext.ReportedTelemetryExceptions );
            // The exception is wrapped in a SyntaxProcessingException, whose message gives the kind of the node.
            Assert.Contains( "while processing the ExpressionStatement", reported.Exception.Message, StringComparison.Ordinal );
            Assert.Equal( TelemetryRouting.Repository, reported.Routing );
            Assert.Equal( Path.GetDirectoryName( projectPath ), reported.Directory );
        }

        /// <summary>
        /// Verifies that the diagnostic sink of a workspace reports each diagnostic of the reference indexer once per scope of
        /// <see cref="DiagnosticReporter"/>, and again after <see cref="DiagnosticReporter.ClearCounters"/> starts a new scope. See
        /// issue #2415.
        /// </summary>
        [Fact]
        public async Task ReferenceDiagnosticSink_ReportsOncePerScope()
        {
            using var testContext = this.CreateTestContext();

            var projectPath = await CreateMetalamaEnabledProjectAsync( testContext, "class C;" );
            var workspaceCollection = new WorkspaceCollection( testContext.ServiceProvider );
            using var workspace = await workspaceCollection.LoadAsync( [projectPath], DefaultProperties, cancellationToken: testContext.CancellationToken );
            var compilation = workspace.Projects.Single().SourceCode.Compilations.Single();

            var descriptor = new DiagnosticDescriptor( "TEST0001", "Title", "The message", "Test", DiagnosticSeverity.Warning, true );
            var diagnostics = ImmutableArray.Create( Diagnostic.Create( descriptor, Location.None ) );

            var reported = new List<IIntrospectionDiagnostic>();
            var previousReportAction = DiagnosticReporter.ReportAction;
            DiagnosticReporter.ReportAction = reported.AddRange;

            try
            {
                var sink = new WorkspaceReferenceDiagnosticSink();

                DiagnosticReporter.ClearCounters();
                sink.Report( compilation, diagnostics );
                sink.Report( compilation, diagnostics );

                Assert.Single( reported );

                DiagnosticReporter.ClearCounters();
                sink.Report( compilation, diagnostics );

                Assert.Equal( 2, reported.Count );
            }
            finally
            {
                DiagnosticReporter.ReportAction = previousReportAction;
            }
        }

        [Fact]
        public async Task InboundDeclarationReferences()
        {
            const string code = """
                                class A;
                                class B : A
                                {
                                  A f;
                                }
                                class C : System.Collections.Generic.List<int>
                                {
                                  int f;
                                }
                                """;

            using var testContext = this.CreateTestContext();

            var projectPath = await CreateMetalamaEnabledProjectAsync(
                testContext,
                code );

            var workspaceCollection = new WorkspaceCollection( testContext.ServiceProvider );

            using var workspace = await workspaceCollection.LoadAsync( [projectPath], DefaultProperties, cancellationToken: testContext.CancellationToken );

            CheckWorkspace( workspace );

            var typeA = workspace.Projects.Single().Types.Single( t => t.Name == "A" );

            var references = GetReferences( typeA );

            Assert.Equal( ["B", "B.f"], references );

            static IEnumerable<string> GetReferences( IDeclaration d )
                => d.GetInboundReferences().Select( x => x.OriginDeclaration.ToDisplayString() ).OrderBy( x => x );
        }

        [Fact]
        public async Task OutboundDeclarationReferences()
        {
            const string code = """
                                class A;
                                class B : A 
                                {
                                  A f;
                                  int g;
                                } 
                                """;

            using var testContext = this.CreateTestContext();

            var projectPath = await CreateMetalamaEnabledProjectAsync(
                testContext,
                code );

            var workspaceCollection = new WorkspaceCollection( testContext.ServiceProvider );

            using var workspace = await workspaceCollection.LoadAsync( [projectPath], DefaultProperties, cancellationToken: testContext.CancellationToken );

            CheckWorkspace( workspace );

            var typeB = workspace.Projects.Single().Types.Single( t => t.Name == "B" );

            var references = GetReferences( typeB );

            Assert.Equal( ["B -> A", "B.f -> A", "B.g -> int"], references );

            static IEnumerable<string> GetReferences( IDeclaration d ) => d.GetOutboundReferences().Select( x => x.ToString()! ).OrderBy( x => x );
        }

        [Fact]
        public async Task InboundSyntaxReferences()
        {
            const string code = """
                                class A { public static void M() {} }
                                class B : A
                                {
                                  A f;
                                  void M() => A.M();
                                }
                                class C : System.Collections.Generic.List<int>
                                {
                                  int f;
                                }
                                """;

            using var testContext = this.CreateTestContext();

            var projectPath = await CreateMetalamaEnabledProjectAsync(
                testContext,
                code );

            var workspaceCollection = new WorkspaceCollection( testContext.ServiceProvider );

            using var workspace = await workspaceCollection.LoadAsync( [projectPath], DefaultProperties, cancellationToken: testContext.CancellationToken );

            CheckWorkspace( workspace );

            var typeA = workspace.Projects.Single().Types.Single( t => t.Name == "A" );

            Assert.Equal( ["B.f[MemberType]", "B.M()[Invocation]", "B[BaseType]"], GetReferences( typeA ) );

            static IEnumerable<string> GetReferences( IDeclaration d )
                => d.GetInboundReferences()
                    .Select( x => x.OriginDeclaration.ToDisplayString() + "[" + string.Join( ",", x.Details.Select( y => y.Kinds ) ) + "]" )
                    .OrderBy( x => x )
                    .ToArray();
        }

        [Fact]
        public async Task CrossProjectInboundReferences()
        {
            const string code1 = """
                                 public class A { public static void M() {} }
                                 """;

            const string code2 = """
                                 class B : A
                                 {
                                   A f;
                                   void M() => A.M();
                                 }
                                 class C : System.Collections.Generic.List<int>
                                 {
                                   int f;
                                 }
                                 """;

            using var testContext = this.CreateTestContext();

            var projectPath1 = await CreateMetalamaEnabledProjectAsync(
                testContext,
                code1,
                "Project1" );

            var projectPath2 = await CreateMetalamaEnabledProjectAsync(
                testContext,
                code2,
                "Project2",
                [projectPath1] );

            var workspaceCollection = new WorkspaceCollection( testContext.ServiceProvider );

            using var workspace = await workspaceCollection.LoadAsync( [projectPath1, projectPath2], DefaultProperties, cancellationToken: testContext.CancellationToken );

            CheckWorkspace( workspace );

            var typesA = workspace.SourceCode.Types.Where( t => t.Name == "A" ).ToArray();

            Assert.Single( typesA );

            var references = GetReferences( typesA.Single() );

            Assert.Equal( ["B -> A", "B.f -> A", "B.M() -> A.M()"], references );

            static IEnumerable<string> GetReferences( IDeclaration d ) => d.GetInboundReferences().Select( x => x.ToString()! ).OrderBy( x => x );
        }

        [Fact]
        public async Task LoadXmlSolution()
        {
            using var testContext = this.CreateTestContext();

            var projectPath = Path.Combine( testContext.BaseDirectory, "Project", "Project.csproj" );
            Directory.CreateDirectory( Path.GetDirectoryName( projectPath )! );

            await File.WriteAllTextAsync(
                projectPath,
                """
                <Project Sdk="Microsoft.NET.Sdk">
                    <PropertyGroup>
                        <TargetFramework>net10.0</TargetFramework>
                    </PropertyGroup>
                </Project>
                """,
                testContext.CancellationToken );

            await File.WriteAllTextAsync( Path.Combine( testContext.BaseDirectory, "Project", "Code.cs" ), "class MyClass {}", testContext.CancellationToken );

            var solutionPath = Path.Combine( testContext.BaseDirectory, "Solution.slnx" );

            await File.WriteAllTextAsync(
                solutionPath,
                """
                <Solution>
                  <Project Path="Project/Project.csproj" />
                </Solution>
                """,
                testContext.CancellationToken );

            var workspaceCollection = new WorkspaceCollection( testContext.ServiceProvider );

            using var workspace = await workspaceCollection.LoadAsync( [solutionPath], _buildProperties, cancellationToken: testContext.CancellationToken );

            CheckWorkspace( workspace );

            Assert.Single( workspace.Projects );
            Assert.Single( workspace.Projects[0].Types );
        }

        [Fact]
        public async Task NuGetAuditWarningsAreWarnings()
        {
            using var testContext = this.CreateTestContext();

            var projectPath = Path.Combine( testContext.BaseDirectory, "Project.csproj" );

            // Newtonsoft.Json 12.0.1 has a known high severity vulnerability (GHSA-5crp-9r3c-p9vr), so restore reports NU1903,
            // which dotnet build reports as a warning.
            await File.WriteAllTextAsync(
                projectPath,
                """
                <Project Sdk="Microsoft.NET.Sdk">
                    <PropertyGroup>
                        <TargetFramework>net10.0</TargetFramework>
                        <ManagePackageVersionsCentrally>false</ManagePackageVersionsCentrally>
                        <NuGetAudit>true</NuGetAudit>
                        <NuGetAuditMode>direct</NuGetAuditMode>
                    </PropertyGroup>
                    <ItemGroup>
                        <PackageReference Include="Newtonsoft.Json" Version="12.0.1" />
                    </ItemGroup>
                </Project>
                """,
                testContext.CancellationToken );

            await File.WriteAllTextAsync( Path.Combine( testContext.BaseDirectory, "Code.cs" ), "class MyClass {}", testContext.CancellationToken );

            var workspaceCollection = new WorkspaceCollection( testContext.ServiceProvider );

            using var workspace = await workspaceCollection.LoadAsync( [projectPath], _buildProperties, cancellationToken: testContext.CancellationToken );

            var diagnostics = workspace.WorkspaceDiagnostics.Select( d => $"{d.Severity}: {d.Message}" ).ToArray();

            Assert.Contains( diagnostics, d => d.StartsWith( "Warning: ", StringComparison.Ordinal ) && d.Contains( "Newtonsoft.Json", StringComparison.Ordinal ) );
            Assert.DoesNotContain( diagnostics, d => d.StartsWith( "Error: ", StringComparison.Ordinal ) );
            Assert.Single( workspace.Projects );
        }

        [Fact]
        public void NuGetAssetsDiagnosticsAreScopedToTheirProject()
        {
            using var testContext = this.CreateTestContext();

            const string message = "Package 'P' 1.0.0 has a known high severity vulnerability.";

            // The same message is a warning in Project1 and an error in Project2.
            var project1 = CreateProjectWithAssetsFile( testContext, "Project1", "obj/project.assets.json", message, "Warning" );
            var project2 = CreateProjectWithAssetsFile( testContext, "Project2", "obj/project.assets.json", message, "Error" );

            var diagnostics = new NuGetAssetsDiagnostics();
            diagnostics.AddAssetsFile( project1, "obj/project.assets.json" );
            diagnostics.AddAssetsFile( project2, "obj/project.assets.json" );

            var fixedDiagnostics = diagnostics.Fix( [CreateRoslynDiagnostic( project1, message ), CreateRoslynDiagnostic( project2, message )] );

            Assert.Equal( new[] { WorkspaceDiagnosticKind.Warning, WorkspaceDiagnosticKind.Failure }, fixedDiagnostics.Select( d => d.Kind ) );
        }

        [Fact]
        public void NuGetAssetsDiagnosticsResolveRelativeAssetsFileAgainstProject()
        {
            using var testContext = this.CreateTestContext();

            const string message = "Package 'P' 1.0.0 has a known moderate severity vulnerability.";

            var project = CreateProjectWithAssetsFile( testContext, "Project", "custom/assets.json", message, "Warning" );

            var diagnostics = new NuGetAssetsDiagnostics();
            diagnostics.AddAssetsFile( project, "custom/assets.json" );

            var fixedDiagnostics = diagnostics.Fix( [CreateRoslynDiagnostic( project, message )] );

            Assert.Equal( WorkspaceDiagnosticKind.Warning, Assert.Single( fixedDiagnostics ).Kind );
        }

        private static string CreateProjectWithAssetsFile( MetalamaTestContext testContext, string name, string assetsFile, string message, string level )
        {
            var projectDirectory = Path.Combine( testContext.BaseDirectory, name );
            var assetsPath = Path.Combine( projectDirectory, assetsFile );
            Directory.CreateDirectory( Path.GetDirectoryName( assetsPath )! );

            File.WriteAllText(
                assetsPath,
                $$"""
                  { "version": 3, "logs": [ { "code": "NU1903", "level": "{{level}}", "message": "{{message}}" } ] }
                  """ );

            var projectPath = Path.Combine( projectDirectory, $"{name}.csproj" );
            File.WriteAllText( projectPath, "<Project />" );

            return projectPath;
        }

        // Formats the message as MSBuildWorkspace does.
        private static WorkspaceDiagnostic CreateRoslynDiagnostic( string projectPath, string message )
            => new(
                WorkspaceDiagnosticKind.Failure,
                $"Msbuild failed when processing the file '{projectPath}' with message: {projectPath}: (0, 0): {message}" );

        /// <summary>
        /// Asserts that the workspace loaded without a diagnostic.
        /// </summary>
        /// <remarks>
        /// The failure message of <see cref="Assert.Empty(System.Collections.IEnumerable)"/> truncates each element of the collection, so the cause of a
        /// diagnostic would be lost. The method therefore writes the complete messages to the test output and includes them
        /// in the failure message.
        /// </remarks>
        internal static void CheckWorkspace( Workspace workspace )
        {
            AssertNoMessage( "workspace diagnostics", workspace.WorkspaceDiagnostics.Select( d => $"{d.Severity}: {d.Message}" ) );

            AssertNoMessage(
                "compilation errors",
                workspace.Projects.SelectMany( c => c.RoslynCompilation.GetDiagnostics().Where( d => d.Severity == DiagnosticSeverity.Error ) )
                    .Select( d => d.ToString() ) );

            static void AssertNoMessage( string kind, IEnumerable<string> messages )
            {
                var messageList = messages.ToList();

                if ( messageList.Count == 0 )
                {
                    return;
                }

                var text = $"The workspace has {messageList.Count} {kind}:{Environment.NewLine}{string.Join( Environment.NewLine, messageList )}";

                TestContext.Current.TestOutputHelper?.WriteLine( text );

                Assert.Fail( text );
            }
        }

        private static async Task<string> CreatePlainProjectAsync( MetalamaTestContext testContext, string name )
        {
            var directory = Path.Combine( testContext.BaseDirectory, name );
            Directory.CreateDirectory( directory );
            var projectPath = Path.Combine( directory, name + ".csproj" );

            await File.WriteAllTextAsync(
                projectPath,
                """
                <Project Sdk="Microsoft.NET.Sdk">
                    <PropertyGroup>
                        <TargetFramework>net10.0</TargetFramework>
                    </PropertyGroup>
                </Project>
                """,
                testContext.CancellationToken );

            await File.WriteAllTextAsync( Path.Combine( directory, "Code.cs" ), $"class {name}Class {{}}", testContext.CancellationToken );

            return projectPath;
        }

        [Fact]
        public async Task CancelledLoadIsNotCached()
        {
            using var testContext = this.CreateTestContext();
            var projectPath = await CreatePlainProjectAsync( testContext, "Cancelled" );
            var workspaceCollection = new WorkspaceCollection( testContext.ServiceProvider );

            using var cancelled = new System.Threading.CancellationTokenSource();
            await cancelled.CancelAsync();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => workspaceCollection.LoadAsync( [projectPath], _buildProperties, cancellationToken: cancelled.Token ) );

            // A load that did not complete must not stay cached: the same load is retried.
            using var workspace = await workspaceCollection.LoadAsync( [projectPath], _buildProperties, cancellationToken: testContext.CancellationToken );

            CheckWorkspace( workspace );
        }

        [Fact]
        public async Task FailedLoadDoesNotBreakTryFindProject()
        {
            using var testContext = this.CreateTestContext();
            var loadedPath = await CreatePlainProjectAsync( testContext, "Loaded" );
            var cancelledPath = await CreatePlainProjectAsync( testContext, "Cancelled" );
            var workspaceCollection = new WorkspaceCollection( testContext.ServiceProvider );

            using var workspace = await workspaceCollection.LoadAsync( [loadedPath], _buildProperties, cancellationToken: testContext.CancellationToken );

            using var cancelled = new System.Threading.CancellationTokenSource();
            await cancelled.CancelAsync();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => workspaceCollection.LoadAsync( [cancelledPath], _buildProperties, cancellationToken: cancelled.Token ) );

            // TryFindProject must skip the load that did not complete instead of throwing. A compilation of no workspace
            // makes it visit every entry, whatever the order of the dictionary.
            var unknownCompilation = Microsoft.CodeAnalysis.CSharp.CSharpCompilation.Create( "Unknown" );
            Assert.False( workspaceCollection.TryFindProject( unknownCompilation, out _, out _, out _ ) );

            Assert.True( workspaceCollection.TryFindProject( workspace.Projects[0].RoslynCompilation, out var found, out _, out _ ) );
            Assert.Same( workspace, found );
        }
    }
}