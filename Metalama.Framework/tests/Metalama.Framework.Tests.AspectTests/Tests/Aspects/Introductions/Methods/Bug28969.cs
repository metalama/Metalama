using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Methods.Bug28969
{
    internal class MyAspect : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            var methodBuilder = builder.IntroduceMethod( nameof(Method) );
        }

        [Template( IsVirtual = true )]
        public void Method() { }
    }

    // <target>
    [MyAspect]
    internal class TargetCode { }
}