// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code.Collections;
using Metalama.Framework.Engine;
using Metalama.Framework.Engine.Extensibility;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities.Threading;
using Metalama.Testing.UnitTesting;
using SharpCrafters.Backstage.Diagnostics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Sdk;
using Xunit.v3;

namespace Metalama.Testing.AspectTesting.XunitFramework
{
    /// <summary>
    /// Runs the test cases of a test assembly and reports their progress and results to xunit.
    /// </summary>
    internal sealed class TestExecutor : ITestFrameworkExecutor
    {
        /// <summary>
        /// The factory of the test assembly.
        /// </summary>
        private readonly TestFactory _factory;
        /// <summary>
        /// The lock that prevents two test runs from launching the debugger at the same time.
        /// </summary>
        private static readonly object _launchingDebuggerLock = new();
        /// <summary>
        /// The global service provider of the test framework.
        /// </summary>
        private readonly GlobalServiceProvider _serviceProvider;
        /// <summary>
        /// The task runner that runs the tests when parallelization is disabled.
        /// </summary>
        private readonly ITaskRunner _taskRunner;
        /// <summary>
        /// The reader of the metadata of the test assembly.
        /// </summary>
        private readonly ITestAssemblyMetadataReader _metadataReader;

        /// <summary>
        /// Initializes a new instance of the <see cref="TestExecutor"/> class.
        /// </summary>
        public TestExecutor( GlobalServiceProvider serviceProvider, TestFactory factory )
        {
            this._factory = factory;
            this._serviceProvider = serviceProvider;
            this._taskRunner = this._serviceProvider.GetRequiredService<ITaskRunner>();
            this._metadataReader = this._serviceProvider.GetRequiredService<ITestAssemblyMetadataReader>();
        }

        /// <inheritdoc />
        public ValueTask RunTestCases(
            IReadOnlyCollection<ITestCase> testCases,
            IMessageSink executionMessageSink,
            ITestFrameworkExecutionOptions executionOptions,
            CancellationToken? cancellationToken = null )
        {
            this.RunTests( testCases.Cast<TestCase>(), executionMessageSink, executionOptions, cancellationToken ?? CancellationToken.None );

            return default;
        }

        /// <summary>
        /// Runs test cases and blocks until they are finished or cancelled.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Every test is reported to the <see cref="Metrics"/> of its node before any test starts, so that a class or a
        /// collection cannot finish while one of its tests has not been reported.
        /// </para>
        /// <para>
        /// When the run is cancelled, the tests that have not started are reported as not run, and the method waits for
        /// the tests that have started. These tests observe the cancellation token and end promptly. The method returns
        /// only when no test can send a message any more, because the linked cancellation token source is disposed on
        /// return, and because xunit does not expect a message after the assembly has finished.
        /// </para>
        /// </remarks>
        /// <param name="testCases">The test cases.</param>
        /// <param name="executionMessageSink">The sink that receives the messages. When it returns <c>false</c>, the run is cancelled.</param>
        /// <param name="executionOptions">The execution options of xunit.</param>
        /// <param name="cancellationToken">The token by which xunit cancels the run. It is linked to the
        /// <see cref="MetalamaTestContext.CancellationToken"/> of every test.</param>
        public void RunTests(
            IEnumerable<TestCase> testCases,
            IMessageSink executionMessageSink,
            ITestFrameworkExecutionOptions executionOptions,
            CancellationToken cancellationToken )
        {
            using var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource( cancellationToken );
            var runCancellationToken = cancellationTokenSource.Token;

            void SendMessage( IMessageSinkMessage message )
            {
                if ( !executionMessageSink.OnMessage( message ) )
                {
                    cancellationTokenSource.Cancel();
                }
            }

            var eventLock = new object();
            var assemblyMetrics = new Metrics( eventLock );
            var runSynchronously = executionOptions.ParallelModeOrDefault() == ParallelMode.None;

            // Increasing the concurrency seems detrimental to performance and to responsiveness of the test runner in case of cancellation.
            using var semaphore = new SemaphoreSlim( Environment.ProcessorCount * 2 );
            var tasks = new List<Task>();

            SendMessage(
                TestMessages.AssemblyStarting(
                    this._factory.TestAssembly,
                    this._factory.Assembly.GetCustomAttribute<TargetFrameworkAttribute>()?.FrameworkName ) );

            try
            {
                var plannedTests = this.PlanTests( testCases, executionMessageSink, assemblyMetrics, SendMessage );
                var directoryOptionsReader = new TestDirectoryOptionsReader( this._serviceProvider, this._factory.ProjectProperties.SourceDirectory );

                foreach ( var plannedTest in plannedTests )
                {
                    if ( runCancellationToken.IsCancellationRequested )
                    {
                        plannedTest.Metrics.OnTestNotRun();

                        continue;
                    }

                    Task RunTestAsync()
                        => this.RunTestAsync(
                            SendMessage,
                            plannedTest.ProjectReferences,
                            directoryOptionsReader,
                            plannedTest.Test,
                            plannedTest.Metrics,
                            plannedTest.Logger,
                            runCancellationToken );

                    if ( runSynchronously )
                    {
                        // RunTestAsync observes the cancellation token and reports its own exceptions, so the task runner
                        // receives no token and cannot throw.
                        this._taskRunner.RunSynchronously( RunTestAsync, CancellationToken.None );
                    }
                    else
                    {
                        // Throttle execution thanks to the semaphore.
                        try
                        {
                            semaphore.Wait( runCancellationToken );
                        }
                        catch ( OperationCanceledException )
                        {
                            plannedTest.Metrics.OnTestNotRun();

                            continue;
                        }

                        // No cancellation token is passed to Task.Run: a task that is cancelled before it starts would not
                        // release the semaphore and would not report the test.
                        tasks.Add(
                            Task.Run(
                                async () =>
                                {
                                    try
                                    {
                                        await RunTestAsync();
                                    }
                                    finally
                                    {
                                        semaphore.Release();
                                    }
                                } ) );
                    }
                }
            }
            finally
            {
                // RunTestAsync does not throw, and it ends promptly when the run is cancelled, so this wait needs no
                // cancellation token.
#pragma warning disable VSTHRD002
                Task.WhenAll( tasks ).Wait();
#pragma warning restore VSTHRD002

                SendMessage( TestMessages.AssemblyFinished( this._factory.TestAssembly, assemblyMetrics ) );
            }
        }

