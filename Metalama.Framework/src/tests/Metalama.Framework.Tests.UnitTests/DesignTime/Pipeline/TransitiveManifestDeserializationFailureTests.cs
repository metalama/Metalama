// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.DesignTime.Pipeline;
using Metalama.Framework.Engine;
using Metalama.Framework.Engine.CompileTime;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Testing;
using Metalama.Framework.Engine.Utilities;
using Metalama.Framework.Tests.UnitTestHelpers.Mocks;
using Metalama.Framework.Tests.UnitTestHelpers.TestClasses;
using Metalama.Testing.UnitTesting;
using Microsoft.CodeAnalysis;
using System;
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

        AssertConsumerWasAnalyzed( consumerResult );
        AssertManifestFailureWasReported( consumerResult );
    }

    /// <summary>
    /// The aspect library shared by the producing and the consuming project. It is compiled once per target framework,
    /// so that the two projects reference two distinct compile-time copies of it and the consumer cannot reuse the
    /// producer's live manifest.
    /// </summary>
    private static string GetSharedCode( string targetFramework )
        => $$"""
             using Metalama.Framework.Advising;
             using Metalama.Framework.Aspects;
             using Metalama.Framework.Code;

             [assembly: System.Runtime.Versioning.TargetFramework("{{targetFramework}}")]

             namespace Shared
             {
                 public class PullAspect : ConstructorAspect
                 {
                     public override void BuildAspect( IAspectBuilder<IConstructor> builder )
                     {
                         builder.IntroduceParameter(
                             "p1",
                             typeof(int),
                             TypedConstant.Create( 15 ),
                             PullStrategy.IntroduceParameterAndPull( defaultValue: TypedConstant.Create( 20 ) ) );
                     }
                 }

                 public class IntroduceAspect : TypeAspect
                 {
                     [Introduce]
                     public int IntroducedMethod() => 42;
                 }
             }
             """;

    private const string _libraryCode = """
                                        using Shared;

                                        public partial class C
                                        {
                                            [PullAspect]
                                            public C() { }

                                            public C( string s ) : this() { }
                                        }
                                        """;

    private const string _appCode = """
                                    using Shared;

                                    public partial class D : C
                                    {
                                        D( string s ) : base( s ) { }
                                    }

                                    [IntroduceAspect]
                                    public partial class AppLocal { }
                                    """;

    /// <summary>
    /// The same property for a reference to another project of the solution rather than to an assembly on disk. This is
    /// the path of the stacks reported in issue #2049: the consumer deserializes the manifest that the referenced
    /// project's own pipeline produced, because the two projects reference distinct compile-time copies of the shared
    /// aspect library.
    /// </summary>
    /// <remarks>
    /// The bytes of that manifest are produced by the referenced project's pipeline, so a test cannot damage them from
    /// the outside. The failure is injected instead, at the fault injection point in
    /// <c>TransitiveAspectsManifest.Deserialize</c>, with the exception that was reported. The injection point is
    /// outside the code that handles the failure, so a branch that did not handle it would let the exception travel and
    /// the test would fail.
    /// </remarks>
    [Fact]
    public void UnreadableManifestFromAReferencedProject_DoesNotAbortTheConsumingProject()
    {
        using var testContext = this.CreateTestContext();
        using var libraryContext = this.CreateTestContext();
        using var appContext = this.CreateTestContext();

        var sharedForLibrary = testContext.CreateCSharpCompilation( GetSharedCode( ".NETStandard,Version=v2.0" ), assemblyName: "Shared" );
        var sharedForApp = testContext.CreateCSharpCompilation( GetSharedCode( ".NETFramework,Version=v4.7.2" ), assemblyName: "Shared" );

        var library = testContext.CreateCSharpCompilation(
            _libraryCode,
            assemblyName: "Library",
            additionalReferences: [sharedForLibrary.ToMetadataReference()] );

        using var pipelineFactory = new TestDesignTimeAspectPipelineFactory( testContext );

        Assert.True( pipelineFactory.TryExecute( libraryContext.ProjectOptions, library, default, out var libraryResult ) );

        // Materialized, because WithFilePath returns a new syntax tree on each call and Roslyn enumerates the argument
        // more than once, which would put distinct instances in the compilation and in the version index.
        var generatedTrees = libraryResult.Result.SyntaxTreeResults.Values
            .SelectMany( r => r.Introductions )
            .Select( i => i.GeneratedSyntaxTree.WithFilePath( $"{SourceGeneratorHelper.GeneratedFilePathSegment}/{i.Name}.cs" ) )
            .ToArray();

        Assert.NotEmpty( generatedTrees );

        var libraryWithDesignTimeCode = library.AddSyntaxTrees( generatedTrees );

        var app = testContext.CreateCSharpCompilation(
            _appCode,
            assemblyName: "App",
            additionalReferences: [sharedForApp.ToMetadataReference(), libraryWithDesignTimeCode.ToMetadataReference()] );

        // The exception reported in the issue, raised by System.IO.Compression.Inflater for zlib's Z_DATA_ERROR.
        testContext.FaultInjector.ArmFault(
            FaultInjectionPoints.TransitiveManifestDeserialization,
            () => new InvalidDataException( "The archive entry was compressed using an unsupported compression method." ) );

        Assert.True( pipelineFactory.TryExecute( appContext.ProjectOptions, app, default, out var appResult ) );

        this.TestOutput.WriteLine( DumpResults( appResult ) );

        Assert.Equal( 1, testContext.FaultInjector.GetInjectedFaultCount( FaultInjectionPoints.TransitiveManifestDeserialization ) );

        AssertConsumerWasAnalyzed( appResult );
        AssertManifestFailureWasReported( appResult );
    }

    /// <summary>
    /// Asserts that the consuming project was analyzed, by looking for the code generated by the aspect it applies
    /// itself. This is the work that was lost when one unreadable manifest aborted the whole pass.
    /// </summary>
    private static void AssertConsumerWasAnalyzed( DesignTimeAspectPipelineResultAndState consumerResult )
    {
        var introducedCode = string.Join(
            "\n",
            consumerResult.Result.SyntaxTreeResults.Values
                .SelectMany( r => r.Introductions )
                .Select( i => i.GeneratedSyntaxTree.ToString() ) );

        Assert.Contains( "IntroducedMethod", introducedCode, StringComparison.Ordinal );
    }

    /// <summary>
    /// Asserts that the failure was reported rather than passed over in silence. A reference whose aspects are dropped
    /// without a word changes the code emitted by a batch compilation with no signal at all, which would be a worse
    /// defect than the one being fixed.
    /// </summary>
    private static void AssertManifestFailureWasReported( DesignTimeAspectPipelineResultAndState consumerResult )
    {
        var diagnostic = consumerResult.Result.SyntaxTreeResults.Values
            .SelectMany( r => r.Diagnostics )
            .FirstOrDefault( d => d.Id == "LAMA0087" );

        Assert.NotNull( diagnostic );
        Assert.Equal( DiagnosticSeverity.Error, diagnostic.Severity );
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
