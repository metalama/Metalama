// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Compiler;
using Metalama.Framework.Engine.CompileTime;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Pipeline.CompileTime;
using Metalama.Testing.UnitTesting;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.Aspects;

/// <summary>
/// Verifies that the managed resources that the compile-time pipeline embeds in the assembly do not depend on the order in which
/// the pipeline processes the declarations (issue #2248).
/// </summary>
/// <remarks>
/// With <c>Deterministic=true</c>, the identifier of the reference assembly is a hash of its content, including the managed
/// resources. When the bytes of a resource change between two compilations of the same source, every dependent project is
/// compiled again. The tests vary the order of the syntax trees, which varies the order in which the aspect instances are produced.
/// </remarks>
public sealed class ManagedResourceDeterminismTests : UnitTestClass
{
    private const string _aspectCode = """
                                       using Metalama.Framework.Aspects;
                                       using Metalama.Framework.Code;

                                       [Inheritable]
                                       public class TrackedAttribute : TypeAspect { }

                                       [Inheritable]
                                       public class AuditedAttribute : TypeAspect { }

                                       [Inheritable]
                                       public class VersionedAttribute : TypeAspect { }
                                       """;

    /// <summary>
    /// The SHA-256 hash of the inflated content of <c>Metalama.InheritableAspects.bin</c> produced by <see cref="InheritableAspectResource_DoesNotDependOnSyntaxTreeOrder"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The CLR randomizes the hash codes of strings in each new process, so a comparison of two outputs produced in the same process
    /// cannot detect an order that follows these hash codes. Each test run is a new process, so a comparison with a hash stored in the
    /// source code detects it.
    /// </para>
    /// <para>
    /// The hash is computed on the inflated content because the bytes produced by <see cref="DeflateStream"/> depend on the runtime.
    /// The content names the core library of the runtime and the assembly of the engine, whose name includes the Roslyn version.
    /// Each combination of target framework and Roslyn version therefore has its own value. The value must be updated when the
    /// serialization format, a supported Roslyn version or the name of an assembly changes.
    /// </para>
    /// </remarks>
#if ROSLYN_5_12_0_OR_GREATER && NET
    private const string _expectedInheritableAspectPayloadHash = "634e1cbe0b41989e30fbfa17e6a3bf66ca64bc94634591486d53e7d1f83dcbd3";
#elif ROSLYN_5_12_0_OR_GREATER
    private const string _expectedInheritableAspectPayloadHash = "29c76b28490f997fb574700c0f6b2c19f984124251d79bf77a3dc7ad6d899b4a";
#elif NET
    private const string _expectedInheritableAspectPayloadHash = "c75fcb4929ccdb96c0f68a3e6182ffb766a71fbfe0233a880aedc7c8c266ffff";
#else
    private const string _expectedInheritableAspectPayloadHash = "dcd7d6dc596af956e7d0de95a95f1837ecc8d2fd37103efac7c7191472bfba5b";
#endif

    private static readonly string[] _aspectNames = ["Tracked", "Audited", "Versioned"];

    [Fact]
    public async Task InheritableAspectResource_DoesNotDependOnSyntaxTreeOrder()
    {
        using var testContext = this.CreateTestContext();
        using var domain = testContext.Domain;

        var files = new List<KeyValuePair<string, string>> { new( "Aspects.cs", _aspectCode ) };

        for ( var i = 0; i < 16; i++ )
        {
            files.Add(
                new KeyValuePair<string, string>(
                    $"Contract{i:00}.cs",
                    $"[{_aspectNames[i % _aspectNames.Length]}] public interface IContract{i:00} {{ }} public class Contract{i:00} : IContract{i:00} {{ }}" ) );
        }

        var forward = await GetResourcesAsync( testContext, files );
        var backward = await GetResourcesAsync( testContext, Enumerable.Reverse( files ) );

        AssertSameResources( forward, backward );

        var payload = Inflate( forward[CompileTimeConstants.InheritableAspectManifestResourceName] );
        Assert.Equal( _expectedInheritableAspectPayloadHash, ComputeHash( payload ) );
    }

