// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;

namespace Metalama.Framework.Advising
{
    /// <summary>
    /// Indicates the action taken by the advice when implementing an interface.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This outcome is available via <see cref="IInterfaceImplementationResult.Outcome"/> and indicates
    /// whether the interface was implemented or ignored (because it was already implemented on the target type).
    /// </para>
    /// </remarks>
    /// <seealso cref="IInterfaceImplementationResult"/>
    /// <seealso cref="IImplementInterfaceAdviceResult"/>
    /// <seealso cref="AdviserExtensions.ImplementInterface(IAdviser{Code.INamedType}, Code.INamedType, OverrideStrategy, object?)"/>
    /// <seealso href="@implementing-interfaces"/>
    [CompileTime]
    public enum InterfaceImplementationOutcome
    {
        /// <summary>
        /// The interface was implemented by the advice.
        /// </summary>
        Implement = 0,

        /// <summary>
        /// The interface was ignored because it was already implemented on the target type or a base type,
        /// and the <see cref="OverrideStrategy.Ignore"/> strategy was used.
        /// </summary>
        Ignore = 1
    }
}