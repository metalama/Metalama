// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Serialization;

namespace Metalama.Framework.Advising;

public interface IPullStrategy : ICompileTimeSerializable
{
    /// <summary>
    /// Gets the <seealso cref="PullAction"/> instructing how to assign a parameter to
    /// an introduced constructor parameter.
    /// </summary>
    /// <param name="pulledParameter">The parameter that has been introduced in the base constructor.</param>
    /// <param name="targetMember">The member into which <paramref name="pulledParameter"/> is being pulled.</param>
    /// <returns>A <seealso cref="PullAction"/>.</returns>
    PullAction GetPullAction( IParameter pulledParameter, IHasParameters targetMember );
}