// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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