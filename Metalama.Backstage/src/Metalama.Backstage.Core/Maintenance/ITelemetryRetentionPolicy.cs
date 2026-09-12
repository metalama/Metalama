// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Extensibility;
using System;

namespace Metalama.Backstage.Maintenance;

/// <summary>
/// Gives the period after which the files of the telemetry directories are deleted. The telemetry package registers
/// the implementation, which reads the period from the telemetry configuration; when no implementation is
/// registered, <see cref="TempFileManager"/> uses a default period.
/// </summary>
internal interface ITelemetryRetentionPolicy : IBackstageService
{
    /// <summary>
    /// Gets the retention period. It is read every time the directories are swept, so that a change of the
    /// configuration takes effect on the next sweep.
    /// </summary>
    TimeSpan RetentionPeriod { get; }
}
