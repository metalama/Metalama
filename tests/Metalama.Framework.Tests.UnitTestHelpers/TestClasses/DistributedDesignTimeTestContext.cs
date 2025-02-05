// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.DesignTime.Extensibility;
using Metalama.Framework.DesignTime.Pipeline;
using Metalama.Framework.DesignTime.Services;
using Metalama.Framework.DesignTime.VisualStudio.ServiceHub;
using Metalama.Framework.DesignTime.VisualStudio.ServiceProvider;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Services;
using Metalama.Framework.Tests.UnitTestHelpers.Mocks;
using Metalama.Testing.UnitTesting;
using Microsoft.VisualStudio.Threading;
using System;
using System.Threading.Tasks;

namespace Metalama.Framework.Tests.UnitTestHelpers.TestClasses;

public sealed class DistributedDesignTimeTestContext : TestContext
{
    private readonly TaskCompletionSource<bool> _whenInitialized = new();
    private readonly TaskCompletionSource<bool> _whenFieldsInitialized = new();
    private ServiceHubServerEndpoint? _userProcessServiceHubEndpoint;
    private ServiceHubClientEndpoint? _analysisProcessServiceHubEndpoint;
    private RpcServiceProviderServerEndpoint? _analysisProcessEndpoint;
    private TestDesignTimeAspectPipelineFactory? _pipelineFactory;

    internal DistributedDesignTimeTestContext( TestContextOptions contextOptions, IAdditionalServiceCollection additionalServices ) : base(
        contextOptions with { RequiresExclusivity = true },
        additionalServices )
    {
        this.WorkspaceProvider = this.ServiceProvider.Global.GetRequiredService<TestWorkspaceProvider>();
    }

    internal async Task InitializeAsync(
        ServiceProviderBuilder<IGlobalService>? userProcessServices,
        ServiceProviderBuilder<IGlobalService>? analysisProcessServices )
    {
        try
        {
            var testGuid = Guid.NewGuid();
            var hubPipeName = $"Metalama_Hub_{testGuid}";
            var servicePipeName = $"Metalama_Analysis_{testGuid}";

            // Build the service provider of the analysis process.
            analysisProcessServices ??= new ServiceProviderBuilder<IGlobalService>();
            analysisProcessServices.Add( sp => new DesignTimeExtensionManager( sp, DesignTimeProcessKind.VsAnalysisProcess ) );
            analysisProcessServices.Add( sp => new AnalysisProcessEventHub( sp ) );
            analysisProcessServices.Add( sp => new TestDesignTimeAspectPipelineFactory( this, sp ) );
            analysisProcessServices.Add( sp => new ServiceHubClientEndpoint( sp, hubPipeName ) );
            analysisProcessServices.Add( sp => new RpcServiceProviderServerEndpoint( sp, servicePipeName ) );

            var analysisProcessServiceProvider = (GlobalServiceProvider) this.ServiceProvider.Global.Underlying.WithDisjointSharedServices();

            analysisProcessServiceProvider = analysisProcessServices.Build( analysisProcessServiceProvider );

            // Build the service provider of the background process.
            userProcessServices ??= new ServiceProviderBuilder<IGlobalService>();
            userProcessServices.Add( sp => new DesignTimeExtensionManager( sp, DesignTimeProcessKind.VsUserProcess ) );
            userProcessServices.Add( sp => new ServiceHubServerEndpoint( sp, hubPipeName ) );

            var userProcessServiceProvider = (GlobalServiceProvider) this.ServiceProvider.Global.Underlying.WithDisjointSharedServices();

            userProcessServiceProvider = userProcessServices.Build( userProcessServiceProvider );

            // Start the hub service on both ends.
            this._userProcessServiceHubEndpoint = userProcessServiceProvider.GetRequiredService<ServiceHubServerEndpoint>();
            this._userProcessServiceHubEndpoint.Start();
            this._analysisProcessServiceHubEndpoint = analysisProcessServiceProvider.GetRequiredService<ServiceHubClientEndpoint>();
            var connectAnalysisProcessTask = this._analysisProcessServiceHubEndpoint.ConnectAsync(); // Do not await so we get more randomness.

            // Start the main services in the analysis process. It should call the service hub in the user process and call the user process 
            // to create the client.
            this._pipelineFactory = analysisProcessServiceProvider.GetRequiredService<TestDesignTimeAspectPipelineFactory>();
            this._analysisProcessEndpoint = analysisProcessServiceProvider.GetRequiredService<RpcServiceProviderServerEndpoint>();
            this._analysisProcessEndpoint.Start();

            this.UserProcessServiceProvider = userProcessServiceProvider;

            this._whenFieldsInitialized.SetResult( true );

            await Task.WhenAll(
                this._userProcessServiceHubEndpoint.WaitUntilInitializedAsync( this.CancellationToken ).AsTask(),
                this._analysisProcessServiceHubEndpoint.WaitUntilInitializedAsync( this.CancellationToken ).AsTask(),
                connectAnalysisProcessTask );

            this._whenInitialized.SetResult( true );
        }
        catch ( Exception e )
        {
            this._whenInitialized.SetException( e );
            this._whenFieldsInitialized.TrySetException( e );

            throw;
        }
    }

    public Task WhenFullyInitialized => this._whenInitialized.Task.WithCancellation( this.CancellationToken );

    public Task WhenFieldsInitialized => this._whenFieldsInitialized.Task.WithCancellation( this.CancellationToken );

    public TestWorkspaceProvider WorkspaceProvider { get; }

    public GlobalServiceProvider UserProcessServiceProvider { get; private set; }

    public ServiceHubServerEndpoint ServiceHubServerEndpoint => this._userProcessServiceHubEndpoint ?? throw new InvalidOperationException();

    public RpcServiceProviderServerEndpoint RpcServiceProviderEndpoint => this._analysisProcessEndpoint ?? throw new InvalidOperationException();

    public TestDesignTimeAspectPipelineFactory PipelineFactory => this._pipelineFactory ?? throw new InvalidOperationException();

    ~DistributedDesignTimeTestContext()
    {
        this.Dispose( false );
    }

    protected override void Dispose( bool disposing )
    {
        if ( disposing )
        {
            GC.SuppressFinalize( this );
        }

        this._userProcessServiceHubEndpoint?.Dispose();
        this._analysisProcessServiceHubEndpoint?.Dispose();
        this._analysisProcessEndpoint?.Dispose();
        base.Dispose( disposing );
    }
}