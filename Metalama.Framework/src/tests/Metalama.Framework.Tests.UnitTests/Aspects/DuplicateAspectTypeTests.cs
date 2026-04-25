// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.CompileTime;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Extensibility;
using Metalama.Framework.Engine.Services;
using Metalama.Testing.UnitTesting;
using System.Collections.Immutable;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Framework.Tests.UnitTests.Aspects;

public sealed class DuplicateAspectTypeTests : UnitTestClass
{
    public DuplicateAspectTypeTests( ITestOutputHelper logger ) : base( logger, false ) { }

    protected override void ConfigureServices( IAdditionalServiceCollection services )
    {
        base.ConfigureServices( services );
        services.AddProjectService( new PipelineExtensionProvider( ImmutableArray<PipelineExtension>.Empty ) );
    }

    /// <summary>
    /// Tests that when duplicate aspect type entries are passed to the test-only <c>GetClasses</c>
    /// overload, it reports LAMA0290 instead of throwing <see cref="System.ArgumentException"/>.
    /// This covers the scenario from issue #614, where two versions of the same assembly are loaded
    /// and both provide the same aspect type name.
    /// </summary>
    [Fact]
    public void DuplicateAspectTypeFromDifferentAssemblies_ReportsDiagnostic()
    {
        const string aspectCode = @"
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
namespace TestNamespace
{
    public class MyAspect : TypeAspect { }
}
";

        using var testContext = this.CreateTestContext();

        var compilation = testContext.CreateCompilationModel( aspectCode );

        var serviceProvider = testContext.ServiceProvider;
        var compileTimeDomain = testContext.Domain;

        var compileTimeProjectRepository = CompileTimeProjectRepository.Create(
                compileTimeDomain,
                serviceProvider,
                compilation.RoslynCompilation,
                NullDiagnosticAdder.Instance )
            .AssertNotNull();

        var compileTimeProject = compileTimeProjectRepository.RootProject;
        serviceProvider = serviceProvider.WithCompileTimeProjectServices( compileTimeProjectRepository );

        var aspectTypeFactory = new AspectClassFactory(
            new AspectDriverFactory( compilation, ImmutableArray<object>.Empty, serviceProvider ),
            compilation.CompilationContext );

        var aspectTypeSymbol = compilation.Types.OfName( "MyAspect" ).Single().GetSymbol();

        // Pass the same type symbol twice to simulate duplicate entries (same reflection name from different assemblies).
        var diagnostics = new DiagnosticBag();

        // Before the fix, this would throw ArgumentException from ToDictionary.
        // After the fix, it should report a diagnostic and continue.
        _ = aspectTypeFactory.GetClasses(
            serviceProvider,
            compilation.CompilationContext,
            ImmutableArray.Create( aspectTypeSymbol, aspectTypeSymbol ),
            compileTimeProject,
            diagnostics );

        Assert.Contains(
            diagnostics,
            d => d.Id == "LAMA0290" );
    }
}