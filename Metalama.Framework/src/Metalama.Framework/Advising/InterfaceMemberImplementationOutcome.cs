// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;

namespace Metalama.Framework.Advising
{
    /// <summary>
    /// Indicates the action taken by the advice when implementing an individual interface member.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This outcome is available via <see cref="IInterfaceMemberImplementationResult.Outcome"/> and indicates
    /// how the interface member was resolved: introduced, overridden, or mapped to an existing member.
    /// </para>
    /// </remarks>
    /// <seealso cref="IInterfaceMemberImplementationResult"/>
    /// <seealso cref="IImplementInterfaceAdviceResult"/>
    /// <seealso cref="AdviserExtensions.ImplementInterface(IAdviser{Code.INamedType}, Code.INamedType, OverrideStrategy, object?)"/>
    /// <seealso href="@implementing-interfaces"/>
    [CompileTime]
    public enum InterfaceMemberImplementationOutcome
    {
        /// <summary>
        /// The interface member was introduced as a new declaration in the target type.
        /// </summary>
        Introduce = 0,

        /// <summary>
        /// An existing declaration was overridden using the interface member template.
        /// </summary>
        Override = 1,

        /// <summary>
        /// An existing compatible member in the target type was used to satisfy the interface member requirement.
        /// </summary>
        UseExisting = 2
    }
}