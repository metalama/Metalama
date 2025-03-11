// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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