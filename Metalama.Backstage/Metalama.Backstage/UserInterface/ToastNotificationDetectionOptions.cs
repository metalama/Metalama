// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Metalama.Backstage.UserInterface;

/// <summary>
/// Options for <see cref="IToastNotificationDetectionService"/>.
/// </summary>
public record ToastNotificationDetectionOptions
{
    /// <summary>
    /// Gets a value indicating whether the caller knows that a valid license is available.
    /// This typically happens when the license key is registered in source code. In this case, there should be no toast
    /// notification about a missing license key.
    /// </summary>
    public bool HasValidLicense { get; init; }
}