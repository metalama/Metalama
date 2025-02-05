using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;

class Aspect : OverrideMethodAspect
{
    public override void BuildAspect( IAspectBuilder<IMethod> builder )
    {
        base.BuildAspect( builder );

        var templates = new MethodTemplateSelector(
            nameof(this.OverrideMethod),
            nameof(this.OverrideAsyncMethod),
            nameof(this.OverrideEnumerableMethod),
            nameof(this.OverrideEnumeratorMethod),
            nameof(this.OverrideAsyncEnumerableMethod),
            nameof(this.OverrideAsyncEnumeratorMethod) );

        builder.Advice.Override( builder.Target, templates );
    }

    public override dynamic? OverrideMethod()
    {
        throw new NotImplementedException();
    }
}

class Target
{
    [Aspect]
    static void M() { }
}