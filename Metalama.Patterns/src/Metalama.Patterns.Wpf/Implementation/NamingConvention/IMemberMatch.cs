// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System.Collections.Immutable;

namespace Metalama.Patterns.Wpf.Implementation.NamingConvention;

[CompileTime]
internal interface IMemberMatch
{
    MemberMatchOutcome Outcome { get; }

    IMemberOrNamedType? Member { get; }

    ImmutableArray<string> CandidateNames { get; }
}