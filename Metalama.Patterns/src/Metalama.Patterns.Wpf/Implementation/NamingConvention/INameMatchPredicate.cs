// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using System.Collections.Immutable;

namespace Metalama.Patterns.Wpf.Implementation.NamingConvention;

[CompileTime]
internal interface INameMatchPredicate
{
    bool IsMatch( string name );

    ImmutableArray<string> Candidates { get; }
}