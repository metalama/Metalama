using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Tests.Templating.Syntax.Tuples.ReturnCompileTimeTuple
{
    internal class Aspect
    {
        [TestTemplate]
        private (int, Type) Template()
        {
            var t = ( meta.Target.Method.Parameters.Count, typeof(int) );

            return ( t.Count + 1, t.Item2 );
        }
    }

    internal class TargetCode
    {
        private (int, Type) Method( int a )
        {
            return meta.CompileTime( ( a, typeof(int) ) );
        }
    }
}