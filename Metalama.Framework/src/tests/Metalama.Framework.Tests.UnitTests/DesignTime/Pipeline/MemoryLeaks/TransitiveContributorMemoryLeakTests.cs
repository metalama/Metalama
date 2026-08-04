// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.AdviceImpl.Introduction.Constructors;
using Metalama.Framework.Tests.UnitTestHelpers.Mocks;
using Metalama.Framework.Tests.UnitTestHelpers.TestClasses;
using System.Collections.Generic;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Framework.Tests.UnitTests.DesignTime.Pipeline.MemoryLeaks;

/// <summary>
/// Tests that the transitive pipeline contributors kept in the design-time results do not accumulate as the user
/// edits the project.
/// </summary>
/// <remarks>
/// <para>
/// A contributor is stored in the <c>SyntaxTreePipelineResult</c> of the file that declares its target, which the
/// pipeline carries forward from one version of the project to the next without re-analysing that file. The
/// production implementation holds a target declaration built from the code model and the syntax tree of that
/// declaration, so a contributor that survives an edit could pin the version of the project it was produced in, in
/// the same way as the inheritable aspect instances addressed by issue #1793.
/// </para>
/// <para>
/// The aspect below is the producer named in the audit of #1793: <c>PullStrategy.IntroduceParameterAndPull</c> makes
/// the advice export a transitive aspect onto the declaring type of the base constructor, which is a declaration the
/// aspect did not itself target and which lives in another file. That file is never edited by these tests, so its
/// result, and the contributor it carries, survive every subsequent version.
/// </para>
/// </remarks>
public sealed class TransitiveContributorMemoryLeakTests : DesignTimeTestBase
{
    public TransitiveContributorMemoryLeakTests( ITestOutputHelper logger ) : base( logger ) { }

    private const string _baseFileName = "Base.cs";
    private const string _derivedFileName = "Derived.cs";
    private const string _editedFileName = "Edited.cs";

    private const string _aspectCode = """
                                       using System;
                                       using Metalama.Framework.Advising;
                                       using Metalama.Framework.Aspects;
                                       using Metalama.Framework.Code;
                                       using Metalama.Framework.Code.SyntaxBuilders;

                                       public class PullAspect : TypeAspect
                                       {
                                           public override void BuildAspect( IAspectBuilder<INamedType> builder )
                                           {
                                               foreach ( var constructor in builder.Target.Constructors )
                                               {
                                                   builder.With( constructor )
                                                       .IntroduceParameter(
                                                           "creationTime",
                                                           typeof(DateTime),
                                                           pullStrategy: PullStrategy.IntroduceParameterAndPull(
                                                               forwarderExpression: ExpressionFactory.Parse( "global::System.DateTime.Now" ) ) );
                                               }
                                           }
                                       }
                                       """;

    /// <summary>
    /// The base class, whose constructor receives the pulled parameter. Its file is never edited.
    /// </summary>
    private const string _baseCode = """
                                     public class BaseClass
                                     {
                                         public int Id;

                                         public BaseClass( int id )
                                         {
                                             this.Id = id;
                                         }
                                     }
                                     """;

    private const string _derivedCode = """
                                        [PullAspect]
                                        public class DerivedClass : BaseClass
                                        {
                                            public DerivedClass( int id ) : base( id ) { }
                                        }
                                        """;

    /// <summary>
    /// Returns the content of the run-time file that the test edits.
    /// </summary>
    private static string GetEditedCode( int version )
        => $$"""
             public class Edited
             {
                 public int Method() => {{version}};
             }
             """;

    private static Dictionary<string, string> CreateInitialCode()
        => new()
        {
            ["Aspect.cs"] = _aspectCode,
            [_baseFileName] = _baseCode,
            [_derivedFileName] = _derivedCode,
            [_editedFileName] = GetEditedCode( 0 )
        };

    /// <summary>
    /// Verifies that the transitive contributor which survives the session does not retain the version of the project
    /// it was produced in.
    /// </summary>
    /// <remarks>
    /// The contributor reached a compilation by two routes, both now closed: the target declaration of the aspect
    /// instance that carries it, and the reference to the pulled parameter held by the aspect object itself. Both are
    /// durable references. Issue #1797 carries the history, including the measurement that established the second
    /// route and the reason it was not closed at the same time as the first.
    /// </remarks>
    [Fact]
    public void TransitiveContributor_DoesNotRetainTheCompilationItWasProducedIn()
    {
        using var testContext = this.CreateTestContext();
        using var factory = new TestDesignTimeAspectPipelineFactory( testContext );

        var simulator = new DesignTimeEditingSimulator(
            testContext,
            factory,
            nameof(this.TransitiveContributor_DoesNotRetainTheCompilationItWasProducedIn),
            CreateInitialCode() );

        var initialCompilation = simulator.GetWeakReferenceToCurrentCompilation();
        simulator.Execute();

        for ( var version = 1; version <= 10; version++ )
        {
            simulator.ApplyEdit( _editedFileName, GetEditedCode( version ) );
        }

        // Without a surviving contributor the assertion below would hold for a reason unrelated to what it measures.
        Assert.NotEmpty( simulator.GetPipeline().AspectPipelineResult.Extensions.Extensions );

        MemoryLeakAssert.Collected(
            initialCompilation,
            "The compilation in which the transitive contributor was produced",
            ("pipelineFactory", factory) );
    }

    /// <summary>
    /// Verifies that the number of transitive contributors the pipeline holds does not grow with the number of edits.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is the end-to-end counterpart of
    /// <see cref="SplitResultsByTreeExtensionAccumulationTests.ContributorOnTreeOutsidePartialCompilation_DoesNotAccumulate"/>,
    /// which drives the same property through <c>Update</c> directly.
    /// </para>
    /// <para>
    /// The contributor that survives is a single one, but it does retain the compilation it was produced in, through
    /// the reference held by the aspect object it carries. That is a bounded retention rather than an accumulation,
    /// it has a cause of its own, and it is tracked separately by issue #1797. This suite asserts the count only, so
    /// that a fix for the accumulation is not held up by the other defect.
    /// </para>
    /// </remarks>
    [Theory]
    [InlineData( 4 )]
    [InlineData( 16 )]
    public void TransitiveContributorCountDoesNotGrowWithEdits( int editCount )
    {
        using var testContext = this.CreateTestContext();
        using var factory = new TestDesignTimeAspectPipelineFactory( testContext );

        var simulator = new DesignTimeEditingSimulator(
            testContext,
            factory,
            nameof(this.TransitiveContributorCountDoesNotGrowWithEdits),
            CreateInitialCode() );

        simulator.Execute();

        var afterFirstRun = simulator.GetPipeline().AspectPipelineResult.Extensions.Extensions.Length;

        for ( var version = 1; version <= editCount; version++ )
        {
            simulator.ApplyEdit( _editedFileName, GetEditedCode( version ) );
        }

        var afterEdits = simulator.GetPipeline().AspectPipelineResult.Extensions.Extensions.Length;

        this.TestOutput.WriteLine( $"Contributors after the first run: {afterFirstRun}; after {editCount} edits: {afterEdits}." );

        Assert.Equal( afterFirstRun, afterEdits );
    }
}
