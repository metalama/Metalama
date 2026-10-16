// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.CompileTime;
using Metalama.Testing.UnitTesting;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
#if NET5_0_OR_GREATER
using System.Reflection;
using System.Runtime.Loader;
#endif

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
    /// Verifies that <see cref="CompileTimeDomain.TryReserveAssemblies"/> succeeds when the assembly is the one that the
    /// domain has already loaded.
    /// </summary>
    [Fact]
    public void TryReserveAssemblies_Compatible_ReturnsTrue()
    {
        using var testContext = this.CreateTestContext();
        using var domain = testContext.Domain;

        var tempDir = Path.Combine( Path.GetTempPath(), "Metalama.Tests", Guid.NewGuid().ToString() );
        Directory.CreateDirectory( tempDir );

        try
        {
            var path1 = CreateAssemblyOnDisk( tempDir, "ExtensionA", new Version( 1, 0, 0, 0 ), "public class A {}" );

            domain.LoadAssembly( path1, null, new LoadAssemblyOptions { IsShared = true } );

            // The same assembly must be reservable, because its identity is the one already loaded.
            Assert.True( domain.TryReserveAssemblies( new[] { path1 } ) );
        }
        finally
        {
            TryDeleteDirectory( tempDir );
        }
    }

    /// <summary>
    /// Verifies that <see cref="CompileTimeDomain.TryReserveAssemblies"/> fails when an assembly of the same name but of
    /// a different version has already been loaded into the domain.
    /// </summary>
    [Fact]
    public void TryReserveAssemblies_VersionConflict_ReturnsFalse()
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

            // A different version of the same assembly must not be reservable.
            Assert.False( domain.TryReserveAssemblies( new[] { path2 } ) );
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

        CompileTimeDomain? domain1 = null;
        CompileTimeDomain? domain2 = null;

        try
        {
            var path1 = CreateAssemblyOnDisk( tempDir, "Extension", new Version( 1, 0, 0, 0 ), "public class V1 {}" );
            var path2 = CreateAssemblyOnDisk( tempDir, "Extension", new Version( 2, 0, 0, 0 ), "public class V2 {}" );

            // Get a domain and load v1.
            domain1 = factory.GetOrCreateDomain( new[] { path1 } );
            var assembly1 = domain1.LoadAssembly( path1, null, new LoadAssemblyOptions { IsShared = true } );
            Assert.Equal( new Version( 1, 0, 0, 0 ), assembly1.GetName().Version );

            // Get a domain for v2 — should be a different domain since v1 is incompatible.
            domain2 = factory.GetOrCreateDomain( new[] { path2 } );
            Assert.NotSame( domain1, domain2 );

            // Load v2 in the new domain — should succeed.
            var assembly2 = domain2.LoadAssembly( path2, null, new LoadAssemblyOptions { IsShared = true } );
            Assert.Equal( new Version( 2, 0, 0, 0 ), assembly2.GetName().Version );
        }
        finally
        {
            domain2?.Dispose();

            // Only dispose domain1 if it's different from domain2 (it always should be in this test).
            if ( domain1 != null && !ReferenceEquals( domain1, domain2 ) )
            {
                domain1.Dispose();
            }

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

        CompileTimeDomain? domain1 = null;

        try
        {
            var pathA = CreateAssemblyOnDisk( tempDir, "ExtensionA", new Version( 1, 0, 0, 0 ), "public class A {}" );
            var pathB = CreateAssemblyOnDisk( tempDir, "ExtensionB", new Version( 1, 0, 0, 0 ), "public class B {}" );

            // Get a domain and load A.
            domain1 = factory.GetOrCreateDomain( new[] { pathA } );
            domain1.LoadAssembly( pathA, null, new LoadAssemblyOptions { IsShared = true } );

            // Get a domain for B — should be the same domain since there's no conflict.
            var domain2 = factory.GetOrCreateDomain( new[] { pathB } );
            Assert.Same( domain1, domain2 );
        }
        finally
        {
            domain1?.Dispose();
            TryDeleteDirectory( tempDir );
        }
    }

    /// <summary>
    /// Regression test for #1749: two conflicting versions must get two domains even when neither has been loaded yet.
    /// </summary>
    /// <remarks>
    /// The sibling tests all load the first assembly before asking for the second domain, which is why they passed while
    /// this was broken. Compatibility was decided from the assemblies a domain had actually loaded, but a project loads
    /// long after it has been given its domain, so a second project asking in that window saw no conflict and was handed
    /// the same domain. Both then loaded, and the second failed with "Assembly with same name is already loaded". No
    /// concurrency is needed to show it: the two requests below are strictly sequential.
    /// </remarks>
    [Fact]
    public void GetOrCreateDomain_VersionConflictBeforeLoading_CreatesNewDomain()
    {
        using var testContext = this.CreateTestContext();
        var factory = testContext.ServiceProvider.Global.GetRequiredService<ICompileTimeDomainFactory>();

        var tempDir = Path.Combine( Path.GetTempPath(), "Metalama.Tests", Guid.NewGuid().ToString() );
        Directory.CreateDirectory( tempDir );

        CompileTimeDomain? domain1 = null;
        CompileTimeDomain? domain2 = null;

        try
        {
            var path1 = CreateAssemblyOnDisk( tempDir, "Extension", new Version( 1, 0, 0, 0 ), "public class V1 {}" );
            var path2 = CreateAssemblyOnDisk( tempDir, "Extension", new Version( 2, 0, 0, 0 ), "public class V2 {}" );

            // Both domains are requested before either assembly is loaded, which is what a compiler process building
            // two projects does.
            domain1 = factory.GetOrCreateDomain( new[] { path1 } );
            domain2 = factory.GetOrCreateDomain( new[] { path2 } );

            Assert.NotSame( domain1, domain2 );

            // Loading in the order the domains were requested must now work for both.
            Assert.Equal( new Version( 1, 0, 0, 0 ), domain1.LoadAssembly( path1, null, new LoadAssemblyOptions { IsShared = true } ).GetName().Version );
            Assert.Equal( new Version( 2, 0, 0, 0 ), domain2.LoadAssembly( path2, null, new LoadAssemblyOptions { IsShared = true } ).GetName().Version );
        }
        finally
        {
            domain2?.Dispose();

            if ( domain1 != null && !ReferenceEquals( domain1, domain2 ) )
            {
                domain1.Dispose();
            }

            TryDeleteDirectory( tempDir );
        }
    }

    /// <summary>
    /// Verifies that a request whose own assemblies conflict with each other does not consume a new domain every time
    /// it is made.
    /// </summary>
    /// <remarks>
    /// No domain can satisfy such a request, so answering "incompatible" would make the factory mint a fresh domain per
    /// request and never say why. The conflict belongs to the input, and is diagnosed as <c>LAMA0079</c> where the
    /// closure is built, so all the domain owes is a coherent reservation: the first identity of each simple name,
    /// which is the one the load context resolves to. See #1749.
    /// </remarks>
    [Fact]
    public void GetOrCreateDomain_SelfConflictingRequest_ReusesOneDomain()
    {
        using var testContext = this.CreateTestContext();
        var factory = testContext.ServiceProvider.Global.GetRequiredService<ICompileTimeDomainFactory>();

        var tempDir = Path.Combine( Path.GetTempPath(), "Metalama.Tests", Guid.NewGuid().ToString() );
        Directory.CreateDirectory( tempDir );

        CompileTimeDomain? domain1 = null;
        CompileTimeDomain? domain2 = null;

        try
        {
            var path1 = CreateAssemblyOnDisk( tempDir, "Extension", new Version( 1, 0, 0, 0 ), "public class V1 {}" );
            var path2 = CreateAssemblyOnDisk( tempDir, "Extension", new Version( 2, 0, 0, 0 ), "public class V2 {}" );

            domain1 = factory.GetOrCreateDomain( new[] { path1, path2 } );
            domain2 = factory.GetOrCreateDomain( new[] { path1, path2 } );

            Assert.Same( domain1, domain2 );

            Assert.Equal(
                new Version( 1, 0, 0, 0 ),
                domain1.LoadAssembly( path1, null, new LoadAssemblyOptions { IsShared = true } ).GetName().Version );
        }
        finally
        {
            domain2?.Dispose();

            if ( domain1 != null && !ReferenceEquals( domain1, domain2 ) )
            {
                domain1.Dispose();
            }

            TryDeleteDirectory( tempDir );
        }
    }

    /// <summary>
    /// Regression test for #1749 reproducing the interleaving of two concurrent compilations: both select a domain
    /// before either loads.
    /// </summary>
    /// <remarks>
    /// The interleaving is forced with <see cref="SemaphoreSlim"/> sync points rather than left to the scheduler, so the
    /// test is deterministic. Without the reservation, both threads are handed one domain and the second
    /// <c>LoadAssembly</c> throws.
    /// </remarks>
    [Fact]
    public async Task GetOrCreateDomain_ConcurrentSelectionBeforeLoading_CreatesNewDomain()
    {
        using var testContext = this.CreateTestContext();
        var factory = testContext.ServiceProvider.Global.GetRequiredService<ICompileTimeDomainFactory>();

        var tempDir = Path.Combine( Path.GetTempPath(), "Metalama.Tests", Guid.NewGuid().ToString() );
        Directory.CreateDirectory( tempDir );

        using var cancellationTokenSource = new CancellationTokenSource( TimeSpan.FromMinutes( 2 ) );
        var cancellationToken = cancellationTokenSource.Token;

        // SemaphoreSlim rather than TaskCompletionSource: WaitAsync takes a cancellation token on every target
        // framework, including net48, where Task.WaitAsync does not exist, and it returns a task started by this code,
        // which a TaskCompletionSource's Task is not. Awaiting the latter is VSTHRD003, a warning locally and an error
        // under ContinuousIntegrationBuild.
        using var firstHasSelected = new SemaphoreSlim( 0, 1 );
        using var secondHasSelected = new SemaphoreSlim( 0, 1 );

        CompileTimeDomain? domain1 = null;
        CompileTimeDomain? domain2 = null;

        try
        {
            var path1 = CreateAssemblyOnDisk( tempDir, "Extension", new Version( 1, 0, 0, 0 ), "public class V1 {}" );
            var path2 = CreateAssemblyOnDisk( tempDir, "Extension", new Version( 2, 0, 0, 0 ), "public class V2 {}" );

            var first = Task.Run(
                async () =>
                {
                    domain1 = factory.GetOrCreateDomain( new[] { path1 } );
                    firstHasSelected.Release();

                    // Load only once the other side has also chosen its domain, which is the window that used to be
                    // unguarded.
                    await secondHasSelected.WaitAsync( cancellationToken );

                    return domain1.LoadAssembly( path1, null, new LoadAssemblyOptions { IsShared = true } );
                },
                cancellationToken );

            var second = Task.Run(
                async () =>
                {
                    await firstHasSelected.WaitAsync( cancellationToken );

                    domain2 = factory.GetOrCreateDomain( new[] { path2 } );
                    secondHasSelected.Release();

                    return domain2.LoadAssembly( path2, null, new LoadAssemblyOptions { IsShared = true } );
                },
                cancellationToken );

            // These two awaits take no cancellation token, which is deliberate rather than an oversight. Task.WaitAsync
            // does not exist on net48, which this assembly targets. Neither task can outlive the token in any case: each
            // was started with it, and every wait inside them observes it, so cancelling makes both complete promptly
            // with an OperationCanceledException that the test then propagates.
            var assembly1 = await first;
            var assembly2 = await second;

            Assert.NotSame( domain1, domain2 );
            Assert.Equal( new Version( 1, 0, 0, 0 ), assembly1.GetName().Version );
            Assert.Equal( new Version( 2, 0, 0, 0 ), assembly2.GetName().Version );
        }
        finally
        {
            domain2?.Dispose();

            if ( domain1 != null && !ReferenceEquals( domain1, domain2 ) )
            {
                domain1.Dispose();
            }

            TryDeleteDirectory( tempDir );
        }
    }

