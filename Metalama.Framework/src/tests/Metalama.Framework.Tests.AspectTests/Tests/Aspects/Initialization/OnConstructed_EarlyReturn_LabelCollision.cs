// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Initialization.OnConstructed_EarlyReturn_LabelCollision;

// Regression test: the epilogue label name is allocated through the constructor's lexical scope,
// so when the user already declares a label named `epilogue` the generated label falls back to
// `epilogue_1` (or similar) via the linker's unique-name mechanism.
public class TheAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.AddInitializer( nameof(InitializerTemplate), InitializerKind.AfterLastInstanceConstructor );
    }

    [Template]
    private void InitializerTemplate()
    {
        Console.WriteLine( "OnConstructed!" );
    }
}

// <target>
[TheAspect]
public class TargetCode
{
    public TargetCode( int value )
    {
        if ( value < 0 )
        {
            goto epilogue;
        }

        Console.WriteLine( value );

        return;

    epilogue:
        Console.WriteLine( "user epilogue" );
    }
}
