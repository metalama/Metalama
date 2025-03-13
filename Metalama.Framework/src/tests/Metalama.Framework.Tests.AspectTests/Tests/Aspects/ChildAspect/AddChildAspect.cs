// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Linq;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Fabrics;
using Metalama.Framework.Tests.AspectTests.Tests.Aspects.AddAspect.AddChildAspect;

[assembly: AspectOrder( AspectOrderDirection.RunTime, typeof(Aspect2), typeof(Aspect1) )]

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.AddAspect.AddChildAspect
{
    internal class Aspect1 : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            builder.Outbound.SelectMany( t => t.Methods ).AddAspect( _ => new Aspect2( "Hello, world." ) );
        }
    }

    internal class Aspect2 : OverrideMethodAspect
    {
        private string _value;

        public Aspect2( string value )
        {
            this._value = value;
        }

        public override dynamic? OverrideMethod()
        {
            Console.WriteLine( this._value );
            Console.WriteLine( meta.AspectInstance.Predecessors.Single().Instance.ToString() );

            return meta.Proceed();
        }
    }

    [Aspect1]
    internal class TargetCode
    {
        // <target>
        private int Method( int a )
        {
            return a;
        }
    }
}