// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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