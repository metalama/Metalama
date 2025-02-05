// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;

namespace Metalama.Extensions.Multicast;

[RunTimeOrCompileTime]
internal static class EnumExtensions
{
    public static bool HasFlagFast( this MulticastAttributes attributes, MulticastAttributes flag ) => (attributes & flag) == flag;

    public static bool HasFlagFast( this MulticastTargets targets, MulticastTargets flag ) => (targets & flag) == flag;

    public static bool HasAnyFlag( this MulticastTargets targets, MulticastTargets flag ) => (targets & flag) != 0;
}