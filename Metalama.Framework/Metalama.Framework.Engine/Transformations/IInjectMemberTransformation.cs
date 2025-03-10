// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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