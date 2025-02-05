using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Properties.AccessorBasedNoGetter
{
    internal class MyAspect : TypeAspect
    {
        [Template]
        public void Setter( int value )
        {
            Console.WriteLine( "Introduced" );
        }

        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            builder.IntroduceProperty( "TheProperty", null, nameof(Setter) );
        }
    }

    // <target>
    [MyAspect]
    internal class C { }
}