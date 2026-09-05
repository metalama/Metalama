// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Utilities;
using Metalama.Compiler;
using Metalama.Framework.Engine;
using Metalama.Framework.Engine.CompileTime;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Pipeline.CompileTime;
using Metalama.Framework.Tests.UnitTestHelpers.Mocks;
using Metalama.Testing.UnitTesting;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Emit;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Framework.Tests.UnitTests.CompileTime;

#pragma warning disable VSTHRD200

/// <summary>
/// Tests that a compilation keeps its aspect support when one of its references has no file path. See issue #1960.
/// </summary>
/// <remarks>
/// <para>
/// Roslyn gives a compilation a reference that has no <see cref="PortableExecutableReference.FilePath"/> when a project
/// reference crosses a language boundary. <c>SolutionCompilationState.GetMetadataReferenceAsync</c> returns a
/// <see cref="CompilationReference"/> when the referencing and the referenced project share their language services, and
/// otherwise emits the referenced project metadata-only into memory and wraps it without a file path. That reference is
/// called a skeleton reference. A build never produces one, because MSBuild passes every reference as a path, which is
/// why every report of this issue comes from the design-time pipeline.
/// </para>
/// <para>
/// Two sites used to throw an assertion for such a reference:
/// <c>CompileTimeProjectRepository.Builder.TryGetCompileTimeProject</c>, which aborted the initialization of the
/// pipeline, and <c>TransitivePipelineContributorSource.Create</c>, which aborted its execution. Both now skip the
/// reference, so the project keeps its aspect support.
/// </para>
/// </remarks>
public sealed class SkeletonMetadataReferenceTests : UnitTestClass
{
    public SkeletonMetadataReferenceTests( ITestOutputHelper testOutput ) : base( testOutput ) { }

    /// <summary>
    /// The source of the project that the skeleton reference stands for. It declares a run-time class and no
    /// compile-time code, which is the case of a project of another language than the consuming one.
    /// </summary>
    private const string _referencedCode = "public class ReferencedClass { }";

    private const string _compileTimeReferencedCode = """
                                                      using Metalama.Framework.Aspects;

                                                      [assembly: CompileTime]

                                                      public class CompileTimeReferencedClass { }
                                                      """;

    /// <summary>
    /// The name of the member that the aspect introduces, so that a test can tell that the aspect was really applied and
    /// not merely that the pipeline reported success.
    /// </summary>
    private const string _aspectMarker = "MyAspectWasApplied";

    /// <summary>
    /// The aspect and its target are in two files, because a syntax tree that declares an aspect is compile-time code and
    /// the design-time pipeline reports no result for it.
    /// </summary>
    private static readonly IReadOnlyDictionary<string, string> _mainCode = new Dictionary<string, string>
    {
        ["Aspect.cs"] = $$"""
                          using Metalama.Framework.Aspects;

                          public class MyAspect : TypeAspect
                          {
                              [Introduce]
                              public void {{_aspectMarker}}() { }
                          }
                          """,
        // The target is partial, because the design-time pipeline reports an introduction as a partial declaration of the
        // target type.
        ["TargetClass.cs"] = """
                             [MyAspect]
                             public partial class TargetClass
                             {
                                 public void Method() { }
                             }
                             """
    };

    /// <summary>
    /// Verifies that the repository is created for a compilation that has a skeleton reference.
    /// </summary>
    [Fact]
    public void RepositoryIsCreatedWhenReferenceHasNoFilePath()
    {
        using var testContext = this.CreateTestContext();
        using var domain = testContext.Domain;

        var skeletonReference = CreateSkeletonReference( testContext, "Metalama.Tests.SkeletonReference" );

        var compilation = testContext.CreateCSharpCompilation( _mainCode, additionalReferences: [skeletonReference] );

        var repository = CompileTimeProjectRepository.Create( domain, testContext.ServiceProvider, compilation );

        Assert.NotNull( repository );

        // The aspect declared by the compilation itself must remain available, which is what the design-time pipeline
        // loses when the initialization of the repository fails.
        Assert.NotNull( repository.RootProject );
    }

