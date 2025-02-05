using System.Collections.Generic;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug31214;

public class MyAspect : OverrideMethodAspect
{
    private string _title;

    public MyAspect( string title )
    {
        _title = title;
    }

    public override dynamic? OverrideMethod()
    {
        var httpResult = "N/A";

        Dictionary<string, string> result =
            new() { { "Title", _title }, { "HTTP result", httpResult } };

        return default;
    }
}

// <target>
public class Foo
{
    [MyAspect( "The title" )]
    private void M() { }
}