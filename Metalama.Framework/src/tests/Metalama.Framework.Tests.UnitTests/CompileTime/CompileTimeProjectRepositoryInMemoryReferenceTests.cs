// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Utilities;
using Metalama.Framework.Engine;
using Metalama.Framework.Engine.CompileTime;
using Metalama.Testing.UnitTesting;
using Microsoft.CodeAnalysis;
using System;
using System.IO;
using System.Linq;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.CompileTime;

/// <summary>
/// Tests the creation of a <see cref="CompileTimeProjectRepository"/> for a compilation that references an assembly
/// supplied as bytes instead of as a file on disk. See issue #1960.
/// </summary>
/// <remarks>
/// <c>MetadataReference.CreateFromImage</c> and <c>MetadataReference.CreateFromStream</c> return a
/// <see cref="PortableExecutableReference"/> whose <see cref="PortableExecutableReference.FilePath"/> is <c>null</c>.
/// A host can add such a reference to the compilation that it gives to the design-time analyzer. The repository used to
/// throw an assertion for that reference, so the whole design-time pipeline aborted and the project lost aspect support.
/// </remarks>
public sealed class CompileTimeProjectRepositoryInMemoryReferenceTests : UnitTestClass
{
    private const string _referencedCode = "public class ReferencedClass { }";

    private const string _compileTimeReferencedCode = """
                                                      using Metalama.Framework.Aspects;

                                                      [assembly: CompileTime]

                                                      public class CompileTimeReferencedClass { }
                                                      """;

    private const string _mainCode = """
                                     using Metalama.Framework.Aspects;

                                     public class MyAspect : OverrideMethodAspect
                                     {
                                         public override dynamic? OverrideMethod() => meta.Proceed();
                                     }

                                     public class TargetClass
                                     {
                                         [MyAspect]
                                         public void Method() { }
                                     }
                                     """;

    /// <summary>
    /// Verifies that the repository is created for a compilation that has a reference created from a metadata image.
    /// </summary>
    [Fact]
    public void RepositoryIsCreatedWhenReferenceHasNoFilePath()
    {
        using var testContext = this.CreateTestContext();
        using var domain = testContext.Domain;

        var inMemoryReference = CreateInMemoryReference( testContext, "Metalama.Tests.InMemoryReference" );

        var compilation = testContext.CreateCSharpCompilation( _mainCode, additionalReferences: [inMemoryReference] );

        var repository = CompileTimeProjectRepository.Create( domain, testContext.ServiceProvider, compilation );

        Assert.NotNull( repository );

        // The aspect declared by the compilation itself must remain available, which is what the design-time pipeline
        // loses when the initialization of the repository fails.
        Assert.NotNull( repository.RootProject );
    }

    /// <summary>
    /// Verifies that the reference that has no file path is the only one that is skipped, and that a compile-time project
    /// referenced through a file is still part of the closure.
    /// </summary>
    [Fact]
    public void OtherReferencesAreStillResolvedWhenOneReferenceHasNoFilePath()
    {
        using var testContext = this.CreateTestContext();
        using var domain = testContext.Domain;

        var inMemoryReference = CreateInMemoryReference( testContext, "Metalama.Tests.InMemoryReference" );

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
                additionalReferences: [inMemoryReference, compileTimeReference] );

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
    /// Compiles <paramref name="assemblyName"/> and returns a reference to the resulting metadata image, without writing
    /// the assembly to disk.
    /// </summary>
    private static PortableExecutableReference CreateInMemoryReference( TestContext testContext, string assemblyName )
    {
        var compilation = testContext.CreateCSharpCompilation( _referencedCode, assemblyName: assemblyName );

        using var stream = new MemoryStream();

        Emit( compilation, stream, assemblyName );

        var reference = MetadataReference.CreateFromImage( stream.ToArray() );

        // The reference must really be the kind that the design-time host supplies, otherwise the test does not cover
        // the intended path.
        Assert.Null( reference.FilePath );
        Assert.Equal( "Microsoft.CodeAnalysis.MetadataImageReference", reference.GetType().FullName );

        return reference;
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
