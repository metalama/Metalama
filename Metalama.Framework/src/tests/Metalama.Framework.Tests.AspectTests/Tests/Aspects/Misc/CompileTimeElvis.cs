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
        // Compile-time expression (IParameter?) with null-conditional access to .Value (dynamic).
        // Uses ToLower() instead of ToString() to verify the null branch uses the correct type cast
        // (ToLower() is only available on string, not on object).
        var tokenParameters = meta.Target.Parameters.FirstOrDefault( p => p.Name == "token" )?.Value?.ToLower();

        return tokenParameters;
    }
}

// <target>
internal class TargetCode
{
    // Method WITH a matching parameter - should resolve to token?.ToLower().
    [Aspect]
    private string? MethodWithToken( string token )
    {
        return token;
    }

    // Method WITHOUT a matching parameter - should resolve to ((dynamic)null)?.ToLower() since
    // the compile-time expression is null. The chain is preserved to maintain the correct result type.
    [Aspect]
    private string? MethodWithoutToken( int x )
    {
        return x.ToString();
    }
}
