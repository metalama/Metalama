using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.IntegrationTests.Aspects.Introductions.Fields.InitializerMethod
{
    public class IntroductionAttribute : TypeAspect
    {
        [Introduce]
        public string IntroducedField = Foo();

        [Introduce]
        public static string IntroducedField_Static = Foo();

        [Introduce]
        private static string Foo()
        {
            return "foo";
        }
    }

    // <target>
    [Introduction]
    internal class TargetClass { }
}