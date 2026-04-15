// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Initialization.OnConstructed_Position_MultipleAdvices;

// Single [Inheritable] aspect contributing two BeforeBase and two AfterBase templates to
// the OnConstructed method. On the derived class, within-aspect programmatic add-order should be
// preserved in each bucket, with base.OnConstructed(context) sitting between them.

[Inheritable]
public class TheAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.AddInitializer( nameof(BeforeBase1), InitializerKind.AfterLastInstanceConstructor, InitializerPosition.BeforeBase );
        builder.AddInitializer( nameof(AfterBase1), InitializerKind.AfterLastInstanceConstructor, InitializerPosition.AfterBase );
        builder.AddInitializer( nameof(BeforeBase2), InitializerKind.AfterLastInstanceConstructor, InitializerPosition.BeforeBase );
        builder.AddInitializer( nameof(AfterBase2), InitializerKind.AfterLastInstanceConstructor, InitializerPosition.AfterBase );
    }

    [Template]
    private void BeforeBase1() => Console.WriteLine( "BeforeBase1" );

    [Template]
    private void BeforeBase2() => Console.WriteLine( "BeforeBase2" );

    [Template]
    private void AfterBase1() => Console.WriteLine( "AfterBase1" );

    [Template]
    private void AfterBase2() => Console.WriteLine( "AfterBase2" );
}

// <target>
[TheAspect]
public class BaseClass
{
}

// <target>
public class DerivedClass : BaseClass
{
}