    /// <summary>
    /// Verifies that the inheritable aspect manifest does not depend on the order of several instances of one aspect class that
    /// share a target declaration.
    /// </summary>
    /// <remarks>
    /// <c>IBoth</c> inherits one instance of <c>TaggedAttribute</c> from each of its two base interfaces, and the two instances
    /// have different values. They have the same aspect class and the same target declaration, so only their predecessors
    /// distinguish them.
    /// </remarks>
    [Fact]
    public async Task InheritableAspectResource_InstancesSharingATarget_DoNotDependOnSyntaxTreeOrder()
    {
        using var testContext = this.CreateTestContext();
        using var domain = testContext.Domain;

        var files = new List<KeyValuePair<string, string>>
        {
            new(
                "Aspects.cs",
                """
                using Metalama.Framework.Aspects;

                [Inheritable]
                public class TaggedAttribute : TypeAspect
                {
                    public TaggedAttribute( string tag )
                    {
                        this.Tag = tag;
                    }

                    public string Tag { get; }
                }
                """ ),
            new( "IFirst.cs", """[Tagged( "First" )] public interface IFirst { }""" ),
            new( "ISecond.cs", """[Tagged( "Second" )] public interface ISecond { }""" ),
            new( "IBoth.cs", "public interface IBoth : IFirst, ISecond { }" )
        };

        var forward = await GetResourcesAsync( testContext, files );
        var backward = await GetResourcesAsync( testContext, Enumerable.Reverse( files ) );

        AssertSameResources( forward, backward );
    }

    private static async Task<IReadOnlyDictionary<string, byte[]>> GetResourcesAsync(
        MetalamaTestContext testContext,
        IEnumerable<KeyValuePair<string, string>> orderedFiles )
    {
        var compilation = testContext.CreateCSharpCompilation( orderedFiles.ToDictionary( x => x.Key, x => x.Value ), assemblyName: "Producer" );

        var pipeline = new CompileTimeAspectPipeline( testContext.ServiceProvider );
        var diagnostics = new DiagnosticBag();

        var result = await pipeline.ExecuteAsync( diagnostics.Report, null, compilation, ImmutableArray<ManagedResource>.Empty );

        Assert.True( result.IsSuccessful, string.Join( "\n", diagnostics.ToImmutableArray() ) );

        var peStream = new MemoryStream();

        var emitResult = result.Value.ResultingCompilation.Compilation.Emit(
            peStream,
            manifestResources: result.Value.AdditionalResources.Select( x => x.Resource ) );

        Assert.True( emitResult.Success );

        return ReadManagedResources( peStream.ToArray() );
    }

    private static void AssertSameResources( IReadOnlyDictionary<string, byte[]> expected, IReadOnlyDictionary<string, byte[]> actual )
    {
        Assert.Contains( expected.Keys, name => name.Contains( "Inheritable" ) );
        Assert.Equal( expected.Keys.OrderBy( x => x ), actual.Keys.OrderBy( x => x ) );

        foreach ( var name in expected.Keys )
        {
            Assert.True( expected[name].SequenceEqual( actual[name] ), $"The bytes of the resource '{name}' differ." );
        }
    }

    private static byte[] Inflate( byte[] compressed )
    {
        using var deflateStream = new DeflateStream( new MemoryStream( compressed ), CompressionMode.Decompress );
        var inflated = new MemoryStream();
        deflateStream.CopyTo( inflated );

        return inflated.ToArray();
    }

    private static string ComputeHash( byte[] bytes )
    {
        using var sha256 = SHA256.Create();

        return BitConverter.ToString( sha256.ComputeHash( bytes ) ).Replace( "-", "" ).ToLowerInvariant();
    }

    private static IReadOnlyDictionary<string, byte[]> ReadManagedResources( byte[] peImage )
    {
        using var peReader = new PEReader( ImmutableArray.Create( peImage ) );
        var metadataReader = peReader.GetMetadataReader();
        var resourcesDirectory = peReader.PEHeaders.CorHeader!.ResourcesDirectory;

        var resources = new Dictionary<string, byte[]>();

        foreach ( var handle in metadataReader.ManifestResources )
        {
            var resource = metadataReader.GetManifestResource( handle );
            var name = metadataReader.GetString( resource.Name );

            // An embedded resource is stored as a 32-bit length followed by the content, at the given offset of the resources directory.
            var reader = peReader.GetSectionData( resourcesDirectory.RelativeVirtualAddress + (int) resource.Offset ).GetReader();
            var length = reader.ReadInt32();

            resources[name] = reader.ReadBytes( length );
        }

        return resources;
    }
}
