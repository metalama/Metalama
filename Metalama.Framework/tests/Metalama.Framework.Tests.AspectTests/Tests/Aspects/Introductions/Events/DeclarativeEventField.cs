#pragma warning disable CS0067

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Introductions.Events.Declarative
{
    public class IntroductionAttribute : TypeAspect
    {
        [Introduce]
        public event EventHandler? Event;
    }

    // <target>
    [Introduction]
    internal class TargetClass { }
}