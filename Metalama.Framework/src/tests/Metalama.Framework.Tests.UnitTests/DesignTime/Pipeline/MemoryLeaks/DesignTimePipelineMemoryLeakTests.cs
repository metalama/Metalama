// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Tests.UnitTestHelpers.Mocks;
using Metalama.Framework.Tests.UnitTestHelpers.TestClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Framework.Tests.UnitTests.DesignTime.Pipeline.MemoryLeaks;

/// <summary>
/// Tests that a design-time editing session does not retain the Roslyn objects of the versions of the project that
/// the user has already replaced.
/// </summary>
/// <remarks>
/// <para>
/// Roslyn produces a new <see cref="Microsoft.CodeAnalysis.Compilation"/> and a new
/// <see cref="Microsoft.CodeAnalysis.SyntaxTree"/> for the edited file on every keystroke, and it releases the
/// previous ones as soon as the analysers it hosts do. A single retained compilation pins the syntax trees of the
/// whole project and the symbol tables of every referenced assembly, therefore a component that retains one
/// compilation per edit consumes memory in proportion to the length of the editing session. That is the shape of the
/// reported growth of the Roslyn analysis process to several gigabytes over several hours.
/// </para>
/// <para>
/// These tests deliberately restrict themselves to edits of run-time code. Editing the code of an aspect is known to
/// accumulate the compiled compile-time assemblies, which is an accepted cost, and a test that edited aspect code
/// could not distinguish that accepted cost from a genuine defect. The single exception is
/// <see cref="AspectEdits_CompilationsAreCollected"/>, which exists precisely to establish whether the accepted cost
/// is limited to the compile-time assemblies or whether it also retains compilations.
/// </para>
/// </remarks>
public sealed class DesignTimePipelineMemoryLeakTests : DesignTimeTestBase
{
    /// <summary>
    /// The number of surviving versions of the project that the design-time host is allowed to retain.
    /// </summary>
    /// <remarks>
    /// The host legitimately retains the version it has most recently analysed, because that is the version whose
    /// results it serves. One additional version is tolerated to account for a version that is still referenced by a
    /// diff that has not yet been superseded. Anything beyond that grows with the length of the session.
    /// </remarks>
    private const int _allowedSurvivingVersions = 2;

    /// <summary>
    /// The name of the file that contains the aspect, that is, the compile-time code.
    /// </summary>
    private const string _aspectFileName = "Aspect.cs";

    /// <summary>
    /// The name of the run-time file that the tests edit.
    /// </summary>
    private const string _targetFileName = "Target.cs";

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

    public DesignTimePipelineMemoryLeakTests( ITestOutputHelper logger ) : base( logger ) { }

    /// <summary>
    /// Returns the content of the run-time file that the tests edit, for a given version.
    /// </summary>
    /// <remarks>
    /// Only the body of a method changes from one version to the next. This is the cheapest possible edit and the one
    /// a user performs most often, therefore it is the one for which retention matters most.
    /// </remarks>
    private static string GetTargetCode( int version )
        => $$"""
             public class Target
             {
                 [Log]
                 public int Method()
                 {
                     var x = {{version}};
                     return x;
                 }

                 [Log]
                 public string OtherMethod() => "{{version}}";
             }
             """;

    /// <summary>
    /// Returns the content of a run-time file that the tests never edit, so that the compilation has a realistic size.
    /// </summary>
    private static string GetFillerCode( int index )
        => $$"""
             public class Filler{{index}}
             {
                 [Log]
                 public int Compute( int a, int b ) => a + b + {{index}};

                 public int Property { get; set; }
             }
             """;

    /// <summary>
    /// Returns the version of the aspect file for a given version, so that a test can edit compile-time code.
    /// </summary>
    private static string GetAspectCode( int version )
        => $$"""
             using Metalama.Framework.Aspects;

             public class LogAttribute : OverrideMethodAspect
             {
                 public override dynamic? OverrideMethod()
                 {
                     var version = {{version}};
                     return meta.Proceed();
                 }
             }
             """;

