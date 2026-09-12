// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Enums.InTemplate;

public class IntroductionAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        var introducedEnum = builder.IntroduceEnum(
            "Level",
            e =>
            {
                e.Accessibility = Accessibility.Public;
                e.AddMember( "None" );
                e.AddMember( "High", 10 );
            } );

        var introducedDelegate = builder.IntroduceDelegate(
            "Handler",
            d =>
            {
                d.Accessibility = Accessibility.Public;
                d.ReturnType = TypeFactory.GetType( SpecialType.Void );
                d.AddParameter( "value", typeof(int) );
            } );

        // A template body reads the introduced type through a compile-time argument, which is how an aspect uses a
        // type it has just introduced.
        builder.IntroduceMethod(
            nameof(MethodTemplate),
            args: new { enumType = introducedEnum.Declaration, delegateType = introducedDelegate.Declaration } );
    }

    [Template]
    public void MethodTemplate( [CompileTime] INamedType enumType, [CompileTime] INamedType delegateType )
    {
        Console.WriteLine( typeof(void) == typeof(void) ? enumType.Name : delegateType.Name );

        var level = meta.CompileTime( enumType );
        Console.WriteLine( level.Fields.Count );
    }
}

// <target>
[Introduction]
public class TargetType { }
