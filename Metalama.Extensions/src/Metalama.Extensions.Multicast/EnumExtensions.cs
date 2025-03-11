// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;

namespace Metalama.Extensions.Multicast;

[RunTimeOrCompileTime]
internal static class EnumExtensions
{
    public static bool HasFlagFast( this MulticastAttributes attributes, MulticastAttributes flag ) => (attributes & flag) == flag;

    public static bool HasFlagFast( this MulticastTargets targets, MulticastTargets flag ) => (targets & flag) == flag;

    public static bool HasAnyFlag( this MulticastTargets targets, MulticastTargets flag ) => (targets & flag) != 0;
}