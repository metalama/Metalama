using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Templating.Dynamic.SerializationErrorWeaklyTyped
{
    [CompileTime]
    internal class Aspect
    {
        [TestTemplate]
        private dynamic? Template()
        {
            object method = meta.Target.Method;
            Console.WriteLine( meta.RunTime( method ) );

            return default;
        }
    }

    // <target>
    internal class TargetCode
    {
        private int Method( int a )
        {
            return a;
        }
    }
}