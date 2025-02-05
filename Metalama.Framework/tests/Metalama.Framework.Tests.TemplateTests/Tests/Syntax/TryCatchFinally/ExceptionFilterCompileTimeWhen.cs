using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Templating.Syntax.TryCatchFinally.ExceptionFilterCompileTimeWhen
{
    [CompileTime]
    internal class Aspect
    {
        [TestTemplate]
        private dynamic Template()
        {
            var ctString = meta.CompileTime( "DivideByZero" );

            try
            {
                return 1;
            }
            catch (Exception e) when (e.GetType().Name.Contains( ctString ))
            {
                return -1;
            }
        }
    }

    internal class TargetCode
    {
        private int Method( int a )
        {
            return 42 / a;
        }
    }
}