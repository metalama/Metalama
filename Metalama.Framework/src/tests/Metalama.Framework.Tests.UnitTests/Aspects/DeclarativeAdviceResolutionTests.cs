// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine;
using Metalama.Framework.Engine.Advising;
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
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Framework.Tests.UnitTests.Aspects;

/// <summary>
/// Tests the resolution of the declaration identifiers that an aspect class stores for its declarative advice.
/// </summary>
/// <remarks>
/// An aspect class records one <c>SerializableDeclarationId</c> per declarative advice member, and resolves it back to a
/// symbol every time the aspect runs. At design time the aspect class is reached from the pipeline configuration, which
/// is reused across compilations, so the compilation that the identifier is resolved against is not always the one it
/// was written from. Issue #2052 reports that an identifier which does not resolve was costing the user every
/// design-time service of the project.
/// </remarks>
public sealed class DeclarativeAdviceResolutionTests : UnitTestClass
{
    public DeclarativeAdviceResolutionTests( ITestOutputHelper logger ) : base( logger, false ) { }

    protected override void ConfigureServices( IAdditionalServiceCollection services )
    {
        base.ConfigureServices( services );
        services.AddProjectService( new PipelineExtensionProvider( ImmutableArray<PipelineExtension>.Empty ) );
    }

    private static string GetAspectCode( string fieldName, string methodName )
        => $$"""
             using Metalama.Framework.Aspects;

             public class MyAspect : TypeAspect
             {
                 [Introduce]
                 public int {{fieldName}};

                 [Introduce]
                 public int {{methodName}}() => 0;
             }
             """;

    /// <summary>
    /// Creates the aspect class of <c>MyAspect</c> from the given compilation, which is also the compilation that the
    /// identifiers of its declarative advice are written from.
    /// </summary>
    private static (AspectClass AspectClass, ProjectServiceProvider ServiceProvider) CreateAspectClass(
        TestContext testContext,
        CompilationModel compilation )
    {
        var compileTimeProjectRepository = CompileTimeProjectRepository.Create(
                testContext.Domain,
                testContext.ServiceProvider,
                compilation.RoslynCompilation,
                ThrowingDiagnosticAdder.Instance )
            .AssertNotNull();

        var serviceProvider = testContext.ServiceProvider.WithCompileTimeProjectServices( compileTimeProjectRepository );

        var diagnostics = new DiagnosticBag();

        var aspectClassFactory = new AspectClassFactory(
            new AspectDriverFactory( compilation, ImmutableArray<object>.Empty, serviceProvider, diagnostics ),
            compilation.CompilationContext );

        var aspectClasses = aspectClassFactory.GetClasses(
            serviceProvider,
            compilation.CompilationContext,
            ImmutableArray.Create( compilation.Types.OfName( "MyAspect" ).Single().GetSymbol() ),
            compileTimeProjectRepository.RootProject.AssertNotNull(),
            diagnostics );

        Assert.Empty( diagnostics );

        return (aspectClasses.Single( c => c.FullName == "MyAspect" ), serviceProvider);
    }

    /// <summary>
    /// Verifies that the declarative advice of an aspect class resolves against the compilation its identifiers were
    /// written from. This is the case that must keep working once an identifier that does not resolve is skipped.
    /// </summary>
    [Fact]
    public void DeclarativeAdviceResolvesInTheSameCompilation()
    {
        using var testContext = this.CreateTestContext();

        var compilation = testContext.CreateCompilationModel( GetAspectCode( "IntroducedField", "IntroducedMethod" ) );

        var (aspectClass, serviceProvider) = CreateAspectClass( testContext, compilation );

        var declarativeAdvice = aspectClass.GetDeclarativeAdvice( serviceProvider, compilation, default, ObjectReader.Empty )
            .Select( a => a.Symbol.Name )
            .OrderBy( name => name, StringComparer.Ordinal )
            .ToArray();

        Assert.Equal( ["IntroducedField", "IntroducedMethod"], declarativeAdvice );
    }

    /// <summary>
    /// Verifies that a declarative advice member whose identifier does not resolve in the compilation the aspect runs
    /// against is skipped, and that the members whose identifiers do resolve are still returned.
    /// </summary>
    /// <remarks>
    /// The second compilation declares the same aspect class with the named members renamed, so the identifiers written
    /// from the first compilation no longer resolve in it. Issue #2052 reports the two identifier shapes separately: a
    /// field identifier, <c>F:MyAspect.IntroducedField</c>, which carries no type, and a method identifier,
    /// <c>M:MyAspect.IntroducedMethod~System.Int32</c>, which carries a return type. Each is covered by its own case,
    /// and the case that renames both members verifies that nothing is returned rather than that an exception is
    /// thrown.
    /// </remarks>
    [Theory]
    [InlineData( "RenamedField", "IntroducedMethod", new[] { "IntroducedMethod" } )]
    [InlineData( "IntroducedField", "RenamedMethod", new[] { "IntroducedField" } )]
    [InlineData( "RenamedField", "RenamedMethod", new string[0] )]
    public void DeclarativeAdviceThatDoesNotResolveIsSkipped( string fieldName, string methodName, string[] expectedAdvice )
    {
        using var testContext = this.CreateTestContext();

        var compilation = testContext.CreateCompilationModel( GetAspectCode( "IntroducedField", "IntroducedMethod" ) );
        var otherCompilation = testContext.CreateCompilationModel( GetAspectCode( fieldName, methodName ) );

        var (aspectClass, serviceProvider) = CreateAspectClass( testContext, compilation );

        var declarativeAdvice = aspectClass.GetDeclarativeAdvice( serviceProvider, otherCompilation, default, ObjectReader.Empty )
            .Select( a => a.Symbol.Name )
            .OrderBy( name => name, StringComparer.Ordinal )
            .ToArray();

        Assert.Equal( expectedAdvice, declarativeAdvice );
    }
}
