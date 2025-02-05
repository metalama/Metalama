using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.IntegrationTests.Aspects.InvalidCode.TargetTypeWithInvalidBaseList;

[Inheritable]
public class TestAttribute : TypeAspect
{
    [Introduce]
    public void Foo()
    {
    }
}

#if TESTRUNNER
// <target>
[Test]
internal partial class InvalidBase : SomethingThatDoesNotExist 
{ 
}
#endif

#if TESTRUNNER
// <target>
[Test]
internal partial class MissingInterface : object, 
{ 
}
#endif

#if TESTRUNNER
// <target>
[Test]
internal partial class InvalidInterface : object, ISomethingThatDoesNotExist
{ 
}
#endif

#if TESTRUNNER
// <target>
[Test]
internal partial class InvalidTypeParameterList<T,>
{
}
#endif