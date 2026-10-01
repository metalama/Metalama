// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.DesignTime;
using Metalama.Framework.DesignTime.Pipeline;
using Metalama.Framework.DesignTime.Rpc;
using Metalama.Framework.Engine;
using Metalama.Framework.Engine.Options;
using Metalama.Framework.Engine.Pipeline;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities.Threading;
using Metalama.Framework.Project;
using Metalama.Testing.UnitTesting;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace Metalama.Framework.Tests.UnitTestHelpers.Mocks;

#pragma warning disable LAMA0821 // Do not expose internal APIs.

public sealed class TestDesignTimeAspectPipelineFactory : DesignTimeAspectPipelineFactory
{
    private readonly IProjectOptions _projectOptions;
    private readonly ConcurrentDictionary<ProjectKey, IProjectOptions> _projectOptionsByProjectKey = new();
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
        base( GetServiceProvider( testContext, serviceProvider ) )
    {
        this._projectOptions = testContext.ProjectOptions;
        this.EventHub = this.ServiceProvider.GetRequiredService<AnalysisProcessEventHub>();
    }

    /// <summary>
    /// Assigns specific project options to a project, instead of the options of the <see cref="TestContext"/>.
    /// </summary>
    /// <remarks>
    /// All projects of a test share the options of the <see cref="TestContext"/> by default. A test assigns specific
    /// options when projects must differ, for instance to give each project its own build touch file. The options must be
    /// assigned before the pipeline of the project is created, and the same instance must be used for the whole test,
    /// because the factory recreates a pipeline when it receives options that are not equal to those of the pipeline.
    /// </remarks>
    public void SetProjectOptions( ProjectKey projectKey, IProjectOptions projectOptions ) => this._projectOptionsByProjectKey[projectKey] = projectOptions;

    private IProjectOptions GetProjectOptions( Compilation compilation )
        => this._projectOptionsByProjectKey.TryGetValue( ProjectKeyFactory.FromCompilation( compilation ), out var projectOptions )
            ? projectOptions
            : this._projectOptions;

    internal override ValueTask<FallibleResultWithDiagnostics<DesignTimeAspectPipeline>> GetOrCreatePipelineAsync(
        IProjectVersion projectVersion,
        TestableCancellationToken cancellationToken )
        => new( this.GetOrCreatePipeline( this.GetProjectOptions( projectVersion.Compilation ), projectVersion.Compilation, cancellationToken ).AssertNotNull() );

    internal override ValueTask<DesignTimeAspectPipeline?> GetPipelineAndWaitAsync( Compilation compilation, CancellationToken cancellationToken )
        => new( this.GetOrCreatePipeline( this.GetProjectOptions( compilation ), compilation ) );

    internal override bool TryGetMetalamaVersion( Compilation compilation, [NotNullWhen( true )] out Version? version )
        => _projectClassifier.TryGetMetalamaVersion( compilation, out version );

    public DesignTimeAspectPipeline CreatePipeline( Compilation compilation )
        => this.GetOrCreatePipeline( this.GetProjectOptions( compilation ), compilation ).AssertNotNull();
}