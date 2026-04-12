// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Tests.AspectTests.Tests.Aspects.Initialization.OnInitialized_TwoAspects_Inheritance;

[assembly: AspectOrder( AspectOrderDirection.RunTime, typeof(FirstAspect), typeof(SecondAspect) )]

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Initialization.OnInitialized_TwoAspects_Inheritance;

[Inheritable]
public class FirstAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.AddInitializer( nameof(Template1), InitializerKind.AfterObjectInitializer );
        builder.AddInitializer( nameof(Template2), InitializerKind.AfterObjectInitializer );
    }

    [Template]
    public void Template1()
    {
        Console.WriteLine( "First1" );
    }

    [Template]
    public void Template2()
    {
        Console.WriteLine( "First2" );
    }
}

[Inheritable]
public class SecondAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.AddInitializer( nameof(Template1), InitializerKind.AfterObjectInitializer );
        builder.AddInitializer( nameof(Template2), InitializerKind.AfterObjectInitializer );
    }

    [Template]
    public void Template1()
    {
        Console.WriteLine( "Second1" );
    }

    [Template]
    public void Template2()
    {
        Console.WriteLine( "Second2" );
    }
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