    /// <summary>
    /// Builds the initial content of the simulated project.
    /// </summary>
    private static Dictionary<string, string> CreateInitialCode( int fillerCount = 4 )
    {
        var code = new Dictionary<string, string> { [_aspectFileName] = _aspectCode, [_targetFileName] = GetTargetCode( 0 ) };

        for ( var i = 0; i < fillerCount; i++ )
        {
            code[$"Filler{i}.cs"] = GetFillerCode( i );
        }

        return code;
    }

    /// <summary>
    /// Verifies that the compilation of the first version of the project is released once the user has made further
    /// edits to run-time code.
    /// </summary>
    [Fact]
    public void RuntimeEdits_InitialCompilationIsCollected()
    {
        using var testContext = this.CreateTestContext();
        using var factory = new TestDesignTimeAspectPipelineFactory( testContext );

        var simulator = new DesignTimeEditingSimulator(
            testContext,
            factory,
            nameof(this.RuntimeEdits_InitialCompilationIsCollected),
            CreateInitialCode() );

        var initialCompilation = simulator.GetWeakReferenceToCurrentCompilation();
        simulator.Execute();

        var firstEditCompilation = simulator.EditAndExecute( _targetFileName, GetTargetCode( 1 ) );

        const int editCount = 10;

        for ( var version = 2; version <= editCount; version++ )
        {
            simulator.ApplyEdit( _targetFileName, GetTargetCode( version ) );
        }

        this.AssertPipelineDidWork( simulator, editCount );

        MemoryLeakAssert.Collected( initialCompilation, "The initial compilation", ("pipelineFactory", factory) );
        MemoryLeakAssert.Collected( firstEditCompilation, "The compilation of the first edit", ("pipelineFactory", factory) );
    }

    /// <summary>
    /// Verifies that the number of compilations that survive an editing session does not grow with the number of
    /// edits.
    /// </summary>
    /// <remarks>
    /// The same assertion is made for three session lengths. A component that retains one compilation per edit fails
    /// the longest session while possibly passing the shortest one, and the comparison between the three cases
    /// distinguishes a bounded cache from unbounded growth.
    /// </remarks>
    [Theory]
    [InlineData( 5 )]
    [InlineData( 15 )]
    [InlineData( 40 )]
    public void RuntimeEdits_SurvivingCompilationCountDoesNotGrow( int editCount )
    {
        using var testContext = this.CreateTestContext();
        using var factory = new TestDesignTimeAspectPipelineFactory( testContext );

        var simulator = new DesignTimeEditingSimulator(
            testContext,
            factory,
            nameof(this.RuntimeEdits_SurvivingCompilationCountDoesNotGrow),
            CreateInitialCode() );

        simulator.Execute();

        var compilations = new WeakReference[editCount];

        for ( var version = 1; version <= editCount; version++ )
        {
            compilations[version - 1] = simulator.EditAndExecute( _targetFileName, GetTargetCode( version ) );
        }

        this.AssertPipelineDidWork( simulator, editCount );

        MemoryLeakAssert.AtMostAlive(
            compilations,
            _allowedSurvivingVersions,
            $"compilations of an editing session of {editCount} edits",
            ("pipelineFactory", factory) );
    }

    /// <summary>
    /// Asserts that the pipeline actually analysed every version submitted to it.
    /// </summary>
    /// <remarks>
    /// Without this check, a test in which the pipeline silently declined to run every version would report that
    /// nothing is retained, which is true but meaningless. The expected count is one execution for the initial
    /// version plus one per edit.
    /// </remarks>
    private void AssertPipelineDidWork( DesignTimeEditingSimulator simulator, int editCount )
    {
        var executionCount = simulator.GetPipeline().PipelineExecutionCount;

        this.TestOutput.WriteLine( $"The pipeline was executed {executionCount} times for {editCount} edits." );

        Assert.True(
            executionCount >= editCount,
            $"The pipeline was executed only {executionCount} times for {editCount} edits, therefore the test did not "
            + "exercise the retention of the versions it was supposed to exercise." );
    }

