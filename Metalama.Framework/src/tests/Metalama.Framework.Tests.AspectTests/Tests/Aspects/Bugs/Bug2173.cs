// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug2173;

public abstract class BaseAspect : OverrideMethodAspect
{
    private string? _message;

    public virtual string? Message
    {
        get => this._message;
        set => this._message = value;
    }
}

// The override declares only a getter. The setter is inherited from BaseAspect.Message.
public class TheAspect : BaseAspect
{
    public override string? Message => base.Message;

    public override dynamic? OverrideMethod()
    {
        Console.WriteLine( meta.CompileTime( this.Message ) );

        return meta.Proceed();
    }
}

// The setter is inherited through two getter-only overrides.
public class DerivedAspect : TheAspect
{
    public override string? Message => base.Message;
}

// <target>
public class C
{
    [TheAspect( Message = "Hello" )]
    public void M() { }

    [DerivedAspect( Message = "World" )]
    public void N() { }
}
