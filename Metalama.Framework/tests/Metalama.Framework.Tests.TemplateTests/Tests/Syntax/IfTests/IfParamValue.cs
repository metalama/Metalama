using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Templating.Syntax.IfTests.IfParamValue
{
    [CompileTime]
    internal class Aspect
    {
        [TestTemplate]
        private dynamic? Template()
        {
            if (meta.Target.Parameters[0].Value == null)
            {
                throw new ArgumentNullException( meta.Target.Parameters[0].Name );
            }

            var p = meta.Target.Parameters[1];

            if (p.Value == null)
            {
                throw new ArgumentNullException( p.Name );
            }

            return meta.Proceed();
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