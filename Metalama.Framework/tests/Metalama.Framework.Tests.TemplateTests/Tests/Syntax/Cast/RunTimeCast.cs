#pragma warning disable CS8600, CS8603
using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Templating.Syntax.Cast.RunTimeCast
{
    [CompileTime]
    internal class Aspect
    {
        [TestTemplate]
        private dynamic Template()
        {
            object arg0 = null;

            if (meta.Target.Parameters.Count > 0)
            {
                arg0 = meta.Target.Parameters[0].Value;

                if (arg0 is string)
                {
                    var s = (string)arg0;
                    Console.WriteLine( s );
                }
            }

            var result = meta.Proceed();
            object obj = result;
            var text = obj as string;

            if (text != null)
            {
                return text.Trim();
            }

            return obj;
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