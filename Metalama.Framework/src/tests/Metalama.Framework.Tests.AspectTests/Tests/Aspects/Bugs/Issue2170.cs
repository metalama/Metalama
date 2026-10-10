// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Issue2170;

// LAMA0237 must stop the creation of the compile-time project. No other error must be reported.

public class RunTimeOnlyClass { }

public abstract class AbstractAspect : FieldOrPropertyAspect
{
    public override void BuildAspect( IAspectBuilder<IFieldOrProperty> builder )
    {
        builder.Override( nameof(OverrideProperty) );
    }

    [Template]
    public abstract RunTimeOnlyClass OverrideProperty { get; set; }
}

public class DerivedAspect : AbstractAspect
{
    public override RunTimeOnlyClass OverrideProperty
    {
        get => throw new NotImplementedException();
        set => throw new NotImplementedException();
    }
}

// <target>
internal class Target
{
    [DerivedAspect]
    private RunTimeOnlyClass? _field;
}
