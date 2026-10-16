// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Initialization.OnConstructed_Forwarder_OptionalSourceParameter;

[Inheritable]
public class InitializerAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.AddInitializer(
            nameof(InitializerTemplate),
            InitializerKind.AfterLastInstanceConstructor,
            overloadingStrategy: ConstructorOverloadingStrategy.ForwardSourceConstructors );
    }

    [Template]
    private void InitializerTemplate()
    {
        Console.WriteLine( $"OnConstructed {meta.Target.Type.Name}" );
    }
}

// <target>
[InitializerAspect]
public class BaseClass
{
    public BaseClass( int count = 0 )
    {
        _ = count;
    }

    public BaseClass( string name )
    {
        _ = name;
    }
}

// <target>
public class DerivedClass : BaseClass
{
    public DerivedClass( int count = 0 ) : base( count ) { }
}

internal class Program
{
    public static void TestMain()
    {
        _ = new BaseClass();
        _ = new BaseClass( "name" );
        _ = new DerivedClass();
    }
}
