// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using System.Threading;

namespace Metalama.Framework.IntegrationTests.Aspects.Introductions.Fields.ReferencingStaticIntroductions;

internal class IdAttribute : TypeAspect
{
    [Introduce]
    private static int _nextId;

    [Introduce]
    public static int Id { get; } = Interlocked.Increment( ref _nextId );

    [Introduce]
    public static void Method( int? id )
    {
        if (id == null)
        {
            Method( Id );
        }
    }
}

// <target>
[Id]
internal class TargetClass { }