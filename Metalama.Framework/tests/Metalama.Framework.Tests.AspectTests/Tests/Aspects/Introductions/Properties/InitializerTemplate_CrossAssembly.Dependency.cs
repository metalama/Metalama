using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.IntegrationTests.Aspects.Introductions.Properties.InitializerTemplate_CrossAssembly
{
    public class Introduction : TypeAspect
    {
        [Introduce]
        public string IntroducedProperty { get; set; } = meta.Target.Member.Name;

        [Introduce]
        public static string IntroducedProperty_Static { get; set; } = meta.Target.Member.Name;
    }
}