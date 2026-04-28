// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using System;

#pragma warning disable CS0067

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug690;

public class MyAspect : OverrideEventAspect
{
    public override void OverrideAdd( dynamic value )
    {
        Console.WriteLine( "Add" );
        meta.Proceed();
    }

    public override void OverrideRemove( dynamic value )
    {
        Console.WriteLine( "Remove" );
        meta.Proceed();
    }
}

// <target>
internal class TargetClass
{
    // Without target specifier (should work).
    [MyAspect]
    public event EventHandler? E1;

    // With explicit event target specifier (should also work).
    [event: MyAspect]
    public event EventHandler? E2;
}