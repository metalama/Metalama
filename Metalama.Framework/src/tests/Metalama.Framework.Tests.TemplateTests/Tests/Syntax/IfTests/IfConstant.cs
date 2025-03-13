// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;
using System;

namespace Metalama.Framework.Tests.AspectTests.Templating.Syntax.IfTests.IfConstant;

[CompileTime]
internal class Aspect
{
    [TestTemplate]
    private void Template()
    {
        if (true)
        {
            Console.WriteLine( "true" );
        }

        const bool c = true;

        if (c)
        {
            Console.WriteLine( "c" );
        }

        var b = true;

        if (b)
        {
            Console.WriteLine( "b" );
        }
    }
}

internal class TargetCode
{
    private void Method() { }
}