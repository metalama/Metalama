// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System;

namespace Metalama.Framework.Engine.Linking;

/// <exclude />
internal readonly struct AspectLinkerDeclarationAnnotation
{
    public AspectLinkerDeclarationFlags Flags { get; }

    public AspectLinkerDeclarationAnnotation( AspectLinkerDeclarationFlags flags )
    {
        this.Flags = flags;
    }

    public static AspectLinkerDeclarationAnnotation FromString( string str )
    {
        // ReSharper disable once RedundantAssignment
        var success = Enum.TryParse<AspectLinkerDeclarationFlags>( str, out var flags );

        Invariant.Assert( success );

        return new AspectLinkerDeclarationAnnotation( flags );
    }

    public override string ToString() => $"{this.Flags}";
}