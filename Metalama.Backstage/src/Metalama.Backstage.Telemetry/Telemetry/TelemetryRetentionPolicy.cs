// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Configuration;
using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Maintenance;
using System;

namespace Metalama.Backstage.Telemetry;

/// <summary>
/// The implementation of <see cref="ITelemetryRetentionPolicy"/> that reads the period from
/// <see cref="TelemetryConfiguration.RetentionPeriodInDays"/>.
/// </summary>
internal sealed class TelemetryRetentionPolicy : ITelemetryRetentionPolicy
{
    private readonly IConfigurationManager _configurationManager;

    public TelemetryRetentionPolicy( IServiceProvider serviceProvider )
    {
        this._configurationManager = serviceProvider.GetRequiredBackstageService<IConfigurationManager>();
    }

    /// <inheritdoc />
    public TimeSpan RetentionPeriod
    {
        get
        {
            var retentionInDays = this._configurationManager.Get<TelemetryConfiguration>().RetentionPeriodInDays
                                  ?? TelemetryConfiguration.DefaultRetentionPeriodInDays;

            return TimeSpan.FromDays( Math.Max( 0, retentionInDays ) );
        }
    }
}
