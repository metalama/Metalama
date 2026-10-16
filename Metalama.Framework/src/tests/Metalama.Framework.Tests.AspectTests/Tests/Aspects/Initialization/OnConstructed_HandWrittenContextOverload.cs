// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.RunTime.Initialization;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Initialization.OnConstructed_HandWrittenContextOverload;

public class InitializerAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.AddInitializer( nameof(InitializerTemplate), InitializerKind.AfterLastInstanceConstructor );
    }

    [Template]
    private void InitializerTemplate()
    {
        Console.WriteLine( $"OnConstructed {meta.Target.Type.Name}" );
    }
}

// <target>
public class BaseClass
{
    public BaseClass()
    {
        Console.WriteLine( "BaseClass()" );
    }

    public BaseClass( InitializationContext context )
    {
        Console.WriteLine( "BaseClass(InitializationContext)" );
    }
}

// <target>
[InitializerAspect]
public class DerivedClass : BaseClass
{
    public DerivedClass() : base() { }
}

// <target>
[InitializerAspect]
public class ImplicitDerivedClass : BaseClass
{
}

internal class Program
{
    public static void TestMain()
    {
        _ = new DerivedClass();
        _ = new ImplicitDerivedClass();
    }
}
