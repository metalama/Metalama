// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Backstage.Extensibility;
using System;

namespace Metalama.Backstage.Telemetry;

[PublicAPI]
public interface ITelemetryConfigurationService : IBackstageService
{
    void Initialize();

    void SetStatus( bool enabled );

    Guid DeviceId { get; }

    bool IsEnabled( TelemetryScenario scenario );

    void ResetDeviceId();

    long Salt { get; }

    /// <summary>
    /// Gets the salt used to hash identifiers that are sent only to the first-party diagnostic store (bits),
    /// never to the third-party analytics platform (Matomo). Keying diagnostic identifiers with this salt
    /// instead of <see cref="Salt"/> makes the Matomo pseudonym mathematically unjoinable to our diagnostic
    /// data. It is rotated together with <see cref="Salt"/> and <see cref="DeviceId"/>. See issue #1668.
    /// </summary>
    long InternalDiagnosticSalt { get; }
}

public enum TelemetryScenario
{
    None,
    Usage,
    Exception,
    Performance,
    Rss
}