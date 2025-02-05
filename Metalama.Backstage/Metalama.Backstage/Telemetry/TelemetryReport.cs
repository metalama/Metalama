// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Metalama.Backstage.Telemetry;

internal abstract class TelemetryReport
{
    public abstract string Kind { get; }

    public MetricCollection Metrics { get; } = [];
}