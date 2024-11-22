using System;
using System.Collections.Generic;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.CharLiteral;

class Aspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod() => new char[] { 'a', '\n', '\x22', '\u0022', '"', '\'' };
}

class Foo
{
    // <target>
    [Aspect]
    public static char[] M() => [ '0' ];
}