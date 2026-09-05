// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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
/// <see cref="MetadataReference.CreateFromImage(System.Collections.Immutable.ImmutableArray{byte},MetadataReferenceProperties,Microsoft.CodeAnalysis.DocumentationProvider,string)"/>
/// returns a <see cref="PortableExecutableReference"/> whose <see cref="MetadataReference.Display"/> is not a file path
/// and whose <see cref="PortableExecutableReference.FilePath"/> is <c>null</c>. A host can add such a reference to the
/// compilation it gives to the design-time analyzer. The repository used to throw an assertion for that reference, so
/// the whole design-time pipeline aborted and the project lost aspect support.
/// </remarks>
public sealed class CompileTimeProjectRepositoryInMemoryReferenceTests : UnitTestClass
{
    private const string _referencedCode = "public class ReferencedClass { }";

    private const string _mainCode = """
                                     using Metalama.Framework.Advising;
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
    /// Compiles <paramref name="assemblyName"/> and returns a reference to the resulting metadata image, without writing
    /// the assembly to disk.
    /// </summary>
    private static PortableExecutableReference CreateInMemoryReference( TestContext testContext, string assemblyName )
    {
        var compilation = testContext.CreateCSharpCompilation( _referencedCode, assemblyName: assemblyName );

        using var stream = new MemoryStream();
        var emitResult = compilation.Emit( stream );

        if ( !emitResult.Success )
        {
            throw new InvalidOperationException(
                $"Cannot emit '{assemblyName}': {string.Join( ", ", emitResult.Diagnostics.Select( d => d.ToString() ) )}" );
        }

        var reference = MetadataReference.CreateFromImage( stream.ToArray() );

        // The reference must really be the kind that the design-time host supplies, otherwise the test does not cover
        // the intended path.
        Assert.Null( reference.FilePath );
        Assert.Equal( "Microsoft.CodeAnalysis.MetadataImageReference", reference.GetType().FullName );

        return reference;
    }
}
