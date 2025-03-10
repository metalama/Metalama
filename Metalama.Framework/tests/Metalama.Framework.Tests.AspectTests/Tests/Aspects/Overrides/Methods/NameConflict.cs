// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Methods.NameConflict;

[assembly: AspectOrder( AspectOrderDirection.RunTime, typeof(InnerOverrideAttribute), typeof(OuterOverrideAttribute) )]
#pragma warning disable CS0219

namespace Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Methods.NameConflict
{
    public class InnerOverrideAttribute : OverrideMethodAspect
    {
        public override dynamic? OverrideMethod()
        {
            var i = 27;

            return meta.Proceed();
        }
    }

    public class OuterOverrideAttribute : OverrideMethodAspect
    {
        public override dynamic? OverrideMethod()
        {
            var i = 42;
            var j = 42;

            return meta.Proceed();
        }
    }

    // <target>
    internal class TargetClass
    {
        [InnerOverride]
        [OuterOverride]
        public int TargetMethod_ConflictBetweenOverrides()
        {
            return 42;
        }

        [InnerOverride]
        [OuterOverride]
        public int TargetMethod_ConflictWithParameter( int i )
        {
            return 42;
        }

        [InnerOverride]
        [OuterOverride]
        public int TargetMethod_ConflictWithTarget()
        {
            var i = 0;

            return 42;
        }

        [InnerOverride]
        [OuterOverride]
        public int TargetMethod_MultipleConflicts()
        {
            var i = 0;
            var j = 0;

            return 42;
        }
    }
}