// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Templating.Syntax.IfTests.IfCompileTimeNested
{
    [CompileTime]
    internal class Aspect
    {
        [TestTemplate]
        private dynamic? Template()
        {
            var t = meta.CompileTime( 0 );
            var name = meta.Target.Parameters[0].Name;

            if (name.Contains( "a" ))
            {
                if (name.Contains( "b" ))
                {
                    t = 1;
                }
                else
                {
                    if (name.Contains( "c" ))
                    {
                        t = 42;
                    }
                    else
                    {
                        t = 3;
                    }
                }
            }
            else
            {
                t = 4;
            }

            Console.WriteLine( t );
            var result = meta.Proceed();

            return result;
        }
    }

    internal class TargetCode
    {
        private void Method( string aBc ) { }
    }
}