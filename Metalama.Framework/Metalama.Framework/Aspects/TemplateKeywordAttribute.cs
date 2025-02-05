// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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