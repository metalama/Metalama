using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using System;

#pragma warning disable CS0067, CS0414

namespace Metalama.Framework.IntegrationTests.Aspects.Introductions.Events.Struct_NoCtor
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
        public int ExistingField;

        public int ExistingProperty { get; set; }
    }
}