    /// <summary>
    /// Verifies that the syntax trees of the versions of the edited file that the user has replaced are released.
    /// </summary>
    /// <remarks>
    /// A syntax tree can be retained independently of its compilation, for example by a
    /// <see cref="Microsoft.CodeAnalysis.Diagnostic"/> that a result cache keeps, because a diagnostic holds a
    /// location and a location holds its source tree. This test therefore complements
    /// <see cref="RuntimeEdits_SurvivingCompilationCountDoesNotGrow"/> instead of duplicating it.
    /// </remarks>
    [Fact]
    public void RuntimeEdits_ReplacedSyntaxTreesAreCollected()
    {
        using var testContext = this.CreateTestContext();
        using var factory = new TestDesignTimeAspectPipelineFactory( testContext );

        var simulator = new DesignTimeEditingSimulator(
            testContext,
            factory,
            nameof(this.RuntimeEdits_ReplacedSyntaxTreesAreCollected),
            CreateInitialCode() );

        simulator.Execute();

        const int editCount = 12;
        var syntaxTrees = new WeakReference[editCount];

        for ( var version = 1; version <= editCount; version++ )
        {
            syntaxTrees[version - 1] = EditAndGetWeakReferenceToEditedTree( simulator, version );
        }

        MemoryLeakAssert.AtMostAlive(
            syntaxTrees,
            _allowedSurvivingVersions,
            $"syntax trees of '{_targetFileName}'",
            ("pipelineFactory", factory) );
    }

    /// <summary>
    /// Applies one edit and returns a weak reference to the syntax tree that the edit produced.
    /// </summary>
    private static WeakReference EditAndGetWeakReferenceToEditedTree( DesignTimeEditingSimulator simulator, int version )
    {
        simulator.ApplyEdit( _targetFileName, GetTargetCode( version ) );

        return simulator.GetWeakReferenceToSyntaxTree( _targetFileName );
    }

    /// <summary>
    /// Verifies that run-time edits performed while the pipeline is paused do not retain the compilations they
    /// produce.
    /// </summary>
    /// <remarks>
    /// Editing compile-time code pauses the pipeline until the next external build. A user who has just edited an
    /// aspect and then continues to work on run-time code therefore keeps the pipeline paused for a long time, and
    /// every compilation produced during that period follows the code path that computes a difference from the last
    /// analysed version rather than from the immediately preceding one. This is the path most likely to accumulate
    /// versions, and it is common in practice.
    /// </remarks>
    [Fact]
    public void RuntimeEditsWhilePipelineIsPaused_CompilationsAreCollected()
    {
        using var testContext = this.CreateTestContext();
        using var factory = new TestDesignTimeAspectPipelineFactory( testContext );

        var simulator = new DesignTimeEditingSimulator(
            testContext,
            factory,
            nameof(this.RuntimeEditsWhilePipelineIsPaused_CompilationsAreCollected),
            CreateInitialCode() );

        simulator.Execute();

        // Edit the aspect, which pauses the pipeline.
        simulator.ApplyEdit( _aspectFileName, GetAspectCode( 1 ) );

        const int editCount = 20;
        var compilations = new WeakReference[editCount];

        for ( var version = 1; version <= editCount; version++ )
        {
            compilations[version - 1] = simulator.EditAndExecute( _targetFileName, GetTargetCode( version ) );
        }

        MemoryLeakAssert.AtMostAlive(
            compilations,
            _allowedSurvivingVersions,
            $"compilations produced while the pipeline was paused ({editCount} edits)",
            ("pipelineFactory", factory) );
    }

