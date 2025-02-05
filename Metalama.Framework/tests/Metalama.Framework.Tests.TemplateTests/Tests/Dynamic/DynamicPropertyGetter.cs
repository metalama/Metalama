using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Templating.Dynamic.DynamicPropertyMember
{
    [CompileTime]
    internal class Aspect
    {
        [TestTemplate]
        private dynamic? Template()
        {
            // Dynamic property as method argument.
            Console.WriteLine( meta.This );
            Console.WriteLine( meta.Target.Parameters[0].Value );

            // Dynamic property in assignment;
            object o;
            o = meta.This;

            // Dynamic property in variable initialization/
            object x = meta.This;

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