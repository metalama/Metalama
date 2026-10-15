// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Compiler;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.Collections;
using Metalama.Framework.Engine.CompileTime;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.HierarchicalOptions;
using Metalama.Framework.Engine.Pipeline.CompileTime;
using Metalama.Framework.Options;
using Metalama.Framework.Tests.UnitTestHelpers.Mocks;
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
#if ROSLYN_4_12_0 && NET
    private const string _expectedInheritableAspectPayloadHash = "308b7434df442bb333dae96f50f29c911a5b59286a48bd850a519a185ba832f6";
#elif ROSLYN_4_12_0
    private const string _expectedInheritableAspectPayloadHash = "9dac17200e34586d02597e250fa489d9e04a6fed2f9da1a7e5dc5ac1f1b67a6b";
#elif NET
    private const string _expectedInheritableAspectPayloadHash = "ebdac4094ec0a5faf2763f888671e9abd8a12cee3f9557a6782129f3ec6803e2";
#else
    private const string _expectedInheritableAspectPayloadHash = "37e6bcf607d5042a1440ca2d4ed64aed47a98e736ec0fc51cff6baa054b80edb";
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
    /// Verifies that the serialized manifest does not depend on the order of several instances of one aspect class that share a
    /// target declaration.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A type can inherit several instances of one aspect class, for example one from each of two base interfaces, and these
    /// instances can have different values. Their target declarations have the same identifier, so a sort by target identifier
    /// alone keeps the order of the input, and the pipeline does not produce these instances in a defined order.
    /// </para>
    /// <para>
    /// The design-time pipeline keeps a single instance for each target declaration, so the test moves the instance of
    /// <c>ISecond</c> to the target declaration of the instance of <c>IFirst</c>. It then builds the manifest from the two
    /// instances in both orders.
    /// </para>
    /// </remarks>
    [Fact]
    public void InheritableAspectManifest_InstancesSharingATarget_DoNotDependOnInputOrder()
    {
        using var testContext = this.CreateTestContext();
        using var factory = new TestDesignTimeAspectPipelineFactory( testContext );

        const string code = """
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

                            [Tagged( "First" )]
                            public interface IFirst { }

                            [Tagged( "Second" )]
                            public interface ISecond { }
                            """;

        var compilation = testContext.CreateCSharpCompilation( code );
        Assert.True( factory.TryExecute( testContext.ProjectOptions, compilation, default, out var result ) );

        var serviceProvider = result.Result.Configuration!.ServiceProvider;
        var instances = result.Result.GetInheritableAspects( "TaggedAttribute" ).ToList();

        var first = Assert.Single( instances, i => i.TargetDeclaration.ToSerializableId().Id == "Y:global::IFirst!" );
        var second = Assert.Single( instances, i => i.TargetDeclaration.ToSerializableId().Id == "Y:global::ISecond!" );
        var secondOnFirst = new InheritableAspectInstance( new RetargetedAspectInstance( second, first.TargetDeclaration ) );

        byte[] Serialize( params InheritableAspectInstance[] orderedInstances )
            => TransitiveAspectsManifest.Create(
                    orderedInstances.ToImmutableArray(),
                    ImmutableArray<ITransitiveAspectsManifestExtension>.Empty,
                    ImmutableDictionary<HierarchicalOptionsKey, IHierarchicalOptions>.Empty,
                    ImmutableDictionaryOfArray<SerializableDeclarationId, IAnnotation>.Empty,
                    false )
                .ToBytes( serviceProvider, compress: false );

        Assert.Equal( Serialize( first, secondOnFirst ), Serialize( secondOnFirst, first ) );
    }

    private static async Task<IReadOnlyDictionary<string, byte[]>> GetResourcesAsync(
        TestContext testContext,
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

    /// <summary>
    /// An aspect instance that has the values of another instance and a different target declaration.
    /// </summary>
    private sealed class RetargetedAspectInstance : IAspectInstance, IAspectPredecessorImpl
    {
        private readonly InheritableAspectInstance _instance;

        public RetargetedAspectInstance( InheritableAspectInstance instance, IRef<IDeclaration> targetDeclaration )
        {
            this._instance = instance;
            this.TargetDeclaration = targetDeclaration;
        }

        public IRef<IDeclaration> TargetDeclaration { get; }

        public IAspect Aspect => this._instance.Aspect;

        public IAspectClass AspectClass => this._instance.AspectClass;

        public bool IsSkipped => false;

        public bool IsInheritable => true;

        public ImmutableArray<IAspectInstance> SecondaryInstances => ImmutableArray<IAspectInstance>.Empty;

        public IAspectState? AspectState => this._instance.AspectState;

        public int PredecessorDegree => this._instance.PredecessorDegree;

        public ImmutableArray<AspectPredecessor> Predecessors => ImmutableArray<AspectPredecessor>.Empty;

        public int TargetDeclarationDepth => this._instance.TargetDeclarationDepth;

        public ImmutableArray<SyntaxTree> PredecessorTreeClosure => ImmutableArray<SyntaxTree>.Empty;

        public FormattableString FormatPredecessor( ICompilation compilation ) => this._instance.FormatPredecessor( compilation );

        public Location? GetDiagnosticLocation( Compilation compilation ) => null;
    }
}
