using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Operators.Declarative;

/*
 * Tests that using the declarative finalizer introduction produces an error.
 */

public class TestAttribute : TypeAspect
{
    [Introduce]
    public static TestAttribute operator -( TestAttribute t )
    {
        return t;
    }

    [Introduce]
    public static explicit operator string( TestAttribute t )
    {
        return t.ToString();
    }
}

// <target>
[Test]
internal class Target { }