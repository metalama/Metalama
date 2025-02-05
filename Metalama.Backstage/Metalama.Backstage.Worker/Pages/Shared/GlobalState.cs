// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;

namespace Metalama.Backstage.Pages.Shared;

public static class GlobalState
{
    public static LicenseKind LicenseKind { get; set; }

    public static string? LicenseKey { get; set; }
}

[PublicAPI]
public enum LicenseKind
{    
    None,
    Trial,
    Free,
    Register,
    Skip
}