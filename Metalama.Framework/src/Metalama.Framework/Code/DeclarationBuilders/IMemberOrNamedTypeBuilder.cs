// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Framework.Code.DeclarationBuilders;

/// <summary>
/// Allows to complete the construction of a member or named type that has been created by an advice.
/// </summary>
public interface IMemberOrNamedTypeBuilder : IMemberOrNamedType, IDeclarationBuilder
{
    /// <summary>
    /// Gets or sets the accessibility of the member.
    /// </summary>
    new Accessibility Accessibility { get; set; }

    /// <summary>
    /// Gets or sets the member name.
    /// </summary>
    new string Name { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the member is <c>static</c>.
    /// </summary>
    new bool IsStatic { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the member is <c>sealed</c>.
    /// </summary>
    new bool IsSealed { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the member is <c>abstract</c>.
    /// </summary>
    new bool IsAbstract { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the member is <c>partial</c>.
    /// </summary>
    new bool IsPartial { get; set; }
}