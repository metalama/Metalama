// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Services;
using Metalama.Testing.AspectTesting;
using Metalama.Testing.AspectTesting.XunitFramework;
using Metalama.Testing.UnitTesting;
using SharpCrafters.Backstage.Infrastructure;
using SharpCrafters.Backstage.Testing;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using Xunit;
using Xunit.Sdk;

namespace Metalama.Framework.Tests.UnitTests.TestFramework;

/// <summary>
/// Tests the messages that the <see cref="TestExecutor"/> of the aspect test framework sends to xunit.
/// </summary>
public sealed class TestExecutorTests : UnitTestClass
{
    /// <summary>
    /// The content of a test file whose test passes.
    /// </summary>
    private const string _passingTest = "/* Empty */";

    /// <summary>
    /// The expected output of <see cref="_passingTest"/>.
    /// </summary>
    private const string _passingTestOutput = "// The compilation was successful.";

    /// <summary>
    /// The test files used by the tests of the finishing order and of cancellation: three tests in the class <c>A</c> and
    /// two in the class <c>B</c>.
    /// </summary>
    private static readonly string[] _fiveTestsInTwoClasses = [Path.Combine( "A", "T1" ), Path.Combine( "A", "T2" ), Path.Combine( "A", "T3" ), Path.Combine( "B", "T4" ), Path.Combine( "B", "T5" )];

    [Theory]
    [InlineData(
        "Error!",
        "**ERROR**",
        "TestAssemblyStarting,TestCollectionStarting,TestClassStarting,TestMethodStarting,TestCaseStarting,TestStarting,TestFailed,TestFinished,TestCaseFinished,TestMethodFinished,TestClassFinished,TestCollectionFinished,TestAssemblyFinished" )]
    [InlineData(
        _passingTest,
        _passingTestOutput,
        "TestAssemblyStarting,TestCollectionStarting,TestClassStarting,TestMethodStarting,TestCaseStarting,TestStarting,TestPassed,TestFinished,TestCaseFinished,TestMethodFinished,TestClassFinished,TestCollectionFinished,TestAssemblyFinished" )]
    [InlineData(
        """
        #if TEST_OPTIONS
        // @Skipped
        #endif
        """,
        "",
        "TestAssemblyStarting,TestCollectionStarting,TestClassStarting,TestMethodStarting,TestCaseStarting,TestStarting,TestSkipped,TestFinished,TestCaseFinished,TestMethodFinished,TestClassFinished,TestCollectionFinished,TestAssemblyFinished" )]
    public void EventSequence( string testInput, string expectedTestOutput, string expectedEventSequence )
    {
        using var testContext = this.CreateTestContext();

        var messages = RunTests( testContext, new Dictionary<string, (string, string)> { ["Test"] = (testInput, expectedTestOutput) } );

        var sequence = string.Join( ",", messages.SelectAsReadOnlyList( x => x.GetType().Name ).Where( x => x != "TestOutput" ) );

        Assert.Equal( expectedEventSequence, sequence );
    }

    /// <summary>
    /// Verifies that each class and the collection finish after their last test, and that their finishing messages report
    /// the final counts.
    /// </summary>
    [Theory]
    [InlineData( ParallelMode.None )]
    [InlineData( ParallelMode.All )]
    public void EachNodeFinishesAfterItsLastTest( ParallelMode parallelMode )
    {
        using var testContext = this.CreateTestContext();

        var messages = RunTests( testContext, PassingTests( _fiveTestsInTwoClasses ), parallelMode );

        var testsFinished = messages.OfType<ITestFinished>().ToList();
        Assert.Equal( 5, testsFinished.Count );

        var classesFinished = messages.OfType<ITestClassFinished>().ToList();
        Assert.Equal( 2, classesFinished.Count );

        foreach ( var classFinished in classesFinished )
        {
            var testsOfClass = testsFinished.Where( t => t.TestClassUniqueID == classFinished.TestClassUniqueID ).ToList();

            Assert.All( testsOfClass, t => Assert.True( messages.IndexOf( t ) < messages.IndexOf( classFinished ) ) );
            Assert.Equal( testsOfClass.Count, classFinished.TestsTotal );
            Assert.Equal( 0, classFinished.TestsFailed );
        }

        var collectionFinished = Assert.Single( messages.OfType<ITestCollectionFinished>() );
        Assert.All( testsFinished, t => Assert.True( messages.IndexOf( t ) < messages.IndexOf( collectionFinished ) ) );
        Assert.All( classesFinished, c => Assert.True( messages.IndexOf( c ) < messages.IndexOf( collectionFinished ) ) );
        Assert.Equal( 5, collectionFinished.TestsTotal );

        var assemblyFinished = Assert.IsAssignableFrom<ITestAssemblyFinished>( messages[^1] );
        Assert.Equal( 5, assemblyFinished.TestsTotal );
        Assert.Equal( 0, assemblyFinished.TestsFailed );
        Assert.Equal( 0, assemblyFinished.TestsNotRun );
    }

