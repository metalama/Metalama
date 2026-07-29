// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using NewAspects;
using OldAspects;

namespace Consumer;

/// <summary>
/// Derives from both base classes, so that the compile-time closure of this project contains the compile-time
/// project of an assembly built against the old Metalama.Framework and one built against the new one.
/// </summary>
internal static class Program
{
    private static void Main()
    {
        System.Console.WriteLine( new OldDerived().GetOldMessage() );
        System.Console.WriteLine( new NewDerived().GetNewMessage() );
    }
}

public class OldDerived : OldBaseClass { }

public class NewDerived : NewBaseClass { }
