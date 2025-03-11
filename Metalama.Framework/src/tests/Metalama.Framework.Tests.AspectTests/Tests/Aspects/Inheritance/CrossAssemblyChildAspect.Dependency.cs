// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Tests.AspectTests.Tests.Aspects.Inheritance.CrossAssemblyChildAspect;

[assembly: AspectOrder( AspectOrderDirection.RunTime, typeof(ChildAspect), typeof(ParentAspect) )]

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Inheritance.CrossAssemblyChildAspect
{
    public class ParentAspect : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            base.BuildAspect( builder );

            foreach (var m in builder.Target.Methods)
            {
                builder.With( m ).AddAspect( new ChildAspect() );
            }
        }
    }

    [Inheritable]
    public class ChildAspect : OverrideMethodAspect
    {
        public override dynamic? OverrideMethod()
        {
            Console.WriteLine( "From ChildAspect" );

            return meta.Proceed();
        }
    }

    [ParentAspect]
    public interface I
    {
        void M();
    }
}