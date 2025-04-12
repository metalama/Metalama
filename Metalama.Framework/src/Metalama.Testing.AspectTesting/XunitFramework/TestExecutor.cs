// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Diagnostics;
using Metalama.Framework.Code.Collections;
using Metalama.Framework.Engine;
using Metalama.Framework.Engine.Extensibility;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities.Threading;
using Metalama.Testing.UnitTesting;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Metalama.Testing.AspectTesting.XunitFramework
{
    internal sealed class TestExecutor : LongLivedMarshalByRefObject, ITestFrameworkExecutor
    {
        private readonly TestFactory _factory;
        private static readonly object _launchingDebuggerLock = new();
        private readonly GlobalServiceProvider _serviceProvider;
        private readonly ITaskRunner _taskRunner;
        private readonly ITestAssemblyMetadataReader _metadataReader;
        private readonly TestDiscoverer _discoverer;

        public TestExecutor( GlobalServiceProvider serviceProvider, AssemblyName assemblyName )
        {
            var assembly = Assembly.Load( assemblyName );
            var assemblyInfo = new ReflectionAssemblyInfo( assembly );
            this._discoverer = new TestDiscoverer( serviceProvider, assemblyInfo );
            var projectProperties = this._discoverer.GetTestProjectProperties();

            this._factory = new TestFactory(
                serviceProvider,
                projectProperties,
                new TestDirectoryOptionsReader( serviceProvider, projectProperties.SourceDirectory ),
                assemblyInfo );

            this._serviceProvider = serviceProvider;
            this._taskRunner = this._serviceProvider.GetRequiredService<ITaskRunner>();
            this._metadataReader = this._serviceProvider.GetRequiredService<ITestAssemblyMetadataReader>();
        }

        public TestExecutor( GlobalServiceProvider serviceProvider, TestFactory factory )
        {
            this._factory = factory;
            this._serviceProvider = serviceProvider;
            this._taskRunner = this._serviceProvider.GetRequiredService<ITaskRunner>();
            this._metadataReader = this._serviceProvider.GetRequiredService<ITestAssemblyMetadataReader>();
            this._discoverer = new TestDiscoverer( serviceProvider, factory.AssemblyInfo );
        }

        void IDisposable.Dispose() { }

        public ITestCase Deserialize( string value )
        {
            return new TestCase( this._factory, value );
        }

        void ITestFrameworkExecutor.RunAll(
            IMessageSink executionMessageSink,
            ITestFrameworkDiscoveryOptions discoveryOptions,
            ITestFrameworkExecutionOptions executionOptions )
        {
            var testCases = this._discoverer.Discover( "", ImmutableHashSet<string>.Empty );
            this.RunTests( testCases, executionMessageSink, executionOptions );
        }

        public void RunTests(
            IEnumerable<ITestCase> testCases,
            IMessageSink executionMessageSink,
            ITestFrameworkExecutionOptions executionOptions )
        {
            var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;

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

            var collections = testCasesList.GroupBy( t => t.TestMethod.TestClass.TestCollection );

            var tasks = new ConcurrentDictionary<Task, Task>();

            // Increasing the concurrency seems detrimental to performance and to responsiveness of the test runner in case of cancellation.
            var semaphore = new SemaphoreSlim( Environment.ProcessorCount * 2 );
            var eventLock = new object();

            var assemblyMetrics = new Metrics( eventLock );

            SendMessage(
                new TestAssemblyStarting(
                    testCasesList,
                    this._factory.TestAssembly,
                    DateTime.Now,
                    "CompileTime",
                    "CompileTime" ) );

            try
            {
                foreach ( var collection in collections )
                {
                    if ( cancellationToken.IsCancellationRequested )
                    {
                        return;
                    }

                    var collectionMetrics = new Metrics( assemblyMetrics );

                    collectionMetrics.Started += () => SendMessage( new TestCollectionStarting( collection, collection.Key ) );

                    collectionMetrics.Finished += () => SendMessage(
                        new TestCollectionFinished(
                            collection,
                            collection.Key,
                            collectionMetrics.ExecutionTime,
                            collectionMetrics.TestsRun,
                            collectionMetrics.TestFailed,
                            collectionMetrics.TestSkipped ) );

                    var projectMetadata = this._metadataReader.GetMetadata( collection.Key.TestAssembly.Assembly );

                    lock ( _launchingDebuggerLock )
                    {
                        if ( projectMetadata.MustLaunchDebugger && !hasLaunchedDebugger )
                        {
                            Debugger.Launch();
                            hasLaunchedDebugger = true;
                        }
                    }

                    var projectReferences = projectMetadata.ToProjectReferences();

                    foreach ( var type in collection.GroupBy( c => c.TestMethod.TestClass ) )
                    {
                        if ( cancellationToken.IsCancellationRequested )
                        {
                            return;
                        }

                        var typeMetrics = new Metrics( collectionMetrics );
                        typeMetrics.Started += () => SendMessage( new TestClassStarting( type, type.Key ) );

                        typeMetrics.Finished += () => SendMessage(
                            new TestClassFinished(
                                type,
                                type.Key,
                                typeMetrics.ExecutionTime,
                                typeMetrics.TestsRun,
                                typeMetrics.TestFailed,
                                typeMetrics.TestSkipped ) );

                        typeMetrics.OnTestsDiscovered( type.Count() );

                        foreach ( var testCase in type )
                        {
                            if ( cancellationToken.IsCancellationRequested )
                            {
                                return;
                            }

                            var testMetrics = new Metrics( typeMetrics );
                            var test = new Test( testCase );
                            var logger = new TestOutputHelper( executionMessageSink, test );

                            testMetrics.Started += () =>
                            {
                                SendMessage( new TestMethodStarting( [testCase], testCase.TestMethod ) );
                                SendMessage( new TestCaseStarting( testCase ) );
                                SendMessage( new TestStarting( test ) );
                            };

                            testMetrics.Finished += () =>
                            {
                                SendMessage( new TestFinished( test, testMetrics.ExecutionTime, logger.ToString() ) );

                                SendMessage(
                                    new TestCaseFinished(
                                        testCase,
                                        testMetrics.ExecutionTime,
                                        testMetrics.TestsRun,
                                        testMetrics.TestFailed,
                                        testMetrics.TestSkipped ) );

                                SendMessage(
                                    new TestMethodFinished(
                                        [testCase],
                                        testCase.TestMethod,
                                        testMetrics.ExecutionTime,
                                        testMetrics.TestsRun,
                                        testMetrics.TestFailed,
                                        testMetrics.TestSkipped ) );
                            };

                            testMetrics.OnTestsDiscovered( 1 );

                            if ( executionOptions.DisableParallelizationOrDefault() )
                            {
                                this._taskRunner.RunSynchronously(
                                    () => this.RunTestAsync(
                                        SendMessage,
                                        projectReferences,
                                        directoryOptionsReader,
                                        testCase,
                                        test,
                                        testMetrics,
                                        logger,
                                        cancellationToken ),
                                    cancellationToken );
                            }
                            else
                            {
                                var task = Task.Run(
                                    () => this.RunTestAsync(
                                        SendMessage,
                                        projectReferences,
                                        directoryOptionsReader,
                                        testCase,
                                        test,
                                        testMetrics,
                                        logger,
                                        cancellationToken ),
                                    cancellationToken );

                                // Throttle execution thanks to the semaphore.
                                semaphore.Wait( cancellationToken );

                                if ( cancellationToken.IsCancellationRequested )
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
                Task.WhenAll( tasks.Keys ).Wait( cancellationToken );
#pragma warning restore VSTHRD002
            }
            finally
            {
                SendMessage(
                    new TestAssemblyFinished(
                        testCasesList,
                        this._factory.TestAssembly,
                        assemblyMetrics.ExecutionTime,
                        assemblyMetrics.TestsRun,
                        assemblyMetrics.TestFailed,
                        assemblyMetrics.TestSkipped ) );
            }
        }

        private async Task RunTestAsync(
            Action<IMessageSinkMessage> sendMessage,
            TestProjectReferences projectReferences,
            TestDirectoryOptionsReader directoryOptionsReader,
            ITestCase testCase,
            Test test,
            Metrics testMetrics,
            ITestOutputHelper logger,
            CancellationToken cancellationToken )
        {
            var testStopwatch = Stopwatch.StartNew();

            try
            {
                testMetrics.OnTestStarted();

                var testInput = this._factory.TestInputFactory.FromFile( this._factory.ProjectProperties, directoryOptionsReader, testCase.UniqueID );

                var testOptions =
                    new TestContextOptions
                    {
                        AdditionalMetadataReferences = projectReferences.MetadataReferences,
                        ExtensionAssemblies = projectReferences.ExtensionReferences.SelectAsImmutableArray( r => r.Path.AssertNotNull() ),
                        CompileTimeAssemblies = [..projectReferences.CompileTimeAssemblyReferences.Select( x => x.Path ).WhereNotNull()],
                        TestPlugInTypes = projectReferences.PlugInTypes
                    };

                testOptions = testInput.Options.ApplyToTestContextOptions( testOptions );

                if ( testInput.IsSkipped )
                {
                    sendMessage( new TestSkipped( test, testInput.SkipReason ) );

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
                            .WithServiceConditional<IExtensionLoader>( _ => new TestExtensionLoader( testOptions ) )
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

                    sendMessage( new TestPassed( test, testMetrics.ExecutionTime, logger.ToString() ) );

                    // This raises the messages on parent nodes and need to be called last.
                    testMetrics.OnTestSucceeded( testStopwatch.Elapsed );
                }
            }
            catch ( Exception e )
            {
                IFailureInformation failureInformation;

                if ( e is AggregateException { InnerExceptions.Count: 1 } aggregateException )
                {
                    failureInformation = ExceptionUtility.ConvertExceptionToFailureInformation( aggregateException.InnerException );
                }
                else
                {
                    failureInformation = ExceptionUtility.ConvertExceptionToFailureInformation( e );
                }

                sendMessage(
                    new TestFailed(
                        test,
                        testMetrics.ExecutionTime,
                        logger.ToString(),
                        failureInformation.ExceptionTypes,
                        failureInformation.Messages,
                        failureInformation.StackTraces,
                        failureInformation.ExceptionParentIndices ) );

                // This will raise the events on parents, so it should be last.
                testMetrics.OnTestFailed( testStopwatch.Elapsed );
            }
        }
    }
}