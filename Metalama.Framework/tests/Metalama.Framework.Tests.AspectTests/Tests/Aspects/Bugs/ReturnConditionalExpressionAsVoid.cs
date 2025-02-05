using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code.SyntaxBuilders;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.ReturnConditionalExpressionAsVoid;

internal class ConditionalPropertyAccess : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        return new RunTimeClass()?.P;
    }
}

internal class ConditionalMethodCall : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        meta.Proceed();
        var runTimeClass = ExpressionFactory.Capture( new RunTimeClass() ).Value;

        return runTimeClass?.M();
    }
}

internal class TargetCode
{
    // <target>
    [ConditionalMethodCall]
    [ConditionalPropertyAccess]
    private void Method() { }
}

internal class RunTimeClass
{
    public int P { get; }

    public void M() { }
}