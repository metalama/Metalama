// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

#pragma warning disable CS0169 // The field 'Target.i' is never used

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.AbstractPropertyTemplate;

public abstract class AbstractAspect : FieldOrPropertyAspect
{
    public override void BuildAspect( IAspectBuilder<IFieldOrProperty> builder )
    {
        builder.Override( nameof(OverrideProperty) );
    }

    [Template]
    public abstract dynamic? OverrideProperty { get; set; }
}

public class DerivedAspect : AbstractAspect
{
    public override dynamic? OverrideProperty
    {
        get => throw new NotImplementedException();
        set => throw new NotImplementedException();
    }
}

// <target>
internal class Target
{
    [DerivedAspect]
    private int i;
}