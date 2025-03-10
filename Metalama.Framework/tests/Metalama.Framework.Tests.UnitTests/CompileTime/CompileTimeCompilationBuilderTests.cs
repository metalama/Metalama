// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Utilities;
using Metalama.Compiler;
using Metalama.Framework.Code;
using Metalama.Framework.Engine;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.CompileTime;
using Metalama.Framework.Engine.CompileTime.Manifest;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Pipeline.CompileTime;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities;
using Metalama.Testing.UnitTesting;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

#pragma warning disable VSTHRD200

namespace Metalama.Framework.Tests.UnitTests.CompileTime
{
    public sealed class CompileTimeCompilationBuilderTests : UnitTestClass
    {
        [Fact]
        public void RemoveInvalidNamespaceImport()
        {
            using var testContext = this.CreateTestContext();

            var compilation = testContext.CreateCSharpCompilation(
                @"
using System;
using Nonsense;
using Foo;

namespace Foo
{
    class C {}
}
",
                ignoreErrors: true );

            const string expected = @"
using System;
using Foo;

namespace Foo
{
    class C {}
}
";

            var rewriter = new CompileTimeCompilationBuilder.RemoveInvalidUsingRewriter( compilation );

            var actual = rewriter.Visit( compilation.SyntaxTrees.Single().GetRoot() )!.ToFullString();

            AssertEx.EolInvariantEqual( expected, actual );
        }

        [Fact]
        public void Attributes()
        {
            const string code = @"
using System;
using Metalama.Framework.Advising; 
using Metalama.Framework.Aspects; 
using Metalama.Framework.Fabrics;

[assembly: A(42, new[] { E.A }, new[] { typeof(C<int[]>.N<string>), typeof(C<>.N<>) }, P = 13)]
[assembly: CompileTime]

enum E { A }

class C<T1>
{
    public class N<T2> {}
}

class A : Attribute
{
    private string constructorArguments;

    public int P { get; set; }

    public A(int i, E[] es, Type[] types) => constructorArguments = $""{i}, {es[0]}, {types[0]}, {types[1]}"";

    public override string ToString() => $""A({constructorArguments}, P={P})"";
}";

            using var testContext = this.CreateTestContext();

            var roslynCompilation = testContext.CreateCSharpCompilation( code );
            var compilation = CompilationModel.CreateInitialInstance( new ProjectModel( roslynCompilation, testContext.ServiceProvider ), roslynCompilation );

            using var compileTimeDomain = testContext.Domain;
            var loader = CompileTimeProjectRepository.Create( compileTimeDomain, testContext.ServiceProvider, compilation.RoslynCompilation ).AssertNotNull();

            if ( !loader.CreateAttributeDeserializer( testContext.ServiceProvider, compilation.CompilationContext )
                    .TryCreateAttribute( compilation.Attributes.First(), new DiagnosticBag(), out var attribute ) )
            {
                throw new AssertionFailedException();
            }

            Assert.Equal( "A(42, A, C`1+N`1[System.Int32[],System.String], C`1+N`1[T1,T2], P=13)", attribute.ToString() );
        }

        [Fact]
        public void CompilationMetadataReference()
        {
            // This tests that we can create compile-time assemblies that have reference projects in the same solution with compile-time code.

            const string referencedCode = @"
using Metalama.Framework.Advising; 
using Metalama.Framework.Aspects; 
[assembly: CompileTime]
public class ReferencedClass
{
}
";

            const string referencingCode = @"

using Metalama.Framework.Advising; 
using Metalama.Framework.Aspects; 
[assembly: CompileTime]
class ReferencingClass
{
  ReferencedClass c;
}
";

            using var testContext = this.CreateTestContext();

            var roslynCompilation = testContext.CreateCSharpCompilation( referencingCode, referencedCode );

            var domain = testContext.Domain;
            CompileTimeProjectRepository.Create( domain, testContext.ServiceProvider, roslynCompilation ).AssertNotNull();
        }

        [Fact]
        public void CompilationDuplicateMetadataReference()
        {
            const string referencedCode = @"
using Metalama.Framework.Advising; 
using Metalama.Framework.Aspects; 
[assembly: CompileTime]
public class ReferencedClass
{
}
";

            const string referencingCode = @"

using Metalama.Framework.Advising; 
using Metalama.Framework.Aspects; 
[assembly: CompileTime]
class ReferencingClass
{
  ReferencedClass c;
}
";

            using var testContext = this.CreateTestContext();

            var referencedCompilation = testContext.CreateCSharpCompilation( referencedCode );

            var referencedCompilationModified =
                referencedCompilation.WithOptions( referencedCompilation.Options.WithAllowUnsafe( !referencedCompilation.Options.AllowUnsafe ) );

            var roslynCompilation = testContext.CreateCSharpCompilation(
                referencingCode,
                additionalReferences: [referencedCompilation.ToMetadataReference(), referencedCompilationModified.ToMetadataReference()] );

            var domain = testContext.Domain;
            CompileTimeProjectRepository.Create( domain, testContext.ServiceProvider, roslynCompilation ).AssertNotNull();
        }

