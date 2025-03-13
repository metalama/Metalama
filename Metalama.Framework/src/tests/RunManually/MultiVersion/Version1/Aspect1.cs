// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

[Inherited]
public class Aspect1 : TypeAspect
{
    
    [Introduce( WhenExists = OverrideStrategy.New )]
    public static void TheMethod()
    {
        meta.Proceed();

        var version = meta.CompileTime( typeof(IAspectBuilder).Assembly.GetName() );
        Console.WriteLine($"Method {meta.Target.Method} introduced by Metalama version {version}");
    }
}