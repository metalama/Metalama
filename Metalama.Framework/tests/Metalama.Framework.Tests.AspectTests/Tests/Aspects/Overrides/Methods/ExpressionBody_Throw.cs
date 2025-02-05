using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

#pragma warning disable CS8618

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Overrides.Methods.ExpressionBody_Throw
{
    public class OverrideAttribute : OverrideMethodAspect
    {
        public override dynamic? OverrideMethod()
        {
            Console.WriteLine( "This is the overriding method." );

            return meta.Proceed();
        }
    }

    // <target>
    internal partial class Target
    {
        [Override]
        public void M1( string m ) => throw new Exception();

        [Override]
        public int M2( string m ) => throw new Exception();
    }
}