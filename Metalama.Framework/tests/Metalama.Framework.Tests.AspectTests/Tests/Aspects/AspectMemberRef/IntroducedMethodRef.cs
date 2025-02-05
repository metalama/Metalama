using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.IntegrationTests.Aspects.AspectMemberRef.IntroducedMethodRef
{
    public class RetryAttribute : TypeAspect
    {
        [Introduce]
        private void IntroducedMethod1( string name )
        {
            IntroducedMethod2( meta.Target.Method.Name );
        }

        [Introduce]
        private void IntroducedMethod2( string name )
        {
            IntroducedMethod1( meta.Target.Method.Name );
        }
    }

    // <target>
    [Retry]
    internal class Program { }
}