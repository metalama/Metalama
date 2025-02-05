using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Templating.Syntax.IfTests.IfCompileTimeIfRunTime
{
    [CompileTime]
    internal class Aspect
    {
        [TestTemplate]
        private dynamic? Template()
        {
            var p = meta.Target.Parameters[0];

            if (string.Equals( meta.Target.Method.Name, "NotNullMethod", StringComparison.Ordinal ))
            {
                if (p.Value == null)
                {
                    throw new ArgumentNullException( p.Name );
                }
            }
            else
            {
                if (string.IsNullOrEmpty( p.Value ))
                {
                    throw new ArgumentException( "IsNullOrEmpty", p.Name );
                }
            }

            return meta.Proceed();
        }
    }

    internal class TargetCode
    {
        private string Method( string a )
        {
            return a;
        }
    }
}