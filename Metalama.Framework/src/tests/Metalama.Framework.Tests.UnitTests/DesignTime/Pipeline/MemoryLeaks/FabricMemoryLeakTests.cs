// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Tests.UnitTestHelpers.Mocks;
using Metalama.Framework.Tests.UnitTestHelpers.TestClasses;
using Metalama.Testing.UnitTesting;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Framework.Tests.UnitTests.DesignTime.Pipeline.MemoryLeaks;

/// <summary>
/// Tests that a project that declares a fabric does not retain the version of the project from which the pipeline
/// configuration was built.
/// </summary>
/// <remarks>
/// <para>
/// A fabric contributes to the pipeline through an amender, and the amender is reachable from the
/// <c>AspectPipelineConfiguration</c>, which the design-time pipeline builds once and then reuses for every
/// subsequent version of the project. The configuration is discarded only when the compile-time code changes, so in a
/// session in which the user edits run-time code alone it is the configuration built from the very first version that
/// remains in use, however long the session lasts.
/// </para>
/// <para>
/// That reuse is correct in itself, and it is what makes the design-time pipeline fast. It is sound only as long as
/// the objects the configuration holds carry no reference bound to the compilation they were built from, because the
/// version so retained is the oldest of the session and is held in addition to the current one. This suite states
/// that property for the objects a fabric contributes.
/// </para>
/// <para>
/// The defect this suite reports is tracked by issue #1799.
/// </para>
/// <para>
/// The suite is the fabric counterpart of <see cref="DesignTimePipelineMemoryLeakTests"/>, which states the same
/// property for a project whose aspects are applied by attribute alone. A fabric is also the mechanism by which the
/// extension packages register their own contributors, so this is the path taken by a project that uses reference
/// validation.
/// </para>
/// </remarks>
public sealed class FabricMemoryLeakTests : DesignTimeTestBase
{
    /// <summary>
    /// The number of versions of the project that the design-time host is allowed to retain, for the same reasons as
    /// in <see cref="DesignTimePipelineMemoryLeakTests"/>: the version whose results it currently serves, plus one for
    /// a version that a difference not yet superseded still references.
    /// </summary>
    private const int _allowedSurvivingVersions = 2;

    private const string _aspectFileName = "Aspect.cs";
    private const string _fabricFileName = "Fabric.cs";
    private const string _targetFileName = "Target.cs";

    public FabricMemoryLeakTests( ITestOutputHelper logger ) : base( logger ) { }

    private const string _aspectCode = """
                                       using Metalama.Framework.Aspects;

                                       public class LogAttribute : OverrideMethodAspect
                                       {
                                           public override dynamic? OverrideMethod()
                                           {
                                               return meta.Proceed();
                                           }
                                       }
                                       """;

    /// <summary>
    /// A project fabric that adds an aspect to every method of the project.
    /// </summary>
    /// <remarks>
    /// The call to <c>AddAspect</c> is what makes these tests meaningful rather than vacuous. A fabric that registers
    /// no contributor leaves its amender unreachable from the pipeline configuration, so nothing of it survives the
    /// run and the assertions would hold for a reason unrelated to the property under test.
    /// </remarks>
    private const string _fabricCode = """
                                       using Metalama.Framework.Aspects;
                                       using Metalama.Framework.Fabrics;
                                       using System.Linq;

                                       public class TheFabric : ProjectFabric
                                       {
                                           public override void AmendProject( IProjectAmender amender )
                                               => amender.SelectMany( c => c.Types.SelectMany( t => t.Methods ) ).AddAspect<LogAttribute>();
                                       }
                                       """;

    /// <summary>
    /// Returns the content of the run-time file that the tests edit, for a given version.
    /// </summary>
    /// <remarks>
    /// The methods carry no aspect attribute, because the fabric is what applies the aspect here. Applying it twice
    /// would exercise the deduplication of aspect instances rather than the retention under test.
    /// </remarks>
    private static string GetTargetCode( int version )
        => $$"""
             public class Target
             {
                 public int Method()
                 {
                     var x = {{version}};
                     return x;
                 }

                 public string OtherMethod() => "{{version}}";
             }
             """;

    private static string GetFillerCode( int index )
        => $$"""
             public class Filler{{index}}
             {
                 public int Compute( int a, int b ) => a + b + {{index}};
             }
             """;

