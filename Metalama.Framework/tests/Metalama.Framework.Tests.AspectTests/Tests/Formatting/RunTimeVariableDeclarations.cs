#pragma warning disable CS0219

using System;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.TestInputs.Highlighting.Declarations.RunTimeVariableDeclarations
{
    class RuntimeClass
    {
        public void RunTimeMethod()
        {
        }
    }

    class Aspect : IAspect
    {
        [Template]
        dynamic? Template()
        {
            var runTimeClassInstance = new RuntimeClass();

            int scalar = 0;
            int[] array = new int[10];
            object @object = "";
            string @string = "";
            Action action = runTimeClassInstance.RunTimeMethod;
            (int, byte) tuple = (0, 1);
            Tuple<int, byte> generic = new Tuple<int, byte>(2, 3);

            return meta.Proceed();
        }
    }
}