        [Fact]
        public void BinaryMetadataReferences()
        {
            // This tests that we can create compile-time assemblies that have reference compiled assemblies (out of the solution) with compile-time code.

            const string indirectlyReferencedCode = @"
using Metalama.Framework.Advising; 
using Metalama.Framework.Aspects; 
[assembly: CompileTime]
public class ReferencedClass
{
}
";

            const string directlyReferencedCode = @"
using Metalama.Framework.Advising; 
using Metalama.Framework.Aspects; 
[assembly: CompileTime]
public class MiddleClass
{
  ReferencedClass c;
}
";

            const string referencingCode = @"

using Metalama.Framework.Advising; 
using Metalama.Framework.Aspects; 
[assembly: CompileTime]
class ReferencingClass
{
  MiddleClass d;
}
";

            List<string> tempFiles = [];

            try
            {
                var testAssemblyLocator = new TestAssemblyLocator();
                var mocks = new AdditionalServiceCollection( testAssemblyLocator );

                using var testContext = this.CreateTestContext( mocks );
                var domain = testContext.Domain;

                PortableExecutableReference CompileProject( string code, params MetadataReference[] references )
                {
                    var compilation = testContext.CreateCSharpCompilation( code, additionalReferences: references );

                    var compileTimeProjectRepository = CompileTimeProjectRepository.Create( domain, testContext.ServiceProvider, compilation ).AssertNotNull();

                    var runTimePath = MetalamaPathUtilities.GetTempFileName();
                    tempFiles.Add( runTimePath );

                    // We must create the dll on disk to emulate the path taken by real code.
                    using ( var runTimeStream = File.Create( runTimePath ) )
                    {
                        var emitResult = compilation.Emit(
                            runTimeStream,
                            null,
                            null,
                            null,
                            [compileTimeProjectRepository.RootProject.ToResource().Resource] );

                        Assert.True( emitResult.Success );
                    }

                    var referenceToSelf = MetadataReference.CreateFromFile( runTimePath );
                    testAssemblyLocator.Files.Add( compilation.Assembly.Identity, referenceToSelf );

                    return referenceToSelf;
                }

                var indirectlyReferenced = CompileProject( indirectlyReferencedCode );
                var directlyReferenced = CompileProject( directlyReferencedCode, indirectlyReferenced );
                _ = CompileProject( referencingCode, directlyReferenced );
            }
            finally
            {
                foreach ( var path in tempFiles )
                {
                    if ( File.Exists( path ) )
                    {
                        File.Delete( path );
                    }
                }
            }
        }

        [Fact]
        public void UpdatedReference()
        {
            // This test verifies that one can create a project A v1.0 that references B v1.0, but it still works when B is updated to v1.1
            // and A is not recompiled.

            string GenerateVersionedCode( int version )
                => @"
using Metalama.Framework.Advising; 
using Metalama.Framework.Aspects; 
[assembly: CompileTime]
public class VersionedClass
{
    public static int Version => $version;
}
".ReplaceOrdinal( "$version", version.ToString( CultureInfo.InvariantCulture ) );

            const string classA = @"

using Metalama.Framework.Advising; 
using Metalama.Framework.Aspects; 
[assembly: CompileTime]
class A
{
  public  static int Version => VersionedClass.Version;
}
";

            const string classB = @"

using Metalama.Framework.Advising; 
using Metalama.Framework.Aspects; 
[assembly: CompileTime]
class B
{
  public static int Version => VersionedClass.Version;
}
";

            using var testContext = this.CreateTestContext();

            var guid = RandomIdGenerator.GenerateId();
            var versionedCompilationV1 = testContext.CreateCSharpCompilation( GenerateVersionedCode( 1 ), assemblyName: "test_Versioned_" + guid );
            var versionedCompilationV2 = testContext.CreateCSharpCompilation( GenerateVersionedCode( 2 ), assemblyName: "test_Versioned_" + guid );

            var compilationA = testContext.CreateCSharpCompilation(
                classA,
                additionalReferences: [versionedCompilationV1.ToMetadataReference()],
                assemblyName: "test_A_" + guid );

            var compilationB1 = testContext.CreateCSharpCompilation(
                classB,
                additionalReferences: [versionedCompilationV1.ToMetadataReference(), compilationA.ToMetadataReference()],
                assemblyName: "test_B_" + guid );

            var compilationB2 = testContext.CreateCSharpCompilation(
                classB,
                additionalReferences: [versionedCompilationV2.ToMetadataReference(), compilationA.ToMetadataReference()],
                assemblyName: "test_B_" + guid );

            using var domain = testContext.Domain;

            var compileTimeProjectRepository1 = CompileTimeProjectRepository.Create( domain, testContext.ServiceProvider, compilationB1 ).AssertNotNull();

            ExecuteAssertions( compileTimeProjectRepository1.RootProject, 1 );

            var compileTimeProjectRepository2 = CompileTimeProjectRepository.Create( domain, testContext.ServiceProvider, compilationB2 ).AssertNotNull();

            ExecuteAssertions( compileTimeProjectRepository2.RootProject, 2 );

            void ExecuteAssertions( CompileTimeProject project, int expectedVersion )
            {
                var valueFromA = project.References
                    .Single( p => p.RunTimeIdentity.Name == compilationA.AssemblyName )
                    .GetType( "A" )
                    .GetProperty( "Version" )!
                    .GetValue( null );

                Assert.Equal( expectedVersion, valueFromA );

                var valueFromB = project
                    .GetType( "B" )
                    .GetProperty( "Version" )!
                    .GetValue( null );

                Assert.Equal( expectedVersion, valueFromB );
            }
        }

        [Fact]
        public void CanCreateCompileTimeProjectWithInvalidRunTimeCode()
        {
            // We need to be able to have a compile-time assembly even if there is an error in run-time-only code,
            // otherwise the design-time experience is doomed to fail.

            const string code = @"

using Metalama.Framework.Fabrics;
[CompileTime]
class B
{

}

class C 
{
    Intentionally Invalid
}
";

            using var testContext = this.CreateTestContext();
            var domain = testContext.Domain;
            var compilation = testContext.CreateCSharpCompilation( code, ignoreErrors: true );
            CompileTimeProjectRepository.Create( domain, testContext.ServiceProvider, compilation ).AssertNotNull();
        }

