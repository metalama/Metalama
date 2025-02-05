using static Metalama.Framework.Tests.LinkerTests.Tests.Api;

namespace Metalama.Framework.Tests.LinkerTests.Tests.Methods.Overrides.Signature.ReturnsVoid_NoParameter
{
    // <target>
    class Target
    {
        void Foo()
        {
        }

        [PseudoOverride( nameof(Foo),"TestAspect")]
        void Foo_Override()
        {
            link( _this.Foo, inline)();
        }
    }
}
