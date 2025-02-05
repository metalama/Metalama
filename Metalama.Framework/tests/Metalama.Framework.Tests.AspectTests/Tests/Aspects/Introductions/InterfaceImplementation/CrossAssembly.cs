#pragma warning disable CS0067

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.CrossAssembly
{
    /*
     * Tests that interface is correctly introduced by aspect defined in another assembly.
     */

    // <target>
    [Introduction]
    public class TargetClass { }
}