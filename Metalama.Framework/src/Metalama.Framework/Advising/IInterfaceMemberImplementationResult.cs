// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Advising
{
    /// <summary>
    /// Describes an interface member implemented by <see cref="IAdviceFactory.ImplementInterface(INamedType, INamedType, OverrideStrategy, object?)"/>.
    /// </summary>
    [CompileTime]
    public interface IInterfaceMemberImplementationResult
    {
        /// <summary>
        /// Gets an interface member that was implemented.
        /// </summary>
        IMember InterfaceMember { get; }

        /// <summary>
        /// Gets a value indicating the action taken to implement the interface member.
        /// </summary>
        InterfaceMemberImplementationOutcome Outcome { get; }

        /// <summary>
        /// Gets the member used to implement the interface. This may be either an existing member or a newly introduced member.
        /// </summary>
        IMember TargetMember { get; }
    }
}