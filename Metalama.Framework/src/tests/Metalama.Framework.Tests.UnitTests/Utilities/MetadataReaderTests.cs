// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Utilities;
using Metalama.Framework.Engine.Utilities;
using Metalama.Testing.UnitTesting;
using System.IO;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.Utilities;

public sealed class MetadataReaderTests : UnitTestClass
{
    // Regression test for #1664. Reading the metadata of an assembly that has an assembly-level
    // generic attribute (C# 11) used to throw InvalidCastException in MetadataReader.GetMetadataCore,
    // because the constructor of a constructed generic attribute is referenced through a
    // TypeSpecification, while the code cast MemberReference.Parent to TypeReferenceHandle unconditionally.
    [Theory]

    // Parameterless constructor of a constructed generic attribute.
    [InlineData( "[assembly: GenAttribute<int>]" )]

    // Constructor with an argument, which exercises a different member-reference signature.
    [InlineData( "[assembly: GenAttribute<int>( 42 )]" )]

    // A generic attribute placed before a regular attribute, to ensure the scan continues past it.
    [InlineData( "[assembly: GenAttribute<int>]\n[assembly: System.CLSCompliant( false )]" )]
    public void AssemblyLevelGenericAttributeDoesNotThrow( string assemblyAttributes )
    {
        using var testContext = this.CreateTestContext();

        var compilation = testContext.CreateCSharpCompilation(
            assemblyAttributes +
            @"

[System.AttributeUsage( System.AttributeTargets.Assembly, AllowMultiple = true )]
public class GenAttribute<T> : System.Attribute
{
    public GenAttribute() { }

    public GenAttribute( int x ) { }
}
" );

        var assemblyPath = MetalamaPathUtilities.GetTempFileName();

        try
        {
            // We must create the dll on disk because MetadataReader reads from a file path.
            using ( var stream = File.Create( assemblyPath ) )
            {
                var emitResult = compilation.Emit( stream );
                Assert.True( emitResult.Success );
            }

            // This used to throw InvalidCastException before the fix for #1664.
            Assert.True( MetadataReader.TryGetMetadata( assemblyPath, out var metadata ) );
            Assert.NotNull( metadata );

            // The generic attribute is not [CompileTime], so the assembly must not be flagged as compile-time.
            Assert.False( metadata.HasCompileTimeAttribute );
        }
        finally
        {
            if ( File.Exists( assemblyPath ) )
            {
                File.Delete( assemblyPath );
            }
        }
    }
}
