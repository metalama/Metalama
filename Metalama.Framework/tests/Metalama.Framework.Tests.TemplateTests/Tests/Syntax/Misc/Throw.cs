#pragma warning disable CS0162 // Unreachable code detected

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Templating.Misc.Throw
{
    internal class Aspect
    {
        [TestTemplate]
        private dynamic Template()
        {
            try
            {
                throw new ArgumentNullException( meta.Target.Parameters[0].Name );
            }
            catch
            {
                throw;
            }

            return meta.Proceed();
        }
    }

    internal class TargetCode
    {
        private int Method( int a )
        {
            return a;
        }
    }
}