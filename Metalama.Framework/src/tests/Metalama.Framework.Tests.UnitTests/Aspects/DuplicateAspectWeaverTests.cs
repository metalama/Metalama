// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine;
using Metalama.Framework.Engine.AspectWeavers;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CompileTime;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Extensibility;
using Metalama.Framework.Engine.Services;
using Metalama.Testing.UnitTesting;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Framework.Tests.UnitTests.Aspects;

/// <summary>
/// Tests that duplicate aspect weaver plug-ins do not abort pipeline initialization.
/// </summary>
/// <remarks>
/// Covers issue #1743: when the same aspect library reaches the compilation through two routes (for instance
/// as a package and through a project reference), the plug-in loader yields two distinct objects whose type
/// name is the same. <see cref="AspectDriverFactory"/> indexes weavers by that name, so a collision-intolerant
/// dictionary construction throws <see cref="System.ArgumentException"/> ("An element with the same key but a
/// different value already exists") out of <c>AspectPipeline.TryInitialize</c>, which silently kills aspect
/// code generation and diagnostics for the whole project.
/// </remarks>
public sealed class DuplicateAspectWeaverTests : UnitTestClass
{
    public DuplicateAspectWeaverTests( ITestOutputHelper logger ) : base( logger, false ) { }

    protected override void ConfigureServices( IAdditionalServiceCollection services )
    {
        base.ConfigureServices( services );
        services.AddProjectService( new PipelineExtensionProvider( ImmutableArray<PipelineExtension>.Empty ) );
    }

    /// <summary>
    /// Tests that <see cref="AspectDriverFactory"/> tolerates two plug-in instances that report the same weaver
    /// type name, which is what happens when the same weaver assembly is loaded twice.
    /// </summary>
    [Fact]
    public void DuplicateWeaverPlugIn_DoesNotThrow()
    {
        const string code = @"
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
namespace TestNamespace
{
    public class MyAspect : TypeAspect { }
}
";

        using var testContext = this.CreateTestContext();

        var compilation = testContext.CreateCompilationModel( code );

        var serviceProvider = testContext.ServiceProvider;

        var compileTimeProjectRepository = CompileTimeProjectRepository.Create(
                testContext.Domain,
                serviceProvider,
                compilation.RoslynCompilation,
                NullDiagnosticAdder.Instance )
            .AssertNotNull();

        serviceProvider = serviceProvider.WithCompileTimeProjectServices( compileTimeProjectRepository );

        // Two distinct instances of the same weaver type, simulating the same weaver assembly loaded twice.
        var plugIns = ImmutableArray.Create<object>( new TestWeaver(), new TestWeaver() );

        // Before the fix, this threw ArgumentException from ToImmutableDictionary.
        var aspectDriverFactory = new AspectDriverFactory( compilation, plugIns, serviceProvider );

        Assert.NotNull( aspectDriverFactory );
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
