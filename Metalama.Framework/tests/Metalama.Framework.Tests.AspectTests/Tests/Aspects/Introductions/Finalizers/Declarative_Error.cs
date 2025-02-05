using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Finalizers.Declarative_Error;

/*
 * Tests that using the declarative finalizer introduction produces an error.
 */

public class TestAttribute : TypeAspect
{
    [Introduce]
    ~TestAttribute() { }
}

// <target>
[Test]
internal class Target { }