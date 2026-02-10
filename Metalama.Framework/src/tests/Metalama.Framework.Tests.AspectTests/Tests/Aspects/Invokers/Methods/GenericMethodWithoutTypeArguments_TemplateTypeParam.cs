// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Linq;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Methods.GenericMethodWithoutTypeArguments_TemplateTypeParam;

/*
 * Tests that invoking a generic method without specifying type arguments from a template with a compile-time type parameter
 * produces valid code. This is the exact scenario described in issue #765.
 */

public class TestData<T>
{
}

public class TestAspect : OverrideMethodAspect
{
    [Template]
    public dynamic? Template<[CompileTime] T>()
    {
        var method = meta.Target.Type.Methods.OfName( "Bar" ).Single();
        method.Invoke( new TestData<T>() );

        return meta.Proceed();
    }

    public override dynamic? OverrideMethod() => throw new System.NotSupportedException();

    public override void BuildAspect( IAspectBuilder<IMethod> builder )
    {
        builder.Override( nameof(Template), new { T = typeof(int) } );
    }
}

// <target>
public class TargetClass
{
    [TestAspect]
    public void Foo()
    {
    }

    public void Bar<T>( TestData<T> data )
    {
    }
}
