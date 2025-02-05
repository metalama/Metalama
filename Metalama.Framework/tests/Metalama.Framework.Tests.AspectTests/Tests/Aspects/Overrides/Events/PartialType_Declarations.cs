using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.IntegrationTests.Aspects.Overrides.Events.PartialType_Declarations
{
    // Tests that overriding methods of types with multiple partial declarations does work and targets correct methods.

    public class OverrideAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            foreach (var @event in builder.Target.Events)
            {
                builder.With( @event ).OverrideAccessors( nameof(Template), nameof(Template), null, tags: new { name = @event.Name } );
            }
        }

        [Template]
        public dynamic? Template()
        {
            Console.WriteLine( $"This is the override of {meta.Tags["name"]}." );

            return meta.Proceed();
        }
    }

    // <target>
    [Override]
    internal partial class TargetClass
    {
        public event EventHandler TargetEvent1
        {
            add => Console.WriteLine( "This is TargetEvent1." );
            remove => Console.WriteLine( "This is TargetEvent1." );
        }
    }

    // <target>
    internal partial class TargetClass
    {
        public event EventHandler TargetEvent2
        {
            add => Console.WriteLine( "This is TargetEvent2." );
            remove => Console.WriteLine( "This is TargetEvent2." );
        }
    }

    // <target>
    internal partial class TargetClass
    {
        public event EventHandler TargetEvent3
        {
            add => Console.WriteLine( "This is TargetEvent3." );
            remove => Console.WriteLine( "This is TargetEvent3." );
        }
    }
}