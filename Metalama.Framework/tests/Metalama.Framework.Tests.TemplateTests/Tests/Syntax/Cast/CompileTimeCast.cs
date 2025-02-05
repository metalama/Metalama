using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Templating.Syntax.Cast.CompileTimeCast
{
    [CompileTime]
    internal class Aspect
    {
        [TestTemplate]
        private dynamic? Template()
        {
            var c = (short)meta.Target.Parameters.Count;

            if (c > 0)
            {
                var text = meta.CompileTime( "" );
                object s = meta.Target.Parameters[0].Name;

                if (s is string)
                {
                    text = ( s as string ) + " = ";
                }

                Console.WriteLine( text + meta.Target.Parameters[0].Value );
            }

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