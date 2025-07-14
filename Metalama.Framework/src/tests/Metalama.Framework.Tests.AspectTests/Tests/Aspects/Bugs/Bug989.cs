// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Advising;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug989;

public class TheAspect : TypeAspect
{
    
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        base.BuildAspect( builder );

        var targetType = builder.Target;
        builder.IntroduceMethod( nameof(this.IntroducedUntypedEquals), whenExists: OverrideStrategy.Override, args: new { T = targetType } );
    }
    
    [Template( Name = "Equals" )]
    public bool IntroducedUntypedEquals<[CompileTime] T>( object? other )
        => other is T typed && meta.This.Equals( typed );
}

// <target>
[TheAspect]
public class C;