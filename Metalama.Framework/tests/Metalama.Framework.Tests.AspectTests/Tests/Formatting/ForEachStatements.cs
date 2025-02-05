#pragma warning disable CS0649, CS8618

using System.Collections.Generic;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.TestInputs.Highlighting.ForEachStatements.ForEachStatements
{
    class RunTimeClass
    {
        public IEnumerable<int> runTimeEnumerable;
    }

    [CompileTime]
    class CompileTimeClass
    {
        public IEnumerable<int> compileTimeEnumerable;
    }

    class Aspect : IAspect
    {
        [Template]
        dynamic? Template()
        {
            var runTimeObject = new RunTimeClass();
            var compileTimeObject = new CompileTimeClass();

            foreach (var x in compileTimeObject.compileTimeEnumerable)
            {
                x.ToString();
            }


            foreach (var x in runTimeObject.runTimeEnumerable)
            {
                x.ToString();
            }

            return meta.Proceed();
        }
    }
}
