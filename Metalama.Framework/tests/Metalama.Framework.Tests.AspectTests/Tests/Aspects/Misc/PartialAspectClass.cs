using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Misc.PartialAspectClass
{
    internal partial class MyAspect : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            foreach (var m in builder.Target.Methods)
            {
                builder.With( m ).Override( nameof(MethodTemplate) );
            }
        }
    }

    // <target>
    [MyAspect]
    internal class C
    {
        private void M() { }
    }
}