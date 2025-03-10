// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @RequiredConstant(ROSLYN_4_8_0_OR_GREATER)
#endif

#if ROSLYN_4_8_0_OR_GREATER

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp12.RefReadonlyParameter_Introduce;

internal class TheAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        base.BuildAspect( builder );

        builder.IntroduceMethod(
            nameof(Programmatic),
            buildMethod: method =>
            {
                method.AddParameter( "i", typeof(int), RefKind.In );
                method.AddParameter( "j", typeof(int), RefKind.RefReadOnly );
            } );
    }

    [Introduce]
    private void Declarative( in int i, ref readonly int j ) { }

    [Template]
    private void Programmatic() { }
}

[TheAspect]
internal class C { }

#endif