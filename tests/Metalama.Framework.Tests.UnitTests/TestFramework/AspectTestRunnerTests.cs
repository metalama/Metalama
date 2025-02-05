// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

#if NET5_0_OR_GREATER
using Metalama.Backstage.Infrastructure;
using Metalama.Backstage.Testing;
using Metalama.Framework.Engine.Services;
using Metalama.Testing.AspectTesting;
using Metalama.Testing.UnitTesting;
using System;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using IMessageSink = Xunit.Abstractions.IMessageSink;

#pragma warning disable VSTHRD200 // Use "Async" suffix for async methods

namespace Metalama.Framework.Tests.UnitTests.TestFramework
{
    public sealed class AspectTestRunnerTests : UnitTestClass
    {
        private static readonly SemaphoreSlim _executionSemaphore = new( 1 );
        private static readonly AsyncLocal<(Task InsideTestTask, Task TestWaitTask)> _testLocal = new();

        [Fact]
        public async Task AwaitsMainMethod_Task()
        {
            var source = $@"
public class Program
{{
    static async System.Threading.Tasks.Task Main()
    {{
        var typeName = ""{this.GetType().AssemblyQualifiedName}"";
        var type = System.Type.GetType(typeName);
        var asyncLocalField = type.GetField( ""{nameof(_testLocal)}"", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static );
        var asyncLocal = (System.Threading.AsyncLocal<(System.Threading.Tasks.Task InsideTestTask, System.Threading.Tasks.Task TestWaitTask)>)asyncLocalField.GetValue(null);
        asyncLocal.Value.InsideTestTask.Start();
        await asyncLocal.Value.TestWaitTask;
    }}
}}
";

            await this.RunAwaitTest( source );
        }

        [Fact]
        public async Task AwaitsMainMethod_TaskInt()
        {
            var source = $@"
public class Program
{{
    static async System.Threading.Tasks.Task<int> Main()
    {{
        var typeName = ""{this.GetType().AssemblyQualifiedName}"";
        var type = System.Type.GetType(typeName);
        var asyncLocalField = type.GetField( ""{nameof(_testLocal)}"", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static );
        var asyncLocal = (System.Threading.AsyncLocal<(System.Threading.Tasks.Task InsideTestTask, System.Threading.Tasks.Task TestWaitTask)>)asyncLocalField.GetValue(null);
        asyncLocal.Value.InsideTestTask.Start();
        await asyncLocal.Value.TestWaitTask;
        return 0;
    }}
}}
";

            await this.RunAwaitTest( source );
        }

        [Fact]
        public async Task AwaitsMainMethod_NonAsyncTask()
        {
            var source = $@"
public class Program
{{
    static System.Threading.Tasks.Task Main() => InnerMain();

    static async System.Threading.Tasks.Task InnerMain()
    {{
        var typeName = ""{this.GetType().AssemblyQualifiedName}"";
        var type = System.Type.GetType(typeName);
        var asyncLocalField = type.GetField( ""{nameof(_testLocal)}"", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static );
        var asyncLocal = (System.Threading.AsyncLocal<(System.Threading.Tasks.Task InsideTestTask, System.Threading.Tasks.Task TestWaitTask)>)asyncLocalField.GetValue(null);
        asyncLocal.Value.InsideTestTask.Start();
        await asyncLocal.Value.TestWaitTask;
    }}
}}
";

            await this.RunAwaitTest( source );
        }

        [Fact]
        public async Task AwaitsMainMethod_NonAsyncTaskInt()
        {
            var source = $@"
public class Program
{{
    static System.Threading.Tasks.Task<int> Main() => InnerMain();

    static async System.Threading.Tasks.Task<int> InnerMain()
    {{
        var typeName = ""{this.GetType().AssemblyQualifiedName}"";
        var type = System.Type.GetType(typeName);
        var asyncLocalField = type.GetField( ""{nameof(_testLocal)}"", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static );
        var asyncLocal = (System.Threading.AsyncLocal<(System.Threading.Tasks.Task InsideTestTask, System.Threading.Tasks.Task TestWaitTask)>)asyncLocalField.GetValue(null);
        asyncLocal.Value.InsideTestTask.Start();
        await asyncLocal.Value.TestWaitTask;
        return 0;
    }}
}}
";

            await this.RunAwaitTest( source );
        }

