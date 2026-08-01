// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using System.Reflection;

[assembly: AssemblyVersion( "1.0.0.0" )]

namespace SharedV1;

/// <summary>
/// An aspect of version 1.0 of the <c>Shared</c> assembly.
/// </summary>
/// <remarks>
/// Declared in its own namespace, so that the consumer can name both aspects without ambiguity. Only the assembly
/// identity has to collide, not the type names.
/// </remarks>
public sealed class V1Aspect : OverrideMethodAspect
{
    /// <inheritdoc />
    public override dynamic? OverrideMethod()
    {
        System.Console.WriteLine( "V1" );

        return meta.Proceed();
    }
}
