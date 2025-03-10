// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Backstage.Extensibility;
using System;

namespace Metalama.Backstage.Licensing.Consumption
{
    /// <summary>
    /// Exposes a service to verify the current license and consume features from iut.
    /// </summary>
    [PublicAPI]
    public interface ILicenseConsumptionService : IBackstageService
    {
        /// <summary>
        /// Creates an <see cref="ILicenseConsumer"/>.
        /// </summary>
        ILicenseConsumer CreateConsumer( LicenseConsumptionOptions? options = null, Action<LicensingMessage>? reportMessage = null );

        /// <summary>
        /// Event raised when user-profile license keys have changed.
        /// </summary>
        event Action? Changed;
    }
}