// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Metalama.Framework.Engine.Linking;

internal static class LinkerGeneratedFlagsExtensions
{
    public static bool HasFlagFast( this LinkerGeneratedFlags value, LinkerGeneratedFlags flags ) => (value & flags) == flags;
}