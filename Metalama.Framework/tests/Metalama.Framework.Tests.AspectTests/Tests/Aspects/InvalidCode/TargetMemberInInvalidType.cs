// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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