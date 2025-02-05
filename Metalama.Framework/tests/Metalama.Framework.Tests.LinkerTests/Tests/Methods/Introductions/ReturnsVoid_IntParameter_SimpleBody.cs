namespace Metalama.Framework.Tests.LinkerTests.Tests.Methods.Introductions.ReturnsVoid_IntParameter_SimpleBody
{
    // <target>
    class Target
    {
        [PseudoIntroduction("TestAspect")]
        public void Foo(int x)
        {
        }

        [PseudoIntroduction("TestAspect")]
        public static void Bar(int x)
        {
        }
    }
}
