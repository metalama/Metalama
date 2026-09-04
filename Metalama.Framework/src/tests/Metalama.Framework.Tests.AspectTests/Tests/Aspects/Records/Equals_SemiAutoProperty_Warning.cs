// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Records.Equals_SemiAutoProperty_Warning;

/// <summary>
/// The property is declared with the <c>field</c> keyword and its getter has a body that returns something else than the
/// backing field. The C# compiler compares the backing field in the synthesized <c>Equals</c>, whereas the materialized one
/// has no name for that field and calls the getter, which the linker reports as <c>LAMA0654</c>.
/// </summary>
public class OverrideAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.IntroduceMethod( nameof(IntroducedEquals), whenExists: OverrideStrategy.Override, args: new { T = builder.Target } );
    }

    [Template( Name = "Equals" )]
    public bool IntroducedEquals<[CompileTime] T>( T? other )
    {
        return meta.Proceed();
    }
}

// <target>
[Override]
internal record Target
{
    public int Offset { get => field + 1; set => field = value; }
}