    /// <summary>
    /// Builds the initial content of the simulated project.
    /// </summary>
    /// <param name="withFabric">
    /// Whether the project declares a fabric. The two projects are otherwise identical, which is what makes the
    /// control case in <see cref="WithoutFabric_InitialCompilationIsCollected"/> discriminating.
    /// </param>
    private static Dictionary<string, string> CreateInitialCode( bool withFabric )
    {
        var code = new Dictionary<string, string> { [_aspectFileName] = _aspectCode, [_targetFileName] = GetTargetCode( 0 ) };

        if ( withFabric )
        {
            code[_fabricFileName] = _fabricCode;
        }

        for ( var i = 0; i < 3; i++ )
        {
            code[$"Filler{i}.cs"] = GetFillerCode( i );
        }

        return code;
    }

    /// <summary>
    /// Runs an editing session over the given project and returns a weak reference to the compilation of its first
    /// version, which is the version from which the pipeline configuration is built.
    /// </summary>
    /// <remarks>
    /// Only the run-time file is edited. An edit to the fabric or to the aspect is an edit to compile-time code, which
    /// makes the pipeline discard its configuration and build a new one, and would therefore hide the very retention
    /// this suite is about. It would also bring in the accumulation of the compiled compile-time assemblies, which is
    /// an accepted cost and to which no assertion here should be sensitive.
    /// </remarks>
    [MethodImpl( MethodImplOptions.NoInlining )]
    private WeakReference RunEditingSession(
        TestContext testContext,
        TestDesignTimeAspectPipelineFactory factory,
        bool withFabric,
        int editCount )
    {
        var simulator = new DesignTimeEditingSimulator(
            testContext,
            factory,
            $"FabricMemoryLeak_{withFabric}",
            CreateInitialCode( withFabric ) );

        var initialCompilation = simulator.GetWeakReferenceToCurrentCompilation();

        // This first run is the one that builds the pipeline configuration, from the version the weak reference above
        // points to.
        simulator.Execute();

        for ( var version = 1; version <= editCount; version++ )
        {
            simulator.ApplyEdit( _targetFileName, GetTargetCode( version ) );
        }

        var executionCount = simulator.GetPipeline().PipelineExecutionCount;
        this.TestOutput.WriteLine( $"The pipeline was executed {executionCount} times for {editCount} edits." );

        Assert.True(
            executionCount >= editCount,
            $"The pipeline was executed only {executionCount} times for {editCount} edits, therefore the test did not "
            + "exercise the retention it was supposed to exercise." );

        return initialCompilation;
    }

    /// <summary>
    /// An aspect whose template has a parameter of a type declared in the project.
    /// </summary>
    /// <remarks>
    /// The template members of an aspect class hold the types of their template parameters, and the aspect classes
    /// belong to the pipeline configuration. Whether that is a retention depends on which compilation the symbol comes
    /// from: a symbol of the compile-time projection is harmless, because that compilation has the same lifetime as the
    /// configuration and is discarded with it, whereas a symbol of the run-time compilation keeps the first version of
    /// the session alive. Neither the code nor the reachability report answers that question, which is what this test
    /// is for. See #1803.
    /// </remarks>
    private const string _aspectWithTemplateParameterCode = """
                                                            using Metalama.Framework.Aspects;

                                                            public class IntroduceAspect : TypeAspect
                                                            {
                                                                [Introduce]
                                                                public void IntroducedMethod( Filler0 parameter ) { }
                                                            }
                                                            """;

    private const string _fabricAddingIntroduceAspectCode = """
                                                            using Metalama.Framework.Aspects;
                                                            using Metalama.Framework.Fabrics;
                                                            using System.Linq;

                                                            public class TheFabric : ProjectFabric
                                                            {
                                                                public override void AmendProject( IProjectAmender amender )
                                                                    => amender.SelectTypes().Where( t => t.Name == "Filler1" ).AddAspect<IntroduceAspect>();
                                                            }
                                                            """;

    /// <summary>
    /// Determines whether an aspect whose template has a parameter of a type declared in the project retains the
    /// compilation of the first version.
    /// </summary>
    /// <remarks>
    /// The parameter type is <c>Filler0</c>, which is declared in a file the session never edits, so that the result
    /// cannot be attributed to the edited file. The aspect is applied to <c>Filler1</c> rather than to the edited type
    /// for the same reason.
    /// </remarks>
    [Fact]
    public void AspectWithATemplateParameterOfASourceType_InitialCompilationIsCollected()
    {
        using var testContext = this.CreateTestContext();
        using var factory = new TestDesignTimeAspectPipelineFactory( testContext );

        var initialCompilation = this.RunEditingSessionWithTemplateParameterAspect( testContext, factory, editCount: 10 );

        MemoryLeakAssert.Collected(
            initialCompilation,
            "The compilation from which the configuration of a project with a template parameter of a source type was built",
            ("pipelineFactory", factory),
            ("testContext", testContext),
            ("domain", testContext.Domain) );
    }

