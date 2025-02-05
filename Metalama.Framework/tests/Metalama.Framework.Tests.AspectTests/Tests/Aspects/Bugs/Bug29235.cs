using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Aspects.Bugs.Bug29235
{
    internal class Aspect : OverrideFieldOrPropertyAspect
    {
        public override dynamic? OverrideProperty
        {
            get
            {
                Console.WriteLine( "Overridden getter." );

                return meta.Proceed();
            }

            set
            {
                Console.WriteLine( "Overridden setter." );
                meta.Proceed();
            }
        }
    }

    // <target>
    internal class TargetClass
    {
        [Aspect]
        public object Field;

        [Aspect]
        public object Property { get; }

        public TargetClass( object value )
        {
            Field = value;
            Property = value;
        }
    }
}