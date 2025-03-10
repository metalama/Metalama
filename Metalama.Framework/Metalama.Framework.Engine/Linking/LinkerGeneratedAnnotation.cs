// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;

namespace Metalama.Framework.Engine.Linking;

internal readonly struct LinkerGeneratedAnnotation
{
    public LinkerGeneratedFlags Flags { get; }

    public LinkerGeneratedAnnotation( LinkerGeneratedFlags flags )
    {
        this.Flags = flags;
    }

    public static LinkerGeneratedAnnotation FromString( string str )
    {
        // ReSharper disable once RedundantAssignment
        var success = Enum.TryParse<LinkerGeneratedFlags>( str, out var flags );

        Invariant.Assert( success );

        return new LinkerGeneratedAnnotation( flags );
    }

    public override string ToString() => $"{this.Flags}";
}