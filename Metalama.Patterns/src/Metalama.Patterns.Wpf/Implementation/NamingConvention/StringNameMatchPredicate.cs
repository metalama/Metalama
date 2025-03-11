// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using System.Collections.Immutable;

namespace Metalama.Patterns.Wpf.Implementation.NamingConvention;

[CompileTime]
internal sealed class StringNameMatchPredicate : INameMatchPredicate
{
    public StringNameMatchPredicate( string name )
    {
        this.Candidates = ImmutableArray.Create( name );
    }

    public StringNameMatchPredicate( ImmutableArray<string> names )
    {
        this.Candidates = names;
    }

    public bool IsMatch( string name )
    {
        foreach ( var candidate in this.Candidates )
        {
            if ( candidate.Equals( name, StringComparison.Ordinal ) )
            {
                return true;
            }
        }

        return false;
    }

    public ImmutableArray<string> Candidates { get; }
}