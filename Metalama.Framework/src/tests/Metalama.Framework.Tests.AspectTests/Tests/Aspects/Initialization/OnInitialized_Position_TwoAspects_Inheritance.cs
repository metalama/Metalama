// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Tests.AspectTests.Tests.Aspects.Initialization.OnInitialized_Position_TwoAspects_Inheritance;
using System;

// Cross-aspect ordering convention for BeforeBase/AfterBase (spec §5.2.1):
//   - BeforeBase across aspects: outermost-first (outer aspect's pre-base statements run first).
//   - AfterBase across aspects: innermost-first (inner aspect's post-base statements run first,
//     so the matryoshka unwinds correctly).
// With AspectOrderDirection.RunTime, the first aspect in the list runs first at runtime and is
// therefore the outermost layer. AspectOrder lists FirstAspect first, so FirstAspect is
// outermost and SecondAspect is innermost. On the derived class we therefore expect:
//   First.BeforeBase, Second.BeforeBase, base.Initialize, Second.AfterBase, First.AfterBase.
[assembly: AspectOrder( AspectOrderDirection.RunTime, typeof(FirstAspect), typeof(SecondAspect) )]

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Initialization.OnInitialized_Position_TwoAspects_Inheritance;

[Inheritable]
public class FirstAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.AddInitializer( nameof(BeforeBase), InitializerKind.AfterObjectInitializer, InitializerPosition.BeforeBase );
        builder.AddInitializer( nameof(AfterBase), InitializerKind.AfterObjectInitializer, InitializerPosition.AfterBase );
    }

    [Template]
    private void BeforeBase() => Console.WriteLine( "First.BeforeBase" );

    [Template]
    private void AfterBase() => Console.WriteLine( "First.AfterBase" );
}

[Inheritable]
public class SecondAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.AddInitializer( nameof(BeforeBase), InitializerKind.AfterObjectInitializer, InitializerPosition.BeforeBase );
        builder.AddInitializer( nameof(AfterBase), InitializerKind.AfterObjectInitializer, InitializerPosition.AfterBase );
    }

    [Template]
    private void BeforeBase() => Console.WriteLine( "Second.BeforeBase" );

    [Template]
    private void AfterBase() => Console.WriteLine( "Second.AfterBase" );
}

// <target>
[FirstAspect]
[SecondAspect]
public class BaseClass
{
}

// <target>
public class DerivedClass : BaseClass
{
}
