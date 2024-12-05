// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.DesignTime.Pipeline;
using Metalama.Framework.Engine;
using Metalama.Framework.Engine.Options;
using Metalama.Framework.Engine.Pipeline;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities.Threading;
using Metalama.Framework.Project;
using Metalama.Testing.UnitTesting;
using Microsoft.CodeAnalysis;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace Metalama.Framework.Tests.UnitTests.DesignTime.Mocks;

internal sealed class TestDesignTimeAspectPipelineFactory : DesignTimeAspectPipelineFactory
{
    private readonly IProjectOptions _projectOptions;
    private static readonly TestMetalamaProjectClassifier _projectClassifier = new();

    public AnalysisProcessEventHub EventHub { get; }

    private static GlobalServiceProvider GetServiceProvider( TestContext testContext, GlobalServiceProvider? serviceProvider = null )
    {
        serviceProvider ??= testContext.ServiceProvider;

        var analysisProcessEventHub = serviceProvider.Value.GetService<AnalysisProcessEventHub>();

        if ( analysisProcessEventHub == null )
        {
            serviceProvider = serviceProvider.Value.WithService( new AnalysisProcessEventHub( serviceProvider.Value ) );
        }

        return serviceProvider.Value;
    }

    public TestDesignTimeAspectPipelineFactory( TestContext testContext, GlobalServiceProvider? serviceProvider = null ) :
        base(
            GetServiceProvider( testContext, serviceProvider ),
            testContext.Domain )
    {
        this._projectOptions = testContext.ProjectOptions;
        this.EventHub = this.ServiceProvider.GetRequiredService<AnalysisProcessEventHub>();
    }

    internal override ValueTask<FallibleResultWithDiagnostics<DesignTimeAspectPipeline>> GetOrCreatePipelineAsync(
        IProjectVersion projectVersion,
        TestableCancellationToken cancellationToken )
        => new( this.GetOrCreatePipeline( this._projectOptions, projectVersion.Compilation ) );

    internal override ValueTask<DesignTimeAspectPipeline?> GetPipelineAndWaitAsync( Compilation compilation, CancellationToken cancellationToken )
        => new( this.GetOrCreatePipeline( this._projectOptions, compilation ) );

    internal override bool TryGetMetalamaVersion( Compilation compilation, [NotNullWhen( true )] out Version? version )
        => _projectClassifier.TryGetMetalamaVersion( compilation, out version );

    public DesignTimeAspectPipeline CreatePipeline( Compilation compilation ) => this.GetOrCreatePipeline( this._projectOptions, compilation ).AssertNotNull();
}