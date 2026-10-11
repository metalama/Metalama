// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using System.Linq;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Misc.CompileTimeElvis_ElementAccess;

internal class Aspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        // The member binding '.Value' after '?.' is wrapped in an element access. LAMA0289 must be reported.
        var tokenParameters = meta.Target.Parameters.FirstOrDefault( p => p.Name == "token" )?.Value[0];

        return tokenParameters;
    }
}

// <target>
internal class TargetCode
{
    [Aspect]
    private object? MethodWithToken( string token )
    {
        return token;
    }
}
