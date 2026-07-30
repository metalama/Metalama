// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using SharedV1;
using SharedV2;

namespace Consumer;

/// <summary>
/// Applies one aspect from each version of <c>Shared</c>, so that the design-time pipeline of this project has to
/// resolve both references and therefore builds a compile-time project closure over both.
/// </summary>
/// <remarks>
/// The two aspects live in different namespaces, so naming both is unambiguous and no <c>CS0433</c> arises. The
/// collision this scenario targets is between the two assembly identities, not between type names.
/// </remarks>
public class Target
{
    /// <summary>
    /// A method overridden by the aspect of version 1.0.
    /// </summary>
    [V1Aspect]
    public void M1() { }

    /// <summary>
    /// A method overridden by the aspect of version 2.0.
    /// </summary>
    [V2Aspect]
    public void M2() { }
}
