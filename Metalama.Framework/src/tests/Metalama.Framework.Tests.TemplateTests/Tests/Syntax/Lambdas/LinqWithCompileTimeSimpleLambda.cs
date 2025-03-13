// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using System.Collections.Generic;
using System.Linq;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Templating.Syntax.Lambdas.LinqWithCompileTimeSimpleLambda
{
    internal class Aspect
    {
        [TestTemplate]
        private dynamic? Template()
        {
            var list = new List<int>();
            list.Add( 1 );
            list.Add( 2 );
            list.Add( 5 );

            var p = list.Where( a => a > meta.Target.Parameters.Count ).Count();
            Console.WriteLine( p );

            return meta.Proceed();
        }
    }

    internal class TargetCode
    {
        private int Method( int a, int b )
        {
            return a + b;
        }
    }
}