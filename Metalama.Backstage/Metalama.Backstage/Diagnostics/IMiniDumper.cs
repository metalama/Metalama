// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Extensibility;
using System;

namespace Metalama.Backstage.Diagnostics;

public interface IMiniDumper : IBackstageService
{
    bool MustWrite( Exception exception );

    string? Write( MiniDumpOptions? options = null );
}

public sealed record MiniDumpOptions( bool Compress = true )
{
    public static MiniDumpOptions Default { get; } = new();
}