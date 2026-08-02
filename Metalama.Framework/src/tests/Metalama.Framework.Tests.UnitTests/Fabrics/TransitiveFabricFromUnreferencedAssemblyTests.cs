// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Utilities;
using Metalama.Framework.Engine;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.Collections;
using Metalama.Framework.Engine.CompileTime;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Pipeline;
using Metalama.Framework.Engine.Pipeline.DesignTime;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Tests.UnitTests.CompileTime;
using Metalama.Testing.UnitTesting;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Framework.Tests.UnitTests.Fabrics;

/// <summary>
/// Regression tests for the initialization of the aspect pipeline when a transitive fabric is declared in an assembly
/// that is not referenced by the run-time compilation (issue #1759).
/// </summary>
/// <remarks>
/// <para>
/// A compile-time project is loaded for every assembly of the compile-time closure, and the closure is walked
/// recursively through <c>CompileTimeProject.References</c>. Referenced compile-time projects are resolved through
/// <see cref="IAssemblyLocator"/>, so the closure can legitimately contain an assembly that the run-time compilation
/// does not reference: it is enough that an intermediate assembly references it while the consuming project does not.
/// </para>
/// <para>
/// <c>FabricDriver.GetCreationData</c> nevertheless resolved the fabric type into the run-time compilation, which
/// threw <see cref="System.InvalidOperationException"/> ("The type ... cannot be used at run-time, because the
/// assembly ... is not referenced in project ...") and aborted the whole pipeline initialization instead of reporting
/// a diagnostic.
/// </para>
/// </remarks>
public sealed class TransitiveFabricFromUnreferencedAssemblyTests : UnitTestClass
{
    public TransitiveFabricFromUnreferencedAssemblyTests( ITestOutputHelper logger ) : base( logger ) { }

    private const string _fabricCode = @"
using Metalama.Framework.Fabrics;

namespace UnreferencedFabricNamespace
{
    public class Fabric : TransitiveProjectFabric
    {
        public override void AmendProject( IProjectAmender amender ) { }
    }
}
";

    private const string _middleCode = @"
using Metalama.Framework.Aspects;

public class MiddleAspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod() => meta.Proceed();
}
";

    private const string _consumerCode = @"
public class ConsumerClass
{
    public void M() { }
}
";

    [Fact]
    public void PipelineInitializesWhenFabricAssemblyIsNotReferenced()
    {
        var assemblyLocator = new TestAssemblyLocator();
        var services = CreateAdditionalServiceCollection( assemblyLocator );

        using var testContext = this.CreateTestContext( services );

        List<string> tempFiles = [];

        try
        {
            // Compiles the given code with Metalama, emits the run-time assembly (including the compile-time project
            // resource) to disk, and registers it with the assembly locator so that the compile-time project can be
            // loaded even by a compilation that does not reference the assembly.
            PortableExecutableReference CompileProject( string code, string assemblyName, params MetadataReference[] references )
            {
                var compilation = testContext.CreateCSharpCompilation( code, additionalReferences: references, assemblyName: assemblyName );

                var repository = CompileTimeProjectRepository.Create( testContext.Domain, testContext.ServiceProvider, compilation )
                    .AssertNotNull();

                var path = MetalamaPathUtilities.GetTempFileName();
                tempFiles.Add( path );

                using ( var stream = File.Create( path ) )
                {
                    var emitResult = compilation.Emit( stream, manifestResources: [repository.RootProject.ToResource().Resource] );

                    Assert.True( emitResult.Success );
                }

                var reference = MetadataReference.CreateFromFile( path );
                assemblyLocator.Files.Add( compilation.Assembly.Identity, reference );

                return reference;
            }

            var fabricAssembly = CompileProject( _fabricCode, "FabricAssembly" );
            var middleAssembly = CompileProject( _middleCode, "MiddleAssembly", fabricAssembly );

            // The consumer references the middle assembly only. The fabric assembly is a part of the compile-time
            // closure, but its run-time assembly is not referenced.
            var consumerCompilation = testContext.CreateCSharpCompilation(
                _consumerCode,
                additionalReferences: [middleAssembly],
                assemblyName: "ConsumerAssembly" );

            var pipeline = new TestablePipeline( testContext.ServiceProvider );
            var diagnostics = new DiagnosticBag();

            // This used to throw InvalidOperationException from FabricDriver.GetCreationData.
            Assert.True(
                pipeline.InvokeTryInitialize( diagnostics, consumerCompilation, out _ ),
                $"Pipeline initialization failed.\n{FormatDiagnostics( diagnostics )}" );
        }
        finally
        {
            foreach ( var path in tempFiles )
            {
                if ( File.Exists( path ) )
                {
                    File.Delete( path );
                }
            }
        }
    }

    private static string FormatDiagnostics( DiagnosticBag bag )
        => string.Join( "\n  ", bag.Select( d => d.GetMessage( CultureInfo.InvariantCulture ) ) );

    /// <summary>
    /// Test-only subclass exposing the <c>protected</c> <see cref="AspectPipeline.TryInitialize"/>.
    /// </summary>
    private sealed class TestablePipeline : PreviewAspectPipeline
    {
        public TestablePipeline( ProjectServiceProvider serviceProvider ) : base( serviceProvider, ExecutionScenario.Preview ) { }

        public bool InvokeTryInitialize( IDiagnosticAdder diagnosticAdder, Compilation compilation, out AspectPipelineConfiguration? configuration )
            => this.TryInitialize( diagnosticAdder, compilation, null, CancellationToken.None, out configuration );
    }
}
