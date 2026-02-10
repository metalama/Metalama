// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Linq;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Methods.GenericMethodWithoutTypeArguments_MultipleTypeParams;

/*
 * Tests that invoking a generic method with multiple type parameters correctly infers all type arguments.
 * Regression test for https://github.com/metalama/Metalama/issues/765
 */

public class TestAspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        var method = meta.Target.Type.Methods.OfName( "Bar" ).Single();
        method.Invoke( "hello", 42 );

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

    public void Bar<TKey, TValue>( TKey key, TValue value )
    {
    }
}
