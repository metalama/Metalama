// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.Issue1093;
using System;

[Parent]
internal partial class Foo : IGotParent
{
    public Foo? Parent { get; set; }

    public EventHandler Event;

    public long Method() => 0;

    object? IGotParent.Property
    {
        get
        {
            return null;
        }
    }

    int IGotParent.Method()
    {
        return (int) 1;
    }

    private event Action _event = default !;

    event Action IGotParent.Event
    {
        add
        {
            this._event += value;
        }
        remove
        {
            this._event -= value;
        }
    }
}