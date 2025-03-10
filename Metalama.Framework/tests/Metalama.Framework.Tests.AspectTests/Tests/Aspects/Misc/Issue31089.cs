// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code.SyntaxBuilders;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Misc.Issue31089;

public class MyAspect : TypeAspect
{
    [Introduce]
    public void Method()
    {
        var stringBuilder = new InterpolatedStringBuilder();
        stringBuilder.AddText( "MachineName=" );
        stringBuilder.AddExpression( Environment.MachineName );

        Console.WriteLine( stringBuilder.ToValue() );
    }
}

[MyAspect]
internal class C { }