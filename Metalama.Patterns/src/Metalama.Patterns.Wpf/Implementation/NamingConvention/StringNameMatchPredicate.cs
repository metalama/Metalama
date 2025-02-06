// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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