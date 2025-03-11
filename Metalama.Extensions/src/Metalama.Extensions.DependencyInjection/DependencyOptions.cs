// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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