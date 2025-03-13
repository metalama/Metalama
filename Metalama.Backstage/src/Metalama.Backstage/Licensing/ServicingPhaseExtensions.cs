// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Backstage.Licensing;

internal static class ServicingPhaseExtensions
{
    public static string GetDisplayName( this ServicingPhase servicingPhase )
        => servicingPhase switch
        {
            ServicingPhase.Default => "Mainstream",
            ServicingPhase.Extended => "Extended",
            ServicingPhase.LongTerm => "Long Term",
            _ => servicingPhase.ToString()
        };
}