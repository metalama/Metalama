// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.DesignTime;
using Metalama.Framework.DesignTime.Pipeline;
using Metalama.Framework.DesignTime.SourceGeneration;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities.Threading;
using Metalama.Framework.Tests.UnitTestHelpers.Mocks;
using Metalama.Framework.Tests.UnitTestHelpers.TestClasses;
using Metalama.Testing.UnitTesting;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Framework.Tests.UnitTests.DesignTime.Pipeline.MemoryLeaks;

/// <summary>
/// Tests that the source generator of the analysis process does not retain the compilations whose analysis was
/// abandoned because a newer version arrived.
/// </summary>
/// <remarks>
/// <para>
/// Roslyn calls the source generator once per version of the compilation, that is, once per keystroke.
/// <c>AnalysisProcessProjectSourceGenerator.GenerateSources</c> serves the previous result immediately, cancels the
/// analysis of the previous version, and schedules the analysis of the new one on a background task whose delegate
/// closes over the new compilation. A user typing continuously therefore produces a long sequence of scheduled
/// analyses of which only the last one is useful.
/// </para>
/// <para>
/// These tests make the cancellation deterministic by supplying a factory that returns cancellation sources which are
/// already signalled. That is not an artificial situation: it is the limit case of the race that occurs whenever the
/// next keystroke arrives before the thread pool has started the task scheduled for the previous one, which is the
/// normal situation when a project is large enough for the analysis to take longer than the interval between two
/// keystrokes.
/// </para>
/// </remarks>
public sealed class SourceGeneratorMemoryLeakTests : DesignTimeTestBase
{
    public SourceGeneratorMemoryLeakTests( ITestOutputHelper logger ) : base( logger ) { }

    private const string _aspectFileName = "Aspect.cs";
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

    /// <summary>
    /// A factory of cancellation sources that are signalled from the moment they are created.
    /// </summary>
    /// <remarks>
    /// It reproduces, deterministically, the outcome of the race in which a scheduled analysis is cancelled by the
    /// arrival of the next version before the thread pool has started it.
    /// </remarks>
    private sealed class AlreadyCancelledTokenSourceFactory : ITestableCancellationTokenSourceFactory
    {
        public TestableCancellationTokenSource Create()
        {
            var source = new TestableCancellationTokenSource();
            source.CancellationTokenSource.Cancel();

            return source;
        }
    }

    /// <summary>
    /// Returns the content of the run-time file for a given version.
    /// </summary>
    private static string GetTargetCode( int version )
        => $$"""
             public class Target
             {
                 [Log]
                 public int Method() => {{version}};
             }
             """;

    /// <summary>
    /// Verifies that the versions whose analysis was cancelled before it started are not retained by the source
    /// generator.
    /// </summary>
    [Fact]
    public void CancelledAnalyses_DoNotRetainTheirCompilations()
    {
        using var testContext = this.CreateTestContext( new TestContextOptions { HasSourceGeneratorTouchFile = true } );

        GlobalServiceProvider serviceProvider = testContext.ServiceProvider;
        serviceProvider = serviceProvider.Underlying.WithService( new AnalysisProcessEventHub( serviceProvider ) );

        using var pipelineFactory = new TestDesignTimeAspectPipelineFactory( testContext, serviceProvider );

        var generatorServiceProvider = serviceProvider.Underlying
            .WithService( pipelineFactory )
            .WithService( new AlreadyCancelledTokenSourceFactory(), allowOverride: true );

        var projectKey = ProjectKeyFactory.CreateTest( nameof(this.CancelledAnalyses_DoNotRetainTheirCompilations) );

        var sourceGenerator = new AnalysisProcessProjectSourceGenerator(
            generatorServiceProvider,
            testContext.ProjectOptions,
            projectKey );

        try
        {
            const int editCount = 15;
            var compilations = RunEditingSession( testContext, sourceGenerator, editCount );

            MemoryLeakAssert.AtMostAlive(
                compilations,
                2,
                $"compilations submitted to the source generator during {editCount} edits",
                ("sourceGenerator", sourceGenerator),
                ("pipelineFactory", pipelineFactory) );
        }
        finally
        {
            // The source generator is not disposed by a using statement, because disposing it waits for the pending
            // tasks and therefore throws when one of them was cancelled. An exception raised while the stack is
            // unwinding replaces the exception that the assertion raised, and would hide the result of the test.
            try
            {
                sourceGenerator.Dispose();
            }
            catch ( Exception e )
            {
                this.TestOutput.WriteLine( $"Disposing the source generator failed, which is a second symptom of the same defect: {e.Message}" );
            }
        }
    }

    /// <summary>
    /// Submits a sequence of versions to the source generator and returns weak references to all of them.
    /// </summary>
    /// <remarks>
    /// Every strong reference to a compilation is confined to this method, so that none of them survives in the frame
    /// of the calling test method.
    /// </remarks>
    [MethodImpl( MethodImplOptions.NoInlining )]
    private static WeakReference[] RunEditingSession( TestContext testContext, ProjectSourceGenerator sourceGenerator, int editCount )
    {
        var code = new Dictionary<string, string> { [_aspectFileName] = _aspectCode, [_targetFileName] = GetTargetCode( 0 ) };

        Compilation current = testContext.CreateCSharpCompilation( code, assemblyName: "SourceGeneratorMemoryLeakTest" );
        var parseOptions = (CSharpParseOptions) current.SyntaxTrees.First().Options;

        // The first call is synchronous and populates the cache of the generator, which is the precondition for the
        // asynchronous path taken by all the subsequent calls.
        _ = sourceGenerator.GenerateSources( current, default );

        var weakReferences = new WeakReference[editCount];

        for ( var version = 1; version <= editCount; version++ )
        {
            var oldTree = current.SyntaxTrees.Single( t => t.FilePath == _targetFileName );
            var newTree = CSharpSyntaxTree.ParseText( GetTargetCode( version ), parseOptions, _targetFileName, oldTree.Encoding );

            current = current.ReplaceSyntaxTree( oldTree, newTree );

            _ = sourceGenerator.GenerateSources( current, default );

            weakReferences[version - 1] = new WeakReference( current );
        }

        return weakReferences;
    }
}
