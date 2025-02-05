using System.Linq;
using System.Collections.Generic;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Templating.Syntax.Lambdas.Bug28768
{
    internal class Aspect
    {
        [TestTemplate]
        private dynamic? Template()
        {
            // The cast to IEnumerable is to avoid using the LinqExtensions class in the engine project.

            var parameterNamesTypes =
                meta.RunTime( ( (IEnumerable<IParameter>)meta.Target.Parameters ).Select( p => ( (IParameter)p ).Type.ToType() ).ToArray() );

            return meta.Proceed();
        }
    }

    internal class TargetCode
    {
        private int Method( int a, string b )
        {
            return a;
        }
    }
}