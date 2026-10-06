// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Tests.ExtensionPoints;
using System;
using System.Linq;
using Metalama.Framework.Diagnostics;

namespace Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.SourceExpressions.InspectionOnly_EmitValue_LAMA0297;

// A template that emits an inspection-only expression fails with LAMA0297, because the expression is already evaluated where it is written.

internal class EmitAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        var expression = builder.Target.Fields.OfName( "A" ).Single().TestGetInspectionOnlyInitializer();

        builder.IntroduceMethod( nameof(this.GetValue), args: new { value = expression } );
    }

    [Template]
    public int GetValue( [CompileTime] IExpression value ) => value.Value;
}

// <target>
[Emit]
internal class Target
{
    public int A = 42;
}
