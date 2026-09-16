// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Enums.ExistingConflictWithClass_Fail;

public class BaseClass
{
    public class Conflicting { }
}

public class IntroductionAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        // Fail reports that the name is taken, whatever the kind of the type that takes it.
        builder.IntroduceEnum( "Conflicting", e => e.AddMember( "None" ), whenExists: OverrideStrategy.Fail );
    }
}

// <target>
[Introduction]
public class TargetType : BaseClass { }
