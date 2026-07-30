// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine;
using Metalama.Framework.Engine.AspectWeavers;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.CompileTime;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Extensibility;
using Metalama.Framework.Engine.Services;
using Metalama.Testing.UnitTesting;
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Framework.Tests.UnitTests.Aspects;

/// <summary>
/// Tests how <see cref="AspectDriverFactory"/> handles several plug-ins contributing an aspect weaver under the
/// same type name.
/// </summary>
/// <remarks>
/// Covers issue #1743. <see cref="AspectDriverFactory"/> indexes weavers by type name, which is what
/// <c>RequireAspectWeaverAttribute</c> stores, so a collision-intolerant dictionary construction threw
/// <see cref="ArgumentException"/> ("An element with the same key but a different value already exists") out of
/// <c>AspectPipeline.TryInitialize</c>, which silently killed aspect code generation and diagnostics for the whole
/// project. Truly duplicate weavers are now deduplicated, and weavers that only share a type name are an error.
/// </remarks>
public sealed class DuplicateAspectWeaverTests : UnitTestClass
{
    private const string _code = @"
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
namespace TestNamespace
{
    public class MyAspect : TypeAspect { }
}
";

    public DuplicateAspectWeaverTests( ITestOutputHelper logger ) : base( logger, false ) { }

    protected override void ConfigureServices( IAdditionalServiceCollection services )
    {
        base.ConfigureServices( services );
        services.AddProjectService( new PipelineExtensionProvider( ImmutableArray<PipelineExtension>.Empty ) );
    }

    /// <summary>
    /// Tests that two truly duplicate weavers, i.e. two instances of the same type from the same assembly, are
    /// deduplicated silently.
    /// </summary>
    /// <remarks>
    /// This is what the same weaver assembly reaching the compilation twice produces. Plug-ins are instantiated with
    /// their default constructor, so the two instances are interchangeable and the duplication is not worth a
    /// user-visible diagnostic.
    /// </remarks>
    [Fact]
    public void DuplicateWeaverPlugIn_DoesNotThrow()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( _code );
        var serviceProvider = GetServiceProviderWithCompileTimeProject( testContext, compilation );

        var plugIns = ImmutableArray.Create<object>( new TestWeaver(), new TestWeaver() );

        var diagnostics = new DiagnosticBag();

        // Before the fix, this threw ArgumentException from ToImmutableDictionary.
        var aspectDriverFactory = new AspectDriverFactory( compilation, plugIns, serviceProvider, diagnostics );

