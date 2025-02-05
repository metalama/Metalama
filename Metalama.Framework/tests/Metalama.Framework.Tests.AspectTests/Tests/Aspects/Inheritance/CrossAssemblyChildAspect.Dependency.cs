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