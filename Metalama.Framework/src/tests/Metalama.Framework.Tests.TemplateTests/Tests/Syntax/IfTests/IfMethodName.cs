// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Templating.Syntax.IfTests.IfMethodName
{
    [CompileTime]
    internal class Aspect
    {
        [TestTemplate]
        private dynamic? Template()
        {
            var b = meta.CompileTime( 0 );

            if (meta.Target.Method.Name == "Method")
            {
                b = 1;
            }
            else
            {
                b = 2;
            }

            Console.WriteLine( b );

            return meta.Proceed();
        }
    }

    internal class TargetCode
    {
        private void Method() { }
    }
}