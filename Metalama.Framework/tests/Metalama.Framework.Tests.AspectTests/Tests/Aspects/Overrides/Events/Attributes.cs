using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;
using Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Events.Attributes;

[assembly: AspectOrder( AspectOrderDirection.RunTime, typeof(OverrideAttribute), typeof(IntroductionAttribute) )]

#pragma warning disable CS0169
#pragma warning disable CS0414

namespace Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Events.Attributes
{
    /*
     * Tests that overriding event keeps all the existing attributes.
     */

    public class OverrideAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            foreach (var @event in builder.Target.Events)
            {
                builder.With( @event ).OverrideAccessors( nameof(OverrideAdd), nameof(OverrideRemove) );
            }
        }

        [Template]
        public void OverrideAdd( dynamic value )
        {
            Console.WriteLine( "This is the overridden add." );
            meta.Proceed();
        }

        [Template]
        public void OverrideRemove( dynamic value )
        {
            Console.WriteLine( "This is the overridden remove." );
            meta.Proceed();
        }
    }

    public class IntroductionAttribute : TypeAspect
    {
        [Introduce]
        [EventOnly]
        public event EventHandler? IntroducedEvent
        {
            [MethodOnly]
            [param: ParamOnly]
            [return: ReturnValueOnly]
            add
            {
                Console.WriteLine( "This is the introduced add." );
                meta.Proceed();
            }

            [MethodOnly]
            [param: ParamOnly]
            [return: ReturnValueOnly]
            remove
            {
                Console.WriteLine( "This is the introduced remove." );
                meta.Proceed();
            }
        }
    }

    [AttributeUsage( AttributeTargets.Method )]
    public class MethodOnlyAttribute : Attribute { }

    [AttributeUsage( AttributeTargets.Event )]
    public class EventOnlyAttribute : Attribute { }

    [AttributeUsage( AttributeTargets.Parameter )]
    public class ParamOnlyAttribute : Attribute { }

    [AttributeUsage( AttributeTargets.ReturnValue )]
    public class ReturnValueOnlyAttribute : Attribute { }

    // <target>
    [Introduction]
    [Override]
    internal class TargetClass
    {
        [EventOnly]
        public event EventHandler? Event
        {
            [MethodOnly]
            [param: ParamOnly]
            [return: ReturnValueOnly]
            add
            {
                Console.WriteLine( "This is the original add." );
            }

            [MethodOnly]
            [param: ParamOnly]
            [return: ReturnValueOnly]
            remove
            {
                Console.WriteLine( "This is the original remove." );
            }
        }
    }
}