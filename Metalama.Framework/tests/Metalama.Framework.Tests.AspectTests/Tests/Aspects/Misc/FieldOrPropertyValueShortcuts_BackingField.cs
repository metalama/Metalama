using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

#pragma warning disable CS1717, CS0414

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Misc.FieldOrPropertyValueShortcuts_BackingField
{
    internal class MyAspect : TypeAspect
    {
        [Introduce]
        public void Method()
        {
            // TODO: Throw an exception. Backing fields should not be accessible from invokers.

            foreach (var field in meta.Target.Type.Fields)
            {
                field.Value = field.Value;
            }
        }
    }

    // <target>
    [MyAspect]
    internal class C
    {
        public int P { get; set; }
    }
}