using System;

namespace Metalama.Framework.IntegrationTests.Aspects.Overrides.Events.PartialType_SyntaxTrees
{
    // <target>
    internal partial class TargetClass
    {
        public event EventHandler TargetEvent2
        {
            add => Console.WriteLine("This is TargetEvent2.");
            remove => Console.WriteLine("This is TargetEvent2.");
        }
    }
}