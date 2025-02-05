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