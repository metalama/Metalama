using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.PatternMatching.PropertyPattern
{
    internal class Aspect
    {
        [TestTemplate]
        private dynamic? Template()
        {
            // Compile time
            var ct = meta.CompileTime( new object() );
            var a1 = ct is IParameter { Index: var index } p && p.DefaultValue.HasValue && index > 0;
            meta.InsertComment( "a1 = " + a1 );

            // Run-time
            var a2 = meta.Target.Parameters[0].Value is >= 0 and < 5;

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