// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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