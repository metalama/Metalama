// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if DEBUG // These tests are debug-only because TestableCancellationToken is testable in the DEBUG config only.

using Metalama.Backstage.Utilities;
using Metalama.Framework.DesignTime;
using Metalama.Framework.DesignTime.Pipeline;
using Metalama.Framework.DesignTime.Rpc;
using Metalama.Framework.DesignTime.SourceGeneration;
using Metalama.Framework.DesignTime.VisualStudio.Rpc;
using Metalama.Framework.DesignTime.VisualStudio.ServiceHub;
using Metalama.Framework.DesignTime.VisualStudio.ServiceProvider;
using Metalama.Framework.DesignTime.VisualStudio.SourceGenerating;
using Metalama.Framework.Engine;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.Pipeline.DesignTime;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities.Threading;
using Metalama.Framework.Tests.UnitTestHelpers.Mocks;
using Metalama.Framework.Tests.UnitTestHelpers.TestClasses;
using Metalama.Testing.UnitTesting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Framework.Tests.UnitTests.DesignTime.Pipeline;

#pragma warning disable VSTHRD200 // Use "Async" suffix for async methods

public sealed class PipelineCancellationTests : UnitTestClass
{
    private const int _maxCancellationPoints = 22;
    private const int _getConfigurationMaxCancellationPoints = 2;

    public PipelineCancellationTests( ITestOutputHelper logger ) : base( logger ) { }

    protected override void ConfigureServices( IAdditionalServiceCollection services )
    {
        base.ConfigureServices( services );

        services.AddUntypedGlobalService( typeof(IJsonSerializationBinderProvider), new JsonSerializationBinderProvider() );
    }

    [Theory]
    [MemberData( nameof(GetCancellationPoints) )]
    public async Task WithCancellation( int cancelOnCancellationPointIndex )
    {
        var wasCancellationRequested = await this.RunTestAsync( cancelOnCancellationPointIndex );
        Assert.True( wasCancellationRequested );
    }

    public static IEnumerable<object[]> GetCancellationPoints() => Enumerable.Range( 1, _maxCancellationPoints ).Select( i => new object[] { i } );

    [Fact]
    public async Task MaxCancellationPointsIsCorrect()
    {
        var isSmallEnough = await this.RunTestAsync( _maxCancellationPoints );
        var isLargeEnough = !await this.RunTestAsync( _maxCancellationPoints + 1 );

        if ( isSmallEnough && isLargeEnough )
        {
            // Successful.
            return;
        }

        var min = isSmallEnough ? _maxCancellationPoints : 1;
        var max = isLargeEnough ? _maxCancellationPoints : _maxCancellationPoints * 10;

        for ( var i = min; i < max; i++ )
        {
            if ( !await this.RunTestAsync( i ) )
            {
                this.TestOutput.WriteLine( $"The correct value for {nameof(_maxCancellationPoints)} is {i - 1}." );
#pragma warning disable xUnit2000
                Assert.Equal( _maxCancellationPoints, i - 1 );
#pragma warning restore xUnit2000
            }
        }

        // Not enough iterations.
        Assert.Fail( "Cancellation was not requested. The value of the 'max' variable may be too low." );
    }

    [Fact]
    public async Task WithoutCancellation()
    {
        var wasCancellationRequested = await this.RunTestAsync( int.MaxValue );
        Assert.False( wasCancellationRequested );
    }

