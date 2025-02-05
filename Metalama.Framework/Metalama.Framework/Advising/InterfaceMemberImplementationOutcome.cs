// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;

namespace Metalama.Framework.Advising
{
    /// <summary>
    /// Actions taken by the advice when implementing an interface member.
    /// </summary>
    [CompileTime]
    public enum InterfaceMemberImplementationOutcome
    {
        /// <summary>
        /// Interface member was introduced as a new declaration.
        /// </summary>
        Introduce = 0,

        /// <summary>
        /// The interface member template was used to override an existing declaration.
        /// </summary>
        Override = 1,

        /// <summary>
        /// An existing class member was used for to implement the interface member.
        /// </summary>
        UseExisting = 2
    }
}