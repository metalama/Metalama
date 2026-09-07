// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @LanguageVersion(preview)
// @TargetFrameworks(net48;net10.0)
#endif

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;

/*
 * The property counterpart of IntroduceMethodStatic. A non-virtual static property whose accessors have a body is
 * legal in an interface from C# 15 on, on every runtime. See issue #1938.
 */

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp15.StaticInterfaceMembers_IntroducePropertyStatic;

public class IntroductionAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        var @interface = builder.IntroduceInterface( "ITest" );
        @interface.IntroduceProperty( nameof(TestProperty) );
    }

    [Template]
    public static int TestProperty
    {
        get
        {
            Console.WriteLine( "Default" );

            return 0;
        }

        set
        {
            Console.WriteLine( "Default" );
        }
    }
}

// <target>
[IntroductionAttribute]
public class TargetType { }
