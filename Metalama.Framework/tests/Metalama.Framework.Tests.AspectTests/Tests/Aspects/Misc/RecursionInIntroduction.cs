// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code.Invokers;

namespace Metalama.Framework.Tests.AspectTests.Aspects.Misc.RecursionInIntroduction
{
    internal class IntroductionAspect : TypeAspect
    {
        [Introduce]
        public int Ackermann1( int m, int n )
        {
            if (m == 0)
            {
                return n + 1;
            }
            else if (n == 0)
            {
                return Ackermann1( m - 1, 1 );
            }
            else
            {
                return Ackermann1( m - 1, Ackermann1( m, n - 1 ) );
            }
        }

        [Introduce]
        public int Ackermann2( int m, int n )
        {
            if (m == 0)
            {
                return n + 1;
            }
            else if (n == 0)
            {
                return meta.This.Ackermann2( m - 1, 1 );
            }
            else
            {
                return meta.This.Ackermann2( m - 1, meta.This.Ackermann2( m, n - 1 ) );
            }
        }

        [Introduce]
        public int Ackermann3( int m, int n )
        {
            if (m == 0)
            {
                return n + 1;
            }
            else if (n == 0)
            {
                return meta.Target.Method.With( InvokerOptions.Final ).Invoke( m - 1, 1 );
            }
            else
            {
                return meta.Target.Method.With( InvokerOptions.Final ).Invoke( m - 1, meta.Target.Method.With( InvokerOptions.Final ).Invoke( m, n - 1 ) );
            }
        }
    }

    // <target>
    [IntroductionAspect]
    internal class MyClass { }
}