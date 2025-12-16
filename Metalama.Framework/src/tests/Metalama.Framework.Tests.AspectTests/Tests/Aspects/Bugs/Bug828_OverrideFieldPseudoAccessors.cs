// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug828_OverrideFieldPseudoAccessors;

#pragma warning disable CS0169, CS0414

// Test that overriding field pseudo accessors works correctly
public class LogAccessAspect : TypeAspect
{
    public override void BuildAspect(IAspectBuilder<INamedType> builder)
    {
        foreach (var field in builder.Target.Fields)
        {
            // Override the field's pseudo accessors
            if (field.GetMethod != null)
            {
                builder.With(field.GetMethod).Override(nameof(OverrideGetter));
            }

            if (field.SetMethod != null)
            {
                builder.With(field.SetMethod).Override(nameof(OverrideSetter));
            }
        }
    }

    [Template]
    public dynamic? OverrideGetter()
    {
        Console.WriteLine($"Getting {meta.Target.Field.Name}");
        return meta.Proceed();
    }

    [Template]
    public void OverrideSetter()
    {
        Console.WriteLine($"Setting {meta.Target.Field.Name}");
        meta.Proceed();
    }
}

// <target>
[LogAccessAspect]
internal class C
{
    private int _field;
}
