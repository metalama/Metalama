// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System.Collections.Generic;

namespace Metalama.Framework.Engine.Transformations;

/// <summary>
/// Represents any transformation that injects a member, including introducing or overriding members, which work by introducing a new member.
/// </summary>
internal interface IInjectMemberTransformation : ISyntaxTreeTransformation
{
    /// <summary>
    /// Gets the full syntax of introduced members including the body.
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    IEnumerable<InjectedMember> GetInjectedMembers( MemberInjectionContext context );

    /// <summary>
    /// Gets the node after which the new members should be inserted.
    /// </summary>
    InsertPosition InsertPosition { get; }
}