using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.LanguageVersion.Template_Property_OldVersion;

public class TheAspect : TypeAspect
{
    [Introduce]
    public string Property1
    {
        get
        {
            return """get""";
        }
    }

    [Introduce]
    public string Property2
    {
        get => "";
        set
        {
            Console.WriteLine( """set""" );
        }
    }
}