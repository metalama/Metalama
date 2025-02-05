using static Metalama.Framework.Tests.LinkerTests.Tests.Api;

namespace Metalama.Framework.Tests.LinkerTests.Tests.Methods.Overrides.Signature.ReturnsInt_NoParameter
{
    // <target>
    class Target
    {
        int Foo()
        {
            return 42;
        }

        [PseudoOverride( nameof(Foo),"TestAspect")]
        int Foo_Override()
        {
            return link( _this.Foo, inline)();
        }
    }
}
