using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.LanguageVersion.Template_Expression;

public class TheAspect : TypeAspect
{
    [Introduce]
    public string Property
        => """
           This is a long message.
           It has several lines.
               Some are indented
                       more than others.
           Some should start at the first column.
           Some have "quoted text" in them.
           """;
}

// <target>
[TheAspect]
internal class Target { }