        [Fact]
        public void CacheWithSameLoader()
        {
            const string code = @"
using Metalama.Framework.Advising; 
using Metalama.Framework.Aspects; 
[assembly: CompileTime]
public class ReferencedClass
{
}
";

            using var testContext = this.CreateTestContext();

            var roslynCompilation = testContext.CreateCSharpCompilation( code );

            var domain = testContext.Domain;
            var loader = new CompileTimeProjectRepository.Builder( domain, testContext.ServiceProvider );

            DiagnosticBag diagnosticBag = new();

            // Getting from cache should fail.
            Assert.False(
                loader.TryGetCompileTimeProjectFromCompilation(
                    roslynCompilation,
                    null,
                    diagnosticBag,
                    true,
                    CancellationToken.None,
                    out _ ) );

            // Building the project should succeed.
            Assert.True(
                loader.TryGetCompileTimeProjectFromCompilation(
                    roslynCompilation,
                    null,
                    diagnosticBag,
                    false,
                    CancellationToken.None,
                    out var compileTimeProject1 ) );

            // After building, getting from cache should succeed.
            Assert.True(
                loader.TryGetCompileTimeProjectFromCompilation(
                    roslynCompilation,
                    null,
                    diagnosticBag,
                    true,
                    CancellationToken.None,
                    out var compileTimeProject2 ) );

            Assert.Same( compileTimeProject1, compileTimeProject2 );
        }

        [Fact]
        public void CacheWithDifferentIdentityButSameCodeSameLoader()
        {
            const string code = @"
using Metalama.Framework.Advising; 
using Metalama.Framework.Aspects; 
[assembly: CompileTime]
public class ReferencedClass
{
}
";

            using var testContext = this.CreateTestContext();
            var domain = testContext.Domain;
            var builder = new CompileTimeProjectRepository.Builder( domain, testContext.ServiceProvider );

            DiagnosticBag diagnosticBag = new();

            // Building the project should succeed.
            Assert.True(
                builder.TryGetCompileTimeProjectFromCompilation(
                    testContext.CreateCSharpCompilation( code ),
                    null,
                    diagnosticBag,
                    false,
                    CancellationToken.None,
                    out _ ) );

            // After building, getting from cache should fail.
            Assert.False(
                builder.TryGetCompileTimeProjectFromCompilation(
                    testContext.CreateCSharpCompilation( code ),
                    null,
                    diagnosticBag,
                    true,
                    CancellationToken.None,
                    out _ ) );
        }

        [Fact]
        public void CacheWithDifferentIdentityButSameCodeDifferentLoader()
        {
            const string code = @"
using Metalama.Framework.Advising; 
using Metalama.Framework.Aspects; 
[assembly: CompileTime]
public class ReferencedClass
{
}
";

            DiagnosticBag diagnosticBag = new();

            using var testContext = this.CreateTestContext();
            var domain = testContext.Domain;
            var builder1 = new CompileTimeProjectRepository.Builder( domain, testContext.ServiceProvider );

            // Building the project should succeed.
            Assert.True(
                builder1.TryGetCompileTimeProjectFromCompilation(
                    testContext.CreateCSharpCompilation( code ),
                    null,
                    diagnosticBag,
                    false,
                    CancellationToken.None,
                    out _ ) );

            // After building, getting from cache should fail because the memory cache is empty and the disk cache checks the assembly name.
            var builder2 = new CompileTimeProjectRepository.Builder( domain, testContext.ServiceProvider );

            Assert.False(
                builder2.TryGetCompileTimeProjectFromCompilation(
                    testContext.CreateCSharpCompilation( code ),
                    null,
                    diagnosticBag,
                    true,
                    CancellationToken.None,
                    out _ ) );
        }

