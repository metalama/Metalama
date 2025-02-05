using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Misc.CompileTimeDelegate
{
    internal class Aspect : MethodAspect
    {
        public override void BuildAspect( IAspectBuilder<IMethod> builder )
        {
            // This is just to test that it builds.
            CompileTimeDelegate d = new( () => { } );
            d.Invoke();
        }
    }

    [CompileTime]
    internal delegate void CompileTimeDelegate();

    internal delegate void RunTimeOnlyDelegate();

    // <target>
    internal class TargetCode
    {
        [Aspect]
        private int Method( int a )
        {
            return a;
        }
    }
}