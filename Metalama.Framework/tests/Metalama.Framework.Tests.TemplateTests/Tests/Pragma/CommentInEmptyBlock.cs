#pragma warning disable CS0162 // Unreachable code detected

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Templating.Pragma.CommentInEmptyBlock
{
    internal class Aspect
    {
        [TestTemplate]
        private dynamic? Template()
        {
            if (meta.RunTime( false ))
            {
                meta.InsertComment( "Oops 1" );
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