        [Fact]
        public void CacheWithDifferentLoader()
        {
            const string code = @"
using Metalama.Framework.Advising; 
using Metalama.Framework.Aspects; 
[assembly: CompileTime]
public class ReferencedClass
{
}
";

            using var testContext = this.CreateTestContext();

            var roslynCompilation = testContext.CreateCSharpCompilation( code );

            DiagnosticBag diagnosticBag = new();

            // We create a single testContext.ServiceProvider because we need to share the filesystem cache, and there is one per testContext.ServiceProvider
            // in test projects.
            var domain = testContext.Domain;

            // Getting from cache should fail.

            var loader1 = new CompileTimeProjectRepository.Builder( domain, testContext.ServiceProvider );

            Assert.False(
                loader1.TryGetCompileTimeProjectFromCompilation(
                    roslynCompilation,
                    null,
                    diagnosticBag,
                    true,
                    CancellationToken.None,
                    out _ ) );

            // Building the project should succeed.
            var loader2 = new CompileTimeProjectRepository.Builder( domain, testContext.ServiceProvider );

            Assert.True(
                loader2.TryGetCompileTimeProjectFromCompilation(
                    roslynCompilation,
                    null,
                    diagnosticBag,
                    false,
                    CancellationToken.None,
                    out _ ) );

            // After building, getting from cache should succeed.
            var loader3 = new CompileTimeProjectRepository.Builder( domain, testContext.ServiceProvider );

            Assert.True(
                loader3.TryGetCompileTimeProjectFromCompilation(
                    roslynCompilation,
                    null,
                    diagnosticBag,
                    true,
                    CancellationToken.None,
                    out _ ) );
        }

#if NET6_0_OR_GREATER
        [Fact]
        public void CleanCacheAndDeserialize()
        {
            const string referencedCode = @"
using Metalama.Framework.Advising; 
using Metalama.Framework.Aspects; 
[assembly: CompileTime]
public class ReferencedClass
{
}
";

            const string referencingCode = @"

using Metalama.Framework.Advising; 
using Metalama.Framework.Aspects; 
[assembly: CompileTime]
class ReferencingClass
{
  ReferencedClass c;
}
";

            var rootTestContext = this.CreateTestContext();

            var referencedCompilation = rootTestContext.CreateCSharpCompilation( referencedCode );

            var referencedPath = Path.Combine( rootTestContext.BaseDirectory, "referenced.dll" );

            using ( var testContext = this.CreateTestContext() )
            {
                var loader = new CompileTimeProjectRepository.Builder( rootTestContext.Domain, testContext.ServiceProvider );

                DiagnosticBag diagnosticBag = new();

                Assert.True(
                    loader.TryGetCompileTimeProjectFromCompilation(
                        referencedCompilation,
                        null,
                        diagnosticBag,
                        false,
                        CancellationToken.None,
                        out var referencedCompileTimeProject ) );

                using var peStream = File.Create( referencedPath );

                Assert.True(
                    referencedCompilation.Emit(
                            peStream,
                            manifestResources: [referencedCompileTimeProject!.ToResource().Resource] )
                        .Success );
            }

            var referencingCompilation = rootTestContext.CreateCSharpCompilation(
                referencingCode,
                additionalReferences: [MetadataReference.CreateFromFile( referencedPath )] );

            using ( rootTestContext )
            {
                var loader = new CompileTimeProjectRepository.Builder( rootTestContext.Domain, rootTestContext.ServiceProvider );

                DiagnosticBag diagnosticBag = new();

                Assert.True(
                    loader.TryGetCompileTimeProjectFromCompilation(
                        referencingCompilation,
                        null,
                        diagnosticBag,
                        false,
                        CancellationToken.None,
                        out _ ) );
            }
        }
#endif

        [Fact]
        public void EmptyProjectWithReference()
        {
            using var testContext = this.CreateTestContext();
            var domain = testContext.Domain;

            var loader = new CompileTimeProjectRepository.Builder( domain, testContext.ServiceProvider );

            const string referencedCode = @"
using Metalama.Framework.Advising; 
using Metalama.Framework.Aspects; 
[assembly: CompileTime]
public class ReferencedClass
{
}";

            const string referencingCode = @"/* Intentionally empty. */";

            // Emit the referenced assembly.
            var referencedCompilation = testContext.CreateCSharpCompilation( referencedCode );
            var referencedPath = Path.Combine( testContext.BaseDirectory, "referenced.dll" );

            DiagnosticBag diagnosticBag = new();

            Assert.True(
                loader.TryGetCompileTimeProjectFromCompilation(
                    referencedCompilation,
                    null,
                    diagnosticBag,
                    false,
                    CancellationToken.None,
                    out var referencedCompileTimeProject ) );

            using ( var peStream = File.Create( referencedPath ) )
            {
                Assert.True(
                    referencedCompilation.Emit(
                            peStream,
                            manifestResources: [referencedCompileTimeProject!.ToResource().Resource] )
                        .Success );
            }

            // Create the referencing compile-time project.
            Assert.True(
                loader.TryGetCompileTimeProjectFromCompilation(
                    testContext.CreateCSharpCompilation(
                        referencingCode,
                        additionalReferences: [MetadataReference.CreateFromFile( referencedPath )] ),
                    null,
                    diagnosticBag,
                    false,
                    CancellationToken.None,
                    out var compileTimeProject ) );

            Assert.NotNull( compileTimeProject );
            Assert.Single( compileTimeProject.References, r => !r.IsFramework );
        }

        [Fact]
        public void RewriteTypeOf()
        {
            const string code = """
                                using System;
                                using Metalama.Framework.Aspects; 

                                [CompileTime]
                                public class CompileTimeOnlyClass
                                {
                                   static Type Type1 = typeof(RunTimeOnlyClass);
                                   static Type Type2 = typeof(CompileTimeOnlyClass);
                                   static string Name1 = nameof(RunTimeOnlyClass);
                                   static string Name2 = nameof(CompileTimeOnlyClass);
                                
                                   void Method() { var t = typeof(RunTimeOnlyClass); }
                                   string Property => nameof(RunTimeOnlyClass);
                                }

                                public class RunTimeOnlyClass
                                {
                                   static Type Type1 = typeof(RunTimeOnlyClass);
                                   static Type Type3 = typeof(CompileTimeOnlyClass);

                                }
                                """;

            const string expected = """
                                    using global::System;
                                    using global::Metalama.Framework.Aspects;

                                    [CompileTime]
                                    public class CompileTimeOnlyClass
                                    {
                                       static global::System.Type Type1 = global::Metalama.Framework.CompileTimeContracts.TypeOfResolver.Resolve("typeof(global::RunTimeOnlyClass)",((string?)null),"RunTimeOnlyClass","RunTimeOnlyClass","RunTimeOnlyClass");
                                       static global::System.Type Type2 = typeof(global::CompileTimeOnlyClass);
                                       static string Name1 = "RunTimeOnlyClass";
                                       static string Name2 = "CompileTimeOnlyClass";
                                    
                                       void Method() { var t = global::Metalama.Framework.CompileTimeContracts.TypeOfResolver.Resolve("typeof(global::RunTimeOnlyClass)",((string?)null),"RunTimeOnlyClass","RunTimeOnlyClass","RunTimeOnlyClass"); }
                                       string Property => "RunTimeOnlyClass";
                                    }

                                    """;

            using var testContext = this.CreateTestContext();

            var compilation = testContext.CreateCSharpCompilation( code, assemblyName: "test" );

            var domain = testContext.Domain;

            var loader = new CompileTimeProjectRepository.Builder( domain, testContext.ServiceProvider );

            DiagnosticBag diagnosticBag = new();

            Assert.True(
                loader.TryGetCompileTimeProjectFromCompilation(
                    compilation,
                    null,
                    diagnosticBag,
                    false,
                    CancellationToken.None,
                    out var compileTimeProject ) );

            var transformed = File.ReadAllText( Path.Combine( compileTimeProject!.Directory!, compileTimeProject.CodeFiles[0].TransformedPath ) );

            AssertEx.EolInvariantEqual( expected, transformed );

            // We are not testing the rewriting of typeof in a template because this is done by the template compiler and covered by template tests.
        }

