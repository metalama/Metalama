// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using System.Linq;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Misc.CompileTimeElvis;

internal class Aspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        // This uses the null-conditional operator (?.) where the left side is compile-time
        // (meta.Target.Parameters.FirstOrDefault(...) returns IParameter?) and the right side crosses
        // into run-time via .Value (which is a compile-time-returning-run-time member).
        // This is not supported because when the compile-time expression is null, we don't know its type,
        // so we cannot generate type-preserving run-time code.
        var tokenParameters = meta.Target.Parameters.FirstOrDefault( p => p.Name == "token" )?.Value?.ToLower();

        return tokenParameters;
    }
}

// <target>
internal class TargetCode
{
    [Aspect]
    private string? MethodWithToken( string token )
    {
        return token;
    }
}
