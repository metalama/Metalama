// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Initialization.OnInitialized_Linker_Record_Inheritance_AspectOnBase;

public class TheAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.AddInitializer( nameof(InitializerTemplate), InitializerKind.AfterObjectInitializer );
    }

    [Template]
    private void InitializerTemplate()
    {
        Console.WriteLine( "Initialized!" );
    }
}

// <target>
[TheAspect]
public record BaseRecord( int X );

// <target>
public record DerivedRecord( int X, int Y ) : BaseRecord( X );

// <target>
public class Caller
{
    public void Method()
    {
        var d = new DerivedRecord( 1, 2 );
        var d2 = d with { Y = 5 };
    }
}
