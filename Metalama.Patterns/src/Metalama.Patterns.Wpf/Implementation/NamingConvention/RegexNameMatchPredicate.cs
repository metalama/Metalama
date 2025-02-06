// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using System.Collections.Immutable;
using System.Text.RegularExpressions;

namespace Metalama.Patterns.Wpf.Implementation.NamingConvention;

[CompileTime]
internal sealed class RegexNameMatchPredicate : INameMatchPredicate
{
    private readonly Regex _matchName;

    public RegexNameMatchPredicate( Regex matchName )
    {
        this._matchName = matchName;
        this.Candidates = ImmutableArray.Create( matchName.ToString() );
    }

    public bool IsMatch( string name ) => this._matchName.IsMatch( name );

    public ImmutableArray<string> Candidates { get; }
}