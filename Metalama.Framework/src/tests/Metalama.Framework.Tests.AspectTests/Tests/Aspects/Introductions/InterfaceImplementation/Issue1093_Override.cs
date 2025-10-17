// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Advising;
using Metalama.Framework.Code;
using System;

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.Issue1093_Override;

// https://github.com/metalama/Metalama/issues/1093

internal interface IGotParent
{
    object? Property { get; }

    event Action Event;

    int Method();
}

internal class ParentAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> _builder )
    {
        base.BuildAspect( _builder );

        var result = _builder.ImplementInterface( typeof(IGotParent), OverrideStrategy.Override );

        result.ExplicitMembers.IntroduceProperty( nameof(IGotParent.Property), nameof(this.GetParentTemplate), null, whenExists: OverrideStrategy.Override );
        result.ExplicitMembers.IntroduceEvent( nameof(this.Event), whenExists: OverrideStrategy.Override );
        result.ExplicitMembers.IntroduceMethod( nameof(this.Method), whenExists: OverrideStrategy.Override );
    }

    [Template]
    private object? GetParentTemplate()
    {
        return null;
    }

    [Template]
    private event Action Event { add { Console.WriteLine( "Adding" ); } remove { Console.WriteLine( "Removing" ); } }

    [Template]
    private int Method() => 1;
}

// <target>
[Parent]
internal partial class Foo : IGotParent
{
    object? IGotParent.Property { get => new(); }

    event Action IGotParent.Event { add { Console.WriteLine( "Before" ); } remove { Console.WriteLine( "Before" ); } }

    int IGotParent.Method() => 0;
}