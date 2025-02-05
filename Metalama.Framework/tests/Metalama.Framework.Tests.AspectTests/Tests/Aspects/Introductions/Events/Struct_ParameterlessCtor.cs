using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using System;

#pragma warning disable CS0067, CS0414

namespace Metalama.Framework.IntegrationTests.Aspects.Introductions.Events.Struct_ParameterlessCtor
{
    public class IntroductionAttribute : TypeAspect
    {
        [Introduce]
        public event EventHandler? IntroducedEvent;

        [Introduce]
        public event EventHandler? IntroducedEvent_Initializer = Foo;

        [Introduce]
        public event EventHandler? IntroducedEvent_Static;

        [Introduce]
        public event EventHandler? IntroducedEvent_Static_Initializer = Foo;

        [Introduce]
        public static void Foo( object? sender, EventArgs args ) { }
    }

    // <target>
    [Introduction]
    internal struct TargetStruct
    {
        public TargetStruct() { }

        public int ExistingField = 42;

        public int ExistingProperty { get; set; } = 42;
    }
}