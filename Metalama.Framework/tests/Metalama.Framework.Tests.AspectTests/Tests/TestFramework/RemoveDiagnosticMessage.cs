using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Diagnostics;

namespace Metalama.Framework.Tests.AspectTests.Tests.TestFramework.RemoveDiagnosticMessage;

public class TheAspect : TypeAspect
{
    public static DiagnosticDefinition MyWarning = new( "ID001", Severity.Warning, "The warning." );

    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.Diagnostics.Report( MyWarning );
    }
}

// <target>
[TheAspect]
internal class C { }