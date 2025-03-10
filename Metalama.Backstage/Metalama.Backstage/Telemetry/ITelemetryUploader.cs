// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Extensibility;
using System.Threading.Tasks;

namespace Metalama.Backstage.Telemetry;

public interface ITelemetryUploader : IBackstageService
{
    /// <summary>
    /// Starts the telemetry upload in a background process avoiding the current processed being blocked during the update. 
    /// </summary>
    /// <param name="force">Starts the upload even when it's been started recently.</param>
    /// <remarks>
    /// The upload is started once per day. If the upload has been started in the past 24 hours, this method has no effect,
    /// unless the <paramref name="force"/> parameter is set to <c>true</c>.
    /// </remarks>
    void StartUpload( bool force = false );

    /// <summary>
    /// Uploads the telemetry.
    /// </summary>
    /// <remarks>
    /// Use the <see cref="StartUpload"/> method to upload the telemetry without blocking the current process.
    /// </remarks>
    Task UploadAsync();
}