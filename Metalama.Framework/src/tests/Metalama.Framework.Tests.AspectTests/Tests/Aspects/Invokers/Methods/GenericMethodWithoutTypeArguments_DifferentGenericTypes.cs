// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Collections.Generic;
using System.Linq;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Methods.GenericMethodWithoutTypeArguments_DifferentGenericTypes;

/*
 * Tests that when the parameter type is a generic type (e.g. List<T>) but the argument is a different generic type
 * with the same arity (e.g. HashSet<int>), type argument inference fails with an error telling the user
 * to supply type arguments explicitly.
 * Regression test for https://github.com/metalama/Metalama/issues/765
 */

public class TestAspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        var method = meta.Target.Type.Methods.OfName( "Bar" ).Single();

        // The method expects List<T> but we pass HashSet<int>.
        // The generic type definitions differ, so T should not be inferred.
        method.Invoke( new HashSet<int>() );

        return meta.Proceed();
    }
}

// <target>
public class TargetClass
{
    [TestAspect]
    public void Foo()
    {
    }

    public void Bar<T>( List<T> items )
    {
    }
}
