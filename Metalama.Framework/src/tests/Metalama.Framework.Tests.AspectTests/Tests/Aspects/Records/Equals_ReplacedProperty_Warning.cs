// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System.Linq;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Records.Equals_ReplacedProperty_Warning;

/// <summary>
/// The aspect overrides the auto-property with a template that does not call the original implementation, so the
/// property loses its backing field. The materialized <c>Equals</c> has no field to read and compares the value that
/// the aspect returns, which the linker reports as <c>LAMA0653</c>.
/// </summary>
public class OverrideAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.IntroduceMethod( nameof(IntroducedEquals), whenExists: OverrideStrategy.Override, args: new { T = builder.Target } );
        builder.With( builder.Target.Properties.Single( p => p.Name == "Value" ) ).Override( nameof(IntroducedProperty) );
    }

    [Template( Name = "Equals" )]
    public bool IntroducedEquals<[CompileTime] T>( T? other )
    {
        return meta.Proceed();
    }

    [Template]
    public dynamic? IntroducedProperty
    {
        get
        {
            return 42;
        }

        set { }
    }
}

// <target>
[Override]
internal record Target
{
    public int Value { get; set; }
}