    /// <summary>
    /// Verifies that the skeleton reference is the only one that is skipped, and that a compile-time project referenced
    /// through a file is still part of the closure.
    /// </summary>
    [Fact]
    public void OtherReferencesAreStillResolvedWhenOneReferenceHasNoFilePath()
    {
        using var testContext = this.CreateTestContext();
        using var domain = testContext.Domain;

        var skeletonReference = CreateSkeletonReference( testContext, "Metalama.Tests.SkeletonReference" );

        var compileTimeReferencePath = MetalamaPathUtilities.GetTempFileName();

        try
        {
            var compileTimeReference = CreateCompileTimeReference(
                testContext,
                domain,
                "Metalama.Tests.CompileTimeReference",
                compileTimeReferencePath );

            var compilation = testContext.CreateCSharpCompilation(
                _mainCode,
                additionalReferences: [skeletonReference, compileTimeReference] );

            var repository = CompileTimeProjectRepository.Create( domain, testContext.ServiceProvider, compilation ).AssertNotNull();

            Assert.Contains(
                repository.RootProject.ClosureProjects,
                project => project.RunTimeIdentity.Name == "Metalama.Tests.CompileTimeReference" );
        }
        finally
        {
            if ( File.Exists( compileTimeReferencePath ) )
            {
                File.Delete( compileTimeReferencePath );
            }
        }
    }

    /// <summary>
    /// Verifies that the whole compile-time pipeline runs and applies the aspect for a compilation that has a skeleton
    /// reference. This covers <c>TransitivePipelineContributorSource.Create</c>, which runs on every pipeline execution
    /// and which the two tests above do not reach, because they only build the repository.
    /// </summary>
    [Fact]
    public async Task CompileTimePipelineRunsWhenReferenceHasNoFilePath()
    {
        using var testContext = this.CreateTestContext();

        var skeletonReference = CreateSkeletonReference( testContext, "Metalama.Tests.SkeletonReference" );

        var compilation = testContext.CreateCSharpCompilation( _mainCode, additionalReferences: [skeletonReference] );

        var pipeline = new CompileTimeAspectPipeline( testContext.ServiceProvider );
        var diagnostics = new DiagnosticBag();

        var result = await pipeline.ExecuteAsync( diagnostics.Report, null, compilation, ImmutableArray<ManagedResource>.Empty );

        foreach ( var diagnostic in diagnostics )
        {
            this.TestOutput.WriteLine( diagnostic.ToString() );
        }

        Assert.True( result.IsSuccessful );
        Assert.DoesNotContain( diagnostics.ToImmutableArray(), d => d.Severity == DiagnosticSeverity.Error );

        // The aspect must really have been applied, otherwise the pipeline would report success while silently having
        // lost the aspect support that this issue is about.
        var transformedCode = string.Join(
            Environment.NewLine,
            result.Value.ResultingCompilation.SyntaxTreeCollection.SelectAsArray( t => t.GetText().ToString() ) );

        Assert.Contains( _aspectMarker, transformedCode, StringComparison.Ordinal );
    }

    /// <summary>
    /// Verifies that the design-time pipeline, which is the one that every crash report comes from, executes for a
    /// compilation that has a skeleton reference.
    /// </summary>
    [Fact]
    public void DesignTimePipelineRunsWhenReferenceHasNoFilePath()
    {
        using var testContext = this.CreateTestContext();

        var skeletonReference = CreateSkeletonReference( testContext, "Metalama.Tests.SkeletonReference" );

        var compilation = testContext.CreateCSharpCompilation( _mainCode, additionalReferences: [skeletonReference] );

        using var pipelineFactory = new TestDesignTimeAspectPipelineFactory( testContext );

        Assert.True( pipelineFactory.TryExecute( testContext.ProjectOptions, compilation, default, out var results ) );

        // The introduction must be reported to the editor, which is the aspect support that is lost when the pipeline
        // aborts.
        var introducedCode = string.Join(
            Environment.NewLine,
            results.Result.SyntaxTreeResults.Values.SelectMany( r => r.Introductions ).Select( i => i.GeneratedSyntaxTree.ToString() ) );

        Assert.Contains( _aspectMarker, introducedCode, StringComparison.Ordinal );
    }

