using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Templating.Syntax.Combined.ForEachParamIfValue
{
    [CompileTime]
    internal class Aspect
    {
        [TestTemplate]
        private dynamic? Template()
        {
            foreach (var p in meta.Target.Parameters)
            {
                if (p.Value == null)
                {
                    throw new ArgumentNullException( p.Name );
                }
            }

            var result = meta.Proceed();

            return result;
        }
    }

    internal class TargetCode
    {
        private string Method( object a, object b )
        {
            return a.ToString() + b.ToString();
        }
    }
}