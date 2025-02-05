// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System.Text;

namespace Metalama.Framework.Engine.Utilities.Caching;

internal sealed class StringBuilderPool : ObjectPool<StringBuilder>
{
    internal static StringBuilderPool Default { get; } = new( 128 );

    private StringBuilderPool( int capacity ) : base( () => new StringBuilder( capacity ) ) { }

    protected override void CleanUp( StringBuilder obj ) => obj.Clear();
}