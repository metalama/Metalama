// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Backstage.Extensibility;
using System;

namespace Metalama.Backstage.Telemetry;

/// <summary>
/// The addresses and keys that the telemetry services use to send reports. The host product supplies them, because
/// they belong to the product and not to the Backstage services.
/// </summary>
/// <param name="UploadUri">The address to which the encrypted telemetry packages are uploaded.</param>
/// <param name="GetUploadEncryptionPublicKey">A delegate that returns the RSA public key, in the <c>RSAKeyValue</c> XML format, that encrypts the symmetric key of a telemetry package. It is invoked when a package is uploaded, not when the options are created.</param>
[PublicAPI]
public sealed record TelemetryInitializationOptions( Uri UploadUri, Func<byte[]> GetUploadEncryptionPublicKey ) : IBackstageService
{
    /// <summary>
    /// Gets the address of the web analytics endpoint that receives the usage and license audit events, including
    /// the site identifier, or <c>null</c> when these events are not sent.
    /// </summary>
    public Uri? AnalyticsUri { get; init; }
}
