// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine;
using Metalama.Framework.Engine.CompileTime;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Tests.UnitTestHelpers.Mocks;
using Metalama.Framework.Tests.UnitTestHelpers.TestClasses;
using Metalama.Testing.UnitTesting;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Framework.Tests.UnitTests.DesignTime.Pipeline;

/// <summary>
/// Covers what happens to a consuming project when the transitive aspect manifest of a referenced assembly cannot be
/// read. A manifest that cannot be read makes the reference unusable, so the aspects inherited through it are lost,
/// but the consuming project must still be analyzed: it must still get its own generated code, diagnostics and
/// suppressions. See issue #2049.
/// </summary>
public sealed class TransitiveManifestDeserializationFailureTests : DesignTimePipelineTestsBase
{
    public TransitiveManifestDeserializationFailureTests( ITestOutputHelper testOutput ) : base( testOutput ) { }

    /// <summary>
    /// Bytes that are neither marked as uncompressed nor a valid DEFLATE stream, which is the shape reported in
    /// issue #2049. The first byte is not <c>SerializationProtocol.UncompressedStreamMarker</c>, so the reader takes
    /// them for a DEFLATE stream; read as one, they are a stored block whose length and complemented length
    /// disagree, so the inflate fails on the first byte the deserializer pulls.
    /// </summary>
    private static readonly byte[] _unreadableManifest = [0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00];

    private const string _producerCode = """
                                         using Metalama.Framework.Aspects;

                                         [Inheritable]
                                         public class MyInheritableAspect : TypeAspect { }

                                         public class MyIntroduceAspect : TypeAspect
                                         {
                                             [Introduce]
                                             public int IntroducedMethod() => 42;
                                         }

                                         [MyInheritableAspect]
                                         public class Base { }
                                         """;

    /// <summary>
    /// The consumer both inherits an aspect through the unreadable manifest (on <c>Derived</c>) and applies an aspect
    /// of its own (on <c>Local</c>). The second is what the assertions read: it is the work the consuming project
    /// loses when one unreadable manifest aborts the whole pass.
    /// </summary>
    private const string _consumerCode = """
                                         public class Derived : Base { }

                                         [MyIntroduceAspect]
                                         public partial class Local { }
                                         """;

    /// <summary>
    /// Pins the premise of the fixture: these bytes reproduce the <see cref="InvalidDataException"/> of issue #2049,
    /// raised on the very first byte pulled through the <see cref="DeflateStream"/>. Without this the test could pass
    /// because the manifest was read successfully rather than because the failure was contained.
    /// </summary>
    [Fact]
    public void TheFixtureBytes_FailToInflate()
    {
        using var deflateStream = new DeflateStream( new MemoryStream( _unreadableManifest ), CompressionMode.Decompress );

        Assert.Throws<InvalidDataException>( () => deflateStream.ReadByte() );
    }

    [Fact]
    public void UnreadableManifestInAReferencedAssembly_DoesNotAbortTheConsumingProject()
    {
        using var testContext = this.CreateTestContext();
        using var consumerContext = this.CreateTestContext();

        var producerReference = CreateProducerReference( testContext );

        var consumer = testContext.CreateCSharpCompilation(
            _consumerCode,
            assemblyName: "Consumer",
            additionalReferences: [producerReference] );

        using var pipelineFactory = new TestDesignTimeAspectPipelineFactory( testContext );

        Assert.True( pipelineFactory.TryExecute( consumerContext.ProjectOptions, consumer, default, out var consumerResult ) );

        this.TestOutput.WriteLine( DumpResults( consumerResult ) );

        // The consuming project keeps its own generated code: the aspect it applies itself still runs.
        var introducedCode = string.Join(
            "\n",
            consumerResult.Result.SyntaxTreeResults.Values
                .SelectMany( r => r.Introductions )
                .Select( i => i.GeneratedSyntaxTree.ToString() ) );

        Assert.Contains( "IntroducedMethod", introducedCode, System.StringComparison.Ordinal );
    }

    /// <summary>
    /// Builds the producing assembly and writes it to disk with its compile-time project resource, which is what makes
    /// the consumer treat it as a Metalama reference, and with an unreadable transitive aspect manifest resource.
    /// </summary>
    private static PortableExecutableReference CreateProducerReference( TestContext testContext )
    {
        var producer = testContext.CreateCSharpCompilation( _producerCode, assemblyName: "Producer" );

        var builder = new CompileTimeProjectRepository.Builder( testContext.Domain, testContext.ServiceProvider );
        DiagnosticBag diagnosticBag = new();

        Assert.True(
            builder.TryGetCompileTimeProjectFromCompilation(
                producer,
                null,
                diagnosticBag,
                false,
                testContext.CancellationToken,
                out var producerCompileTimeProject ) );

        var producerPath = Path.Combine( testContext.BaseDirectory, "Producer.dll" );

        using ( var peStream = File.Create( producerPath ) )
        {
            var resources = new List<ResourceDescription>
            {
                producerCompileTimeProject.AssertNotNull().ToResource().Resource,
                new(
                    CompileTimeConstants.InheritableAspectManifestResourceName,
                    () => new MemoryStream( _unreadableManifest ),
                    isPublic: true )
            };

            Assert.True( producer.Emit( peStream, manifestResources: resources ).Success );
        }

        return MetadataReference.CreateFromFile( producerPath );
    }
}
