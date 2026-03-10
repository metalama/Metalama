// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.IntroducedType_Ignore_BaseImplements;

/*
 * Tests that ImplementInterface with OverrideStrategy.Ignore on an introduced type does NOT add the interface
 * when the base type of the introduced type already implements that interface (issue #625).
 */

public interface ITestInterface
{
    void TestMethod();
}

public class BaseType : ITestInterface
{
    public void TestMethod()
    {
        Console.WriteLine( "Base implementation" );
    }
}

public class IntroductionAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        var type = builder.IntroduceClass(
            "TestType",
            buildType: t => { t.BaseType = (INamedType) TypeFactory.GetType( typeof(BaseType) ); } );

        type.ImplementInterface( typeof(ITestInterface), whenExists: OverrideStrategy.Ignore );
    }

    [InterfaceMember]
    public void TestMethod()
    {
        Console.WriteLine( "Introduced implementation" );
    }
}

// <target>
[IntroductionAttribute]
public class TargetType { }
