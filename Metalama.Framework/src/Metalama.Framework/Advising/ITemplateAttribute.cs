// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Framework.Advising;

/// <summary>
/// Defines an interface for attributes that mark and configure T# template members. Template attributes allow you to
/// override properties such as the name, accessibility, and modifiers (virtual, sealed, abstract, etc.) of members
/// introduced from templates. When a property is not explicitly set, its value is inherited from the template member itself.
/// </summary>
/// <seealso cref="IAdviceAttribute"/>
/// <seealso cref="TemplateAttribute"/>
/// <seealso cref="TemplateAttributeProperties"/>
public interface ITemplateAttribute : IAdviceAttribute
{
    // We are using this design (to expose properties as an object) to make is possible to add more properties
    // in later versions.

    /// <summary>
    /// Gets the properties that configure how the template member should be introduced, including name, accessibility,
    /// and modifiers. Returns <c>null</c> if no properties have been explicitly set.
    /// </summary>
    TemplateAttributeProperties? Properties { get; }
}