    private async Task<bool> RunTestAsync( int cancelOnCancellationPointIndex ) // Return value: whether the test was cancelled.
    {
        // We need a large timeout because this test has many "yield" points, and other tests running concurrently will take precedence over it.
        // Therefore, a timeout equal than the expected duration of all tests is not exagerated.

        using var testContext = this.CreateTestContext( new TestContextOptions() { HasSourceGeneratorTouchFile = true, Timeout = TimeSpan.FromMinutes( 10 ) } );
        var serviceProvider = testContext.ServiceProvider.Global;
        serviceProvider = serviceProvider.WithService( new AnalysisProcessEventHub( serviceProvider ) );

        var projectKey = ProjectKeyFactory.CreateTest( "project" );

        // Start the hub service on both ends.
        var testGuid = Guid.NewGuid();
        var hubPipeName = $"Metalama_Hub_{testGuid}";
        var servicePipeName = $"Metalama_Analysis_{testGuid}";

        using var userProcessServiceHubEndpoint = new ServiceHubServerEndpoint( serviceProvider, hubPipeName );
        userProcessServiceHubEndpoint.Start();
        using var analysisProcessServiceHubEndpoint = new ServiceHubClientEndpoint( serviceProvider, hubPipeName );
        _ = analysisProcessServiceHubEndpoint.ConnectAsync( testContext.CancellationToken ); // Do not await so we get more randomness.

        // Start the main services on both ends.
        using var analysisProcessEndpoint = new RpcServiceProviderServerEndpoint(
            serviceProvider.WithService( new TestServiceHubClientEndpointProvider( analysisProcessServiceHubEndpoint ) ),
            servicePipeName,
            [new SourceGeneratorRpcServiceFactory()] );

        analysisProcessEndpoint.Start();

        var testCancellationTokenSourceFactory = new TestCancellationTokenSourceFactory( cancelOnCancellationPointIndex, testContext.CancellationToken );
        var cancellationTokenSource = testCancellationTokenSourceFactory.Create();

        var pipelineFactory = new TestDesignTimeAspectPipelineFactory( testContext );

        var analysisProcessProjectHandlerObserver = new ProjectSourceGeneratorObserver();

        var analysisProcessServiceProvider = serviceProvider.WithServices(
            new TestRpcServiceProviderServerEndpointProvider( analysisProcessEndpoint ),
            pipelineFactory,
            analysisProcessProjectHandlerObserver );

        using var analysisProcessProjectHandler =
            new VsAnalysisProcessProjectSourceGenerator( analysisProcessServiceProvider, testContext.ProjectOptions, projectKey );

        var userProcessProjectHandlerObserver = new ProjectSourceGeneratorObserver();
        var userProcessServiceProvider = serviceProvider.WithServices( userProcessServiceHubEndpoint, userProcessProjectHandlerObserver );

        using var userProcessProjectHandler = new VsUserProcessProjectSourceGenerator(
            userProcessServiceProvider,
            testContext.ProjectOptions,
            projectKey );

        // If the user process endpoint is not yet initialized, it might miss the event.
        await userProcessServiceHubEndpoint.WaitUntilInitializedAsync();

        bool wasCancellationRequested;

        try
        {
            // The first run of the pipeline is synchronous.
            wasCancellationRequested = ExecutePipeline( 1 );

            // The second run of the pipeline is asynchronous so it uses a different execution path.
            wasCancellationRequested |= ExecutePipeline( 2 );

            // For the third run should go the same path as the second run, but the second run may have been cancelled.
            // For the third run, we take cancellation into account.
            testCancellationTokenSourceFactory.StopCounting();
            wasCancellationRequested |= ExecutePipeline( 3 );
        }
        finally
        {
            // Awaiting here avoids cancellations in the middle of the remoting initialization.
            await analysisProcessEndpoint.WaitUntilInitializedAsync();
            await analysisProcessProjectHandler.PendingTasks.WaitAllAsync( testContext.CancellationToken );
            await userProcessProjectHandler.PendingTasks.WaitAllAsync( testContext.CancellationToken );
        }

        return wasCancellationRequested;

        string ReadTouchFile()
        {
            var touchFile = testContext.ProjectOptions.SourceGeneratorTouchFile.AssertNotNull();

            this.TestOutput.WriteLine( $"Reading the touch file '{touchFile}'." );

            return File.Exists( touchFile )
                ? RetryHelper.Retry( () => File.ReadAllText( touchFile ) )
                : "";
        }

        bool ExecutePipeline( int version )
        {
            // Touch files are updated during the initialization phase so it's good to clear the queues here.
            userProcessProjectHandlerObserver.Reset();
            analysisProcessProjectHandlerObserver.Reset();

            try
            {
                cancellationTokenSource.Token.ThrowIfCancellationRequested();

                this.TestOutput.WriteLine( $"----- {version}-th execution of ExecutePipeline ---- " );

                var touchFileBefore = ReadTouchFile();

                // The code is an aspect that produces some introduction that depends on the target code,
                // then we change the target code in each version. If we change the aspect in each version, we 
                // will have to cope with pausing and resuming the pipeline.
                const string aspectCode = @"
                using Metalama.Framework.Advising;
                using Metalama.Framework.Aspects; 
                using Metalama.Framework.Code;
                using Metalama.Framework.Diagnostics;
                using Metalama.Framework.Eligibility;

                class MyAspect : TypeAspect
                {
                    public override void BuildAspect( IAspectBuilder<INamedType> builder )
                    {
                        foreach ( var m in builder.Target.Methods )
                        {
                            builder.IntroduceField( ""__"" + m.Name, typeof(int) );
                        }
                    }

                }
                ";

                var targetCode = $"[MyAspect] partial class C {{ void M{version}(){{}} }}";

                var compilation = testContext.CreateCSharpCompilation(
                    new Dictionary<string, string>() { ["aspect.cs"] = aspectCode, ["target.cs"] = targetCode },
                    assemblyName: projectKey.AssemblyName );

                var analysisProcessGenerateSources =
                    (SyntaxTreeSourceGeneratorResult) analysisProcessProjectHandler.GenerateSources( compilation, cancellationTokenSource.Token );

                Assert.Single( analysisProcessGenerateSources.AdditionalSources );

                if ( version == 1 )
                {
                    // For the first version, the code is published synchronously.
                    Assert.Contains(
                        $"__M{version}",
                        analysisProcessGenerateSources.AdditionalSources.First().Value.GeneratedSyntaxTree.ToString(),
                        StringComparison.Ordinal );
                }

                var asynchronouslyPublishedSource = analysisProcessProjectHandlerObserver.PublishedSources.Take( cancellationTokenSource.Token );
                Assert.Single( asynchronouslyPublishedSource.Sources );
                Assert.Contains( $"__M{version}", asynchronouslyPublishedSource.Sources.First().Value, StringComparison.Ordinal );

                // Publishing is never synchronous.
                var userProcessGeneratedSources = userProcessProjectHandlerObserver.PublishedSources.Take( cancellationTokenSource.Token );

                Assert.Single( userProcessGeneratedSources.Sources );
                Assert.Contains( $"__M{version}", userProcessGeneratedSources.Sources.First().Value, StringComparison.Ordinal );

                // Verify the touch file. First wait until it has been written, because it must be written after the generated code has been published.
                // We are not reading the file from the filesystem because it seems there may be some race situation where we get the old
                // content even if the new one was written.
                this.TestOutput.WriteLine( "Waiting for the touch file to be touched." );

                try
                {
                    while ( analysisProcessProjectHandlerObserver.PublishedTouchFiles.Take( testContext.CancellationToken ).Content == touchFileBefore )
                    {
                        // Loop, but no more than 5 seconds.    
                    }
                }
                catch ( OperationCanceledException )
                {
                    Assert.Fail( "The source was not published in due time." );
                }

                return false;
            }
            catch ( OperationCanceledException )
            {
                return true;
            }
            catch ( AggregateException aggregateException ) when ( aggregateException.InnerExceptions is [OperationCanceledException] )
            {
                return true;
            }
        }
    }

