// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @RequiredConstant(ROSLYN_4_12_0_OR_GREATER)
#endif

#if ROSLYN_4_12_0_OR_GREATER

using Metalama.Framework.Aspects;
using System.Collections.Generic;
using System.Linq;

namespace Metalama.Framework.Tests.Integration.Tests.Aspects.CSharp13.ParamsCollections_Invocation;

public class TheAspect : TypeAspect
{
    [Introduce]
    private static void M()
    {
        var constructor = meta.Target.Type.Constructors.Single();

        var value = constructor.Invoke(1, 2, 3)!;

        var method = meta.Target.Type.Methods.Single();

        method.With(value).Invoke(1, 2, 3);

        value.Foo(1, 2, 3);
    }
}

// <target>
[TheAspect]
public class Target
{
    public Target(params List<int> ints) { }

    private void Foo(params List<int> ints) { }
}

#endif