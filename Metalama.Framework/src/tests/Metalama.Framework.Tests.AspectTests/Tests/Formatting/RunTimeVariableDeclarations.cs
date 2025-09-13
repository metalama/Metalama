// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#pragma warning disable CS0219

using System;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.TestInputs.Highlighting.Declarations.RunTimeVariableDeclarations
{
    internal class RuntimeClass
    {
        public void RunTimeMethod()
        {
        }
    }

    internal class Aspect : IAspect
    {
        [Template]
        private dynamic? Template()
        {
            var runTimeClassInstance = new RuntimeClass();

            var scalar = 0;
            var array = new int[10];
            object @object = "";
            var @string = "";
            Action action = runTimeClassInstance.RunTimeMethod;
            (int, byte) tuple = (0, 1);
            var generic = new Tuple<int, byte>(2, 3);

            return meta.Proceed();
        }
    }
}
