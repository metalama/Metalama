using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using System;
using Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.EventFields.InterfaceMember;
using Metalama.Framework.Code;

#pragma warning disable CS0067

[assembly: AspectOrder( AspectOrderDirection.RunTime, typeof(OverrideAttribute), typeof(IntroductionAttribute) )]

namespace Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.EventFields.InterfaceMember
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

    public interface Interface
    {
        public event EventHandler? Event;

        public event EventHandler? InitializerEvent;
    }

    public interface IntroducedInterface
    {
        public event EventHandler? IntroducedEvent;

        public event EventHandler? InitializerIntroducedEvent;

        public event EventHandler? ExplicitIntroducedEvent;

        public event EventHandler? InitializerExplicitIntroducedEvent;
    }

    public class IntroductionAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            builder.ImplementInterface( typeof(IntroducedInterface) );
        }

        [InterfaceMember( IsExplicit = false )]
        public event EventHandler? IntroducedEvent;

        [InterfaceMember( IsExplicit = false )]
        public event EventHandler? InitializerIntroducedEvent = Bar;

        [InterfaceMember( IsExplicit = true )]
        public event EventHandler? ExplicitIntroducedEvent;

        [InterfaceMember( IsExplicit = true )]
        public event EventHandler? InitializerExplicitIntroducedEvent = Bar;

        [Introduce]
        public static void Bar( object? sender, EventArgs args ) { }
    }

    // <target>
    [Override]
    [Introduction]
    internal class TargetClass : Interface
    {
        public event EventHandler? Event;

        public event EventHandler? InitializerEvent = Foo;

        public static void Foo( object? sender, EventArgs args ) { }
    }
}