using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.PublicPipeline.Aspects.Misc.CompileTimeRecord
{
    internal class Aspect : MethodAspect
    {
        public override void BuildAspect( IAspectBuilder<IMethod> builder )
        {
            // This is just to test that it builds.
            CompileTimeRecord r = new( 0, "" );
        }
    }

    [CompileTime]
    internal record CompileTimeRecord( int a, string b );

    internal record RunTimeRecord( int a, string b );

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