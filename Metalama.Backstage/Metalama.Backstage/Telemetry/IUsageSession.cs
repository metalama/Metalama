// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using System;

namespace Metalama.Backstage.Telemetry;

[PublicAPI]
public interface IUsageSession : IDisposable
{
    string Kind { get; }

    MetricCollection Metrics { get; }
}