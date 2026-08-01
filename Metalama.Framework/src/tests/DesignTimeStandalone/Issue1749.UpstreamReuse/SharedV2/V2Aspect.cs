// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using System.Reflection;

// A different version from SharedV1, which is what makes the two AssemblyIdentity values differ while their
// ProjectKey values stay equal: a ProjectKey is an assembly name and a hash of the preprocessor symbols, with no
// version in it.
[assembly: AssemblyVersion( "2.0.0.0" )]

namespace SharedV2;

/// <summary>
/// An aspect of version 2.0 of the <c>Shared</c> assembly.
/// </summary>
public sealed class V2Aspect : OverrideMethodAspect
{
    /// <inheritdoc />
    public override dynamic? OverrideMethod()
    {
        System.Console.WriteLine( "V2" );

        return meta.Proceed();
    }
}
