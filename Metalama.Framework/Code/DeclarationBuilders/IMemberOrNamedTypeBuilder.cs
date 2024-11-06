// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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