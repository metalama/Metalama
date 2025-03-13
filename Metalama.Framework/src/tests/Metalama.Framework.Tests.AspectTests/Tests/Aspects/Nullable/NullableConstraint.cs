// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;
using System.IO;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Nullable.NullableConstraint;

internal class MyAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        base.BuildAspect( builder );

        builder.ImplementInterface( typeof(I) );
    }

    [Introduce]
    private void M<T1, T2>()
        where T1 : class?
        where T2 : Stream? { }

    [InterfaceMember]
    public void IM<T1, T2>() { }
}

internal interface I
{
    void IM<T1, T2>()
        where T1 : class?
        where T2 : Stream?;
}

// <target>
[MyAspect]
internal class C { }