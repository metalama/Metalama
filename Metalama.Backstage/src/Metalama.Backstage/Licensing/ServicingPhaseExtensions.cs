// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Backstage.Licensing;

public static class ServicingPhaseExtensions
{
    public static string GetDisplayName( this ServicingPhase servicingPhase, bool listAllSupported = false )
        => (servicingPhase, listAllSupported) switch
        {
            (ServicingPhase.Current, false) => "Current",
            (ServicingPhase.Extended, false) => "Extended",
            (ServicingPhase.LongTerm, false) => "Long Term",
            (ServicingPhase.Current, true) => "Current",
            (ServicingPhase.Extended, true) => "Curent, Extended",
            (ServicingPhase.LongTerm, true) => "Current, Extented, Long Term",
            _ => servicingPhase.ToString()
        };
}