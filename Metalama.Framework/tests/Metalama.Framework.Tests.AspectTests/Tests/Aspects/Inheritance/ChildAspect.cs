// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Tests.AspectTests.Tests.Aspects.Inheritance.ChildAspect_;

[assembly: AspectOrder( AspectOrderDirection.RunTime, typeof(ChildAspect), typeof(ParentAspect) )]

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Inheritance.ChildAspect_;

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

// <target>
internal class Targets
{
    [ParentAspect]
    public class BaseTarget
    {
        public virtual void M() { }
    }

    public class DerivedTarget : BaseTarget
    {
        public override void M()
        {
            Console.WriteLine( "Hello, world." );
        }
    }
}