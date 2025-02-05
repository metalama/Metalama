using System;
using System.IO;
using System.Linq;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Misc.SourceReference;

public class TheAspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        Console.WriteLine( meta.CompileTime( Path.GetFileName( meta.Target.Declaration.Sources.Single( s => s.IsImplementationPart ).Span.FilePath ) ) );

        return meta.Proceed();
    }
}

// <target>
internal class C
{
    [TheAspect]
    private void M() { }
}