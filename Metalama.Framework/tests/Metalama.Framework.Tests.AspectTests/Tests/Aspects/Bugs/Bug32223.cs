using System.Collections.Generic;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug32223;

#pragma warning disable CS0168, CS8618, CS0169

public class Base<T> { }

// <target>
public class C : Base<dynamic>
{
    private dynamic _f1;
    private dynamic[] _f2;
    private List<dynamic> _f3;

    private dynamic M( dynamic x )
    {
        dynamic l1;
        dynamic[] l2;
        List<dynamic> l3;

        return x;
    }
}