#if NET5_0_OR_GREATER
    /// <summary>
    /// Verifies that two domains that use the same <see cref="SharedAssemblyLoadContext"/> get the same instance of a shared
    /// assembly, and that this instance is not collectible.
    /// </summary>
    [Fact]
    public void SharedContext_TwoDomains_ShareOneAssembly()
    {
        using var testContext = this.CreateTestContext();
        var sharedContext = new SharedAssemblyLoadContext();
        using var domain1 = new UnloadableCompileTimeDomain( testContext.ServiceProvider.Global, sharedContext );
        using var domain2 = new UnloadableCompileTimeDomain( testContext.ServiceProvider.Global, sharedContext );

        var tempDir = CreateTempDirectory();

        try
        {
            var path = CreateAssemblyOnDisk( tempDir, "SharedExtension", new Version( 1, 0, 0, 0 ), "public class C {}" );

            var assembly1 = domain1.LoadAssembly( path, null, LoadAssemblyOptions.Shared );
            var assembly2 = domain2.LoadAssembly( path, null, LoadAssemblyOptions.Shared );

            Assert.Same( assembly1, assembly2 );
            Assert.False( assembly1.IsCollectible );
            Assert.Same( sharedContext, AssemblyLoadContext.GetLoadContext( assembly1 ) );
        }
        finally
        {
            TryDeleteDirectory( tempDir );
        }
    }

    /// <summary>
    /// Verifies that an assembly that is not shared, loaded into the collectible context of a domain, can use a type of a shared
    /// assembly that the domain has loaded.
    /// </summary>
    [Fact]
    public void SharedContext_DomainAssemblyReferencingSharedAssembly_IsResolved()
    {
        using var testContext = this.CreateTestContext();
        var sharedContext = new SharedAssemblyLoadContext();
        using var domain = new UnloadableCompileTimeDomain( testContext.ServiceProvider.Global, sharedContext );

        var tempDir = CreateTempDirectory();

        try
        {
            var sharedPath = CreateAssemblyOnDisk(
                tempDir,
                "SharedExtension",
                new Version( 1, 0, 0, 0 ),
                "public static class SharedType { public static int Value => 42; }" );

            var userPath = CreateAssemblyOnDisk(
                tempDir,
                "UserAssembly",
                new Version( 1, 0, 0, 0 ),
                "public static class UserType { public static int GetValue() => SharedType.Value; }",
                sharedPath );

            var sharedAssembly = domain.LoadAssembly( sharedPath, null, LoadAssemblyOptions.Shared );
            var userAssembly = domain.LoadAssembly( userPath );

            Assert.True( userAssembly.IsCollectible );
            Assert.Equal( 42, InvokeGetValue( userAssembly ) );

            // The reference must have been bound to the shared copy, not to a second copy in the collectible context.
            Assert.DoesNotContain( AssemblyLoadContext.GetLoadContext( userAssembly )!.Assemblies, a => a.GetName().Name == "SharedExtension" );
            Assert.Same( sharedContext, AssemblyLoadContext.GetLoadContext( sharedAssembly ) );
        }
        finally
        {
            TryDeleteDirectory( tempDir );
        }
    }

    /// <summary>
    /// Verifies that disposing a domain does not affect a shared assembly that another domain uses.
    /// </summary>
    [Fact]
    public void SharedContext_DisposingOneDomain_DoesNotAffectOtherDomain()
    {
        using var testContext = this.CreateTestContext();
        var sharedContext = new SharedAssemblyLoadContext();
        using var domain2 = new UnloadableCompileTimeDomain( testContext.ServiceProvider.Global, sharedContext );

        var tempDir = CreateTempDirectory();

        try
        {
            var sharedPath = CreateAssemblyOnDisk(
                tempDir,
                "SharedExtension",
                new Version( 1, 0, 0, 0 ),
                "public static class SharedType { public static int Value => 42; }" );

            var userPath = CreateAssemblyOnDisk(
                tempDir,
                "UserAssembly",
                new Version( 1, 0, 0, 0 ),
                "public static class UserType { public static int GetValue() => SharedType.Value; }",
                sharedPath );

            using ( var domain1 = new UnloadableCompileTimeDomain( testContext.ServiceProvider.Global, sharedContext ) )
            {
                domain1.LoadAssembly( sharedPath, null, LoadAssemblyOptions.Shared );
            }

            domain2.LoadAssembly( sharedPath, null, LoadAssemblyOptions.Shared );
            var userAssembly = domain2.LoadAssembly( userPath );

            Assert.Equal( 42, InvokeGetValue( userAssembly ) );
        }
        finally
        {
            TryDeleteDirectory( tempDir );
        }
    }

    /// <summary>
    /// Verifies that a shared assembly whose file has changed since it was loaded is loaded again into the collectible context of
    /// the domain instead of being taken from the shared context.
    /// </summary>
    [Fact]
    public void SharedContext_FileChanged_LoadsIntoDomain()
    {
        using var testContext = this.CreateTestContext();
        var sharedContext = new SharedAssemblyLoadContext();
        using var domain1 = new UnloadableCompileTimeDomain( testContext.ServiceProvider.Global, sharedContext );
        using var domain2 = new UnloadableCompileTimeDomain( testContext.ServiceProvider.Global, sharedContext );

        var tempDir = CreateTempDirectory();

        try
        {
            var path = CreateAssemblyOnDisk( tempDir, "SharedExtension", new Version( 1, 0, 0, 0 ), "public class C {}" );
            var assembly1 = domain1.LoadAssembly( path, null, LoadAssemblyOptions.Shared );

            // Rebuild the file with a different content and a later write time.
            CreateAssemblyOnDisk( tempDir, "SharedExtension", new Version( 1, 0, 0, 0 ), "public class C {} public class D {}" );
            File.SetLastWriteTimeUtc( path, File.GetLastWriteTimeUtc( path ).AddMinutes( 1 ) );

            var assembly2 = domain2.LoadAssembly( path, null, LoadAssemblyOptions.Shared );

            Assert.NotSame( assembly1, assembly2 );
            Assert.True( assembly2.IsCollectible );
            Assert.NotNull( assembly2.GetType( "D" ) );
        }
        finally
        {
            TryDeleteDirectory( tempDir );
        }
    }

    /// <summary>
    /// Verifies that a shared assembly whose name is already used in the shared context by an assembly loaded from another path is
    /// loaded into the collectible context of the domain.
    /// </summary>
    [Fact]
    public void SharedContext_SameNameFromOtherPath_LoadsIntoDomain()
    {
        using var testContext = this.CreateTestContext();
        var sharedContext = new SharedAssemblyLoadContext();
        using var domain1 = new UnloadableCompileTimeDomain( testContext.ServiceProvider.Global, sharedContext );
        using var domain2 = new UnloadableCompileTimeDomain( testContext.ServiceProvider.Global, sharedContext );

        var tempDir1 = CreateTempDirectory();
        var tempDir2 = CreateTempDirectory();

        try
        {
            var path1 = CreateAssemblyOnDisk( tempDir1, "SharedExtension", new Version( 1, 0, 0, 0 ), "public class C {}" );
            var path2 = CreateAssemblyOnDisk( tempDir2, "SharedExtension", new Version( 2, 0, 0, 0 ), "public class C {}" );

            var assembly1 = domain1.LoadAssembly( path1, null, LoadAssemblyOptions.Shared );
            var assembly2 = domain2.LoadAssembly( path2, null, LoadAssemblyOptions.Shared );

            Assert.False( assembly1.IsCollectible );
            Assert.True( assembly2.IsCollectible );
            Assert.Equal( new Version( 2, 0, 0, 0 ), assembly2.GetName().Version );
        }
        finally
        {
            TryDeleteDirectory( tempDir1 );
            TryDeleteDirectory( tempDir2 );
        }
    }

    /// <summary>
    /// Invokes the static <c>UserType.GetValue</c> method of an assembly created by the tests of <see cref="SharedAssemblyLoadContext"/>.
    /// </summary>
    private static int InvokeGetValue( Assembly assembly )
        => (int) assembly.GetType( "UserType", true )!.GetMethod( "GetValue" )!.Invoke( null, null )!;
