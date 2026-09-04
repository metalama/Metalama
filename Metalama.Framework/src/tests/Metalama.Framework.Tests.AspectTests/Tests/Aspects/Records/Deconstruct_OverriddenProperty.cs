// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System.Linq;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Records.Deconstruct_OverriddenProperty;

/// <summary>
/// The aspect overrides both a positional property and the synthesized <c>Deconstruct</c>. The C# compiler assigns
/// the value of the property, which it reads through the getter, so the materialized body calls the property and
/// reaches the advice.
/// </summary>
public class OverrideAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.IntroduceMethod( nameof(IntroducedDeconstruct), whenExists: OverrideStrategy.Override );
        builder.With( builder.Target.Properties.Single( p => p.Name == "X" ) ).Override( nameof(IntroducedProperty) );
    }

    [Template( Name = "Deconstruct" )]
    public void IntroducedDeconstruct( out int X, out string Y )
    {
        X = default;
        Y = default!;

        meta.Proceed();
    }

    [Template]
    public dynamic? IntroducedProperty
    {
        get
        {
            return meta.Proceed();
        }

        set
        {
            meta.Proceed();
        }
    }
}

// <target>
[Override]
internal record Target( int X, string Y );
