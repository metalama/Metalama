using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;
using Metalama.Framework.Tests.AspectTests.TestInputs.Templating.NamespaceExpansion.PartiallyQualified.ChildNs;

namespace Metalama.Framework.Tests.AspectTests.TestInputs.Templating.NamespaceExpansion
{
    namespace PartiallyQualified
    {
        [CompileTime]
        internal class Aspect
        {
            [TestTemplate]
            private dynamic? Template()
            {
                var c = new ChildClass();

                return meta.Proceed();
            }
        }

        internal class TargetCode
        {
            private int Method( int a )
            {
                return a;
            }
        }

        namespace ChildNs
        {
            internal class ChildClass { }
        }
    }
}