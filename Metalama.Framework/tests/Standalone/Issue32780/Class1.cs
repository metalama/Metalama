using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System.ComponentModel.DataAnnotations;

namespace Issue32780;

public class TheAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        var required = new RequiredAttribute();
    }

    [Introduce]
    public static void Introduced() { }
}

[TheAspect]
public class C
{
    void M()
    {
        Introduced();
    }
}

