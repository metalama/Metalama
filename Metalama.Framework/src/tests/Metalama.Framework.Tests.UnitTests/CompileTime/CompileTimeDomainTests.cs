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
    /// Verifies that loading the same assembly (same identity) twice returns the cached instance.
    /// </summary>
    [Fact]
    public void LoadAssembly_SameIdentity_ReturnsCachedAssembly()
    {
        using var testContext = this.CreateTestContext();
        using var domain = testContext.Domain;

        var tempDir = Path.Combine( Path.GetTempPath(), "Metalama.Tests", Guid.NewGuid().ToString() );
        Directory.CreateDirectory( tempDir );

        try
        {
            var path = CreateAssemblyOnDisk( tempDir, "TestAssembly", new Version( 1, 0, 0, 0 ), "public class C {}" );

            var assembly1 = domain.LoadAssembly( path );
            var assembly2 = domain.LoadAssembly( path );

            Assert.Same( assembly1, assembly2 );
        }
        finally
        {
            TryDeleteDirectory( tempDir );
        }
    }

    /// <summary>
    /// Verifies that loading assemblies with different simple names works correctly.
    /// </summary>
    [Fact]
    public void LoadAssembly_DifferentNames_LoadsBoth()
    {
        using var testContext = this.CreateTestContext();
        using var domain = testContext.Domain;

        var tempDir = Path.Combine( Path.GetTempPath(), "Metalama.Tests", Guid.NewGuid().ToString() );
        Directory.CreateDirectory( tempDir );

        try
        {
            var path1 = CreateAssemblyOnDisk( tempDir, "ExtensionA", new Version( 1, 0, 0, 0 ), "public class A {}" );
            var path2 = CreateAssemblyOnDisk( tempDir, "ExtensionB", new Version( 1, 0, 0, 0 ), "public class B {}" );

            var assembly1 = domain.LoadAssembly( path1, null, new LoadAssemblyOptions { IsShared = true } );
            var assembly2 = domain.LoadAssembly( path2, null, new LoadAssemblyOptions { IsShared = true } );

            Assert.NotSame( assembly1, assembly2 );
            Assert.Equal( "ExtensionA", assembly1.GetName().Name );
            Assert.Equal( "ExtensionB", assembly2.GetName().Name );
        }
        finally
        {
            TryDeleteDirectory( tempDir );
        }
    }

    /// <summary>
    /// Verifies that IsCompatibleWithAssemblies returns true when assemblies are compatible.
    /// </summary>
    [Fact]
    public void IsCompatibleWithAssemblies_Compatible_ReturnsTrue()
    {
        using var testContext = this.CreateTestContext();
        using var domain = testContext.Domain;

        var tempDir = Path.Combine( Path.GetTempPath(), "Metalama.Tests", Guid.NewGuid().ToString() );
        Directory.CreateDirectory( tempDir );

        try
        {
            var path1 = CreateAssemblyOnDisk( tempDir, "ExtensionA", new Version( 1, 0, 0, 0 ), "public class A {}" );

            domain.LoadAssembly( path1, null, new LoadAssemblyOptions { IsShared = true } );

            // Same assembly path should be compatible.
            Assert.True( domain.IsCompatibleWithAssemblies( new[] { path1 } ) );
        }
        finally
        {
            TryDeleteDirectory( tempDir );
        }
    }

    /// <summary>
    /// Verifies that IsCompatibleWithAssemblies returns false when an assembly with the same name
    /// but different version is already loaded.
    /// </summary>
    [Fact]
    public void IsCompatibleWithAssemblies_VersionConflict_ReturnsFalse()
    {
        using var testContext = this.CreateTestContext();
        using var domain = testContext.Domain;

        var tempDir = Path.Combine( Path.GetTempPath(), "Metalama.Tests", Guid.NewGuid().ToString() );
        Directory.CreateDirectory( tempDir );

        try
        {
            var path1 = CreateAssemblyOnDisk( tempDir, "Extension", new Version( 1, 0, 0, 0 ), "public class V1 {}" );
            var path2 = CreateAssemblyOnDisk( tempDir, "Extension", new Version( 2, 0, 0, 0 ), "public class V2 {}" );

            domain.LoadAssembly( path1, null, new LoadAssemblyOptions { IsShared = true } );

            // Different version of the same assembly should be incompatible.
            Assert.False( domain.IsCompatibleWithAssemblies( new[] { path2 } ) );
        }
        finally
        {
            TryDeleteDirectory( tempDir );
        }
    }

    /// <summary>
    /// Regression test for issue #579: GetOrCreateDomain returns a new domain when assembly versions conflict,
    /// allowing the new version to be loaded in a fresh domain.
    /// </summary>
    [Fact]
    public void GetOrCreateDomain_VersionConflict_CreatesNewDomain()
    {
        using var testContext = this.CreateTestContext();
        var factory = testContext.ServiceProvider.Global.GetRequiredService<ICompileTimeDomainFactory>();

        var tempDir = Path.Combine( Path.GetTempPath(), "Metalama.Tests", Guid.NewGuid().ToString() );
        Directory.CreateDirectory( tempDir );

        try
        {
            var path1 = CreateAssemblyOnDisk( tempDir, "Extension", new Version( 1, 0, 0, 0 ), "public class V1 {}" );
            var path2 = CreateAssemblyOnDisk( tempDir, "Extension", new Version( 2, 0, 0, 0 ), "public class V2 {}" );

            // Get a domain and load v1.
            var domain1 = factory.GetOrCreateDomain( new[] { path1 } );
            var assembly1 = domain1.LoadAssembly( path1, null, new LoadAssemblyOptions { IsShared = true } );
            Assert.Equal( new Version( 1, 0, 0, 0 ), assembly1.GetName().Version );

            // Get a domain for v2 — should be a different domain since v1 is incompatible.
            var domain2 = factory.GetOrCreateDomain( new[] { path2 } );
            Assert.NotSame( domain1, domain2 );

            // Load v2 in the new domain — should succeed.
            var assembly2 = domain2.LoadAssembly( path2, null, new LoadAssemblyOptions { IsShared = true } );
            Assert.Equal( new Version( 2, 0, 0, 0 ), assembly2.GetName().Version );
        }
        finally
        {
            TryDeleteDirectory( tempDir );
        }
    }

    /// <summary>
    /// Verifies that GetOrCreateDomain returns the same domain when assemblies are compatible.
    /// </summary>
    [Fact]
    public void GetOrCreateDomain_Compatible_ReturnsSameDomain()
    {
        using var testContext = this.CreateTestContext();
        var factory = testContext.ServiceProvider.Global.GetRequiredService<ICompileTimeDomainFactory>();

        var tempDir = Path.Combine( Path.GetTempPath(), "Metalama.Tests", Guid.NewGuid().ToString() );
        Directory.CreateDirectory( tempDir );

        try
        {
            var pathA = CreateAssemblyOnDisk( tempDir, "ExtensionA", new Version( 1, 0, 0, 0 ), "public class A {}" );
            var pathB = CreateAssemblyOnDisk( tempDir, "ExtensionB", new Version( 1, 0, 0, 0 ), "public class B {}" );

            // Get a domain and load A.
            var domain1 = factory.GetOrCreateDomain( new[] { pathA } );
            domain1.LoadAssembly( pathA, null, new LoadAssemblyOptions { IsShared = true } );

            // Get a domain for B — should be the same domain since there's no conflict.
            var domain2 = factory.GetOrCreateDomain( new[] { pathB } );
            Assert.Same( domain1, domain2 );
        }
        finally
        {
            TryDeleteDirectory( tempDir );
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

    private static void TryDeleteDirectory( string path )
    {
        try
        {
            Directory.Delete( path, true );
        }
        catch
        {
            // Ignore cleanup errors.
        }
    }
}
