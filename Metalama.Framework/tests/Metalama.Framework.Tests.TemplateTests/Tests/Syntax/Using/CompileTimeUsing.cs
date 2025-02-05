#pragma warning disable CS0162

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Templating.Syntax.Using.CompileTimeUsing
{
    [CompileTime]
    internal class DisposableClass : IDisposable
    {
        public void Dispose() { }
    }

    internal class Aspect
    {
        [TestTemplate]
        private dynamic? Template()
        {
            using (new DisposableClass())
            {
                return meta.Proceed();
            }

            using (DisposableClass c = null) { }
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