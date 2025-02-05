using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;

namespace Metalama.Framework.IntegrationTests.Aspects.InvalidCode.TargetMemberInInvalidType;

[Inheritable]
public class TestAttribute : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        Console.WriteLine("Aspect");
        return meta.Proceed();
    }
}

#if TESTRUNNER
// <target>
internal partial class InvalidBase : SomethingThatDoesNotExist 
{ 
    [Test]
    public void Foo()
    {
    }
}
#endif

#if TESTRUNNER
// <target>
internal partial class MissingInterface : object,
{ 
    [Test]
    public void Foo()
    {
    }
}
#endif

#if TESTRUNNER
// <target>
internal partial class InvalidInterface : object, ISomethingThatDoesNotExist
{ 
    [Test]
    public void Foo()
    {
    }
}
#endif

#if TESTRUNNER
// <target>
internal partial class InvalidTypeParameterList<T,>
{ 
    [Test]
    public void Foo()
    {
    }
}
#endif