// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @AcceptInvalidInput
#endif

using System.Linq;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Aspects.CodeModel.ComplexTypedConstant_Invalid;

public class Aspect : TypeAspect
{
    [Template]
    private int[] P { get; } = null!;

    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        var typedConstant = TypedConstant.Create( new[] { 42 } );
        builder.With( builder.Target.Constructors.Single() ).IntroduceParameter( "p", typeof(int[]), typedConstant );
    }
}

// <target>
[Aspect]
internal class TargetCode { }