// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @RequiredConstant(NET8_0_OR_GREATER)
#endif

#if NET8_0_OR_GREATER
using System.Runtime.CompilerServices;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp12.InlineArrays_CompileTime_Partial;

public class TheAspect : OverrideMethodAspect
{
    public override void BuildAspect( IAspectBuilder<IMethod> builder )
    {
        base.BuildAspect( builder );

        var buffer = new Buffer();

        for ( var i = 0; i < 10; i++ )
        {
            buffer[i] = i;
        }
    }

    public override dynamic? OverrideMethod() => meta.Proceed();
}

// The declaration is split so that the attribute is applied to a different part than the one that declares the field.
// The diagnostic must be reported once, and not once per part.
[RunTimeOrCompileTime]
public partial struct Buffer
{
    private int _element0;
}

#pragma warning disable CS0436 // Type conflicts with imported type
[InlineArray( 10 )]
#pragma warning restore CS0436
public partial struct Buffer { }

public class C
{
    [TheAspect]
    private void M() { }
}
#endif
