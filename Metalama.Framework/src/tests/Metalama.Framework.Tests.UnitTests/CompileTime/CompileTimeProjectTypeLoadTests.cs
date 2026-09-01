// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.CompileTime;
using Metalama.Framework.Engine.CompileTime.Manifest;
using Metalama.Testing.UnitTesting;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.CompileTime;

/// <summary>
/// Tests the creation of a <see cref="CompileTimeProject"/> whose compile-time assembly declares a type that cannot be
/// loaded. See issue #1870.
/// </summary>
/// <remarks>
/// The constructor of <see cref="CompileTimeProject"/> builds the diagnostic manifest of the project by enumerating the
/// types of the compile-time assembly. <see cref="Assembly.GetTypes"/> throws <see cref="ReflectionTypeLoadException"/>
/// when at least one type cannot be loaded, which happens when a referenced assembly cannot be bound. The exception used
/// to reach the design-time diagnostic analyzer, so the whole compile-time project became unavailable and the failure
/// repeated on every analysis pass.
/// </remarks>
public sealed class CompileTimeProjectTypeLoadTests : UnitTestClass
{
    private const string _missingAssemblyName = "Metalama.Tests.MissingReference";
    private const string _compileTimeAssemblyName = "Metalama.Tests.PartiallyLoadable";

    private const string _missingAssemblyCode = "public class MissingBase { }";

    private const string _compileTimeAssemblyCode = """
                                                    using Metalama.Framework.Diagnostics;

                                                    // This type cannot be loaded once the assembly of its base type has been deleted.
                                                    public class DerivedFromMissingBase : MissingBase { }

                                                    // This type has no unresolvable reference, so the loader returns it in ReflectionTypeLoadException.Types.
                                                    public static class Definitions
                                                    {
                                                        public static readonly DiagnosticDefinition<string> Warning = new( "MY001", Severity.Warning, "Warning: {0}." );

                                                        public static readonly SuppressionDefinition Suppression = new( "CS0169" );
                                                    }
                                                    """;

    /// <summary>
    /// Verifies that a <see cref="CompileTimeProject"/> is created when a type of the compile-time assembly cannot be
    /// loaded, and that its diagnostic manifest contains the definitions declared by the types that did load.
    /// </summary>
    [Fact]
    public void ProjectIsCreatedWhenATypeCannotBeLoaded()
    {
        using var testContext = this.CreateTestContext();
        using var domain = testContext.Domain;

        var directory = Path.Combine( Path.GetTempPath(), "Metalama.Tests", Guid.NewGuid().ToString() );
        Directory.CreateDirectory( directory );

        try
        {
            var missingAssemblyPath = EmitAssembly( testContext, directory, _missingAssemblyName, _missingAssemblyCode );

            var compileTimeAssemblyPath = EmitAssembly(
                testContext,
                directory,
                _compileTimeAssemblyName,
                _compileTimeAssemblyCode,
                MetadataReference.CreateFromFile( missingAssemblyPath ) );

            // The base type of one of the two types is now unresolvable.
            File.Delete( missingAssemblyPath );

            // The assembly must really fail to enumerate its types, otherwise the test does not cover the intended path.
            var assembly = domain.LoadAssembly( compileTimeAssemblyPath );
            var typeLoadException = Assert.Throws<ReflectionTypeLoadException>( () => assembly.GetTypes() );
            Assert.Contains( typeLoadException.Types, type => type == null );

            var project = CompileTimeProject.Create(
                testContext.ServiceProvider,
                domain,
                new AssemblyIdentity( _compileTimeAssemblyName ),
                new AssemblyIdentity( _compileTimeAssemblyName ),
                Array.Empty<CompileTimeProject>(),
                CreateManifest(),
                compileTimeAssemblyPath,
                directory,
                null,
                null );

            // The definitions of the type that did load must be in the manifest.
            Assert.Contains( "MY001", project.ClosureDiagnosticManifest.DiagnosticDefinitions.Keys );
            Assert.Contains( "CS0169", project.ClosureDiagnosticManifest.SuppressionDefinitions.Keys );
        }
        finally
        {
            TryDeleteDirectory( directory );
        }
    }

    private static CompileTimeProjectManifest CreateManifest()
        => new(
            "test",
            ".NET Framework, Version=4.8",
            [],
            [],
            [],
            [],
            [],
            [],
            null,
            null,
            0,
            [],
            [],
            false,
            0,
            LanguageVersion.Latest );

    private static string EmitAssembly(
        TestContext testContext,
        string directory,
        string assemblyName,
        string code,
        params MetadataReference[] additionalReferences )
    {
        var compilation = testContext.CreateCSharpCompilation(
            code,
            additionalReferences: additionalReferences,
            assemblyName: assemblyName );

        var path = Path.Combine( directory, assemblyName + ".dll" );

        using ( var stream = File.Create( path ) )
        {
            var result = compilation.Emit( stream );

            if ( !result.Success )
            {
                throw new InvalidOperationException(
                    $"Cannot emit '{assemblyName}': {string.Join( ", ", result.Diagnostics.Select( d => d.ToString() ) )}" );
            }
        }

        return path;
    }

    private static void TryDeleteDirectory( string path )
    {
        try
        {
            Directory.Delete( path, true );
        }
        catch ( IOException )
        {
            // The assembly is loaded in the compile-time domain, so the file may still be locked.
        }
        catch ( UnauthorizedAccessException )
        {
            // The assembly is loaded in the compile-time domain, so the file may still be locked.
        }
    }
}
