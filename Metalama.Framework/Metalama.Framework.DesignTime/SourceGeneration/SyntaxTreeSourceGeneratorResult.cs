// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using K4os.Hash.xxHash;
using Metalama.Framework.DesignTime.Pipeline.Diff;
using Metalama.Framework.Engine;
using Metalama.Framework.Engine.Pipeline;
using Metalama.Framework.Engine.Utilities;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

namespace Metalama.Framework.DesignTime.SourceGeneration;

/// <summary>
/// An implementation of <see cref="SourceGeneratorResult"/> backed by a set of <see cref="SyntaxTree"/>.
/// </summary>
internal sealed class SyntaxTreeSourceGeneratorResult : SourceGeneratorResult
{
    public ImmutableDictionary<string, IntroducedSyntaxTree> AdditionalSources { get; }

    internal SyntaxTreeSourceGeneratorResult( ImmutableDictionary<string, IntroducedSyntaxTree> additionalSources )
    {
        this.AdditionalSources = additionalSources;
    }

    protected override ulong ComputeDigest()
    {
        var xxh = new XXH64();
        var hasher = new RunTimeCodeHasher( xxh );
        ulong hash = 0;

#if DEBUG
        var uniqueHashes = new HashSet<ulong>();
#endif

        foreach ( var tree in this.AdditionalSources.Values )
        {
            xxh.Reset();
            xxh.Update( tree.Name );
            hasher.Visit( tree.GeneratedSyntaxTree.GetRoot() );

            var digest = xxh.Digest();

#if DEBUG
            if ( !uniqueHashes.Add( digest ) )
            {
                // It is essential that hashes are distinct, because identical hashes nullify themselves.
                throw new AssertionFailedException( "The hash is not distinct." );
            }
#endif

            hash ^= digest;
        }

        return hash;
    }

    internal override void ProduceContent( SourceProductionContext context )
    {
        foreach ( var source in this.AdditionalSources.Values )
        {
            context.AddSource( source.Name, source.GeneratedSyntaxTree.GetText( context.CancellationToken ) );
        }
    }

    public override string ToString() => $"{nameof(SyntaxTreeSourceGeneratorResult)} Count={this.AdditionalSources.Count}";
}