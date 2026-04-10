// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Initialization.OnConstructed_LocalFunction_Return;

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
        // `return;` inside a nested local function belongs to the local function's own
        // control flow and must NOT be rewritten. The constructor has no top-level return
        // statement, so no epilogue label should be emitted.
        LocalCheck();

        void LocalCheck()
        {
            if ( value < 0 )
            {
                return;
            }

            Console.WriteLine( value );
        }
    }
}
