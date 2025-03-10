// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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