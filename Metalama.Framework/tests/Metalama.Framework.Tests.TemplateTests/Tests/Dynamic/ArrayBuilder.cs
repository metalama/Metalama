using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code.SyntaxBuilders;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Tests.Templating.Dynamic.DynamicArrayBuilder
{
    [CompileTime]
    internal class Aspect
    {
        [TestTemplate]
        private dynamic? Template()
        {
            var array = new ArrayBuilder();

            foreach (var p in meta.Target.Parameters)
            {
                array.Add( p.Value );
            }

            var array1 = array.ToValue();
            var array2 = array.ToExpression().Value;

            return default;
        }
    }

    // <target>
    internal class TargetCode
    {
        private int Method( int a, string c, DateTime dt )
        {
            return a;
        }
    }
}