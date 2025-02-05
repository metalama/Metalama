// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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