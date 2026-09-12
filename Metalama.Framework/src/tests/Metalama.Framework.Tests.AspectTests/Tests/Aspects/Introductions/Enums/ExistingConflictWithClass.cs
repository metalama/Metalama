// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Enums.ExistingConflictWithClass;

public class BaseClass
{
    public class Conflicting { }

    public class OtherConflicting { }
}

public class IntroductionAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        // The type that the base class declares is a class and the introduced one is an enum, so the conflict is
        // between two kinds. OverrideStrategy matches by name and by the number of type parameters, and not by kind.
        var ignored = builder.IntroduceEnum( "Conflicting", e => e.AddMember( "None" ), whenExists: OverrideStrategy.Ignore );

        builder.IntroduceMethod(
            nameof(ReportTemplate),
            args: new { outcome = ignored.Outcome.ToString(), typeKind = ignored.Declaration.TypeKind.ToString() } );

        builder.IntroduceEnum( "OtherConflicting", e => e.AddMember( "None" ), whenExists: OverrideStrategy.New );
    }

    [Template]
    public void ReportTemplate( [CompileTime] string outcome, [CompileTime] string typeKind )
    {
        Console.WriteLine( $"Ignore returned {outcome}, a {typeKind}." );
    }
}

// <target>
[Introduction]
public class TargetType : BaseClass { }
