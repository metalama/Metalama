// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Patterns.Wpf.Implementation.NamingConvention;

[CompileTime]
internal readonly struct InspectedMember
{
    internal InspectedMember( IMemberOrNamedType member, bool isValid, string? category )
    {
        this.Member = member;
        this.IsValid = isValid;
        this.Category = category;
    }

    public IMemberOrNamedType Member { get; }

    public bool IsValid { get; }

    public string? Category { get; }
}