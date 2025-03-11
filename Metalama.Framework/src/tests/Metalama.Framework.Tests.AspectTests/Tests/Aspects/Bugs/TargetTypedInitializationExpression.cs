// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.TargetTypedInitializationExpression;

public class TheAspect : TypeAspect
{
    [Introduce]
    public void IntroducedMethod()
    {
        var initializerExpression = meta.Target.Type.Fields.Single().InitializerExpression;
        _ = new StrongBox<object>( initializerExpression.Value );
    }
}

// <target>
[TheAspect]
public class C
{
    public List<int> List = [1, 2, 3];
}