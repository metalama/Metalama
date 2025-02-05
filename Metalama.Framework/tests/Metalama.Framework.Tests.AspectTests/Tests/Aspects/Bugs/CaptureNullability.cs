using System.ComponentModel;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.SyntaxBuilders;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.CaptureNullability;

public class TheAspect : MethodAspect
{
    public override void BuildAspect( IAspectBuilder<IMethod> builder )
    {
        builder.Override( nameof(OverrideMethod) );
    }

    [Template]
    private void OverrideMethod( PropertyChangedEventArgs nonNullable, PropertyChangedEventArgs? nullable )
    {
        var nonNullableExpression = ExpressionFactory.Capture( nonNullable );
        var nullableExpression = ExpressionFactory.Capture( nullable );

        // The null-forgiving token should stay.
        _ = nullableExpression.Value!.PropertyName;

        // The null-forgiving token should be removed.
        _ = nonNullableExpression.Value!.PropertyName;

        meta.Proceed();
    }
}

// <target>
internal class C
{
    [TheAspect]
    private void M( PropertyChangedEventArgs nonNullable, PropertyChangedEventArgs? nullable ) { }
}