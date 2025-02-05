using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.IntegrationTests.Aspects.AspectMemberRef.IntroducedPropertyRef
{
    public class RetryAttribute : TypeAspect
    {
        [Introduce]
        private void IntroducedMethod1( string name )
        {
            IntroducedProperty = name;
        }

        [Introduce]
        private string IntroducedProperty
        {
            get
            {
                return meta.Target.Property.DeclaringType.Name;
            }
            set { }
        }
    }

    // <target>
    [Retry]
    internal class Program { }
}