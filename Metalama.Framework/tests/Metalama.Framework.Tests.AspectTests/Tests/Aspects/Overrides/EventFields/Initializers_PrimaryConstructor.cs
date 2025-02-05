#if TEST_OPTIONS
// @RequiredConstant(ROSLYN_4_8_0_OR_GREATER)
#endif

#if ROSLYN_4_8_0_OR_GREATER

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.SyntaxBuilders;
using System;
using Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.EventFields.Initializers_PrimaryConstructor;

#pragma warning disable CS0067

[assembly: AspectOrder( AspectOrderDirection.RunTime, typeof(OverrideAttribute), typeof(IntroductionAttribute) )]

namespace Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.EventFields.Initializers_PrimaryConstructor
{
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
            Console.WriteLine( "This is the add template." );
            meta.Proceed();
        }

        [Template]
        public void OverrideRemove( dynamic value )
        {
            Console.WriteLine( "This is the remove template." );
            meta.Proceed();
        }
    }

    public class IntroductionAttribute : TypeAspect
    {
        [Introduce]
        public event EventHandler? IntroducedEvent = ExpressionFactory.Parse( "h" ).Value;
    }

    // <target>
    [Override]
    [Introduction]
    internal class TargetClass( EventHandler h )
    {
        public event EventHandler? Event = h;
    }
}

#endif