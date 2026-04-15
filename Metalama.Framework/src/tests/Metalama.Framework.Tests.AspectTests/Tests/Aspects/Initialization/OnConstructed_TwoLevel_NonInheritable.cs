// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Initialization.OnConstructed_TwoLevel_NonInheritable;

// Regression test for https://github.com/metalama/Metalama/issues/1580.
// The aspect is intentionally NOT marked [Inheritable]. The documentation states that because the
// generated OnConstructed method is protected virtual, a derived type inherits the initialization
// chain automatically: its constructor ends with the call to OnConstructed and passes a descended
// context to base. The bug was that the derived class in the same compilation received only the
// pulled InitializationContext parameter and forwarded it unchanged to base, skipping OnConstructed.
public class TheAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.AddInitializer( nameof(InitializerTemplate), InitializerKind.AfterLastInstanceConstructor );
    }

    [Template]
    private void InitializerTemplate()
    {
        Console.WriteLine( $"OnConstructed on {meta.Target.Type.Name}!" );
    }
}

// <target>
[TheAspect]
public class BaseClass
{
    public BaseClass( int x )
    {
        _ = x;
    }
}

// <target>
public class DerivedClass : BaseClass
{
    public DerivedClass() : base( 0 )
    {
    }
}
