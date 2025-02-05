using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.IntegrationTests.Aspects.Overrides.Fields.Struct_Simple
{
    public class TestAttribute : OverrideFieldOrPropertyAspect
    {
        public override dynamic? OverrideProperty
        {
            get
            {
                Console.WriteLine( "This is aspect code." );

                return meta.Proceed();
            }
            set
            {
                Console.WriteLine( "This is aspect code." );
                meta.Proceed();
            }
        }
    }

    // <target>
    internal struct TargetStruct
    {
        [Test]
        public int Field;

        [Test]
        public static int StaticField;
    }
}