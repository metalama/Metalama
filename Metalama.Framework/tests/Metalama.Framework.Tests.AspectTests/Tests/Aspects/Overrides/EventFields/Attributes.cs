using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;
using Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.EventFields.Attributes;

[assembly: AspectOrder( AspectOrderDirection.RunTime, typeof(OverrideAttribute), typeof(IntroductionAttribute) )]

#pragma warning disable CS0169
#pragma warning disable CS0414
#pragma warning disable CS0067

namespace Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.EventFields.Attributes
{
    /*
     * Tests that overriding event field keeps all the existing attributes.
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
        [field: FieldOnly]
        [method: MethodOnly]
        public event EventHandler? IntroducedEventField;
    }

    [AttributeUsage( AttributeTargets.Field )]
    public class FieldOnlyAttribute : Attribute { }

    [AttributeUsage( AttributeTargets.Method )]
    public class MethodOnlyAttribute : Attribute { }

    [AttributeUsage( AttributeTargets.Event )]
    public class EventOnlyAttribute : Attribute { }

    // <target>
    [Introduction]
    [Override]
    internal class TargetClass
    {
        // TODO: It is expected that the field attributes is not properly moved to the backing field as Roslyn currently does not expose the backing field.
        [EventOnly]
        [field: FieldOnly]
        [method: MethodOnly]
        public event EventHandler? EventField;
    }
}