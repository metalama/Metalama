using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.IntegrationTests.Aspects.Introductions.Field.CopyAttributes
{
    public class IntroductionAttribute : TypeAspect
    {
        [Introduce]
        [Foo]
        public int IntroducedField;

        [Introduce]
        [Foo]
        public int IntroducedField_Initializer = 42;

        [Introduce]
        [Foo]
        public static int IntroducedField_Static;

        [Introduce]
        [Foo]
        public static int IntroducedField_Static_Initializer = 42;
    }

    public class FooAttribute : Attribute { }

    // <target>
    [Introduction]
    internal class TargetClass { }
}