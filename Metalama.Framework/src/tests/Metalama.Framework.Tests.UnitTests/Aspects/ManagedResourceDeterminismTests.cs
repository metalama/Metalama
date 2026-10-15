// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Compiler;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Pipeline.CompileTime;
using Metalama.Testing.UnitTesting;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
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

        async Task<IReadOnlyDictionary<string, byte[]>> GetResourcesAsync( IEnumerable<KeyValuePair<string, string>> orderedFiles )
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

        var forward = await GetResourcesAsync( files );
        var backward = await GetResourcesAsync( Enumerable.Reverse( files ) );

        Assert.Contains( forward.Keys, name => name.Contains( "Inheritable" ) );
        Assert.Equal( forward.Keys.OrderBy( x => x ), backward.Keys.OrderBy( x => x ) );

        foreach ( var name in forward.Keys )
        {
            Assert.True( forward[name].SequenceEqual( backward[name] ), $"The bytes of the resource '{name}' differ." );
        }
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
