// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Extensions.DependencyInjection;

/// <summary>
/// Specifies options for a dependency introduced by <see cref="DependencyInjectionExtensions.IntroduceDependency(IAdviser{INamedType}, IType, DependencyOptions)"/>.
/// </summary>
/// <seealso cref="DependencyProperties"/>
/// <seealso cref="IntroduceDependencyAttribute"/>
/// <seealso href="@dependency-injection"/>
[CompileTime]
[PublicAPI]
public sealed record DependencyOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether the dependency field or property is static.
    /// </summary>
    public bool IsStatic { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether the dependency is required. When set to <c>false</c>,
    /// the generated code will accept missing dependencies without throwing an exception.
    /// </summary>
    public bool? IsRequired { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether the dependency should be resolved lazily upon first use.
    /// When set to <c>false</c>, the dependency is resolved during object construction.
    /// </summary>
    public bool? IsLazy { get; init; }

    /// <summary>
    /// Gets or sets the name of the field or property that will expose the dependency in the target type.
    /// </summary>
    public string? MemberName { get; init; }

    /// <summary>
    /// Gets or sets the kind of member to introduce, either <see cref="DeclarationKind.Field"/>
    /// or <see cref="DeclarationKind.Property"/>. The default is <see cref="DeclarationKind.Field"/>.
    /// </summary>
    public DeclarationKind MemberKind { get; init; } = DeclarationKind.Field;

    /// <summary>
    /// Gets the default <see cref="DependencyOptions"/> instance.
    /// </summary>
    public static DependencyOptions Default { get; } = new();
}