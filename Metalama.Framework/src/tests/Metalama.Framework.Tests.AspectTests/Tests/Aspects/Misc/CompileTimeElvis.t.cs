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
        var tokenParameters = meta.Target.Parameters.FirstOrDefault( p => p.Name == "token" )?.Value?.ToString();

        return tokenParameters;
    }
}

// <target>
internal class TargetCode
{
    // Method WITH a matching parameter - should resolve to token.ToString().
    [Aspect]
    private string? MethodWithToken( string token )
    {
        var tokenParameters = token?.ToString();
        return tokenParameters;
    }

    // Method WITHOUT a matching parameter - should resolve to (string?)null.
    [Aspect]
    private string? MethodWithoutToken( int x )
    {
        var tokenParameters = (string?)null;
        return tokenParameters;
    }
}
