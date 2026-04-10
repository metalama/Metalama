// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Initialization.OnConstructed_GenericBase;

// Verifies that BaseConstructorResolver.GetImplicitBaseConstructor correctly resolves the base
// constructor when the base type is a constructed generic type instance (non-trivial T->int mapping).
// In particular, exercises IntroducedConstructor.GetBaseConstructor() when `DerivedClass : BaseClass<int>`
// and `BaseClass<T>` has no explicit constructor (implicit parameterless ctor on the constructed type).

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
}
