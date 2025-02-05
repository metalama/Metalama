using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Properties.AccessorBasedNoSetter
{
    internal class MyAspect : TypeAspect
    {
        [Template]
        public int Getter() => 5;

        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            builder.IntroduceProperty( "TheProperty", nameof(Getter), null );
        }
    }

    // <target>
    [MyAspect]
    internal class C { }
}