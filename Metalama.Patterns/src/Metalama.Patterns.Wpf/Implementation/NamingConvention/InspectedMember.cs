// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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