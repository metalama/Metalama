// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Eligibility;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Attributes.Remove_All;

public class MyAspect : Aspect, IAspect<IDeclaration>
{
    public void BuildEligibility( IEligibilityBuilder<IDeclaration> builder ) { }

    public void BuildAspect( IAspectBuilder<IDeclaration> builder )
    {
        builder.RemoveAttributes( GetType() );
    }
}

#pragma warning disable CS0414, CS8618, CS0067

internal class KeepItAttribute : Attribute { }

// <target>
[MyAspect]
internal class C
{
    [MyAspect]
    [KeepIt]
    private C() { }

    [MyAspect]
    [KeepIt]
    [return: MyAspect]
    private void M( [MyAspect] int p ) { }

    [MyAspect]
    [KeepIt]
    private int _a = 5, _b = 3;

    [MyAspect]
    [KeepIt]
    private event Action MyEvent1, MyEvent2;

    [MyAspect]
    [KeepIt]
    private event Action MyEvent3;

    [MyAspect]
    private event Action MyEvent4
    {
        add { }
        remove { }
    }

    [MyAspect]
    private class D { }

    [MyAspect]
    private struct S { }
}