    /// <summary>
    /// Verifies that the tests that have not started when the message sink cancels the run are reported as not run, and
    /// that the run ends without an exception.
    /// </summary>
    [Fact]
    public void CancellationBySink_ReportsTheRemainingTestsAsNotRun()
    {
        using var testContext = this.CreateTestContext();

        var messages = RunTests(
            testContext,
            PassingTests( _fiveTestsInTwoClasses ),
            ParallelMode.None,
            shouldContinue: m => m is not ITestFinished );

        Assert.Single( messages.OfType<ITestPassed>() );
        AssertStartingAndFinishingMessagesAreBalanced( messages );

        var assemblyFinished = Assert.IsAssignableFrom<ITestAssemblyFinished>( messages[^1] );
        Assert.Equal( 5, assemblyFinished.TestsTotal );
        Assert.Equal( 4, assemblyFinished.TestsNotRun );
    }

    /// <summary>
    /// Verifies that, when the message sink cancels a parallel run, the executor waits for the tests that have started
    /// before it reports that the assembly has finished, and that it sends no message afterwards.
    /// </summary>
    [Fact]
    public void CancellationBySink_InParallel_WaitsForTheStartedTests()
    {
        using var testContext = this.CreateTestContext();

        var messages = RunTests(
            testContext,
            PassingTests( _fiveTestsInTwoClasses ),
            ParallelMode.All,
            shouldContinue: m => m is not ITestStarting );

        AssertStartingAndFinishingMessagesAreBalanced( messages );

        var assemblyFinished = Assert.IsAssignableFrom<ITestAssemblyFinished>( messages[^1] );
        Assert.Equal( 5, assemblyFinished.TestsTotal );
        Assert.Equal( 5 - messages.OfType<ITestStarting>().Count(), assemblyFinished.TestsNotRun );
    }

    /// <summary>
    /// Verifies that a run whose cancellation token is already signalled starts no test and reports every test as not run.
    /// </summary>
    [Fact]
    public void CancelledToken_ReportsEveryTestAsNotRun()
    {
        using var testContext = this.CreateTestContext();
        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        var messages = RunTests(
            testContext,
            PassingTests( _fiveTestsInTwoClasses ),
            ParallelMode.All,
            cancellationToken: cancellationTokenSource.Token );

        Assert.Equal( ["TestAssemblyStarting", "TestAssemblyFinished"], messages.SelectAsReadOnlyList( m => m.GetType().Name ) );

        var assemblyFinished = Assert.IsAssignableFrom<ITestAssemblyFinished>( messages[^1] );
        Assert.Equal( 5, assemblyFinished.TestsTotal );
        Assert.Equal( 5, assemblyFinished.TestsNotRun );
    }