    [Theory]
    [MemberData( nameof(GetGetConfigurationCancellationPoints) )]
    public async Task GetConfigurationWithCancellation( int cancelOnCancellationPointIndex )
    {
        Assert.True( await this.RunGetConfigurationTestAsync( cancelOnCancellationPointIndex ) );
    }

    public static IEnumerable<object[]> GetGetConfigurationCancellationPoints()
        => Enumerable.Range( 1, _getConfigurationMaxCancellationPoints ).Select( i => new object[] { i } );

    [Fact]
    public async Task GetConfigurationWithoutCancellation()
    {
        Assert.False( await this.RunGetConfigurationTestAsync( _getConfigurationMaxCancellationPoints + 1 ) );
    }

    // Return value: whether the test was cancelled.
    private async Task<bool> RunGetConfigurationTestAsync( int cancelOnCancellationPointIndex )
    {
        using var testContext = this.CreateTestContext();

        try
        {
            var code = new Dictionary<string, string> { ["Class1.cs"] = "public class Class1 { }" };

            var compilation = testContext.CreateCSharpCompilation( code );

            var testCancellationTokenSourceFactory = new TestCancellationTokenSourceFactory( cancelOnCancellationPointIndex, testContext.CancellationToken );
            var cancellationTokenSource = testCancellationTokenSourceFactory.Create();

            using var pipelineFactory = new TestDesignTimeAspectPipelineFactory( testContext );
            var pipeline = pipelineFactory.CreatePipeline( compilation );

            var configuration = await pipeline.GetConfigurationAsync(
                PartialCompilation.CreateComplete( compilation ),
                ignoreStatus: false,
                AsyncExecutionContext.Get(),
                cancellationTokenSource.Token );

            Assert.True( configuration.IsSuccessful );

            return false;
        }
        catch ( OperationCanceledException ex ) when ( ex.CancellationToken != testContext.CancellationToken )
        {
            return true;
        }
    }

    private sealed class TestCancellationTokenSource : TestableCancellationTokenSource
    {
        private readonly TestCancellationTokenSourceFactory _factory;

        public TestCancellationTokenSource( TestCancellationTokenSourceFactory factory, CancellationToken linkedToken )
            : base( linkedToken )
        {
            this._factory = factory;
        }

        public override void OnPossibleCancellationPoint()
        {
            if ( this._factory.OnPossibleCancellationPoint() )
            {
                this.CancellationTokenSource.Cancel();
            }
        }

#pragma warning disable CA2215
        public override void Dispose()
        {
            // Do not dispose because the instance is shared.
        }
#pragma warning restore CA2215
    }

#pragma warning disable CA1001
    private sealed class TestCancellationTokenSourceFactory : ITestableCancellationTokenSourceFactory
    {
        // We use a single source for testing because anyway we need to cancel all sources at the same time,
        // so it is equivalent to have one source. If we did not synchronize cancellations, we may cancel one source
        // but not the one we use for the blocking Take() method, and we would block forever.
        private readonly TestCancellationTokenSource _source;
        private readonly int _cancelOnCount;

        private int _count;
        private bool _isCounting = true;

        public TestCancellationTokenSourceFactory( int cancelOnCount, CancellationToken linkedToken )
        {
            this._cancelOnCount = cancelOnCount;
            this._source = new TestCancellationTokenSource( this, linkedToken );
        }

        public TestableCancellationTokenSource Create() => this._isCounting ? this._source : new TestableCancellationTokenSource();

        public bool OnPossibleCancellationPoint()
        {
            if ( this._isCounting )
            {
                if ( Interlocked.Increment( ref this._count ) == this._cancelOnCount )
                {
                    return true;
                }
            }

            return false;
        }

        public void StopCounting() => this._isCounting = false;
    }
#pragma warning restore CA1001
}

#endif