// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Tests.AspectTests.Tests.Aspects.Overrides.EventFields.InterfaceEventField;

[assembly: AspectOrder( AspectOrderDirection.RunTime, typeof(OverrideEventAttribute), typeof(OverrideAttribute), typeof(IntroductionAttribute) )]

#pragma warning disable CS0414

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Overrides.EventFields.InterfaceEventField;

internal interface Interface
{
    event EventHandler? EventField_Default;

    event EventHandler? EventField;
}

internal class IntroductionAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.ImplementInterface( typeof(Interface) );
    }

    [InterfaceMember( IsExplicit = true )]
    public event EventHandler? EventField_Default = default;

    [InterfaceMember( IsExplicit = true )]
    public event EventHandler? EventField = Foo;

    [Introduce]
    public static void Foo( object? sender, EventArgs args ) { }
}

internal class OverrideAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        foreach (var e in builder.AdvisedTarget.Events)
        {
            builder.With( e ).AddAspect<OverrideEventAttribute>();
        }
    }
}

public class OverrideEventAttribute : OverrideEventAspect
{
    public OverrideEventAttribute() { }

    public override void OverrideAdd( dynamic value )
    {
        Console.WriteLine( "Overriden add." );
        meta.Proceed();
    }

    public override void OverrideRemove( dynamic value )
    {
        Console.WriteLine( "Overriden remove." );
        meta.Proceed();
    }
}

// <target>
[Introduction]
[Override]
internal partial class TargetClass { }