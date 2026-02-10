// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Linq;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Methods.GenericMethodWithoutTypeArguments_ConflictingInference;

/*
 * Tests that when a type parameter appears in multiple positions with conflicting types (int vs string),
 * the type argument inference fails with an error telling the user to supply type arguments explicitly.
 * Regression test for https://github.com/metalama/Metalama/issues/765
 */

public class TestAspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        var method = meta.Target.Type.Methods.OfName( "Bar" ).Single();

        // T appears in both parameters but we pass int and string — conflicting inference.
        method.Invoke( 42, "hello" );

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

    public void Bar<T>( T first, T second )
    {
    }
}
