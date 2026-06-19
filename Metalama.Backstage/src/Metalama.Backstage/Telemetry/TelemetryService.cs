// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Extensibility;
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
    private readonly IUsageReporter _usageReporter;
    private readonly IExceptionReporter _exceptionReporter;
    private readonly LocalExceptionReporter? _localExceptionReporter;

    public TelemetryService( IServiceProvider serviceProvider )
    {
        this._repositoryConfigurationService = serviceProvider.GetRequiredBackstageService<IRepositoryConfigurationService>();
        this._telemetryConfigurationService = serviceProvider.GetRequiredBackstageService<ITelemetryConfigurationService>();
        this._usageReporter = serviceProvider.GetRequiredBackstageService<IUsageReporter>();
        this._exceptionReporter = serviceProvider.GetRequiredBackstageService<IExceptionReporter>();
        this._localExceptionReporter = serviceProvider.GetBackstageService<LocalExceptionReporter>();

        this.NullContext = new TelemetryContext(
            isRepositoryTelemetryDisabled: true,
            ImmutableArray<TelemetryContextWarning>.Empty,
            this._telemetryConfigurationService,
            this._usageReporter,
            this._exceptionReporter,
            this._localExceptionReporter );
    }

    public ITelemetryContext NullContext { get; }

    public ITelemetryContext OpenContext( string directory )
    {
        var repositoryConfigurationResult = this._repositoryConfigurationService.GetConfiguration( directory );

        // The repository opts out when metalama.json explicitly sets telemetry.enabled = false. An explicit true (or an
        // absent setting) does not override the process-level / per-category gates: those are evaluated per scenario by
        // the context through ITelemetryConfigurationService.IsEnabled.
        var isOptedOut = repositoryConfigurationResult.Configuration.Telemetry?.Enabled == false;

        var warnings = repositoryConfigurationResult.Warnings.IsDefaultOrEmpty
            ? ImmutableArray<TelemetryContextWarning>.Empty
            : repositoryConfigurationResult.Warnings.Select( w => new TelemetryContextWarning( w.Message, w.FilePath ) ).ToImmutableArray();

        return new TelemetryContext(
            isRepositoryTelemetryDisabled: isOptedOut,
            warnings,
            this._telemetryConfigurationService,
            this._usageReporter,
            this._exceptionReporter,
            this._localExceptionReporter );
    }
}
