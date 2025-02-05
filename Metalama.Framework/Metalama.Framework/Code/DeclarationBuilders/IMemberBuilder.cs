// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Metalama.Framework.Code.DeclarationBuilders;

/// <summary>
/// Allows to complete the construction of a member (but not a named type) that has been created by an advice.
/// </summary>
public interface IMemberBuilder : IMemberOrNamedTypeBuilder, IMember
{
    /// <summary>
    /// Gets or sets a value indicating whether the member is <c>virtual</c>.
    /// </summary>
    new bool IsVirtual { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the member is <c>extern</c>.
    /// </summary>
    new bool IsExtern { get; set; }
}