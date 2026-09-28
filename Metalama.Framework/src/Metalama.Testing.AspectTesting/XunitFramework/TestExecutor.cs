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
using System.Collections.Concurrent;
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
        private readonly TestFactory _factory;
        private static readonly object _launchingDebuggerLock = new();
        private readonly GlobalServiceProvider _serviceProvider;
        private readonly ITaskRunner _taskRunner;
        private readonly ITestAssemblyMetadataReader _metadataReader;

        public TestExecutor( GlobalServiceProvider serviceProvider, TestFactory factory )
        {
            this._factory = factory;
            this._serviceProvider = serviceProvider;
            this._taskRunner = this._serviceProvider.GetRequiredService<ITaskRunner>();
            this._metadataReader = this._serviceProvider.GetRequiredService<ITestAssemblyMetadataReader>();
        }

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

            var testCasesList = testCases.ToList();
            var hasLaunchedDebugger = false;
            var directoryOptionsReader = new TestDirectoryOptionsReader( this._serviceProvider, this._factory.ProjectProperties.SourceDirectory );

            var collections = testCasesList.GroupBy( t => t.TestCollection.UniqueID );

            var tasks = new ConcurrentDictionary<Task, Task>();

            // Increasing the concurrency seems detrimental to performance and to responsiveness of the test runner in case of cancellation.
            var semaphore = new SemaphoreSlim( Environment.ProcessorCount * 2 );
            var eventLock = new object();

            var assemblyMetrics = new Metrics( eventLock );

            var runSynchronously = executionOptions.ParallelModeOrDefault() == ParallelMode.None;

            SendMessage(
                TestMessages.AssemblyStarting(
                    this._factory.TestAssembly,
                    this._factory.Assembly.GetCustomAttribute<TargetFrameworkAttribute>()?.FrameworkName ) );

            try
            {
                foreach ( var collection in collections )
                {
                    if ( runCancellationToken.IsCancellationRequested )
                    {
                        return;
                    }

                    var testCollection = collection.First().TestCollection;
                    var collectionMetrics = new Metrics( assemblyMetrics );

                    collectionMetrics.Started += () => SendMessage( TestMessages.CollectionStarting( testCollection ) );
                    collectionMetrics.Finished += () => SendMessage( TestMessages.CollectionFinished( testCollection, collectionMetrics ) );

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
                        if ( runCancellationToken.IsCancellationRequested )
                        {
                            return;
                        }

                        var testClass = type.First().TestClass;
                        var typeMetrics = new Metrics( collectionMetrics );
                        typeMetrics.Started += () => SendMessage( TestMessages.ClassStarting( testClass ) );
                        typeMetrics.Finished += () => SendMessage( TestMessages.ClassFinished( testClass, typeMetrics ) );

                        typeMetrics.OnTestsDiscovered( type.Count() );

                        foreach ( var testCase in type )
                        {
                            if ( runCancellationToken.IsCancellationRequested )
                            {
                                return;
                            }

                            var testMetrics = new Metrics( typeMetrics );
                            var test = new Test( testCase );
                            var logger = new TestOutputHelper( executionMessageSink, test );

                            testMetrics.Started += () =>
                            {
                                SendMessage( TestMessages.MethodStarting( testCase.TestMethod ) );
                                SendMessage( TestMessages.CaseStarting( testCase ) );
                                SendMessage( TestMessages.TestStarting( test ) );
                            };

                            testMetrics.Finished += () =>
                            {
                                SendMessage( TestMessages.TestFinished( test, testMetrics.ExecutionTime, logger.Output ) );
                                SendMessage( TestMessages.CaseFinished( testCase, testMetrics ) );
                                SendMessage( TestMessages.MethodFinished( testCase.TestMethod, testMetrics ) );
                            };

                            testMetrics.OnTestsDiscovered( 1 );

                            if ( runSynchronously )
                            {
                                this._taskRunner.RunSynchronously(
                                    () => this.RunTestAsync(
                                        SendMessage,
                                        projectReferences,
                                        directoryOptionsReader,
                                        test,
                                        testMetrics,
                                        logger,
                                        runCancellationToken ),
                                    runCancellationToken );
                            }
                            else
                            {
                                var task = Task.Run(
                                    () => this.RunTestAsync(
                                        SendMessage,
                                        projectReferences,
                                        directoryOptionsReader,
                                        test,
                                        testMetrics,
                                        logger,
                                        runCancellationToken ),
                                    runCancellationToken );

                                // Throttle execution thanks to the semaphore.
                                semaphore.Wait( runCancellationToken );

                                if ( runCancellationToken.IsCancellationRequested )
                                {
                                    return;
                                }

                                // When the task is over, release the semaphore.
                                _ = task.ContinueWith( _ => semaphore.Release(), TaskScheduler.Current );

                                tasks.TryAdd( task, task );
                            }
                        }
                    }
                }

                // Wait for all tasks to complete and catch exceptions.
#pragma warning disable VSTHRD002
                Task.WhenAll( tasks.Keys ).Wait( runCancellationToken );
#pragma warning restore VSTHRD002
            }
            finally
            {
                SendMessage( TestMessages.AssemblyFinished( this._factory.TestAssembly, assemblyMetrics ) );
            }
        }

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
