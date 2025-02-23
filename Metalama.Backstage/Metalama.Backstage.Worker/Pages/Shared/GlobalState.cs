// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Licensing;

namespace Metalama.Backstage.Pages.Shared;

internal static class GlobalState
{
    public static SelectedAction SelectedAction { get; set; }

    public static string? LicenseKey { get; set; }

    public static CommunityLicenseReason CommunityLicenseReason { get; set; }
}