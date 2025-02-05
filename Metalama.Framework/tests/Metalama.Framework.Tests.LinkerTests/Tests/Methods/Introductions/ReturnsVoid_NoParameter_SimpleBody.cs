namespace Metalama.Framework.Tests.LinkerTests.Tests.Methods.Introductions.ReturnsVoid_NoParameter_SimpleBody
{
    // <target>
    class Target
    {
        [PseudoIntroduction("TestAspect")]
        public void Foo()
        {
        }

        [PseudoIntroduction("TestAspect")]
        public static void Bar()
        {
        }
    }
}
