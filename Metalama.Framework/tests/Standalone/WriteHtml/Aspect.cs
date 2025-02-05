using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Diagnostics;

public class MyAspect : OverrideMethodAspect
{
    private static readonly DiagnosticDefinition _diagnostic = new("MY001", Severity.Warning, "Some aspect warning.");

    public override void BuildAspect( IAspectBuilder<IMethod> builder )
    {
        base.BuildAspect( builder );
        builder.Diagnostics.Report( _diagnostic );
    }

    public override dynamic? OverrideMethod()
    {
        Console.WriteLine("Overridden.");
        return meta.Proceed();

    }
}