// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Initialization.OnConstructed_GenericBase_ExplicitCtor;

// Verifies that PullConstructorParameterRecursive correctly walks from a constructed-generic base
// (`BaseClass<int>`) into the derived class with an explicit constructor. BaseConstructorResolver
// must be able to match the base ctor through the T->int substitution.

[Inheritable]
public class TheAspect : TypeAspect
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

[TheAspect]
public class BaseClass<T>
{
    public T? Value;
}

// <target>
public class DerivedClass : BaseClass<int>
{
    public DerivedClass( string name )
    {
        Console.WriteLine( name );
    }
}
