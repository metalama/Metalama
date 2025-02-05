using System;

namespace Metalama.Framework.IntegrationTests.Aspects.Overrides.Events.CrossAssembly
{
    // <target>
    [Override]
    [Introduction]
    internal class TargetClass
    {
        public event EventHandler ExistingEvent
        {
            add
            {
                Console.WriteLine("Original");
            }
            remove
            {
                Console.WriteLine("Original");
            }
        }
    }
}