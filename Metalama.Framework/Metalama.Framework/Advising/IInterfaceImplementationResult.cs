// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Advising
{
    /// <summary>
    /// Describes an interface type implemented by <see cref="IAdviceFactory.ImplementInterface(INamedType, INamedType, OverrideStrategy, object?)"/>.
    /// </summary>
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
        
        IInterfaceImplementationAdviser ExplicitMembers { get; }
    }
}