    /// <summary>
    /// Builds the reference that Roslyn builds for a project reference that crosses a language boundary: the referenced
    /// project is emitted metadata-only into memory, and the metadata is wrapped in a
    /// <see cref="PortableExecutableReference"/> that has no <see cref="PortableExecutableReference.FilePath"/>.
    /// </summary>
    /// <remarks>
    /// The steps are the ones of <c>SolutionCompilationState.SkeletonReferenceCache.CreateAndTrackSkeletonReference</c>
    /// and <c>SolutionCompilationState.SkeletonReferenceSet.GetOrCreateMetadataReference</c>: an emit with
    /// <see cref="EmitOptions.EmitMetadataOnly"/> and without private members, then a reference that carries a display
    /// name and no file path. The source is compiled as C#, because this test project does not reference the Visual Basic
    /// language services. The language of the referenced project has no effect on what is tested here, because the
    /// reference that reaches Metalama carries metadata and nothing else.
    /// </remarks>
    private static PortableExecutableReference CreateSkeletonReference( TestContext testContext, string assemblyName )
    {
        var compilation = testContext.CreateCSharpCompilation( _referencedCode, assemblyName: assemblyName );

        using var stream = new MemoryStream();

        var emitResult = compilation.Emit( stream, options: new EmitOptions( metadataOnly: true, includePrivateMembers: false ) );

        if ( !emitResult.Success )
        {
            throw new InvalidOperationException(
                $"Cannot emit '{assemblyName}': {string.Join( ", ", emitResult.Diagnostics.Select( d => d.ToString() ) )}" );
        }

        var reference = AssemblyMetadata.CreateFromImage( stream.ToArray() ).GetReference( display: assemblyName );

        // The reference must have the shape that the design-time host supplies, otherwise the test does not cover the
        // intended path.
        Assert.Null( reference.FilePath );
        Assert.Equal( "Microsoft.CodeAnalysis.MetadataImageReference", reference.GetType().FullName );

        // A skeleton carries no manifest resource, because Roslyn passes none to that emit. The production code does not
        // rely on this, and the assertion is here so that the test models a skeleton rather than an ordinary assembly.
        Assert.Empty( GetManifestResourceNames( reference ) );

        return reference;
    }

    /// <summary>
    /// Returns the names of the manifest resources of a reference. The names are in the metadata tables, so reading them
    /// needs no access to the image of the referenced assembly.
    /// </summary>
    private static IEnumerable<string> GetManifestResourceNames( PortableExecutableReference reference )
    {
        var assemblyMetadata = (AssemblyMetadata) reference.GetMetadata();

        foreach ( var module in assemblyMetadata.GetModules() )
        {
            var metadataReader = module.GetMetadataReader();

            foreach ( var handle in metadataReader.ManifestResources )
            {
                yield return metadataReader.GetString( metadataReader.GetManifestResource( handle ).Name );
            }
        }
    }

    /// <summary>
    /// Compiles <paramref name="assemblyName"/> as a compile-time assembly, writes it to <paramref name="path"/> with its
    /// compile-time project resource, and returns a reference to that file.
    /// </summary>
    private static PortableExecutableReference CreateCompileTimeReference(
        TestContext testContext,
        CompileTimeDomain domain,
        string assemblyName,
        string path )
    {
        var compilation = testContext.CreateCSharpCompilation( _compileTimeReferencedCode, assemblyName: assemblyName );

        var repository = CompileTimeProjectRepository.Create( domain, testContext.ServiceProvider, compilation ).AssertNotNull();

        // The assembly must be on disk, because this is the path that the repository takes for a file-backed reference.
        using ( var stream = File.Create( path ) )
        {
            Emit( compilation, stream, assemblyName, repository.RootProject.ToResource().Resource );
        }

        return MetadataReference.CreateFromFile( path );
    }

    private static void Emit( Compilation compilation, Stream stream, string assemblyName, params ResourceDescription[] resources )
    {
        var emitResult = compilation.Emit( stream, manifestResources: resources );

        if ( !emitResult.Success )
        {
            throw new InvalidOperationException(
                $"Cannot emit '{assemblyName}': {string.Join( ", ", emitResult.Diagnostics.Select( d => d.ToString() ) )}" );
        }
    }
}
