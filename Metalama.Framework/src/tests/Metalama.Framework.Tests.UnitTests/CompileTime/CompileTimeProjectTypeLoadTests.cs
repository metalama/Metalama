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
    /// <summary>
    /// The code of the assembly that is deleted before the compile-time assembly is loaded.
    /// </summary>
    private const string _missingAssemblyCode = "public class MissingBase { }";

    /// <summary>
    /// The code of a compile-time assembly of which one type can be loaded and the other one cannot.
    /// </summary>
    private const string _partiallyLoadableCode = """
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
    /// The code of a compile-time assembly of which no type can be loaded.
    /// </summary>
    private const string _notLoadableCode = "public class DerivedFromMissingBase : MissingBase { }";

    /// <summary>
    /// Verifies that a <see cref="CompileTimeProject"/> is created when a type of the compile-time assembly cannot be
    /// loaded, and that its diagnostic manifest contains the definitions declared by the type that did load.
    /// </summary>
    [Fact]
    public void ProjectIsCreatedWhenSomeTypesCannotBeLoaded()
    {
        using var testContext = this.CreateTestContext();
        using var domain = testContext.Domain;

        var project = CreateProjectWithUnloadableTypes( testContext, domain, "Metalama.Tests.PartiallyLoadable", _partiallyLoadableCode );

        Assert.Contains( "MY001", project.ClosureDiagnosticManifest.DiagnosticDefinitions.Keys );
        Assert.Contains( "CS0169", project.ClosureDiagnosticManifest.SuppressionDefinitions.Keys );
    }

    /// <summary>
    /// Verifies that a <see cref="CompileTimeProject"/> is created when no type of the compile-time assembly can be
    /// loaded, in which case its diagnostic manifest is empty.
    /// </summary>
    [Fact]
    public void ProjectIsCreatedWhenNoTypeCanBeLoaded()
    {
        using var testContext = this.CreateTestContext();
        using var domain = testContext.Domain;

        var project = CreateProjectWithUnloadableTypes( testContext, domain, "Metalama.Tests.NotLoadable", _notLoadableCode );

        Assert.True( project.ClosureDiagnosticManifest.IsEmpty );
    }

    /// <summary>
    /// Emits an assembly that declares the base type, then a compile-time assembly that references it, deletes the first
    /// one so that the types of the second one cannot all be loaded, and creates a <see cref="CompileTimeProject"/> for
    /// the second one.
    /// </summary>
    private static CompileTimeProject CreateProjectWithUnloadableTypes(
        TestContext testContext,
        CompileTimeDomain domain,
        string compileTimeAssemblyName,
        string compileTimeAssemblyCode )
    {
        // The base directory of the test context is deleted when the context is disposed, and the deletion waits for
        // the compile-time domain to be unloaded, so the assemblies emitted here need no explicit cleanup.
        var directory = Path.Combine( testContext.BaseDirectory, compileTimeAssemblyName );
        Directory.CreateDirectory( directory );

        var missingAssemblyPath = EmitAssembly( testContext, directory, compileTimeAssemblyName + ".MissingReference", _missingAssemblyCode );

        var compileTimeAssemblyPath = EmitAssembly(
            testContext,
            directory,
            compileTimeAssemblyName,
            compileTimeAssemblyCode,
            MetadataReference.CreateFromFile( missingAssemblyPath ) );

        // The base type of one of the types of the compile-time assembly is now unresolvable.
        File.Delete( missingAssemblyPath );

        // The assembly must really fail to enumerate its types, otherwise the test does not cover the intended path.
        var assembly = domain.LoadAssembly( compileTimeAssemblyPath );
        var typeLoadException = Assert.Throws<ReflectionTypeLoadException>( () => assembly.GetTypes() );
        Assert.Contains( typeLoadException.Types, type => type == null );

        return CompileTimeProject.Create(
            testContext.ServiceProvider,
            domain,
            new AssemblyIdentity( compileTimeAssemblyName ),
            new AssemblyIdentity( compileTimeAssemblyName ),
            Array.Empty<CompileTimeProject>(),
            CreateManifest(),
            compileTimeAssemblyPath,
            directory,
            null,
            null );
    }

    /// <summary>
    /// Creates a manifest that declares no compile-time code file, because the assembly of these tests is emitted by the
    /// test itself instead of being built by the compile-time compilation builder.
    /// </summary>
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

    /// <summary>
    /// Compiles <paramref name="code"/> into an assembly written to <paramref name="directory"/> and returns its path.
    /// </summary>
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
}
