// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Advising
{
    /// <summary>
    /// Describes an interface type implemented by advice, including the implementation outcome.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This result is available in the <see cref="IImplementInterfaceAdviceResult.Interfaces"/> collection and describes
    /// each interface (including base interfaces) that was considered when implementing an interface.
    /// </para>
    /// </remarks>
    /// <seealso cref="AdviserExtensions.ImplementInterface(IAdviser{INamedType}, INamedType, OverrideStrategy, object?)"/>
    /// <seealso cref="IInterfaceImplementationAdviser"/>
    /// <seealso cref="InterfaceImplementationOutcome"/>
    /// <seealso cref="IImplementInterfaceAdviceResult"/>
    /// <seealso href="@implementing-interfaces"/>
    [CompileTime]
    public interface IInterfaceImplementationResult
    {
        /// <summary>
        /// Gets an interface type that was considered by the advice.
        /// </summary>
        INamedType InterfaceType { get; }

        /// <summary>
        /// Gets a value indicating the action taken to implement the interface type.
        /// </summary>
        InterfaceImplementationOutcome Outcome { get; }

        /// <summary>
        /// Gets an <see cref="IAdviser{T}"/> allowing to introduce explicit members to the interface.
        /// </summary>
        IInterfaceImplementationAdviser ExplicitMembers { get; }
    }
}