    [MethodImpl( MethodImplOptions.NoInlining )]
    private WeakReference RunEditingSessionWithTemplateParameterAspect(
        TestContext testContext,
        TestDesignTimeAspectPipelineFactory factory,
        int editCount )
    {
        var code = new Dictionary<string, string>
        {
            [_aspectFileName] = _aspectWithTemplateParameterCode,
            [_fabricFileName] = _fabricAddingIntroduceAspectCode,
            [_targetFileName] = GetTargetCode( 0 )
        };

        for ( var i = 0; i < 3; i++ )
        {
            code[$"Filler{i}.cs"] = GetFillerCode( i );
        }

        var simulator = new DesignTimeEditingSimulator( testContext, factory, "FabricMemoryLeak_TemplateParameter", code );

        var initialCompilation = simulator.GetWeakReferenceToCurrentCompilation();

        simulator.Execute();

        for ( var version = 1; version <= editCount; version++ )
        {
            simulator.ApplyEdit( _targetFileName, GetTargetCode( version ) );
        }

        var executionCount = simulator.GetPipeline().PipelineExecutionCount;
        this.TestOutput.WriteLine( $"The pipeline was executed {executionCount} times for {editCount} edits." );

        Assert.True(
            executionCount >= editCount,
            $"The pipeline was executed only {executionCount} times for {editCount} edits, therefore the test did not "
            + "exercise the retention it was supposed to exercise." );

        return initialCompilation;
    }

    /// <summary>
    /// Verifies that a project that declares a fabric releases the compilation of its first version once the user has
    /// made further edits to run-time code.
    /// </summary>
    /// <remarks>
    /// The compilation of the first version is the one from which the pipeline configuration was built. Because the
    /// configuration survives every run-time edit, anything it holds that is bound to that compilation keeps the
    /// oldest version of the project alive for the whole editing session, in addition to the current one.
    /// </remarks>
    [Fact]
    public void WithFabric_InitialCompilationIsCollected()
    {
        using var testContext = this.CreateTestContext();
        using var factory = new TestDesignTimeAspectPipelineFactory( testContext );

        var initialCompilation = this.RunEditingSession( testContext, factory, withFabric: true, editCount: 10 );

        MemoryLeakAssert.Collected(
            initialCompilation,
            "The compilation from which the pipeline configuration of a project with a fabric was built",
            ("pipelineFactory", factory) );
    }

    /// <summary>
    /// The control case for <see cref="WithFabric_InitialCompilationIsCollected"/>: the same project without the
    /// fabric.
    /// </summary>
    /// <remarks>
    /// Without this case a failure of the test above would not be attributable to the fabric, because the same
    /// assertion would fail for any project that retained its first version for an unrelated reason. The two projects
    /// differ by the fabric file alone.
    /// </remarks>
    [Fact]
    public void WithoutFabric_InitialCompilationIsCollected()
    {
        using var testContext = this.CreateTestContext();
        using var factory = new TestDesignTimeAspectPipelineFactory( testContext );

        var initialCompilation = this.RunEditingSession( testContext, factory, withFabric: false, editCount: 10 );

        MemoryLeakAssert.Collected(
            initialCompilation,
            "The compilation from which the pipeline configuration of a project without a fabric was built",
            ("pipelineFactory", factory) );
    }

    /// <summary>
    /// Verifies that the number of versions a project with a fabric retains does not grow with the number of edits.
    /// </summary>
    /// <remarks>
    /// This test and <see cref="WithFabric_InitialCompilationIsCollected"/> answer two different questions, and the
    /// combination of their outcomes is what characterises the defect. The configuration is built once, so a
    /// reference held by it reaches exactly one version however long the session is. A failure here instead would mean
    /// that the configuration, or something reachable from it, accumulates versions, which would be a different and
    /// worse defect.
    /// </remarks>
    [Theory]
    [InlineData( 5 )]
    [InlineData( 25 )]
    public void WithFabric_SurvivingCompilationCountDoesNotGrow( int editCount )
    {
        using var testContext = this.CreateTestContext();
        using var factory = new TestDesignTimeAspectPipelineFactory( testContext );

        var simulator = new DesignTimeEditingSimulator(
            testContext,
            factory,
            nameof(this.WithFabric_SurvivingCompilationCountDoesNotGrow),
            CreateInitialCode( withFabric: true ) );

        simulator.Execute();

        var compilations = new WeakReference[editCount];

        for ( var version = 1; version <= editCount; version++ )
        {
            compilations[version - 1] = simulator.EditAndExecute( _targetFileName, GetTargetCode( version ) );
        }

        MemoryLeakAssert.AtMostAlive(
            compilations,
            _allowedSurvivingVersions,
            $"compilations of a session of {editCount} edits in a project that declares a fabric",
            ("pipelineFactory", factory) );
    }
}
