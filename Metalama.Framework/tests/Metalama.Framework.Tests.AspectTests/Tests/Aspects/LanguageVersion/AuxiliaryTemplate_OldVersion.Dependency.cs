using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.LanguageVersion.AuxiliaryTemplate_OldVersion;

public class TheAspect : TypeAspect
{
    [Introduce]
    private void M()
    {
        AuxiliaryTemplate();
        meta.InvokeTemplate( nameof(AuxiliaryTemplate) );
    }

    [Template]
    private void AuxiliaryTemplate()
    {
        Console.WriteLine( """aux""" );
    }
}