        private async Task RunAwaitTest( string source )
        {
            using var testContext = this.CreateTestContext();
            var fileSystem = new TestFileSystem( testContext.ServiceProvider.Underlying );
            var directory = Path.Combine( Environment.CurrentDirectory, "tests" );

            fileSystem.CreateDirectory( directory );
            fileSystem.WriteAllText( Path.Combine( directory, "Test.cs" ), source );
            fileSystem.WriteAllText( Path.Combine( directory, "Test.t.cs" ), source );

            var serviceProvider = (GlobalServiceProvider) testContext.ServiceProvider.Global.Underlying
                .WithUntypedService( typeof(IFileSystem), fileSystem )
                .WithService( new FakeMetadataReader( directory ) );

            var messageSink = new TestMessageSink();
            var testOutputHelper = new OutputHelper( messageSink );
            var metadataReferences = testContext.GetMetadataReferences();

            var testProjectReferences = new TestProjectReferences(
                metadataReferences.ToImmutableArray(),
                ImmutableArray<TestAssemblyReference>.Empty,
                ImmutableArray<TestAssemblyReference>.Empty,
                ImmutableArray<string>.Empty, 
                null );

            var testProjectProperties =
                new TestProjectProperties(
                    assemblyName: null,
                    directory,
                    directory,
                    ImmutableArray<string>.Empty,
                    "net6.0",
                    ImmutableArray<string>.Empty );

            var testDirectoryOptionsReader = new TestDirectoryOptionsReader( serviceProvider, directory );

            var testRunner = new AspectTestRunner( serviceProvider, directory, testProjectReferences, testOutputHelper );
            var testInput = new TestInput.Factory( serviceProvider ).FromFile( testProjectProperties, testDirectoryOptionsReader, "Test.cs" );
            var testContextOptions = testInput.Options.ApplyToTestContextOptions( new TestContextOptions() );

            // A task that will be started by the test.
            var insideTestTask = new Task( () => { } );

            // A task that the test will wait for after starting the first task.
            var testWaitTask = new Task( () => { } );

            _testLocal.Value = (insideTestTask, testWaitTask);

            // Since AspectTestRunner uses a semaphore internally and this method is working with timeouts, this part should not be executed in parallel.
            await _executionSemaphore.WaitAsync();

            try
            {
                // Start the test.
                var runTestTask = testRunner.RunAndAssertAsync( testInput, testContextOptions );

                // Await for the test to get to the main method.
                if ( insideTestTask != await Task.WhenAny( runTestTask, insideTestTask, Task.Delay( 120000 ) ) )
                {
                    if ( runTestTask.IsFaulted )
                    {
                        throw new InvalidOperationException( $"Exception when running test: {runTestTask.Exception}", runTestTask.Exception );
                    }
                    else
                    {
                        throw new InvalidOperationException( "Waiting for test main execution timed out (this is a severe bug in this test)." );
                    }
                }

                // At this point the test is awaiting for testWaitTask.
                // Await the test and a small delay.
                if ( runTestTask == await Task.WhenAny( runTestTask, Task.Delay( 1000 ) ) )
                {
                    throw new InvalidOperationException( "AspectTestRunner returned even though the the task the test is waiting for was not started." );
                }

                testWaitTask.Start();

                await runTestTask;
            }
            finally
            {
                _executionSemaphore.Release();
            }
        }

        private sealed class OutputHelper : ITestOutputHelper
        {
            private readonly IMessageSink _messageSink;

            public OutputHelper( IMessageSink messageSink )
            {
                this._messageSink = messageSink;
            }

            public void WriteLine( string message )
            {
                this._messageSink.OnMessage( new Message( message ) );
            }

            public void WriteLine( string format, params object[] args )
            {
                this._messageSink.OnMessage( new Message( string.Format( CultureInfo.InvariantCulture, format, args ) ) );
            }
        }

        private sealed class Message( string text ) : LongLivedMarshalByRefObject, IMessageSinkMessage
        {
            public override string ToString() => text;
        }
    }
}
#endif