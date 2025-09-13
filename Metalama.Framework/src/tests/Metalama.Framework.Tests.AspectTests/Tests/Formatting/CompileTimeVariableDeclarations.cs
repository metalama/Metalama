// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.TestInputs.Highlighting.Declarations.CompileTimeVariableDeclarations
{
    [CompileTime]
    internal class CompileTimeClass
    {
        public void CompileTimeMethod()
        {
        }
    }

    internal class Aspect : IAspect
    {
        [Template]
        private dynamic? Template()
        {
            var compileTimeClassInstance = new CompileTimeClass();

#pragma warning disable IDE0007 // Use implicit type
            int scalar = meta.CompileTime(0);
            int[] array = meta.CompileTime(new int[10]);
            object @object = meta.CompileTime("");
            string @string = meta.CompileTime("");
            Action action = compileTimeClassInstance.CompileTimeMethod;
            (int, byte) tuple = meta.CompileTime((0, (byte)1));
            Tuple<int, byte> generic = meta.CompileTime(new Tuple<int, byte>(2, 3));

            return meta.Proceed();
        }
    }
}