        [Fact]
        public void NoBuildTimeCodeNoDependency()
        {
            using var testContext = this.CreateTestContext();

            const string code = @"
using System;
using Metalama.Framework.Advising; 
using Metalama.Framework.Aspects; 

public class SomeRunTimeClass
{
}

";

            var roslynCompilation = testContext.CreateCSharpCompilation( code );
            var domain = testContext.Domain;
            var loader1 = new CompileTimeProjectRepository.Builder( domain, testContext.ServiceProvider );
            DiagnosticBag diagnosticBag = new();

            Assert.True(
                loader1.TryGetCompileTimeProjectFromCompilation(
                    roslynCompilation,
                    null,
                    diagnosticBag,
                    false,
                    CancellationToken.None,
                    out var project ) );

            Assert.NotNull( project );
            Assert.Single( project.References );
            Assert.True( project.References[0].IsFramework );
        }

        [Fact]
        public void FormatCompileTimeCode()
        {
            using var testContext = this.CreateTestContext( new TestContextOptions { FormatCompileTimeCode = true } );

            const string code = @"
using System;
using Metalama.Framework.Advising; 
using Metalama.Framework.Aspects; 

public class MyAspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        return default;
    }
}

";

            var compileTimeCode = GetCompileTimeCode( testContext, code );

