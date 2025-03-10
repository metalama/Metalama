// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using K4os.Hash.xxHash;
using Metalama.Framework.Engine.AdditionalOutputs;
using Metalama.Framework.Engine.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;

namespace Metalama.Framework.DesignTime.SourceGeneration;

/// <summary>
/// An implementation of <see cref="SourceGeneratorResult"/> that is backed by a set of
/// files on the filesystem, generated at compile-time when the design-time pipeline is disabled.
/// </summary>
internal sealed class OfflineSourceGeneratorResult : SourceGeneratorResult
{
    public ImmutableArray<AdditionalCompilationOutputFile> OfflineFiles { get; }

    public OfflineSourceGeneratorResult( ImmutableArray<AdditionalCompilationOutputFile> offlineFiles )
    {
        this.OfflineFiles = offlineFiles;
    }

    protected override ulong ComputeDigest()
    {
        var xxh = new XXH64();
        ulong hash = 0;

        foreach ( var file in this.OfflineFiles )
        {
            xxh.Reset();
            xxh.Update( file.Path );
            xxh.Update( File.GetLastWriteTime( file.Path ).ToFileTimeUtc() );

            hash ^= xxh.Digest();
        }

        return hash;
    }

    internal override void ProduceContent( SourceProductionContext context )
    {
        foreach ( var file in this.OfflineFiles )
        {
            using var stream = file.GetStream();
            context.AddSource( Path.GetFileName( file.Path ), SourceText.From( stream ) );
        }
    }

    public override string ToString() => $"{nameof(OfflineSourceGeneratorResult)} Count={this.OfflineFiles.Length}";
}