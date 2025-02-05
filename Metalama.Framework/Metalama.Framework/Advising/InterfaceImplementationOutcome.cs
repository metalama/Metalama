// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;

namespace Metalama.Framework.Advising
{
    /// <summary>
    /// Actions taken by the advice when implementing an interface.
    /// </summary>
    [CompileTime]
    public enum InterfaceImplementationOutcome
    {
        /// <summary>
        /// The interface was implemented. Individual members of this interface will appear in <see cref="IImplementInterfaceAdviceResult.InterfaceMembers"/> collection.
        /// </summary>
        Implement = 0,

        /// <summary>
        /// The interface type was ignored. Members will not appear in <see cref="IImplementInterfaceAdviceResult.InterfaceMembers"/> collection.
        /// </summary>
        Ignore = 1
    }
}