// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System.Threading;

namespace Issue32772SdkLibrary
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
