// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug620;

public class TheAspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        // Test compile-time recursive pattern (property pattern) in is expression.
        var method = meta.Target.Method;

        // Simple property pattern
        var isSpecial = method is IMethod { Name: "M1" };

        // Nested property pattern (the exact pattern from the issue)
        var hasTargetDeclaringType = method is IMethod { DeclaringType: INamedType { Name: "Target" } };

        // Property pattern with type check
        var p = meta.Target.Parameters[0];
        var isIntParam = p is IParameter { Type: INamedType { SpecialType: SpecialType.Int32 } };

        Console.WriteLine( isSpecial );
        Console.WriteLine( hasTargetDeclaringType );
        Console.WriteLine( isIntParam );

        return meta.Proceed();
    }
}

// <target>
internal class Target
{
    [TheAspect]
    internal void M1( int x ) => throw new NotImplementedException();
}
