using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System.Threading;

#nullable enable

namespace Issue32772Library
{
    public class TestAspect : OverrideMethodAspect
    {
        public override void BuildAspect( IAspectBuilder<IMethod> builder )
        {
            base.BuildAspect( builder );

            // Wait for more than 2 seconds to make sure that the wildcarded assembly version is increased.
            SpinWait.SpinUntil( () => false, 5000 );
        }

        public override dynamic OverrideMethod()
        {
            return meta.Proceed();
        }
    }

    public class TargetClass
    {
        [TestAspect]
        public void Foo()
        {
        }
    }
}