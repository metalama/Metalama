// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using System;

namespace Metalama.Backstage.Licensing.Consumption;

[PublicAPI]
public interface ILicenseConsumer
{
    /// <summary>
    /// Attempts to consume a license. If it succeeds, marks the license for audit.
    /// </summary>
    /// <param name="requirement">A predicate indicating whether the license key can be consumed.</param>
    /// <param name="reportMessage"></param>
    /// <param name="showsToastNotification"></param>
    /// <returns>A value indicating if the <paramref name="requirement"/> is available.</returns>
    bool TryConsume( LicenseRequirement requirement, Action<LicensingMessage>? reportMessage = null, bool showsToastNotification = true );
}