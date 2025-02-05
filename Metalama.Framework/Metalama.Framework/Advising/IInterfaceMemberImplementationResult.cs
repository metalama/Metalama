// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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