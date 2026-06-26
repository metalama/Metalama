// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Infrastructure;
using Metalama.Backstage.Repositories;
using System;
using System.Collections.Immutable;
using System.Linq;

namespace Metalama.Backstage.Telemetry;

/// <inheritdoc cref="ITelemetryService"/>
internal sealed class TelemetryService : ITelemetryService
{
    private readonly IRepositoryConfigurationService _repositoryConfigurationService;
    private readonly ITelemetryConfigurationService _telemetryConfigurationService;
    private readonly IEnvironmentVariableProvider _environmentVariableProvider;
    private readonly IUsageReporter _usageReporter;
    private readonly IExceptionCapturer _exceptionCapturer;
    private readonly LocalExceptionReporter? _localExceptionReporter;

    public TelemetryService( IServiceProvider serviceProvider )
    {
        this._repositoryConfigurationService = serviceProvider.GetRequiredBackstageService<IRepositoryConfigurationService>();
        this._telemetryConfigurationService = serviceProvider.GetRequiredBackstageService<ITelemetryConfigurationService>();
        this._environmentVariableProvider = serviceProvider.GetRequiredBackstageService<IEnvironmentVariableProvider>();
        this._usageReporter = serviceProvider.GetRequiredBackstageService<IUsageReporter>();
        this._exceptionCapturer = serviceProvider.GetRequiredBackstageService<IExceptionCapturer>();
        this._localExceptionReporter = serviceProvider.GetBackstageService<LocalExceptionReporter>();
    }

    public ITelemetryPolicy GetPolicy( string? directory )
    {
        if ( string.IsNullOrEmpty( directory ) )
        {
            // The directory is unknown, so there is no repository to consult: disable telemetry. This lets callers write
            // OpenContext( GetPolicy( directory ) ) without a null check. See #1701.
            return NullTelemetryPolicy.NoContext;
        }

        // directory is non-null here (guarded above), but string.IsNullOrEmpty is not annotated on all target frameworks.
        var repositoryConfigurationResult = this._repositoryConfigurationService.GetRepositoryConfiguration( directory! );

        // The repository opts out when metalama.json explicitly sets telemetry.enabled = false. An explicit true (or an
        // absent setting) does not override the process-level / per-category gates: those are evaluated per scenario by
        // the policy through ITelemetryConfigurationService.GetEffectiveReportingAction.
        var isDisabledByRepository = repositoryConfigurationResult.Configuration?.Telemetry?.Enabled == false;

        var warnings = repositoryConfigurationResult.Warnings.IsDefaultOrEmpty
            ? ImmutableArray<TelemetryContextWarning>.Empty
            : repositoryConfigurationResult.Warnings.Select( w => new TelemetryContextWarning( w.Message, w.FilePath ) ).ToImmutableArray();

        return new TelemetryPolicy(
            repositoryConfigurationResult.IsRepository,
            isDisabledByRepository ? TelemetryDisabledReason.RepositoryOptOut : TelemetryDisabledReason.None,
            this._telemetryConfigurationService,
            warnings );
    }

    public ITelemetryPolicy GetToolingPolicy()
    {
        // The tooling policy is for telemetry about the tooling itself (the CLI, the worker, the compiler outer
        // fallback), which has no explicit project context. We take the process working directory as the context: when
        // it is inside a git repository, we honor that repository's policy (including its metalama.json opt-out);
        // otherwise there is no repository to consult, so telemetry is disabled (the local crash report is still
        // written). See #1701.
        var workingDirectory = this._environmentVariableProvider.CurrentDirectory;

        return this._repositoryConfigurationService.GetRepositoryRoot( workingDirectory ) != null
            ? this.GetPolicy( workingDirectory )
            : NullTelemetryPolicy.NoContext;
    }

    public ITelemetryContext OpenContext( ITelemetryPolicy policy )
        => new TelemetryContext( policy, this._usageReporter, this._exceptionCapturer, this._localExceptionReporter );
}