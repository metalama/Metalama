// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Licensing.Licenses;
using System;
using System.Collections.Generic;

namespace Metalama.Backstage.Licensing.Consumption.Sources
{
    /// <summary>
    /// Source of licenses for consumption.
    /// </summary>
    internal interface ILicenseSource
    {
        /// <summary>
        /// Gets a description of the license source.
        /// </summary>
        string Description { get; }

        LicenseSourceKind Kind { get; }

        /// <summary>
        /// Gets a license, if available and valid. <paramref name="reportMessage"/> is called when the license key is invalid.
        /// </summary>
        /// <param name="reportMessage">Action to be called when the license is invalid.</param>
        /// <returns>The license or <c>null</c>.</returns>
        IEnumerable<ILicense> GetLicenses( Action<LicensingMessage> reportMessage );

        /// <summary>
        /// Event raised when the current source has changed.
        /// </summary>
        event Action? Changed;

        LicenseSourcePriority Priority { get; }
    }

    public enum LicenseSourcePriority
    {
        Unattended,
        Explicit,
        UserProfile
    }
}