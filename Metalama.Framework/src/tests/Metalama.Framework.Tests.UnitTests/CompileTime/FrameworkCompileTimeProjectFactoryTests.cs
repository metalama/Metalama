// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.CompileTime;
using Metalama.Framework.Engine.CompileTime.Manifest;
using Metalama.Testing.UnitTesting;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.CompileTime;

/// <summary>
/// Tests the creation of the compile-time project that represents <c>Metalama.Framework</c> itself.
/// </summary>
/// <remarks>
/// The manifest of that project used to be indexed by the target framework moniker read from the
/// <see cref="TargetFrameworkAttribute"/> of the referenced assembly, and the read was an assertion. It is now indexed
/// by the path of the metadata reference, which identifies the assembly exactly and requires no attribute. See issue
/// #1820.
/// </remarks>
public sealed class FrameworkCompileTimeProjectFactoryTests : UnitTestClass
{
    private static string FrameworkAssemblyPath => typeof(IAspect).Assembly.Location;

    private static CompileTimeProject CreateFrameworkProject( TestContext testContext, Compilation compilation )
    {
        var factory = testContext.ServiceProvider.Global.GetRequiredService<FrameworkCompileTimeProjectFactory>();

        return factory.CreateFrameworkProject( testContext.ServiceProvider, testContext.Domain, compilation );
    }

    /// <summary>
    /// Verifies that the framework compile-time project is created from a compilation that cannot resolve
    /// <see cref="TargetFrameworkAttribute"/>, and therefore cannot supply the target framework moniker of
    /// <c>Metalama.Framework</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The compilation of this test references <c>Metalama.Framework</c> and nothing else, which is enough for Roslyn to
    /// expose the attribute, whose name it takes from the type reference of that assembly, while leaving the constructor
    /// of the attribute unresolved and its arguments therefore empty. An integrated development environment produces a
    /// compilation of that shape transiently, while a project is being loaded or restored, and it used to abort the
    /// initialization of the design-time pipeline for the whole project.
    /// </para>
    /// </remarks>
    [Fact]
    public void ProjectIsCreatedWhenTargetFrameworkAttributeCannotBeResolved()
    {
        using var testContext = this.CreateTestContext();

        var compilation = CSharpCompilation.Create(
            "test",
            references: new[] { MetadataReference.CreateFromFile( FrameworkAssemblyPath ) } );

        var assembly = compilation.SourceModule.ReferencedAssemblySymbols.Single( a => a.Name == "Metalama.Framework" );

        // The premise of the test: the attribute is present, but it carries no argument that could be read.
        var attribute = assembly.GetAttributes().Single( a => a.AttributeClass?.Name == nameof(TargetFrameworkAttribute) );
        Assert.Empty( attribute.ConstructorArguments );

        var project = CreateFrameworkProject( testContext, compilation );

        Assert.Equal( "Metalama.Framework", project.RunTimeIdentity.Name );
    }

    /// <summary>
    /// Verifies that the framework compile-time project is created when the reference to <c>Metalama.Framework</c> has
    /// no path, in which case the manifest cannot be indexed and is built for that compilation only.
    /// </summary>
    /// <remarks>
    /// A reference created from an image has no path, as does the <see cref="CompilationReference"/> that a project
    /// reference to <c>Metalama.Framework</c> takes at design time in a solution that builds it.
    /// </remarks>
    [Fact]
    public void ProjectIsCreatedWhenTheReferenceHasNoPath()
    {
        using var testContext = this.CreateTestContext();

        var reference = MetadataReference.CreateFromImage( File.ReadAllBytes( FrameworkAssemblyPath ) );

        Assert.Null( reference.FilePath );

        var compilation = CSharpCompilation.Create( "test", references: new[] { reference } );

        var project = CreateFrameworkProject( testContext, compilation );

        Assert.Equal( "Metalama.Framework", project.RunTimeIdentity.Name );
    }

    /// <summary>
    /// Verifies that two compilations referencing the same assembly file share the manifest of the framework
    /// compile-time project, which is the purpose of indexing it.
    /// </summary>
    [Fact]
    public void ManifestIsSharedBetweenCompilationsReferencingTheSameFile()
    {
        using var testContext = this.CreateTestContext();

        var project1 = CreateFrameworkProject( testContext, testContext.CreateCSharpCompilation( "", assemblyName: "test1" ) );
        var project2 = CreateFrameworkProject( testContext, testContext.CreateCSharpCompilation( "", assemblyName: "test2" ) );

        Assert.NotNull( project1.Manifest );
        Assert.Same( project1.Manifest, project2.Manifest );
    }

    /// <summary>
    /// Verifies that an assembly rebuilt at a path already in the index is not served from the entry that describes the
    /// previous build.
    /// </summary>
    [Fact]
    public void ManifestIsRebuiltWhenTheFileIsWrittenAgain()
    {
        using var testContext = this.CreateTestContext();

        var path = Path.Combine( testContext.BaseDirectory, "Metalama.Framework.dll" );
        File.Copy( FrameworkAssemblyPath, path );

        CompileTimeProjectManifest? CreateManifest()
            => CreateFrameworkProject(
                    testContext,
                    CSharpCompilation.Create( "test", references: new[] { MetadataReference.CreateFromFile( path ) } ) )
                .Manifest;

        var manifest = CreateManifest();

        // The file is unchanged, so the entry is used.
        Assert.Same( manifest, CreateManifest() );

        File.SetLastWriteTime( path, File.GetLastWriteTime( path ).AddHours( 1 ) );

        Assert.NotSame( manifest, CreateManifest() );
    }
}
