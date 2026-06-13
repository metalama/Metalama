// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Backstage.Telemetry;

/// <summary>
/// Internal extension of <see cref="ITelemetryConfigurationService"/> that exposes the first-party-only
/// diagnostic salt. This member is kept off the public <see cref="ITelemetryConfigurationService"/> interface
/// so that the public API surface is not affected (see issue #1668).
/// </summary>
internal interface IInternalTelemetryConfigurationService : ITelemetryConfigurationService
{
    /// <summary>
    /// Gets the salt used to hash identifiers that are sent only to the first-party diagnostic store (bits),
    /// never to the third-party analytics platform (Matomo). Keying diagnostic identifiers with this salt
    /// instead of <see cref="ITelemetryConfigurationService.Salt"/> makes the Matomo pseudonym mathematically
    /// unjoinable to our diagnostic data. It is rotated together with <see cref="ITelemetryConfigurationService.Salt"/>
    /// and <see cref="ITelemetryConfigurationService.DeviceId"/>.
    /// </summary>
    long DiagnosticSalt { get; }
}
