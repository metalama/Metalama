// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#pragma warning disable CS0162

using System;
using System.IO;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Templating.Syntax.Using.RunTimeUsing
{
    internal class Aspect
    {
        [TestTemplate]
        private dynamic? Template()
        {
            using (new MemoryStream())
            {
                var x = meta.CompileTime( 0 );
                var y = meta.Target.Parameters[0].Value + x;

                return meta.Proceed();
            }

            using (var s = new MemoryStream())
            {
                Console.WriteLine( "" );
            }
        }
    }

    internal class TargetCode
    {
        private int Method( int a )
        {
            return a;
        }
    }
}