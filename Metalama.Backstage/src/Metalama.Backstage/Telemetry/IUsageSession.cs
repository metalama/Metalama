// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using System;

namespace Metalama.Backstage.Telemetry;

/// <summary>
/// Represents either a product use or a license use. The <see cref="Metrics"/> collection allows
/// to add key-value properties to the usage session. When the session is disposed through <see cref="IDisposable"/>,
/// the session data is uploaded.
/// </summary>
[PublicAPI]
public interface IUsageSession : IDisposable
{
    /// <summary>
    /// Determines whether clients are expected to populate the <see cref="Metrics"/> collection.
    /// When <c>true</c>, the detailed usage report is sent when <see cref="IDisposable.Dispose"/> is called.
    /// Otherwise, only the aggregate report is sent.
    /// </summary>
    bool ShouldCollectMetrics { get; }

    /// <summary>
    /// Gets a collection of key-value properties that enrich the usage session.
    /// </summary>
    MetricCollection Metrics { get; }
}