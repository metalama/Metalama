// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Text;
using static System.Math;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Templating.LocalVariables.NameClashWithTargetSymbols
{
    [CompileTime]
    internal class Aspect
    {
        [TestTemplate]
        private dynamic? Template()
        {
            var PI = 3.14d;
            Console.WriteLine( PI );
            var r = 42;
            Console.WriteLine( r );
            var area = r * r;
            Console.WriteLine( area );
            var StringBuilder = new object();
            Console.WriteLine( StringBuilder.ToString() );

            return meta.Proceed();
        }
    }

    internal class TargetCode
    {
        private double Method( double r )
        {
            StringBuilder stringBuilder = new();
            var area = PI * r * r;

            return area;
        }
    }
}