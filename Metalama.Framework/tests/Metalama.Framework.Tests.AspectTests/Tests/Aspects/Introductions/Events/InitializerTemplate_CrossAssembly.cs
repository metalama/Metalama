using System;

#pragma warning disable CS0414

namespace Metalama.Framework.IntegrationTests.Aspects.Introductions.Events.InitializerTemplate_CrossAssembly
{
    // <target>
    [Introduction]
    internal class TargetClass 
    {
        public static EventHandler Foo = new EventHandler(Bar);

        public static void Bar(object? sender, EventArgs eventArgs)
        {
        }
    }
}