    /// <summary>
    /// Verifies that editing the code of an aspect does not retain the compilations of the versions that the user has
    /// replaced.
    /// </summary>
    /// <remarks>
    /// The accumulation of the compiled compile-time assemblies that results from editing an aspect is an accepted
    /// cost, because each version of the aspect must remain loadable. Retaining the Roslyn compilations of those
    /// versions is a different matter: it multiplies that accepted cost by the size of the whole project. This test
    /// therefore asserts the second property only, and a failure identifies an amplification of the accepted cost
    /// rather than the accepted cost itself.
    /// </remarks>
    [Fact]
    public void AspectEdits_CompilationsAreCollected()
    {
        using var testContext = this.CreateTestContext();
        using var factory = new TestDesignTimeAspectPipelineFactory( testContext );

        var simulator = new DesignTimeEditingSimulator(
            testContext,
            factory,
            nameof(this.AspectEdits_CompilationsAreCollected),
            CreateInitialCode() );

        simulator.Execute();

        const int editCount = 8;
        var compilations = new WeakReference[editCount];

        for ( var version = 1; version <= editCount; version++ )
        {
            compilations[version - 1] = simulator.EditAndExecute( _aspectFileName, GetAspectCode( version ) );
        }

        MemoryLeakAssert.AtMostAlive(
            compilations,
            _allowedSurvivingVersions,
            $"compilations of an aspect-editing session of {editCount} edits",
            ("pipelineFactory", factory) );
    }

    /// <summary>
    /// The name of the file that declares a type carrying an inheritable aspect. The tests never edit it.
    /// </summary>
    private const string _inheritanceRootFileName = "InheritanceRoot.cs";

    private const string _aspectCodeWithInheritableAspect = """
                                                            using Metalama.Framework.Advising;
                                                            using Metalama.Framework.Aspects;

                                                            public class LogAttribute : OverrideMethodAspect
                                                            {
                                                                public override dynamic? OverrideMethod()
                                                                {
                                                                    return meta.Proceed();
                                                                }
                                                            }

                                                            [Inheritable]
                                                            public class InheritedAspect : TypeAspect { }
                                                            """;

    private const string _inheritanceRootCode = """
                                                [InheritedAspect]
                                                public interface IInheritanceRoot { }
                                                """;

    /// <summary>
    /// Builds a project that produces an inheritable aspect instance in a file that the tests never edit.
    /// </summary>
    private static Dictionary<string, string> CreateCodeWithInheritableAspect()
        => new()
        {
            [_aspectFileName] = _aspectCodeWithInheritableAspect,
            [_inheritanceRootFileName] = _inheritanceRootCode,
            [_targetFileName] = GetTargetCode( 0 ),
            ["Filler0.cs"] = GetFillerCode( 0 )
        };

    /// <summary>
    /// Verifies that the results of the files that the pipeline did not re-analyse do not retain the compilation in
    /// which they were computed.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The design-time pipeline analyses only the syntax trees that changed, therefore the
    /// <c>SyntaxTreePipelineResult</c> of every other file survives, unchanged, from an earlier run. That is by
    /// design and is sound only as long as such a result holds no reference that is bound to the compilation it was
    /// computed from. Most of its members were reduced to serialisable identifiers for exactly that reason, but
    /// <c>InheritableAspects</c> holds an <c>InheritableAspectInstance</c> whose target declaration is a reference
    /// obtained from the code model.
    /// </para>
    /// <para>
    /// The file that carries the inheritable aspect is never edited here, so its result is produced once, during the
    /// first run, and is then carried forward through every subsequent version. If that result reaches the
    /// compilation of the first run, the first compilation stays alive for the whole session, and, because the
    /// pipeline also caches its results in a table keyed by compilation, each surviving version can in turn keep the
    /// previous one alive.
    /// </para>
    /// </remarks>
    [Fact]
    public void InheritableAspectInAnUneditedFile_DoesNotRetainTheFirstCompilation()
    {
        using var testContext = this.CreateTestContext();
        using var factory = new TestDesignTimeAspectPipelineFactory( testContext );

        var simulator = new DesignTimeEditingSimulator(
            testContext,
            factory,
            nameof(this.InheritableAspectInAnUneditedFile_DoesNotRetainTheFirstCompilation),
            CreateCodeWithInheritableAspect() );

        var initialCompilation = simulator.GetWeakReferenceToCurrentCompilation();
        simulator.Execute();

        const int editCount = 12;

        for ( var version = 1; version <= editCount; version++ )
        {
            simulator.ApplyEdit( _targetFileName, GetTargetCode( version ) );
        }

        this.AssertPipelineDidWork( simulator, editCount );

        var pipeline = simulator.GetPipeline();

        var inheritableAspectTypes = pipeline.AspectPipelineResult.InheritableAspectTypes.ToList();

        this.TestOutput.WriteLine(
            $"The pipeline holds {inheritableAspectTypes.Count} inheritable aspect type(s): {string.Join( ", ", inheritableAspectTypes )}." );

        // The test is meaningless if the project produced no inheritable aspect, because the retention path under
        // examination starts at the collection of inheritable aspects.
        Assert.NotEmpty( inheritableAspectTypes );

        MemoryLeakAssert.Collected(
            initialCompilation,
            "The compilation in which the inheritable aspect of the unedited file was computed",
            ("pipelineFactory", factory) );
    }

