// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;

namespace Metalama.Framework.Tests.Integration.Tests.Aspects.Bugs.Bug35558;

#pragma warning disable CS0169

public class TestAspect : TypeAspect
{
    public override void BuildAspect(IAspectBuilder<INamedType> builder)
    {
        foreach(var field in builder.Target.Fields)
        {
            // First override promoted
            var result = builder.Advice.Override(field, nameof(PropertyTemplate));
            builder.Advice.Override(result.Declaration, nameof(PropertyTemplate));
        }
    }

    [Template]
    public dynamic? PropertyTemplate
    {
        get
        {
            Console.WriteLine("Aspect");
            return meta.Proceed();
        }

        set
        {
            Console.WriteLine("Aspect");
            meta.Proceed();
        }
    }
}

// <target>
[TestAspect]
public partial class TargetClass
{
    private int _field1;
}
