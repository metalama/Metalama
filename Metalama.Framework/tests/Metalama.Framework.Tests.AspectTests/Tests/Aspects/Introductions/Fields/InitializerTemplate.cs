using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.IntegrationTests.Aspects.Introductions.Fields.InitializerTemplate
{
    public class IntroductionAttribute : TypeAspect
    {
        [Introduce]
        public string IntroducedField = meta.Target.Member.Name;

        [Introduce]
        public static string IntroducedField_Static = meta.Target.Member.Name;
    }

    // <target>
    [Introduction]
    internal class TargetClass { }
}