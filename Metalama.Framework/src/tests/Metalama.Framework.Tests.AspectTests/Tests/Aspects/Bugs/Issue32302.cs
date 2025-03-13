// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code.SyntaxBuilders;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Issue32302;

public class MyAspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        var stringBuilder = new InterpolatedStringBuilder();
        stringBuilder.AddExpression( meta.Target.Method.Name );
        Console.WriteLine( stringBuilder.ToExpression().Value );

        return meta.Proceed();
    }
}

// <target>
public class C
{
    [MyAspect]
    private void M() { }
}