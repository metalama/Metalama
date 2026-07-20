// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.CompileTime;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Pipeline;
using Metalama.Framework.Engine.Pipeline.DesignTime;
using Metalama.Framework.Engine.Services;
using Metalama.Testing.UnitTesting;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Threading;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Framework.Tests.UnitTests.CompileTime;

/// <summary>
/// Regression tests for the compile-time project closure built when a downstream project reuses an upstream
/// pipeline's <see cref="CompileTimeProject"/> (issue #1611).
/// </summary>
/// <remarks>
/// <para>
/// Reproduces the scenario of opening a solution in Visual Studio without the Metalama extension: two projects,
/// each with its own design-time pipeline, one referencing the other as a project (hence a
/// <see cref="CompilationReference"/>). The downstream pipeline's <c>CompileTimeProjectRepository.Builder</c> takes
/// the <see cref="IUpstreamCompileTimeProjectProvider"/> shortcut and imports a project built by the
/// <em>upstream</em> Builder.
/// </para>
/// <para>
/// Each Builder creates its own <c>Metalama.Framework</c> compile-time project instance
/// (<c>FrameworkCompileTimeProjectFactory.CreateFrameworkProject</c> caches only the manifest, not the project), and
/// the closure is deduplicated by reference equality. So the imported upstream project drags the upstream Builder's
/// framework instance into a closure that already holds the downstream Builder's own, and
/// <c>ClosureProjectsByCompileTimeAssemblyName</c> then throws
/// <c>ArgumentException: An item with the same key has already been added. Key: Metalama.Framework</c>.
/// </para>
/// </remarks>
public sealed class UpstreamCompileTimeProjectReuseTests : UnitTestClass
{
    public UpstreamCompileTimeProjectReuseTests( ITestOutputHelper logger ) : base( logger ) { }

    private const string _upstreamCode = @"
using Metalama.Framework.Aspects;

public class UpstreamAspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod() => meta.Proceed();
}
";

    private const string _downstreamCode = @"
public class DownstreamClass
{
    [UpstreamAspect]
    public void M() { }
}
";

    [Fact]
    public void ClosureHasSingleFrameworkProjectWhenUpstreamProjectIsReused()
    {
        var upstreamProvider = new TestUpstreamCompileTimeProjectProvider();
        var services = CreateAdditionalServiceCollection();
        services.AddGlobalService<IUpstreamCompileTimeProjectProvider>( upstreamProvider, true );

        using var testContext = this.CreateTestContext( services );

        var upstreamCompilation = testContext.CreateCSharpCompilation( _upstreamCode, assemblyName: "UpstreamProject" );

        // Stand in for the upstream project's own design-time pipeline: it initializes first, and the
        // CompileTimeProject it produces comes from a CompileTimeProjectRepository.Builder of its own.
        var upstreamPipeline = new TestablePipeline( testContext.ServiceProvider );
        var initDiagnostics = new DiagnosticBag();

        Assert.True(
            upstreamPipeline.InvokeTryInitialize( initDiagnostics, upstreamCompilation, out var upstreamConfiguration ),
            $"Upstream pipeline initialization failed.\n{FormatDiagnostics( initDiagnostics )}" );

        upstreamProvider.Add( upstreamCompilation, upstreamConfiguration.AssertNotNull() );

        // The downstream project references the upstream one as a project, i.e. by CompilationReference.
        var downstreamCompilation = testContext.CreateCSharpCompilation(
            _downstreamCode,
            additionalReferences: [upstreamCompilation.ToMetadataReference()],
            assemblyName: "DownstreamProject" );

        var repository = CompileTimeProjectRepository.Create( testContext.Domain, testContext.ServiceProvider, downstreamCompilation )
            .AssertNotNull();

        // The upstream shortcut must actually have been taken, otherwise the test proves nothing.
        Assert.True( upstreamProvider.WasHit, "The upstream CompileTimeProject was not reused; the test does not cover the intended path." );

        // This is the call that actually throws in the reported stack trace, reached through
        // CompileTimeSerializationBinder.BindToName during transitive-manifest serialization.
        Assert.True( repository.RootProject.TryGetProjectByCompileTimeAssemblyName( "Metalama.Framework", out _ ) );

        // The underlying cause: the closure holds two 'Metalama.Framework' projects, one per Builder.
        var frameworkProjects = repository.RootProject.ClosureProjects.Where( p => p.IsFramework ).ToReadOnlyList();

        Assert.Single( frameworkProjects );
    }

    private static string FormatDiagnostics( DiagnosticBag bag )
        => string.Join( "\n  ", bag.SelectAsArray( d => d.GetMessage( CultureInfo.InvariantCulture ) ) );

    /// <summary>
    /// Test double for <see cref="IUpstreamCompileTimeProjectProvider"/> that resolves the configurations
    /// registered by the test, keyed by <see cref="Compilation"/> instance.
    /// </summary>
    private sealed class TestUpstreamCompileTimeProjectProvider : IUpstreamCompileTimeProjectProvider
    {
        private readonly Dictionary<Compilation, AspectPipelineConfiguration> _configurations = new();

        /// <summary>
        /// Gets a value indicating whether a lookup ever succeeded, so the test can assert that the
        /// upstream-reuse branch of the Builder was really exercised.
        /// </summary>
        public bool WasHit { get; private set; }

        public void Add( Compilation compilation, AspectPipelineConfiguration configuration ) => this._configurations[compilation] = configuration;

        public bool TryGetUpstreamConfiguration( Compilation compilation, [NotNullWhen( true )] out AspectPipelineConfiguration? configuration )
        {
            if ( this._configurations.TryGetValue( compilation, out configuration ) )
            {
                this.WasHit = true;

                return true;
            }

            return false;
        }
    }

    /// <summary>
    /// Test-only subclass exposing the <c>protected</c> <see cref="AspectPipeline.TryInitialize"/>, so the test can
    /// obtain a real <see cref="AspectPipelineConfiguration"/> for the upstream project.
    /// </summary>
    private sealed class TestablePipeline : PreviewAspectPipeline
    {
        public TestablePipeline( ProjectServiceProvider serviceProvider ) : base( serviceProvider, ExecutionScenario.Preview ) { }

        public bool InvokeTryInitialize( IDiagnosticAdder diagnosticAdder, Compilation compilation, out AspectPipelineConfiguration? configuration )
            => this.TryInitialize( diagnosticAdder, compilation, null, CancellationToken.None, out configuration );
    }
}
