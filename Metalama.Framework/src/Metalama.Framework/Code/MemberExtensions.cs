// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Framework.Code;

/// <summary>
/// Extension methods for the <see cref="IMember"/> interface.
/// </summary>
public static class MemberExtensions
{
    /// <summary>
    /// Determines whether a member can be overridden, ie. whether it is <c>virtual</c>, <c>abstract</c>, or <c>override</c> but not <c>sealed</c>.
    /// </summary>
    public static bool IsOverridable( this IMember member ) => (member.IsVirtual || member.IsAbstract || member.IsOverride) && !member.IsSealed;
}