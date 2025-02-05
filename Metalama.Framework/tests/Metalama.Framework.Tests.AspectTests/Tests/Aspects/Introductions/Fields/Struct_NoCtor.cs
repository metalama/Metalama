using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.IntegrationTests.Aspects.Introductions.Field.Struct_NoCtor
{
    public class IntroductionAttribute : TypeAspect
    {
        [Introduce]
        public int IntroducedField;

        [Introduce]
        public int IntroducedField_Initializer = 42;

        [Introduce]
        public static int IntroducedField_Static;

        [Introduce]
        public static int IntroducedField_Static_Initializer = 42;
    }

    // <target>
    [Introduction]
    internal struct TargetStruct
    {
        public int ExistingField;

        public int ExistingProperty { get; set; }
    }
}