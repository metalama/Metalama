// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine;
using Metalama.Framework.Engine.Advising;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.CompileTime;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Extensibility;
using Metalama.Framework.Engine.Services;
using Metalama.Testing.UnitTesting;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Framework.Tests.UnitTests.Aspects;

/// <summary>
/// Tests the resolution of the declaration identifiers that an aspect class stores for its declarative advice.
/// </summary>
/// <remarks>
/// An aspect class records one <see cref="SerializableDeclarationId"/> per declarative advice member, and resolves it
/// back to a symbol every time the aspect runs. At design time the aspect class is
/// reached from the pipeline configuration, which is reused across compilations, so the compilation an identifier is
/// resolved against is not always the one it was written from. Issue #2052 reports that an identifier which does not
/// resolve was costing the user every design-time service of the project.
/// </remarks>
public sealed class DeclarativeAdviceResolutionTests : UnitTestClass
{
    public DeclarativeAdviceResolutionTests( ITestOutputHelper logger ) : base( logger, false ) { }

    protected override void ConfigureServices( IAdditionalServiceCollection services )
    {
        base.ConfigureServices( services );
        services.AddProjectService( new PipelineExtensionProvider( ImmutableArray<PipelineExtension>.Empty ) );
        services.AddProjectService<IDiagnosticExtensionPolicy>( ConstantDiagnosticExtensionPolicy.None );
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
    /// Creates the aspect class of <c>MyAspect</c> from the given compilation, which is therefore also the compilation
    /// that the identifiers of its declarative advice are written from.
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
    /// written from, without a diagnostic. This is the case that must keep working once an identifier that does not
    /// resolve is skipped.
    /// </summary>
    [Fact]
    public void DeclarativeAdviceResolvesInTheSameCompilation()
    {
        using var testContext = this.CreateTestContext();

        var compilation = testContext.CreateCompilationModel( GetAspectCode( "IntroducedField", "IntroducedMethod" ) );

        var (aspectClass, serviceProvider) = CreateAspectClass( testContext, compilation );

        var diagnostics = new DiagnosticBag();

        var declarativeAdvice = aspectClass.GetDeclarativeAdvice( serviceProvider, compilation, default, ObjectReader.Empty, diagnostics )
            .Select( a => a.Symbol.Name )
            .OrderBy( name => name, StringComparer.Ordinal )
            .ToArray();

        Assert.Equal( ["IntroducedField", "IntroducedMethod"], declarativeAdvice );
        Assert.Empty( diagnostics );
    }

    /// <summary>
    /// Verifies that a declarative advice member whose identifier does not resolve in the compilation the aspect runs
    /// against is skipped and reported as an error, and that the members whose identifiers do resolve are still
    /// returned.
    /// </summary>
    /// <remarks>
    /// The second compilation declares the same aspect class with the named members renamed, so the identifiers written
    /// from the first compilation no longer resolve in it. Issue #2052 reports the two identifier shapes separately: a
    /// field identifier, which carries no type, and a method identifier, which carries a return type. Each is covered
    /// by its own case, and the third case renames both members.
    /// </remarks>
    [Theory]
    [InlineData( "RenamedField", "IntroducedMethod", new[] { "IntroducedMethod" }, new[] { "F:MyAspect.IntroducedField" } )]
    [InlineData( "IntroducedField", "RenamedMethod", new[] { "IntroducedField" }, new[] { "M:MyAspect.IntroducedMethod~System.Int32" } )]
    [InlineData(
        "RenamedField",
        "RenamedMethod",
        new string[0],
        new[] { "F:MyAspect.IntroducedField", "M:MyAspect.IntroducedMethod~System.Int32" } )]
    public void DeclarativeAdviceThatDoesNotResolveIsSkipped(
        string fieldName,
        string methodName,
        string[] expectedAdvice,
        string[] expectedUnresolvedIds )
    {
        using var testContext = this.CreateTestContext();

        var compilation = testContext.CreateCompilationModel( GetAspectCode( "IntroducedField", "IntroducedMethod" ) );
        var otherCompilation = testContext.CreateCompilationModel( GetAspectCode( fieldName, methodName ) );

        var (aspectClass, serviceProvider) = CreateAspectClass( testContext, compilation );

        var diagnostics = new DiagnosticBag();

        var declarativeAdvice = aspectClass.GetDeclarativeAdvice( serviceProvider, otherCompilation, default, ObjectReader.Empty, diagnostics )
            .Select( a => a.Symbol.Name )
            .OrderBy( name => name, StringComparer.Ordinal )
            .ToArray();

        Assert.Equal( expectedAdvice, declarativeAdvice );

        Assert.All( diagnostics, d => Assert.Equal( DiagnosticSeverity.Error, d.Severity ) );

        // Each identifier that does not resolve is named by exactly one error of the call.
        var reportedMessages = diagnostics
            .SelectAsArray( d => $"{d.Id}: {d.GetMessage( CultureInfo.InvariantCulture )}" )
            .OrderBy( message => message, StringComparer.Ordinal )
            .ToArray();

        Assert.Equal( expectedUnresolvedIds.Length, reportedMessages.Length );

        foreach ( var id in expectedUnresolvedIds )
        {
            Assert.Contains( reportedMessages, message => message.StartsWith( "LAMA0295: ", StringComparison.Ordinal ) && message.Contains( id, StringComparison.Ordinal ) );
        }
    }

    /// <summary>
    /// Verifies that the error is reported by every call, and that a <see cref="UserDiagnosticSink"/> reduces the
    /// repeated reports to one error per identifier.
    /// </summary>
    /// <remarks>
    /// The aspect class is asked for its declarative advice once per aspect instance, and each aspect instance has its
    /// own diagnostic sink. The error has to reach the sink of every instance, otherwise the outcome of an instance
    /// would depend on the order in which the instances were processed. The aspect class must therefore not remember
    /// which identifiers it has already reported, which also matters at design time, where the aspect class is reused
    /// across compilations. The user nevertheless sees the error only once, because the diagnostic carries a
    /// deduplication key and the diagnostics of all the aspect instances are collected by a single
    /// <see cref="UserDiagnosticSink"/>.
    /// </remarks>
    [Fact]
    public void DeclarativeAdviceThatDoesNotResolveIsReportedForEveryAspectInstance()
    {
        using var testContext = this.CreateTestContext();

        var compilation = testContext.CreateCompilationModel( GetAspectCode( "IntroducedField", "IntroducedMethod" ) );
        var otherCompilation = testContext.CreateCompilationModel( GetAspectCode( "RenamedField", "RenamedMethod" ) );

        var (aspectClass, serviceProvider) = CreateAspectClass( testContext, compilation );

        var firstAspectInstanceDiagnostics = new DiagnosticBag();
        var secondAspectInstanceDiagnostics = new DiagnosticBag();

        aspectClass.GetDeclarativeAdvice( serviceProvider, otherCompilation, default, ObjectReader.Empty, firstAspectInstanceDiagnostics )
            .ToReadOnlyList();

        aspectClass.GetDeclarativeAdvice( serviceProvider, otherCompilation, default, ObjectReader.Empty, secondAspectInstanceDiagnostics )
            .ToReadOnlyList();

        // Both aspect instances are given the error, therefore both have the same outcome.
        Assert.True( firstAspectInstanceDiagnostics.HasError );
        Assert.True( secondAspectInstanceDiagnostics.HasError );
        Assert.Equal( 2, firstAspectInstanceDiagnostics.Count );
        Assert.Equal( 2, secondAspectInstanceDiagnostics.Count );

        // The sink that collects the diagnostics of all the aspect instances keeps one error per identifier.
        var sink = new UserDiagnosticSink( serviceProvider );
        sink.Report( firstAspectInstanceDiagnostics );
        sink.Report( secondAspectInstanceDiagnostics );

        var collectedMessages = sink.ToImmutable()
            .ReportedDiagnostics
            .Select( d => d.GetMessage( CultureInfo.InvariantCulture ) )
            .ToArray();

        Assert.Equal( 2, collectedMessages.Length );

        foreach ( var id in new[] { "F:MyAspect.IntroducedField", "M:MyAspect.IntroducedMethod~System.Int32" } )
        {
            Assert.Single( collectedMessages, message => message.Contains( id, StringComparison.Ordinal ) );
        }
    }
}
