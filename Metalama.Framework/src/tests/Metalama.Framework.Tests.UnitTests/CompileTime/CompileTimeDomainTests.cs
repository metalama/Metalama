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
    /// Regression test for issue #579: When EnsureCompatibleWithAssemblies detects that an assembly with the same
    /// simple name but a different version is already loaded, the domain should retire the current loader and create
    /// a new one, allowing the new version to be loaded while keeping the old loader alive.
    /// </summary>
    [Fact]
    public void EnsureCompatibleWithAssemblies_RetiresLoaderOnVersionConflict()
    {
        using var testContext = this.CreateTestContext();
        using var domain = testContext.Domain;

        var tempDir = Path.Combine( Path.GetTempPath(), "Metalama.Tests", Guid.NewGuid().ToString() );
        Directory.CreateDirectory( tempDir );

        try
        {
            var assemblyName = "TestExtension";

            var path1 = CreateAssemblyOnDisk( tempDir, assemblyName, new Version( 1, 0, 0, 0 ), "public class V1 {}" );
            var path2 = CreateAssemblyOnDisk( tempDir, assemblyName, new Version( 2, 0, 0, 0 ), "public class V2 {}" );

            // Load the first version.
            var assembly1 = domain.LoadAssembly( path1, null, new LoadAssemblyOptions { IsShared = true } );
            Assert.NotNull( assembly1 );
            Assert.Equal( assemblyName, assembly1.GetName().Name );
            Assert.Equal( new Version( 1, 0, 0, 0 ), assembly1.GetName().Version );

            // EnsureCompatibleWithAssemblies should detect the conflict and retire the old loader.
            domain.EnsureCompatibleWithAssemblies( new[] { path2 } );

            // Now loading the new version should succeed and return the new assembly.
            var assembly2 = domain.LoadAssembly( path2, null, new LoadAssemblyOptions { IsShared = true } );
            Assert.NotNull( assembly2 );
            Assert.Equal( assemblyName, assembly2.GetName().Name );
            Assert.Equal( new Version( 2, 0, 0, 0 ), assembly2.GetName().Version );

            // The old assembly reference (assembly1) should still be valid — the retired loader is not disposed.
            Assert.Equal( "V1", assembly1.GetTypes()[0].Name );
        }
        finally
        {
            TryDeleteDirectory( tempDir );
        }
    }

    /// <summary>
    /// Verifies that EnsureCompatibleWithAssemblies does not retire the loader when all assemblies are compatible.
    /// </summary>
    [Fact]
    public void EnsureCompatibleWithAssemblies_NoRetireWhenCompatible()
    {
        using var testContext = this.CreateTestContext();
        using var domain = testContext.Domain;

        var tempDir = Path.Combine( Path.GetTempPath(), "Metalama.Tests", Guid.NewGuid().ToString() );
        Directory.CreateDirectory( tempDir );

        try
        {
            var path1 = CreateAssemblyOnDisk( tempDir, "ExtensionA", new Version( 1, 0, 0, 0 ), "public class A {}" );

            // Load the first assembly.
            var assembly1 = domain.LoadAssembly( path1, null, new LoadAssemblyOptions { IsShared = true } );
            Assert.NotNull( assembly1 );

            // EnsureCompatibleWithAssemblies with the same path should not retire the loader.
            domain.EnsureCompatibleWithAssemblies( new[] { path1 } );

            // Loading the same assembly should return the cached instance.
            var assembly1Again = domain.LoadAssembly( path1, null, new LoadAssemblyOptions { IsShared = true } );
            Assert.Same( assembly1, assembly1Again );
        }
        finally
        {
            TryDeleteDirectory( tempDir );
        }
    }

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
    /// Verifies that after a loader retirement, previously loaded compatible assemblies can be reloaded.
    /// </summary>
    [Fact]
    public void EnsureCompatibleWithAssemblies_CanReloadAfterRetire()
    {
        using var testContext = this.CreateTestContext();
        using var domain = testContext.Domain;

        var tempDir = Path.Combine( Path.GetTempPath(), "Metalama.Tests", Guid.NewGuid().ToString() );
        Directory.CreateDirectory( tempDir );

        try
        {
            var pathA = CreateAssemblyOnDisk( tempDir, "ExtensionA", new Version( 1, 0, 0, 0 ), "public class A {}" );
            var pathB1 = CreateAssemblyOnDisk( tempDir, "ExtensionB", new Version( 1, 0, 0, 0 ), "public class B1 {}" );
            var pathB2 = CreateAssemblyOnDisk( tempDir, "ExtensionB", new Version( 2, 0, 0, 0 ), "public class B2 {}" );

            // Load both initial versions.
            domain.LoadAssembly( pathA, null, new LoadAssemblyOptions { IsShared = true } );
            domain.LoadAssembly( pathB1, null, new LoadAssemblyOptions { IsShared = true } );

            // Retire the loader for the new version of B.
            domain.EnsureCompatibleWithAssemblies( new[] { pathA, pathB2 } );

            // Both assemblies should load successfully after retirement.
            var assemblyA = domain.LoadAssembly( pathA, null, new LoadAssemblyOptions { IsShared = true } );
            var assemblyB = domain.LoadAssembly( pathB2, null, new LoadAssemblyOptions { IsShared = true } );

            Assert.Equal( "ExtensionA", assemblyA.GetName().Name );
            Assert.Equal( "ExtensionB", assemblyB.GetName().Name );
            Assert.Equal( new Version( 2, 0, 0, 0 ), assemblyB.GetName().Version );
        }
        finally
        {
            TryDeleteDirectory( tempDir );
        }
    }

    /// <summary>
    /// Verifies that assemblies loaded from a retired loader remain accessible and valid
    /// even after a new loader has been created. This simulates the concurrent compilation scenario
    /// where compilation A holds references to assemblies loaded from the old loader while
    /// compilation B triggers a retirement and uses a new loader.
    /// </summary>
    [Fact]
    public void RetiredLoaderAssemblies_RemainValidForConcurrentUse()
    {
        using var testContext = this.CreateTestContext();
        using var domain = testContext.Domain;

        var tempDir = Path.Combine( Path.GetTempPath(), "Metalama.Tests", Guid.NewGuid().ToString() );
        Directory.CreateDirectory( tempDir );

        try
        {
            var pathV1 = CreateAssemblyOnDisk( tempDir, "SharedExtension", new Version( 1, 0, 0, 0 ), "public class V1 { public static int Value => 1; }" );
            var pathV2 = CreateAssemblyOnDisk( tempDir, "SharedExtension", new Version( 2, 0, 0, 0 ), "public class V2 { public static int Value => 2; }" );
            var pathOther = CreateAssemblyOnDisk( tempDir, "OtherExtension", new Version( 1, 0, 0, 0 ), "public class Other {}" );

            // Simulation: Compilation A loads SharedExtension v1 and OtherExtension.
            var assemblyV1 = domain.LoadAssembly( pathV1, null, new LoadAssemblyOptions { IsShared = true } );
            var assemblyOther = domain.LoadAssembly( pathOther, null, new LoadAssemblyOptions { IsShared = true } );

            Assert.Equal( new Version( 1, 0, 0, 0 ), assemblyV1.GetName().Version );

            // Simulation: Compilation B needs SharedExtension v2. This retires the old loader.
            domain.EnsureCompatibleWithAssemblies( new[] { pathV2 } );

            // Compilation B loads SharedExtension v2 into the new loader.
            var assemblyV2 = domain.LoadAssembly( pathV2, null, new LoadAssemblyOptions { IsShared = true } );

            Assert.Equal( new Version( 2, 0, 0, 0 ), assemblyV2.GetName().Version );

            // Verify that Compilation A's assemblies are still valid and usable.
            // The retired loader was not disposed, so these references should still work.
            Assert.Equal( "V1", assemblyV1.GetTypes()[0].Name );
            Assert.Equal( "Other", assemblyOther.GetTypes()[0].Name );
            Assert.Equal( new Version( 1, 0, 0, 0 ), assemblyV1.GetName().Version );
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
