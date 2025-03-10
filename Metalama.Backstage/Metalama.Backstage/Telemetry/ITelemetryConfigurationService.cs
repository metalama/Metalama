// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Backstage.Extensibility;
using System;

namespace Metalama.Backstage.Telemetry;

[PublicAPI]
public interface ITelemetryConfigurationService : IBackstageService
{
    void SetStatus( bool? enabled );

    Guid DeviceId { get; }

    bool IsEnabled { get; }

    void ResetDeviceId();
}