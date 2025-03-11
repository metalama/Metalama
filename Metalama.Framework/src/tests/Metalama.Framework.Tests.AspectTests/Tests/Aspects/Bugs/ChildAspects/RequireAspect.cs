// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Linq;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Fabrics;
using Metalama.Framework.Tests.AspectTests.Tests.Aspects.AddAspect.RequireAspect;

[assembly: AspectOrder( AspectOrderDirection.RunTime, typeof(Aspect2), typeof(Aspect1) )]

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.AddAspect.RequireAspect
{
    internal class Aspect1 : ParameterAspect
    {
        public override void BuildAspect( IAspectBuilder<IParameter> builder )
        {
            builder.Outbound.Select( t => (IMethod) t.DeclaringMember ).RequireAspect<Aspect2>();
        }
    }

    internal class Aspect2 : OverrideMethodAspect
    {
        public override dynamic? OverrideMethod()
        {
            Console.WriteLine( string.Join( ",", meta.AspectInstance.Predecessors.Select( x => x.Instance.ToString() ).OrderBy( x => x ) ) );

            return meta.Proceed();
        }
    }

    internal class TargetCode
    {
        // <target>
        private int Method( [Aspect1] int a, [Aspect1] string b )
        {
            return a;
        }
    }
}