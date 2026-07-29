// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Compiler;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Pipeline;
using Metalama.Framework.Engine.Pipeline.CompileTime;
using Metalama.Framework.Tests.UnitTestHelpers.Mocks;
using Metalama.Testing.UnitTesting;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Framework.Tests.UnitTests.DesignTime.Pipeline;

#pragma warning disable VSTHRD200

/// <summary>
/// Tests that two references sharing an assembly identity do not abort pipeline initialization.
/// </summary>
/// <remarks>
/// Covers the second registration site of issue #1743. <see cref="TransitivePipelineContributorSource"/> indexes
/// the transitive aspect manifest of every reference by assembly identity. When the same library reaches a project
/// through two routes (for instance as a package and as a project reference), two references carry the same
/// identity but different manifests, and the collision-intolerant <c>ImmutableDictionary.Builder.Add</c> threw
/// <see cref="ArgumentException"/> out of <c>AspectPipeline.CreatePipelineContributorSources</c>.
/// </remarks>
public sealed class DuplicateAssemblyIdentityTests : UnitTestClass
{
    public DuplicateAssemblyIdentityTests( ITestOutputHelper testOutput ) : base( testOutput ) { }

    /// <summary>
    /// The first build of the library: an inheritable aspect and one target, so the project exports a transitive
    /// manifest.
    /// </summary>
    private const string _libraryCode = """
                                        using Metalama.Framework.Aspects;

                                        [Inheritable]
                                        public class LibraryAspect : TypeAspect { }

                                        [LibraryAspect]
                                        public class Base { }
                                        """;

    /// <summary>
    /// The second build of the library, exporting an extra target so that its manifest differs from the first one's.
    /// </summary>
    private const string _otherLibraryCode = _libraryCode + """


                                                            [LibraryAspect]
                                                            public class OtherBase { }
                                                            """;

    /// <summary>
    /// Tests that a project referencing two distinct builds of the same assembly identity, each exporting its own
    /// transitive aspect manifest, initializes its pipeline and reports <c>LAMA0078</c>.
    /// </summary>
    [Fact]
    public async Task DuplicateAssemblyIdentityInReferences_DoesNotThrow()
    {
        using var testContext = this.CreateTestContext();
        using var libraryContext = this.CreateTestContext();
        using var otherLibraryContext = this.CreateTestContext();

        // Two builds of the same library, both claiming the assembly identity 'Library'.
        var library = testContext.CreateCSharpCompilation( _libraryCode, assemblyName: "Library" );
        var otherLibrary = testContext.CreateCSharpCompilation( _otherLibraryCode, assemblyName: "Library" );

        // Produce the transitive manifest of each build by running its design-time pipeline.
        using var pipelineFactory = new TestDesignTimeAspectPipelineFactory( testContext );

        Assert.True( pipelineFactory.TryExecute( libraryContext.ProjectOptions, library, default, out var libraryResult ) );
        Assert.True( pipelineFactory.TryExecute( otherLibraryContext.ProjectOptions, otherLibrary, default, out var otherLibraryResult ) );

        var manifestProvider = new DuplicateIdentityManifestProvider(
            new Dictionary<Compilation, SerializedTransitiveAspectManifest>
            {
                [library] = libraryResult.Result.SerializedTransitiveAspectManifestWithoutValidators,
                [otherLibrary] = otherLibraryResult.Result.SerializedTransitiveAspectManifestWithoutValidators
            } );

        // The compile-time pipeline has no manifest provider of its own, so the fake one below is what makes the
        // two references carry a manifest, as they do at design time in the reported crash.
        var appServices = CreateAdditionalServiceCollection();
        appServices.AddProjectService<ITransitiveAspectManifestProvider>( manifestProvider );

        using var appContext = this.CreateTestContext( appServices );

        // The app references both builds. Roslyn reports the ambiguity, which is exactly the broken user
        // configuration this issue is about, so compilation errors are expected here.
        var app = appContext.CreateCSharpCompilation(
            "public class Derived : Base { }",
            assemblyName: "App",
            ignoreErrors: true,
            additionalReferences: [library.ToMetadataReference(), otherLibrary.ToMetadataReference()] );

        var pipeline = new CompileTimeAspectPipeline( appContext.ServiceProvider );
        var diagnostics = new DiagnosticBag();

        // Before the fix, this threw ArgumentException from ImmutableDictionary.Builder.Add.
        await pipeline.ExecuteAsync( diagnostics.Report, null, app, ImmutableArray<ManagedResource>.Empty );

        foreach ( var diagnostic in diagnostics )
        {
            this.TestOutput.WriteLine( diagnostic.ToString() );
        }

        Assert.Contains( diagnostics.ToImmutableArray(), d => d.Id == "LAMA0078" );
    }

    /// <summary>
    /// Serves the transitive manifest of the referenced compilations from a map, so that the consuming project does
    /// not need a design-time pipeline of its own.
    /// </summary>
    private sealed class DuplicateIdentityManifestProvider : ITransitiveAspectManifestProvider
    {
        private readonly IReadOnlyDictionary<Compilation, SerializedTransitiveAspectManifest> _manifests;

        public DuplicateIdentityManifestProvider( IReadOnlyDictionary<Compilation, SerializedTransitiveAspectManifest> manifests )
        {
            this._manifests = manifests;
        }

        public bool TryGetReusableTransitiveAspectsManifest(
            Compilation compilationReferenceCompilation,
            [NotNullWhen( true )] out ITransitiveAspectsManifest? manifest,
            [NotNullWhen( true )] out AspectPipelineConfiguration? producerConfiguration )
        {
            // Always take the deserializing path, so that the two references get distinct manifest objects, as they
            // do in production when the two builds differ.
            manifest = null;
            producerConfiguration = null;

            return false;
        }

        public SerializedTransitiveAspectManifest? GetSerializedTransitiveAspectsManifest( Compilation compilationReferenceCompilation )
            => this._manifests.TryGetValue( compilationReferenceCompilation, out var manifest ) ? manifest : null;
    }
}
