using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Templating.Dynamic.Issue28709
{
    [CompileTime]
    internal class Aspect
    {
        [TestTemplate]
        private dynamic? Template()
        {
            foreach (var p in meta.Target.Parameters)
            {
                Console.WriteLine( "IsOut=" + ( p.RefKind == RefKind.Out ) );
            }

            return default;
        }
    }

    // Placeholder implementation of a cache because the hosted try.postsharp.net does not allow for MemoryCache.

    // <target>
    internal class TargetCode
    {
        private static int Method( int a, int b, out int c )
        {
            Console.WriteLine( "Thinking..." );

            return c = a + b;
        }
    }
}