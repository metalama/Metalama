// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Utilities.Diagnostics;
using System.Collections.Immutable;

namespace Metalama.Framework.DesignTime.Pipeline;

public sealed partial class DesignTimeAspectPipelineResult
{
    internal sealed class Invalidator
    {
        private readonly DesignTimeAspectPipelineResult _parent;
        private readonly ImmutableDictionary<string, SyntaxTreePipelineResult>.Builder _syntaxTreeBuilders;
        private readonly ImmutableDictionary<string, SyntaxTreePipelineResult>.Builder _invalidSyntaxTreeBuilders;

        public Invalidator( DesignTimeAspectPipelineResult parent )
        {
            this._parent = parent;
            this._syntaxTreeBuilders = parent.SyntaxTreeResults.ToBuilder();

            this._invalidSyntaxTreeBuilders = parent._invalidSyntaxTreeResults.ToBuilder();
        }

        public void InvalidateSyntaxTree( string path )
        {
            Logger.DesignTime.Trace?.Log( $"DesignTimeSyntaxTreeResultCache.InvalidateCache({path}): removed from cache." );

            if ( this._syntaxTreeBuilders.TryGetValue( path, out var oldSyntaxTreeResult ) )
            {
                this._syntaxTreeBuilders.Remove( path );
                this._invalidSyntaxTreeBuilders.Add( path, oldSyntaxTreeResult );
            }
        }

        public DesignTimeAspectPipelineResult ToImmutable()
        {
            return new DesignTimeAspectPipelineResult(
                this._parent.Configuration,
                this._syntaxTreeBuilders.ToImmutable(),
                this._invalidSyntaxTreeBuilders.ToImmutable(),
                this._parent.IntroducedSyntaxTrees,
                this._parent._inheritableAspects,
                this._parent.Extensions,
                this._parent.InheritableOptions,
                this._parent.Annotations,
                this._parent.AspectInstancesHashCode );
        }
    }
}