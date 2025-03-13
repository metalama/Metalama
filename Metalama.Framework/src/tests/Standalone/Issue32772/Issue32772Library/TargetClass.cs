// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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