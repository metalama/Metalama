// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Tests.Templating.Syntax.Misc.CompileTimeThis
{
    class Aspect
    {
        [TestTemplate]
        dynamic Template()
        {

            Console.WriteLine(CompileTimeMethod( this ));
            
            return 0;
        }

        static string CompileTimeMethod( Aspect a ) => a.ToString()!;

    }

    class TargetCode
    {
        int Method(int a)
        {
            return a;
        }
    }
}