        Assert.NotNull( aspectDriverFactory );
        Assert.Empty( diagnostics );
    }

    /// <summary>
    /// Tests that two weavers that share a type name but come from assemblies of a different identity are reported
    /// as <c>LAMA0077</c> instead of aborting pipeline initialization.
    /// </summary>
    /// <remarks>
    /// This is what two versions or two builds of the same aspect library in the reference graph produce. The
    /// instances are not interchangeable, and the type name is all that is left to tell them apart, so Metalama
    /// cannot know which one the user means.
    /// </remarks>
    [Fact]
    public void DuplicateWeaverPlugInFromDifferentAssemblies_ReportsError()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( _code );
        var serviceProvider = GetServiceProviderWithCompileTimeProject( testContext, compilation );

        var plugIns = ImmutableArray.Create<object>(
            CreateWeaverInDynamicAssembly( "DuplicateWeaverTestAssembly1" ),
            CreateWeaverInDynamicAssembly( "DuplicateWeaverTestAssembly2" ) );

        var diagnostics = new DiagnosticBag();

        var aspectDriverFactory = new AspectDriverFactory( compilation, plugIns, serviceProvider, diagnostics );

        Assert.NotNull( aspectDriverFactory );
        Assert.Contains( diagnostics, d => d.Id == "LAMA0077" );
    }

    /// <summary>
    /// Tests that the weaver kept for a duplicated type name does not depend on the order in which the plug-ins are
    /// supplied.
    /// </summary>
    /// <remarks>
    /// The plug-ins are ordered by the order of the references, which is not stable, so the choice is made on the
    /// assembly-qualified type name instead. The assembly named first in the <c>LAMA0077</c> message is the one that
    /// was kept, so comparing the message of both orders exercises the ordering.
    /// </remarks>
    [Fact]
    public void DuplicateWeaverPlugInFromDifferentAssemblies_KeepsTheSameWeaverInAnyOrder()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( _code );
        var serviceProvider = GetServiceProviderWithCompileTimeProject( testContext, compilation );

        var weaverA = CreateWeaverInDynamicAssembly( "DuplicateWeaverTestAssemblyA" );
        var weaverB = CreateWeaverInDynamicAssembly( "DuplicateWeaverTestAssemblyB" );

        var messageInGivenOrder = GetDuplicateWeaverMessage( ImmutableArray.Create<object>( weaverA, weaverB ) );
        var messageInReverseOrder = GetDuplicateWeaverMessage( ImmutableArray.Create<object>( weaverB, weaverA ) );

        Assert.Equal( messageInGivenOrder, messageInReverseOrder );

        // The weaver that is kept is the one of the assembly that sorts first, and the message names it before the
        // one that is ignored.
        var indexOfA = messageInGivenOrder.IndexOf( "DuplicateWeaverTestAssemblyA", StringComparison.Ordinal );
        var indexOfB = messageInGivenOrder.IndexOf( "DuplicateWeaverTestAssemblyB", StringComparison.Ordinal );

        Assert.True( indexOfA >= 0 );
        Assert.True( indexOfA < indexOfB );

        string GetDuplicateWeaverMessage( ImmutableArray<object> plugIns )
        {
            var diagnostics = new DiagnosticBag();
            _ = new AspectDriverFactory( compilation, plugIns, serviceProvider, diagnostics );

            return diagnostics.Single( d => d.Id == "LAMA0077" ).GetMessage();
        }
    }

    /// <summary>
    /// Returns a service provider that has the compile-time project services of the given compilation, which
    /// <see cref="AspectDriverFactory"/> requires.
    /// </summary>
    private static ProjectServiceProvider GetServiceProviderWithCompileTimeProject( TestContext testContext, CompilationModel compilation )
    {
        var compileTimeProjectRepository = CompileTimeProjectRepository.Create(
                testContext.Domain,
                testContext.ServiceProvider,
                compilation.RoslynCompilation,
                NullDiagnosticAdder.Instance )
            .AssertNotNull();

        return testContext.ServiceProvider.WithCompileTimeProjectServices( compileTimeProjectRepository );
    }

    /// <summary>
    /// Creates an <see cref="IAspectDriver"/> whose type has a fixed full name but belongs to an assembly of the
    /// given name, so that several instances can share a type name while having different assembly identities.
    /// </summary>
    /// <remarks>
    /// <see cref="IAspectDriver"/> has no members, so the emitted type needs no method body and no IL.
    /// </remarks>
    private static IAspectDriver CreateWeaverInDynamicAssembly( string assemblyName )
    {
        var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly( new AssemblyName( assemblyName ), AssemblyBuilderAccess.Run );
        var moduleBuilder = assemblyBuilder.DefineDynamicModule( assemblyName );

        var typeBuilder = moduleBuilder.DefineType(
            "TestNamespace.DuplicatedWeaver",
            TypeAttributes.Public | TypeAttributes.Class,
            typeof(object),
            new[] { typeof(IAspectDriver) } );

        return (IAspectDriver) Activator.CreateInstance( typeBuilder.CreateTypeInfo().AssertNotNull().AsType() ).AssertNotNull();
    }

    /// <summary>
    /// A minimal weaver used only to populate the plug-in list. It is never executed by these tests.
    /// </summary>
    [MetalamaPlugIn]
    private sealed class TestWeaver : IAspectWeaver
    {
        public Task TransformAsync( AspectWeaverContext context ) => Task.CompletedTask;
    }
}
