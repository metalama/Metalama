// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Licensing;

namespace Metalama.Backstage.Pages.Shared;

internal static class GlobalState
{
    public static SelectedAction SelectedAction { get; set; }

    public static string? LicenseKey { get; set; }

    public static CommunityLicenseReason CommunityLicenseReason { get; set; }
}