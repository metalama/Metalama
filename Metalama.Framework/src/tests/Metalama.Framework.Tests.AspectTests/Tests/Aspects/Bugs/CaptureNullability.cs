// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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