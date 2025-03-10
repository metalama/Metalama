// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;

namespace Metalama.Framework.Aspects
{
    /// <summary>
    /// Marks members that must be highlighted as "template keywords" in the IDE.
    /// </summary>
    [AttributeUsage( AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Class )]
    internal sealed class TemplateKeywordAttribute : Attribute
    {
        // TODO: This attribute and the Proceed attribute could be merged into one, if this attribute has a parameter
        // that accepts the category, and Proceeds is its own category.
    }
}