#endif

    /// <summary>
    /// Creates a new temporary directory for the files of a test.
    /// </summary>
    private static string CreateTempDirectory()
    {
        var tempDir = Path.Combine( Path.GetTempPath(), "Metalama.Tests", Guid.NewGuid().ToString() );
        Directory.CreateDirectory( tempDir );

        return tempDir;
    }

    private static string CreateAssemblyOnDisk( string directory, string assemblyName, Version version, string code, params string[] referencePaths )
    {
        var compilation = CSharpCompilation.Create(
            assemblyName,
            [CSharpSyntaxTree.ParseText( $"[assembly: System.Reflection.AssemblyVersion(\"{version}\")]\n{code}" )],
            [MetadataReference.CreateFromFile( typeof(object).Assembly.Location ), ..Array.ConvertAll( referencePaths, p => MetadataReference.CreateFromFile( p ) )],
            new CSharpCompilationOptions( OutputKind.DynamicallyLinkedLibrary ) );

        // Use a unique file name per version to avoid file conflicts.
        var path = Path.Combine( directory, $"{assemblyName}.{version}.dll" );

        using var peStream = File.Create( path );
        var result = compilation.Emit( peStream );

        if ( !result.Success )
        {
            throw new InvalidOperationException( $"Failed to emit assembly: {string.Join( ", ", result.Diagnostics )}" );
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