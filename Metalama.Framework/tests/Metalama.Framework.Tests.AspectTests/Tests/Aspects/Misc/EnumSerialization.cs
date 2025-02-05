using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Aspects.Misc.EnumSerialization
{
    internal class LogAttribute : OverrideMethodAspect
    {
        // Template that overrides the methods to which the aspect is applied.
        public override dynamic? OverrideMethod()
        {
            var color = meta.CompileTime( ConsoleColor.Blue );

            Console.ForegroundColor = color;

            return meta.Proceed();
        }
    }

// <target>
    internal class TargetCode
    {
        [LogAttribute]
        private int Method( int a )
        {
            return a;
        }
    }
}