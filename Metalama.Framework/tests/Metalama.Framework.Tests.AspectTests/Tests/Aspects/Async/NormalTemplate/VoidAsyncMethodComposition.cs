// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @IgnoredDiagnostic(CS1998)
#endif

using System;
using System.Threading.Tasks;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Tests.AspectTests.Tests.Aspects.Async.NormalTemplate.VoidAsyncMethodComposition;

[assembly: AspectOrder( AspectOrderDirection.RunTime, typeof(Aspect1), typeof(Aspect2) )]

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Async.NormalTemplate.VoidAsyncMethodComposition
{
    internal class Aspect1 : OverrideMethodAspect
    {
        public override dynamic? OverrideMethod()
        {
            Console.WriteLine( "Aspect1.Before" );
            var result = meta.Proceed();
            Console.WriteLine( "Aspect1.After" );

            return result;
        }
    }

    internal class Aspect2 : OverrideMethodAspect
    {
        public override dynamic? OverrideMethod()
        {
            Console.WriteLine( "Aspect2.Before" );
            var result = meta.Proceed();
            Console.WriteLine( "Aspect2.After" );

            return result;
        }
    }

    // <target>
    internal class TargetCode
    {
        [Aspect1]
        [Aspect2]
        private async void MethodReturningValueTaskOfInt( int a )
        {
            await Task.Yield();
            Console.WriteLine( "Oops" );
        }
    }
}