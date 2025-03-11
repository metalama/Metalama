// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;
using System.Collections.Generic;

namespace Metalama.Framework.Advising;

/// <summary>
/// Represents the result of the <see cref="IAdviceFactory.ImplementInterface(INamedType,INamedType,OverrideStrategy,object?)"/>
/// method. The result can be used to introduce interface members using the extension methods in <see cref="AdviserExtensions"/>.
/// </summary>
[CompileTime]
public interface IImplementInterfaceAdviceResult : IAdviceResult
{
    /// <summary>
    /// Gets a list of interfaces that were considered when implementing the given interface.
    /// </summary>
    /// <remarks>
    /// This property contains an empty list if the advice was completely ignored.
    /// </remarks>
    IReadOnlyCollection<IInterfaceImplementationResult> Interfaces { get; }

    /// <summary>
    /// Gets a list of interface members specified using <see cref="InterfaceMemberAttribute"/> that were considered when implementing the given interface.
    /// </summary>
    /// <remarks>
    /// This property contains only members of interfaces that were implemented. Members of interfaces that were ignored are not included in the list.
    /// </remarks>
    [Obsolete( "This property is no longer supported because members may be resolved after the call to the ImplementInterface method." )]
    IReadOnlyCollection<IInterfaceMemberImplementationResult> InterfaceMembers { get; }

    /// <summary>
    /// Gets an <see cref="IAdviser{T}"/> allowing to introduce explicit members to the primary implemented interface.
    /// For introducing memebers to its base interfaces, use the <see cref="Interfaces"/> property.
    /// </summary>
    IInterfaceImplementationAdviser ExplicitMembers { get; }
}