        /// <summary>
        /// Creates the objects that describe each test and its parents, subscribes to their events, and reports every test
        /// to the <see cref="Metrics"/> of its node.
        /// </summary>
        private List<PlannedTest> PlanTests(
            IEnumerable<TestCase> testCases,
            IMessageSink executionMessageSink,
            Metrics assemblyMetrics,
            Action<IMessageSinkMessage> sendMessage )
        {
            var plannedTests = new List<PlannedTest>();
            var hasLaunchedDebugger = false;

            foreach ( var collection in testCases.GroupBy( t => t.TestCollection.UniqueID ) )
            {
                var testCollection = collection.First().TestCollection;
                var collectionMetrics = new Metrics( assemblyMetrics );

                collectionMetrics.Started += () => sendMessage( TestMessages.CollectionStarting( testCollection ) );
                collectionMetrics.Finished += () => sendMessage( TestMessages.CollectionFinished( testCollection, collectionMetrics ) );

                var projectMetadata = this._metadataReader.GetMetadata( this._factory.Assembly );

                lock ( _launchingDebuggerLock )
                {
                    if ( projectMetadata.MustLaunchDebugger && !hasLaunchedDebugger )
                    {
                        Debugger.Launch();
                        hasLaunchedDebugger = true;
                    }
                }

                var projectReferences = projectMetadata.ToProjectReferences();

                foreach ( var type in collection.GroupBy( c => c.TestClass.UniqueID ) )
                {
                    var testClass = type.First().TestClass;
                    var typeMetrics = new Metrics( collectionMetrics );
                    typeMetrics.Started += () => sendMessage( TestMessages.ClassStarting( testClass ) );
                    typeMetrics.Finished += () => sendMessage( TestMessages.ClassFinished( testClass, typeMetrics ) );

                    foreach ( var testCase in type )
                    {
                        var testMetrics = new Metrics( typeMetrics );
                        var test = new Test( testCase );
                        var logger = new TestOutputHelper( executionMessageSink, test );

                        testMetrics.Started += () =>
                        {
                            sendMessage( TestMessages.MethodStarting( testCase.TestMethod ) );
                            sendMessage( TestMessages.CaseStarting( testCase ) );
                            sendMessage( TestMessages.TestStarting( test ) );
                        };

                        testMetrics.Finished += () =>
                        {
                            sendMessage( TestMessages.TestFinished( test, testMetrics.ExecutionTime, logger.Output ) );
                            sendMessage( TestMessages.CaseFinished( testCase, testMetrics ) );
                            sendMessage( TestMessages.MethodFinished( testCase.TestMethod, testMetrics ) );
                        };

                        testMetrics.OnTestsDiscovered( 1 );

                        plannedTests.Add( new PlannedTest( test, testMetrics, logger, projectReferences ) );
                    }
                }
            }

            return plannedTests;
        }

        /// <summary>
        /// A test that <see cref="RunTests"/> has prepared, with the objects that it needs to run it.
        /// </summary>
        private sealed record PlannedTest( Test Test, Metrics Metrics, TestOutputHelper Logger, TestProjectReferences ProjectReferences );

