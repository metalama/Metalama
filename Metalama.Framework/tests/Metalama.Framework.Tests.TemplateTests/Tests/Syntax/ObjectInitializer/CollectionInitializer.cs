using System;
using System.Collections.Generic;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Templating.CSharpSyntax.Misc.CollectionInitializer
{
    [CompileTime]
    internal class Aspect
    {
        [TestTemplate]
        private dynamic? Template()
        {
            var x = new List<int> { 1, 2, 3 };
            var y = new List<string> { meta.Target.Parameters.Count.ToString(), "a", "b" };
            Console.WriteLine( string.Join( ", ", y ) );
            var z = new List<int> { meta.Target.Parameters[0].Value, 2, 3 };

            return default;
        }
    }

    internal class TargetCode
    {
        private int Method( int a, int b )
        {
            return a + b;
        }
    }
}