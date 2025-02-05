#if TEST_OPTIONS
// @RequiredConstant(NET5_0_OR_GREATER) - Array slices are not supported in .NET Framework
#endif

#if NET5_0_OR_GREATER
#pragma warning disable CS8600, CS8603
using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects; 
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Templating.CSharpSyntax.OperatorsCompileTime
{
    [CompileTime]
    internal class Aspect
    {
        [TestTemplate]
        private dynamic Template()
        {
            var i = meta.Target.Parameters.Count;

            i = +-i;
            i = unchecked(i + 1);
            i = checked(i - 1);

            unchecked
            {
                i++;
            }

            checked
            {
                i--;
            }

            ++i;
            --i;
            i += 1;
            i -= 1;
            i *= 1;
            i /= 1;
            i %= 3;
            i = i * 1;
            i = i / 1;
            i = i % 3;
            i ^= 1;
            i &= 2;
            i |= 2;
            i = i ^ 1;
            i = i & 2;
            i = i | 2;
            i <<= 1;
            i >>= 1;
            i = i << 1;
            i = i >> 1;
            i = ~( ~i );

            var x = i switch
            {
                1 => true,
                _ => false
            };

            var y = ( i >= 2 );

            var t = ( x, y );
            ( x, y ) = t;

            bool? z = ( ( x ^ y ) && y ) || !x;

            string s = default;
            s ??= "42";

            s = s[0..2];

            Console.WriteLine( i );
            Console.WriteLine( t );
            Console.WriteLine( z.Value );
            Console.WriteLine( s );
            Console.WriteLine( sizeof(bool) );
            Console.WriteLine( typeof(int) );

            dynamic result = meta.Proceed();

            return result;
        }
    }

    internal class TargetCode
    {
        private object Method( int a, int b )
        {
            return a + b;
        }
    }
}
#endif