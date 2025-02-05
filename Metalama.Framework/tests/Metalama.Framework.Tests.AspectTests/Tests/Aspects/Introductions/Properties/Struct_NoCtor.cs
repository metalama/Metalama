using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.IntegrationTests.Aspects.Introductions.Properties.Struct_NoCtor
{
    public class IntroductionAttribute : TypeAspect
    {
        [Introduce]
        public int IntroducedProperty { get; set; }

        [Introduce]
        public int IntroducedProperty_Initializer { get; set; } = 42;

        [Introduce]
        public static int IntroducedProperty_Static { get; set; }

        [Introduce]
        public static int IntroducedProperty_Static_Initializer { get; set; } = 42;
    }

    // <target>
    [Introduction]
    internal struct TargetStruct
    {
        public int ExistingField;

        public int ExistingProperty { get; set; }
    }
}