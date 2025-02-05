#pragma warning disable CS0067

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Introductions.Events.DeclarativeEvent
{
    public class IntroductionAttribute : TypeAspect
    {
        [Introduce]
        public event EventHandler? Event
        {
            add
            {
                Console.WriteLine( "Original add accessor." );
            }

            remove
            {
                Console.WriteLine( "Original remove accessor." );
            }
        }
    }

    // <target>
    [Introduction]
    internal class TargetClass { }
}