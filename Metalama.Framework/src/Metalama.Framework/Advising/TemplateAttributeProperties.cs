// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Advising;

/// <summary>
/// Represents a set of properties that configure how a T# template member should be introduced into target code.
/// These properties allow overriding the name, accessibility, and modifiers of members introduced from templates.
/// When a property is <c>null</c>, its value is inherited from the template member itself.
/// </summary>
/// <remarks>
/// This record is used by <see cref="ITemplateAttribute"/> to store configuration properties for template members.
/// It supports progressive enhancement of template attributes in future versions by encapsulating all configuration
/// in a single object.
/// </remarks>
/// <seealso cref="ITemplateAttribute"/>
/// <seealso cref="TemplateAttribute"/>
[CompileTime]
public sealed record TemplateAttributeProperties(
    /// <summary>
    /// Gets the name of the introduced member. When <c>null</c>, the name is taken from the template member.
    /// </summary>
    string? Name = null,

    /// <summary>
    /// Gets the accessibility level of the introduced member (e.g., public, private, protected).
    /// When <c>null</c>, the accessibility is taken from the template member.
    /// </summary>
    Accessibility? Accessibility = null,

    /// <summary>
    /// Gets a value indicating whether the introduced member is virtual.
    /// When <c>null</c>, the value is taken from the template member.
    /// </summary>
    bool? IsVirtual = null,

    /// <summary>
    /// Gets a value indicating whether the introduced member is sealed.
    /// When <c>null</c>, the value is taken from the template member.
    /// </summary>
    bool? IsSealed = null,

    /// <summary>
    /// Gets a value indicating whether the introduced member is required (C# 11+ feature).
    /// When <c>null</c>, the value is taken from the template member.
    /// </summary>
    bool? IsRequired = null,

    /// <summary>
    /// Gets a value indicating whether the introduced member is abstract.
    /// When <c>null</c>, the value is taken from the template member.
    /// </summary>
    bool? IsAbstract = null,

    /// <summary>
    /// Gets a value indicating whether the introduced member is partial.
    /// When <c>null</c>, the value is taken from the template member.
    /// </summary>
    bool? IsPartial = null,

    /// <summary>
    /// Gets a value indicating whether the introduced member is extern.
    /// When <c>null</c>, the value is taken from the template member.
    /// </summary>
    bool? IsExtern = null );