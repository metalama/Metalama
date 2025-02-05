// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Extensions.DependencyInjection;

[CompileTime]
public sealed record DependencyOptions
{
    public bool IsStatic { get; init; }

    public bool? IsRequired { get; init; }

    public bool? IsLazy { get; init; }

    public string? MemberName { get; init; }

    public DeclarationKind MemberKind { get; init; } = DeclarationKind.Field;

    public static DependencyOptions Default { get; } = new();
}