    /// <summary>
    /// Verifies that the number of versions retained during a session that produces inheritable aspects does not grow
    /// with the number of edits.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The previous test states whether the first version survives. This one states whether the whole history
    /// survives, which is the difference between a constant overhead and growth proportional to the length of the
    /// session.
    /// </para>
    /// <para>
    /// This test passes while <see cref="InheritableAspectInAnUneditedFile_DoesNotRetainTheFirstCompilation"/> fails,
    /// and the combination is the informative result. The pipeline caches its results in a table keyed by
    /// compilation, so a reference from the result of one version to an older version could have kept the whole
    /// history alive. It does not: exactly one stale version survives, whatever the length of the session. The cost
    /// of the defect is therefore one retained compilation per project, which is a constant overhead on a large
    /// solution rather than growth over the length of an editing session.
    /// </para>
    /// </remarks>
    [Theory]
    [InlineData( 5 )]
    [InlineData( 25 )]
    public void InheritableAspects_SurvivingCompilationCountDoesNotGrow( int editCount )
    {
        using var testContext = this.CreateTestContext();
        using var factory = new TestDesignTimeAspectPipelineFactory( testContext );

        var simulator = new DesignTimeEditingSimulator(
            testContext,
            factory,
            nameof(this.InheritableAspects_SurvivingCompilationCountDoesNotGrow),
            CreateCodeWithInheritableAspect() );

        simulator.Execute();

        var compilations = new WeakReference[editCount];

        for ( var version = 1; version <= editCount; version++ )
        {
            compilations[version - 1] = simulator.EditAndExecute( _targetFileName, GetTargetCode( version ) );
        }

        this.AssertPipelineDidWork( simulator, editCount );

        MemoryLeakAssert.AtMostAlive(
            compilations,
            _allowedSurvivingVersions,
            $"compilations of a session of {editCount} edits in a project that produces inheritable aspects",
            ("pipelineFactory", factory) );
    }

    /// <summary>
    /// Verifies that the design-time host does not retain the results it computed for versions that have been
    /// superseded.
    /// </summary>
    /// <remarks>
    /// The results of the pipeline are keyed by the path of a syntax tree, so their number is bounded by the number
    /// of files. This test guards that property, which is the reason why the results are keyed by path rather than by
    /// syntax tree instance, and which a future change could silently break.
    /// </remarks>
    [Fact]
    public void RuntimeEdits_ResultCountIsBoundedByFileCount()
    {
        using var testContext = this.CreateTestContext();
        using var factory = new TestDesignTimeAspectPipelineFactory( testContext );

        var code = CreateInitialCode();

        var simulator = new DesignTimeEditingSimulator(
            testContext,
            factory,
            nameof(this.RuntimeEdits_ResultCountIsBoundedByFileCount),
            code );

        simulator.Execute();

        for ( var version = 1; version <= 15; version++ )
        {
            simulator.ApplyEdit( _targetFileName, GetTargetCode( version ) );
        }

        var pipeline = simulator.GetPipeline();
        var resultCount = pipeline.AspectPipelineResult.SyntaxTreeResults.Count;

        this.TestOutput.WriteLine( $"The pipeline holds {resultCount} syntax tree results for {code.Count} files." );

        // One additional result is allowed, because diagnostics that have no source location are grouped under an
        // entry whose key is the empty string.
        Assert.True(
            resultCount <= code.Count + 1,
            $"The pipeline holds {resultCount} syntax tree results, but the project has only {code.Count} files. "
            + "The results are therefore not keyed by file path, and their number grows with the number of edits." );
    }
}
