// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.TestInputs.Highlighting.IfStatements.CompileTimeIfCondition
{
    class RunTimeClass
    {
        public void RunTimeMethod()
        {
        }
    }

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
            var runTimeObject = new RunTimeClass();
            var compileTimeObject = new CompileTimeClass();

            int compileTimeVariable = meta.CompileTime(0);

            if (compileTimeVariable == 0
                // Intentionally on three lines
                || compileTimeVariable == 1
                || compileTimeVariable == 2 )
            {
                runTimeObject.RunTimeMethod();
                compileTimeObject.CompileTimeMethod();
                meta.Target.Method.ToString();
            }
            else
            {
                runTimeObject.RunTimeMethod();
                compileTimeObject.CompileTimeMethod();
                meta.Target.Method.ToString();
            }

            return meta.Proceed();
        }
    }
}
