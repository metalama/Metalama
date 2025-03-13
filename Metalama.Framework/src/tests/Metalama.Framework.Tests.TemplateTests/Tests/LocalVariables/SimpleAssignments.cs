// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Templating.LocalVariables.SimpleAssignments
{
    [CompileTime]
    internal class Aspect
    {
        [TestTemplate]
        private dynamic? Template()
        {
            var n = meta.Target.Parameters.Count; // build-time

            //var n = meta.RunTime(target.Method.Parameters.Count); // run-time
            var a0 = meta.Target.Parameters[0].Value; // run-time
            var x = 0;                                // run-time
            var y = meta.CompileTime( 0 );            // compile-time    

            Console.WriteLine( n );
            Console.WriteLine( a0 );
            Console.WriteLine( x );
            Console.WriteLine( y );

            return meta.Proceed();
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