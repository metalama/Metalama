// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.CompileTime;
using Metalama.Testing.UnitTesting;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.IO;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.CompileTime;

public sealed class CompileTimeDomainTests : UnitTestClass
{
    /// <summary>
    /// Regression test for issue #579: Loading a different version of an extension assembly with the same simple name
    /// into an existing CompileTimeDomain should not throw FileLoadException.
    /// </summary>
    [Fact]
    public void LoadAssembly_SameNameDifferentVersion_ShouldNotThrow()
    {
        using var testContext = this.CreateTestContext();
        using var domain = testContext.Domain;

        var tempDir = Path.Combine( Path.GetTempPath(), "Metalama.Tests", Guid.NewGuid().ToString() );
        Directory.CreateDirectory( tempDir );

        try
        {
            // Create two assemblies with the same simple name but different versions.
            var assemblyName = "TestExtension";

            var path1 = CreateAssemblyOnDisk( tempDir, assemblyName, new Version( 1, 0, 0, 0 ), "public class V1 {}" );
            var path2 = CreateAssemblyOnDisk( tempDir, assemblyName, new Version( 2, 0, 0, 0 ), "public class V2 {}" );

            // Load the first version - should succeed.
            var assembly1 = domain.LoadAssembly( path1, null, new LoadAssemblyOptions { IsShared = true } );
            Assert.NotNull( assembly1 );
            Assert.Equal( assemblyName, assembly1.GetName().Name );

            // Load the second version with the same simple name - this is the scenario from issue #579.
            // Before the fix, this throws FileLoadException: "Assembly with same name is already loaded".
            // After the fix, the domain should handle this gracefully.
            var assembly2 = domain.LoadAssembly( path2, null, new LoadAssemblyOptions { IsShared = true } );
            Assert.NotNull( assembly2 );
        }
        finally
        {
            try
            {
                Directory.Delete( tempDir, true );
            }
            catch
            {
                // Ignore cleanup errors.
            }
        }
    }

    private static string CreateAssemblyOnDisk( string directory, string assemblyName, Version version, string code )
    {
        var compilation = CSharpCompilation.Create(
            assemblyName,
            [CSharpSyntaxTree.ParseText( $"[assembly: System.Reflection.AssemblyVersion(\"{version}\")]\n{code}" )],
            [MetadataReference.CreateFromFile( typeof(object).Assembly.Location )],
            new CSharpCompilationOptions( OutputKind.DynamicallyLinkedLibrary ) );

        // Use a unique file name per version to avoid file conflicts.
        var path = Path.Combine( directory, $"{assemblyName}.{version}.dll" );

        using var peStream = File.Create( path );
        var result = compilation.Emit( peStream );

        if ( !result.Success )
        {
            throw new InvalidOperationException(
                $"Failed to emit assembly: {string.Join( ", ", result.Diagnostics )}" );
        }

        return path;
    }
}