    /// <summary>
    /// Asserts that every node that sent a starting message also sent a finishing message.
    /// </summary>
    private static void AssertStartingAndFinishingMessagesAreBalanced( IReadOnlyList<IMessageSinkMessage> messages )
    {
        Assert.Equal( messages.OfType<ITestStarting>().Count(), messages.OfType<ITestFinished>().Count() );
        Assert.Equal( messages.OfType<ITestCaseStarting>().Count(), messages.OfType<ITestCaseFinished>().Count() );
        Assert.Equal( messages.OfType<ITestMethodStarting>().Count(), messages.OfType<ITestMethodFinished>().Count() );
        Assert.Equal( messages.OfType<ITestClassStarting>().Count(), messages.OfType<ITestClassFinished>().Count() );
        Assert.Equal( messages.OfType<ITestCollectionStarting>().Count(), messages.OfType<ITestCollectionFinished>().Count() );
        Assert.Single( messages.OfType<ITestAssemblyFinished>() );
    }

    /// <summary>
    /// Creates the content of passing tests, keyed by the path of the test file without extension.
    /// </summary>
    private static Dictionary<string, (string Input, string ExpectedOutput)> PassingTests( IEnumerable<string> paths )
        => paths.ToDictionary( p => p, _ => (_passingTest, _passingTestOutput) );

    /// <summary>
    /// Writes test files to a test file system, discovers them and runs them with a <see cref="TestExecutor"/>.
    /// </summary>
    /// <param name="testContext">The test context.</param>
    /// <param name="tests">The input and the expected output of each test, keyed by the path of the test file relative to
    /// the test directory and without extension.</param>
    /// <param name="parallelMode">The parallelization mode of the run.</param>
    /// <param name="shouldContinue">The value returned by the message sink for each message.</param>
    /// <param name="cancellationToken">The token that cancels the run, or <c>null</c> to use the token of the context.</param>
    /// <returns>The messages sent by the executor, in the order in which they were sent.</returns>
    private static List<IMessageSinkMessage> RunTests(
        MetalamaTestContext testContext,
        IReadOnlyDictionary<string, (string Input, string ExpectedOutput)> tests,
        ParallelMode parallelMode = ParallelMode.All,
        Func<IMessageSinkMessage, bool>? shouldContinue = null,
        CancellationToken? cancellationToken = null )
    {
        var fileSystem = new TestFileSystem( testContext.ServiceProvider.Underlying );
        var directory = Path.Combine( Environment.CurrentDirectory, "tests" );
        fileSystem.CreateDirectory( directory );

        foreach ( var test in tests )
        {
            var path = Path.Combine( directory, test.Key );
            fileSystem.CreateDirectory( Path.GetDirectoryName( path )! );
            fileSystem.WriteAllText( path + ".cs", test.Value.Input );
            fileSystem.WriteAllText( path + ".t.cs", test.Value.ExpectedOutput );
        }

        var serviceProvider = (GlobalServiceProvider) testContext.ServiceProvider.Global.Underlying
            .WithUntypedService( typeof(IFileSystem), fileSystem )
            .WithService( new FakeMetadataReader( directory ) );

        var testProperties = new TestProjectProperties(
            assemblyName: null,
            directory,
            directory,
            ImmutableArray<string>.Empty,
            "net10.0",
            "net10.0",
            ImmutableArray<string>.Empty );

        // The metadata of the assembly is supplied by FakeMetadataReader, so any assembly can stand for the test assembly.
        var testFactory = new TestFactory(
            serviceProvider,
            testProperties,
            new TestDirectoryOptionsReader( serviceProvider, directory ),
            typeof(TestExecutorTests).Assembly );

        var messageSink = new TestMessageSink( shouldContinue );

        var executionOptions = new TestFrameworkExecutionOptions();
        executionOptions.SetValue( TestOptionsNames.Execution.ParallelMode, parallelMode.ToString() );

        var testExecutor = new TestExecutor( serviceProvider, testFactory );
        var testDiscoverer = new TestDiscoverer( testFactory );
        var discoveredTests = testDiscoverer.Discover( directory, ImmutableHashSet<string>.Empty );
        testExecutor.RunTests( discoveredTests, messageSink, executionOptions, cancellationToken ?? testContext.CancellationToken );

        return messageSink.Messages;
    }
}
