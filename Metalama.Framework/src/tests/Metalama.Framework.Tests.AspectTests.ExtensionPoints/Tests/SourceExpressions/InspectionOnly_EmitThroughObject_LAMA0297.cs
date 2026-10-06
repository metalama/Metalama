// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Tests.ExtensionPoints;
using System.Linq;

namespace Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.SourceExpressions.InspectionOnly_EmitThroughObject_LAMA0297;

// A template that converts a compile-time object to a run-time value serializes it. When the object is an inspection-only expression, the
// serializer emits the expression, which fails with LAMA0297 as when the template emits the expression directly.

internal class EmitAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        object expression = builder.Target.Fields.OfName( "A" ).Single().TestGetInspectionOnlyInitializer();

        builder.IntroduceMethod( nameof(this.GetValue), args: new { value = expression } );
    }

    [Template]
    public object? GetValue( [CompileTime] object value ) => meta.RunTime( value );
}

// <target>
[Emit]
internal class Target
{
    public int A = 42;
}
