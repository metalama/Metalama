// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using K4os.Hash.xxHash;
using Metalama.Framework.Engine.Utilities;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

namespace Metalama.Framework.DesignTime.SourceGeneration;

/// <summary>
/// An implementation of <see cref="SourceGeneratorResult"/> backed by strings. It is used in the VS user process,
/// because the strings come deserialized from the analysis process.
/// </summary>
internal sealed class TextSourceGeneratorResult : SourceGeneratorResult
{
    private readonly ImmutableDictionary<string, string> _sources;

    public TextSourceGeneratorResult( ImmutableDictionary<string, string> sources )
    {
        this._sources = sources;
    }

    protected override ulong ComputeDigest()
    {
        var xxh = new XXH64();
        ulong hash = 0;

        foreach ( var source in this._sources )
        {
            xxh.Reset();
            xxh.Update( source.Key );
            xxh.Update( source.Value );

            hash ^= xxh.Digest();
        }

        return hash;
    }

    internal override void ProduceContent( SourceProductionContext context )
    {
        foreach ( var source in this._sources )
        {
            context.AddSource( source.Key, source.Value );
        }
    }

    public override string ToString() => $"{nameof(TextSourceGeneratorResult)} Count={this._sources.Count}";
}