using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Overrides.Properties.Record_Positional_Attribute
{
    internal class MyAspect : OverrideFieldOrPropertyAspect
    {
        public override dynamic? OverrideProperty
        {
            get
            {
                Console.WriteLine( "Sob" );

                return meta.Proceed();
            }
            set
            {
                meta.Proceed();
            }
        }
    }

    // <target>
    internal record MyRecord( int A, [property: MyAspect] int B );
}