            Assert.Contains( "using Microsoft.CodeAnalysis", compileTimeCode, StringComparison.Ordinal );
        }

        private static string GetCompileTimeCode( TestContext testContext, string code, OutputKind outputKind = OutputKind.DynamicallyLinkedLibrary )
        {
            var compileTimeSyntaxTrees = GetCompileTimeCode( testContext, new Dictionary<string, string> { { "main.cs", code } }, outputKind );

            return compileTimeSyntaxTrees
                .Single( x => !x.Key.StartsWith( "__", StringComparison.Ordinal ) )
                .Value;
        }

        private static IReadOnlyDictionary<string, string> GetCompileTimeCode(
            TestContext testContext,
            IReadOnlyDictionary<string, string> code,
            OutputKind outputKind = OutputKind.DynamicallyLinkedLibrary )
        {
            var roslynCompilation = testContext.CreateCSharpCompilation( code, outputKind: outputKind );
            var domain = testContext.Domain;
            var loader1 = new CompileTimeProjectRepository.Builder( domain, testContext.ServiceProvider );
            DiagnosticBag diagnosticBag = new();

            Assert.True(
                loader1.TryGetCompileTimeProjectFromCompilation(
                    roslynCompilation,
                    null,
                    diagnosticBag,
                    false,
                    CancellationToken.None,
                    out var project ) );

            Assert.NotNull( project );
            Assert.NotNull( project.Directory );

            // Just test that the output file has gone through formatting (we don't test that the whole formatting is correct). 
            var files = Directory
                .GetFiles( project.Directory!, "*.cs" )
                .Where( f => !CompileTimeConstants.IsPredefinedSyntaxTree( f ) );

            return files.ToImmutableDictionary( f => Path.GetFileName( f ), File.ReadAllText );
        }

        [Fact]
        public void EmptyNamespacesAreRemovedFromCompileTimeAssembly()
        {
            using var testContext = this.CreateTestContext();

            // The namespace Ns should be removed because it does not contain any build-time code.
            var compileTimeCode = GetCompileTimeCode(
                testContext,
                new Dictionary<string, string>
                {
                    ["BuildTime.cs"] = "class Aspect : Metalama.Framework.Aspects.MethodAspect {}",
                    ["RunTime.cs"] = @"namespace Ns { class C {} } ",
                    ["Both.cs"] =
                        "namespace Ns1 { class Aspect : Metalama.Framework.Aspects.MethodAspect {} } namespace Ns2 { class C {} }"
                } );

            // Test that run-time-only trees are removed from the build-time compilation.
            Assert.DoesNotContain( compileTimeCode.Keys, k => k.StartsWith( "RunTime_", StringComparison.OrdinalIgnoreCase ) );

            // Test that run-time-only namespaces are removed from the build-time compilation.
            Assert.DoesNotContain(
                "namespace Ns2",
                compileTimeCode.Single( p => p.Key.StartsWith( "Both_", StringComparison.OrdinalIgnoreCase ) ).Value,
                StringComparison.Ordinal );
        }

        [Fact]
        public void TopLevelStatementsAreRemoved()
        {
            using var testContext = this.CreateTestContext( new TestContextOptions { FormatCompileTimeCode = true } );

            const string code = @"
using System;
using Metalama.Framework.Advising; 
using Metalama.Framework.Aspects; 

Method();

void Method() { }
int field;

[CompileTime]
class CompileTimeClass { }
";

            var compileTimeCode = GetCompileTimeCode( testContext, code, OutputKind.ConsoleApplication );

            const string expected = @"
using System;
using Metalama.Framework.Advising; 
using Metalama.Framework.Aspects; 

[CompileTime]
class CompileTimeClass { }
";

            AssertEx.EolInvariantEqual( expected, compileTimeCode );
        }

        [Fact]
        public void FabricClassesAreUnNested()
        {
            using var testContext = this.CreateTestContext( new TestContextOptions { FormatCompileTimeCode = true } );

            const string code = @"
using System;
using Metalama.Framework.Fabrics;

public class SomeClass
{
    class Fabric : TypeFabric { public override void AmendType( ITypeAmender amender ) {} }
}

namespace SomeNamespace
{
    class OtherClass<T>
    {
        class NestedTwice
        {
            class Fabric  : TypeFabric { public override void AmendType( ITypeAmender amender ) {} }
        }
    }
}
";

            var compileTimeCode = GetCompileTimeCode( testContext, code );

            const string expected = @"
using System;
using Metalama.Framework.Fabrics;
using Metalama.Framework.CompileTimeContracts;
using Metalama.Framework.Serialization;

[OriginalPath(""main.cs"")]
[OriginalId(""T:SomeClass.Fabric"")]
internal class SomeClass_Fabric : TypeFabric
{
    public override void AmendType(ITypeAmender amender) { }
    public SomeClass_Fabric()
    {
    }
    protected SomeClass_Fabric(IArgumentsReader reader)
    {
    }
    public class Serializer : ReferenceTypeSerializer
    {
        public Serializer()
        {
        }

        public override object CreateInstance(Type type, IArgumentsReader constructorArguments)
        {
            return new global::SomeClass_Fabric(constructorArguments);
        }

        public override void SerializeObject(object obj, IArgumentsWriter constructorArguments, IArgumentsWriter initializationArguments)
        {
        }

        public override void DeserializeFields(object obj, IArgumentsReader initializationArguments)
        {
        }
    }
}

namespace SomeNamespace
{
    [OriginalPath(""main.cs"")]
    [OriginalId(""T:SomeNamespace.OtherClass`1.NestedTwice.Fabric"")]
    internal class OtherClassX1_NestedTwice_Fabric : TypeFabric
    {
        public override void AmendType(ITypeAmender amender) { }
        public OtherClassX1_NestedTwice_Fabric()
        {
        }
        protected OtherClassX1_NestedTwice_Fabric(IArgumentsReader reader)
        {
        }
        public class Serializer : ReferenceTypeSerializer
        {
            public Serializer()
            {
            }

            public override object CreateInstance(Type type, IArgumentsReader constructorArguments)
            {
                return new global::SomeNamespace.OtherClassX1_NestedTwice_Fabric(constructorArguments);
            }

            public override void SerializeObject(object obj, IArgumentsWriter constructorArguments, IArgumentsWriter initializationArguments)
            {
            }

            public override void DeserializeFields(object obj, IArgumentsReader initializationArguments)
            {
            }
        }
    }
}
";

            AssertEx.EolInvariantEqual( expected, compileTimeCode );
        }

        [Fact]
        public void CompileTypeTypesOfAllTKindsAreCopied()
        {
            using var testContext = this.CreateTestContext( new TestContextOptions { FormatCompileTimeCode = true } );

            const string code = @"
using System;
using Metalama.Framework.Advising; 
using Metalama.Framework.Aspects; 

namespace System.Runtime.CompilerServices { internal static class IsExternalInit {} }

[CompileTime]
public class SomeClass
{
    public void M() {}
}

[CompileTime]
public struct SomeStruct
{
    public void M() {}
}

[CompileTime]
public interface SomeInterface
{
    void M();
}

[CompileTime]
public record SomeRecord( int P );

[CompileTime]
public delegate void SomeDelegate();
";

            var compileTimeCode = GetCompileTimeCode( testContext, code );

            const string expected = @"
using System;
using Metalama.Framework.Advising; 
using Metalama.Framework.Aspects; 

[CompileTime]
public class SomeClass
{
    public void M() { }
}

[CompileTime]
public struct SomeStruct
{
    public void M() { }
}

[CompileTime]
public interface SomeInterface
{
    void M();
}

[CompileTime]
public record SomeRecord(int P);

[CompileTime]
public delegate void SomeDelegate();
";

            AssertEx.EolInvariantEqual( expected, compileTimeCode );
        }

        [Fact]
        public void SyntaxTreeWithOnlyCompileTimeInterfaceIsCopied()
        {
            using var testContext = this.CreateTestContext( new TestContextOptions { FormatCompileTimeCode = true } );

            const string code = @"
using System;
using Metalama.Framework.Advising; 
using Metalama.Framework.Aspects; 

[CompileTime]
public interface SomeInterface
{
    void M();
}
";

            var compileTimeCode = GetCompileTimeCode( testContext, code );

            const string expected = @"
using System;
using Metalama.Framework.Advising; 
using Metalama.Framework.Aspects; 

[CompileTime]
public interface SomeInterface
{
    void M();
}
";

            AssertEx.EolInvariantEqual( expected, compileTimeCode );
        }

        [Fact]
        public async Task CacheWithPreprocessorSymbolsAsync()
        {
            // Create a compilation that depends on a preprocessor symbol.
            using var testContext1 = this.CreateTestContext();

            const string code1 = @"
using Metalama.Framework.Advising; 
using Metalama.Framework.Aspects; 
[assembly: CompileTime]
#if SYMBOL
public class ReferencedClass
{
}
#else
Intentional syntax error.
#endif
";

            var compilation1 = testContext1.CreateCSharpCompilation( code1, preprocessorSymbols: ["METALAMA", "SYMBOL"] );

            using var domain1 = testContext1.Domain;
            var pipeline1 = new CompileTimeAspectPipeline( testContext1.ServiceProvider );

            var pipelineResult1 = await pipeline1.ExecuteAsync(
                NullDiagnosticAdder.Instance.Report,
                null,
                compilation1,
                ImmutableArray<ManagedResource>.Empty );

            Assert.True( pipelineResult1.IsSuccessful );

            var peFilePath = Path.Combine( testContext1.BaseDirectory, "reference.dll" );

            // ReSharper disable once UseAwaitUsing
            using ( var peFile = File.Create( peFilePath ) )
            {
                Assert.True(
                    pipelineResult1.Value.ResultingCompilation.Compilation.Emit(
                            peFile,
                            manifestResources: pipelineResult1.Value.AdditionalResources.Select( x => x.Resource ) )
                        .Success );
            }

            // Create compilation that references the compilation above, but
            // we use a different test context so that the cache of the first step is not used, however we can use only one domain per test.

            using var testContext2 = this.CreateTestContext();

            var compilation2 = testContext2.CreateCSharpCompilation(
                "",
                additionalReferences: [MetadataReference.CreateFromFile( peFilePath )] );

            var pipeline2 = new CompileTimeAspectPipeline( testContext2.ServiceProvider );
            DiagnosticBag diagnosticBag = new();
            var pipelineResult2 = await pipeline2.ExecuteAsync( diagnosticBag.Report, null, compilation2, ImmutableArray<ManagedResource>.Empty );

            Assert.True( pipelineResult2.IsSuccessful );
        }

        [Fact]
        public void PreprocessorDirectivesAreRemoved()
        {
            using var testContext = this.CreateTestContext( new TestContextOptions { FormatCompileTimeCode = true } );

            const string code = @"
#region Namespaces
using System;
using Metalama.Framework.Advising; 
using Metalama.Framework.Aspects; 
using StrippedNamespace;
#endregion

#region Using Attributes
#if SYMBOL
[assembly: RemainingNamespace.MyRunTimeAttribute]
#else
[assembly: RemainingNamespace.MyCompileTimeAttribute]
#endif
#endregion

#region Outside Namespace
namespace RemainingNamespace
{
#region Inside Namespace
[CompileTime]
public class MyCompileTimeAttribute : Attribute {}
#endregion

#region Defining MyRunTimeAttribute
public class MyRunTimeAttribute : Attribute 
{
#region BadRegion
}
#endregion
#endregion
}
#endregion

#region StrippedNamespace
namespace StrippedNamespace {
#region InsideNamespace A
#endregion
class C {
#region InsideClass
#endregion
}
#region InsideNamespace B
#endregion
}
#endregion
";

            var compileTimeCode = GetCompileTimeCode( testContext, code );

            const string expected = @"
using System;
using Metalama.Framework.Advising; 
using Metalama.Framework.Aspects; 

namespace RemainingNamespace
{
    [CompileTime]
    public class MyCompileTimeAttribute : Attribute { }
}
";

            AssertEx.EolInvariantEqual( expected, compileTimeCode );
        }

        [Fact]
        public void Manifest()
        {
            const string code = """
                                using System;
                                using Metalama.Framework.Advising;
                                using Metalama.Framework.Aspects; 

                                namespace Ns
                                {
                                    namespace Ns2
                                    {
                                        class Aspect1 : OverrideMethodAspect
                                        {
                                            public override dynamic? OverrideMethod() { return meta.Proceed(); }
                                        }
                                
                                        class Aspect2 : OverrideFieldOrPropertyAspect
                                        {
                                            public override dynamic? OverrideProperty
                                            {
                                                get => null!;
                                                set {}
                                            }
                                        }
                                
                                        class RunTimeOnlyClass {}
                                
                                        [CompileTime]
                                        class CompileTimeOnlyClass {}
                                
                                        class Aspect3 : TypeAspect 
                                        {
                                            [Template]
                                            void TemplateMethod<T1, [CompileTime] T2>( int runTimeParameter, [CompileTime] int compileTimeParameter ) {}
                                        }
                                    }

                                }


                                """;

            using var testContext = this.CreateTestContext();

            var roslynCompilation = testContext.CreateCSharpCompilation( code );
            var compilation = CompilationModel.CreateInitialInstance( new ProjectModel( roslynCompilation, testContext.ServiceProvider ), roslynCompilation );

            using var compileTimeDomain = testContext.Domain;
            var loader = CompileTimeProjectRepository.Create( compileTimeDomain, testContext.ServiceProvider, compilation.RoslynCompilation ).AssertNotNull();

            // Roundloop serialization.
            var json = loader.RootProject.Manifest!.ToJson();
            var manifest = CompileTimeProjectManifest.FromJson( json );

            // Test the execution scope.
            void AssertExecutionScope( ExecutionScope expectedScope, IDeclaration declaration )
            {
                Assert.Equal( expectedScope, manifest.Templates!.GetExecutionScope( declaration.GetSymbol().AssertNotNull() ) );
            }

            var aspect1Type = compilation.Types.OfName( "Aspect1" ).Single();
            AssertExecutionScope( ExecutionScope.RunTimeOrCompileTime, aspect1Type );
            AssertExecutionScope( ExecutionScope.CompileTime, aspect1Type.Methods.Single() );

            var aspect2Type = compilation.Types.OfName( "Aspect2" ).Single();
            AssertExecutionScope( ExecutionScope.RunTimeOrCompileTime, aspect2Type );
            AssertExecutionScope( ExecutionScope.CompileTime, aspect2Type.Properties.Single() );
            AssertExecutionScope( ExecutionScope.CompileTime, aspect2Type.Properties.Single().GetMethod! );

            AssertExecutionScope( ExecutionScope.RunTime, compilation.Types.OfName( "RunTimeOnlyClass" ).Single() );
            AssertExecutionScope( ExecutionScope.CompileTime, compilation.Types.OfName( "CompileTimeOnlyClass" ).Single() );

            var aspect3Method = compilation.Types.OfName( "Aspect3" ).Single().Methods.Single();
            AssertExecutionScope( ExecutionScope.RunTime, aspect3Method.TypeParameters[0] );
            AssertExecutionScope( ExecutionScope.CompileTime, aspect3Method.TypeParameters[1] );
            AssertExecutionScope( ExecutionScope.RunTime, aspect3Method.Parameters[0] );
            AssertExecutionScope( ExecutionScope.CompileTime, aspect3Method.Parameters[1] );

            // Test the template info.
            void AssertTemplateType( TemplateAttributeType expectedType, IDeclaration declaration )
            {
                var templateInfo = manifest.Templates!.GetTemplateInfo( declaration.GetSymbol()! );
                Assert.Equal( expectedType, templateInfo!.AttributeType );
            }

            AssertTemplateType( TemplateAttributeType.Template, aspect1Type.Methods.Single() );
        }

        [Fact]
        public void DiagnosticsAreCached()
        {
            var code = $$"""
                         using Metalama.Framework.Advising;
                         using Metalama.Framework.Aspects;
                         using Metalama.Framework.Code;

                         namespace NS_{{Guid.NewGuid():N}};

                         [CompileTime]
                         class C
                         {
                             [Template]
                             void Template(IField field)
                             {
                                 field.Value.M();
                             }
                         }
                         """;

            using var testContext = this.CreateTestContext();

            var roslynCompilation = testContext.CreateCSharpCompilation( code );
            var compilation = CompilationModel.CreateInitialInstance( new ProjectModel( roslynCompilation, testContext.ServiceProvider ), roslynCompilation );

            var diagnostics = new DiagnosticBag();

            using var domain = testContext.Domain;

            CompileTimeProjectRepository.Create( domain, testContext.ServiceProvider, compilation.RoslynCompilation, diagnostics );

            var warnings = new[] { "Dereference of a possibly null reference." };

            Assert.Equal( warnings, diagnostics.SelectAsArray( d => d.GetMessage( CultureInfo.InvariantCulture ) ) );

            diagnostics.Clear();

            CompileTimeProjectRepository.Create( domain, testContext.ServiceProvider, compilation.RoslynCompilation, diagnostics );

            Assert.Equal( warnings, diagnostics.SelectAsArray( d => d.GetMessage( CultureInfo.InvariantCulture ) ) );
        }

        [Fact]
        public void CompilationCanBeCollected()
        {
            using var testContext = this.CreateTestContext();
            using var domain = testContext.Domain;

            var result = CreateCompileTimeProject( testContext, domain );

            GC.Collect();
            Assert.False( result.WeakRef.IsAlive );
        }

        private static (CompileTimeProject Project, WeakReference WeakRef) CreateCompileTimeProject( TestContext testContext, CompileTimeDomain domain )
        {
            var code = $$"""
                         using Metalama.Framework.Advising;using Metalama.Framework.Aspects;
                         using Metalama.Framework.Code;

                         namespace NS_{{Guid.NewGuid():N}};

                         [CompileTime]
                         class C
                         {
                             [Template]
                             void Template(IField field)
                             {
                                 field.Value.M();
                             }
                         }
                         """;

            var roslynCompilation = testContext.CreateCSharpCompilation( code );
            var compilation = CompilationModel.CreateInitialInstance( new ProjectModel( roslynCompilation, testContext.ServiceProvider ), roslynCompilation );

            var diagnostics = new DiagnosticBag();

            var project = CompileTimeProjectRepository.Create( domain, testContext.ServiceProvider, compilation.RoslynCompilation, diagnostics )
                .AssertNotNull()
                .RootProject;

            return (project, new WeakReference( compilation ));
        }

        [Fact]
        public async Task AssemblyNameTruncated()
        {
            using var testContext = this.CreateTestContext( new TestContextOptions() { TempPathLength = 133 } );

            const string dependencyCode = """
                                          using Metalama.Framework.Aspects;
                                          using System;

                                          [Inheritable]
                                          [AttributeUsage( AttributeTargets.Class )]
                                          public class Aspect1 : TypeAspect { }

                                          [Aspect1]
                                          public class BaseClass { }
                                          """;

            const string mainCode = """
                                    public class TargetClass : BaseClass
                                    {
                                    }
                                    """;

            const string dependencyAssemblyName = "VeryVeryVeryLongNameThatShouldBeTrimmed";
            var dependencyCompilation = testContext.CreateCSharpCompilation( dependencyCode, assemblyName: dependencyAssemblyName );

            var mainCompilation = testContext.CreateCSharpCompilation(
                mainCode,
                additionalReferences: [dependencyCompilation.ToMetadataReference()] );

            var pipeline = new CompileTimeAspectPipeline( testContext.ServiceProvider );
            var result = await pipeline.ExecuteAsync( ThrowingDiagnosticAdder.Instance.Report, null, mainCompilation, ImmutableArray<ManagedResource>.Empty );
            Assert.True( result.IsSuccessful );

            var dependencyProject =
                result.Value.Configuration.CompileTimeProject.ClosureProjects.Single( p => p.RunTimeIdentity.Name == dependencyAssemblyName );

            // The name must have been trimmed, otherwise the test is worthless.
            Assert.DoesNotContain( dependencyAssemblyName, dependencyProject.CompileTimeIdentity.Name, StringComparison.Ordinal );
        }
    }
}