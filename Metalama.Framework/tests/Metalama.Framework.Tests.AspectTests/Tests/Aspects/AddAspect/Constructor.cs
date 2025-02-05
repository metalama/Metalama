using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.AddAspect.Constructor
{
    internal class Aspect : OverrideMethodAspect
    {
        private string _value;

        public Aspect( string value )
        {
            _value = value;
        }

        public override dynamic? OverrideMethod()
        {
            Console.WriteLine( _value );

            return meta.Proceed();
        }
    }

    internal class TargetCode
    {
        // <target>
        [Aspect( "The Value" )]
        private int Method( int a )
        {
            return a;
        }
    }
}