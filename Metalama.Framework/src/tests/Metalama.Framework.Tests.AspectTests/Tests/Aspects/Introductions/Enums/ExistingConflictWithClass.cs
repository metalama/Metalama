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

        // The outcome is compared rather than rendered. AdviceOutcome declares Ignored as a synonym of Ignore, and
        // Enum.ToString chooses between two names of one value differently on .NET Framework and on .NET, so a test
        // that rendered it would expect a different output under each target framework of this project.
        builder.IntroduceMethod(
            nameof(ReportTemplate),
            args: new { isIgnored = ignored.Outcome == AdviceOutcome.Ignore, typeKind = ignored.Declaration.TypeKind.ToString() } );

        builder.IntroduceEnum( "OtherConflicting", e => e.AddMember( "None" ), whenExists: OverrideStrategy.New );
    }

    [Template]
    public void ReportTemplate( [CompileTime] bool isIgnored, [CompileTime] string typeKind )
    {
        Console.WriteLine( $"Ignore reported the ignored outcome: {isIgnored}. The existing type is a {typeKind}." );
    }
}

// <target>
[Introduction]
public class TargetType : BaseClass { }
