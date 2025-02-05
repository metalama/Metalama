using System;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.TestInputs.Highlighting.Declarations.CompileTimeVariableDeclarations
{
    [CompileTime]
    class CompileTimeClass
    {
        public void CompileTimeMethod()
        {
        }
    }

    class Aspect : IAspect
    {
        [Template]
        dynamic? Template()
        {
            var compiletimeClassInstance = new CompileTimeClass();

            int scalar = meta.CompileTime(0);
            int[] array = meta.CompileTime(new int[10]);
            object @object = meta.CompileTime("");
            string @string = meta.CompileTime("");
            Action action = compiletimeClassInstance.CompileTimeMethod;
            (int, byte) tuple = meta.CompileTime((0, (byte)1));
            Tuple<int, byte> generic = meta.CompileTime(new Tuple<int, byte>(2, 3));

            return meta.Proceed();
        }
    }
}
