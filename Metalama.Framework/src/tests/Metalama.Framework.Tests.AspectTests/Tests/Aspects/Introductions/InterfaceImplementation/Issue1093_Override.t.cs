// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;

[Parent]
internal partial class Foo : IGotParent
{
    object? IGotParent.Property
    {
        get
        {
            return null;
        }
    }

    event Action IGotParent.Event
    {
        add
        {
            Console.WriteLine( "Adding" );
        }
        remove
        {
            Console.WriteLine( "Removing" );
        }
    }

    int IGotParent.Method()
    {
        return (int) 1;
    }
}