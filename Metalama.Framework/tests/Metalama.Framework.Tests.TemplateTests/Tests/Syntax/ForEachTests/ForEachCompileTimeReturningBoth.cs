#pragma warning disable CS0649, CS8618

using System;
using System.Collections.Generic;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Templating.Syntax.ForEach.CompileTimeReturningBoth
{
    [CompileTime]
    internal class CompileTimeClass
    {
        public IEnumerable<int> compileTimeEnumerable = new[] { 1, 2, 3 };
    }

    [CompileTime]
    internal class Aspect
    {
        [TestTemplate]
        private dynamic? Template()
        {
            var compileTimeObject = new CompileTimeClass();

            foreach (var x in compileTimeObject.compileTimeEnumerable)
            {
                Console.WriteLine( x.ToString() );
            }

            foreach (var x in meta.RunTime( compileTimeObject.compileTimeEnumerable ))
            {
                Console.WriteLine( x.ToString() );
            }

            return meta.Proceed();
        }
    }

    internal class TargetCode
    {
        private void Method() { }
    }
}