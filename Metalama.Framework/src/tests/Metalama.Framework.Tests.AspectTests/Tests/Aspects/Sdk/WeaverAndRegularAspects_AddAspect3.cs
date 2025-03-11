// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Fabrics;
using Metalama.Framework.Tests.AspectTests.Tests.Aspects.Sdk.WeaverAndRegularAspects_AddAspect3;

[assembly: AspectOrder( AspectOrderDirection.RunTime, typeof(RegularAspect1), typeof(WeaverAspect), typeof(CombinedAspect), typeof(RegularAspect2) )]

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Sdk.WeaverAndRegularAspects_AddAspect3
{
    [RequireAspectWeaver( "Metalama.Framework.Tests.AspectTests.Tests.Aspects.Sdk.WeaverAndRegularAspects_AddAspect3.AspectWeaver" )]
    internal class WeaverAspect : MethodAspect { }

    // Weaver aspect is not actually used, so weaver does not have to exist.

    internal class RegularAspect1 : OverrideMethodAspect
    {
        public override dynamic? OverrideMethod()
        {
            Console.WriteLine( "Added by regular aspect #1." );

            return meta.Proceed();
        }
    }

    internal class RegularAspect2 : OverrideMethodAspect
    {
        public override dynamic? OverrideMethod()
        {
            Console.WriteLine( "Added by regular aspect #2." );

            return meta.Proceed();
        }
    }

    internal class CombinedAspect : MethodAspect
    {
        public override void BuildAspect( IAspectBuilder<IMethod> builder )
        {
            builder.AddAspect<RegularAspect1>();
            builder.AddAspect<RegularAspect2>();
        }
    }

    // <target>
    internal class TargetCode
    {
        [CombinedAspect]
        private void M() { }
    }
}