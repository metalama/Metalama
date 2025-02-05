using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug32491;

[MyAttribute]
public class MyAspect : TypeAspect
{
    [MyAttribute]
    [CompileTime]
    public void Method() { }
}

[AttributeUsage( AttributeTargets.Class | AttributeTargets.Method )]
public class MyAttribute : Attribute { }

// <target>
internal class C { }