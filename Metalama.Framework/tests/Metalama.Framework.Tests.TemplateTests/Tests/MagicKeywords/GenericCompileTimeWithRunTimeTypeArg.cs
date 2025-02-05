using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

#pragma warning disable CS8632 // Cannot convert null literal to non-nullable reference type.

namespace Metalama.Framework.Tests.AspectTests.TestInputs.MagicKeywords.GenericCompileTimeWithRunTimeTypeArg
{
    namespace UsingStatic
    {
        [CompileTime]
        internal class Aspect
        {
            [TestTemplate]
            private dynamic? Template()
            {
                var x = meta.CompileTime<TargetCode?>( null );
                var y = meta.CompileTime<TargetCode>( null );

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
}