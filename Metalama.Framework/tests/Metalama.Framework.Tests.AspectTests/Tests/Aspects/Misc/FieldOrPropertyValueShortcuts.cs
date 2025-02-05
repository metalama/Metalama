using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

#pragma warning disable CS1717, CS0414

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Misc.FieldOrPropertyValueShortcuts
{
    internal class MyAspect : TypeAspect
    {
        [Introduce]
        public void Method()
        {
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
        private int _instanceField = 5;
        private static int _staticField = 6;
    }
}