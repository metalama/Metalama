using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp11.RawLiteral_TargetCode;

public class TheAspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        Console.WriteLine( "Oops" );

        return meta.Proceed();
    }
}

public class C
{
    // <target>
    [TheAspect]
    private void M()
    {
        var longMessage = """
                          This is a long message.
                          It has several lines.
                              Some are indented
                                      more than others.
                          Some should start at the first column.
                          Some have "quoted text" in them.
                          """;

        Console.WriteLine( longMessage );
    }
}