        /// <summary>
        /// Runs a test and reports its result to xunit and to its <see cref="Metrics"/>. The method does not throw.
        /// </summary>
        private async Task RunTestAsync(
            Action<IMessageSinkMessage> sendMessage,
            TestProjectReferences projectReferences,
            TestDirectoryOptionsReader directoryOptionsReader,
            Test test,
            Metrics testMetrics,
            TestOutputHelper logger,
            CancellationToken cancellationToken )
        {
            var testStopwatch = Stopwatch.StartNew();
            var testCase = test.TestCase;

            // Makes Xunit.TestContext.Current describe this test, so that the code of a test plug-in or of a runner
            // can read the test, its output helper and its cancellation token in the same way as in a test of xunit.
            TestContext.SetForTest( test, TestEngineStatus.Running, cancellationToken, TestResultState.ForNotRun(), logger, null );

            try
            {
                testMetrics.OnTestStarted();

                var testInput = this._factory.TestInputFactory.FromFile( this._factory.ProjectProperties, directoryOptionsReader, testCase.RelativePath );

                var testOptions =
                    new MetalamaTestContextOptions
                    {
                        AdditionalMetadataReferences = projectReferences.MetadataReferences,
                        ExtensionAssemblies = projectReferences.ExtensionReferences.SelectAsImmutableArray( r => r.Path.AssertNotNull() ),
                        CompileTimeAssemblies = [..projectReferences.CompileTimeAssemblyReferences.Select( x => x.Path ).WhereNotNull()],
                        TestPlugInTypes = projectReferences.PlugInTypes,
                        AllTargetFrameworks = this._factory.ProjectProperties.AllTargetFrameworks,
                        DurableRefKind = this._factory.ProjectProperties.DurableRefKind,

                        // The NoWarn of the test project reaches the compile-time compilation of the test, in the same
                        // way as the NoWarn of a user project does in a production build. See issue #1948.
                        IgnoredWarnings = this._factory.ProjectProperties.IgnoredWarnings
                    };

                testOptions = testInput.Options.ApplyToTestContextOptions( testOptions );

                if ( testInput.IsSkipped )
                {
                    sendMessage( TestMessages.TestSkipped( test, testInput.SkipReason ?? "", logger.Output ) );

                    // This raises the messages on parent nodes and need to be called last.
                    testMetrics.OnTestSkipped();
                }
                else
                {
                    var repeat = testInput.Options.Repeat ?? 1;

                    if ( repeat == 1 )
                    {
                        var firstSeed = testInput.Options.RandomSeed ?? new Random().Next();

                        var serviceProvider = this._serviceProvider.Underlying
                            .WithUntypedService( typeof(ILoggerFactory), new XunitLoggerFactory( logger, false ) )
                            .WithService( new RandomNumberProvider( firstSeed ), true )
                            .WithServiceConditional<IExtensionLoader>( sp => new TestExtensionLoader( sp, testOptions ) )
                            .WithDisjointSharedServices();

                        var testRunner = TestRunnerFactory.CreateTestRunner(
                            testInput,
                            serviceProvider,
                            projectReferences,
                            logger );

                        await testRunner.RunAndAssertAsync( testInput, testOptions, cancellationToken );
                    }
                    else
                    {
                        var firstSeed = testInput.Options.RandomSeed ?? 0;

                        for ( var i = 0; i < repeat; i++ )
                        {
                            if ( i > 0 )
                            {
                                logger.WriteLine( "-------------------------------------------------------------------------" );
                            }

                            var seed = firstSeed + i;
                            logger.WriteLine( $"Running rep #{i + 1} of {repeat}. Seed = {seed}." );

                            var serviceProvider = this._serviceProvider.Underlying
                                .WithUntypedService( typeof(ILoggerFactory), new XunitLoggerFactory( logger, false ) )
                                .WithService( new RandomNumberProvider( seed ) );

                            var testRunner = TestRunnerFactory.CreateTestRunner(
                                testInput,
                                serviceProvider,
                                projectReferences,
                                logger );

                            await testRunner.RunAndAssertAsync( testInput, testOptions, cancellationToken );
                        }
                    }

                    sendMessage( TestMessages.TestPassed( test, (decimal) testStopwatch.Elapsed.TotalSeconds, logger.Output ) );

                    // This raises the messages on parent nodes and need to be called last.
                    testMetrics.OnTestSucceeded( testStopwatch.Elapsed );
                }
            }
            catch ( Exception e )
            {
                var exception = e is AggregateException { InnerExceptions.Count: 1 } aggregateException ? aggregateException.InnerExceptions[0] : e;

                sendMessage( TestMessages.TestFailed( test, exception, (decimal) testStopwatch.Elapsed.TotalSeconds, logger.Output ) );

                // This will raise the events on parents, so it should be last.
                testMetrics.OnTestFailed( testStopwatch.Elapsed );
            }
        }
    }
}
