#if TEST_OPTIONS
// @TestScenario(DesignTime)
#endif

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.IntegrationTests.Aspects.DesignTime.UnsupportedCacheDependency
{
    public class MyAspect : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            foreach (var t in builder.Target.ContainingNamespace.Types) { }
        }
    }

    // <target>
    internal class Target
    {
        [MyAspect]
        private partial class Nested { }
    }
}