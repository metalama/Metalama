// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Metalama.Backstage.Licensing;

public enum ServicingPhase : byte
{
    Default = 0,
    Extended = 1,
    LongTerm = 2
}

public static class ServicingPhaseExtensions
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